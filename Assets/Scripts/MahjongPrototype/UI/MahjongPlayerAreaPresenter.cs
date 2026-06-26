using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Player Area Presenter")]
    public sealed class MahjongPlayerAreaPresenter : MonoBehaviour
    {
        [Header("Player UI Controllers")]
        [SerializeField] private MahjongPlayerUiController selfBottomPlayerUiController;
        [SerializeField] private MahjongPlayerUiController nextLeftPlayerUiController;
        [SerializeField] private MahjongPlayerUiController acrossTopPlayerUiController;
        [SerializeField] private MahjongPlayerUiController previousRightPlayerUiController;

        private void Reset()
        {
            CachePlayerUiControllerReferences();
        }

        private void Awake()
        {
            CachePlayerUiControllerReferences();
        }

        public void Refresh(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            RefreshPlayerWinds(state);
            RefreshHand(state, canUseSelfInput);
            RefreshDrawnTile(state, canUseSelfInput);
            RefreshDiscardRiver(state);
        }

        public void RefreshHand(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId dataSeat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(dataSeat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, dataSeat);
                MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
                controller.RenderHand(
                    state.GetPlayerSeat(dataSeat).Hand.GetTiles(),
                    dataSeat,
                    isSelf,
                    isSelf && canUseSelfInput);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedPlayerHands(renderedViewSlots);
        }

        public void RefreshHandForSeat(MahjongGameState state, SeatId seat, bool canUseSelfInput)
        {
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            if (seatSlot.IsEmpty)
                return;

            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
            controller.RenderHand(
                state.GetPlayerSeat(seat).Hand.GetTiles(),
                seat,
                isSelf,
                isSelf && canUseSelfInput);
        }

        public void RefreshPlayerWinds(MahjongGameState state)
        {
            if (state == null)
                return;

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId dataSeat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(dataSeat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, dataSeat);
                MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                controller.RenderWind(dataSeat);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedPlayerWinds(renderedViewSlots);
        }

        public void RefreshDrawnTile(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId seat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(seat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
                RefreshDrawnTileForSeat(state, seat, canUseSelfInput);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedDrawnTiles(renderedViewSlots);
        }

        public void RefreshDrawnTileForSeat(MahjongGameState state, SeatId seat, bool canUseSelfInput)
        {
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            if (seatSlot.IsEmpty)
                return;

            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
            Tile? drawnTile = state.GetPlayerSeat(seat).DrawnTile;
            if (drawnTile.HasValue)
            {
                controller.RenderDrawnTile(
                    drawnTile,
                    isSelf,
                    isSelf && canUseSelfInput);
            }
            else
            {
                controller.ClearDrawnTile();
            }
        }

        public void RefreshDiscardRiver(MahjongGameState state)
        {
            if (state == null)
                return;

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId seat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(seat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
                MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                controller.RenderDiscardRiver(state.Discards, seat);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedDiscardRivers(renderedViewSlots);
        }

        public void RefreshDiscardRiverForSeat(MahjongGameState state, SeatId seat)
        {
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            if (seatSlot.IsEmpty)
                return;

            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.RenderDiscardRiver(state.Discards, seat);
        }

        public void SetSelfInteractable(MahjongGameState state, bool interactable)
        {
            if (state == null)
                return;

            MahjongPlayerUiController selfController = GetPlayerUiController(
                SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat));
            if (selfController != null)
                selfController.SetHandInteractable(interactable);

            if (selfController != null)
                selfController.SetDrawnTileInteractable(interactable);
        }

        public MahjongPlayerUiController GetPlayerUiController(ViewSlot viewSlot)
        {
            CachePlayerUiControllerReferences();
            switch (viewSlot)
            {
                case ViewSlot.SelfBottom:
                    return selfBottomPlayerUiController;
                case ViewSlot.NextLeft:
                    return nextLeftPlayerUiController;
                case ViewSlot.AcrossTop:
                    return acrossTopPlayerUiController;
                case ViewSlot.PreviousRight:
                    return previousRightPlayerUiController;
                default:
                    return null;
            }
        }

        private MahjongPlayerUiController FindPlayerUiController(ViewSlot targetViewSlot)
        {
            MahjongPlayerUiController[] controllers = GetComponentsInChildren<MahjongPlayerUiController>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                MahjongPlayerUiController controller = controllers[i];
                if (controller != null && controller.ViewSlot == targetViewSlot)
                    return controller;
            }

            return null;
        }

        private void CachePlayerUiControllerReferences()
        {
            if (selfBottomPlayerUiController == null)
                selfBottomPlayerUiController = FindPlayerUiController(ViewSlot.SelfBottom);

            if (nextLeftPlayerUiController == null)
                nextLeftPlayerUiController = FindPlayerUiController(ViewSlot.NextLeft);

            if (acrossTopPlayerUiController == null)
                acrossTopPlayerUiController = FindPlayerUiController(ViewSlot.AcrossTop);

            if (previousRightPlayerUiController == null)
                previousRightPlayerUiController = FindPlayerUiController(ViewSlot.PreviousRight);
        }

        private void ClearUnrenderedPlayerHands(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerHandIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerHandIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerHandIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerHandIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerHandIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearHand();
        }

        private void ClearUnrenderedPlayerWinds(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerWindIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerWindIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerWindIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerWindIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerWindIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearWind();
        }

        private void ClearUnrenderedDrawnTiles(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerDrawnTileIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearDrawnTile();
        }

        private void ClearUnrenderedDiscardRivers(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerDiscardRiverIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearDiscardRiver();
        }
    }
}
