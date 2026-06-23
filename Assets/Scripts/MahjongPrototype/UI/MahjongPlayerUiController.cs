using System;
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
        private bool warnedMissingDrawnTileView;
        private bool isDrawnTileViewSubscribed;

        public event Action DrawnTileClicked;

        public ViewSlot ViewSlot => viewSlot;
        public MahjongHandView HandView => handView;
        public MahjongDiscardRiverView DiscardRiverView => discardRiverView;
        public MahjongDrawnTileView DrawnTileView => drawnTileView;

        private void OnEnable()
        {
            CacheMissingViewReferences();
            SubscribeViewEvents();
        }

        private void OnDisable()
        {
            UnsubscribeViewEvents();
        }

        public void ConfigureMissingViews(
            MahjongHandView fallbackHandView,
            MahjongDiscardRiverView fallbackDiscardRiverView,
            MahjongDrawnTileView fallbackDrawnTileView)
        {
            if (handView == null)
                handView = fallbackHandView;

            if (discardRiverView == null)
                discardRiverView = fallbackDiscardRiverView;

            if (drawnTileView == null)
                drawnTileView = fallbackDrawnTileView;

            CacheMissingViewReferences();
            SubscribeViewEvents();
        }

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

        public void RenderDrawnTile(MahjongGameState state, SeatId dataSeat)
        {
            if (state == null)
            {
                WarnMissingOnce(ref warnedMissingGameState, "Cannot render drawn tile because game state is not assigned.");
                return;
            }

            if (drawnTileView == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileView, "Drawn tile view is not assigned.");
                return;
            }

            drawnTileView.Rebuild(state.GetPlayerSeat(dataSeat).DrawnTile);
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

        private void CacheMissingViewReferences()
        {
            if (handView == null)
                handView = GetComponentInChildren<MahjongHandView>(true);

            if (discardRiverView == null)
                discardRiverView = GetComponentInChildren<MahjongDiscardRiverView>(true);

            if (drawnTileView == null)
                drawnTileView = GetComponentInChildren<MahjongDrawnTileView>(true);
        }

        private void SubscribeViewEvents()
        {
            if (drawnTileView == null || isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked += HandleDrawnTileClicked;
            isDrawnTileViewSubscribed = true;
        }

        private void UnsubscribeViewEvents()
        {
            if (drawnTileView == null || !isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked -= HandleDrawnTileClicked;
            isDrawnTileViewSubscribed = false;
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
