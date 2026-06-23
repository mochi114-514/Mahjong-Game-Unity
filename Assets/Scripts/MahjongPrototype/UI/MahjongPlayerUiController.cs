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

        private bool warnedMissingGameState;
        private bool warnedMissingDiscardRiverView;

        public ViewSlot ViewSlot => viewSlot;
        public MahjongHandView HandView => handView;
        public MahjongDiscardRiverView DiscardRiverView => discardRiverView;
        public MahjongDrawnTileView DrawnTileView => drawnTileView;

        public void RenderDiscardRiver(MahjongGameState state, SeatId dataSeat)
        {
            if (state == null)
            {
                WarnMissingOnce(ref warnedMissingGameState, "Cannot render discard river because game state is not assigned.");
                return;
            }

            if (discardRiverView == null)
            {
                WarnMissingOnce(ref warnedMissingDiscardRiverView, "Discard river view is not assigned.");
                return;
            }

            discardRiverView.Rebuild(state.Discards, dataSeat, viewSlot);
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
