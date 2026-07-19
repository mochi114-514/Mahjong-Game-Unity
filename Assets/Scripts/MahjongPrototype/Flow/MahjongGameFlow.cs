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
    public sealed class MahjongGameFlow : MonoBehaviour,
        ICpuTurnGateway,
        IMahjongAuthorityCommandPort,
        IMahjongAuthorityDecisionPort,
        IReactionDecisionResponseRejectionHandler
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
        [SerializeField, Min(0f)] private float roundProgressCompletionDelaySeconds = 0.5f;
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
        private readonly KanService kanService = new KanService();
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
        private Coroutine pendingRoundSetupCoroutine;
        private MahjongGameState pendingRoundSetupState;
        private WindProgress pendingRoundSetupProgress;
        private bool isRoundSetupPending;
        private bool roundProgressPlaybackStarted;
        private bool roundProgressPlaybackCompleted;
        private int roundSetupSequence;
        private readonly MatchStartValidator matchStartValidator = new MatchStartValidator();
        private MatchRoster matchRoster;
        private DecisionProviderRegistry decisionProviderRegistry;
        private DecisionCoordinator decisionCoordinator;
        private readonly ReactionWindowSeatAnswerResolver
            reactionWindowSeatAnswerResolver = new ReactionWindowSeatAnswerResolver();
        private readonly ReactionWindowSeatAnswerDeclarationService
            reactionWindowSeatAnswerDeclarationService =
                new ReactionWindowSeatAnswerDeclarationService();
        private readonly Dictionary<long, PendingReactionDecision>
            pendingReactionDecisionsByRequestId =
                new Dictionary<long, PendingReactionDecision>();
        private readonly Dictionary<long, DecisionRequest>
            pendingDecisionRequestsById =
                new Dictionary<long, DecisionRequest>();
        private readonly HashSet<SeatId> notifiedReactionAnswerSeats =
            new HashSet<SeatId>();
        private ReactionWindowSeatAnswerCollection reactionWindowSeatAnswers;
        private ReactionWindow reactionWindowAwaitingRequestRetry;
        private int notifiedReactionAnswerWindowId;
        private long nextDecisionRequestId = 1;
        private MahjongViewContext viewContext = new MahjongViewContext(PlayerId.Player1);

        public MahjongGameState CurrentState => gameState;
        public MahjongEventNotifier EventNotifier => eventNotifier;
        public MatchRoster MatchRoster => matchRoster;
        public DecisionProviderRegistry DecisionProviderRegistry => decisionProviderRegistry;
        public MahjongViewContext ViewContext => viewContext;
        public DecisionCoordinator DecisionCoordinator => decisionCoordinator;
        public MatchStartValidationResult LastMatchStartValidationResult { get; private set; }
        public bool IsWinDecisionPending => gameState != null && gameState.IsWinDecisionPending;
        public bool IsAutoSortEnabled => autoSortEnabled;
        public bool IsInteractionLocked =>
            isRoundSetupPending ||
            (gameState != null && gameState.IsInteractionLocked);
        public bool IsRoundSetupPending => isRoundSetupPending;

        /// <summary>
        /// Returns the immutable reaction request currently waiting for the
        /// given local player. This intentionally exposes no ReactionWindow or
        /// ReactionWindowCandidate to the UI/provider layer.
        /// </summary>
        public bool TryGetPendingReactionDecisionRequest(
            PlayerId playerId,
            out DecisionRequest request)
        {
            request = null;
            if (gameState == null || reactionWindowSeatAnswers == null ||
                !ReferenceEquals(
                    gameState.CurrentReactionWindow,
                    reactionWindowSeatAnswers.ReactionWindow))
            {
                return false;
            }

            foreach (PendingReactionDecision pending in
                     pendingReactionDecisionsByRequestId.Values)
            {
                if (pending.Request.PlayerId == playerId)
                {
                    request = pending.Request;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the immutable request currently owned by one player. UI and
        /// providers use this projection instead of reading mutable decision
        /// state directly from the game flow.
        /// </summary>
        public bool TryGetPendingDecisionRequest(
            PlayerId playerId,
            DecisionKind kind,
            out DecisionRequest request)
        {
            request = null;
            if (kind == DecisionKind.Reaction)
                return TryGetPendingReactionDecisionRequest(playerId, out request);

            foreach (DecisionRequest pending in pendingDecisionRequestsById.Values)
            {
                if (pending.PlayerId == playerId && pending.Kind == kind)
                {
                    request = pending;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gives the local UI its configured provider endpoint, so the UI can
        /// submit an immutable response through the coordinator instead of
        /// invoking a direct reaction declaration API.
        /// </summary>
        public bool TryGetLocalUiDecisionProvider(
            PlayerId playerId,
            out LocalUiDecisionProvider provider)
        {
            provider = null;
            return decisionProviderRegistry != null &&
                decisionProviderRegistry.TryResolve(
                    playerId,
                    out DecisionProviderRegistration registration) &&
                registration.Provider is LocalUiDecisionProvider localProvider &&
                (provider = localProvider) != null;
        }

        private MahjongFlowEventPublisher EventPublisher =>
            eventPublisher ??= new MahjongFlowEventPublisher(() => eventNotifier, Warn);

        bool ICpuTurnGateway.RequestDrawForCpu(PlayerId playerId, SeatId seat, int turnIndex)
        {
            return TryExecuteCommand(new MahjongAuthorityCommand(
                MahjongAuthorityCommandKind.Draw,
                playerId,
                seat,
                turnIndex)).Accepted;
        }

        bool ICpuTurnGateway.RequestDiscardDrawnTileForCpu(
            PlayerId playerId,
            SeatId seat,
            int turnIndex)
        {
            return TryExecuteCommand(new MahjongAuthorityCommand(
                MahjongAuthorityCommandKind.DiscardDrawnTile,
                playerId,
                seat,
                turnIndex)).Accepted;
        }

        bool ICpuTurnGateway.IsSameGameStateAndTurn(
            MahjongGameState expectedGameState,
            PlayerId playerId,
            SeatId seat,
            int turnIndex)
        {
            return ReferenceEquals(gameState, expectedGameState) &&
                expectedGameState != null &&
                expectedGameState.CurrentTurn == seat &&
                expectedGameState.TurnIndex == turnIndex &&
                expectedGameState.GetSeatSlot(seat).PlayerId == playerId;
        }

        public MahjongAuthorityCommandResult TryExecuteCommand(MahjongAuthorityCommand command)
        {
            if (gameState == null)
                return MahjongAuthorityCommandResult.Rejected("GameStateMissing");
            if (!System.Enum.IsDefined(typeof(SeatId), command.ActorSeat))
                return MahjongAuthorityCommandResult.Rejected("PlayerSeatMismatch");

            SeatSlot actorSlot = gameState.GetSeatSlot(command.ActorSeat);
            if (!actorSlot.HasPlayer || actorSlot.PlayerId != command.PlayerId)
                return MahjongAuthorityCommandResult.Rejected("PlayerSeatMismatch");
            if (gameState.TurnIndex != command.TurnIndex)
                return MahjongAuthorityCommandResult.Rejected("StaleTurnIndex");
            if (command.Kind != MahjongAuthorityCommandKind.ForceDrawSkill &&
                gameState.CurrentTurn != command.ActorSeat)
                return MahjongAuthorityCommandResult.Rejected("NotCurrentTurn");

            bool accepted;
            switch (command.Kind)
            {
                case MahjongAuthorityCommandKind.Draw:
                    accepted = TryRequestDrawForSeat(command.ActorSeat);
                    break;
                case MahjongAuthorityCommandKind.DiscardHand:
                    accepted = TryRequestDiscardForSeat(command.ActorSeat, command.HandIndex);
                    break;
                case MahjongAuthorityCommandKind.DiscardDrawnTile:
                    accepted = TryRequestDiscardDrawnTileForSeat(command.ActorSeat);
                    break;
                case MahjongAuthorityCommandKind.DeclareWin:
                    return MahjongAuthorityCommandResult.Rejected(
                        "WinDeclarationRequiresDecisionResponse");
                case MahjongAuthorityCommandKind.CancelReachDiscardSelection:
                    accepted = TryCancelReachDiscardSelectionForSeat(command.ActorSeat);
                    break;
                case MahjongAuthorityCommandKind.ForceDrawSkill:
                    accepted = TryRequestForceDrawSkillCommand(
                        command.ActorSeat,
                        command.TextPayload);
                    break;
                default:
                    return MahjongAuthorityCommandResult.Rejected("UnsupportedCommand");
            }

            return accepted
                ? MahjongAuthorityCommandResult.Succeeded()
                : MahjongAuthorityCommandResult.Rejected("CommandRejectedByAuthority");
        }

        public DecisionResponseResult TryExecuteDecisionResponse(DecisionResponse response)
        {
            if (response == null)
                return DecisionResponseResult.Rejected("DecisionResponseMissing");
            if (gameState == null)
                return DecisionResponseResult.Rejected("GameStateMissing");
            if (!System.Enum.IsDefined(typeof(SeatId), response.ActorSeat))
                return DecisionResponseResult.Rejected("PlayerSeatMismatch");

            SeatSlot actorSlot = gameState.GetSeatSlot(response.ActorSeat);
            if (!actorSlot.HasPlayer || actorSlot.PlayerId != response.PlayerId)
                return DecisionResponseResult.Rejected("PlayerSeatMismatch");

            // A reaction is owned by the reaction window, not the active turn.
            // It must therefore be handled before the normal current-turn
            // identity checks used by existing decision kinds.
            if (response.Kind == DecisionKind.Reaction)
                return TryExecuteReactionDecisionResponse(response);

            if (gameState.CurrentTurn != response.ActorSeat)
                return DecisionResponseResult.Rejected("NotCurrentTurn");
            if (gameState.TurnIndex != response.TurnIndex)
                return DecisionResponseResult.Rejected("StaleTurnIndex");

            if (!pendingDecisionRequestsById.TryGetValue(
                    response.RequestId,
                    out DecisionRequest request))
            {
                return DecisionResponseResult.Rejected("DecisionRequestMissingOrCancelled");
            }
            if (!MatchesDecisionIdentity(request, response))
                return DecisionResponseResult.Rejected("DecisionResponseIdentityMismatch");
            if (!request.TryValidateResponsePayload(response, out string validationReason))
                return DecisionResponseResult.Rejected(validationReason);

            switch (response.Kind)
            {
                case DecisionKind.WinDeclaration:
                    return TryExecuteWinDeclarationDecisionResponse(request, response);
                case DecisionKind.Reach:
                    return TryExecuteReachDecisionResponse(request, response);
                case DecisionKind.SelfKan:
                    return TryExecuteSelfKanDecisionResponse(request, response);
                default:
                    return DecisionResponseResult.Rejected("DecisionKindNotIntegrated");
            }
        }

        private DecisionResponseResult TryExecuteReactionDecisionResponse(
            DecisionResponse response)
        {
            if (!pendingReactionDecisionsByRequestId.TryGetValue(
                    response.RequestId,
                    out PendingReactionDecision pending))
            {
                return DecisionResponseResult.Rejected("ReactionDecisionRequestMissing");
            }

            DecisionRequest request = pending.Request;
            if (!MatchesDecisionIdentity(request, response))
                return DecisionResponseResult.Rejected("DecisionResponseIdentityMismatch");
            if (!request.TryValidateResponsePayload(response, out string validationReason))
                return DecisionResponseResult.Rejected(validationReason);

            ReactionWindow reactionWindow = gameState.CurrentReactionWindow;
            if (reactionWindowSeatAnswers == null || reactionWindow == null ||
                !ReferenceEquals(reactionWindow, pending.ReactionWindow) ||
                !ReferenceEquals(
                    reactionWindow,
                    reactionWindowSeatAnswers.ReactionWindow) ||
                !reactionWindow.IsAcceptingAnswers ||
                request.Reaction == null ||
                request.Reaction.WindowId != reactionWindow.WindowId)
            {
                ClearReactionSeatAnswerState(pending.WindowId);
                return DecisionResponseResult.Rejected("ReactionWindowStale");
            }

            ReactionDecisionResponse reaction = response.Reaction;
            ReactionWindowSeatAnswer answer = new ReactionWindowSeatAnswer(
                reaction.WindowId,
                response.ActorSeat,
                reaction.Kind,
                reaction.ChiOptionId);
            ReactionWindowSeatAnswerRegistrationResult registration =
                reactionWindowSeatAnswers.TryRegister(answer);
            if (!registration.Accepted)
                return DecisionResponseResult.Rejected(registration.Reason);

            pendingReactionDecisionsByRequestId.Remove(response.RequestId);
            ReactionWindowCandidate answeredCandidate = null;
            if (answer.Kind != ReactionWindowSeatAnswerKind.Pass)
            {
                reactionWindowSeatAnswers.TryGetDeclaredCandidate(
                    answer,
                    out answeredCandidate);
            }

            ReactionWindowAnswerResult answeredNotification =
                ReactionWindowAnswerResult.AcceptedAnswer(
                    reactionWindow.WindowId,
                    answeredCandidate,
                    ReactionWindowResolution.Pending(
                        reactionWindow.WindowId,
                        reactionWindow.Source));

            if (reactionWindowSeatAnswers.HasUnansweredSeats)
            {
                NotifyReactionWindowAnsweredOnce(answer.Seat, answeredNotification);
                return DecisionResponseResult.Succeeded();
            }

            ReactionWindowSeatAnswerResolution answerResolution =
                reactionWindowSeatAnswerResolver.Resolve(
                    reactionWindowSeatAnswers,
                    gameState.ActiveTurnSeats);
            ReactionWindowSeatAnswerPreparationResult preparation =
                reactionWindowSeatAnswerDeclarationService.Prepare(
                    gameState,
                    reactionWindowSeatAnswers,
                    answerResolution);
            if (!preparation.Prepared)
            {
                bool retryScheduled = TryScheduleReactionSeatAnswerRequestRetry(
                    reactionWindow,
                    out string retryReason);
                NotifyTurnBlocked(
                    "ReactionBlocked",
                    retryScheduled || string.IsNullOrEmpty(retryReason)
                        ? preparation.Reason
                        : retryReason);
                return DecisionResponseResult.Rejected(preparation.Reason);
            }

            ReactionWindowSeatAnswerCommitResult commit =
                reactionWindowSeatAnswerDeclarationService.Commit(
                    gameState,
                    preparation.PreparedDeclaration);
            if (!commit.Committed)
            {
                bool retryScheduled = TryScheduleReactionSeatAnswerRequestRetry(
                    reactionWindow,
                    out string retryReason);
                NotifyTurnBlocked(
                    "ReactionBlocked",
                    retryScheduled || string.IsNullOrEmpty(retryReason)
                        ? commit.Reason
                        : retryReason);
                return DecisionResponseResult.Rejected(commit.Reason);
            }

            NotifyReactionWindowAnsweredOnce(answer.Seat, answeredNotification);
            winDecisionService.ApplyDeclinedReactionRonFuriten(
                gameState,
                reactionWindowSeatAnswers);
            if (!ResolveReactionWindow(commit.Resolution))
                return DecisionResponseResult.Rejected("ReactionWindowResolutionFailed");

            return DecisionResponseResult.Succeeded();
        }

        private DecisionResponseResult TryExecuteWinDeclarationDecisionResponse(
            DecisionRequest request,
            DecisionResponse response)
        {
            if (request.WinDeclaration == null ||
                !gameState.IsWinDecisionPending ||
                gameState.WinDecisionSeat != response.ActorSeat ||
                gameState.WinDecisionTurnIndex != response.TurnIndex ||
                gameState.WinDecisionType != request.WinDeclaration.WinType ||
                gameState.WinningTile != request.WinDeclaration.WinningTile ||
                gameState.WinSourceSeat != request.WinDeclaration.SourceSeat ||
                request.WinDeclaration.WinType != WinType.Tsumo)
            {
                return DecisionResponseResult.Rejected("WinDecisionStale");
            }

            pendingDecisionRequestsById.Remove(request.RequestId);
            if (response.Accepted)
            {
                return TryRequestDeclareWinForSeat(response.ActorSeat)
                    ? DecisionResponseResult.Succeeded()
                    : DecisionResponseResult.Rejected("WinDeclarationRejected");
            }

            return TryDeclineWinDecisionForSeat(response.ActorSeat)
                ? DecisionResponseResult.Succeeded()
                : DecisionResponseResult.Rejected("WinDecisionStale");
        }

        private DecisionResponseResult TryExecuteReachDecisionResponse(
            DecisionRequest request,
            DecisionResponse response)
        {
            if (request.Reach == null || !gameState.IsReachDecisionPending ||
                gameState.ReachDecisionSeat != response.ActorSeat ||
                gameState.ReachDecisionTurnIndex != response.TurnIndex)
            {
                return DecisionResponseResult.Rejected("ReachDecisionStale");
            }

            pendingDecisionRequestsById.Remove(request.RequestId);
            EnsureDecisionServices();
            if (response.Accepted)
            {
                ReachDecisionResult result = reachDecisionService.BeginDiscardSelection(
                    gameState,
                    response.ActorSeat);
                if (!result.Success)
                    return DecisionResponseResult.Rejected(result.FailureReason);

                EventPublisher.NotifyTurnDebug(
                    "ReachDiscardSelection",
                    $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                    seat: result.Seat,
                    turnIndex: result.TurnIndex);
                EventPublisher.NotifyReachDiscardSelectionStarted(result.Seat, result.TurnIndex);
                return DecisionResponseResult.Succeeded();
            }

            ReachDecisionResult declined = reachDecisionService.Decline(gameState);
            if (!declined.Success)
                return DecisionResponseResult.Rejected(declined.FailureReason);

            ApplyDeferredAutoSortAfterReachDecisionIfNeeded("ReachDeclined");
            EventPublisher.NotifyTurnDebug(
                "ReachDeclined",
                $"phase={gameState.TurnPhase}",
                seat: declined.Seat,
                turnIndex: declined.TurnIndex);
            EventPublisher.NotifyReachDeclined(declined.Seat, declined.TurnIndex);
            return DecisionResponseResult.Succeeded();
        }

        private DecisionResponseResult TryExecuteSelfKanDecisionResponse(
            DecisionRequest request,
            DecisionResponse response)
        {
            if (request.SelfKan == null || !gameState.IsSelfKanDecisionPending ||
                gameState.CurrentSelfKanDecision == null ||
                gameState.CurrentSelfKanDecision.Seat != response.ActorSeat ||
                gameState.CurrentSelfKanDecision.TurnIndex != response.TurnIndex)
            {
                return DecisionResponseResult.Rejected("SelfKanDecisionStale");
            }

            pendingDecisionRequestsById.Remove(request.RequestId);
            if (!response.Accepted)
            {
                return TryDeclineSelfKanDecisionForSeat(response.ActorSeat)
                    ? DecisionResponseResult.Succeeded()
                    : DecisionResponseResult.Rejected("SelfKanDecisionStale");
            }

            if (response.SelfKan == null ||
                !request.SelfKan.TryGetOption(response.SelfKan.OptionId, out SelfKanDecisionOption option) ||
                option.OptionId < 0 ||
                option.OptionId >= gameState.CurrentSelfKanDecision.Candidates.Count)
            {
                return DecisionResponseResult.Rejected("SelfKanOptionMissing");
            }

            SelfKanCandidate candidate =
                gameState.CurrentSelfKanDecision.Candidates[option.OptionId];
            if (candidate == null || candidate.Kind != option.Kind ||
                candidate.Tile != option.Tile ||
                candidate.AddedTileLocation != option.AddedTileLocation ||
                candidate.SourcePonMeldIndex != option.SourcePonMeldIndex ||
                candidate.Seat != response.ActorSeat ||
                candidate.TurnIndex != response.TurnIndex)
            {
                return DecisionResponseResult.Rejected("SelfKanDecisionStale");
            }

            return TryRequestDeclareSelfKanForSeat(response.ActorSeat, candidate)
                ? DecisionResponseResult.Succeeded()
                : DecisionResponseResult.Rejected("SelfKanDeclarationRejected");
        }

        public void HandleRejectedReactionDecisionResponse(
            DecisionResponse response,
            DecisionResponseResult result)
        {
            if (response == null || response.Kind != DecisionKind.Reaction ||
                result.Accepted ||
                !pendingReactionDecisionsByRequestId.TryGetValue(
                    response.RequestId,
                    out PendingReactionDecision pending))
            {
                return;
            }

            bool retryScheduled = TryScheduleReactionSeatAnswerRequestRetry(
                pending.ReactionWindow,
                out string retryReason);
            if (!retryScheduled && !string.IsNullOrEmpty(retryReason) &&
                retryReason != "ReactionWindowStale")
            {
                NotifyTurnBlocked("ReactionBlocked", retryReason);
            }
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

        private void Update()
        {
            // Provider callbacks may be synchronous. Pumping only from the
            // authority update keeps them from re-entering authority logic.
            decisionCoordinator?.Pump();
            TryResumeReactionSeatAnswerRequests();
        }

        private void OnDisable()
        {
            CancelPendingRoundSetup();
            CancelPendingDecisions();
            CancelPendingAutoDiscardDrawnTile();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeParticipantCount();
            initialHandTileCount = Mathf.Max(1, initialHandTileCount);
            roundProgressCompletionDelaySeconds = Mathf.Max(0f, roundProgressCompletionDelaySeconds);
            autoDiscardDrawnTileDelaySeconds = Mathf.Max(0f, autoDiscardDrawnTileDelaySeconds);
        }
#endif

        [ContextMenu("Prototype/Start New Round")]
        public void StartNewRound()
        {
            TryStartNewRound();
        }

        /// <summary>
        /// Starts a new match round after validating the match-lifetime roster
        /// and answer routes. The existing void entry point remains for scene
        /// and UI compatibility; callers that need the reason can use this API.
        /// </summary>
        public MatchStartValidationResult TryStartNewRound()
        {
            CancelPendingDecisions();
            EnsureRoundLifecycleService();
            SeatId initialSelfSeat = ResolveSelfSeat();
            return StartRound(
                roundLifecycleService.GetInitialWindProgress(),
                true,
                initialSelfSeat);
        }

        /// <summary>
        /// Replaces the match-lifetime configuration. It is intentionally only
        /// consumed when starting a round; non-reaction decision handling
        /// remains on the legacy paths during this migration phase.
        /// </summary>
        public void ConfigureMatch(
            MatchRoster roster,
            DecisionProviderRegistry providerRegistry)
        {
            CancelPendingDecisions();
            matchRoster = roster ?? throw new System.ArgumentNullException(nameof(roster));
            decisionProviderRegistry = providerRegistry ??
                throw new System.ArgumentNullException(nameof(providerRegistry));
            decisionCoordinator = null;
        }

        public void ConfigureViewContext(MahjongViewContext context)
        {
            viewContext = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        private MatchStartValidationResult StartRound(
            WindProgress windProgress,
            bool notifyRunStarted,
            SeatId selfSeat)
        {
            CancelPendingRoundSetup();
            EnsureMatchConfiguration();
            MatchStartValidationResult validationResult = matchStartValidator.Validate(
                matchRoster,
                decisionProviderRegistry);
            LastMatchStartValidationResult = validationResult;
            if (!validationResult.IsValid)
            {
                Warn($"Match start rejected: {validationResult.FailureReason}");
                return validationResult;
            }

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
            EnsureDecisionCoordinator();
            cpuTurnController?.CancelPendingTurn();
            CancelPendingDecisions();
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
                matchRoster,
                decisionProviderRegistry);
            gameState = setupResult.GameState;

            BeginWaitingForRoundProgress(gameState);
            EventPublisher.NotifySeatSlotsAssigned();
            EventPublisher.NotifyRoundStarted(gameState.TurnIndex, gameState.Wall.Count);

            if (!Application.isPlaying)
            {
                CompletePendingRoundSetupImmediately(gameState, roundSetupSequence);
            }
            else
            {
                pendingRoundSetupCoroutine = StartCoroutine(
                    CompleteRoundSetupAfterRoundProgress(gameState, roundSetupSequence));
            }

            return validationResult;
        }

        /// <summary>
        /// Called by the presentation layer after it accepted the current round-start animation.
        /// </summary>
        public void NotifyRoundProgressPlaybackStarted(WindProgress progress)
        {
            if (!IsAwaitingRoundProgress(progress))
                return;

            roundProgressPlaybackStarted = true;
        }

        /// <summary>
        /// Called by the presentation layer when the current round-start animation has finished.
        /// </summary>
        public void NotifyRoundProgressCompleted(WindProgress progress)
        {
            if (!IsAwaitingRoundProgress(progress))
                return;

            roundProgressPlaybackCompleted = true;
        }

        private void BeginWaitingForRoundProgress(MahjongGameState state)
        {
            pendingRoundSetupState = state;
            pendingRoundSetupProgress = state.WindProgress;
            isRoundSetupPending = true;
            roundProgressPlaybackStarted = false;
            roundProgressPlaybackCompleted = false;
            roundSetupSequence++;
        }

        private IEnumerator CompleteRoundSetupAfterRoundProgress(
            MahjongGameState state,
            int sequence)
        {
            // Let RoundStarted subscribers synchronously report whether a presentation began.
            yield return null;

            if (!IsPendingRoundSetup(state, sequence))
                yield break;

            // A missing UI host/controller cannot block game progression.
            if (!roundProgressPlaybackStarted)
                roundProgressPlaybackCompleted = true;

            while (IsPendingRoundSetup(state, sequence) && !roundProgressPlaybackCompleted)
                yield return null;

            if (!IsPendingRoundSetup(state, sequence))
                yield break;

            if (roundProgressCompletionDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(roundProgressCompletionDelaySeconds);

            CompletePendingRoundSetupImmediately(state, sequence);
        }

        private void CompletePendingRoundSetupImmediately(MahjongGameState state, int sequence)
        {
            if (!IsPendingRoundSetup(state, sequence))
                return;

            ClearPendingRoundSetupState();
            DealInitialHands();
            if (gameState == null || gameState.IsRoundEnded)
                return;

            EventPublisher.NotifyRoundSetupCompleted();
            StartTurn(gameState.CurrentTurn, gameState.TurnIndex);
        }

        private bool IsAwaitingRoundProgress(WindProgress progress)
        {
            return isRoundSetupPending &&
                pendingRoundSetupState == gameState &&
                pendingRoundSetupProgress.Equals(progress);
        }

        private bool IsPendingRoundSetup(MahjongGameState state, int sequence)
        {
            return isRoundSetupPending &&
                pendingRoundSetupState == state &&
                gameState == state &&
                roundSetupSequence == sequence;
        }

        private void CancelPendingRoundSetup()
        {
            if (pendingRoundSetupCoroutine != null)
            {
                StopCoroutine(pendingRoundSetupCoroutine);
                pendingRoundSetupCoroutine = null;
            }

            ClearPendingRoundSetupState();
            roundSetupSequence++;
        }

        private void ClearPendingRoundSetupState()
        {
            pendingRoundSetupCoroutine = null;
            pendingRoundSetupState = null;
            isRoundSetupPending = false;
            roundProgressPlaybackStarted = false;
            roundProgressPlaybackCompleted = false;
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

            EnsureMatchConfiguration();
            MatchStartValidationResult validationResult = matchStartValidator.Validate(
                matchRoster,
                decisionProviderRegistry);
            LastMatchStartValidationResult = validationResult;
            if (!validationResult.IsValid)
            {
                Warn($"Next round start rejected: {validationResult.FailureReason}");
                return;
            }

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

            TryDrawForSeatByPhase(
                gameState.SelfSeat,
                "DrawCompleted",
                "DrawBlocked",
                true);
        }

        public bool TryRequestDrawForSeat(SeatId actorSeat)
        {
            if (!CanUseGameState())
                return false;

            return TryDrawForSeatByPhase(
                actorSeat,
                "DrawCompleted",
                "DrawBlocked",
                false);
        }

        private bool TryDrawForSeatByPhase(
            SeatId seat,
            string completedEventName,
            string blockedEventName,
            bool warnOnBlocked)
        {
            if (gameState.TurnPhase == TurnPhase.WaitingForRinshanDraw)
            {
                return TryDrawRinshanForSeat(
                    seat,
                    completedEventName,
                    blockedEventName,
                    warnOnBlocked,
                    out _);
            }

            return TryDrawForSeat(
                seat,
                completedEventName,
                blockedEventName,
                warnOnBlocked);
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

            TryRequestDiscardForSeatInternal(gameState.SelfSeat, handIndex, true);
        }

        public bool TryRequestDiscardForSeat(SeatId actorSeat, int handIndex)
        {
            return TryRequestDiscardForSeatInternal(actorSeat, handIndex, false);
        }

        private bool TryRequestDiscardForSeatInternal(
            SeatId actorSeat,
            int handIndex,
            bool warnOnBlocked)
        {
            if (!CanUseGameState())
                return false;

            if (gameState.CurrentTurn != actorSeat)
            {
                if (warnOnBlocked)
                    Warn("Only the current seat can discard.");

                NotifyTurnBlocked("DiscardBlocked", "NotCurrentTurn");
                return false;
            }

            if (gameState.IsRoundEnded)
            {
                if (warnOnBlocked)
                    Warn("Round already ended. Press Retry.");

                NotifyTurnBlocked("DiscardBlocked", "RoundEnded");
                return false;
            }

            PlayerSeat actorPlayerSeat = gameState.GetPlayerSeat(actorSeat);
            bool isPostCallDiscard =
                gameState.TurnPhase == TurnPhase.WaitingForDiscardAfterCall;
            if (!actorPlayerSeat.HasDrawnTile && !isPostCallDiscard)
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
                gameState.TurnPhase != TurnPhase.ReachDiscardSelection &&
                !isPostCallDiscard)
            {
                if (warnOnBlocked)
                    Warn("Discard is not available in the current turn phase.");

                NotifyTurnBlocked("DiscardBlocked", "InvalidTurnPhase");
                return false;
            }

            if (!IsValidReachDiscardCandidate(actorSeat, DiscardSource.Hand, handIndex))
            {
                if (warnOnBlocked)
                    Warn("Only reach discard candidates can be discarded.");

                NotifyTurnBlocked("DiscardBlocked", "ReachDiscardCandidateMissing");
                return false;
            }

            if (actorPlayerSeat.IsReachDeclared)
            {
                if (warnOnBlocked)
                    Warn("Hand discards are locked after reach.");

                NotifyTurnBlocked("DiscardBlocked", "ReachDeclaredHandLocked");
                return false;
            }

            DiscardResult result = discardService.DiscardTile(gameState, actorSeat, handIndex);
            if (!result.Success)
            {
                if (warnOnBlocked)
                    Warn(result.Reason);

                return false;
            }

            CommitDrawnTileToHandIfPresent(actorSeat);
            bool declaredReachNow = CompleteReachDeclarationIfPending(result.Record);
            gameState.EnterWaitingForDraw();
            ExpireIppatsuAfterDiscard(result.Record, declaredReachNow);
            CompleteDiscard(result.Record);
            return true;
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
            if (!CanUseGameState())
                return;

            SeatId selfSeat = gameState.SelfSeat;
            if (viewContext != null && viewContext.TryGetSelfSeat(gameState, out SeatId viewSelfSeat))
                selfSeat = viewSelfSeat;

            RequestForceDrawSkillForSeat(selfSeat, targetTileCode);
        }

        public void RequestForceDrawSkillForSeat(SeatId ownerSeat, string targetTileCode)
        {
            if (!CanUseGameState())
                return;

            SeatSlot slot = gameState.GetSeatSlot(ownerSeat);
            if (!slot.HasPlayer)
                return;

            TryExecuteCommand(new MahjongAuthorityCommand(
                MahjongAuthorityCommandKind.ForceDrawSkill,
                slot.PlayerId.Value,
                ownerSeat,
                gameState.TurnIndex,
                textPayload: targetTileCode));
        }

        public bool CanRequestForceDrawSkillForSeat(SeatId ownerSeat)
        {
            if (gameState == null || isRoundSetupPending)
                return false;

            EnsureSkillFlowService();
            return skillFlowService.CanRequestForceDraw(gameState, ownerSeat);
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

            if (TrySubmitLegacyDecisionResponse(
                    gameState.SelfSeat,
                    DecisionKind.WinDeclaration,
                    true))
            {
                return;
            }

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
            if (IsReactionSeatAnswerPathActiveOrRetryScheduled() &&
                !TryEnterLegacyReactionWindowPath(
                    actorSeat,
                    reactionWindowId,
                    ReactionKind.Ron,
                    true,
                    out string legacyReason))
            {
                NotifyTurnBlocked("ReactionBlocked", legacyReason);
                return false;
            }

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

            return CompleteReactionWindowAnswer(answer);
        }

        public bool TryRequestDeclineRonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            if (!CanUseGameState())
                return false;
            if (IsReactionSeatAnswerPathActiveOrRetryScheduled() &&
                !TryEnterLegacyReactionWindowPath(
                    actorSeat,
                    reactionWindowId,
                    ReactionKind.Ron,
                    true,
                    out string legacyReason))
            {
                NotifyTurnBlocked("ReactionBlocked", legacyReason);
                return false;
            }

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

            return CompleteReactionWindowAnswer(
                answer,
                () =>
                {
                    EventPublisher.NotifyWinDeclined(
                        answer.Candidate.Seat,
                        answer.Resolution.Source.TurnIndex);
                    EventPublisher.NotifyWinDeclinedDetailed(
                        answer.Candidate.Seat,
                        WinType.Ron,
                        answer.Resolution.Source.TurnIndex);
                });
        }

        public bool TryRequestDeclarePonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            return TryRequestDeclareMeldCallForSeat(
                actorSeat,
                reactionWindowId,
                MeldCallKind.Pon,
                0);
        }

        public bool TryRequestDeclareDaiminkanForSeat(
            SeatId actorSeat,
            int reactionWindowId)
        {
            return TryRequestDeclareMeldCallForSeat(
                actorSeat,
                reactionWindowId,
                MeldCallKind.Kan,
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
            if (IsReactionSeatAnswerPathActiveOrRetryScheduled())
            {
                if (!TryGetReactionKind(kind, out ReactionKind reactionKind))
                {
                    NotifyTurnBlocked("ReactionBlocked", "MeldCallKindUnsupported");
                    return false;
                }

                if (!IsLegacyMeldCallAvailable(
                        gameState.CurrentReactionWindow,
                        actorSeat,
                        reactionKind))
                {
                    NotifyTurnBlocked("ReactionBlocked", "MeldCallKindUnavailable");
                    return false;
                }

                if (!TryEnterLegacyReactionWindowPath(
                        actorSeat,
                        reactionWindowId,
                        reactionKind,
                        false,
                        out string legacyReason))
                {
                    NotifyTurnBlocked("ReactionBlocked", legacyReason);
                    return false;
                }
            }

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

            return CompleteReactionWindowAnswer(answer);
        }

        public IReadOnlyList<Tile> GetAnkanCandidatesForSeat(SeatId actorSeat)
        {
            return gameState == null
                ? System.Array.Empty<Tile>()
                : kanService.CollectAnkanCandidates(gameState, actorSeat);
        }

        public IReadOnlyList<SelfKanCandidate> GetSelfKanCandidatesForSeat(SeatId actorSeat)
        {
            if (gameState == null)
                return System.Array.Empty<SelfKanCandidate>();

            if (gameState.IsSelfKanDecisionPending &&
                gameState.CurrentSelfKanDecision != null &&
                gameState.CurrentSelfKanDecision.Seat == actorSeat)
            {
                return gameState.CurrentSelfKanDecision.Candidates;
            }

            return kanService.CollectSelfKanCandidates(gameState, actorSeat);
        }

        public bool TryRequestDeclareAnkanForSeat(SeatId actorSeat, int tileTypeIndex)
        {
            if (TrySubmitLegacySelfKanDecisionResponse(
                    actorSeat,
                    SelfKanKind.Ankan,
                    tileTypeIndex,
                    -1))
            {
                return true;
            }

            IReadOnlyList<SelfKanCandidate> candidates = GetSelfKanCandidatesForSeat(actorSeat);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Kind == SelfKanKind.Ankan &&
                    candidates[i].Tile.TypeIndex == tileTypeIndex)
                {
                    return TryRequestDeclareSelfKanForSeat(actorSeat, candidates[i]);
                }
            }

            NotifyKanBlocked(actorSeat, default, "AnkanCandidateMissing");
            return false;
        }

        public bool TryRequestDeclareAnkanForSeat(SeatId actorSeat, Tile tile)
        {
            if (TrySubmitLegacySelfKanDecisionResponse(
                    actorSeat,
                    SelfKanKind.Ankan,
                    tile.TypeIndex,
                    -1))
            {
                return true;
            }

            IReadOnlyList<SelfKanCandidate> candidates = GetSelfKanCandidatesForSeat(actorSeat);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Kind == SelfKanKind.Ankan && candidates[i].Tile == tile)
                    return TryRequestDeclareSelfKanForSeat(actorSeat, candidates[i]);
            }

            NotifyKanBlocked(actorSeat, tile, "AnkanCandidateMissing");
            return false;
        }

        public bool TryRequestDeclareKakanForSeat(
            SeatId actorSeat,
            int tileTypeIndex,
            int sourcePonMeldIndex)
        {
            if (TrySubmitLegacySelfKanDecisionResponse(
                    actorSeat,
                    SelfKanKind.Kakan,
                    tileTypeIndex,
                    sourcePonMeldIndex))
            {
                return true;
            }

            IReadOnlyList<SelfKanCandidate> candidates = GetSelfKanCandidatesForSeat(actorSeat);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Kind == SelfKanKind.Kakan &&
                    candidates[i].Tile.TypeIndex == tileTypeIndex &&
                    candidates[i].SourcePonMeldIndex == sourcePonMeldIndex)
                {
                    return TryRequestDeclareSelfKanForSeat(actorSeat, candidates[i]);
                }
            }

            NotifyKanBlocked(actorSeat, default, "KakanCandidateMissing");
            return false;
        }

        public bool TryRequestDeclareSelfKanForSeat(
            SeatId actorSeat,
            SelfKanCandidate candidate)
        {
            if (!CanUseGameState())
                return false;

            if (candidate == null || candidate.Seat != actorSeat)
            {
                NotifyKanBlocked(actorSeat, default, "SelfKanCandidateMissing");
                return false;
            }

            if (!kanService.IsCurrentSelfKanCandidate(gameState, candidate))
            {
                NotifyKanBlocked(actorSeat, candidate.Tile, "SelfKanStateChanged");
                return false;
            }

            if (gameState.IsSelfKanDecisionPending &&
                !gameState.TryAcceptSelfKanDecision(candidate))
            {
                NotifyKanBlocked(actorSeat, candidate.Tile, "SelfKanDecisionStale");
                return false;
            }

            if (candidate.Kind == SelfKanKind.Kakan)
                return TryBeginKakanReactionWindow(candidate);

            AnkanDeclarationResult result = kanService.TryDeclareAnkan(
                gameState,
                actorSeat,
                candidate.Tile);
            if (!result.Declared)
            {
                NotifyKanBlocked(actorSeat, candidate.Tile, result.Reason);
                return false;
            }

            ApplyAutoSortIfEnabled(actorSeat, "AnkanDeclared", false);
            gameState.ClearIppatsuEligibilityForAllPlayers();
            EventPublisher.NotifyTurnDebug(
                "AnkanDeclared",
                $"caller={actorSeat}; tile={candidate.Tile}; phase={gameState.TurnPhase}; liveWall={gameState.Wall.Count}; deadWall={gameState.Wall.DeadWallCount}; rinshanRemaining={gameState.Wall.RemainingRinshanTileCount}",
                seat: actorSeat,
                tile: candidate.Tile,
                turnIndex: gameState.TurnIndex);
            EventPublisher.NotifyMeldDeclared(result.Meld);
            TryAutoDrawRinshanAfterKan(actorSeat, "Ankan");
            return true;
        }

        public bool TryRequestDeclineSelfKanForSeat(SeatId actorSeat)
        {
            if (TrySubmitLegacyDecisionResponse(
                    actorSeat,
                    DecisionKind.SelfKan,
                    false))
            {
                return true;
            }

            if (!CanUseGameState() || !gameState.TryDeclineSelfKanDecision(actorSeat))
            {
                NotifyKanBlocked(actorSeat, default, "SelfKanDecisionMissing");
                return false;
            }

            return TryRequestDiscardDrawnTileForSeatInternal(actorSeat, false);
        }

        public bool TryRequestDeclinePonForSeat(SeatId actorSeat, int reactionWindowId)
        {
            if (!CanUseGameState())
                return false;
            if (IsReactionSeatAnswerPathActiveOrRetryScheduled() &&
                !TryEnterLegacyReactionWindowPath(
                    actorSeat,
                    reactionWindowId,
                    ReactionKind.Pon,
                    true,
                    out string legacyReason))
            {
                NotifyTurnBlocked("ReactionBlocked", legacyReason);
                return false;
            }

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

            return CompleteReactionWindowAnswer(answer);
        }

        public bool TryRequestDeclineMeldCallsForSeat(SeatId actorSeat, int reactionWindowId)
        {
            if (!CanUseGameState())
                return false;
            if (IsReactionSeatAnswerPathActiveOrRetryScheduled())
            {
                if (!HasAnyLegacyMeldCallAvailable(
                        gameState.CurrentReactionWindow,
                        actorSeat))
                {
                    NotifyTurnBlocked("ReactionBlocked", "MeldCallKindUnavailable");
                    return false;
                }

                if (!TryEnterLegacyReactionWindowPath(
                    actorSeat,
                    reactionWindowId,
                    null,
                    false,
                    out string legacyReason))
                {
                    NotifyTurnBlocked("ReactionBlocked", legacyReason);
                    return false;
                }
            }

            EnsureReactionWindowService();
            ReactionWindowAnswerResult answer = reactionWindowService.DeclineMeldCalls(
                gameState,
                actorSeat,
                reactionWindowId);
            if (!answer.Accepted)
            {
                NotifyTurnBlocked("ReactionBlocked", answer.Reason);
                return false;
            }

            return CompleteReactionWindowAnswer(answer);
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

            if (TrySubmitLegacyDecisionResponse(
                    gameState.WinDecisionSeat,
                    DecisionKind.WinDeclaration,
                    false))
            {
                return;
            }

            EnsureDecisionServices();
            WinDecisionDeclineResult result = winDecisionService.Decline(gameState);

            EventPublisher.NotifyWinDeclined(result.Seat, result.TurnIndex);
            EventPublisher.NotifyWinDeclinedDetailed(
                result.Seat,
                result.WinType,
                result.TurnIndex);

            if (result.WinType == WinType.Tsumo)
            {
                ContinueAfterDeclinedTsumo(result.Seat);
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

            if (TrySubmitLegacyDecisionResponse(
                    gameState.ReachDecisionSeat,
                    DecisionKind.Reach,
                    true))
            {
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

            SeatSlot slot = gameState.GetSeatSlot(gameState.SelfSeat);
            if (slot.HasPlayer)
            {
                TryExecuteCommand(new MahjongAuthorityCommand(
                    MahjongAuthorityCommandKind.CancelReachDiscardSelection,
                    slot.PlayerId.Value,
                    gameState.SelfSeat,
                    gameState.TurnIndex));
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

            if (TrySubmitLegacyDecisionResponse(
                    gameState.ReachDecisionSeat,
                    DecisionKind.Reach,
                    false))
            {
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
            CancelPendingDecisions();
            EnsureReactionWindowService();
            ReactionWindowStartResult start = reactionWindowService.Begin(gameState, record);
            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();

            if (!start.Resolution.IsResolved && start.ReactionWindow != null &&
                !TryBeginReactionSeatAnswerRequests(
                    start.ReactionWindow,
                    out string reactionRequestReason))
            {
                NotifyTurnBlocked("ReactionBlocked", reactionRequestReason);
                ScheduleReactionSeatAnswerRequestRetry(start.ReactionWindow);
            }

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

        private bool TryBeginKakanReactionWindow(SelfKanCandidate candidate)
        {
            EnsureReactionWindowService();
            ReactionWindowStartResult start = reactionWindowService.BeginChankan(
                gameState,
                candidate);
            if (start.ReactionWindow == null)
            {
                NotifyKanBlocked(candidate.Seat, candidate.Tile, "KakanWindowUnavailable");
                return false;
            }

            cpuTurnController?.CancelPendingTurn();
            CancelPendingAutoDiscardDrawnTile();
            if (!start.Resolution.IsResolved &&
                !TryBeginReactionSeatAnswerRequests(
                    start.ReactionWindow,
                    out string reactionRequestReason))
            {
                NotifyKanBlocked(candidate.Seat, candidate.Tile, reactionRequestReason);
                ScheduleReactionSeatAnswerRequestRetry(start.ReactionWindow);
            }
            EventPublisher.NotifyTurnDebug(
                "KakanDeclaredPending",
                $"windowId={start.ReactionWindow.WindowId}; caller={candidate.Seat}; tile={candidate.Tile}; candidates={start.ReactionWindow.Candidates.Count}",
                seat: candidate.Seat,
                tile: candidate.Tile,
                turnIndex: candidate.TurnIndex);
            EventPublisher.NotifyReactionWindowStarted(start.ReactionWindow);
            NotifyWinDecisionStartedIfNeeded(
                start.ReactionWindow.PendingRonCandidate != null);
            NotifyWinCheckResults(start.WinCheckNotifications);

            if (start.Resolution.IsResolved)
                return ResolveReactionWindow(start.Resolution);

            return true;
        }

        private bool CompleteReactionWindowAnswer(
            ReactionWindowAnswerResult answer,
            System.Action afterAnswered = null)
        {
            if (!answer.Accepted)
                return false;

            if (!answer.Resolution.IsResolved)
            {
                EventPublisher.NotifyReactionWindowAnswered(answer);
                afterAnswered?.Invoke();
                return true;
            }

            return ResolveReactionWindow(
                answer.Resolution,
                () =>
                {
                    EventPublisher.NotifyReactionWindowAnswered(answer);
                    afterAnswered?.Invoke();
                });
        }

        private bool ResolveReactionWindow(ReactionWindowResolution resolution)
        {
            return ResolveReactionWindow(resolution, null);
        }

        private bool ResolveReactionWindow(
            ReactionWindowResolution resolution,
            System.Action afterFinalStateEstablished)
        {
            if (!resolution.IsResolved || gameState == null ||
                !gameState.BeginReactionWindowResolution(resolution.WindowId))
            {
                return false;
            }

            // A closing window must not leave provider callbacks or immutable
            // request projections alive for delayed responses.
            ClearReactionSeatAnswerState(resolution.WindowId);

            if (!gameState.CompleteReactionWindowResolution(resolution.WindowId))
                return false;

            bool shouldAutoDrawRinshan =
                resolution.Type == ReactionWindowResolutionType.DaiminkanDeclared;
            PlayerMeld committedKakan = null;
            if (resolution.Source.IsKakan &&
                resolution.Type == ReactionWindowResolutionType.NoReaction)
            {
                ReactionWindow closedWindow = gameState.CurrentReactionWindow;
                string kakanReason = "KakanWindowMissing";
                if (closedWindow == null || !gameState.TryCommitKakan(
                        closedWindow,
                        gameState.PendingKakan,
                        out committedKakan,
                        out kakanReason))
                {
                    NotifyKanBlocked(resolution.Source.ActorSeat, resolution.Source.Tile, kakanReason);
                    EndRound(RoundLifecycleService.RoundEndReasonWallEmpty);
                    return false;
                }

                gameState.ClearIppatsuEligibilityForAllPlayers();
                shouldAutoDrawRinshan = true;
            }
            else
            {
                ApplyReactionWindowResolution(resolution);
            }
            afterFinalStateEstablished?.Invoke();
            EventPublisher.NotifyTurnDebug(
                "ReactionWindowResolved",
                $"windowId={resolution.WindowId}; resolution={resolution.Type}; sourceSeat={resolution.Source.ActorSeat}; source={resolution.Source.Kind}",
                seat: resolution.Source.ActorSeat,
                tile: resolution.Source.Tile,
                turnIndex: resolution.Source.TurnIndex);
            EventPublisher.NotifyReactionWindowResolved(resolution);
            EventPublisher.NotifyReactionWindowClosed(resolution.WindowId);
            if (committedKakan != null)
                EventPublisher.NotifyMeldDeclared(committedKakan);
            if (shouldAutoDrawRinshan && resolution.Candidate != null)
                TryAutoDrawRinshanAfterKan(resolution.Candidate.Seat, "Daiminkan");
            else if (shouldAutoDrawRinshan && committedKakan != null)
                TryAutoDrawRinshanAfterKan(committedKakan.OwnerSeat, "Kakan");
            return true;
        }

        private bool TryDrawRinshanForSeat(
            SeatId seat,
            string completedEventName,
            string blockedEventName,
            bool warnOnBlocked,
            out string failureReason)
        {
            failureReason = string.Empty;
            if (gameState.CurrentTurn != seat)
            {
                if (warnOnBlocked)
                    Warn("Only the current seat can draw a rinshan tile.");

                NotifyTurnBlocked(blockedEventName, "NotCurrentTurn");
                failureReason = "NotCurrentTurn";
                return false;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (gameState.IsRoundEnded || gameState.IsWinDecisionPending ||
                gameState.IsReachDecisionPending ||
                gameState.IsReachDiscardSelectionPending ||
                playerSeat.HasDrawnTile ||
                gameState.TurnPhase != TurnPhase.WaitingForRinshanDraw)
            {
                NotifyTurnBlocked(blockedEventName, "RinshanDrawUnavailable");
                failureReason = "RinshanDrawUnavailable";
                return false;
            }

            DrawResult result = drawService.DrawTile(
                seat,
                gameState,
                DrawPurpose.RinshanDraw);
            if (!result.Success)
            {
                NotifyTurnBlocked(blockedEventName, "RinshanDrawUnavailable");
                failureReason = result.Message;
                return false;
            }

            playerSeat.SetDrawnTile(result.Tile);
            gameState.EnterWaitingForDiscard();
            playerSeat.ClearTemporaryFuriten();
            EventPublisher.NotifyTurnDebug(
                "RinshanDrawCompleted",
                $"phase={gameState.TurnPhase}; drawnTile={result.Tile}; liveWall={gameState.Wall.Count}; deadWall={gameState.Wall.DeadWallCount}; rinshanRemaining={gameState.Wall.RemainingRinshanTileCount}",
                seat: seat,
                tile: result.Tile,
                turnIndex: gameState.TurnIndex);
            EventPublisher.NotifyTileDrawn(result);
            ResolveAfterDraw(
                seat,
                result.Purpose == DrawPurpose.RinshanDraw &&
                result.Source == DrawSource.Rinshan);
            return true;
        }

        private void TryAutoDrawRinshanAfterKan(SeatId seat, string kanType)
        {
            if (TryDrawRinshanForSeat(
                    seat,
                    "RinshanDrawCompleted",
                    "RinshanDrawBlocked",
                    false,
                    out string failureReason))
            {
                return;
            }

            EventPublisher.NotifyTurnDebug(
                "RinshanDrawFailed",
                $"kan={kanType}; reason={failureReason}; phase={gameState?.TurnPhase}; currentTurn={gameState?.CurrentTurn}; liveWall={gameState?.Wall.Count}; rinshanRemaining={gameState?.Wall.RemainingRinshanTileCount}",
                seat: seat,
                turnIndex: gameState != null ? gameState.TurnIndex : 0);
            EndRound(RoundLifecycleService.RoundEndReasonWallEmpty);
        }

        private void ApplyReactionWindowResolution(ReactionWindowResolution resolution)
        {
            switch (resolution.Type)
            {
                case ReactionWindowResolutionType.NoReaction:
                    if (resolution.Source.IsDiscard)
                        AdvanceOrEndAfterDiscard(resolution.SourceDiscard);
                    break;
                case ReactionWindowResolutionType.RonDeclared:
                    EndRound(
                        RoundLifecycleService.RoundEndReasonWin,
                        () => NotifyDeclaredReactionRon(resolution),
                        resolution);
                    break;
                case ReactionWindowResolutionType.PonDeclared:
                case ReactionWindowResolutionType.ChiDeclared:
                    BeginTurnAfterMeldCall(resolution);
                    break;
                case ReactionWindowResolutionType.DaiminkanDeclared:
                    BeginTurnAfterDaiminkan(resolution);
                    break;
            }
        }

        private void BeginTurnAfterMeldCall(ReactionWindowResolution resolution)
        {
            if (resolution.Candidate == null || resolution.Meld == null)
                return;

            EnsureTurnFlowService();
            gameState.ClearIppatsuEligibilityForAllPlayers();
            turnFlowService.BeginTurnAfterCall(gameState, resolution.Candidate.Seat);
            EventPublisher.NotifyTurnStarted(
                resolution.Candidate.Seat,
                gameState.TurnIndex);
            EventPublisher.NotifyTurnDebug(
                $"{resolution.Meld.Type}Declared",
                $"windowSourceDiscardId={resolution.SourceDiscard.Id}; caller={resolution.Candidate.Seat}; source={resolution.SourceDiscard.ActorSeat}; phase={gameState.TurnPhase}",
                seat: resolution.Candidate.Seat,
                tile: resolution.SourceDiscard.Tile,
                turnIndex: gameState.TurnIndex);
        }

        private void BeginTurnAfterDaiminkan(ReactionWindowResolution resolution)
        {
            if (resolution.Candidate == null || resolution.Meld == null)
                return;

            EnsureTurnFlowService();
            gameState.ClearIppatsuEligibilityForAllPlayers();
            turnFlowService.BeginTurnAfterKan(gameState, resolution.Candidate.Seat);
            EventPublisher.NotifyTurnStarted(
                resolution.Candidate.Seat,
                gameState.TurnIndex);
            EventPublisher.NotifyTurnDebug(
                "DaiminkanDeclared",
                $"windowSourceDiscardId={resolution.SourceDiscard.Id}; caller={resolution.Candidate.Seat}; source={resolution.SourceDiscard.ActorSeat}; phase={gameState.TurnPhase}; liveWall={gameState.Wall.Count}; deadWall={gameState.Wall.DeadWallCount}; rinshanRemaining={gameState.Wall.RemainingRinshanTileCount}",
                seat: resolution.Candidate.Seat,
                tile: resolution.SourceDiscard.Tile,
                turnIndex: gameState.TurnIndex);
            EventPublisher.NotifyMeldDeclared(resolution.Meld);
        }

        private void NotifyDeclaredReactionRon(ReactionWindowResolution resolution)
        {
            ReactionWindowCandidate candidate = resolution.Candidate;
            if (candidate == null)
                return;

            EventPublisher.NotifyWinDeclared(candidate.Seat, resolution.Source.TurnIndex);
            EventPublisher.NotifyWinDeclaredDetailed(
                candidate.Seat,
                WinType.Ron,
                resolution.Source.TurnIndex);
            EventPublisher.NotifyWinDeclaredEvaluated(
                candidate.Seat,
                WinType.Ron,
                resolution.Source.Tile,
                resolution.Source.ActorSeat,
                resolution.Source.TurnIndex,
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
                PlayerId? playerId = gameState.GetSeatSlot(seat).PlayerId;
                if (!playerId.HasValue)
                    return;

                cpuTurnController?.TryStartCpuTurn(
                    this,
                    gameState,
                    playerId.Value,
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
            return turnFlowService.BuildAutomationPolicy(
                gameState,
                seat,
                enableAutoDraw,
                IsCpuPlayer(seat));
        }

        private bool IsCpuPlayer(SeatId seat)
        {
            if (gameState == null || matchRoster == null)
                return false;

            SeatSlot seatSlot = gameState.GetSeatSlot(seat);
            PlayerId? playerId = seatSlot.PlayerId;
            return playerId.HasValue &&
                seatSlot.ParticipantType == ParticipantType.Cpu &&
                matchRoster.TryGetParticipant(playerId.Value, out MatchParticipant participant) &&
                participant.Kind == ParticipantKind.Cpu;
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

        private void CheckWinPrototype(bool isRinshanDraw = false)
        {
            // PROTOTYPE: Check only the current seat's self-draw declaration candidate.
            InitializeEvaluators();
            WinDecisionEvaluation evaluation = winDecisionService.EvaluateTsumo(
                gameState,
                isRinshanDraw);
            if (evaluation.DecisionStarted &&
                !TryBeginWinDeclarationDecisionRequest(out string requestReason))
            {
                NotifyTurnBlocked("WinBlocked", requestReason);
                // A provider route may be configured without a strategy (for
                // example the intentionally unimplemented network path). Do
                // not leave a newly-created self-draw decision pending
                // forever when no provider can receive it.
                TryDeclineWinDecisionForSeat(gameState.WinDecisionSeat);
            }
            NotifyWinDecisionStartedIfNeeded(evaluation);
            NotifyWinCheckResults(evaluation);
        }

        private void ResolveAfterDraw(SeatId seat, bool isRinshanDraw = false)
        {
            CheckWinPrototype(isRinshanDraw);

            if (gameState.IsWinDecisionPending)
                return;

            if (TryBeginSelfKanDecisionAfterDraw(seat))
                return;

            if (ShouldAutoDiscardDrawnTileAfterDraw(seat))
            {
                TryAutoDiscardDrawnTileAfterDraw(seat);
                return;
            }

            TryBeginReachDecisionAfterDraw(seat);
        }

        private void ContinueAfterDeclinedTsumo(SeatId seat)
        {
            if (TryBeginSelfKanDecisionAfterDraw(seat))
                return;

            if (ShouldAutoDiscardDrawnTileAfterDraw(seat))
            {
                TryAutoDiscardDrawnTileAfterDraw(seat);
                return;
            }

            TryBeginReachDecisionAfterDraw(seat);
        }

        private bool TryBeginSelfKanDecisionAfterDraw(SeatId seat)
        {
            if (gameState == null || gameState.CurrentTurn != seat ||
                gameState.TurnPhase != TurnPhase.WaitingForDiscard)
            {
                return false;
            }

            IReadOnlyList<SelfKanCandidate> candidates =
                kanService.CollectSelfKanCandidates(gameState, seat);
            if (candidates.Count <= 0 || !gameState.BeginSelfKanDecision(seat, candidates))
                return false;

            if (!TryBeginSelfKanDecisionRequest(seat, out string requestReason))
            {
                gameState.TryDeclineSelfKanDecision(seat);
                NotifyKanBlocked(seat, default, requestReason);
                return false;
            }

            EventPublisher.NotifyTurnDebug(
                "SelfKanDecisionStarted",
                $"seat={seat}; candidates={candidates.Count}",
                seat: seat,
                turnIndex: gameState.TurnIndex);
            EventPublisher.NotifySelfKanDecisionStarted(seat, gameState.TurnIndex);
            return true;
        }

        private bool ShouldAutoDiscardDrawnTileAfterDraw(SeatId seat)
        {
            EnsureTurnFlowService();
            return turnFlowService.ShouldAutoDiscardDrawnTileAfterDraw(
                gameState,
                seat,
                enableAutoDraw,
                IsCpuPlayer(seat));
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

            if (!TryBeginReachDecisionRequest(result.Seat, result.TurnIndex, out string requestReason))
            {
                reachDecisionService.Decline(gameState);
                NotifyTurnBlocked("ReachBlocked", requestReason);
                return;
            }

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
            CancelPendingDecisions();
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

        private bool IsReactionSeatAnswerCollectionActive()
        {
            return gameState != null && reactionWindowSeatAnswers != null &&
                gameState.IsReactionWindowPending &&
                ReferenceEquals(
                    gameState.CurrentReactionWindow,
                    reactionWindowSeatAnswers.ReactionWindow);
        }

        private bool IsReactionSeatAnswerPathActiveOrRetryScheduled()
        {
            if (IsReactionSeatAnswerCollectionActive())
                return true;

            return gameState != null &&
                reactionWindowAwaitingRequestRetry != null &&
                gameState.IsReactionWindowPending &&
                reactionWindowAwaitingRequestRetry.IsAcceptingAnswers &&
                ReferenceEquals(
                    gameState.CurrentReactionWindow,
                    reactionWindowAwaitingRequestRetry);
        }

        /// <summary>
        /// The direct declaration APIs remain a compatibility surface for the
        /// existing serial ReactionWindow tests and integrations.  A normal
        /// Local UI response never calls them; it uses the request-bound
        /// provider path instead.  When a compatible direct call is made,
        /// release the multi-seat provider requests before resuming the
        /// established serial service path.
        /// </summary>
        private bool TryEnterLegacyReactionWindowPath(
            SeatId actorSeat,
            int reactionWindowId,
            ReactionKind? expectedKind,
            bool requireCurrentCandidate,
            out string reason)
        {
            reason = string.Empty;
            ReactionWindow reactionWindow = gameState != null
                ? gameState.CurrentReactionWindow
                : null;
            if (!IsReactionSeatAnswerPathActiveOrRetryScheduled() ||
                reactionWindow == null ||
                reactionWindow.WindowId != reactionWindowId)
            {
                reason = "ReactionWindowStale";
                return false;
            }

            if (expectedKind.HasValue)
            {
                ReactionWindowCandidate candidate = requireCurrentCandidate
                    ? reactionWindow.PendingCandidate
                    : FindPendingReactionCandidate(
                        reactionWindow,
                        actorSeat,
                        expectedKind.Value);
                if (candidate == null || candidate.Seat != actorSeat ||
                    candidate.Kind != expectedKind.Value)
                {
                    reason = requireCurrentCandidate
                        ? "ReactionKindMismatch"
                        : "ReactionCandidateMissing";
                    return false;
                }
            }
            else if (!HasPendingMeldCandidate(reactionWindow, actorSeat))
            {
                reason = "MeldCallCandidateMissing";
                return false;
            }

            if (reactionWindowSeatAnswers != null &&
                ReferenceEquals(
                    reactionWindowSeatAnswers.ReactionWindow,
                    reactionWindow) &&
                reactionWindowSeatAnswers.RegisteredAnswers.Count > 0)
            {
                reason = "ReactionSeatAnswersPending";
                return false;
            }

            bool hasProviderWaitingRequest = false;
            foreach (KeyValuePair<long, PendingReactionDecision> pair in
                     pendingReactionDecisionsByRequestId)
            {
                if (pair.Value.WindowId != reactionWindowId)
                    continue;

                if (decisionCoordinator != null &&
                    decisionCoordinator.IsResponseQueued(pair.Key))
                {
                    reason = "ReactionSeatAnswerQueued";
                    return false;
                }

                if (decisionCoordinator == null ||
                    !decisionCoordinator.IsPending(pair.Key))
                {
                    reason = "ReactionDecisionRequestUnavailable";
                    return false;
                }

                hasProviderWaitingRequest = true;
            }

            if (reactionWindowSeatAnswers != null &&
                ReferenceEquals(
                    reactionWindowSeatAnswers.ReactionWindow,
                    reactionWindow) &&
                !hasProviderWaitingRequest)
            {
                reason = "ReactionDecisionRequestUnavailable";
                return false;
            }

            ClearReactionSeatAnswerState(reactionWindow.WindowId);
            return true;
        }

        private static bool TryGetReactionKind(
            MeldCallKind meldCallKind,
            out ReactionKind reactionKind)
        {
            switch (meldCallKind)
            {
                case MeldCallKind.Pon:
                    reactionKind = ReactionKind.Pon;
                    return true;
                case MeldCallKind.Chi:
                    reactionKind = ReactionKind.Chi;
                    return true;
                case MeldCallKind.Kan:
                    reactionKind = ReactionKind.Daiminkan;
                    return true;
                default:
                    reactionKind = default;
                    return false;
            }
        }

        private static bool IsLegacyMeldCallAvailable(
            ReactionWindow reactionWindow,
            SeatId seat,
            ReactionKind kind)
        {
            if (reactionWindow == null || !reactionWindow.IsAcceptingAnswers ||
                reactionWindow.PendingRonCandidate != null)
            {
                return false;
            }

            bool hasAnyPon = HasPendingReactionCandidate(
                reactionWindow,
                null,
                ReactionKind.Pon);
            bool hasAnyDaiminkan = HasPendingReactionCandidate(
                reactionWindow,
                null,
                ReactionKind.Daiminkan);
            bool hasSeatPon = HasPendingReactionCandidate(
                reactionWindow,
                seat,
                ReactionKind.Pon);
            bool hasSeatDaiminkan = HasPendingReactionCandidate(
                reactionWindow,
                seat,
                ReactionKind.Daiminkan);

            switch (kind)
            {
                case ReactionKind.Pon:
                    return hasSeatPon;
                case ReactionKind.Daiminkan:
                    return hasSeatDaiminkan;
                case ReactionKind.Chi:
                    return HasPendingReactionCandidate(
                        reactionWindow,
                        seat,
                        ReactionKind.Chi) &&
                        ((!hasAnyPon && !hasAnyDaiminkan) ||
                         hasSeatPon || hasSeatDaiminkan);
                default:
                    return false;
            }
        }

        private static bool HasAnyLegacyMeldCallAvailable(
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            return IsLegacyMeldCallAvailable(
                       reactionWindow,
                       seat,
                       ReactionKind.Pon) ||
                IsLegacyMeldCallAvailable(
                    reactionWindow,
                    seat,
                    ReactionKind.Daiminkan) ||
                IsLegacyMeldCallAvailable(
                    reactionWindow,
                    seat,
                    ReactionKind.Chi);
        }

        private static bool HasPendingMeldCandidate(
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            return HasPendingReactionCandidate(
                       reactionWindow,
                       seat,
                       ReactionKind.Pon) ||
                HasPendingReactionCandidate(
                    reactionWindow,
                    seat,
                    ReactionKind.Daiminkan) ||
                HasPendingReactionCandidate(
                    reactionWindow,
                    seat,
                    ReactionKind.Chi);
        }

        private static ReactionWindowCandidate FindPendingReactionCandidate(
            ReactionWindow reactionWindow,
            SeatId seat,
            ReactionKind kind)
        {
            if (reactionWindow == null)
                return null;

            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate != null && candidate.IsPending &&
                    candidate.Seat == seat && candidate.Kind == kind)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool HasPendingReactionCandidate(
            ReactionWindow reactionWindow,
            SeatId? seat,
            ReactionKind kind)
        {
            if (reactionWindow == null)
                return false;

            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate != null && candidate.IsPending &&
                    candidate.Kind == kind &&
                    (!seat.HasValue || candidate.Seat == seat.Value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryValidateReactionSeatAnswerCandidateState(
            ReactionWindow reactionWindow,
            out string reason)
        {
            reason = string.Empty;
            if (reactionWindow == null || reactionWindow.Candidates.Count <= 0)
            {
                reason = "ReactionCandidateMissing";
                return false;
            }

            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate == null)
                {
                    reason = "ReactionCandidateInvalid";
                    return false;
                }
                if (!candidate.IsPending)
                {
                    reason = "ReactionCandidateStateChanged";
                    return false;
                }
            }

            return true;
        }

        private void ScheduleReactionSeatAnswerRequestRetry(
            ReactionWindow reactionWindow)
        {
            TryScheduleReactionSeatAnswerRequestRetry(reactionWindow, out _);
        }

        private bool TryScheduleReactionSeatAnswerRequestRetry(
            ReactionWindow reactionWindow,
            out string reason)
        {
            reason = string.Empty;
            if (reactionWindow == null)
            {
                reason = "ReactionWindowStale";
                return false;
            }

            ClearReactionSeatAnswerState(reactionWindow.WindowId);
            if (gameState == null || !gameState.IsReactionWindowPending ||
                !ReferenceEquals(gameState.CurrentReactionWindow, reactionWindow) ||
                !reactionWindow.IsAcceptingAnswers)
            {
                reason = "ReactionWindowStale";
                return false;
            }

            if (!TryValidateReactionSeatAnswerCandidateState(
                    reactionWindow,
                    out reason))
            {
                return false;
            }

            reactionWindowAwaitingRequestRetry = reactionWindow;
            return true;
        }

        private void TryResumeReactionSeatAnswerRequests()
        {
            ReactionWindow reactionWindow = reactionWindowAwaitingRequestRetry;
            if (reactionWindow == null)
                return;

            if (gameState == null || !gameState.IsReactionWindowPending ||
                !ReferenceEquals(gameState.CurrentReactionWindow, reactionWindow) ||
                !reactionWindow.IsAcceptingAnswers)
            {
                reactionWindowAwaitingRequestRetry = null;
                return;
            }

            reactionWindowAwaitingRequestRetry = null;
            if (TryBeginReactionSeatAnswerRequests(
                    reactionWindow,
                    out string requestReason))
            {
                // The window itself is unchanged, but its provider-facing
                // requests were rebuilt. Reuse the established notification
                // so the existing local UI refreshes from the new immutable
                // request identities.
                EventPublisher.NotifyReactionWindowStarted(reactionWindow);
                return;
            }

            bool retryScheduled = TryScheduleReactionSeatAnswerRequestRetry(
                reactionWindow,
                out string retryReason);
            if (!retryScheduled && !string.IsNullOrEmpty(retryReason) &&
                retryReason != "ReactionWindowStale")
            {
                NotifyTurnBlocked("ReactionBlocked", retryReason);
            }
            else if (!retryScheduled && !string.IsNullOrEmpty(requestReason) &&
                requestReason != "ReactionWindowMissing")
            {
                NotifyTurnBlocked("ReactionBlocked", requestReason);
            }
        }

        private bool TryBeginReactionSeatAnswerRequests(
            ReactionWindow reactionWindow,
            out string reason)
        {
            reason = string.Empty;
            if (gameState == null || reactionWindow == null ||
                !gameState.IsReactionWindowPending ||
                !ReferenceEquals(gameState.CurrentReactionWindow, reactionWindow) ||
                !reactionWindow.IsAcceptingAnswers)
            {
                reason = "ReactionWindowMissing";
                return false;
            }

            if (!TryValidateReactionSeatAnswerCandidateState(
                    reactionWindow,
                    out reason))
            {
                ClearReactionSeatAnswerState(reactionWindow.WindowId);
                return false;
            }

            EnsureDecisionCoordinator();
            if (decisionCoordinator == null)
            {
                reason = "DecisionCoordinatorMissing";
                return false;
            }

            EnsureReactionAnswerNotificationWindow(reactionWindow.WindowId);
            ClearReactionSeatAnswerState();
            ReactionWindowSeatAnswerCollection answers;
            try
            {
                answers = new ReactionWindowSeatAnswerCollection(reactionWindow);
            }
            catch (System.ArgumentException)
            {
                reason = "ReactionCandidateInvalid";
                return false;
            }
            if (answers.TargetSeats.Count <= 0)
            {
                reason = "ReactionCandidateMissing";
                return false;
            }

            reactionWindowSeatAnswers = answers;
            for (int i = 0; i < answers.TargetSeats.Count; i++)
            {
                SeatId seat = answers.TargetSeats[i];
                SeatSlot slot = gameState.GetSeatSlot(seat);
                if (!slot.HasPlayer)
                {
                    reason = "ReactionPlayerMissing";
                    ClearReactionSeatAnswerState(reactionWindow.WindowId);
                    return false;
                }

                ReactionDecisionRequest reactionRequest;
                try
                {
                    reactionRequest = CreateReactionDecisionRequest(
                        reactionWindow,
                        seat);
                }
                catch (System.ArgumentException)
                {
                    reason = "ReactionDecisionRequestInvalid";
                    ClearReactionSeatAnswerState(reactionWindow.WindowId);
                    return false;
                }

                DecisionRequest request = new DecisionRequest(
                    AllocateDecisionRequestId(),
                    DecisionKind.Reaction,
                    slot.PlayerId.Value,
                    seat,
                    reactionWindow.TurnIndex,
                    reactionRequest);
                pendingReactionDecisionsByRequestId.Add(
                    request.RequestId,
                    new PendingReactionDecision(request, reactionWindow));

                DecisionResponseResult requestResult = decisionCoordinator.Request(request);
                if (!requestResult.Accepted)
                {
                    pendingReactionDecisionsByRequestId.Remove(request.RequestId);
                    reason = requestResult.Reason;
                    ClearReactionSeatAnswerState(reactionWindow.WindowId);
                    return false;
                }
            }

            return true;
        }

        private static ReactionDecisionRequest CreateReactionDecisionRequest(
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            List<ReactionDecisionOption> options =
                new List<ReactionDecisionOption>
                {
                    new ReactionDecisionOption(ReactionWindowSeatAnswerKind.Pass)
                };

            AddReactionDecisionOption(
                options,
                reactionWindow,
                seat,
                ReactionKind.Ron);
            AddReactionDecisionOption(
                options,
                reactionWindow,
                seat,
                ReactionKind.Pon);
            AddReactionDecisionOption(
                options,
                reactionWindow,
                seat,
                ReactionKind.Daiminkan);
            AddReactionDecisionOption(
                options,
                reactionWindow,
                seat,
                ReactionKind.Chi);

            return new ReactionDecisionRequest(
                reactionWindow.WindowId,
                reactionWindow.Source.Kind,
                reactionWindow.Source.ActorSeat,
                reactionWindow.Source.Tile,
                reactionWindow.Source.TurnIndex,
                options);
        }

        private static void AddReactionDecisionOption(
            List<ReactionDecisionOption> options,
            ReactionWindow reactionWindow,
            SeatId seat,
            ReactionKind kind)
        {
            if (kind == ReactionKind.Chi)
            {
                AddChiReactionDecisionOption(options, reactionWindow, seat);
                return;
            }

            ReactionWindowCandidate candidate = null;
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate current = reactionWindow.Candidates[i];
                if (current != null && current.Seat == seat &&
                    current.Kind == kind && current.IsPending)
                {
                    candidate = current;
                    break;
                }
            }

            if (candidate == null)
                return;

            switch (kind)
            {
                case ReactionKind.Ron:
                    options.Add(new ReactionDecisionOption(
                        ReactionWindowSeatAnswerKind.Ron));
                    break;
                case ReactionKind.Pon:
                    options.Add(new ReactionDecisionOption(
                        ReactionWindowSeatAnswerKind.Pon));
                    break;
                case ReactionKind.Daiminkan:
                    options.Add(new ReactionDecisionOption(
                        ReactionWindowSeatAnswerKind.Daiminkan));
                    break;
            }
        }

        private static void AddChiReactionDecisionOption(
            List<ReactionDecisionOption> options,
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            Dictionary<int, ReactionDecisionChiOption> optionsById =
                new Dictionary<int, ReactionDecisionChiOption>();
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate == null || candidate.Seat != seat ||
                    candidate.Kind != ReactionKind.Chi || !candidate.IsPending ||
                    candidate.ChiDetail == null)
                {
                    continue;
                }

                for (int optionIndex = 0;
                     optionIndex < candidate.ChiDetail.Options.Count;
                     optionIndex++)
                {
                    ChiOption chiOption = candidate.ChiDetail.Options[optionIndex];
                    if (chiOption == null)
                        continue;

                    if (optionsById.ContainsKey(chiOption.OptionId))
                    {
                        throw new System.ArgumentException(
                            "Reaction window chi option ids must be unique per seat.",
                            nameof(reactionWindow));
                    }

                    optionsById.Add(
                        chiOption.OptionId,
                        new ReactionDecisionChiOption(
                            chiOption.OptionId,
                            chiOption.HandTiles,
                            chiOption.MeldTiles));
                }
            }

            if (optionsById.Count <= 0)
                return;

            List<ReactionDecisionChiOption> chiOptions =
                new List<ReactionDecisionChiOption>(optionsById.Values);
            chiOptions.Sort((left, right) =>
                left.OptionId.CompareTo(right.OptionId));
            options.Add(new ReactionDecisionOption(
                ReactionWindowSeatAnswerKind.Chi,
                chiOptions));
        }

        private long AllocateDecisionRequestId()
        {
            long requestId = nextDecisionRequestId;
            nextDecisionRequestId =
                nextDecisionRequestId == long.MaxValue
                    ? 1
                    : nextDecisionRequestId + 1;
            return requestId;
        }

        private void EnsureReactionAnswerNotificationWindow(int windowId)
        {
            if (notifiedReactionAnswerWindowId == windowId)
                return;

            notifiedReactionAnswerWindowId = windowId;
            notifiedReactionAnswerSeats.Clear();
        }

        private void NotifyReactionWindowAnsweredOnce(
            SeatId seat,
            ReactionWindowAnswerResult answer)
        {
            EnsureReactionAnswerNotificationWindow(answer.WindowId);
            if (notifiedReactionAnswerSeats.Add(seat))
                EventPublisher.NotifyReactionWindowAnswered(answer);
        }

        private void ClearReactionSeatAnswerState(int? windowId = null)
        {
            List<long> requestIdsToClear = new List<long>();
            foreach (KeyValuePair<long, PendingReactionDecision> pair in
                     pendingReactionDecisionsByRequestId)
            {
                if (!windowId.HasValue || pair.Value.WindowId == windowId.Value)
                    requestIdsToClear.Add(pair.Key);
            }

            for (int i = 0; i < requestIdsToClear.Count; i++)
            {
                long requestId = requestIdsToClear[i];
                decisionCoordinator?.Cancel(requestId);
                pendingReactionDecisionsByRequestId.Remove(requestId);
            }

            if (!windowId.HasValue ||
                (reactionWindowSeatAnswers != null &&
                    reactionWindowSeatAnswers.WindowId == windowId.Value))
            {
                reactionWindowSeatAnswers = null;
            }

            if (!windowId.HasValue ||
                (reactionWindowAwaitingRequestRetry != null &&
                    reactionWindowAwaitingRequestRetry.WindowId == windowId.Value))
            {
                reactionWindowAwaitingRequestRetry = null;
            }
        }

        private static bool MatchesDecisionIdentity(
            DecisionRequest request,
            DecisionResponse response)
        {
            return request != null && response != null &&
                request.RequestId == response.RequestId &&
                request.Kind == response.Kind &&
                request.PlayerId == response.PlayerId &&
                request.ActorSeat == response.ActorSeat &&
                request.TurnIndex == response.TurnIndex;
        }

        private bool TryBeginWinDeclarationDecisionRequest(out string reason)
        {
            reason = string.Empty;
            if (gameState == null || !gameState.IsWinDecisionPending ||
                gameState.WinDecisionType != WinType.Tsumo)
            {
                reason = "WinDecisionMissing";
                return false;
            }

            SeatId seat = gameState.WinDecisionSeat;
            SeatSlot slot = gameState.GetSeatSlot(seat);
            if (!slot.HasPlayer)
            {
                reason = "WinDecisionPlayerMissing";
                return false;
            }

            if (TryGetPendingDecisionRequest(
                    slot.PlayerId.Value,
                    DecisionKind.WinDeclaration,
                    out _))
            {
                return true;
            }

            DecisionRequest request = new DecisionRequest(
                AllocateDecisionRequestId(),
                DecisionKind.WinDeclaration,
                slot.PlayerId.Value,
                seat,
                gameState.WinDecisionTurnIndex,
                new WinDeclarationDecisionRequest(
                    gameState.WinDecisionType.Value,
                    gameState.WinningTile,
                    gameState.WinSourceSeat));
            return TryQueueDecisionRequest(request, out reason);
        }

        private bool TryBeginReachDecisionRequest(
            SeatId seat,
            int turnIndex,
            out string reason)
        {
            reason = string.Empty;
            if (gameState == null || !gameState.IsReachDecisionPending ||
                gameState.ReachDecisionSeat != seat ||
                gameState.ReachDecisionTurnIndex != turnIndex)
            {
                reason = "ReachDecisionMissing";
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            if (!slot.HasPlayer)
            {
                reason = "ReachDecisionPlayerMissing";
                return false;
            }

            DecisionRequest request = new DecisionRequest(
                AllocateDecisionRequestId(),
                DecisionKind.Reach,
                slot.PlayerId.Value,
                seat,
                turnIndex,
                new ReachDecisionRequest());
            return TryQueueDecisionRequest(request, out reason);
        }

        private bool TryBeginSelfKanDecisionRequest(SeatId seat, out string reason)
        {
            reason = string.Empty;
            if (gameState == null || !gameState.IsSelfKanDecisionPending ||
                gameState.CurrentSelfKanDecision == null ||
                gameState.CurrentSelfKanDecision.Seat != seat)
            {
                reason = "SelfKanDecisionMissing";
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            if (!slot.HasPlayer)
            {
                reason = "SelfKanDecisionPlayerMissing";
                return false;
            }

            DecisionRequest request = new DecisionRequest(
                AllocateDecisionRequestId(),
                DecisionKind.SelfKan,
                slot.PlayerId.Value,
                seat,
                gameState.CurrentSelfKanDecision.TurnIndex,
                new SelfKanDecisionRequest(gameState.CurrentSelfKanDecision.Candidates));
            return TryQueueDecisionRequest(request, out reason);
        }

        private bool TryQueueDecisionRequest(DecisionRequest request, out string reason)
        {
            reason = string.Empty;
            if (request == null)
            {
                reason = "DecisionRequestMissing";
                return false;
            }

            EnsureDecisionCoordinator();
            if (decisionCoordinator == null)
            {
                reason = "DecisionCoordinatorMissing";
                return false;
            }

            if (pendingDecisionRequestsById.ContainsKey(request.RequestId))
            {
                reason = "DuplicateDecisionRequestId";
                return false;
            }

            pendingDecisionRequestsById.Add(request.RequestId, request);
            DecisionResponseResult result = decisionCoordinator.Request(request);
            if (result.Accepted)
                return true;

            pendingDecisionRequestsById.Remove(request.RequestId);
            reason = result.Reason;
            return false;
        }

        // PROTOTYPE: Existing scene events and reflection-based tests still
        // call the pre-decision public methods. When an authority request is
        // present, keep that surface as a thin adapter into the coordinator
        // rather than mutating game state directly. The no-request fallback in
        // each caller preserves older test setup that creates a state-only
        // decision without configuring a provider.
        private bool TrySubmitLegacyDecisionResponse(
            SeatId actorSeat,
            DecisionKind kind,
            bool accepted,
            SelfKanDecisionResponse selfKan = null)
        {
            if (gameState == null)
                return false;

            SeatSlot slot = gameState.GetSeatSlot(actorSeat);
            if (!slot.HasPlayer ||
                !TryGetPendingDecisionRequest(slot.PlayerId.Value, kind, out DecisionRequest request) ||
                request.ActorSeat != actorSeat ||
                request.TurnIndex != gameState.TurnIndex)
            {
                return false;
            }

            DecisionResponse response = selfKan == null
                ? new DecisionResponse(
                    request.RequestId,
                    kind,
                    slot.PlayerId.Value,
                    actorSeat,
                    request.TurnIndex,
                    accepted)
                : new DecisionResponse(
                    request.RequestId,
                    kind,
                    slot.PlayerId.Value,
                    actorSeat,
                    request.TurnIndex,
                    accepted,
                    selfKan);
            EnsureDecisionCoordinator();
            if (decisionCoordinator == null ||
                !decisionCoordinator.ReceiveResponse(response).Accepted)
            {
                return false;
            }

            decisionCoordinator.Pump();
            return true;
        }

        private bool TrySubmitLegacySelfKanDecisionResponse(
            SeatId actorSeat,
            SelfKanKind kind,
            int tileTypeIndex,
            int sourcePonMeldIndex)
        {
            if (gameState == null)
                return false;

            SeatSlot slot = gameState.GetSeatSlot(actorSeat);
            if (!slot.HasPlayer ||
                !TryGetPendingDecisionRequest(
                    slot.PlayerId.Value,
                    DecisionKind.SelfKan,
                    out DecisionRequest request) ||
                request.SelfKan == null)
            {
                return false;
            }

            for (int i = 0; i < request.SelfKan.Options.Count; i++)
            {
                SelfKanDecisionOption option = request.SelfKan.Options[i];
                if (option.Kind == kind && option.Tile.TypeIndex == tileTypeIndex &&
                    option.SourcePonMeldIndex == sourcePonMeldIndex)
                {
                    return TrySubmitLegacyDecisionResponse(
                        actorSeat,
                        DecisionKind.SelfKan,
                        true,
                        new SelfKanDecisionResponse(option.OptionId));
                }
            }

            return false;
        }

        private bool TryDeclineWinDecisionForSeat(SeatId actorSeat)
        {
            if (!CanUseGameState() || !gameState.IsWinDecisionPending ||
                gameState.WinDecisionSeat != actorSeat ||
                gameState.WinDecisionType != WinType.Tsumo)
            {
                return false;
            }

            EnsureDecisionServices();
            WinDecisionDeclineResult result = winDecisionService.Decline(gameState);
            if (!result.WasPending)
                return false;

            EventPublisher.NotifyWinDeclined(result.Seat, result.TurnIndex);
            EventPublisher.NotifyWinDeclinedDetailed(
                result.Seat,
                result.WinType,
                result.TurnIndex);
            ContinueAfterDeclinedTsumo(result.Seat);
            return true;
        }

        private bool TryDeclineSelfKanDecisionForSeat(SeatId actorSeat)
        {
            if (!CanUseGameState() || gameState.CurrentSelfKanDecision == null)
            {
                NotifyKanBlocked(actorSeat, default, "SelfKanDecisionMissing");
                return false;
            }

            int turnIndex = gameState.CurrentSelfKanDecision.TurnIndex;
            if (!gameState.TryDeclineSelfKanDecision(actorSeat))
            {
                NotifyKanBlocked(actorSeat, default, "SelfKanDecisionMissing");
                return false;
            }

            EventPublisher.NotifySelfKanDecisionDeclined(actorSeat, turnIndex);
            if (!gameState.GetPlayerSeat(actorSeat).IsReachDeclared)
                return true;

            return TryRequestDiscardDrawnTileForSeatInternal(actorSeat, false);
        }

        private bool TryCancelReachDiscardSelectionForSeat(SeatId actorSeat)
        {
            if (!CanUseGameState() || !gameState.IsReachDiscardSelectionPending ||
                gameState.ReachDecisionSeat != actorSeat ||
                gameState.ReachDecisionTurnIndex != gameState.TurnIndex)
            {
                NotifyTurnBlocked("ReachBlocked", "ReachDiscardSelectionMissing");
                return false;
            }

            EnsureDecisionServices();
            ReachDecisionResult result = reachDecisionService.CancelDiscardSelection(
                gameState,
                actorSeat);
            if (!result.Success)
            {
                NotifyTurnBlocked("ReachBlocked", result.FailureReason);
                return false;
            }

            if (!TryBeginReachDecisionRequest(result.Seat, result.TurnIndex, out string requestReason))
            {
                NotifyTurnBlocked("ReachBlocked", requestReason);
                return false;
            }

            EventPublisher.NotifyTurnDebug(
                "ReachDiscardSelectionCanceled",
                $"phase={gameState.TurnPhase}; candidates={gameState.ReachDiscardCandidates.Count}",
                seat: result.Seat,
                turnIndex: result.TurnIndex);
            EventPublisher.NotifyReachDiscardSelectionCanceled(result.Seat, result.TurnIndex);
            return true;
        }

        private bool TryRequestForceDrawSkillCommand(SeatId ownerSeat, string targetTileCode)
        {
            if (!CanUseGameState())
                return false;

            EnsureSkillFlowService();
            SkillFlowResult result = skillFlowService.RequestForceDraw(
                gameState,
                ownerSeat,
                targetTileCode);
            NotifySkillFlowResult(result);
            return result.Success;
        }

        private void EnsureDecisionCoordinator()
        {
            EnsureMatchConfiguration();
            decisionCoordinator ??= new DecisionCoordinator(
                decisionProviderRegistry,
                this);
        }

        private void CancelPendingDecisions()
        {
            decisionCoordinator?.CancelAll();
            pendingDecisionRequestsById.Clear();
            ClearReactionSeatAnswerState();
            notifiedReactionAnswerWindowId = 0;
            notifiedReactionAnswerSeats.Clear();
        }

        private readonly struct PendingReactionDecision
        {
            public PendingReactionDecision(
                DecisionRequest request,
                ReactionWindow reactionWindow)
            {
                Request = request ?? throw new System.ArgumentNullException(nameof(request));
                ReactionWindow = reactionWindow ??
                    throw new System.ArgumentNullException(nameof(reactionWindow));
            }

            public DecisionRequest Request { get; }
            public ReactionWindow ReactionWindow { get; }
            public int WindowId => Request.Reaction != null
                ? Request.Reaction.WindowId
                : 0;
        }

        private void EnsureMatchConfiguration()
        {
            if (matchRoster != null || decisionProviderRegistry != null)
                return;

            List<MatchParticipant> participants = new List<MatchParticipant>();
            List<DecisionProviderRegistration> registrations =
                new List<DecisionProviderRegistration>();
            for (int playerIndex = 1; playerIndex <= participantCount; playerIndex++)
            {
                PlayerId playerId = (PlayerId)playerIndex;
                // PROTOTYPE: Preserve the scene's current LocalHuman + CPU
                // setup by translating its former ParticipantType meaning once
                // at match creation. Future scene configuration will construct
                // MatchRoster and DecisionProviderRegistry directly.
                ParticipantType legacyParticipantType = playerId == PlayerId.Player1
                    ? ParticipantType.LocalHuman
                    : ParticipantType.Cpu;
                participants.Add(ParticipantTypeAdapter.ToMatchParticipant(
                    playerId,
                    legacyParticipantType));
                DecisionProviderRegistration registration =
                    ParticipantTypeAdapter.ToDecisionProviderRegistration(
                        playerId,
                        legacyParticipantType);
                if (registration.Route == DecisionProviderRoute.LocalUi)
                {
                    registration = new DecisionProviderRegistration(
                        playerId,
                        registration.Route,
                        new LocalUiDecisionProvider());
                }
                else if (registration.Route == DecisionProviderRoute.CpuAgent)
                {
                    registration = new DecisionProviderRegistration(
                        playerId,
                        registration.Route,
                        new CpuAgentDecisionProvider());
                }

                registrations.Add(registration);
            }

            matchRoster = new MatchRoster(participants);
            decisionProviderRegistry = new DecisionProviderRegistry(registrations);
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
            if (gameState == null)
            {
                Warn("GameState is not initialized. StartNewRound first.");
                return false;
            }

            if (!isRoundSetupPending)
                return true;

            Warn("Round setup is waiting for the round progress presentation.");
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

        private void NotifyKanBlocked(SeatId seat, Tile tile, string reason)
        {
            if (gameState == null)
                return;

            EventPublisher.NotifyTurnDebug(
                "KanBlocked",
                $"reason={reason}; phase={gameState.TurnPhase}; liveWall={gameState.Wall.Count}; deadWall={gameState.Wall.DeadWallCount}; rinshanRemaining={gameState.Wall.RemainingRinshanTileCount}",
                seat: seat,
                tile: tile.IsValid ? tile : (Tile?)null,
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
