using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong UI Command Router")]
    public sealed class MahjongUiCommandRouter : MonoBehaviour
    {
        [Header("Command Targets")]
        [Tooltip("Game flow controller that receives UI commands.")]
        [SerializeField] private MahjongGameFlow gameFlow;
        [Tooltip("Control area input event source.")]
        [SerializeField] private MahjongUiInputController inputController;
        [Tooltip("Player area tile-click event source.")]
        [SerializeField] private MahjongPlayerAreaPresenter playerAreaPresenter;

        private MahjongUiInputController subscribedInputController;
        private MahjongPlayerAreaPresenter subscribedPlayerAreaPresenter;
        private bool warnedMissingFlow;
        private bool warnedMissingInputController;
        private bool warnedMissingPlayerAreaPresenter;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void CacheReferences()
        {
            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (inputController == null)
                inputController = GetComponentInChildren<MahjongUiInputController>(true);

            if (playerAreaPresenter == null)
                playerAreaPresenter = GetComponentInChildren<MahjongPlayerAreaPresenter>(true);
        }

        public void RefreshSubscriptions()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CacheReferences();
            SubscribeInputControllerEvents();
            SubscribePlayerAreaPresenterEvents();
        }

        private void UnsubscribeEvents()
        {
            UnsubscribeInputControllerEvents();
            UnsubscribePlayerAreaPresenterEvents();
        }

        private void SubscribeInputControllerEvents()
        {
            if (inputController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingInputController,
                    "MahjongUiInputController is not assigned. UI control commands will not be routed.");
                return;
            }

            if (subscribedInputController == inputController)
                return;

            UnsubscribeInputControllerEvents();
            inputController.DrawRequested += HandleDrawRequested;
            inputController.ForceDrawSkillRequested += HandleForceDrawSkillRequested;
            inputController.AutoSortChanged += HandleAutoSortChanged;
            inputController.RetryRequested += HandleRetryRequested;
            inputController.WinRequested += HandleWinRequested;
            inputController.DeclineWinRequested += HandleDeclineWinRequested;
            subscribedInputController = inputController;
        }

        private void UnsubscribeInputControllerEvents()
        {
            if (subscribedInputController == null)
                return;

            subscribedInputController.DrawRequested -= HandleDrawRequested;
            subscribedInputController.ForceDrawSkillRequested -= HandleForceDrawSkillRequested;
            subscribedInputController.AutoSortChanged -= HandleAutoSortChanged;
            subscribedInputController.RetryRequested -= HandleRetryRequested;
            subscribedInputController.WinRequested -= HandleWinRequested;
            subscribedInputController.DeclineWinRequested -= HandleDeclineWinRequested;
            subscribedInputController = null;
        }

        private void SubscribePlayerAreaPresenterEvents()
        {
            if (playerAreaPresenter == null)
            {
                WarnMissingOnce(
                    ref warnedMissingPlayerAreaPresenter,
                    "MahjongPlayerAreaPresenter is not assigned. Player tile-click commands will not be routed.");
                return;
            }

            if (subscribedPlayerAreaPresenter == playerAreaPresenter)
                return;

            UnsubscribePlayerAreaPresenterEvents();
            playerAreaPresenter.HandTileClicked += HandleHandTileClicked;
            playerAreaPresenter.DrawnTileClicked += HandleDrawnTileClicked;
            subscribedPlayerAreaPresenter = playerAreaPresenter;
        }

        private void UnsubscribePlayerAreaPresenterEvents()
        {
            if (subscribedPlayerAreaPresenter == null)
                return;

            subscribedPlayerAreaPresenter.HandTileClicked -= HandleHandTileClicked;
            subscribedPlayerAreaPresenter.DrawnTileClicked -= HandleDrawnTileClicked;
            subscribedPlayerAreaPresenter = null;
        }

        private void HandleDrawRequested()
        {
            if (!TryGetGameFlow("Cannot draw because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestDraw();
        }

        private void HandleForceDrawSkillRequested(string targetTileText)
        {
            if (!TryGetGameFlow("Cannot activate skill because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestForceDrawSkill(targetTileText);
        }

        private void HandleAutoSortChanged(bool enabled)
        {
            if (!TryGetGameFlow("Cannot change auto sort because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestSetAutoSortEnabled(enabled);
        }

        private void HandleRetryRequested()
        {
            if (!TryGetGameFlow("Cannot retry because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RetryPrototype();
        }

        private void HandleWinRequested()
        {
            if (!TryGetGameFlow("Cannot declare win because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestDeclareWin();
        }

        private void HandleDeclineWinRequested()
        {
            if (!TryGetGameFlow("Cannot decline win because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestDeclineWin();
        }

        private void HandleHandTileClicked(SeatId dataSeat, int handIndex)
        {
            if (!TryGetGameFlow("Cannot discard because MahjongGameFlow is not assigned."))
                return;

            MahjongGameState state = gameFlow.CurrentState;
            if (state == null || dataSeat != state.SelfSeat)
                return;

            gameFlow.RequestDiscard(handIndex);
        }

        private void HandleDrawnTileClicked()
        {
            if (!TryGetGameFlow("Cannot discard drawn tile because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestDiscardDrawnTile();
        }

        private bool TryGetGameFlow(string warning)
        {
            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (gameFlow != null)
                return true;

            WarnMissingOnce(ref warnedMissingFlow, warning);
            return false;
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongUiCommandRouter)}: {message}", this);
        }
    }
}
