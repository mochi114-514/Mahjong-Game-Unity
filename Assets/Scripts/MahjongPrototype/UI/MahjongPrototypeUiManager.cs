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

        [Header("3D Table Center UI")]
        [Tooltip("Optional controller for the world-space table center UI.")]
        [SerializeField] private MahjongTableCenterUiController tableCenterUiController;

        [Header("Input")]
        [Tooltip("Controller for draw, skill, retry, and win decision input.")]
        [SerializeField] private MahjongUiInputController inputController;

        [Header("Command Routing")]
        [Tooltip("Routes UI input events to MahjongGameFlow commands.")]
        [SerializeField] private MahjongUiCommandRouter commandRouter;

        [Header("Win Decision")]
        [SerializeField] private MahjongWinDecisionController winDecisionController;

        [Header("Pon Decision")]
        [SerializeField] private MahjongPonDecisionController ponDecisionController;

        [Header("Reach Decision")]
        [SerializeField] private MahjongReachDecisionController reachDecisionController;

        [Header("Round Result")]
        [SerializeField] private MahjongRoundResultController roundResultController;

        [Header("Log Preview")]
        [Tooltip("Controller for the on-screen recent log preview.")]
        [SerializeField] private MahjongLogPreviewController logPreviewController;

        [Header("Zero Han Tenpai")]
        [SerializeField] private MahjongZeroHanTenpaiController zeroHanTenpaiController;

        [Header("Furiten")]
        [SerializeField] private MahjongFuritenController furitenController;

        private bool warnedMissingFlow;
        private bool warnedMissingEventNotifier;
        private bool warnedMissingDisplayController;
        private bool warnedMissingInputController;
        private bool warnedMissingCommandRouter;
        private bool warnedMissingWinDecisionController;
        private bool warnedMissingPonDecisionController;
        private bool warnedMissingReachDecisionController;
        private bool warnedMissingRoundResultController;
        private bool warnedMissingLogPreviewController;
        private bool warnedMissingZeroHanTenpaiController;
        private bool warnedMissingFuritenController;
        private readonly MeldCallService meldCallService = new MeldCallService();

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
            EnsurePonDecisionController();
            EnsureReachDecisionController();
            EnsureRoundResultController();
            EnsureLogPreviewController();
            EnsureZeroHanTenpaiController();
            EnsureFuritenController();
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
            EnsurePonDecisionController();
            EnsureReachDecisionController();
            EnsureRoundResultController();
            EnsureLogPreviewController();
            EnsureZeroHanTenpaiController();
            EnsureFuritenController();
            RefreshFromFlow();
            RefreshLogPreview();
        }

        private void OnDisable()
        {
            UnsubscribeNotifications();
        }

        public void Refresh(MahjongGameState state)
        {
            Refresh(state, true);
        }

        private void Refresh(MahjongGameState state, bool refreshTenpaiIndicators)
        {
            if (state == null)
            {
                ClearRoundResultUi();
                RefreshTableCenterUi(null);
                RefreshPonDecision(null);
                ClearZeroHanTenpaiUi();
                ClearFuritenUi();
                return;
            }

            RefreshDisplay(state);
            RefreshTableCenterUi(state);
            RefreshPlayerArea3D(state);
            RefreshWinDecision(state);
            RefreshPonDecision(state);
            RefreshReachDecision(state);
            RefreshRoundResult(state);
            RefreshInteractionState(state);
            RefreshLogPreview();
            if (refreshTenpaiIndicators)
            {
                RefreshZeroHanTenpaiUi();
                RefreshFuritenUi();
            }
        }

        public void RefreshFromFlow()
        {
            RefreshFromFlow(true);
        }

        private void RefreshFromFlow(bool refreshTenpaiIndicators)
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "MahjongGameFlow is not assigned.");
                ClearRoundResultUi();
                RefreshPonDecision(null);
                ClearZeroHanTenpaiUi();
                ClearFuritenUi();
                return;
            }

            Refresh(gameFlow.CurrentState, refreshTenpaiIndicators);
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

            if (tableCenterUiController == null)
                tableCenterUiController = GetComponentInChildren<MahjongTableCenterUiController>(true);

            if (inputController == null)
                inputController = GetComponentInChildren<MahjongUiInputController>(true);

            if (commandRouter == null)
                commandRouter = GetComponentInChildren<MahjongUiCommandRouter>(true);

            if (logPreviewController == null)
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);

            if (zeroHanTenpaiController == null)
                zeroHanTenpaiController = GetComponentInChildren<MahjongZeroHanTenpaiController>(true);

            if (furitenController == null)
                furitenController = GetComponentInChildren<MahjongFuritenController>(true);

            if (roundResultController == null)
                roundResultController = GetComponentInChildren<MahjongRoundResultController>(true);
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
            eventNotifier.ReactionWindowStarted += HandleReactionWindowChanged;
            eventNotifier.ReactionWindowAnswered += HandleReactionWindowAnswered;
            eventNotifier.ReactionWindowResolved += HandleReactionWindowResolved;
            eventNotifier.ReactionWindowClosed += HandleReactionWindowClosed;
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
            eventNotifier.RoundResultReady += HandleRoundResultReady;
            eventNotifier.RoundResultConfirmed += HandleRoundResultConfirmed;
            eventNotifier.GameEnded += HandleGameEnded;
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
            eventNotifier.ReactionWindowStarted -= HandleReactionWindowChanged;
            eventNotifier.ReactionWindowAnswered -= HandleReactionWindowAnswered;
            eventNotifier.ReactionWindowResolved -= HandleReactionWindowResolved;
            eventNotifier.ReactionWindowClosed -= HandleReactionWindowClosed;
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
            eventNotifier.RoundResultReady -= HandleRoundResultReady;
            eventNotifier.RoundResultConfirmed -= HandleRoundResultConfirmed;
            eventNotifier.GameEnded -= HandleGameEnded;
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

        private void EnsurePonDecisionController()
        {
            if (ponDecisionController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingPonDecisionController,
                "MahjongPonDecisionController is not assigned. Assign its dedicated pon decision area in the Inspector.");
        }

        private void EnsureReachDecisionController()
        {
            if (reachDecisionController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingReachDecisionController,
                "MahjongReachDecisionController is not assigned. Assign it in the Inspector.");
        }

        private void EnsureRoundResultController()
        {
            if (roundResultController == null)
            {
                roundResultController = GetComponentInChildren<MahjongRoundResultController>(true);
            }
        }

        private void HandleRoundStarted(int _, int __)
        {
            RefreshFromFlow(false);
            ClearZeroHanTenpaiUi();
            ClearFuritenUi();
        }

        private void HandleRoundSetupCompleted()
        {
            RefreshFromFlow();
        }

        private void HandleTurnStarted(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
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
            if (IsSelfSeat(result.Seat))
            {
                ClearZeroHanTenpaiUi();
                ClearFuritenUi();
            }
        }

        private void HandleTileDiscarded(DiscardRecord record)
        {
            RefreshPlayerHandForSeat(record.ActorSeat);
            RefreshPlayerDrawnTileForSeat(record.ActorSeat);
            RefreshPlayerDiscardRiverForSeat(record.ActorSeat);
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
            if (IsSelfSeat(record.ActorSeat))
            {
                RefreshZeroHanTenpaiUi();
                RefreshFuritenUi();
            }
        }

        private void HandleReactionWindowChanged(ReactionWindow _)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReactionWindowAnswered(ReactionWindowAnswerResult _)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReactionWindowResolved(ReactionWindowResolution resolution)
        {
            if ((resolution.Type == ReactionWindowResolutionType.PonDeclared ||
                 resolution.Type == ReactionWindowResolutionType.ChiDeclared) &&
                resolution.Candidate != null)
            {
                RefreshPlayerHandForSeat(resolution.Candidate.Seat);
                RefreshPlayerDiscardRiverForSeat(resolution.SourceDiscard.ActorSeat);
                RefreshPlayerOpenMeldsForSeat(resolution.Candidate.Seat);
            }

            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReactionWindowClosed(int _)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
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

        private void HandleWinChecked(SeatId seat, int _, bool __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();

            if (IsSelfSeat(seat))
                RefreshFuritenUi();
        }

        private void HandleWinDeclared(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclined(SeatId seat, int _)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();

            if (IsSelfSeat(seat))
                RefreshFuritenUi();
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
            ClearZeroHanTenpaiUi();
            ClearFuritenUi();
        }

        private void HandleRoundResultReady(RoundResult result)
        {
            SetRoundResultUi(result);
        }

        private void HandleRoundResultConfirmed(RoundResult _)
        {
            ClearRoundResultUi();
        }

        private void HandleGameEnded(RoundResult _)
        {
            ClearRoundResultUi();
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
            if (state == null)
                return;

            RefreshDisplay(state);
            RefreshTableCenterUi(state);
        }

        private void RefreshPlayerArea3D(MahjongGameState state)
        {
            if (playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.Refresh(state, CanUseSelfGameplayInput(state));
        }

        private void RefreshTableCenterUi(MahjongGameState state)
        {
            if (tableCenterUiController != null)
                tableCenterUiController.Refresh(state);
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

        private void RefreshPlayerOpenMeldsForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || playerArea3DPresenter == null)
                return;

            playerArea3DPresenter.RefreshOpenMeldsForSeat(state, seat);
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

        private void RefreshPonDecision(MahjongGameState state)
        {
            if (ponDecisionController == null)
                EnsurePonDecisionController();

            if (ponDecisionController == null)
                return;

            ReactionWindow reactionWindow = state != null
                ? state.CurrentReactionWindow
                : null;
            if (state == null || reactionWindow == null)
            {
                ponDecisionController.SetMeldCallDecision(false, null, null);
                return;
            }

            IReadOnlyList<MeldCallKind> availableKinds =
                meldCallService.GetAvailableKinds(reactionWindow, state.SelfSeat);
            bool showPon = ContainsMeldCallKind(availableKinds, MeldCallKind.Pon);
            IReadOnlyList<ChiOption> chiOptions = ContainsMeldCallKind(
                    availableKinds,
                    MeldCallKind.Chi)
                ? FindSelfChiOptions(reactionWindow, state.SelfSeat)
                : null;
            ponDecisionController.SetMeldCallDecision(
                showPon,
                chiOptions,
                reactionWindow.SourceDiscard.Tile);
        }

        private static bool ContainsMeldCallKind(
            IReadOnlyList<MeldCallKind> kinds,
            MeldCallKind expectedKind)
        {
            if (kinds == null)
                return false;

            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i] == expectedKind)
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<ChiOption> FindSelfChiOptions(
            ReactionWindow reactionWindow,
            SeatId selfSeat)
        {
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate.Seat == selfSeat && candidate.Kind == ReactionKind.Chi &&
                    candidate.IsPending && candidate.ChiDetail != null)
                {
                    return candidate.ChiDetail.Options;
                }
            }

            return null;
        }

        private void RefreshPonDecisionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            RefreshPonDecision(state);
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

        private void RefreshRoundResult(MahjongGameState state)
        {
            if (state != null &&
                state.IsRoundResultPending &&
                state.CurrentRoundResult != null)
            {
                SetRoundResultUi(state.CurrentRoundResult);
                return;
            }

            ClearRoundResultUi();
        }

        private void SetRoundResultUi(RoundResult result)
        {
            if (roundResultController == null)
                EnsureRoundResultController();

            if (roundResultController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingRoundResultController,
                    "MahjongRoundResultController is not assigned. Assign it in the Inspector.");
                return;
            }

            roundResultController.SetResult(result);
        }

        private void ClearRoundResultUi()
        {
            if (roundResultController == null)
                EnsureRoundResultController();

            if (roundResultController != null)
                roundResultController.Clear();
        }

        private void RefreshInteractionState(MahjongGameState state)
        {
            bool canUseSelfTileInput = CanUseSelfGameplayInput(state);
            bool canUseControlPanelInput = CanUseControlAreaInput(state);
            bool canUseAutoSortInput = CanUseAutoSortInput(state);

            if (inputController != null)
            {
                inputController.SetGameplayInputInteractable(canUseControlPanelInput);
                inputController.SetAutoSortInteractable(canUseAutoSortInput);
            }

            if (playerArea3DPresenter != null)
                playerArea3DPresenter.SetSelfInteractable(state, canUseSelfTileInput);

            ApplyReachDiscardCandidateInteractable(state);
            ApplyDeclaredReachInteractable(state, canUseSelfTileInput);
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

        private bool CanUseControlAreaInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                !state.IsWinDecisionPending &&
                !state.IsReactionWindowPending &&
                !state.IsRoundEnded &&
                !state.IsReachDiscardSelectionPending &&
                !IsDeclaredReachWaitingForDraw(state);
        }

        private static bool CanUseAutoSortInput(MahjongGameState state)
        {
            return state != null &&
                !state.IsReachDecisionPending &&
                !state.IsReachDiscardSelectionPending;
        }

        private static bool IsDeclaredReachWaitingForDraw(MahjongGameState state)
        {
            return state != null &&
                state.IsSelfTurn &&
                !state.IsInteractionLocked &&
                state.GetPlayerSeat(state.SelfSeat).IsReachDeclared &&
                !state.GetPlayerSeat(state.SelfSeat).HasDrawnTile;
        }

        private bool IsSelfSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            return state != null && state.IsSelfSeat(seat);
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

        private void EnsureZeroHanTenpaiController()
        {
            if (zeroHanTenpaiController == null)
            {
                zeroHanTenpaiController = GetComponentInChildren<MahjongZeroHanTenpaiController>(true);
            }

            if (zeroHanTenpaiController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingZeroHanTenpaiController,
                "MahjongZeroHanTenpaiController is not assigned. Assign it in the Inspector.");
        }

        private void EnsureFuritenController()
        {
            if (furitenController == null)
            {
                furitenController = GetComponentInChildren<MahjongFuritenController>(true);
            }

            if (furitenController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingFuritenController,
                "MahjongFuritenController is not assigned. Assign it in the Inspector.");
        }

        private void RefreshZeroHanTenpaiUi()
        {
            if (zeroHanTenpaiController == null)
                EnsureZeroHanTenpaiController();

            if (zeroHanTenpaiController == null)
                return;

            if (gameFlow == null)
            {
                zeroHanTenpaiController.Clear();
                return;
            }

            NoYakuTenpaiEvaluationResult result = gameFlow.EvaluateSelfNoYakuTenpai();
            zeroHanTenpaiController.SetVisible(result.ShouldShowZeroHanTenpai);
        }

        private void RefreshFuritenUi()
        {
            if (furitenController == null)
                EnsureFuritenController();

            if (furitenController == null)
                return;

            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (gameFlow == null ||
                state == null ||
                state.IsRoundEnded)
            {
                furitenController.Clear();
                return;
            }

            FuritenEvaluationResultSet resultSet = gameFlow.EvaluateAllFuriten();
            bool shouldShow =
                resultSet != null &&
                resultSet.TryGet(
                    state.SelfSeat,
                    out FuritenSeatEvaluationResult result) &&
                result.IsEvaluated &&
                result.IsFuriten;
            furitenController.SetVisible(shouldShow);
        }

        private void ClearZeroHanTenpaiUi()
        {
            if (zeroHanTenpaiController == null)
                EnsureZeroHanTenpaiController();

            if (zeroHanTenpaiController != null)
                zeroHanTenpaiController.Clear();
        }

        private void ClearFuritenUi()
        {
            if (furitenController == null)
                EnsureFuritenController();

            if (furitenController != null)
                furitenController.Clear();
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
