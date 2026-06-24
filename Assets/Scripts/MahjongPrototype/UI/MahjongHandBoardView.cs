using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Hand Board View")]
    public sealed class MahjongHandBoardView : MonoBehaviour
    {
        [Header("Hand Slots")]
        [SerializeField] private MahjongHandView selfBottomHandView;
        [SerializeField] private MahjongHandView nextLeftHandView;
        [SerializeField] private MahjongHandView acrossTopHandView;
        [SerializeField] private MahjongHandView previousRightHandView;

        public MahjongHandView SelfBottomHandView => selfBottomHandView;

        public void ConfigureMissingReferences(MahjongHandView selfBottomFallback)
        {
            if (selfBottomHandView == null)
                selfBottomHandView = selfBottomFallback;
        }

        public void Render(MahjongGameState state, IReadOnlyList<SeatId> displaySeats, bool canUseSelfInput)
        {
            if (state == null || displaySeats == null)
            {
                ClearAll();
                return;
            }

            List<MahjongHandView> renderedViews = new List<MahjongHandView>();
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId dataSeat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(dataSeat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, dataSeat);
                MahjongHandView handView = GetHandView(viewSlot);
                if (handView == null)
                    continue;

                bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
                handView.Render(
                    state.GetPlayerSeat(dataSeat).Hand.GetTiles(),
                    dataSeat,
                    viewSlot,
                    isSelf,
                    isSelf && canUseSelfInput);
                renderedViews.Add(handView);
            }

            ClearUnrenderedViews(renderedViews);
        }

        public void SetSelfInteractable(MahjongGameState state, bool interactable)
        {
            if (state == null)
                return;

            MahjongHandView selfView = GetHandView(SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat));
            if (selfView != null)
                selfView.SetTilesInteractable(interactable);
        }

        private MahjongHandView GetHandView(ViewSlot viewSlot)
        {
            switch (viewSlot)
            {
                case ViewSlot.SelfBottom:
                    return selfBottomHandView;
                case ViewSlot.NextLeft:
                    return nextLeftHandView;
                case ViewSlot.AcrossTop:
                    return acrossTopHandView;
                case ViewSlot.PreviousRight:
                    return previousRightHandView;
                default:
                    return null;
            }
        }

        private void ClearAll()
        {
            ClearView(selfBottomHandView);
            ClearView(nextLeftHandView);
            ClearView(acrossTopHandView);
            ClearView(previousRightHandView);
        }

        private void ClearUnrenderedViews(List<MahjongHandView> renderedViews)
        {
            ClearViewIfUnrendered(selfBottomHandView, renderedViews);
            ClearViewIfUnrendered(nextLeftHandView, renderedViews);
            ClearViewIfUnrendered(acrossTopHandView, renderedViews);
            ClearViewIfUnrendered(previousRightHandView, renderedViews);
        }

        private static void ClearViewIfUnrendered(MahjongHandView handView, List<MahjongHandView> renderedViews)
        {
            if (handView != null && !renderedViews.Contains(handView))
                handView.Clear();
        }

        private static void ClearView(MahjongHandView handView)
        {
            if (handView != null)
                handView.Clear();
        }

    }
}
