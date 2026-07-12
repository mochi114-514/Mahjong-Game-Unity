using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Definitions;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Mahjong Game Flow")]
    public sealed class MahjongGameFlow : MonoBehaviour, ICpuTurnGateway
    {
        [Header("Prototype Players")]
        [SerializeField, Range(1, 4)] private int participantCount = 1;

        [Header("Self Seat")]
        [FormerlySerializedAs("randomizeSelfWind")]
        [Tooltip("有効な場合、開始時の自席をランダムに決定します。オフの場合はFixed Self Seatを使用します。")]
        [SerializeField] private bool randomizeSelfSeat = true;
        [Tooltip("Randomize Self Seatをオフにしている場合、この席を自席として開始します。")]
        [FormerlySerializedAs("fixedSelfWind")]
        [SerializeField] private SeatId fixedSelfSeat = SeatId.East;

        [Header("Round Setup")]
        [SerializeField, Min(1)] private int initialHandTileCount = 13;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool enableAutoDraw;
        [SerializeField] private bool useFixedRandomSeed = false;
        [SerializeField] private int fixedRandomSeed = 12345;

        [Header("Turn Automation")]
        [SerializeField, Min(0f)] private float autoDiscardDrawnTileDelaySeconds = 0.75f;

        [Header("Scene References")]
        [SerializeField] private MahjongEventNotifier eventNotifier;
        [SerializeField] private CpuTurnController cpuTurnController;

        [Header("Warnings")]
        [SerializeField] private bool logWarnings = true;

        [Header("Yaku Definitions")]
        [SerializeField] private YakuDefinitionCatalog yakuDefinitionCatalog;

        [Header("Hand Sort")]
        [SerializeField] private bool autoSortEnabled;

        private readonly PlayerTurnManager playerTurnManager = new PlayerTurnManager(new TurnOrderService());
        private readonly RoundStartingSeatResolver roundStartingSeatResolver =
            new RoundStartingSeatResolver();
        private readonly DrawService drawService = new DrawService();
        private readonly DiscardService discardService = new DiscardService();
        private readonly WinChecker winChecker = new WinChecker();
        private readonly ReachChecker reachChecker = new ReachChecker();
        private readonly SkillSystem skillSystem = new SkillSystem();
        private readonly SkillReservationService skillReservationService = new SkillReservationService();

        private HandEvaluator handEvaluator;
        private WinDeclarationEvaluator winDeclarationEvaluator;
        private NoYakuTenpaiEvaluator noYakuTenpaiEvaluator;
        private FuritenEvaluator furitenEvaluator;
        private YakuDefinitionCatalog initializedYakuDefinitionCatalog;
        private RoundSetupService roundSetupService;
        private RoundLifecycleService roundLifecycleService;
        private TurnFlowService turnFlowService;
        private WinDecisionService winDecisionService;
        private ReactionWindowService reactionWindowService;
        private ReachDecisionService reachDecisionService;
        private SkillFlowService skillFlowService;
        private HandAutoSortService handAutoSortService;
        private MahjongFlowEventPublisher eventPublisher;
        private MahjongGameState gameState;
        private AutoDiscardDrawnTileController autoDiscardDrawnTileController;

        public MahjongGameState CurrentState => gameState;
        public MahjongEventNotifier EventNotifier => eventNotifier;
        public bool IsWinDecisionPending => gameState != null && gameState.IsWinDecisionPending;
        public bool IsAutoSortEnabled => autoSortEnabled;
        public bool IsInteractionLocked => gameState != null && gameState.IsInteractionLocked;

        private MahjongFlowEventPublisher EventPublisher =>
            eventPublisher ??= new MahjongFlowEventPublisher(() => eventNotifier, Warn);

        bool ICpuTurnGateway.RequestDrawForCpu(SeatId seat)
        {
            return TryRequestDrawForSeat(seat);
        }

        bool ICpuTurnGateway.RequestDiscardDrawnTileForCpu(SeatId seat)
        {
            return TryRequestDiscardDrawnTileForSeat(seat);
        }

        bool ICpuTurnGateway.RequestDeclareWinForCpu(SeatId seat)
        {
            return TryRequestDeclareWinForSeat(seat);
        }

        bool ICpuTurnGateway.IsSameGameStateAndTurn(
            MahjongGameState expectedGameState,
            SeatId seat,
            int turnIndex)
        {
            return ReferenceEquals(gameState, expectedGameState) &&
                expectedGameState != null &&
                expectedGameState.CurrentTurn == seat &&
                expectedGameState.TurnIndex == turnIndex;
        }

        public FuritenEvaluationResultSet EvaluateAllFuriten()
        {
            InitializeEvaluators();
            return winDecisionService.EvaluateAllFuriten(gameState);
        }

        public NoYakuTenpaiEvaluationResult EvaluateSelfNoYakuTenpai()
        {
            InitializeEvaluators();

            if (gameState == null || yakuDefinitionCatalog == null ||
                noYakuTenpaiEvaluator == null)
            {
                return NoYakuTenpaiEvaluationResult.NotEvaluated;
            }
            return winDecisionService.EvaluateSelfNoYakuTenpai(gameState);
        }

        private void Reset()
        {
            CacheReferences();
            NormalizeParticipantCount();
        }

        private void Awake()
        {
            CacheReferences();
            EnsureCpuTurnController();
            EnsureAutoDiscardDrawnTileController();
            EnsureTurnFlowService();
            EnsureDecisionServices();
            EnsureSkillFlowService();
            EnsureHandAutoSortService();
            InitializeEvaluators();
            NormalizeParticipantCount();
        }

        private void Start()
        {
            if (autoStart)
                StartNewRound();
        }

        private void OnDisable()
        {
            CancelPendingAutoDiscardDrawnTile();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeParticipantCount();
            initialHandTileCount = Mathf.Max(1, initialHandTileCount);
            autoDiscardDrawnTileDelaySeconds = Mathf.Max(0f, autoDiscardDrawnTileDelaySeconds);
        }
#endif

        [ContextMenu("Prototype/Start New Round")]
        public void StartNewRound()
        {
            EnsureRoundLifecycleService();
            SeatId initialSelfSeat = ResolveSelfSeat();
            StartRound(roundLifecycleService.GetInitialWindProgress(), true, initialSelfSeat);
        }

        private void StartRound(
            WindProgress windProgress,
            bool notifyRunStarted,
            SeatId selfSeat)
        {
            CacheReferences();
            EnsureCpuTurnController();
            InitializeEvaluators();
            NormalizeParticipantCount();
            EnsureRoundSetupService();
            EnsureTurnFlowService();
            EnsureDecisionServices();
            EnsureSkillFlowService();
            EnsureHandAutoSortService();
            EnsureAutoDiscardDrawnTileController();
            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();
            handAutoSortService.ClearDeferred();
            skillFlowService.ClearReservations();
            if (notifyRunStarted)
                EventPublisher.NotifyRunStarted();

            int? seed = useFixedRandomSeed ? fixedRandomSeed : (int?)null;
            RoundSetupResult setupResult = roundSetupService.SetupRound(
                windProgress,
                seed,
                selfSeat,
                participantCount);
            gameState = setupResult.GameState;

            EventPublisher.NotifySeatSlotsAssigned();
            EventPublisher.NotifyRoundStarted(gameState.TurnIndex, gameState.Wall.Count);

            DealInitialHands();
            EventPublisher.NotifyRoundSetupCompleted();
            StartTurn(gameState.CurrentTurn, gameState.TurnIndex);
        }

        public void RetryPrototype()
        {
            // PROTOTYPE: Reset only the current flow state without reloading the scene.
            StartNewRound();
        }

        public void RequestAdvanceFromRoundResult()
        {
            EnsureRoundLifecycleService();
            RoundResult result = roundLifecycleService.GetPendingRoundResult(gameState);
            if (result == null)
                return;

            EventPublisher.NotifyRoundResultConfirmed(result);
            RoundLifecycleTransition transition =
                roundLifecycleService.AdvanceFromRoundResult(gameState);

            if (transition.Type == RoundLifecycleTransitionType.GameEnded)
            {
                EventPublisher.NotifyTurnDebug(
                    "GameEnded",
                    $"windProgress={result.WindProgress}",
                    seat: gameState.CurrentTurn,
                    turnIndex: gameState.TurnIndex);
                EventPublisher.NotifyGameEnded(result);
                return;
            }

            if (transition.Type == RoundLifecycleTransitionType.StartNextRound)
            {
                StartRound(
                    transition.NextWindProgress.Value,
                    false,
                    transition.NextSelfSeat.Value);
            }
        }

        public void RequestDraw()
        {
            if (!CanUseSelfTurnInput("DrawBlocked"))
                return;

            TryDrawForSeat(gameState.SelfSeat, "DrawCompleted", "DrawBlocked", true);
        }

        public bool TryRequestDrawForSeat(SeatId actorSeat)
        {
            if (!CanUseGameState())
                return false;

            return TryDrawForSeat(
                actorSeat,
                "DrawCompleted",
                "DrawBlocked",
                false);
        }

        private bool TryDrawForSeat(
            SeatId seat,
            string completedEventName,
            string blockedEventName,
            bool warnOnBlocked)
        {
            if (gameState.CurrentTurn != seat)
            {
                if (warnOnBlocked)
                    Warn("Only the current seat can draw.");

                NotifyTurnBlocked(blockedEventName, "NotCurrentTurn");
                return false;
            }

            if (gameState.IsRoundEnded)
            {
                if (warnOnBlocked)
                    Warn("Round already ended. Press Retry.");

                NotifyTurnBlocked(blockedEventName, "RoundEnded");
                return false;
            }

            if (gameState.IsWinDecisionPending)
            {
                if (warnOnBlocked)
                    Warn("Declare or decline win before drawing.");

                NotifyTurnBlocked(blockedEventName, "WinDecisionPending");
                return false;
            }

            if (gameState.IsReachDecisionPending || gameState.IsReachDiscardSelectionPending)
            {
                if (warnOnBlocked)
                    Warn("Resolve reach decision before drawing.");

                NotifyTurnBlocked(blockedEventName, "ReachDecisionPending");
                return false;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.HasDrawnTile)
            {
                if (warnOnBlocked)
                    Warn("Already drew this turn. Discard a tile first.");

                NotifyTurnBlocked(blockedEventName, "DrawnTileExists");
                return false;
            }

            if (gameState.TurnPhase != TurnPhase.WaitingForDraw)
            {
                if (warnOnBlocked)
                    Warn("Draw is not available in the current turn phase.");

                NotifyTurnBlocked(blockedEventName, "InvalidTurnPhase");
                return false;
            }

            DrawResult result = drawService.DrawTile(seat, gameState, DrawPurpose.TurnDraw);

            if (!result.Success)
            {
                NotifySkillResolutionEvents(result);
                EndRound(RoundLifecycleService.RoundEndReasonWallEmpty);
                return false;
            }

            RecordTurnDrawIfNeeded(result);
            playerSeat.SetDrawnTile(result.Tile);
            gameState.EnterWaitingForDiscard();
            playerSeat.ClearTemporaryFuriten();
            EventPublisher.NotifyTurnDebug(
                completedEventName,
                $"phase={gameState.TurnPhase}; drawnTile={result.Tile}",
                seat: seat,
                tile: result.Tile,
                turnIndex: gameState.TurnIndex);
            NotifySkillResolutionEvents(result);
            EventPublisher.NotifyTileDrawn(result);
            ResolveAfterDraw(seat);

            return true;
        }

        public void RequestDiscard(int handIndex)
        {
            if (!CanUseSelfTurnInput("DiscardBlocked"))
                return;

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                NotifyTurnBlocked("DiscardBlocked", "RoundEnded");
                return;
            }

            SeatId selfSeat = gameState.SelfSeat;
            PlayerSeat selfPlayerSeat = gameState.GetPlayerSeat(selfSeat);
            bool isPostCallDiscard =
                gameState.TurnPhase == TurnPhase.WaitingForDiscardAfterCall;
            if (!selfPlayerSeat.HasDrawnTile && !isPostCallDiscard)
            {
                Warn("Draw before discarding.");
                NotifyTurnBlocked("DiscardBlocked", "DrawnTileMissing");
                return;
            }

            if (gameState.IsWinDecisionPending)
            {
                Warn("Declare or decline win before discarding.");
                NotifyTurnBlocked("DiscardBlocked", "WinDecisionPending");
                return;
            }

            if (gameState.IsReachDecisionPending)
            {
                Warn("Declare or decline reach before discarding.");
                NotifyTurnBlocked("DiscardBlocked", "ReachDecisionPending");
                return;
            }

            if (gameState.TurnPhase != TurnPhase.WaitingForDiscard &&
                gameState.TurnPhase != TurnPhase.ReachDiscardSelection &&
                !isPostCallDiscard)
            {
                Warn("Discard is not available in the current turn phase.");
                NotifyTurnBlocked("DiscardBlocked", "InvalidTurnPhase");
                return;
            }

            if (!IsValidReachDiscardCandidate(selfSeat, DiscardSource.Hand, handIndex))
            {
                Warn("Only reach discard candidates can be discarded.");
                NotifyTurnBlocked("DiscardBlocked", "ReachDiscardCandidateMissing");
                return;
            }

            if (selfPlayerSeat.IsReachDeclared)
            {
                Warn("Hand discards are locked after reach.");
                NotifyTurnBlocked("DiscardBlocked", "ReachDeclaredHandLocked");
                return;
            }

            DiscardResult result = discardService.DiscardTile(gameState, selfSeat, handIndex);
            if (!result.Success)
            {
                Warn(result.Reason);
                return;
            }

            CommitDrawnTileToHandIfPresent(selfSeat);
            bool declaredReachNow = CompleteReachDeclarationIfPending(result.Record);
            gameState.EnterWaitingForDraw();
            ExpireIppatsuAfterDiscard(result.Record, declaredReachNow);
            CompleteDiscard(result.Record);
        }

        public void RequestDiscardDrawnTile()
        {
            if (!CanUseSelfTurnInput("DiscardBlocked"))
                return;

            TryRequestDiscardDrawnTileForSeatInternal(gameState.SelfSeat, true);
        }

        public bool TryRequestDiscardDrawnTileForSeat(SeatId actorSeat)
        {
            return TryRequestDiscardDrawnTileForSeatInternal(actorSeat, false);
        }

        private bool TryRequestDiscardDrawnTileForSeatInternal(
            SeatId actorSeat,
            bool warnOnBlocked)
        {
            if (!CanUseGameState())
                return false;

            if (gameState.IsRoundEnded)
            {
                if (warnOnBlocked)
                    Warn("Round already ended. Press Retry.");

                NotifyTurnBlocked("DiscardBlocked", "RoundEnded");
                return false;
            }

            PlayerSeat actorPlayerSeat = gameState.GetPlayerSeat(actorSeat);
            if (!actorPlayerSeat.HasDrawnTile)
            {
                if (warnOnBlocked)
                    Warn("Draw before discarding.");

                NotifyTurnBlocked("DiscardBlocked", "DrawnTileMissing");
                return false;
            }

            if (gameState.IsWinDecisionPending)
            {
                if (warnOnBlocked)
                    Warn("Declare or decline win before discarding.");

                NotifyTurnBlocked("DiscardBlocked", "WinDecisionPending");
                return false;
            }

            if (gameState.IsReachDecisionPending)
            {
                if (warnOnBlocked)
                    Warn("Declare or decline reach before discarding.");

                NotifyTurnBlocked("DiscardBlocked", "ReachDecisionPending");
                return false;
            }

            if (gameState.TurnPhase != TurnPhase.WaitingForDiscard &&
                gameState.TurnPhase != TurnPhase.ReachDiscardSelection)
            {
                if (warnOnBlocked)
                    Warn("Discard is not available in the current turn phase.");

                NotifyTurnBlocked("DiscardBlocked", "InvalidTurnPhase");
                return false;
            }

            if (!IsValidReachDiscardCandidate(actorSeat, DiscardSource.DrawnTile, -1))
            {
                if (warnOnBlocked)
                    Warn("Only reach discard candidates can be discarded.");

                NotifyTurnBlocked("DiscardBlocked", "ReachDiscardCandidateMissing");
                return false;
            }

            DiscardResult result = discardService.DiscardDrawnTile(gameState, actorSeat);
            if (!result.Success)
            {
                if (warnOnBlocked)
                    Warn(result.Reason);

                return false;
            }

            bool declaredReachNow = CompleteReachDeclarationIfPending(result.Record);
            gameState.EnterWaitingForDraw();
            ExpireIppatsuAfterDiscard(result.Record, declaredReachNow);
            CompleteDiscard(result.Record);
            return true;
        }

        public void RequestForceDrawSkill(string targetTileCode)
        {
            if (!CanUseSelfTurnInput("SkillBlocked"))
                return;

            RequestForceDrawSkillForSeat(gameState.SelfSeat, targetTileCode);
        }

        public void RequestForceDrawSkillForSeat(SeatId ownerSeat, string targetTileCode)
        {
            if (!CanUseGameState())
                return;
            EnsureSkillFlowService();
            NotifySkillFlowResult(skillFlowService.RequestForceDraw(
                gameState,
                ownerSeat,
                targetTileCode));
        }

        public void RequestSetAutoSortEnabled(bool enabled)
        {
            if (autoSortEnabled == enabled)
                return;

            autoSortEnabled = enabled;
            if (!enabled)
                handAutoSortService?.ClearDeferred();

            EventPublisher.NotifyAutoSortChanged(enabled);

            if (enabled && gameState != null)
                ApplyAutoSort(gameState.SelfSeat, "ToggleEnabled", true);
        }

        public void RequestDeclareWin()
        {
            if (!CanUseGameState())
                return;

            TryRequestDeclareWinForSeat(gameState.SelfSeat);
        }

        public bool TryRequestDeclareWinForSeat(SeatId actorSeat)
        {
            if (!CanUseGameState())
                return false;

            if (gameState.IsReactionWindowPending)
            {
                ReactionWindow reactionWindow = gameState.CurrentReactionWindow;
                return reactionWindow != null &&
                    TryRequestDeclareRonForSeat(actorSeat, reactionWindow.WindowId);
            }

            if (!gameState.IsWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return false;
            }

            if (gameState.WinDecisionSeat != actorSeat)
            {
                Warn("Only the win decision seat can declare win.");
                NotifyTurnBlocked("WinBlocked", "NotWinDecisionSeat");
                return false;
            }

            SeatId seat = gameState.WinDecisionSeat;
            WinType? winType = gameState.WinDecisionType;
            Tile? winningTile = gameState.WinningTile;
            SeatId? sourceSeat = gameState.WinSourceSeat;
            int turnIndex = gameState.WinDecisionTurnIndex;
            WinDeclarationEvaluationResult evaluationResult =
                gameState.PendingWinDeclarationEvaluation;

            EndRound(
                RoundLifecycleService.RoundEndReasonWin,
                () =>
                {
                    EventPublisher.NotifyWinDeclared(seat, turnIndex);
                    EventPublisher.NotifyWinDeclaredDetailed(seat, winType, turnIndex);
                    EventPublisher.NotifyWinDeclaredEvaluated(
                        seat,
                        winType,
                        winningTile,
                        sourceSeat,
                        turnIndex,
                        evaluationResult);
                });
            return true;
        }

        public bool TryRequestDeclareRonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            if (!CanUseGameState())
                return false;

            EnsureReactionWindowService();
            ReactionWindowAnswerResult answer = reactionWindowService.DeclareRon(
                gameState,
                actorSeat,
                reactionWindowId);
            if (!answer.Accepted)
            {
                NotifyTurnBlocked("ReactionBlocked", answer.Reason);
                return false;
            }

            EventPublisher.NotifyReactionWindowAnswered(answer);
            ResolveReactionWindow(answer.Resolution);
            return true;
        }

        public bool TryRequestDeclineRonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            if (!CanUseGameState())
                return false;

            EnsureReactionWindowService();
            ReactionWindowAnswerResult answer = reactionWindowService.DeclineRon(
                gameState,
                actorSeat,
                reactionWindowId);
            if (!answer.Accepted)
            {
                NotifyTurnBlocked("ReactionBlocked", answer.Reason);
                return false;
            }

            EventPublisher.NotifyReactionWindowAnswered(answer);
            EventPublisher.NotifyWinDeclined(answer.Candidate.Seat, answer.Resolution.SourceDiscard.TurnIndex);
            EventPublisher.NotifyWinDeclinedDetailed(
                answer.Candidate.Seat,
                WinType.Ron,
                answer.Resolution.SourceDiscard.TurnIndex);
            ResolveReactionWindow(answer.Resolution);
            return true;
        }

        public bool TryRequestDeclarePonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            return TryRequestDeclareMeldCallForSeat(
                actorSeat,
                reactionWindowId,
                MeldCallKind.Pon,
                0);
        }

        public bool TryRequestDeclareChiForSeat(
            SeatId actorSeat,
            int reactionWindowId,
            int optionId)
        {
            return TryRequestDeclareMeldCallForSeat(
                actorSeat,
                reactionWindowId,
                MeldCallKind.Chi,
                optionId);
        }

        public bool TryRequestDeclareMeldCallForSeat(
            SeatId actorSeat,
            int reactionWindowId,
            MeldCallKind kind,
            int chiOptionId)
        {
            if (!CanUseGameState())
                return false;

            EnsureReactionWindowService();
            ReactionWindowAnswerResult answer = reactionWindowService.DeclareCall(
                gameState,
                actorSeat,
                reactionWindowId,
                kind,
                chiOptionId);
            if (!answer.Accepted)
            {
                NotifyTurnBlocked("ReactionBlocked", answer.Reason);
                return false;
            }

            EventPublisher.NotifyReactionWindowAnswered(answer);
            ResolveReactionWindow(answer.Resolution);
            return true;
        }

        public bool TryRequestDeclinePonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            if (!CanUseGameState())
                return false;

            EnsureReactionWindowService();
            ReactionWindowAnswerResult answer = reactionWindowService.DeclinePon(
                gameState,
                actorSeat,
                reactionWindowId);
            if (!answer.Accepted)
            {
                NotifyTurnBlocked("ReactionBlocked", answer.Reason);
                return false;
            }

            EventPublisher.NotifyReactionWindowAnswered(answer);
            ResolveReactionWindow(answer.Resolution);
            return true;
        }

        public void RequestDeclineWin()
        {
            if (!CanUseGameState())
                return;

            if (gameState.IsReactionWindowPending)
            {
                ReactionWindow reactionWindow = gameState.CurrentReactionWindow;
                ReactionWindowCandidate candidate = reactionWindow != null
                    ? reactionWindow.PendingRonCandidate
                    : null;
                if (candidate == null ||
                    !TryRequestDeclineRonForSeat(candidate.Seat, reactionWindow.WindowId))
                {
                    Warn("No winning hand decision is pending.");
                }

                return;
            }

            if (!gameState.IsWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return;
            }

            EnsureDecisionServices();
            WinDecisionDeclineResult result = winDecisionService.Decline(gameState);

            EventPublisher.NotifyWinDeclined(result.Seat, result.TurnIndex);
            EventPublisher.NotifyWinDeclinedDetailed(
                result.Seat,
                result.WinType,
                result.TurnIndex);

            if (result.WinType == WinType.Tsumo &&
                ShouldAutoDiscardDrawnTileAfterDraw(result.Seat))
            {
                TryAutoDiscardDrawnTileAfterDraw(result.Seat);
                return;
            }

        }

        public void RequestDeclareReach()
        {
            if (!CanUseGameState())
                return;

            if (!gameState.IsReachDecisionPending)
            {
                Warn("No reach decision is pending.");
                NotifyTurnBlocked("ReachBlocked", "ReachDecisionMissing");
                return;
            }

            if (gameState.ReachDecisionSeat != gameState.SelfSeat)
            {
                Warn("Only the self player can declare reach from the current UI.");
                NotifyTurnBlocked("ReachBlocked", "NotSelfReachDecision");
                return;
            }

            EnsureDecisionServices();
            ReachDecisionResult result = reachDecisionService.BeginDiscardSelection(
                gameState,
                gameState.SelfSeat);
            if (!result.Success)
            {
                Warn("Reach discard candidates are not available.");
                NotifyTurnBlocked("ReachBlocked", "ReachCandidatesMissing");
                return;
            }

            EventPublisher.NotifyTurnDebug(
                "ReachDiscardSelection",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: result.Seat,
                turnIndex: result.TurnIndex);
            EventPublisher.NotifyReachDiscardSelectionStarted(result.Seat, result.TurnIndex);
        }

        public void RequestCancelReachDiscardSelection()
        {
            if (!CanUseGameState())
                return;

            if (!gameState.IsReachDiscardSelectionPending)
            {
                Warn("No reach discard selection is pending.");
                NotifyTurnBlocked("ReachBlocked", "ReachDiscardSelectionMissing");
                return;
            }

            if (gameState.ReachDecisionSeat != gameState.SelfSeat)
            {
                Warn("Only the self player can cancel reach discard selection from the current UI.");
                NotifyTurnBlocked("ReachBlocked", "NotSelfReachDecision");
                return;
            }

            EnsureDecisionServices();
            ReachDecisionResult result = reachDecisionService.CancelDiscardSelection(
                gameState,
                gameState.SelfSeat);
            if (!result.Success)
            {
                Warn("Reach discard selection could not be canceled.");
                NotifyTurnBlocked("ReachBlocked", "ReachCandidatesMissing");
                return;
            }

            EventPublisher.NotifyTurnDebug(
                "ReachDiscardSelectionCanceled",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: result.Seat,
                turnIndex: result.TurnIndex);
            EventPublisher.NotifyReachDiscardSelectionCanceled(result.Seat, result.TurnIndex);
        }

        public void RequestDeclineReach()
        {
            if (!CanUseGameState())
                return;

            if (!gameState.IsReachDecisionPending)
            {
                Warn("No reach decision is pending.");
                NotifyTurnBlocked("ReachBlocked", "ReachDecisionMissing");
                return;
            }

            EnsureDecisionServices();
            ReachDecisionResult result = reachDecisionService.Decline(gameState);
            ApplyDeferredAutoSortAfterReachDecisionIfNeeded("ReachDeclined");
            EventPublisher.NotifyTurnDebug(
                "ReachDeclined",
                $"phase={gameState.TurnPhase}",
                seat: result.Seat,
                turnIndex: result.TurnIndex);
            EventPublisher.NotifyReachDeclined(result.Seat, result.TurnIndex);
        }

        private void DealInitialHands()
        {
            EnsureRoundSetupService();
            InitialDealResult result = roundSetupService.DealInitialHands(
                gameState,
                initialHandTileCount,
                EventPublisher.NotifyTileDrawn);
            if (!result.Success)
            {
                EndRound("WallEmptyDuringInitialDeal");
                return;
            }

            ApplyAutoSortToSelfHandIfEnabled("InitialDeal");
        }

        private void AdvanceTurn()
        {
            EnsureTurnFlowService();
            SeatId fromSeat = gameState.CurrentTurn;
            SeatId nextSeat = turnFlowService.AdvanceTurn(gameState);
            EventPublisher.NotifyTurnDebug(
                "EndTurn",
                $"from={fromSeat}; to={nextSeat}; phase={gameState.TurnPhase}",
                seat: nextSeat,
                turnIndex: gameState.TurnIndex);
            StartTurn(nextSeat, gameState.TurnIndex);
        }

        private void AdvanceOrEndAfterDiscard(DiscardRecord record)
        {
            if (record.IsLastLiveWallDiscard)
            {
            EndRound(RoundLifecycleService.RoundEndReasonWallEmpty);
                return;
            }

            AdvanceTurn();
        }

        private void CompleteDiscard(DiscardRecord record)
        {
            EnsureReactionWindowService();
            ReactionWindowStartResult start = reactionWindowService.Begin(gameState, record);
            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();

            EventPublisher.NotifyTurnDebug(
                "DiscardCompleted",
                $"phase={gameState.TurnPhase}; discardTile={record.Tile}",
                seat: record.ActorSeat,
                tile: record.Tile,
                turnIndex: record.TurnIndex);
            EventPublisher.NotifyTileDiscarded(record);

            if (start.ReactionWindow != null)
            {
                EventPublisher.NotifyTurnDebug(
                    "ReactionWindowStarted",
                    $"windowId={start.ReactionWindow.WindowId}; candidates={start.ReactionWindow.Candidates.Count}; sourceSeat={record.ActorSeat}",
                    seat: record.ActorSeat,
                    tile: record.Tile,
                    turnIndex: record.TurnIndex);
                EventPublisher.NotifyReactionWindowStarted(start.ReactionWindow);
            }

            NotifyWinDecisionStartedIfNeeded(
                start.ReactionWindow != null &&
                start.ReactionWindow.PendingRonCandidate != null);
            NotifyWinCheckResults(start.WinCheckNotifications);

            if (start.Resolution.IsResolved)
                ResolveReactionWindow(start.Resolution);
        }

        private void ResolveReactionWindow(ReactionWindowResolution resolution)
        {
            if (!resolution.IsResolved || gameState == null)
                return;

            int windowId = gameState.CurrentReactionWindow != null
                ? gameState.CurrentReactionWindow.WindowId
                : 0;
            EventPublisher.NotifyTurnDebug(
                "ReactionWindowResolved",
                $"windowId={windowId}; resolution={resolution.Type}; sourceSeat={resolution.SourceDiscard.ActorSeat}",
                seat: resolution.SourceDiscard.ActorSeat,
                tile: resolution.SourceDiscard.Tile,
                turnIndex: resolution.SourceDiscard.TurnIndex);
            EventPublisher.NotifyReactionWindowResolved(resolution);
            if (windowId > 0)
                gameState.CloseReactionWindow(windowId);
            EventPublisher.NotifyReactionWindowClosed(windowId);

            switch (resolution.Type)
            {
                case ReactionWindowResolutionType.NoReaction:
                    AdvanceOrEndAfterDiscard(resolution.SourceDiscard);
                    return;
                case ReactionWindowResolutionType.RonDeclared:
                    EndRound(
                        RoundLifecycleService.RoundEndReasonWin,
                        () => NotifyDeclaredReactionRon(resolution),
                        resolution);
                    return;
                case ReactionWindowResolutionType.PonDeclared:
                case ReactionWindowResolutionType.ChiDeclared:
                    BeginTurnAfterMeldCall(resolution);
                    return;
            }
        }

        private void BeginTurnAfterMeldCall(ReactionWindowResolution resolution)
        {
            if (resolution.Candidate == null || resolution.OpenMeld == null)
                return;

            EnsureTurnFlowService();
            gameState.ClearIppatsuEligibilityForAllPlayers();
            turnFlowService.BeginTurnAfterCall(gameState, resolution.Candidate.Seat);
            EventPublisher.NotifyTurnStarted(
                resolution.Candidate.Seat,
                gameState.TurnIndex);
            EventPublisher.NotifyTurnDebug(
                $"{resolution.OpenMeld.Type}Declared",
                $"windowSourceDiscardId={resolution.SourceDiscard.Id}; caller={resolution.Candidate.Seat}; source={resolution.SourceDiscard.ActorSeat}; phase={gameState.TurnPhase}",
                seat: resolution.Candidate.Seat,
                tile: resolution.SourceDiscard.Tile,
                turnIndex: gameState.TurnIndex);
        }

        private void NotifyDeclaredReactionRon(ReactionWindowResolution resolution)
        {
            ReactionWindowCandidate candidate = resolution.Candidate;
            if (candidate == null)
                return;

            EventPublisher.NotifyWinDeclared(candidate.Seat, resolution.SourceDiscard.TurnIndex);
            EventPublisher.NotifyWinDeclaredDetailed(
                candidate.Seat,
                WinType.Ron,
                resolution.SourceDiscard.TurnIndex);
            EventPublisher.NotifyWinDeclaredEvaluated(
                candidate.Seat,
                WinType.Ron,
                resolution.SourceDiscard.Tile,
                resolution.SourceDiscard.ActorSeat,
                resolution.SourceDiscard.TurnIndex,
                candidate.WinDeclarationEvaluation);
        }

        private void RecordTurnDrawIfNeeded(DrawResult result)
        {
            if (gameState == null || !result.Success || result.Purpose != DrawPurpose.TurnDraw)
                return;

            gameState.RecordTurnDraw(
                result.Seat,
                result.Tile,
                gameState.TurnIndex,
                result.WallCountAfterDraw == 0);
        }

        private void StartTurn(SeatId seat, int turnIndex)
        {
            EnsureTurnFlowService();
            EventPublisher.NotifyTurnStarted(seat, turnIndex);
            EventPublisher.NotifyTurnDebug(
                "BeginTurn",
                $"phase={gameState.TurnPhase}; hasDrawnTile={gameState.GetPlayerSeat(seat).HasDrawnTile}",
                seat: seat,
                turnIndex: turnIndex);

            ResolveReservedSkillBeforeDraw(seat);

            if (!turnFlowService.IsSameCurrentTurn(gameState, seat, turnIndex))
                return;

            TryAutoDrawAtTurnStart(seat, turnIndex);

            if (!turnFlowService.CanContinueAutomaticProcessing(gameState, seat, turnIndex))
                return;

            TurnAutomationPolicy policy = BuildTurnAutomationPolicy(seat);
            if (policy.UseCpuController)
            {
                cpuTurnController?.TryStartCpuTurn(
                    this,
                    gameState,
                    seat,
                    turnIndex);
            }
        }

        private bool IsStillCurrentTurn(SeatId seat, int turnIndex)
        {
            EnsureTurnFlowService();
            return turnFlowService.IsSameCurrentTurn(gameState, seat, turnIndex);
        }

        private bool CanEvaluateTurnAutomation(SeatId seat, int turnIndex)
        {
            EnsureTurnFlowService();
            return turnFlowService.CanContinueAutomaticProcessing(gameState, seat, turnIndex);
        }

        private TurnAutomationPolicy BuildTurnAutomationPolicy(SeatId seat)
        {
            EnsureTurnFlowService();
            return turnFlowService.BuildAutomationPolicy(gameState, seat, enableAutoDraw);
        }

        private void ResolveReservedSkillBeforeDraw(SeatId seat)
        {
            EnsureSkillFlowService();
            NotifySkillFlowResult(skillFlowService.ResolveReservedBeforeDraw(gameState, seat));
        }

        private bool TryAutoDrawAtTurnStart(SeatId seat, int turnIndex)
        {
            if (!CanEvaluateTurnAutomation(seat, turnIndex))
                return false;

            TurnAutomationPolicy policy = BuildTurnAutomationPolicy(seat);
            if (!policy.AutoDrawAtTurnStart)
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat == null || playerSeat.HasDrawnTile)
                return false;

            string startedEventName = playerSeat.IsReachDeclared
                ? "ReachAutoDrawStarted"
                : "AutoDrawStarted";
            string completedEventName = playerSeat.IsReachDeclared
                ? "ReachAutoDrawCompleted"
                : "AutoDrawCompleted";
            string blockedEventName = playerSeat.IsReachDeclared
                ? "ReachAutoDrawSkipped"
                : "AutoDrawSkipped";
            EventPublisher.NotifyTurnDebug(
                startedEventName,
                $"phase={gameState.TurnPhase}; hasDrawnTile={playerSeat.HasDrawnTile}",
                seat: seat,
                turnIndex: turnIndex);

            TryDrawForSeat(seat, completedEventName, blockedEventName, false);
            return true;
        }

        private void CheckWinPrototype()
        {
            // PROTOTYPE: Check only a closed-hand self-draw declaration candidate.
            InitializeEvaluators();
            WinDecisionEvaluation evaluation = winDecisionService.EvaluateTsumo(gameState);
            NotifyWinDecisionStartedIfNeeded(evaluation);
            NotifyWinCheckResults(evaluation);
        }

        private void ResolveAfterDraw(SeatId seat)
        {
            CheckWinPrototype();

            if (gameState.IsWinDecisionPending)
            {
                EnsureTurnFlowService();
                if (turnFlowService.IsCpu(gameState, gameState.WinDecisionSeat))
                {
                    cpuTurnController?.TryRespondToWinDecision(
                        this,
                        gameState,
                        gameState.WinDecisionSeat,
                        gameState.WinDecisionTurnIndex);
                }

                return;
            }

            if (ShouldAutoDiscardDrawnTileAfterDraw(seat))
            {
                TryAutoDiscardDrawnTileAfterDraw(seat);
                return;
            }

            TryBeginReachDecisionAfterDraw(seat);
        }

        private bool ShouldAutoDiscardDrawnTileAfterDraw(SeatId seat)
        {
            EnsureTurnFlowService();
            return turnFlowService.ShouldAutoDiscardDrawnTileAfterDraw(
                gameState,
                seat,
                enableAutoDraw);
        }

        private bool TryAutoDiscardDrawnTileAfterDraw(SeatId seat)
        {
            if (!ShouldAutoDiscardDrawnTileAfterDraw(seat))
                return false;

            EnsureAutoDiscardDrawnTileController();
            return autoDiscardDrawnTileController.TryStart(
                seat,
                gameState.TurnIndex,
                autoDiscardDrawnTileDelaySeconds,
                CanExecuteAutoDiscardDrawnTile,
                TryAutoDiscardDrawnTileAfterDrawImmediate);
        }

        private bool TryAutoDiscardDrawnTileAfterDrawImmediate(SeatId seat)
        {
            if (!ShouldAutoDiscardDrawnTileAfterDraw(seat))
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            Tile? drawnTile = playerSeat != null ? playerSeat.DrawnTile : null;
            EventPublisher.NotifyTurnDebug(
                "AutoDiscardDrawnTileStarted",
                $"seat={seat}; tile={drawnTile}",
                seat: seat,
                tile: drawnTile,
                turnIndex: gameState.TurnIndex);

            return TryRequestDiscardDrawnTileForSeatInternal(seat, false);
        }

        private IEnumerator RunAutoDiscardDrawnTileAfterDraw(
            SeatId seat,
            int turnIndex,
            int operationVersion)
        {
            EnsureAutoDiscardDrawnTileController();
            return autoDiscardDrawnTileController.CreateRoutine(
                seat,
                turnIndex,
                operationVersion,
                autoDiscardDrawnTileDelaySeconds,
                CanExecuteAutoDiscardDrawnTile,
                TryAutoDiscardDrawnTileAfterDrawImmediate);
        }

        private void CancelPendingAutoDiscardDrawnTile()
        {
            autoDiscardDrawnTileController?.CancelPending();
        }

        private bool CanExecuteAutoDiscardDrawnTile(SeatId seat, int turnIndex)
        {
            return CanEvaluateTurnAutomation(seat, turnIndex) &&
                ShouldAutoDiscardDrawnTileAfterDraw(seat);
        }

        private void TryBeginReachDecisionAfterDraw(SeatId seat)
        {
            EnsureDecisionServices();
            ReachDecisionResult result = reachDecisionService.TryBeginAfterDraw(gameState, seat);
            if (!result.Success)
                return;

            EventPublisher.NotifyTurnDebug(
                "ReachDecision",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: result.Seat,
                tile: result.DrawnTile,
                turnIndex: result.TurnIndex);
            EventPublisher.NotifyReachDecisionStarted(result.Seat, result.TurnIndex);
        }

        private void CommitDrawnTileToHandIfPresent(SeatId seat)
        {
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (!playerSeat.CommitDrawnTileToHand())
                return;

            ApplyAutoSortIfEnabled(seat, "DrawnTileCommitted", false);
        }

        private bool IsValidReachDiscardCandidate(SeatId seat, DiscardSource source, int handIndex)
        {
            EnsureDecisionServices();
            return reachDecisionService.IsValidDiscardCandidate(
                gameState,
                seat,
                source,
                handIndex);
        }

        private bool CompleteReachDeclarationIfPending(DiscardRecord record)
        {
            EnsureDecisionServices();
            ReachDeclarationResult result = reachDecisionService.CompleteDeclarationIfPending(
                gameState,
                record);
            if (!result.Declared)
                return false;
            ApplyDeferredAutoSortAfterReachDecisionIfNeeded("ReachDeclared");
            EventPublisher.NotifyReachDeclared(result.Seat, result.TurnIndex);
            EventPublisher.NotifyTurnDebug(
                "ReachDeclared",
                $"phase={gameState.TurnPhase}; discardTile={record.Tile}",
                seat: result.Seat,
                tile: record.Tile,
                turnIndex: result.TurnIndex);
            return true;
        }

        private void ExpireIppatsuAfterDiscard(
            DiscardRecord record,
            bool declaredReachNow)
        {
            EnsureDecisionServices();
            reachDecisionService.ExpireIppatsuAfterDiscard(
                gameState,
                record,
                declaredReachNow);
        }

        private void SetWinDecisionPending(bool isPending, SeatId seat, int turnIndex)
        {
            if (gameState == null)
                return;
            EnsureDecisionServices();
            winDecisionService.SetPending(gameState, isPending, seat, turnIndex);
            if (isPending)
            {
                EventPublisher.NotifyTurnDebug(
                    "WinDecision",
                    $"phase={gameState.TurnPhase}",
                    seat: seat,
                    turnIndex: turnIndex);
                return;
            }

        }

        private void ClearWinDecision()
        {
            EnsureDecisionServices();
            winDecisionService.SetPending(gameState, false, default, 0);
        }

        private void EndRound(string reason)
        {
            EndRound(reason, null, ReactionWindowResolution.None);
        }

        private void EndRound(string reason, System.Action afterRoundMarkedEnded)
        {
            EndRound(reason, afterRoundMarkedEnded, ReactionWindowResolution.None);
        }

        private void EndRound(
            string reason,
            System.Action afterRoundMarkedEnded,
            ReactionWindowResolution reactionResolution)
        {
            EnsureRoundLifecycleService();
            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();
            handAutoSortService?.ClearDeferred();
            RoundLifecycleEndResult endResult = roundLifecycleService.EndRound(
                gameState,
                reason,
                reactionResolution);
            RoundResult roundResult = endResult.RoundResult;

            EventPublisher.NotifyTurnDebug(
                "RoundEnded",
                $"phase={gameState.TurnPhase}; reason={reason}; windProgress={gameState.WindProgress}",
                seat: gameState.CurrentTurn,
                turnIndex: gameState.TurnIndex);
            afterRoundMarkedEnded?.Invoke();
            EventPublisher.NotifyRoundEnded(reason);
            if (roundResult != null)
                EventPublisher.NotifyRoundResultReady(roundResult);
        }

        private void NotifySkillResolutionEvents(DrawResult result)
        {
            EnsureSkillFlowService();
            SkillDrawResolutionResult resolution = skillFlowService.ResolveDrawResult(result);
            if (!resolution.Resolved)
                return;

            ActiveSkillEffect effect = resolution.Effect;
            EventPublisher.NotifySkillEffectResolved(result);
            EventPublisher.NotifySkillEffectExpired(effect, "ConsumedByDraw");
        }

        private void NotifySkillFlowResult(SkillFlowResult result)
        {
            if (result.Type == SkillFlowResultType.None)
                return;

            if (result.HasReservation && result.Type != SkillFlowResultType.Reserved)
                EventPublisher.NotifySkillReservationConsumed(result.Reservation);

            switch (result.Type)
            {
                case SkillFlowResultType.Reserved:
                    EventPublisher.NotifySkillReserved(result.Reservation);
                    break;
                case SkillFlowResultType.Activated:
                    EventPublisher.NotifySkillActivated(result.Seat, result.Effect);
                    EventPublisher.NotifySkillActivatedDetailed(
                        result.Seat,
                        result.Effect,
                        result.BeforeDraw);
                    EventPublisher.NotifySkillEffectRegistered(result.Effect);
                    break;
                case SkillFlowResultType.Rejected:
                case SkillFlowResultType.UnsupportedReservation:
                    Warn(result.Reason);
                    if (result.BeforeDraw || result.HasReservation ||
                        !result.TargetTile.Equals(default(Tile)))
                    {
                        EventPublisher.NotifySkillReservationRejected(
                            result.Seat,
                            result.HasReservation
                                ? result.Reservation.SkillEffectKind
                                : SkillEffectKind.ForceDrawTile,
                            result.TargetTile,
                            result.Reason);
                    }
                    break;
            }
        }

        private void NotifyWinCheckResults(WinDecisionEvaluation evaluation)
        {
            NotifyWinCheckResults(evaluation.Notifications);
        }

        private void NotifyWinCheckResults(IReadOnlyList<WinCheckNotification> notifications)
        {
            if (notifications == null)
                return;

            for (int i = 0; i < notifications.Count; i++)
            {
                WinCheckNotification notification = notifications[i];
                EventPublisher.NotifyWinChecked(
                    notification.Seat,
                    notification.TurnIndex,
                    notification.CanDeclareWin);
                EventPublisher.NotifyWinCheckedDetailed(
                    notification.Seat,
                    notification.WinType,
                    notification.Tile,
                    notification.SourceSeat,
                    notification.TurnIndex,
                    notification.CanDeclareWin);
            }
        }

        private void NotifyWinDecisionStartedIfNeeded(WinDecisionEvaluation evaluation)
        {
            NotifyWinDecisionStartedIfNeeded(evaluation.DecisionStarted);
        }

        private void NotifyWinDecisionStartedIfNeeded(bool decisionStarted)
        {
            if (!decisionStarted || gameState == null)
                return;

            EventPublisher.NotifyTurnDebug(
                "WinDecision",
                $"phase={gameState.TurnPhase}; winType={gameState.WinDecisionType}; sourceSeat={gameState.WinSourceSeat}",
                seat: gameState.WinDecisionSeat,
                tile: gameState.WinningTile,
                turnIndex: gameState.WinDecisionTurnIndex);
        }

        private void CacheReferences()
        {
            if (eventNotifier == null)
                eventNotifier = GetComponent<MahjongEventNotifier>();

            if (cpuTurnController == null)
                cpuTurnController = GetComponent<CpuTurnController>();

            if (autoDiscardDrawnTileController == null)
            {
                autoDiscardDrawnTileController =
                    GetComponent<AutoDiscardDrawnTileController>();
            }
        }

        private void EnsureRoundSetupService()
        {
            if (roundSetupService != null)
                return;

            roundSetupService = new RoundSetupService(
                roundStartingSeatResolver,
                playerTurnManager,
                drawService);
        }

        private void EnsureRoundLifecycleService()
        {
            if (roundLifecycleService != null)
                return;

            roundLifecycleService = new RoundLifecycleService(
                new WinningCandidateSelector());
        }

        private void EnsureTurnFlowService()
        {
            if (turnFlowService != null)
                return;

            turnFlowService = new TurnFlowService(playerTurnManager);
        }

        private void InitializeEvaluators()
        {
            if (furitenEvaluator == null)
                furitenEvaluator = new FuritenEvaluator(winChecker);

            if (winDeclarationEvaluator != null &&
                initializedYakuDefinitionCatalog == yakuDefinitionCatalog)
            {
                return;
            }

            handEvaluator = new HandEvaluator(yakuDefinitionCatalog);
            winDeclarationEvaluator = new WinDeclarationEvaluator(winChecker, handEvaluator);
            noYakuTenpaiEvaluator = yakuDefinitionCatalog != null
                ? new NoYakuTenpaiEvaluator(winDeclarationEvaluator)
                : null;
            initializedYakuDefinitionCatalog = yakuDefinitionCatalog;
            winDecisionService = new WinDecisionService(
                winDeclarationEvaluator,
                furitenEvaluator,
                noYakuTenpaiEvaluator);
            reactionWindowService = null;
        }

        private void EnsureDecisionServices()
        {
            InitializeEvaluators();
            EnsureReactionWindowService();
            if (reachDecisionService == null)
                reachDecisionService = new ReachDecisionService(reachChecker);
        }

        private void EnsureReactionWindowService()
        {
            InitializeEvaluators();
            reactionWindowService ??= new ReactionWindowService(winDecisionService);
        }

        private void EnsureSkillFlowService()
        {
            skillFlowService ??= new SkillFlowService(skillSystem, skillReservationService);
        }

        private void EnsureHandAutoSortService()
        {
            handAutoSortService ??= new HandAutoSortService();
        }

        private void EnsureCpuTurnController()
        {
            if (cpuTurnController != null)
                return;

            // PROTOTYPE: Ensure the local prototype can run CPU turns without scene migration.
            cpuTurnController = gameObject.AddComponent<CpuTurnController>();
        }

        private void EnsureAutoDiscardDrawnTileController()
        {
            if (autoDiscardDrawnTileController != null)
                return;

            // PROTOTYPE: Keep the reach auto-discard coroutine separate from game progression.
            autoDiscardDrawnTileController = gameObject.AddComponent<AutoDiscardDrawnTileController>();
        }

        private void NormalizeParticipantCount()
        {
            participantCount = Mathf.Clamp(participantCount, 1, 4);
        }

        private static SeatId RotateSeatForNextRound(SeatId currentSeat)
        {
            return RoundLifecycleService.RotateSelfSeatForNextRound(currentSeat);
        }

        private SeatId ResolveSelfSeat()
        {
            if (!randomizeSelfSeat)
                return fixedSelfSeat;

            return (SeatId)Random.Range(0, 4);
        }

        private bool CanUseGameState()
        {
            if (gameState != null)
                return true;

            Warn("GameState is not initialized. StartNewRound first.");
            return false;
        }

        private bool CanUseSelfTurnInput(string blockedEventName)
        {
            if (!CanUseGameState())
                return false;

            if (gameState.IsSelfTurn)
                return true;

            Warn("User input is only available during the self player's turn.");
            NotifyTurnBlocked(blockedEventName, "NotSelfTurn");
            return false;
        }

        private void NotifyTurnBlocked(string eventName, string reason)
        {
            if (gameState == null)
                return;

            PlayerSeat currentPlayerSeat = gameState.GetPlayerSeat(gameState.CurrentTurn);
            EventPublisher.NotifyTurnDebug(
                eventName,
                $"reason={reason}; phase={gameState.TurnPhase}; hasDrawnTile={currentPlayerSeat.HasDrawnTile}",
                seat: gameState.CurrentTurn,
                turnIndex: gameState.TurnIndex);
        }

        private void ApplyAutoSortToSelfHandIfEnabled(string reason)
        {
            if (!autoSortEnabled || gameState == null)
                return;

            ApplyAutoSort(gameState.SelfSeat, reason, false);
        }

        private void ApplyAutoSortIfEnabled(SeatId seat, string reason, bool notify)
        {
            if (!autoSortEnabled || gameState == null || !gameState.IsSelfSeat(seat))
                return;

            ApplyAutoSort(seat, reason, notify);
        }

        private void ApplyAutoSort(SeatId seat, string reason, bool notify)
        {
            EnsureHandAutoSortService();
            HandAutoSortResult result = handAutoSortService.Apply(
                gameState,
                autoSortEnabled,
                seat,
                reason);
            if (!result.WasApplied)
                return;

            EventPublisher.NotifyHandAutoSortedDetailed(result.Seat, result.TurnIndex, result.Reason);

            if (notify)
                EventPublisher.NotifyHandAutoSorted(result.Seat, result.TurnIndex);
        }

        private void ApplyDeferredAutoSortAfterReachDecisionIfNeeded(string reason)
        {
            EnsureHandAutoSortService();
            HandAutoSortResult result = handAutoSortService.ApplyDeferredIfReady(
                gameState,
                autoSortEnabled,
                reason);
            if (!result.WasApplied)
                return;

            EventPublisher.NotifyHandAutoSortedDetailed(result.Seat, result.TurnIndex, result.Reason);
            EventPublisher.NotifyHandAutoSorted(result.Seat, result.TurnIndex);
        }

        private void Warn(string message)
        {
            if (!logWarnings)
                return;

            Debug.LogWarning($"{nameof(MahjongGameFlow)}: {message}", this);
        }

    }
}
