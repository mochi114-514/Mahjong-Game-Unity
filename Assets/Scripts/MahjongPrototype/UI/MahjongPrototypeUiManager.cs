using System;
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
        private readonly struct TileSelectionIdentity : IEquatable<TileSelectionIdentity>
        {
            public TileSelectionIdentity(
                PlayerId playerId,
                SeatId seatId,
                int turnIndex,
                DiscardSource source,
                int handIndex,
                Tile tile)
            {
                PlayerId = playerId;
                SeatId = seatId;
                TurnIndex = turnIndex;
                Source = source;
                HandIndex = handIndex;
                Tile = tile;
            }

            public PlayerId PlayerId { get; }
            public SeatId SeatId { get; }
            public int TurnIndex { get; }
            public DiscardSource Source { get; }
            public int HandIndex { get; }
            public Tile Tile { get; }

            public Mahjong3DTileHoverInfo ToTileInfo()
            {
                return new Mahjong3DTileHoverInfo(SeatId, Source, HandIndex, Tile);
            }

            public bool Equals(TileSelectionIdentity other)
            {
                return PlayerId == other.PlayerId &&
                    SeatId == other.SeatId &&
                    TurnIndex == other.TurnIndex &&
                    Source == other.Source &&
                    HandIndex == other.HandIndex &&
                    Tile == other.Tile;
            }

            public override bool Equals(object obj)
            {
                return obj is TileSelectionIdentity other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = PlayerId.GetHashCode();
                    hash = (hash * 397) ^ (int)SeatId;
                    hash = (hash * 397) ^ TurnIndex;
                    hash = (hash * 397) ^ (int)Source;
                    hash = (hash * 397) ^ HandIndex;
                    hash = (hash * 397) ^ Tile.GetHashCode();
                    return hash;
                }
            }
        }

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

        [Tooltip("Optional mouse hover input paired with the 3D player area presenter.")]
        [SerializeField] private Mahjong3DTileRaycastInput tileRaycastInput;

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

        [Header("Winning Candidates")]
        [SerializeField] private MahjongWinningCandidateController winningCandidateController;

        [Header("Round Result")]
        [SerializeField] private MahjongRoundResultController roundResultController;

        [Header("Round Progress")]
        [SerializeField] private MahjongRoundProgressController roundProgressController;

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
        private bool warnedMissingRoundProgressController;
        private bool warnedMissingLogPreviewController;
        private bool warnedMissingZeroHanTenpaiController;
        private bool warnedMissingFuritenController;
        private MahjongRoundProgressController subscribedRoundProgressController;
        private Mahjong3DPlayerAreaPresenter subscribedTileHoverPresenter;
        private Mahjong3DTileRaycastInput subscribedTileRaycastInput;
        private Mahjong3DTileHoverInfo? hoveredSelfTile;
        private Mahjong3DTileHoverInfo? pendingTileHoverExit;
        private TileSelectionIdentity? selectedSelfTile;
        private bool discardCommandInProgress;
        private bool tileHoverReevaluationPending;
        private int? reactionHighlightDiscardId;
        private int? resolvedReactionWindowIdAwaitingClosed;
        private readonly WinningTileCandidateEvaluator winningTileCandidateEvaluator =
            new WinningTileCandidateEvaluator();

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
            EnsureWinningCandidateController();
            EnsureRoundResultController();
            EnsureRoundProgressController();
            SubscribeRoundProgressPresentation();
            EnsureLogPreviewController();
            EnsureZeroHanTenpaiController();
            EnsureFuritenController();
            SubscribeTileHoverPresentation();
            SubscribeTileHoverReevaluation();
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
            EnsureWinningCandidateController();
            EnsureRoundResultController();
            EnsureRoundProgressController();
            SubscribeRoundProgressPresentation();
            EnsureLogPreviewController();
            EnsureZeroHanTenpaiController();
            EnsureFuritenController();
            SubscribeTileHoverPresentation();
            SubscribeTileHoverReevaluation();
            RefreshFromFlow();
            RefreshLogPreview();
        }

        private void OnDisable()
        {
            NotifyRoundProgressFallback();
            UnsubscribeRoundProgressPresentation();
            UnsubscribeTileHoverPresentation();
            UnsubscribeTileHoverReevaluation();
            UnsubscribeNotifications();
            if (inputController != null)
                inputController.ClearReactionResponseBindings();
            if (ponDecisionController != null)
                ponDecisionController.ClearReactionMeldCallDecision();
            resolvedReactionWindowIdAwaitingClosed = null;
            ClearTileHoverState();
            ClearTileSelection(true, false);
            if (winningCandidateController != null)
                winningCandidateController.Clear();
            ClearDiscardReactionHighlights();
        }

        public void Refresh(MahjongGameState state)
        {
            Refresh(state, true);
        }

        private void Refresh(MahjongGameState state, bool refreshTenpaiIndicators)
        {
            if (state == null)
            {
                ClearTileHoverState();
                ClearTileSelection(true, false);
                reactionHighlightDiscardId = null;
                resolvedReactionWindowIdAwaitingClosed = null;
                inputController?.ClearReactionResponseBindings();
                ClearRoundResultUi();
                RefreshTableCenterUi(null);
                RefreshPonDecision(null);
                RefreshReachDecision(null);
                ClearZeroHanTenpaiUi();
                ClearFuritenUi();
                ClearDiscardReactionHighlights();
                return;
            }

            ClearTileSelection(false, false);
            RequestTileHoverReevaluation();
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
                ClearTileHoverState();
                ClearTileSelection(true, false);
                WarnMissingOnce(ref warnedMissingFlow, "MahjongGameFlow is not assigned.");
                reactionHighlightDiscardId = null;
                resolvedReactionWindowIdAwaitingClosed = null;
                inputController?.ClearReactionResponseBindings();
                ClearRoundResultUi();
                RefreshPonDecision(null);
                RefreshReachDecision(null);
                ClearZeroHanTenpaiUi();
                ClearFuritenUi();
                ClearDiscardReactionHighlights();
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

            if (tileRaycastInput == null && playerArea3DPresenter != null)
            {
                tileRaycastInput =
                    playerArea3DPresenter.GetComponent<Mahjong3DTileRaycastInput>();
            }

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

            if (roundProgressController == null)
                roundProgressController = GetComponentInChildren<MahjongRoundProgressController>(true);

            if (winningCandidateController == null)
            {
                winningCandidateController =
                    GetComponentInChildren<MahjongWinningCandidateController>(true);
            }
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

            eventNotifier.RunStarted += HandleRunStarted;
            eventNotifier.RoundStarted += HandleRoundStarted;
            eventNotifier.RoundSetupCompleted += HandleRoundSetupCompleted;
            eventNotifier.TurnStarted += HandleTurnStarted;
            eventNotifier.TileDrawn += HandleTileDrawn;
            eventNotifier.TileDiscarded += HandleTileDiscarded;
            eventNotifier.ReactionWindowStarted += HandleReactionWindowChanged;
            eventNotifier.ReactionWindowAnswered += HandleReactionWindowAnswered;
            eventNotifier.ReactionWindowResolved += HandleReactionWindowResolved;
            eventNotifier.ReactionWindowClosed += HandleReactionWindowClosed;
            eventNotifier.MeldDeclared += HandleMeldDeclared;
            eventNotifier.SkillActivated += HandleSkillActivated;
            eventNotifier.SkillReserved += HandleSkillReserved;
            eventNotifier.SkillEffectRegistered += HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved += HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired += HandleSkillEffectExpired;
            eventNotifier.WinChecked += HandleWinChecked;
            eventNotifier.WinDeclared += HandleWinDeclared;
            eventNotifier.WinDeclined += HandleWinDeclined;
            eventNotifier.ReachDecisionStarted += HandleReachDecisionStarted;
            eventNotifier.SelfKanDecisionStarted += HandleSelfKanDecisionStarted;
            eventNotifier.SelfKanDecisionDeclined += HandleSelfKanDecisionDeclined;
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

            eventNotifier.RunStarted -= HandleRunStarted;
            eventNotifier.RoundStarted -= HandleRoundStarted;
            eventNotifier.RoundSetupCompleted -= HandleRoundSetupCompleted;
            eventNotifier.TurnStarted -= HandleTurnStarted;
            eventNotifier.TileDrawn -= HandleTileDrawn;
            eventNotifier.TileDiscarded -= HandleTileDiscarded;
            eventNotifier.ReactionWindowStarted -= HandleReactionWindowChanged;
            eventNotifier.ReactionWindowAnswered -= HandleReactionWindowAnswered;
            eventNotifier.ReactionWindowResolved -= HandleReactionWindowResolved;
            eventNotifier.ReactionWindowClosed -= HandleReactionWindowClosed;
            eventNotifier.MeldDeclared -= HandleMeldDeclared;
            eventNotifier.SkillActivated -= HandleSkillActivated;
            eventNotifier.SkillReserved -= HandleSkillReserved;
            eventNotifier.SkillEffectRegistered -= HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved -= HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired -= HandleSkillEffectExpired;
            eventNotifier.WinChecked -= HandleWinChecked;
            eventNotifier.WinDeclared -= HandleWinDeclared;
            eventNotifier.WinDeclined -= HandleWinDeclined;
            eventNotifier.ReachDecisionStarted -= HandleReachDecisionStarted;
            eventNotifier.SelfKanDecisionStarted -= HandleSelfKanDecisionStarted;
            eventNotifier.SelfKanDecisionDeclined -= HandleSelfKanDecisionDeclined;
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

        private void EnsureWinningCandidateController()
        {
            if (winningCandidateController == null)
            {
                winningCandidateController =
                    GetComponentInChildren<MahjongWinningCandidateController>(true);
            }
        }

        private void EnsureRoundProgressController()
        {
            if (roundProgressController == null)
            {
                roundProgressController = GetComponentInChildren<MahjongRoundProgressController>(true);
            }

            if (roundProgressController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingRoundProgressController,
                    "MahjongRoundProgressController is not assigned. Round-start presentation will not be shown.");
            }
        }

        private void SubscribeRoundProgressPresentation()
        {
            if (subscribedRoundProgressController == roundProgressController)
                return;

            UnsubscribeRoundProgressPresentation();
            if (roundProgressController == null)
                return;

            subscribedRoundProgressController = roundProgressController;
            subscribedRoundProgressController.PresentationCompleted +=
                HandleRoundProgressPresentationCompleted;
        }

        private void UnsubscribeRoundProgressPresentation()
        {
            if (subscribedRoundProgressController == null)
                return;

            subscribedRoundProgressController.PresentationCompleted -=
                HandleRoundProgressPresentationCompleted;
            subscribedRoundProgressController = null;
        }

        private void SubscribeTileHoverPresentation()
        {
            if (subscribedTileHoverPresenter == playerArea3DPresenter)
                return;

            UnsubscribeTileHoverPresentation();
            if (playerArea3DPresenter == null)
                return;

            subscribedTileHoverPresenter = playerArea3DPresenter;
            subscribedTileHoverPresenter.HandTileClicked += HandleHandTileClicked;
            subscribedTileHoverPresenter.DrawnTileClicked += HandleDrawnTileClicked;
            subscribedTileHoverPresenter.TileHoverEntered += HandleTileHoverEntered;
            subscribedTileHoverPresenter.TileHoverExited += HandleTileHoverExited;
        }

        private void UnsubscribeTileHoverPresentation()
        {
            if (subscribedTileHoverPresenter == null)
                return;

            subscribedTileHoverPresenter.HandTileClicked -= HandleHandTileClicked;
            subscribedTileHoverPresenter.DrawnTileClicked -= HandleDrawnTileClicked;
            subscribedTileHoverPresenter.TileHoverEntered -= HandleTileHoverEntered;
            subscribedTileHoverPresenter.TileHoverExited -= HandleTileHoverExited;
            subscribedTileHoverPresenter = null;
        }

        private void SubscribeTileHoverReevaluation()
        {
            if (subscribedTileRaycastInput == tileRaycastInput)
                return;

            UnsubscribeTileHoverReevaluation();
            if (tileRaycastInput == null)
                return;

            subscribedTileRaycastInput = tileRaycastInput;
            subscribedTileRaycastInput.HoverReevaluated += HandleTileHoverReevaluated;
            subscribedTileRaycastInput.TableInputSurfaceClicked += HandleTableInputSurfaceClicked;
        }

        private void UnsubscribeTileHoverReevaluation()
        {
            if (subscribedTileRaycastInput == null)
                return;

            subscribedTileRaycastInput.HoverReevaluated -= HandleTileHoverReevaluated;
            subscribedTileRaycastInput.TableInputSurfaceClicked -= HandleTableInputSurfaceClicked;
            subscribedTileRaycastInput = null;
        }

        private void HandleHandTileClicked(SeatId dataSeat, int handIndex, Tile tile)
        {
            HandleSelfTileClicked(dataSeat, DiscardSource.Hand, handIndex, tile);
        }

        private void HandleDrawnTileClicked(SeatId dataSeat, Tile tile)
        {
            HandleSelfTileClicked(dataSeat, DiscardSource.DrawnTile, -1, tile);
        }

        private void HandleTableInputSurfaceClicked()
        {
            ClearTileSelection(true, true);
        }

        private void HandleSelfTileClicked(
            SeatId dataSeat,
            DiscardSource source,
            int handIndex,
            Tile tile)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || gameFlow.ViewContext == null ||
                !TryGetSelfSeat(state, out SeatId selfSeat) ||
                dataSeat != selfSeat)
            {
                return;
            }

            TileSelectionIdentity clickedTile = new TileSelectionIdentity(
                gameFlow.ViewContext.LocalPlayerId,
                selfSeat,
                state.TurnIndex,
                source,
                handIndex,
                tile);

            if (selectedSelfTile.HasValue &&
                !IsTileSelectionCurrentAndSelectable(state, selectedSelfTile.Value))
            {
                ClearTileSelection(true, false);
            }

            if (!IsTileSelectionCurrentAndSelectable(state, clickedTile))
                return;

            if (!selectedSelfTile.HasValue || !selectedSelfTile.Value.Equals(clickedTile))
            {
                selectedSelfTile = clickedTile;
                ApplyTileSelectionVisual(clickedTile);
                RefreshReachDecision(state);
                return;
            }

            TryConfirmSelectedTileDiscard(clickedTile);
        }

        private void TryConfirmSelectedTileDiscard(TileSelectionIdentity selection)
        {
            if (discardCommandInProgress)
                return;

            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (!selectedSelfTile.HasValue ||
                !selectedSelfTile.Value.Equals(selection) ||
                !IsTileSelectionCurrentAndSelectable(state, selection))
            {
                ClearTileSelection(true, true);
                return;
            }

            if (commandRouter == null)
                EnsureCommandRouter();
            if (commandRouter == null)
                return;

            discardCommandInProgress = true;
            try
            {
                bool accepted = selection.Source == DiscardSource.Hand
                    ? commandRouter.TryDiscardHandFromTileSelection(
                        selection.SeatId,
                        selection.HandIndex)
                    : commandRouter.TryDiscardDrawnTileFromTileSelection();

                if (accepted && selectedSelfTile.HasValue &&
                    selectedSelfTile.Value.Equals(selection))
                {
                    // The accepted discard redraw removes the selected visual.
                    // Do not play a deselection transition before that redraw.
                    ClearTileSelection(false, true);
                }
            }
            finally
            {
                discardCommandInProgress = false;
            }
        }

        private bool IsTileSelectionCurrentAndSelectable(
            MahjongGameState state,
            TileSelectionIdentity selection)
        {
            if (state == null || state.IsRoundEnded || state.IsGameEnded ||
                gameFlow == null || gameFlow.ViewContext == null ||
                selection.PlayerId != gameFlow.ViewContext.LocalPlayerId ||
                selection.TurnIndex != state.TurnIndex ||
                !TryGetSelfSeat(state, out SeatId selfSeat) ||
                selection.SeatId != selfSeat ||
                !CanUseSelfGameplayInput(state) ||
                !TryGetCurrentHoveredTile(
                    state,
                    selfSeat,
                    selection.ToTileInfo(),
                    out PlayerSeat player))
            {
                return false;
            }

            if (state.IsReachDiscardSelectionPending)
            {
                return state.ReachDecisionSeat == selfSeat &&
                    TryFindReachDiscardCandidate(
                        state.ReachDiscardCandidates,
                        selection.ToTileInfo(),
                        out _);
            }

            return !player.IsReachDeclared || selection.Source == DiscardSource.DrawnTile;
        }

        private void ApplyTileSelectionVisual(TileSelectionIdentity selection)
        {
            if (playerArea3DPresenter == null)
                return;

            if (selection.Source == DiscardSource.Hand)
                playerArea3DPresenter.SetSelfSelectedHandTile(selection.HandIndex);
            else
                playerArea3DPresenter.SetSelfDrawnTileSelected(true);
        }

        private void ClearTileSelection(bool clearVisual, bool refreshWinningCandidates)
        {
            bool hadSelection = selectedSelfTile.HasValue;
            selectedSelfTile = null;

            if (clearVisual && playerArea3DPresenter != null)
                playerArea3DPresenter.ClearSelfTileSelectionVisual();

            if (!refreshWinningCandidates || (!hadSelection && !clearVisual))
                return;

            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            RefreshReachDecision(state);
        }

        private void HandleTileHoverEntered(Mahjong3DTileHoverInfo hoverInfo)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || state.IsRoundEnded || state.IsGameEnded)
            {
                ClearTileHoverState();
                return;
            }

            if (!TryGetSelfSeat(state, out SeatId selfSeat) ||
                hoverInfo.SeatId != selfSeat ||
                !hoverInfo.Tile.IsValid)
            {
                return;
            }

            hoveredSelfTile = hoverInfo;
            pendingTileHoverExit = null;
            tileHoverReevaluationPending = false;
            RefreshReachDecision(state);
        }

        private void HandleTileHoverExited(Mahjong3DTileHoverInfo hoverInfo)
        {
            if (!hoveredSelfTile.HasValue ||
                !hoveredSelfTile.Value.Equals(hoverInfo))
            {
                return;
            }

            pendingTileHoverExit = hoverInfo;
            tileHoverReevaluationPending = true;
        }

        private void HandleTileHoverReevaluated()
        {
            if (!tileHoverReevaluationPending && !pendingTileHoverExit.HasValue)
                return;

            if (pendingTileHoverExit.HasValue &&
                hoveredSelfTile.HasValue &&
                hoveredSelfTile.Value.Equals(pendingTileHoverExit.Value))
            {
                hoveredSelfTile = null;
            }

            pendingTileHoverExit = null;
            tileHoverReevaluationPending = false;
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            RefreshReachDecision(state);
        }

        private void RequestTileHoverReevaluation()
        {
            if (!hoveredSelfTile.HasValue && !pendingTileHoverExit.HasValue)
                return;

            tileHoverReevaluationPending = true;
            tileRaycastInput?.RequestHoverRefresh();
        }

        private void ClearTileHoverState()
        {
            hoveredSelfTile = null;
            pendingTileHoverExit = null;
            tileHoverReevaluationPending = false;
        }

        private void HandleRunStarted(string _)
        {
            if (roundProgressController == null)
                EnsureRoundProgressController();

            roundProgressController?.ResetPlaybackHistory();
        }

        private void HandleRoundStarted(int _, int __)
        {
            ClearTileHoverState();
            ClearTileSelection(true, false);
            PlayRoundProgressForCurrentState();
            RefreshFromFlow(false);
            ClearZeroHanTenpaiUi();
            ClearFuritenUi();
        }

        private void HandleRoundSetupCompleted()
        {
            RefreshFromFlow();
        }

        private void PlayRoundProgressForCurrentState()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            if (roundProgressController == null)
                EnsureRoundProgressController();

            if (roundProgressController == null)
            {
                gameFlow?.NotifyRoundProgressCompleted(state.WindProgress);
                return;
            }

            SubscribeRoundProgressPresentation();
            if (roundProgressController.TryPlay(state.WindProgress, state.SelfSeat))
            {
                gameFlow?.NotifyRoundProgressPlaybackStarted(state.WindProgress);
                return;
            }

            gameFlow?.NotifyRoundProgressCompleted(state.WindProgress);
        }

        private void HandleRoundProgressPresentationCompleted(WindProgress progress)
        {
            gameFlow?.NotifyRoundProgressCompleted(progress);
        }

        private void NotifyRoundProgressFallback()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                gameFlow.NotifyRoundProgressCompleted(state.WindProgress);
        }

        private void HandleTurnStarted(SeatId _, int __)
        {
            ClearTileSelection(true, false);
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleTileDrawn(DrawResult result)
        {
            RequestTileHoverReevaluation();
            if (!result.Success || result.Purpose == DrawPurpose.InitialDeal)
            {
                RefreshReachDecisionUi();
                return;
            }

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
            RequestTileHoverReevaluation();
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
            resolvedReactionWindowIdAwaitingClosed = null;
            RefreshDiscardRiversForReactionHighlight();
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReactionWindowAnswered(ReactionWindowAnswerResult _)
        {
            RefreshDiscardRiversForReactionHighlight();
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReactionWindowResolved(ReactionWindowResolution resolution)
        {
            if ((resolution.Type == ReactionWindowResolutionType.PonDeclared ||
                 resolution.Type == ReactionWindowResolutionType.ChiDeclared ||
                 resolution.Type == ReactionWindowResolutionType.DaiminkanDeclared) &&
                resolution.Candidate != null)
            {
                RefreshPlayerHandForSeat(resolution.Candidate.Seat);
                RefreshPlayerOpenMeldsForSeat(resolution.Candidate.Seat);
            }

            RefreshDiscardRiversForReactionHighlight();
            resolvedReactionWindowIdAwaitingClosed = resolution.WindowId;
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleMeldDeclared(PlayerMeld meld)
        {
            RequestTileHoverReevaluation();
            if (meld == null)
                return;

            RefreshPlayerHandForSeat(meld.OwnerSeat);
            RefreshPlayerDrawnTileForSeat(meld.OwnerSeat);
            RefreshPlayerOpenMeldsForSeat(meld.OwnerSeat);
            RefreshGlobalStatus();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReactionWindowClosed(int windowId)
        {
            if (resolvedReactionWindowIdAwaitingClosed == windowId)
                resolvedReactionWindowIdAwaitingClosed = null;
            else
                RefreshDiscardRiversForReactionHighlight();

            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleSkillActivated(SeatId _, ActiveSkillEffect __)
        {
            RefreshGlobalStatus();
            RefreshInteractionUi();
        }

        private void HandleSkillReserved(PendingSkillReservation _)
        {
            RefreshInteractionUi();
        }

        private void HandleSkillEffectRegistered(ActiveSkillEffect _)
        {
            RefreshGlobalStatus();
            RefreshInteractionUi();
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
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshPonDecisionUi();
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
            RefreshPonDecisionUi();
            RefreshReachDecisionUi();
            RefreshInteractionUi();

            if (IsSelfSeat(seat))
                RefreshFuritenUi();
        }

        private void HandleReachDecisionStarted(SeatId _, int __)
        {
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleSelfKanDecisionStarted(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshPonDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleSelfKanDecisionDeclined(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshPonDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDiscardSelectionStarted(SeatId _, int __)
        {
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDiscardSelectionCanceled(SeatId _, int __)
        {
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDeclared(SeatId _, int __)
        {
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleReachDeclined(SeatId _, int __)
        {
            RequestTileHoverReevaluation();
            RefreshGlobalStatus();
            RefreshReachDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleHandAutoSorted(SeatId seat, int _)
        {
            RequestTileHoverReevaluation();
            RefreshPlayerHandForSeat(seat);
            RefreshReachDecisionUi();
        }

        private void HandleRoundEnded(string _)
        {
            ClearTileHoverState();
            ClearTileSelection(true, false);
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
            ClearTileHoverState();
            ClearTileSelection(true, false);
            ClearRoundResultUi();
            RefreshReachDecisionUi();
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

            ClearTileSelection(false, false);
            if (tileRaycastInput == null)
            {
                tileRaycastInput =
                    playerArea3DPresenter.GetComponent<Mahjong3DTileRaycastInput>();
            }
            SubscribeTileHoverPresentation();
            SubscribeTileHoverReevaluation();
            if (gameFlow != null)
                playerArea3DPresenter.SetViewContext(gameFlow.ViewContext);
            reactionHighlightDiscardId = ResolveReactionHighlightDiscardId(state);
            playerArea3DPresenter.Refresh(
                state,
                CanUseSelfGameplayInput(state),
                reactionHighlightDiscardId);
        }

        private void RefreshTableCenterUi(MahjongGameState state)
        {
            if (tableCenterUiController != null)
            {
                if (gameFlow != null)
                    tableCenterUiController.SetViewContext(gameFlow.ViewContext);
                tableCenterUiController.Refresh(state);
            }
        }

        private void RefreshPlayerHand3DForSeat(MahjongGameState state, SeatId seat)
        {
            if (playerArea3DPresenter == null)
                return;

            if (IsSelfSeat(seat))
                ClearTileSelection(false, false);
            ConfigurePresentationViewContext();
            playerArea3DPresenter.RefreshHandForSeat(state, seat, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerDrawnTile3DForSeat(MahjongGameState state, SeatId seat)
        {
            if (playerArea3DPresenter == null)
                return;

            if (IsSelfSeat(seat))
                ClearTileSelection(false, false);
            ConfigurePresentationViewContext();
            playerArea3DPresenter.RefreshDrawnTileForSeat(state, seat, CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerDiscardRiver3DForSeat(MahjongGameState state, SeatId seat)
        {
            if (playerArea3DPresenter == null)
                return;

            ConfigurePresentationViewContext();
            reactionHighlightDiscardId = ResolveReactionHighlightDiscardId(state);
            playerArea3DPresenter.RefreshDiscardRiverForSeat(
                state,
                seat,
                reactionHighlightDiscardId);
        }

        private void RefreshDiscardRiversForReactionHighlight()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
            {
                reactionHighlightDiscardId = null;
                ClearDiscardReactionHighlights();
                return;
            }

            reactionHighlightDiscardId = ResolveReactionHighlightDiscardId(state);
            if (playerArea3DPresenter == null)
                return;

            ConfigurePresentationViewContext();
            playerArea3DPresenter.RefreshDiscardRiver(state, reactionHighlightDiscardId);
        }

        private void ClearDiscardReactionHighlights()
        {
            if (playerArea3DPresenter != null)
                playerArea3DPresenter.ClearDiscardReactionHighlights();
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

            ConfigurePresentationViewContext();
            playerArea3DPresenter.RefreshOpenMeldsForSeat(state, seat);
        }

        private void ConfigurePresentationViewContext()
        {
            if (gameFlow != null && playerArea3DPresenter != null)
                playerArea3DPresenter.SetViewContext(gameFlow.ViewContext);
        }

        private void RefreshWinDecision(MahjongGameState state)
        {
            if (winDecisionController == null)
                EnsureWinDecisionController();

            if (winDecisionController == null)
            {
                inputController?.ClearReactionResponseBindings();
                inputController?.ClearWinDecisionResponseBindings();
                return;
            }

            if (TryGetSelfReactionDecisionRequest(state, out DecisionRequest request))
            {
                inputController?.ClearWinDecisionResponseBindings();
                ReactionDecisionRequest reaction = request.Reaction;
                bool showRon = reaction.Allows(ReactionWindowSeatAnswerKind.Ron);
                inputController?.SetReactionResponseBindings(
                    request.RequestId,
                    reaction.WindowId,
                    showRon,
                    reaction.Allows(ReactionWindowSeatAnswerKind.Pon),
                    !showRon &&
                    (reaction.Allows(ReactionWindowSeatAnswerKind.Pon) ||
                     reaction.Allows(ReactionWindowSeatAnswerKind.Daiminkan) ||
                     reaction.Allows(ReactionWindowSeatAnswerKind.Chi)));
                winDecisionController.SetWinDecision(
                    showRon,
                    showRon ? WinType.Ron : null);
                return;
            }

            inputController?.ClearReactionResponseBindings();
            if (TryGetSelfDecisionRequest(
                    state,
                    DecisionKind.WinDeclaration,
                    out DecisionRequest winRequest) &&
                winRequest.WinDeclaration != null)
            {
                inputController?.SetWinDecisionResponseBindings(winRequest.RequestId);
                winDecisionController.SetWinDecision(
                    true,
                    winRequest.WinDeclaration.WinType);
                return;
            }

            inputController?.ClearWinDecisionResponseBindings();
            winDecisionController.SetWinDecision(false, null);
        }

        private void RefreshWinDecisionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshWinDecision(state);
            else
            {
                inputController?.ClearReactionResponseBindings();
                inputController?.ClearWinDecisionResponseBindings();
            }
        }

        private void RefreshPonDecision(MahjongGameState state)
        {
            if (ponDecisionController == null)
                EnsurePonDecisionController();

            if (ponDecisionController == null)
                return;

            if (state == null)
            {
                ponDecisionController.SetMeldCallDecision(
                    false,
                    false,
                    null,
                    null,
                    null,
                    false,
                    null);
                return;
            }

            if (!TryGetSelfSeat(state, out SeatId selfSeat))
            {
                ponDecisionController.SetMeldCallDecision(
                    false,
                    false,
                    null,
                    null,
                    null,
                    false,
                    null);
                return;
            }

            if (TryGetSelfReactionDecisionRequest(state, out DecisionRequest request))
            {
                ReactionDecisionRequest reaction = request.Reaction;
                bool showRon = reaction.Allows(ReactionWindowSeatAnswerKind.Ron);
                bool showPon = reaction.Allows(ReactionWindowSeatAnswerKind.Pon);
                bool showDaiminkan = reaction.Allows(
                    ReactionWindowSeatAnswerKind.Daiminkan);
                IReadOnlyList<ChiOption> chiOptions = CreateReactionChiOptions(reaction);
                ponDecisionController.SetReactionMeldCallDecision(
                    request.RequestId,
                    reaction.WindowId,
                    showPon,
                    showDaiminkan,
                    chiOptions,
                    reaction.SourceTile,
                    !showRon);
                return;
            }

            if (TryGetSelfDecisionRequest(
                    state,
                    DecisionKind.SelfKan,
                    out DecisionRequest selfKanRequest) &&
                selfKanRequest.SelfKan != null)
            {
                ponDecisionController.SetSelfKanDecision(
                    selfKanRequest.RequestId,
                    selfKanRequest.SelfKan);
                return;
            }

            // The normal local UI must not derive reaction choices from a
            // mutable window. If a request is no longer pending, hide the
            // reaction controls until the existing lifecycle closes it.
            if (state.IsReactionWindowPending)
            {
                ponDecisionController.SetMeldCallDecision(
                    false,
                    false,
                    null,
                    null,
                    null,
                    false,
                    null);
                return;
            }

            ponDecisionController.SetMeldCallDecision(
                false,
                false,
                null,
                null,
                null,
                false,
                null);
        }

        private static IReadOnlyList<ChiOption> CreateReactionChiOptions(
            ReactionDecisionRequest reaction)
        {
            if (reaction == null ||
                !reaction.Allows(ReactionWindowSeatAnswerKind.Chi))
            {
                return null;
            }

            IReadOnlyList<ReactionDecisionChiOption> sourceOptions =
                reaction.GetChiOptions();
            List<ChiOption> options = new List<ChiOption>(sourceOptions.Count);
            for (int i = 0; i < sourceOptions.Count; i++)
            {
                ReactionDecisionChiOption option = sourceOptions[i];
                if (option == null)
                    continue;

                options.Add(new ChiOption(
                    option.OptionId,
                    reaction.SourceTile,
                    option.HandTiles,
                    option.MeldTiles));
            }

            return options;
        }

        private void RefreshPonDecisionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            RefreshPonDecision(state);
        }

        private void RefreshReachDecision(MahjongGameState state)
        {
            if (state == null || state.IsRoundEnded || state.IsGameEnded)
                ClearTileHoverState();

            if (reachDecisionController == null)
                EnsureReachDecisionController();

            if (winningCandidateController == null)
                EnsureWinningCandidateController();

            bool showSelfReachDecision = TryGetSelfDecisionRequest(
                state,
                DecisionKind.Reach,
                out DecisionRequest reachRequest);
            if (showSelfReachDecision)
                inputController?.SetReachDecisionResponseBindings(reachRequest.RequestId);
            else
                inputController?.ClearReachDecisionResponseBindings();

            bool showSelfReachCancel =
                state != null &&
                state.IsReachDiscardSelectionPending &&
                IsSelfSeat(state.ReachDecisionSeat);
            if (reachDecisionController != null)
                reachDecisionController.SetReachUiVisible(showSelfReachDecision, showSelfReachCancel);

            RefreshWinningCandidates(state, showSelfReachDecision, showSelfReachCancel);
        }

        private void RefreshWinningCandidates(
            MahjongGameState state,
            bool showSelfReachDecision,
            bool showSelfReachCancel)
        {
            if (selectedSelfTile.HasValue)
            {
                TileSelectionIdentity selection = selectedSelfTile.Value;
                if (!IsTileSelectionCurrentAndSelectable(state, selection))
                {
                    ClearTileSelection(true, false);
                }
                else
                {
                    IReadOnlyList<WinningTileCandidate> selectedCandidates =
                        EvaluateHoveredTile(
                            state,
                            selection.ToTileInfo(),
                            showSelfReachDecision,
                            showSelfReachCancel);
                    if (selectedCandidates.Count > 0)
                        winningCandidateController?.SetCandidates(selectedCandidates);
                    else
                        winningCandidateController?.Clear();
                    return;
                }
            }

            if (tileHoverReevaluationPending)
                return;

            if (hoveredSelfTile.HasValue)
            {
                IReadOnlyList<WinningTileCandidate> candidates = EvaluateHoveredTile(
                    state,
                    hoveredSelfTile.Value,
                    showSelfReachDecision,
                    showSelfReachCancel);
                if (candidates.Count > 0)
                    winningCandidateController?.SetCandidates(candidates);
                else
                    winningCandidateController?.Clear();
                return;
            }

            if (!showSelfReachDecision ||
                !TryGetSelfSeat(state, out SeatId selfSeat))
            {
                winningCandidateController?.Clear();
                return;
            }

            IReadOnlyList<ReachWinningCandidateGroup> groups =
                winningTileCandidateEvaluator.GroupReachCandidates(
                    state,
                    selfSeat,
                    state.ReachDiscardCandidates);
            winningCandidateController?.SetGroups(groups);
        }

        private IReadOnlyList<WinningTileCandidate> EvaluateHoveredTile(
            MahjongGameState state,
            Mahjong3DTileHoverInfo hoverInfo,
            bool showSelfReachDecision,
            bool showSelfReachCancel)
        {
            if (!TryGetSelfSeat(state, out SeatId selfSeat) ||
                hoverInfo.SeatId != selfSeat ||
                !TryGetCurrentHoveredTile(state, selfSeat, hoverInfo, out PlayerSeat player))
            {
                return System.Array.Empty<WinningTileCandidate>();
            }

            if (showSelfReachDecision || showSelfReachCancel)
            {
                if (!TryFindReachDiscardCandidate(
                        state.ReachDiscardCandidates,
                        hoverInfo,
                        out ReachDiscardCandidate reachCandidate))
                {
                    return System.Array.Empty<WinningTileCandidate>();
                }

                return winningTileCandidateEvaluator.EvaluateAfterDiscard(
                    state,
                    selfSeat,
                    reachCandidate);
            }

            if (player.IsReachDeclared)
                return winningTileCandidateEvaluator.EvaluateCurrentHand(state, selfSeat);

            if (player.HasDrawnTile)
            {
                ReachDiscardCandidate discardCandidate = new ReachDiscardCandidate(
                    hoverInfo.Source,
                    hoverInfo.HandIndex,
                    hoverInfo.Tile);
                return winningTileCandidateEvaluator.EvaluateAfterDiscard(
                    state,
                    selfSeat,
                    discardCandidate);
            }

            if (hoverInfo.Source != DiscardSource.Hand)
                return System.Array.Empty<WinningTileCandidate>();

            return winningTileCandidateEvaluator.EvaluateCurrentHand(state, selfSeat);
        }

        private static bool TryGetCurrentHoveredTile(
            MahjongGameState state,
            SeatId selfSeat,
            Mahjong3DTileHoverInfo hoverInfo,
            out PlayerSeat player)
        {
            player = state != null ? state.GetPlayerSeat(selfSeat) : null;
            if (player == null || !hoverInfo.Tile.IsValid)
                return false;

            if (hoverInfo.Source == DiscardSource.DrawnTile)
            {
                return hoverInfo.HandIndex == -1 &&
                    player.DrawnTile.HasValue &&
                    player.DrawnTile.Value == hoverInfo.Tile;
            }

            if (hoverInfo.Source != DiscardSource.Hand)
                return false;

            IReadOnlyList<Tile> handTiles = player.Hand.GetTiles();
            return hoverInfo.HandIndex >= 0 &&
                hoverInfo.HandIndex < handTiles.Count &&
                handTiles[hoverInfo.HandIndex] == hoverInfo.Tile;
        }

        private static bool TryFindReachDiscardCandidate(
            IReadOnlyList<ReachDiscardCandidate> candidates,
            Mahjong3DTileHoverInfo hoverInfo,
            out ReachDiscardCandidate match)
        {
            match = default;
            if (candidates == null)
                return false;

            for (int i = 0; i < candidates.Count; i++)
            {
                ReachDiscardCandidate candidate = candidates[i];
                if (candidate.Source != hoverInfo.Source ||
                    candidate.HandIndex != hoverInfo.HandIndex ||
                    candidate.Tile != hoverInfo.Tile)
                {
                    continue;
                }

                match = candidate;
                return true;
            }

            return false;
        }

        private void RefreshReachDecisionUi()
        {
            RequestTileHoverReevaluation();
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
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
            bool canUseDrawInput = CanUseDrawInput(state);
            bool canUseForceDrawSkillInput = CanUseForceDrawSkillInput(state);
            bool canUseAutoSortInput = CanUseAutoSortInput(state);

            if (inputController != null)
            {
                inputController.SetDrawButtonInteractable(canUseDrawInput);
                inputController.SetForceDrawSkillButtonInteractable(canUseForceDrawSkillInput);
                inputController.SetAutoSortInteractable(canUseAutoSortInput);
            }

            if (playerArea3DPresenter != null)
                playerArea3DPresenter.SetSelfInteractable(state, canUseSelfTileInput);

            ApplyReachDiscardCandidateInteractable(state);
            ApplyDeclaredReachInteractable(state, canUseSelfTileInput);

            if (selectedSelfTile.HasValue &&
                (!canUseSelfTileInput ||
                 !IsTileSelectionCurrentAndSelectable(state, selectedSelfTile.Value)))
            {
                ClearTileSelection(true, true);
            }
        }

        private void RefreshInteractionUi()
        {
            RequestTileHoverReevaluation();
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshInteractionState(state);
        }

        private bool CanUseSelfGameplayInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                TryGetSelfSeat(state, out SeatId selfSeat) &&
                gameFlow.CanAcceptTileDiscardIntentForSeat(selfSeat);
        }

        private bool CanUseDrawInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                TryGetSelfSeat(state, out SeatId selfSeat) &&
                state.CurrentTurn == selfSeat &&
                !gameFlow.IsInteractionLocked &&
                !state.IsRoundEnded &&
                !state.IsWinDecisionPending &&
                !state.IsReachDecisionPending &&
                !state.IsReachDiscardSelectionPending &&
                !state.GetPlayerSeat(selfSeat).HasDrawnTile &&
                (state.TurnPhase == TurnPhase.WaitingForDraw ||
                    state.TurnPhase == TurnPhase.WaitingForRinshanDraw);
        }

        private bool CanUseForceDrawSkillInput(MahjongGameState state)
        {
            return gameFlow != null &&
                TryGetSelfSeat(state, out SeatId selfSeat) &&
                gameFlow.CanRequestForceDrawSkillForSeat(selfSeat);
        }

        private static bool CanUseAutoSortInput(MahjongGameState state)
        {
            return state != null &&
                !state.IsReachDecisionPending &&
                !state.IsReachDiscardSelectionPending;
        }

        private bool IsSelfSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            return state != null && TryGetSelfSeat(state, out SeatId selfSeat) &&
                selfSeat == seat;
        }

        private bool TryGetSelfSeat(MahjongGameState state, out SeatId selfSeat)
        {
            selfSeat = default;
            return state != null &&
                gameFlow != null &&
                gameFlow.ViewContext != null &&
                gameFlow.ViewContext.TryGetSelfSeat(state, out selfSeat);
        }

        private bool TryGetSelfReactionDecisionRequest(
            MahjongGameState state,
            out DecisionRequest request)
        {
            request = null;
            if (state == null || gameFlow == null || gameFlow.ViewContext == null ||
                !TryGetSelfSeat(state, out SeatId selfSeat) ||
                !gameFlow.TryGetPendingReactionDecisionRequest(
                    gameFlow.ViewContext.LocalPlayerId,
                    out DecisionRequest pending) ||
                pending.Kind != DecisionKind.Reaction || pending.Reaction == null ||
                pending.PlayerId != gameFlow.ViewContext.LocalPlayerId ||
                pending.ActorSeat != selfSeat)
            {
                return false;
            }

            request = pending;
            return true;
        }

        private int? ResolveReactionHighlightDiscardId(MahjongGameState state)
        {
            if (!TryGetSelfReactionDecisionRequest(state, out DecisionRequest request))
                return null;

            return ResolveReactionHighlightDiscardId(state.Discards, request.Reaction);
        }

        private static int? ResolveReactionHighlightDiscardId(
            IReadOnlyList<DiscardRecord> discards,
            ReactionDecisionRequest reaction)
        {
            if (discards == null || reaction == null ||
                reaction.SourceKind != ReactionWindowSourceKind.Discard ||
                (!reaction.Allows(ReactionWindowSeatAnswerKind.Pon) &&
                 !reaction.Allows(ReactionWindowSeatAnswerKind.Chi) &&
                 !reaction.Allows(ReactionWindowSeatAnswerKind.Daiminkan)))
            {
                return null;
            }

            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                if (record.ActorSeat == reaction.SourceSeat &&
                    record.Tile == reaction.SourceTile &&
                    record.TurnIndex == reaction.SourceTurnIndex)
                {
                    return record.Id;
                }
            }

            return null;
        }

        private bool TryGetSelfDecisionRequest(
            MahjongGameState state,
            DecisionKind kind,
            out DecisionRequest request)
        {
            request = null;
            if (state == null || gameFlow == null || gameFlow.ViewContext == null ||
                !TryGetSelfSeat(state, out SeatId selfSeat) ||
                !gameFlow.TryGetPendingDecisionRequest(
                    gameFlow.ViewContext.LocalPlayerId,
                    kind,
                    out DecisionRequest pending) ||
                pending.Kind != kind ||
                pending.PlayerId != gameFlow.ViewContext.LocalPlayerId ||
                pending.ActorSeat != selfSeat ||
                pending.TurnIndex != state.TurnIndex)
            {
                return false;
            }

            request = pending;
            return true;
        }

        private void ApplyReachDiscardCandidateInteractable(MahjongGameState state)
        {
            if (state == null ||
                !state.IsReachDiscardSelectionPending ||
                !TryGetSelfSeat(state, out SeatId selfSeat) ||
                state.ReachDecisionSeat != selfSeat)
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

            if (!TryGetSelfSeat(state, out SeatId selfSeat))
                return;

            PlayerSeat selfPlayerSeat = state.GetPlayerSeat(selfSeat);
            if (!selfPlayerSeat.IsReachDeclared)
                return;

            int[] noHandIndices = new int[0];
            bool drawnTileInteractable = canUseGameplayInput;

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
                TryGetSelfSeat(state, out SeatId selfSeat) &&
                resultSet.TryGet(
                    selfSeat,
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
