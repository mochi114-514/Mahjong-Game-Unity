using MahjongPrototype.Domain;
using MahjongPrototype.UI;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong Table Center Text Presenter")]
    public sealed class MahjongTableCenterTextPresenter : MonoBehaviour
    {
        [Header("Wind Texts")]
        [SerializeField] private TMP_Text selfBottomWindText;
        [SerializeField] private TMP_Text nextLeftWindText;
        [SerializeField] private TMP_Text acrossTopWindText;
        [SerializeField] private TMP_Text previousRightWindText;

        private bool warnedMissingWindTextReferences;

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
            {
                Clear();
                return;
            }

            WarnMissingWindTextReferences();
            Clear();
            SetWindTextForSeat(state, SeatId.East);
            SetWindTextForSeat(state, SeatId.South);
            SetWindTextForSeat(state, SeatId.West);
            SetWindTextForSeat(state, SeatId.North);
        }

        public void Clear()
        {
            SetText(selfBottomWindText, "-");
            SetText(nextLeftWindText, "-");
            SetText(acrossTopWindText, "-");
            SetText(previousRightWindText, "-");
        }

        private void SetWindTextForSeat(MahjongGameState state, SeatId seat)
        {
            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            SetText(GetWindText(viewSlot), ToJapaneseWind(seat));
        }

        private TMP_Text GetWindText(ViewSlot viewSlot)
        {
            switch (viewSlot)
            {
                case ViewSlot.SelfBottom:
                    return selfBottomWindText;
                case ViewSlot.NextLeft:
                    return nextLeftWindText;
                case ViewSlot.AcrossTop:
                    return acrossTopWindText;
                case ViewSlot.PreviousRight:
                default:
                    return previousRightWindText;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static string ToJapaneseWind(SeatId seat)
        {
            switch (seat)
            {
                case SeatId.East:
                    return "東";
                case SeatId.South:
                    return "南";
                case SeatId.West:
                    return "西";
                case SeatId.North:
                    return "北";
                default:
                    return "-";
            }
        }

        private void WarnMissingWindTextReferences()
        {
            if (selfBottomWindText != null &&
                nextLeftWindText != null &&
                acrossTopWindText != null &&
                previousRightWindText != null)
            {
                return;
            }

            WarnMissingOnce(
                ref warnedMissingWindTextReferences,
                "One or more center wind TMP_Text references are not assigned.");
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongTableCenterTextPresenter)}: {message}", this);
        }
    }
}
