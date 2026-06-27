using MahjongPrototype;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using MahjongPrototype.UI3D;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Prototype UI Manager")]
    public sealed class MahjongPrototypeUiManager : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("Game flow controller for the prototype.")]
        [SerializeField] private MahjongGameFlow gameFlow;
        [Tooltip("Event notifier used to refresh UI after game events.")]
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Display")]
        [Tooltip("Controller for global status and skill text.")]
        [SerializeField] private MahjongUiDisplayController displayController;

        [Header("Player Area")]
        [Tooltip("Presenter for the four player areas around the table.")]
        [SerializeField] private MahjongPlayerAreaPresenter playerAreaPresenter;

        [Header("3D Player Area")]
        [Tooltip("Optional presenter for experimental 3D player area views.")]
        [SerializeField] private Mahjong3DPlayerAreaPresenter playerArea3DPresenter;

        [Header("Input")]
        [Tooltip("Controller for draw, skill, retry, and win decision input.")]
        [SerializeField] private MahjongUiInputController inputController;

        [Header("Command Routing")]
        [Tooltip("Routes UI input events to MahjongGameFlow commands.")]
        [SerializeField] private MahjongUiCommandRouter commandRouter;

        [Header("Win Decision")]
        [SerializeField] private MahjongWinDecisionController winDecisionController;

        [Header("Log Preview")]
        [Tooltip("Controller for the on-screen recent log preview.")]
        [SerializeField] private MahjongLogPreviewController logPreviewController;

        private bool warnedMissingFlow;
        private bool warnedMissingEventNotifier;
        private bool warnedMissingDisplayController;
        private bool warnedMissingPlayerAreaPresenter;
        private bool warnedMissingInputController;
        private bool warnedMissingCommandRouter;
        private bool warnedMissingWinDecisionController;
        private bool warnedMissingLogPreviewController;

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
            EnsureDisplayController();
            EnsurePlayerAreaPresenter();
            EnsureInputController();
            EnsureCommandRouter();
            SyncAutoSortToggleFromFlow();
            EnsureWinDecisionController();
            EnsureLogPreviewController();
            SubscribeNotifications();
            RefreshFromFlow();
        }

        private void Start()
        {
            CacheReferences();
            EnsureDisplayController();
            EnsurePlayerAreaPresenter();
            EnsureInputController();
            EnsureCommandRouter();
            SyncAutoSortToggleFromFlow();
            EnsureWinDecisionController();
            EnsureLogPreviewController();
            RefreshFromFlow();
            RefreshLogPreview();
        }

        private void OnDisable()
        {
            UnsubscribeNotifications();
        }

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
                return;

            RefreshDisplay(state);
            RefreshPlayerArea(state);
            RefreshPlayerArea3D(state);
            RefreshWinDecision(state);
            RefreshInteractionState(state);
            RefreshLogPreview();
        }

        public void RefreshFromFlow()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "MahjongGameFlow is not assigned.");
                return;
            }

            Refresh(gameFlow.CurrentState);
        }

        private void CacheReferences()
        {
            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (eventNotifier == null && gameFlow != null)
                eventNotifier = gameFlow.EventNotifier;

            if (displayController == null)
                displayController = GetComponentInChildren<MahjongUiDisplayController>(true);

            if (playerAreaPresenter == null)
                playerAreaPresenter = GetComponentInChildren<MahjongPlayerAreaPresenter>(true);

            if (playerArea3DPresenter == null)
                playerArea3DPresenter = GetComponentInChildren<Mahjong3DPlayerAreaPresenter>(true);

            if (inputController == null)
                inputController = GetComponentInChildren<MahjongUiInputController>(true);

            if (commandRouter == null)
                commandRouter = GetComponentInChildren<MahjongUiCommandRouter>(true);

            if (logPreviewController == null)
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);
        }

        private void SubscribeNotifications()
        {
            if (eventNotifier == null)
            {
                WarnMissingOnce(
                    ref warnedMissingEventNotifier,
                    "MahjongEventNotifier is not assigned. Typed UI refresh events will not be received.");
                return;
            }

            eventNotifier.RoundStarted += HandleRoundStarted;
            eventNotifier.RoundSetupCompleted += HandleRoundSetupCompleted;
            eventNotifier.TurnStarted += HandleTurnStarted;
            eventNotifier.TileDrawn += HandleTileDrawn;
            eventNotifier.TileDiscarded += HandleTileDiscarded;
            eventNotifier.SkillActivated += HandleSkillActivated;
            eventNotifier.SkillEffectRegistered += HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved += HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired += HandleSkillEffectExpired;
            eventNotifier.WinChecked += HandleWinChecked;
            eventNotifier.WinDeclared += HandleWinDeclared;
            eventNotifier.WinDeclined += HandleWinDeclined;
            eventNotifier.HandAutoSorted += HandleHandAutoSorted;
            eventNotifier.RoundEnded += HandleRoundEnded;
        }

        private void UnsubscribeNotifications()
        {
            if (eventNotifier == null)
                return;

            eventNotifier.RoundStarted -= HandleRoundStarted;
            eventNotifier.RoundSetupCompleted -= HandleRoundSetupCompleted;
            eventNotifier.TurnStarted -= HandleTurnStarted;
            eventNotifier.TileDrawn -= HandleTileDrawn;
            eventNotifier.TileDiscarded -= HandleTileDiscarded;
            eventNotifier.SkillActivated -= HandleSkillActivated;
            eventNotifier.SkillEffectRegistered -= HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved -= HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired -= HandleSkillEffectExpired;
            eventNotifier.WinChecked -= HandleWinChecked;
            eventNotifier.WinDeclared -= HandleWinDeclared;
            eventNotifier.WinDeclined -= HandleWinDeclined;
            eventNotifier.HandAutoSorted -= HandleHandAutoSorted;
            eventNotifier.RoundEnded -= HandleRoundEnded;
        }

        private void EnsureDisplayController()
        {
            if (displayController == null)
            {
                displayController = GetComponentInChildren<MahjongUiDisplayController>(true);
            }

            if (displayController != null)
                return;

            displayController = gameObject.AddComponent<MahjongUiDisplayController>();
            if (displayController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingDisplayController,
                    "MahjongUiDisplayController is not assigned. Add it to the UI GameObject and assign the global status texts.");
            }
        }

        private void EnsurePlayerAreaPresenter()
        {
            if (playerAreaPresenter == null)
            {
                playerAreaPresenter = GetComponentInChildren<MahjongPlayerAreaPresenter>(true);
            }

            if (playerAreaPresenter != null)
                return;

            playerAreaPresenter = gameObject.AddComponent<MahjongPlayerAreaPresenter>();
            if (playerAreaPresenter == null)
            {
                WarnMissingOnce(
                    ref warnedMissingPlayerAreaPresenter,
                    "MahjongPlayerAreaPresenter is not assigned. Add it to the UI GameObject and assign the player UI controllers.");
            }
        }

        private void EnsureInputController()
        {
            if (inputController == null)
            {
                inputController = GetComponentInChildren<MahjongUiInputController>(true);
            }

            if (inputController != null)
                return;

            inputController = gameObject.AddComponent<MahjongUiInputController>();
            if (inputController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingInputController,
                    "MahjongUiInputController is not assigned. Add it to the UI GameObject and assign the Draw/Skill/Retry controls.");
            }
        }

        private void SyncAutoSortToggleFromFlow()
        {
            if (inputController == null || gameFlow == null)
                return;

            inputController.SetAutoSortWithoutNotify(gameFlow.IsAutoSortEnabled);
        }

        private void EnsureCommandRouter()
        {
            if (commandRouter == null)
            {
                commandRouter = GetComponentInChildren<MahjongUiCommandRouter>(true);
            }

            if (commandRouter != null)
            {
                commandRouter.RefreshSubscriptions();
                return;
            }

            commandRouter = gameObject.AddComponent<MahjongUiCommandRouter>();
            if (commandRouter != null)
            {
                commandRouter.RefreshSubscriptions();
            }
            else
            {
                WarnMissingOnce(
                    ref warnedMissingCommandRouter,
                    "MahjongUiCommandRouter is not assigned. UI input commands will not be routed.");
            }
        }

        private void EnsureWinDecisionController()
        {
            if (winDecisionController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingWinDecisionController,
                "MahjongWinDecisionController is not assigned. Add it to the UI GameObject and assign WinDecisionArea and its buttons.");
        }

        private void HandleRoundStarted(int _, int __)
        {
            RefreshFromFlow();
        }

        private void HandleRoundSetupCompleted()
        {
            RefreshFromFlow();
        }

        private void HandleTurnStarted(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleTileDrawn(DrawResult result)
        {
            if (!result.Success || result.Purpose == DrawPurpose.InitialDeal)
                return;

            RefreshPlayerHandForSeat(result.Seat);
            RefreshPlayerDrawnTileForSeat(result.Seat);
            RefreshGlobalStatus();
            RefreshInteractionUi();
        }

        private void HandleTileDiscarded(DiscardRecord record)
        {
            RefreshPlayerHandForSeat(record.ActorSeat);
            RefreshPlayerDrawnTileForSeat(record.ActorSeat);
            RefreshPlayerDiscardRiverForSeat(record.ActorSeat);
            RefreshGlobalStatus();
            RefreshInteractionUi();
        }

        private void HandleSkillActivated(SeatId _, ActiveSkillEffect __)
        {
            RefreshGlobalStatus();
        }

        private void HandleSkillEffectRegistered(ActiveSkillEffect _)
        {
            RefreshGlobalStatus();
        }

        private void HandleSkillEffectResolved(DrawResult _)
        {
            RefreshGlobalStatus();
        }

        private void HandleSkillEffectExpired(ActiveSkillEffect _, string __)
        {
            RefreshGlobalStatus();
        }

        private void HandleWinChecked(SeatId _, int __, bool ___)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclared(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclined(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleHandAutoSorted(SeatId seat, int _)
        {
            RefreshPlayerHandForSeat(seat);
        }

        private void HandleRoundEnded(string _)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void RefreshDisplay(MahjongGameState state)
        {
            if (displayController == null)
                EnsureDisplayController();

            if (displayController != null)
                displayController.Refresh(state);
        }

        private void RefreshGlobalStatus()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshDisplay(state);
        }

        private void RefreshPlayerArea(MahjongGameState state)
        {
            if (playerAreaPresenter == null)
                EnsurePlayerAreaPresenter();

            if (playerAreaPresenter != null)
                playerAreaPresenter.Refresh(state, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerArea3D(MahjongGameState state)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.Refresh(state, CanUseSelfGameplayInput(state));
        }

        private void RefreshSelfBottomHand3D(MahjongGameState state)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.RefreshSelfBottomHand(state, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerHandForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            if (playerAreaPresenter == null)
                EnsurePlayerAreaPresenter();

            if (playerAreaPresenter != null)
                playerAreaPresenter.RefreshHandForSeat(state, seat, CanUseSelfGameplayInput(state));

            if (seat == state.SelfSeat)
                RefreshSelfBottomHand3D(state);
        }

        private void RefreshPlayerDrawnTileForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            if (playerAreaPresenter == null)
                EnsurePlayerAreaPresenter();

            if (playerAreaPresenter != null)
                playerAreaPresenter.RefreshDrawnTileForSeat(state, seat, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerDiscardRiverForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            if (playerAreaPresenter == null)
                EnsurePlayerAreaPresenter();

            if (playerAreaPresenter != null)
                playerAreaPresenter.RefreshDiscardRiverForSeat(state, seat);
        }

        private void RefreshWinDecision(MahjongGameState state)
        {
            if (winDecisionController == null)
                EnsureWinDecisionController();

            if (winDecisionController != null)
            {
                bool showSelfWinDecision =
                    state != null &&
                    state.IsWinDecisionPending &&
                    state.WinDecisionSeat == state.SelfSeat;
                WinType? winType = showSelfWinDecision
                    ? state.WinDecisionType
                    : null;
                winDecisionController.SetWinDecision(showSelfWinDecision, winType);
            }
        }

        private void RefreshWinDecisionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshWinDecision(state);
        }

        private void RefreshInteractionState(MahjongGameState state)
        {
            bool canUseGameplayInput = CanUseSelfGameplayInput(state);

            if (inputController != null)
                inputController.SetGameplayInputInteractable(canUseGameplayInput);

            if (playerAreaPresenter == null)
                EnsurePlayerAreaPresenter();

            if (playerAreaPresenter != null)
                playerAreaPresenter.SetSelfInteractable(state, canUseGameplayInput);
        }

        private void RefreshInteractionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
            {
                RefreshInteractionState(state);
                RefreshSelfBottomHand3D(state);
            }
        }

        private bool CanUseSelfGameplayInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                state.IsSelfTurn &&
                !state.IsInteractionLocked;
        }

        private void EnsureLogPreviewController()
        {
            if (logPreviewController == null)
            {
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);
            }

            if (logPreviewController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingLogPreviewController,
                "MahjongLogPreviewController is not assigned. Add it to the UI GameObject and assign RecentLogText there.");
        }

        private void RefreshLogPreview()
        {
            if (logPreviewController == null)
                EnsureLogPreviewController();

            if (logPreviewController != null)
                logPreviewController.Refresh();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongPrototypeUiManager)}: {message}", this);
        }
    }
}
