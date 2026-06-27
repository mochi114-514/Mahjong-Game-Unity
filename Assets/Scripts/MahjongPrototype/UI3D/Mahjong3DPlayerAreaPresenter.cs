using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: sibling 3D presenter for SelfBottom hand only.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Player Area Presenter")]
    public sealed class Mahjong3DPlayerAreaPresenter : MonoBehaviour
    {
        [SerializeField] private Mahjong3DHandView selfBottomHandView;

        private bool warnedMissingSelfBottomHandView;

        public void Refresh(MahjongGameState state, bool canUseSelfInput)
        {
            RefreshSelfBottomHand(state, canUseSelfInput);
        }

        public void RefreshSelfBottomHand(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            if (selfBottomHandView == null)
            {
                WarnMissingOnce(
                    ref warnedMissingSelfBottomHandView,
                    "Self bottom 3D hand view is not assigned.");
                return;
            }

            SeatSlot selfSeatSlot = state.GetSeatSlot(state.SelfSeat);
            if (selfSeatSlot.IsEmpty)
            {
                selfBottomHandView.Clear();
                return;
            }

            PlayerSeat selfPlayerSeat = state.GetPlayerSeat(state.SelfSeat);
            selfBottomHandView.RenderHand(selfPlayerSeat.Hand.GetTiles(), true, canUseSelfInput);
        }

        public void ClearSelfBottomHand()
        {
            if (selfBottomHandView == null)
            {
                WarnMissingOnce(
                    ref warnedMissingSelfBottomHandView,
                    "Self bottom 3D hand view is not assigned.");
                return;
            }

            selfBottomHandView.Clear();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(Mahjong3DPlayerAreaPresenter)}: {message}", this);
        }
    }
}
