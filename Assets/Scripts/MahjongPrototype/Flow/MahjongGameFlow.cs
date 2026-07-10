using System.Collections;
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
    public sealed class MahjongGameFlow : MonoBehaviour
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
        private MahjongFlowEventPublisher eventPublisher;
        private MahjongGameState gameState;
        private Coroutine pendingAutoDiscardDrawnTileCoroutine;
        private int autoDiscardDrawnTileOperationVersion;
        private bool autoSortDeferredUntilReachDecisionResolved;

        private readonly struct TurnAutomationPolicy
        {
            public TurnAutomationPolicy(
                bool isCpu,
                bool autoDrawAtTurnStart,
                bool autoDiscardDrawnTileAfterDraw,
                bool useCpuController)
            {
                IsCpu = isCpu;
                AutoDrawAtTurnStart = autoDrawAtTurnStart;
                AutoDiscardDrawnTileAfterDraw = autoDiscardDrawnTileAfterDraw;
                UseCpuController = useCpuController;
            }

            public bool IsCpu { get; }
            public bool AutoDrawAtTurnStart { get; }
            public bool AutoDiscardDrawnTileAfterDraw { get; }
            public bool UseCpuController { get; }
        }

        public MahjongGameState CurrentState => gameState;
        public MahjongEventNotifier EventNotifier => eventNotifier;
        public bool IsWinDecisionPending => gameState != null && gameState.IsWinDecisionPending;
        public bool IsAutoSortEnabled => autoSortEnabled;
        public bool IsInteractionLocked => gameState != null && gameState.IsInteractionLocked;

        private MahjongFlowEventPublisher EventPublisher =>
            eventPublisher ??= new MahjongFlowEventPublisher(() => eventNotifier, Warn);

        public FuritenEvaluationResultSet EvaluateAllFuriten()
        {
            InitializeEvaluators();
            return furitenEvaluator.EvaluateAll(gameState);
        }

        public NoYakuTenpaiEvaluationResult EvaluateSelfNoYakuTenpai()
        {
            InitializeEvaluators();

            if (gameState == null ||
                yakuDefinitionCatalog == null ||
                noYakuTenpaiEvaluator == null)
            {
                return NoYakuTenpaiEvaluationResult.NotEvaluated;
            }

            if (gameState.IsRoundEnded)
                return NoYakuTenpaiEvaluationResult.NotTenpai;

            PlayerSeat selfPlayerSeat = gameState.GetPlayerSeat(gameState.SelfSeat);
            if (selfPlayerSeat.Hand.Count != 13 || selfPlayerSeat.HasDrawnTile)
                return NoYakuTenpaiEvaluationResult.NotTenpai;

            return noYakuTenpaiEvaluator.Evaluate(
                selfPlayerSeat.Hand.GetTiles(),
                gameState.SelfSeat,
                gameState.WindProgress.RoundWind,
                gameState.SelfSeat,
                selfPlayerSeat.IsReachDeclared,
                true);
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
            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();
            autoSortDeferredUntilReachDecisionResolved = false;

            skillReservationService.Clear();
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

            DrawResult result = drawService.DrawTile(seat, gameState, DrawPurpose.TurnDraw);

            if (!result.Success)
            {
                NotifySkillResolutionEvents(result);
                EndRound(RoundLifecycleService.RoundEndReasonWallEmpty);
                return false;
            }

            RecordTurnDrawIfNeeded(result);
            playerSeat.SetDrawnTile(result.Tile);
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
            if (!selfPlayerSeat.HasDrawnTile)
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
            ExpireIppatsuAfterDiscard(result.Record, declaredReachNow);
            EventPublisher.NotifyTurnDebug(
                "DiscardCompleted",
                $"phase={gameState.TurnPhase}; discardTile={result.Record.Tile}",
                seat: result.Record.ActorSeat,
                tile: result.Record.Tile,
                turnIndex: result.Record.TurnIndex);
            EventPublisher.NotifyTileDiscarded(result.Record);
            if (!TryBeginRonDecision(result.Record))
                AdvanceOrEndAfterDiscard(result.Record);
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
            ExpireIppatsuAfterDiscard(result.Record, declaredReachNow);
            EventPublisher.NotifyTurnDebug(
                "DiscardCompleted",
                $"phase={gameState.TurnPhase}; discardTile={result.Record.Tile}",
                seat: result.Record.ActorSeat,
                tile: result.Record.Tile,
                turnIndex: result.Record.TurnIndex);
            EventPublisher.NotifyTileDiscarded(result.Record);
            if (!TryBeginRonDecision(result.Record))
                AdvanceOrEndAfterDiscard(result.Record);
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

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                return;
            }

            if (gameState.IsWinDecisionPending)
            {
                Warn("Declare or decline win before activating another skill.");
                return;
            }

            if (gameState.IsReachDiscardSelectionPending)
            {
                Warn("Resolve reach discard selection before activating another skill.");
                return;
            }

            if (!Tile.TryParse(targetTileCode, out Tile targetTile))
            {
                Warn("Invalid target tile. Use 1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C.");
                return;
            }

            if (ownerSeat != gameState.CurrentTurn)
            {
                ReserveForceDrawSkill(ownerSeat, targetTile);
                return;
            }

            ActivateForceDrawSkill(ownerSeat, targetTile, false);
        }

        private void ReserveForceDrawSkill(SeatId ownerSeat, Tile targetTile)
        {
            if (!IsActiveSeat(ownerSeat))
            {
                string reason = "Owner seat is not active.";
                Warn(reason);
                EventPublisher.NotifySkillReservationRejected(ownerSeat, SkillEffectKind.ForceDrawTile, targetTile, reason);
                return;
            }

            if (gameState.HasActiveSkillEffect(ownerSeat, SkillEffectKind.ForceDrawTile))
            {
                string reason = "Force draw skill is already active.";
                Warn(reason);
                EventPublisher.NotifySkillReservationRejected(ownerSeat, SkillEffectKind.ForceDrawTile, targetTile, reason);
                return;
            }

            PendingSkillReservation reservation = new PendingSkillReservation(
                ownerSeat,
                SkillEffectKind.ForceDrawTile,
                targetTile,
                gameState.CurrentTurn,
                gameState.TurnIndex);

            if (!skillReservationService.Reserve(reservation, out string reserveReason))
            {
                Warn(reserveReason);
                EventPublisher.NotifySkillReservationRejected(ownerSeat, SkillEffectKind.ForceDrawTile, targetTile, reserveReason);
                return;
            }

            EventPublisher.NotifySkillReserved(reservation);
        }

        private bool ActivateForceDrawSkill(SeatId actorSeat, Tile targetTile, bool beforeDraw)
        {
            SkillActivationResult result = skillSystem.ActivateForceDrawTile(
                gameState,
                actorSeat,
                targetTile);

            if (!result.Success)
            {
                Warn(result.Reason);
                if (beforeDraw)
                    EventPublisher.NotifySkillReservationRejected(actorSeat, SkillEffectKind.ForceDrawTile, targetTile, result.Reason);

                return false;
            }

            EventPublisher.NotifySkillActivated(actorSeat, result.Effect);
            EventPublisher.NotifySkillActivatedDetailed(actorSeat, result.Effect, beforeDraw);
            EventPublisher.NotifySkillEffectRegistered(result.Effect);
            return true;
        }

        public void RequestSetAutoSortEnabled(bool enabled)
        {
            if (autoSortEnabled == enabled)
                return;

            autoSortEnabled = enabled;
            if (!enabled)
                autoSortDeferredUntilReachDecisionResolved = false;

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

        public void RequestDeclineWin()
        {
            if (!CanUseGameState())
                return;

            if (!gameState.IsWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return;
            }

            SeatId seat = gameState.WinDecisionSeat;
            WinType? winType = gameState.WinDecisionType;
            int turnIndex = gameState.WinDecisionTurnIndex;
            bool shouldEndAfterDeclinedLastLiveWallRon =
                winType == WinType.Ron && IsLastDiscardLastLiveWallDiscard();
            MarkDeclinedRonFuriten(seat, winType);
            ClearWinDecision();

            EventPublisher.NotifyWinDeclined(seat, turnIndex);
            EventPublisher.NotifyWinDeclinedDetailed(seat, winType, turnIndex);

            if (winType == WinType.Tsumo && ShouldAutoDiscardDrawnTileAfterDraw(seat))
            {
                TryAutoDiscardDrawnTileAfterDraw(seat);
                return;
            }

            if (winType == WinType.Ron && !gameState.IsRoundEnded)
            {
                if (shouldEndAfterDeclinedLastLiveWallRon)
                    EndRound(RoundLifecycleService.RoundEndReasonWallEmpty);
                else
                    AdvanceTurn();
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

            SeatId seat = gameState.SelfSeat;
            int turnIndex = gameState.ReachDecisionTurnIndex;
            gameState.BeginReachDiscardSelection(seat);
            if (!gameState.IsReachDiscardSelectionPending)
            {
                Warn("Reach discard candidates are not available.");
                NotifyTurnBlocked("ReachBlocked", "ReachCandidatesMissing");
                return;
            }

            EventPublisher.NotifyTurnDebug(
                "ReachDiscardSelection",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: seat,
                turnIndex: turnIndex);
            EventPublisher.NotifyReachDiscardSelectionStarted(seat, turnIndex);
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

            SeatId seat = gameState.ReachDecisionSeat;
            int turnIndex = gameState.ReachDecisionTurnIndex;
            if (!gameState.CancelReachDiscardSelection())
            {
                Warn("Reach discard selection could not be canceled.");
                NotifyTurnBlocked("ReachBlocked", "ReachCandidatesMissing");
                return;
            }

            EventPublisher.NotifyTurnDebug(
                "ReachDiscardSelectionCanceled",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: seat,
                turnIndex: turnIndex);
            EventPublisher.NotifyReachDiscardSelectionCanceled(seat, turnIndex);
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

            SeatId seat = gameState.ReachDecisionSeat;
            int turnIndex = gameState.ReachDecisionTurnIndex;
            gameState.ClearReachDecision();
            ApplyDeferredAutoSortAfterReachDecisionIfNeeded("ReachDeclined");
            EventPublisher.NotifyTurnDebug(
                "ReachDeclined",
                $"phase={gameState.TurnPhase}",
                seat: seat,
                turnIndex: turnIndex);
            EventPublisher.NotifyReachDeclined(seat, turnIndex);
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
            SeatId fromSeat = gameState.CurrentTurn;
            SeatId nextSeat = playerTurnManager.EndTurnAndSelectNext(gameState, gameState.ActiveTurnSeats);
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

        private bool IsLastDiscardLastLiveWallDiscard()
        {
            if (gameState == null || gameState.Discards.Count <= 0)
                return false;

            return gameState.Discards[gameState.Discards.Count - 1].IsLastLiveWallDiscard;
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
            EventPublisher.NotifyTurnStarted(seat, turnIndex);
            EventPublisher.NotifyTurnDebug(
                "BeginTurn",
                $"phase={gameState.TurnPhase}; hasDrawnTile={gameState.GetPlayerSeat(seat).HasDrawnTile}",
                seat: seat,
                turnIndex: turnIndex);

            ResolveReservedSkillBeforeDraw(seat);

            if (!IsStillCurrentTurn(seat, turnIndex))
                return;

            TryAutoDrawAtTurnStart(seat, turnIndex);

            if (!CanEvaluateTurnAutomation(seat, turnIndex))
                return;

            TurnAutomationPolicy policy = BuildTurnAutomationPolicy(seat);
            if (policy.UseCpuController)
                cpuTurnController?.TryStartCpuTurn(this, gameState, seat, turnIndex);
        }

        private bool IsStillCurrentTurn(SeatId seat, int turnIndex)
        {
            return gameState != null &&
                !gameState.IsRoundEnded &&
                !gameState.IsWinDecisionPending &&
                gameState.CurrentTurn == seat &&
                gameState.TurnIndex == turnIndex;
        }

        private bool CanEvaluateTurnAutomation(SeatId seat, int turnIndex)
        {
            return IsStillCurrentTurn(seat, turnIndex) &&
                !gameState.IsReachDecisionPending &&
                !gameState.IsReachDiscardSelectionPending;
        }

        private TurnAutomationPolicy BuildTurnAutomationPolicy(SeatId seat)
        {
            if (gameState == null)
                return new TurnAutomationPolicy(false, false, false, false);

            SeatSlot slot = gameState.GetSeatSlot(seat);
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            bool isCpu = slot.HasPlayer && slot.ParticipantType == ParticipantType.Cpu;
            bool isReachDeclared = playerSeat != null && playerSeat.IsReachDeclared;
            bool autoDrawAtTurnStart = enableAutoDraw || isReachDeclared;
            bool autoDiscardDrawnTileAfterDraw = isReachDeclared;
            bool useCpuController = isCpu;

            return new TurnAutomationPolicy(
                isCpu,
                autoDrawAtTurnStart,
                autoDiscardDrawnTileAfterDraw,
                useCpuController);
        }

        private void ResolveReservedSkillBeforeDraw(SeatId seat)
        {
            if (gameState.IsRoundEnded || gameState.IsWinDecisionPending)
                return;

            if (!skillReservationService.TryConsumeForTurn(seat, out PendingSkillReservation reservation))
                return;

            EventPublisher.NotifySkillReservationConsumed(reservation);

            switch (reservation.SkillEffectKind)
            {
                case SkillEffectKind.ForceDrawTile:
                    ActivateForceDrawSkill(reservation.OwnerSeat, reservation.TargetTile, true);
                    break;
                default:
                    EventPublisher.NotifySkillReservationRejected(
                        reservation.OwnerSeat,
                        reservation.SkillEffectKind,
                        reservation.TargetTile,
                        "Unsupported skill reservation.");
                    break;
            }
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
            SeatId candidateSeat = gameState.CurrentTurn;
            PlayerSeat playerSeat = gameState.GetPlayerSeat(candidateSeat);
            Tile? winningTile = playerSeat.DrawnTile;
            WinDeclarationEvaluationResult evaluationResult =
                winningTile.HasValue
                    ? winDeclarationEvaluator.EvaluateWithTile(CreateWinDeclarationContext(
                        playerSeat,
                        WinType.Tsumo,
                        winningTile.Value,
                        null))
                    : WinDeclarationEvaluationResult.NotWinningShape(WinCheckResult.NotWin);
            bool canDeclareWin = evaluationResult.CanDeclareWin;

            if (canDeclareWin)
            {
                SetWinDecisionPendingDetailed(
                    candidateSeat,
                    WinType.Tsumo,
                    winningTile.Value,
                    null,
                    gameState.TurnIndex,
                    evaluationResult);
            }
            else
            {
                ClearWinDecision();
            }

            EventPublisher.NotifyWinChecked(candidateSeat, gameState.TurnIndex, canDeclareWin);
            EventPublisher.NotifyWinCheckedDetailed(
                candidateSeat,
                WinType.Tsumo,
                winningTile,
                null,
                gameState.TurnIndex,
                canDeclareWin);
        }

        private void ResolveAfterDraw(SeatId seat)
        {
            CheckWinPrototype();

            if (gameState.IsWinDecisionPending)
            {
                cpuTurnController?.TryRespondToWinDecision(
                    this,
                    gameState,
                    gameState.WinDecisionSeat,
                    gameState.WinDecisionTurnIndex);
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
            if (gameState == null ||
                gameState.IsRoundEnded ||
                gameState.IsWinDecisionPending ||
                gameState.IsReachDecisionPending ||
                gameState.IsReachDiscardSelectionPending ||
                gameState.CurrentTurn != seat ||
                gameState.TurnPhase != TurnPhase.WaitingForDiscard)
            {
                return false;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat == null || !playerSeat.HasDrawnTile)
                return false;

            TurnAutomationPolicy policy = BuildTurnAutomationPolicy(seat);
            return policy.AutoDiscardDrawnTileAfterDraw;
        }

        private bool TryAutoDiscardDrawnTileAfterDraw(SeatId seat)
        {
            if (!ShouldAutoDiscardDrawnTileAfterDraw(seat))
                return false;

            CancelPendingAutoDiscardDrawnTile();

            if (autoDiscardDrawnTileDelaySeconds <= 0f)
                return TryAutoDiscardDrawnTileAfterDrawImmediate(seat);

            int operationVersion = autoDiscardDrawnTileOperationVersion;
            int turnIndex = gameState.TurnIndex;
            pendingAutoDiscardDrawnTileCoroutine = StartCoroutine(
                RunAutoDiscardDrawnTileAfterDraw(seat, turnIndex, operationVersion));
            return true;
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
            if (autoDiscardDrawnTileDelaySeconds > 0f)
                yield return new WaitForSeconds(autoDiscardDrawnTileDelaySeconds);
            else
                yield return null;

            if (operationVersion != autoDiscardDrawnTileOperationVersion)
                yield break;

            if (!CanEvaluateTurnAutomation(seat, turnIndex) ||
                !ShouldAutoDiscardDrawnTileAfterDraw(seat))
            {
                pendingAutoDiscardDrawnTileCoroutine = null;
                yield break;
            }

            pendingAutoDiscardDrawnTileCoroutine = null;
            TryAutoDiscardDrawnTileAfterDrawImmediate(seat);
        }

        private void CancelPendingAutoDiscardDrawnTile()
        {
            autoDiscardDrawnTileOperationVersion++;

            if (pendingAutoDiscardDrawnTileCoroutine == null)
                return;

            StopCoroutine(pendingAutoDiscardDrawnTileCoroutine);
            pendingAutoDiscardDrawnTileCoroutine = null;
        }

        private void TryBeginReachDecisionAfterDraw(SeatId seat)
        {
            if (gameState == null ||
                gameState.IsRoundEnded ||
                gameState.IsWinDecisionPending ||
                gameState.IsReachDecisionPending ||
                gameState.IsReachDiscardSelectionPending)
            {
                return;
            }

            if (!gameState.IsSelfTurn || !gameState.IsSelfSeat(seat))
                return;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.IsReachDeclared ||
                !playerSeat.HasDrawnTile ||
                !playerSeat.DrawnTile.HasValue ||
                playerSeat.Hand.Count != 13)
            {
                return;
            }

            ReachCheckResult result = reachChecker.CheckReach(
                playerSeat.Hand.GetTiles(),
                playerSeat.DrawnTile.Value);
            if (!result.CanReach)
                return;

            gameState.BeginReachDecision(seat, result.Candidates, gameState.TurnIndex);
            if (!gameState.IsReachDecisionPending)
                return;

            EventPublisher.NotifyTurnDebug(
                "ReachDecision",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: seat,
                tile: playerSeat.DrawnTile,
                turnIndex: gameState.TurnIndex);
            EventPublisher.NotifyReachDecisionStarted(seat, gameState.TurnIndex);
        }

        private bool TryBeginRonDecision(DiscardRecord discard)
        {
            InitializeEvaluators();
            FuritenEvaluationResultSet furitenResults =
                furitenEvaluator.EvaluateAll(gameState);

            // PROTOTYPE: Only locally-operated seats can answer the current single win decision.
            // CPU/RemoteHuman ron decisions will be introduced with a reaction window.
            for (int i = 0; i < gameState.SeatSlots.Count; i++)
            {
                SeatSlot candidateSlot = gameState.SeatSlots[i];
                if (!candidateSlot.HasPlayer)
                    continue;

                SeatId candidateSeat = candidateSlot.Wind;
                if (candidateSeat == discard.ActorSeat)
                    continue;

                if (candidateSlot.ParticipantType != ParticipantType.LocalHuman)
                    continue;

                PlayerSeat candidatePlayerSeat = gameState.GetPlayerSeat(candidateSeat);
                WinDeclarationEvaluationResult evaluationResult =
                    winDeclarationEvaluator.EvaluateWithTile(CreateWinDeclarationContext(
                        candidatePlayerSeat,
                        WinType.Ron,
                        discard.Tile,
                        discard.ActorSeat,
                        discard));
                if (IsNoYakuWinningShape(evaluationResult, candidatePlayerSeat))
                    candidatePlayerSeat.MarkTemporaryFuriten();

                bool passesFuritenCheck =
                    furitenResults.TryGet(
                        candidateSeat,
                        out FuritenSeatEvaluationResult furitenResult) &&
                    furitenResult.IsEvaluated &&
                    !furitenResult.IsFuriten;
                bool canDeclareWin = evaluationResult.CanDeclareWin && passesFuritenCheck;

                if (canDeclareWin)
                {
                    SetWinDecisionPendingDetailed(
                        candidateSeat,
                        WinType.Ron,
                        discard.Tile,
                        discard.ActorSeat,
                        discard.TurnIndex,
                        evaluationResult);
                }

                EventPublisher.NotifyWinChecked(candidateSeat, discard.TurnIndex, canDeclareWin);
                EventPublisher.NotifyWinCheckedDetailed(
                    candidateSeat,
                    WinType.Ron,
                    discard.Tile,
                    discard.ActorSeat,
                    discard.TurnIndex,
                    canDeclareWin);

                if (!canDeclareWin)
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsNoYakuWinningShape(
            WinDeclarationEvaluationResult evaluationResult,
            PlayerSeat playerSeat)
        {
            return evaluationResult != null &&
                evaluationResult.IsWinningShape &&
                !evaluationResult.HasYaku &&
                playerSeat != null &&
                !playerSeat.IsReachDeclared;
        }

        private void MarkDeclinedRonFuriten(SeatId seat, WinType? winType)
        {
            if (winType != WinType.Ron)
                return;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.IsReachDeclared)
            {
                playerSeat.MarkReachPassFuriten();
                return;
            }

            playerSeat.MarkTemporaryFuriten();
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
            if (gameState == null || !gameState.IsReachDiscardSelectionPending)
                return true;

            if (seat != gameState.ReachDecisionSeat)
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat == null)
                return false;

            for (int i = 0; i < gameState.ReachDiscardCandidates.Count; i++)
            {
                ReachDiscardCandidate candidate = gameState.ReachDiscardCandidates[i];
                if (candidate.Source != source || candidate.HandIndex != handIndex)
                    continue;

                if (source == DiscardSource.DrawnTile)
                {
                    if (playerSeat.HasDrawnTile &&
                        playerSeat.DrawnTile.HasValue &&
                        candidate.Tile == playerSeat.DrawnTile.Value)
                    {
                        return true;
                    }

                    continue;
                }

                if (handIndex < 0 || handIndex >= playerSeat.Hand.Count)
                    continue;

                if (candidate.Tile == playerSeat.Hand.GetTiles()[handIndex])
                    return true;
            }

            return false;
        }

        private bool CompleteReachDeclarationIfPending(DiscardRecord record)
        {
            if (gameState == null ||
                !gameState.IsReachDiscardSelectionPending ||
                record.ActorSeat != gameState.ReachDecisionSeat)
            {
                return false;
            }

            SeatId seat = record.ActorSeat;
            int turnIndex = gameState.TurnIndex;
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            bool isDoubleReachDeclared = IsFirstDiscardBySeat(gameState, seat);
            playerSeat.DeclareReach(turnIndex, isDoubleReachDeclared);
            gameState.ClearReachDecision();
            ApplyDeferredAutoSortAfterReachDecisionIfNeeded("ReachDeclared");
            EventPublisher.NotifyReachDeclared(seat, turnIndex);
            EventPublisher.NotifyTurnDebug(
                "ReachDeclared",
                $"phase={gameState.TurnPhase}; discardTile={record.Tile}",
                seat: seat,
                tile: record.Tile,
                turnIndex: turnIndex);
            return true;
        }

        private static bool IsFirstDiscardBySeat(
            MahjongGameState gameState,
            SeatId seat)
        {
            if (gameState == null || gameState.Discards == null)
                return false;

            int discardCount = 0;
            for (int i = 0; i < gameState.Discards.Count; i++)
            {
                if (gameState.Discards[i].ActorSeat != seat)
                    continue;

                discardCount++;
                if (discardCount > 1)
                    return false;
            }

            return discardCount == 1;
        }

        private static bool IsFirstTurnTsumoEligible(
            MahjongGameState gameState,
            SeatId seat,
            WinType winType)
        {
            if (winType != WinType.Tsumo ||
                gameState == null ||
                gameState.Discards == null)
            {
                return false;
            }

            bool hasAnyDiscard = false;
            for (int i = 0; i < gameState.Discards.Count; i++)
            {
                hasAnyDiscard = true;
                if (gameState.Discards[i].ActorSeat == seat)
                    return false;
            }

            return seat != SeatId.East || !hasAnyDiscard;
        }

        private static bool IsLastLiveWallDraw(
            MahjongGameState gameState,
            SeatId seat,
            Tile winningTile,
            WinType winType)
        {
            if (winType != WinType.Tsumo || gameState == null)
                return false;

            TurnDrawRecord? lastTurnDraw = gameState.LastTurnDraw;
            if (!lastTurnDraw.HasValue)
                return false;

            TurnDrawRecord record = lastTurnDraw.Value;
            return record.IsLastLiveWallDraw &&
                record.ActorSeat == seat &&
                record.TurnIndex == gameState.TurnIndex &&
                record.Tile.Equals(winningTile);
        }

        private void ExpireIppatsuAfterDiscard(
            DiscardRecord record,
            bool declaredReachNow)
        {
            if (gameState == null || declaredReachNow)
                return;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(record.ActorSeat);
            if (playerSeat.IsIppatsuEligible)
                playerSeat.ClearIppatsuEligibility();
        }

        private WinDeclarationEvaluationContext CreateWinDeclarationContext(
            PlayerSeat playerSeat,
            WinType winType,
            Tile winningTile,
            SeatId? sourceSeat,
            DiscardRecord? sourceDiscard = null)
        {
            SeatId winnerSeat = playerSeat.SeatId;
            bool isFirstTurnTsumoEligible =
                IsFirstTurnTsumoEligible(gameState, winnerSeat, winType);
            bool isLastLiveWallDraw =
                IsLastLiveWallDraw(gameState, winnerSeat, winningTile, winType);
            bool isLastLiveWallDiscard =
                winType == WinType.Ron &&
                sourceDiscard.HasValue &&
                sourceDiscard.Value.IsLastLiveWallDiscard;
            return new WinDeclarationEvaluationContext(
                playerSeat.Hand.GetTiles(),
                winningTile,
                winType,
                winnerSeat,
                sourceSeat,
                gameState.WindProgress.RoundWind,
                winnerSeat,
                playerSeat.IsReachDeclared,
                true,
                playerSeat.IsIppatsuEligible,
                playerSeat.IsDoubleReachDeclared,
                isFirstTurnTsumoEligible,
                isLastLiveWallDraw,
                isLastLiveWallDiscard);
        }

        private void SetWinDecisionPending(bool isPending, SeatId seat, int turnIndex)
        {
            if (gameState == null)
                return;

            if (isPending)
            {
                gameState.BeginWinDecision(seat, turnIndex);
                EventPublisher.NotifyTurnDebug(
                    "WinDecision",
                    $"phase={gameState.TurnPhase}",
                    seat: seat,
                    turnIndex: turnIndex);
                return;
            }

            gameState.ClearWinDecision();
        }

        private void SetWinDecisionPendingDetailed(
            SeatId seat,
            WinType winType,
            Tile winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            WinDeclarationEvaluationResult evaluationResult)
        {
            if (gameState == null)
                return;

            gameState.BeginWinDecisionDetailed(
                seat,
                winType,
                winningTile,
                sourceSeat,
                turnIndex,
                evaluationResult);
            EventPublisher.NotifyTurnDebug(
                "WinDecision",
                $"phase={gameState.TurnPhase}; winType={winType}; sourceSeat={sourceSeat}",
                seat: seat,
                tile: winningTile,
                turnIndex: turnIndex);
        }

        private void ClearWinDecision()
        {
            SetWinDecisionPending(false, default, 0);
        }

        private void EndRound(string reason)
        {
            EndRound(reason, null);
        }

        private void EndRound(string reason, System.Action afterRoundMarkedEnded)
        {
            EnsureRoundLifecycleService();
            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();
            autoSortDeferredUntilReachDecisionResolved = false;
            RoundLifecycleEndResult endResult = roundLifecycleService.EndRound(gameState, reason);
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
            if (!result.SkillWasPresent || result.ResolvedSkillEffect == null)
                return;

            ActiveSkillEffect effect = result.ResolvedSkillEffect;
            EventPublisher.NotifySkillEffectResolved(result);
            EventPublisher.NotifySkillEffectExpired(effect, "ConsumedByDraw");
        }

        private void CacheReferences()
        {
            if (eventNotifier == null)
                eventNotifier = GetComponent<MahjongEventNotifier>();

            if (cpuTurnController == null)
                cpuTurnController = GetComponent<CpuTurnController>();
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
        }

        private void EnsureCpuTurnController()
        {
            if (cpuTurnController != null)
                return;

            // PROTOTYPE: Ensure the local prototype can run CPU turns without scene migration.
            cpuTurnController = gameObject.AddComponent<CpuTurnController>();
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

        private bool IsActiveSeat(SeatId seat)
        {
            if (gameState == null)
                return false;

            for (int i = 0; i < gameState.ActiveTurnSeats.Count; i++)
            {
                if (gameState.ActiveTurnSeats[i] == seat)
                    return true;
            }

            return false;
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
            if (ShouldDeferAutoSortUntilReachDecisionResolved(seat))
            {
                autoSortDeferredUntilReachDecisionResolved = true;
                return;
            }

            gameState.GetPlayerSeat(seat).Hand.SortByTypeIndex();
            EventPublisher.NotifyHandAutoSortedDetailed(seat, gameState.TurnIndex, reason);

            if (notify)
                EventPublisher.NotifyHandAutoSorted(seat, gameState.TurnIndex);
        }

        private bool ShouldDeferAutoSortUntilReachDecisionResolved(SeatId seat)
        {
            return gameState != null &&
                gameState.IsSelfSeat(seat) &&
                (gameState.IsReachDecisionPending || gameState.IsReachDiscardSelectionPending);
        }

        private void ApplyDeferredAutoSortAfterReachDecisionIfNeeded(string reason)
        {
            if (!autoSortDeferredUntilReachDecisionResolved ||
                gameState == null ||
                gameState.IsReachDecisionPending ||
                gameState.IsReachDiscardSelectionPending)
            {
                return;
            }

            autoSortDeferredUntilReachDecisionResolved = false;
            if (autoSortEnabled)
                ApplyAutoSort(gameState.SelfSeat, reason, true);
        }

        private void Warn(string message)
        {
            if (!logWarnings)
                return;

            Debug.LogWarning($"{nameof(MahjongGameFlow)}: {message}", this);
        }

    }
}
