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

        private SeatId handDataSeat = SeatId.East;
        private bool warnedMissingHandView;
        private bool warnedMissingDrawnTileView;
        private bool warnedMissingDiscardRiverView;

        public ViewSlot ViewSlot => viewSlot;
        public Mahjong3DHandView HandView => handView;
        public Mahjong3DDrawnTileView DrawnTileView => drawnTileView;
        public Mahjong3DDiscardRiverView DiscardRiverView => discardRiverView;
        public SeatId HandDataSeat => handDataSeat;

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

        public void RenderDrawnTile(Tile? drawnTile, bool faceUp, bool interactable)
        {
            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "3D drawn tile view is not assigned.");
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
                WarnMissingOnce(ref warnedMissingDrawnTileView, "3D drawn tile view is not assigned.");
                return;
            }

            drawnTileView.SetTileInteractable(interactable);
        }

        public void RenderDiscardRiver(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            if (discardRiverView == null)
            {
                WarnMissingOnce(ref warnedMissingDiscardRiverView, "3D discard river view is not assigned.");
                return;
            }

            discardRiverView.RenderDiscardRiver(discards, dataSeat);
        }

        public void ClearDiscardRiver()
        {
            if (discardRiverView == null)
                return;

            discardRiverView.Clear();
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
