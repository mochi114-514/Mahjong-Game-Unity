using System.Collections.Generic;
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

        [Header("Reach Decision")]
        [SerializeField] private MahjongReachDecisionController reachDecisionController;

        [Header("Log Preview")]
        [Tooltip("Controller for the on-screen recent log preview.")]
        [SerializeField] private MahjongLogPreviewController logPreviewController;

        private bool warnedMissingFlow;
        private bool warnedMissingEventNotifier;
        private bool warnedMissingDisplayController;
        private bool warnedMissingInputController;
        private bool warnedMissingCommandRouter;
        private bool warnedMissingWinDecisionController;
        private bool warnedMissingReachDecisionController;
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
            EnsureInputController();
            EnsureCommandRouter();
            SyncAutoSortToggleFromFlow();
            EnsureWinDecisionController();
            EnsureReachDecisionController();
            EnsureLogPreviewController();
            SubscribeNotifications();
            RefreshFromFlow();
        }

        private void Start()
        {
            CacheReferences();
            EnsureDisplayController();
            EnsureInputController();
            EnsureCommandRouter();
            SyncAutoSortToggleFromFlow();
            EnsureWinDecisionController();
            EnsureReachDecisionController();
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
            RefreshPlayerArea3D(state);
            RefreshWinDecision(state);
            RefreshReachDecision(state);
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
            eventNotifier.ReachDecisionStarted += HandleReachDecisionStarted;
            eventNotifier.ReachDiscardSelectionStarted += HandleReachDiscardSelectionStarted;
            eventNotifier.ReachDiscardSelectionCanceled += HandleReachDiscardSelectionCanceled;
            eventNotifier.ReachDeclared += HandleReachDeclared;
            eventNotifier.ReachDeclined += HandleReachDeclined;
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
            eventNotifier.ReachDecisionStarted -= HandleReachDecisionStarted;
            eventNotifier.ReachDiscardSelectionStarted -= HandleReachDiscardSelectionStarted;
            eventNotifier.ReachDiscardSelectionCanceled -= HandleReachDiscardSelectionCanceled;
            eventNotifier.ReachDeclared -= HandleReachDeclared;
            eventNotifier.ReachDeclined -= HandleReachDeclined;
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

        private void EnsureReachDecisionController()
        {
            if (reachDecisionController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingReachDecisionController,
                "MahjongReachDecisionController is not assigned. Assign it in the Inspector.");
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
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleTileDrawn(DrawResult result)
        {
            if (!result.Success || result.Purpose == DrawPurpose.InitialDeal)
                return;

            RefreshPlayerHandForSeat(result.Seat);
            RefreshPlayerDrawnTileForSeat(result.Seat);
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleTileDiscarded(DiscardRecord record)
        {
            RefreshPlayerHandForSeat(record.ActorSeat);
            RefreshPlayerDrawnTileForSeat(record.ActorSeat);
            RefreshPlayerDiscardRiverForSeat(record.ActorSeat);
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
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
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclared(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclined(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDecisionStarted(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDiscardSelectionStarted(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDiscardSelectionCanceled(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDeclared(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDeclined(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
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
            RefreshReachDecisionUi();
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

        private void RefreshPlayerArea3D(MahjongGameState state)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.Refresh(state, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerHand3DForSeat(MahjongGameState state, SeatId seat)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.RefreshHandForSeat(state, seat, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerDrawnTile3DForSeat(MahjongGameState state, SeatId seat)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.RefreshDrawnTileForSeat(state, seat, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerDiscardRiver3DForSeat(MahjongGameState state, SeatId seat)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.RefreshDiscardRiverForSeat(state, seat);
        }

        private void RefreshPlayerHandForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            RefreshPlayerHand3DForSeat(state, seat);
        }

        private void RefreshPlayerDrawnTileForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            RefreshPlayerDrawnTile3DForSeat(state, seat);
        }

        private void RefreshPlayerDiscardRiverForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            RefreshPlayerDiscardRiver3DForSeat(state, seat);
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

        private void RefreshReachDecision(MahjongGameState state)
        {
            if (reachDecisionController == null)
                EnsureReachDecisionController();

            if (reachDecisionController != null)
            {
                bool showSelfReachDecision =
                    state != null &&
                    state.IsReachDecisionPending &&
                    state.ReachDecisionSeat == state.SelfSeat;
                bool showSelfReachCancel =
                    state != null &&
                    state.IsReachDiscardSelectionPending &&
                    state.ReachDecisionSeat == state.SelfSeat;
                reachDecisionController.SetReachUiVisible(showSelfReachDecision, showSelfReachCancel);
            }
        }

        private void RefreshReachDecisionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshReachDecision(state);
        }

        private void RefreshInteractionState(MahjongGameState state)
        {
            bool canUseGameplayInput = CanUseSelfGameplayInput(state);
            bool isDeclaredReachWaitingForDraw =
                state != null &&
                state.IsSelfTurn &&
                !state.IsInteractionLocked &&
                state.GetPlayerSeat(state.SelfSeat).IsReachDeclared &&
                !state.GetPlayerSeat(state.SelfSeat).HasDrawnTile;
            bool canUseControlPanelInput =
                canUseGameplayInput &&
                (state == null || !state.IsReachDiscardSelectionPending) &&
                !isDeclaredReachWaitingForDraw;

            if (inputController != null)
                inputController.SetGameplayInputInteractable(canUseControlPanelInput);

            if (playerArea3DPresenter != null)
                playerArea3DPresenter.SetSelfInteractable(state, canUseGameplayInput);

            ApplyReachDiscardCandidateInteractable(state);
            ApplyDeclaredReachInteractable(state, canUseGameplayInput);
        }

        private void RefreshInteractionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshInteractionState(state);
        }

        private bool CanUseSelfGameplayInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                state.IsSelfTurn &&
                !state.IsInteractionLocked;
        }

        private void ApplyReachDiscardCandidateInteractable(MahjongGameState state)
        {
            if (state == null ||
                !state.IsReachDiscardSelectionPending ||
                state.ReachDecisionSeat != state.SelfSeat)
            {
                if (playerArea3DPresenter != null && state != null)
                    playerArea3DPresenter.ClearSelfTileDimmed(state);

                return;
            }

            HashSet<int> handIndices = new HashSet<int>();
            bool drawnTileInteractable = false;
            for (int i = 0; i < state.ReachDiscardCandidates.Count; i++)
            {
                ReachDiscardCandidate candidate = state.ReachDiscardCandidates[i];
                if (candidate.Source == DiscardSource.Hand)
                {
                    handIndices.Add(candidate.HandIndex);
                }
                else if (candidate.Source == DiscardSource.DrawnTile)
                {
                    drawnTileInteractable = true;
                }
            }

            if (playerArea3DPresenter != null)
            {
                playerArea3DPresenter.SetSelfReachCandidateInteractable(
                    state,
                    handIndices,
                    drawnTileInteractable);
            }
        }

        private void ApplyDeclaredReachInteractable(MahjongGameState state, bool canUseGameplayInput)
        {
            if (state == null || state.IsReachDiscardSelectionPending)
                return;

            PlayerSeat selfPlayerSeat = state.GetPlayerSeat(state.SelfSeat);
            if (!selfPlayerSeat.IsReachDeclared)
                return;

            int[] noHandIndices = new int[0];
            bool drawnTileInteractable = false;

            if (playerArea3DPresenter != null)
            {
                playerArea3DPresenter.SetSelfHandTileInteractableByIndices(state, noHandIndices);
                playerArea3DPresenter.SetSelfDrawnTileInteractable(state, drawnTileInteractable);
            }
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
