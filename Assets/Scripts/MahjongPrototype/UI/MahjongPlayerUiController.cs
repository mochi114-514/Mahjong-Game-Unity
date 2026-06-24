using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Player UI Controller")]
    public sealed class MahjongPlayerUiController : MonoBehaviour
    {
        [Header("View Slot")]
        [SerializeField] private ViewSlot viewSlot = ViewSlot.SelfBottom;

        [Header("Views")]
        [SerializeField] private MahjongHandView handView;
        [SerializeField] private MahjongDiscardRiverView discardRiverView;
        [SerializeField] private MahjongDrawnTileView drawnTileView;
        [SerializeField] private MahjongSeatWindView seatWindView;

        private SeatId handDataSeat = SeatId.East;
        private bool warnedMissingHandView;
        private bool warnedMissingDiscardRiverView;
        private bool warnedMissingDrawnTileView;
        private bool warnedMissingSeatWindView;
        private bool isHandViewSubscribed;
        private bool isDrawnTileViewSubscribed;

        public event Action<SeatId, int> HandTileClicked;
        public event Action DrawnTileClicked;

        public ViewSlot ViewSlot => viewSlot;
        public MahjongHandView HandView => handView;
        public MahjongDiscardRiverView DiscardRiverView => discardRiverView;
        public MahjongDrawnTileView DrawnTileView => drawnTileView;
        public MahjongSeatWindView SeatWindView => seatWindView;

        private void OnEnable()
        {
            SubscribeViewEvents();
        }

        private void OnDisable()
        {
            UnsubscribeViewEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeViewEvents();
        }

        public void RenderHand(
            IReadOnlyList<Tile> handTiles,
            SeatId dataSeat,
            bool faceUp,
            bool interactable)
        {
            handDataSeat = dataSeat;
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "Hand view is not assigned.");
                return;
            }

            handView.Render(handTiles, dataSeat, viewSlot, faceUp, interactable);
        }

        public void ClearHand()
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "Hand view is not assigned.");
                return;
            }

            handView.Clear();
        }

        public void SetHandInteractable(bool interactable)
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "Hand view is not assigned.");
                return;
            }

            handView.SetTilesInteractable(interactable);
        }

        public void RenderDiscardRiver(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            if (discardRiverView == null)
            {
                WarnMissingOnce(ref warnedMissingDiscardRiverView, "Discard river view is not assigned.");
                return;
            }

            discardRiverView.Rebuild(discards, dataSeat, viewSlot);
        }

        public void ClearDiscardRiver()
        {
            if (discardRiverView == null)
            {
                WarnMissingOnce(ref warnedMissingDiscardRiverView, "Discard river view is not assigned.");
                return;
            }

            discardRiverView.Clear();
        }

        public void RenderDrawnTile(Tile? drawnTile)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "Drawn tile view is not assigned.");
                return;
            }

            drawnTileView.Rebuild(drawnTile);
        }

        public void RenderDrawnTile(Tile? drawnTile, bool faceUp, bool interactable)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "Drawn tile view is not assigned.");
                return;
            }

            drawnTileView.Render(drawnTile, faceUp, interactable);
        }

        public void ClearDrawnTile()
        {
            if (drawnTileView == null)
                return;

            drawnTileView.Clear();
        }

        public void SetDrawnTileInteractable(bool interactable)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "Drawn tile view is not assigned.");
                return;
            }

            drawnTileView.SetTileInteractable(interactable);
        }

        public void RenderWind(SeatId seatId)
        {
            if (seatWindView == null)
            {
                WarnMissingOnce(ref warnedMissingSeatWindView, "Seat wind view is not assigned.");
                return;
            }

            seatWindView.Render(seatId);
        }

        public void ClearWind()
        {
            if (seatWindView == null)
            {
                WarnMissingOnce(ref warnedMissingSeatWindView, "Seat wind view is not assigned.");
                return;
            }

            seatWindView.Clear();
        }

        private void SubscribeViewEvents()
        {
            SubscribeHandViewEvents();
            SubscribeDrawnTileViewEvents();
        }

        private void SubscribeHandViewEvents()
        {
            if (handView == null || isHandViewSubscribed)
                return;

            handView.TileClicked += HandleHandTileClicked;
            isHandViewSubscribed = true;
        }

        private void SubscribeDrawnTileViewEvents()
        {
            if (drawnTileView == null || isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked += HandleDrawnTileClicked;
            isDrawnTileViewSubscribed = true;
        }

        private void UnsubscribeViewEvents()
        {
            UnsubscribeHandViewEvents();
            UnsubscribeDrawnTileViewEvents();
        }

        private void UnsubscribeHandViewEvents()
        {
            if (handView == null || !isHandViewSubscribed)
                return;

            handView.TileClicked -= HandleHandTileClicked;
            isHandViewSubscribed = false;
        }

        private void UnsubscribeDrawnTileViewEvents()
        {
            if (drawnTileView == null || !isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked -= HandleDrawnTileClicked;
            isDrawnTileViewSubscribed = false;
        }

        private void HandleHandTileClicked(int handIndex)
        {
            HandTileClicked?.Invoke(handDataSeat, handIndex);
        }

        private void HandleDrawnTileClicked()
        {
            DrawnTileClicked?.Invoke();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongPlayerUiController)}: {message}", this);
        }
    }
}
