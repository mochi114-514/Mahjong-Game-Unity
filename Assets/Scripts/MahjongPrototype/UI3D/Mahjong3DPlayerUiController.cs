using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using MahjongPrototype.UI;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: 3D companion for one player's tile area.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Player UI Controller")]
    public sealed class Mahjong3DPlayerUiController : MonoBehaviour
    {
        [Header("View Slot")]
        [SerializeField] private ViewSlot viewSlot = ViewSlot.SelfBottom;

        [Header("Views")]
        [SerializeField] private Mahjong3DHandView handView;
        [SerializeField] private Mahjong3DDrawnTileView drawnTileView;
        [SerializeField] private Mahjong3DDiscardRiverView discardRiverView;
        [SerializeField] private Mahjong3DOpenMeldView openMeldView;

        private SeatId handDataSeat = SeatId.East;
        private bool warnedMissingHandView;
        private bool warnedMissingDrawnTileView;
        private bool warnedMissingDiscardRiverView;
        private bool warnedMissingOpenMeldView;
        private bool isHandViewSubscribed;
        private bool isDrawnTileViewSubscribed;

        public event Action<SeatId, int> HandTileClicked;
        public event Action DrawnTileClicked;

        public ViewSlot ViewSlot => viewSlot;
        public Mahjong3DHandView HandView => handView;
        public Mahjong3DDrawnTileView DrawnTileView => drawnTileView;
        public Mahjong3DDiscardRiverView DiscardRiverView => discardRiverView;
        public Mahjong3DOpenMeldView OpenMeldView => openMeldView;
        public SeatId HandDataSeat => handDataSeat;

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
                WarnMissingOnce(ref warnedMissingHandView, "3D hand view is not assigned.");
                return;
            }

            handView.RenderHand(handTiles, faceUp, interactable);
            RepositionDrawnTileAtHandEnd();
        }

        public void ClearHand()
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "3D hand view is not assigned.");
                return;
            }

            handView.Clear();
        }

        public void SetHandInteractable(bool interactable)
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "3D hand view is not assigned.");
                return;
            }

            handView.SetTilesInteractable(interactable);
        }

        public void SetHandTileInteractableByIndices(IReadOnlyCollection<int> handIndices)
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "3D hand view is not assigned.");
                return;
            }

            handView.SetTileInteractableByIndices(handIndices);
        }

        public void SetReachCandidateHandTileInteractableByIndices(IReadOnlyCollection<int> handIndices)
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "3D hand view is not assigned.");
                return;
            }

            handView.SetReachCandidateInteractableByIndices(handIndices);
        }

        public void RenderDrawnTile(Tile? drawnTile, bool faceUp, bool interactable)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "3D drawn tile view is not assigned.");
                return;
            }

            if (handView == null)
            {
                drawnTileView.Render(drawnTile, faceUp, interactable);
                return;
            }

            drawnTileView.RenderAtWorldPosition(
                drawnTile,
                faceUp,
                interactable,
                handView.GetTrailingTileWorldPosition(drawnTileView.HandGap));
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
                WarnMissingOnce(ref warnedMissingDrawnTileView, "3D drawn tile view is not assigned.");
                return;
            }

            drawnTileView.SetTileInteractable(interactable);
        }

        public void SetReachCandidateDrawnTileInteractable(bool selectable)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "3D drawn tile view is not assigned.");
                return;
            }

            drawnTileView.SetReachCandidateInteractable(selectable);
        }

        public void ClearDimmedTiles()
        {
            if (handView != null)
                handView.ClearDimmed();

            if (drawnTileView != null)
                drawnTileView.SetDimmed(false);
        }

        public void RenderDiscardRiver(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            RenderDiscardRiver(discards, null, dataSeat, false, 0);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat)
        {
            RenderDiscardRiver(discards, discardClaims, dataSeat, false, 0);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat,
            bool isReachDeclared,
            int reachDeclaredTurnIndex)
        {
            if (discardRiverView == null)
            {
                WarnMissingOnce(ref warnedMissingDiscardRiverView, "3D discard river view is not assigned.");
                return;
            }

            discardRiverView.RenderDiscardRiver(
                discards,
                discardClaims,
                dataSeat,
                isReachDeclared,
                reachDeclaredTurnIndex);
        }

        public void ClearDiscardRiver()
        {
            if (discardRiverView == null)
                return;

            discardRiverView.Clear();
        }

        public void RenderOpenMelds(IReadOnlyList<OpenMeld> openMelds)
        {
            if (openMeldView == null)
            {
                WarnMissingOnce(ref warnedMissingOpenMeldView, "3D open meld view is not assigned.");
                return;
            }

            openMeldView.RenderOpenMelds(openMelds);
        }

        public void ClearOpenMelds()
        {
            if (openMeldView != null)
                openMeldView.Clear();
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

        private void RepositionDrawnTileAtHandEnd()
        {
            if (handView == null || drawnTileView == null)
                return;

            drawnTileView.SetWorldPosition(
                handView.GetTrailingTileWorldPosition(drawnTileView.HandGap));
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(Mahjong3DPlayerUiController)} ({viewSlot}): {message}", this);
        }
    }
}
