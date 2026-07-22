using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using MahjongPrototype.UI;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    public readonly struct Mahjong3DTileHoverInfo : IEquatable<Mahjong3DTileHoverInfo>
    {
        public Mahjong3DTileHoverInfo(
            SeatId seatId,
            DiscardSource source,
            int handIndex,
            Tile tile)
        {
            SeatId = seatId;
            Source = source;
            HandIndex = handIndex;
            Tile = tile;
        }

        public SeatId SeatId { get; }
        public DiscardSource Source { get; }
        public int HandIndex { get; }
        public Tile Tile { get; }

        public bool Equals(Mahjong3DTileHoverInfo other)
        {
            return SeatId == other.SeatId &&
                Source == other.Source &&
                HandIndex == other.HandIndex &&
                Tile == other.Tile;
        }

        public override bool Equals(object obj)
        {
            return obj is Mahjong3DTileHoverInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)SeatId;
                hash = (hash * 397) ^ (int)Source;
                hash = (hash * 397) ^ HandIndex;
                hash = (hash * 397) ^ Tile.GetHashCode();
                return hash;
            }
        }
    }

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
        private Mahjong3DTileHoverInfo? activeTileHover;

        public event Action<SeatId, int, Tile> HandTileClicked;
        public event Action<SeatId, Tile> DrawnTileClicked;
        public event Action<Mahjong3DTileHoverInfo> TileHoverEntered;
        public event Action<Mahjong3DTileHoverInfo> TileHoverExited;

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
            ClearActiveTileHover();
            UnsubscribeViewEvents();
        }

        private void OnDestroy()
        {
            ClearActiveTileHover();
            UnsubscribeViewEvents();
        }

        public void RenderHand(
            IReadOnlyList<Tile> handTiles,
            SeatId dataSeat,
            bool faceUp,
            bool interactable)
        {
            ClearActiveTileHover();
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

        public void SetSelectedHandTile(int handIndex)
        {
            if (handView == null)
            {
                WarnMissingOnce(ref warnedMissingHandView, "3D hand view is not assigned.");
                return;
            }

            handView.SetSelectedTileByIndex(handIndex);
            if (drawnTileView != null)
                drawnTileView.SetSelected(false);
        }

        public void SetDrawnTileSelected(bool selected)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "3D drawn tile view is not assigned.");
                return;
            }

            drawnTileView.SetSelected(selected);
            if (selected && handView != null)
                handView.ClearSelectedTile();
        }

        public void ClearTileSelectionVisual()
        {
            if (handView != null)
                handView.ClearSelectedTile();
            if (drawnTileView != null)
                drawnTileView.SetSelected(false);
        }

        public void RenderDiscardRiver(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            RenderDiscardRiver(discards, null, dataSeat, false, 0, null);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat)
        {
            RenderDiscardRiver(discards, discardClaims, dataSeat, false, 0, null);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat,
            bool isReachDeclared,
            int reachDeclaredTurnIndex)
        {
            RenderDiscardRiver(
                discards,
                discardClaims,
                dataSeat,
                isReachDeclared,
                reachDeclaredTurnIndex,
                null);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat,
            bool isReachDeclared,
            int reachDeclaredTurnIndex,
            int? reactionHighlightDiscardId)
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
                reachDeclaredTurnIndex,
                reactionHighlightDiscardId);
        }

        public void ClearDiscardRiver()
        {
            if (discardRiverView == null)
                return;

            discardRiverView.Clear();
        }

        public void ClearDiscardReactionHighlights()
        {
            if (discardRiverView != null)
                discardRiverView.ClearReactionHighlights();
        }

        public void RenderOpenMelds(IReadOnlyList<PlayerMeld> melds)
        {
            if (openMeldView == null)
            {
                WarnMissingOnce(ref warnedMissingOpenMeldView, "3D open meld view is not assigned.");
                return;
            }

            openMeldView.RenderOpenMelds(melds);
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
            handView.TileHoverEntered += HandleHandTileHoverEntered;
            handView.TileHoverExited += HandleHandTileHoverExited;
            isHandViewSubscribed = true;
        }

        private void SubscribeDrawnTileViewEvents()
        {
            if (drawnTileView == null || isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked += HandleDrawnTileClicked;
            drawnTileView.DrawnTileHoverEntered += HandleDrawnTileHoverEntered;
            drawnTileView.DrawnTileHoverExited += HandleDrawnTileHoverExited;
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
            handView.TileHoverEntered -= HandleHandTileHoverEntered;
            handView.TileHoverExited -= HandleHandTileHoverExited;
            isHandViewSubscribed = false;
        }

        private void UnsubscribeDrawnTileViewEvents()
        {
            if (drawnTileView == null || !isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked -= HandleDrawnTileClicked;
            drawnTileView.DrawnTileHoverEntered -= HandleDrawnTileHoverEntered;
            drawnTileView.DrawnTileHoverExited -= HandleDrawnTileHoverExited;
            isDrawnTileViewSubscribed = false;
        }

        private void HandleHandTileClicked(int handIndex, Tile tile)
        {
            HandTileClicked?.Invoke(handDataSeat, handIndex, tile);
        }

        private void HandleDrawnTileClicked(Tile tile)
        {
            DrawnTileClicked?.Invoke(handDataSeat, tile);
        }

        private void HandleHandTileHoverEntered(int handIndex, Tile tile)
        {
            SetActiveTileHover(new Mahjong3DTileHoverInfo(
                handDataSeat,
                DiscardSource.Hand,
                handIndex,
                tile));
        }

        private void HandleHandTileHoverExited(int handIndex, Tile tile)
        {
            ClearActiveTileHover(new Mahjong3DTileHoverInfo(
                handDataSeat,
                DiscardSource.Hand,
                handIndex,
                tile));
        }

        private void HandleDrawnTileHoverEntered(Tile tile)
        {
            SetActiveTileHover(new Mahjong3DTileHoverInfo(
                handDataSeat,
                DiscardSource.DrawnTile,
                -1,
                tile));
        }

        private void HandleDrawnTileHoverExited(Tile tile)
        {
            ClearActiveTileHover(new Mahjong3DTileHoverInfo(
                handDataSeat,
                DiscardSource.DrawnTile,
                -1,
                tile));
        }

        private void SetActiveTileHover(Mahjong3DTileHoverInfo hoverInfo)
        {
            if (activeTileHover.HasValue && activeTileHover.Value.Equals(hoverInfo))
                return;

            ClearActiveTileHover();
            activeTileHover = hoverInfo;
            TileHoverEntered?.Invoke(hoverInfo);
        }

        private void ClearActiveTileHover(Mahjong3DTileHoverInfo hoverInfo)
        {
            if (!activeTileHover.HasValue || !activeTileHover.Value.Equals(hoverInfo))
                return;

            ClearActiveTileHover();
        }

        private void ClearActiveTileHover()
        {
            if (!activeTileHover.HasValue)
                return;

            Mahjong3DTileHoverInfo previous = activeTileHover.Value;
            activeTileHover = null;
            TileHoverExited?.Invoke(previous);
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
