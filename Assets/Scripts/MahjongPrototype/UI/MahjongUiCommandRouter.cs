using MahjongPrototype.Domain;
using MahjongPrototype.UI3D;
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
        [Tooltip("3D player area tile-click event source.")]
        [SerializeField] private Mahjong3DPlayerAreaPresenter playerArea3DPresenter;

        private MahjongUiInputController subscribedInputController;
        private Mahjong3DPlayerAreaPresenter subscribedPlayerArea3DPresenter;
        private bool warnedMissingFlow;
        private bool warnedMissingInputController;

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

            if (playerArea3DPresenter == null)
                playerArea3DPresenter = GetComponentInChildren<Mahjong3DPlayerAreaPresenter>(true);
        }

        public void RefreshSubscriptions()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CacheReferences();
            SubscribeInputControllerEvents();
            SubscribePlayerArea3DPresenterEvents();
        }

        private void UnsubscribeEvents()
        {
            UnsubscribeInputControllerEvents();
            UnsubscribePlayerArea3DPresenterEvents();
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
            inputController.ReachRequested += HandleReachRequested;
            inputController.DeclineReachRequested += HandleDeclineReachRequested;
            inputController.CancelReachRequested += HandleCancelReachRequested;
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
            subscribedInputController.ReachRequested -= HandleReachRequested;
            subscribedInputController.DeclineReachRequested -= HandleDeclineReachRequested;
            subscribedInputController.CancelReachRequested -= HandleCancelReachRequested;
            subscribedInputController = null;
        }

        private void SubscribePlayerArea3DPresenterEvents()
        {
            if (playerArea3DPresenter == null)
                return;

            if (subscribedPlayerArea3DPresenter == playerArea3DPresenter)
                return;

            UnsubscribePlayerArea3DPresenterEvents();
            playerArea3DPresenter.HandTileClicked += HandleHandTileClicked;
            playerArea3DPresenter.DrawnTileClicked += HandleDrawnTileClicked;
            subscribedPlayerArea3DPresenter = playerArea3DPresenter;
        }

        private void UnsubscribePlayerArea3DPresenterEvents()
        {
            if (subscribedPlayerArea3DPresenter == null)
                return;

            subscribedPlayerArea3DPresenter.HandTileClicked -= HandleHandTileClicked;
            subscribedPlayerArea3DPresenter.DrawnTileClicked -= HandleDrawnTileClicked;
            subscribedPlayerArea3DPresenter = null;
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

        private void HandleReachRequested()
        {
            if (!TryGetGameFlow("Cannot declare reach because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestDeclareReach();
        }

        private void HandleDeclineReachRequested()
        {
            if (!TryGetGameFlow("Cannot decline reach because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestDeclineReach();
        }

        private void HandleCancelReachRequested()
        {
            if (!TryGetGameFlow("Cannot cancel reach discard selection because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestCancelReachDiscardSelection();
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
