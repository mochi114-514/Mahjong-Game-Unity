using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using MahjongPrototype.UI;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: sibling 3D presenter for player tile areas.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Player Area Presenter")]
    public sealed class Mahjong3DPlayerAreaPresenter : MonoBehaviour
    {
        [Header("Player 3D UI Controllers")]
        [SerializeField] private Mahjong3DPlayerUiController selfBottomPlayerUiController;
        [SerializeField] private Mahjong3DPlayerUiController nextLeftPlayerUiController;
        [SerializeField] private Mahjong3DPlayerUiController acrossTopPlayerUiController;
        [SerializeField] private Mahjong3DPlayerUiController previousRightPlayerUiController;

        private readonly HashSet<Mahjong3DPlayerUiController> handEventSubscribedControllers =
            new HashSet<Mahjong3DPlayerUiController>();
        private Mahjong3DPlayerUiController drawnTileSubscribedController;

        public event Action<SeatId, int> HandTileClicked;
        public event Action DrawnTileClicked;

        private void Reset()
        {
            CachePlayerUiControllerReferences();
        }

        private void Awake()
        {
            CachePlayerUiControllerReferences();
        }

        private void OnEnable()
        {
            SubscribePlayerEvents();
        }

        private void OnDisable()
        {
            UnsubscribePlayerEvents();
        }

        private void OnDestroy()
        {
            UnsubscribePlayerEvents();
        }

        public void Refresh(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            SubscribePlayerEvents();
            RefreshHand(state, canUseSelfInput);
            RefreshDrawnTile(state, canUseSelfInput);
            RefreshDiscardRiver(state);
            RefreshOpenMelds(state);
        }

        public void RefreshHand(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            CachePlayerUiControllerReferences();

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId dataSeat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(dataSeat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, dataSeat);
                Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
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
            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            if (seatSlot.IsEmpty)
            {
                controller.ClearHand();
                return;
            }

            bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
            controller.RenderHand(
                state.GetPlayerSeat(seat).Hand.GetTiles(),
                seat,
                isSelf,
                isSelf && canUseSelfInput);
        }

        public void ClearHands()
        {
            ClearHand(ViewSlot.SelfBottom);
            ClearHand(ViewSlot.NextLeft);
            ClearHand(ViewSlot.AcrossTop);
            ClearHand(ViewSlot.PreviousRight);
        }

        public void RefreshDrawnTile(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            CachePlayerUiControllerReferences();

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
            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            if (seatSlot.IsEmpty)
            {
                controller.ClearDrawnTile();
                return;
            }

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

        public void ClearDrawnTiles()
        {
            ClearDrawnTile(ViewSlot.SelfBottom);
            ClearDrawnTile(ViewSlot.NextLeft);
            ClearDrawnTile(ViewSlot.AcrossTop);
            ClearDrawnTile(ViewSlot.PreviousRight);
        }

        public void RefreshDiscardRiver(MahjongGameState state)
        {
            if (state == null)
                return;

            CachePlayerUiControllerReferences();

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId seat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(seat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
                Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                controller.RenderDiscardRiver(state.Discards, state.DiscardClaims, seat);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedDiscardRivers(renderedViewSlots);
        }

        public void RefreshDiscardRiverForSeat(MahjongGameState state, SeatId seat)
        {
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            if (seatSlot.IsEmpty)
            {
                controller.ClearDiscardRiver();
                return;
            }

            controller.RenderDiscardRiver(state.Discards, state.DiscardClaims, seat);
        }

        public void ClearDiscardRivers()
        {
            ClearDiscardRiver(ViewSlot.SelfBottom);
            ClearDiscardRiver(ViewSlot.NextLeft);
            ClearDiscardRiver(ViewSlot.AcrossTop);
            ClearDiscardRiver(ViewSlot.PreviousRight);
        }

        public void RefreshOpenMelds(MahjongGameState state)
        {
            if (state == null)
                return;

            CachePlayerUiControllerReferences();

            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId seat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(seat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
                Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                controller.RenderOpenMelds(state.GetPlayerSeat(seat).OpenMelds);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedOpenMelds(renderedViewSlots);
        }

        public void RefreshOpenMeldsForSeat(MahjongGameState state, SeatId seat)
        {
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            if (seatSlot.IsEmpty)
            {
                controller.ClearOpenMelds();
                return;
            }

            controller.RenderOpenMelds(state.GetPlayerSeat(seat).OpenMelds);
        }

        public void ClearOpenMelds()
        {
            ClearOpenMelds(ViewSlot.SelfBottom);
            ClearOpenMelds(ViewSlot.NextLeft);
            ClearOpenMelds(ViewSlot.AcrossTop);
            ClearOpenMelds(ViewSlot.PreviousRight);
        }

        public void SetSelfInteractable(MahjongGameState state, bool interactable)
        {
            if (state == null)
                return;

            ViewSlot selfViewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(selfViewSlot);
            if (controller == null)
                return;

            controller.SetHandInteractable(interactable);
            controller.SetDrawnTileInteractable(interactable);
        }

        public void SetSelfHandTileInteractableByIndices(
            MahjongGameState state,
            IReadOnlyCollection<int> handIndices)
        {
            if (state == null)
                return;

            ViewSlot selfViewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(selfViewSlot);
            if (controller != null)
                controller.SetHandTileInteractableByIndices(handIndices);
        }

        public void SetSelfDrawnTileInteractable(MahjongGameState state, bool interactable)
        {
            if (state == null)
                return;

            ViewSlot selfViewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(selfViewSlot);
            if (controller != null)
                controller.SetDrawnTileInteractable(interactable);
        }

        public void SetSelfReachCandidateInteractable(
            MahjongGameState state,
            IReadOnlyCollection<int> handIndices,
            bool drawnTileSelectable)
        {
            if (state == null)
                return;

            ViewSlot selfViewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(selfViewSlot);
            if (controller == null)
                return;

            controller.SetReachCandidateHandTileInteractableByIndices(handIndices);
            controller.SetReachCandidateDrawnTileInteractable(drawnTileSelectable);
        }

        public void ClearSelfTileDimmed(MahjongGameState state)
        {
            if (state == null)
                return;

            ViewSlot selfViewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat);
            Mahjong3DPlayerUiController controller = GetPlayerUiController(selfViewSlot);
            if (controller != null)
                controller.ClearDimmedTiles();
        }

        public Mahjong3DPlayerUiController GetPlayerUiController(ViewSlot viewSlot)
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

        public void RefreshSelfBottomHand(MahjongGameState state, bool canUseSelfInput)
        {
            if (state == null)
                return;

            RefreshHandForSeat(state, state.SelfSeat, canUseSelfInput);
        }

        public void ClearSelfBottomHand()
        {
            ClearHand(ViewSlot.SelfBottom);
        }

        private Mahjong3DPlayerUiController FindPlayerUiController(ViewSlot targetViewSlot)
        {
            Mahjong3DPlayerUiController[] controllers = GetComponentsInChildren<Mahjong3DPlayerUiController>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                Mahjong3DPlayerUiController controller = controllers[i];
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

        private void SubscribePlayerEvents()
        {
            CachePlayerUiControllerReferences();
            SubscribePlayerHandEvents(selfBottomPlayerUiController);
            SubscribePlayerHandEvents(nextLeftPlayerUiController);
            SubscribePlayerHandEvents(acrossTopPlayerUiController);
            SubscribePlayerHandEvents(previousRightPlayerUiController);
            SubscribeDrawnTileEvents(selfBottomPlayerUiController);
        }

        private void SubscribePlayerHandEvents(Mahjong3DPlayerUiController controller)
        {
            if (controller == null || handEventSubscribedControllers.Contains(controller))
                return;

            controller.HandTileClicked += HandleHandTileClicked;
            handEventSubscribedControllers.Add(controller);
        }

        private void SubscribeDrawnTileEvents(Mahjong3DPlayerUiController controller)
        {
            if (controller == null || drawnTileSubscribedController == controller)
                return;

            UnsubscribeDrawnTileEvents();
            controller.DrawnTileClicked += HandleDrawnTileClicked;
            drawnTileSubscribedController = controller;
        }

        private void UnsubscribePlayerEvents()
        {
            UnsubscribePlayerHandEvents();
            UnsubscribeDrawnTileEvents();
        }

        private void UnsubscribePlayerHandEvents()
        {
            foreach (Mahjong3DPlayerUiController controller in handEventSubscribedControllers)
            {
                if (controller != null)
                    controller.HandTileClicked -= HandleHandTileClicked;
            }

            handEventSubscribedControllers.Clear();
        }

        private void UnsubscribeDrawnTileEvents()
        {
            if (drawnTileSubscribedController == null)
                return;

            drawnTileSubscribedController.DrawnTileClicked -= HandleDrawnTileClicked;
            drawnTileSubscribedController = null;
        }

        private void HandleHandTileClicked(SeatId dataSeat, int handIndex)
        {
            HandTileClicked?.Invoke(dataSeat, handIndex);
        }

        private void HandleDrawnTileClicked()
        {
            DrawnTileClicked?.Invoke();
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

            ClearHand(viewSlot);
        }

        private void ClearHand(ViewSlot viewSlot)
        {
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearHand();
        }

        private void ClearUnrenderedDrawnTiles(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearDrawnTileIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearDrawnTileIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearDrawnTileIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearDrawnTileIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearDrawnTileIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            ClearDrawnTile(viewSlot);
        }

        private void ClearDrawnTile(ViewSlot viewSlot)
        {
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearDrawnTile();
        }

        private void ClearUnrenderedDiscardRivers(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearDiscardRiverIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearDiscardRiverIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearDiscardRiverIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearDiscardRiverIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearDiscardRiverIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            ClearDiscardRiver(viewSlot);
        }

        private void ClearDiscardRiver(ViewSlot viewSlot)
        {
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearDiscardRiver();
        }

        private void ClearUnrenderedOpenMelds(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearOpenMeldsIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearOpenMeldsIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearOpenMeldsIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearOpenMeldsIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearOpenMeldsIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (!renderedViewSlots.Contains(viewSlot))
                ClearOpenMelds(viewSlot);
        }

        private void ClearOpenMelds(ViewSlot viewSlot)
        {
            Mahjong3DPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearOpenMelds();
        }
    }
}
