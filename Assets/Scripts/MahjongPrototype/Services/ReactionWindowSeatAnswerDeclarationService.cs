using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    /// <summary>
    /// Prepares and commits the one declaration selected from a fully collected
    /// set of seat answers.  This service intentionally does not resolve the
    /// reaction window lifecycle, advance a turn, or publish notifications.
    /// </summary>
    public sealed class ReactionWindowSeatAnswerDeclarationService
    {
        private readonly PonService ponService;
        private readonly ChiService chiService;
        private readonly KanService kanService;

        public ReactionWindowSeatAnswerDeclarationService()
            : this(new PonService(), new ChiService(), new KanService())
        {
        }

        public ReactionWindowSeatAnswerDeclarationService(
            PonService ponService,
            ChiService chiService,
            KanService kanService)
        {
            this.ponService = ponService ?? throw new ArgumentNullException(nameof(ponService));
            this.chiService = chiService ?? throw new ArgumentNullException(nameof(chiService));
            this.kanService = kanService ?? throw new ArgumentNullException(nameof(kanService));
        }

        /// <summary>
        /// Validates a fully collected answer result and prepares only the
        /// declaration selected by the resolver.  No game or reaction-window
        /// state is changed.
        /// </summary>
        public ReactionWindowSeatAnswerPreparationResult Prepare(
            MahjongGameState gameState,
            ReactionWindowSeatAnswerCollection answers,
            ReactionWindowSeatAnswerResolution resolution)
        {
            if (!TryValidatePreparation(
                    gameState,
                    answers,
                    resolution,
                    out ReactionWindow reactionWindow,
                    out string reason))
            {
                return ReactionWindowSeatAnswerPreparationResult.Rejected(reason);
            }

            if (resolution.IsNoReaction)
            {
                return ReactionWindowSeatAnswerPreparationResult.Succeeded(
                    new PreparedReactionWindowSeatAnswerDeclaration(
                        gameState,
                        reactionWindow,
                        resolution,
                        null,
                        new ReactionWindowSeatAnswerPreparationSnapshot(
                            gameState,
                            reactionWindow)));
            }

            ReactionWindowCandidate candidate = resolution.Candidate;
            PreparedMeldCall preparedMeldCall = null;
            switch (candidate.Kind)
            {
                case ReactionKind.Ron:
                    if (candidate.RonDetail == null ||
                        candidate.WinDeclarationEvaluation == null)
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(
                            "ReactionRonEvaluationMissing");
                    }
                    break;
                case ReactionKind.Pon:
                    if (!reactionWindow.Source.IsDiscard)
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(
                            "MeldCallSourceUnsupported");
                    }
                    if (!ponService.TryPrepareDeclaration(
                            gameState,
                            reactionWindow,
                            candidate,
                            out preparedMeldCall,
                            out reason))
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(reason);
                    }
                    break;
                case ReactionKind.Chi:
                    if (!reactionWindow.Source.IsDiscard ||
                        !resolution.ChiOptionId.HasValue)
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(
                            "ChiOptionMissing");
                    }
                    if (!chiService.TryPrepareDeclaration(
                            gameState,
                            reactionWindow,
                            candidate,
                            resolution.ChiOptionId.Value,
                            out preparedMeldCall,
                            out reason))
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(reason);
                    }
                    break;
                case ReactionKind.Daiminkan:
                    if (!reactionWindow.Source.IsDiscard)
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(
                            "MeldCallSourceUnsupported");
                    }
                    if (!kanService.TryPrepareDaiminkanDeclaration(
                            gameState,
                            reactionWindow,
                            candidate,
                            out preparedMeldCall,
                            out reason))
                    {
                        return ReactionWindowSeatAnswerPreparationResult.Rejected(reason);
                    }
                    break;
                default:
                    return ReactionWindowSeatAnswerPreparationResult.Rejected(
                        "ReactionKindUnsupported");
            }

            return ReactionWindowSeatAnswerPreparationResult.Succeeded(
                new PreparedReactionWindowSeatAnswerDeclaration(
                    gameState,
                    reactionWindow,
                    resolution,
                    preparedMeldCall,
                    new ReactionWindowSeatAnswerPreparationSnapshot(
                        gameState,
                        reactionWindow)));
        }

        /// <summary>
        /// Applies the prepared declaration exactly once.  The returned
        /// resolution is intentionally left for the existing GameFlow to close
        /// the reaction window and perform its normal follow-up work.
        /// </summary>
        public ReactionWindowSeatAnswerCommitResult Commit(
            MahjongGameState gameState,
            PreparedReactionWindowSeatAnswerDeclaration preparedDeclaration)
        {
            if (preparedDeclaration == null)
                return ReactionWindowSeatAnswerCommitResult.Rejected(
                    "ReactionPreparationMissing");

            if (preparedDeclaration.IsCommitted)
                return ReactionWindowSeatAnswerCommitResult.Rejected(
                    "ReactionPreparationAlreadyCommitted");

            if (!preparedDeclaration.IsPreparedFor(gameState) ||
                !preparedDeclaration.IsSnapshotCurrent(gameState))
            {
                return ReactionWindowSeatAnswerCommitResult.Rejected(
                    "ReactionPreparationStale");
            }

            ReactionWindow reactionWindow = preparedDeclaration.ReactionWindow;
            ReactionWindowResolution resolution;
            if (preparedDeclaration.IsNoReaction)
            {
                reactionWindow.CloseCandidatesExcept(null);
                resolution = ReactionWindowResolution.NoReaction(
                    reactionWindow.WindowId,
                    reactionWindow.Source);
            }
            else if (preparedDeclaration.ReactionKind == ReactionKind.Ron)
            {
                if (!gameState.TryCommitReactionRon(
                        reactionWindow,
                        preparedDeclaration.Candidate,
                        out string reason))
                {
                    return ReactionWindowSeatAnswerCommitResult.Rejected(reason);
                }

                resolution = ReactionWindowResolution.RonDeclared(
                    reactionWindow.WindowId,
                    reactionWindow.Source,
                    preparedDeclaration.Candidate);
            }
            else
            {
                if (!gameState.TryCommitMeldCall(
                        reactionWindow,
                        preparedDeclaration.PreparedMeldCall,
                        out string reason))
                {
                    return ReactionWindowSeatAnswerCommitResult.Rejected(reason);
                }

                // TryCommitMeldCall retains the legacy behavior of declining
                // only meld candidates.  This new path has a complete seat
                // answer set, so it can also finish unselected ron candidates.
                reactionWindow.CloseCandidatesExcept(preparedDeclaration.Candidate);
                resolution = CreateMeldResolution(
                    reactionWindow,
                    preparedDeclaration.Candidate,
                    preparedDeclaration.Meld);
            }

            preparedDeclaration.MarkCommitted();
            return ReactionWindowSeatAnswerCommitResult.Succeeded(resolution);
        }

        private static ReactionWindowResolution CreateMeldResolution(
            ReactionWindow reactionWindow,
            ReactionWindowCandidate candidate,
            PlayerMeld meld)
        {
            switch (candidate.Kind)
            {
                case ReactionKind.Pon:
                    return ReactionWindowResolution.PonDeclared(
                        reactionWindow.WindowId,
                        reactionWindow.SourceDiscard,
                        candidate,
                        meld);
                case ReactionKind.Chi:
                    return ReactionWindowResolution.ChiDeclared(
                        reactionWindow.WindowId,
                        reactionWindow.SourceDiscard,
                        candidate,
                        meld);
                case ReactionKind.Daiminkan:
                    return ReactionWindowResolution.DaiminkanDeclared(
                        reactionWindow.WindowId,
                        reactionWindow.SourceDiscard,
                        candidate,
                        meld);
                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate));
            }
        }

        private static bool TryValidatePreparation(
            MahjongGameState gameState,
            ReactionWindowSeatAnswerCollection answers,
            ReactionWindowSeatAnswerResolution resolution,
            out ReactionWindow reactionWindow,
            out string reason)
        {
            reactionWindow = null;
            reason = string.Empty;
            if (gameState == null)
            {
                reason = "GameStateMissing";
                return false;
            }
            if (answers == null)
            {
                reason = "ReactionAnswersMissing";
                return false;
            }
            if (answers.HasUnansweredSeats)
            {
                reason = "ReactionAnswersPending";
                return false;
            }

            reactionWindow = answers.ReactionWindow;
            if (!gameState.IsReactionWindowPending ||
                !ReferenceEquals(gameState.CurrentReactionWindow, reactionWindow))
            {
                reason = "ReactionWindowStale";
                return false;
            }
            if (!reactionWindow.IsAcceptingAnswers)
            {
                reason = "ReactionWindowResolving";
                return false;
            }
            if (resolution.IsPending)
            {
                reason = "ReactionResolutionPending";
                return false;
            }
            if (resolution.WindowId != reactionWindow.WindowId ||
                !HasSameSource(resolution.Source, reactionWindow.Source))
            {
                reason = "ReactionResolutionMismatch";
                return false;
            }
            if (!AreAllCandidatesPending(reactionWindow))
            {
                reason = "ReactionCandidateStateChanged";
                return false;
            }

            if (resolution.IsNoReaction)
                return TryValidateNoReaction(answers, resolution, out reason);

            if (!resolution.HasSelectedDeclaration)
            {
                reason = "ReactionResolutionUnsupported";
                return false;
            }

            return TryValidateSelectedDeclaration(
                answers,
                reactionWindow,
                resolution,
                out reason);
        }

        private static bool TryValidateNoReaction(
            ReactionWindowSeatAnswerCollection answers,
            ReactionWindowSeatAnswerResolution resolution,
            out string reason)
        {
            reason = string.Empty;
            if (resolution.Candidate != null || resolution.SelectedSeat.HasValue ||
                resolution.SelectedKind.HasValue || resolution.ChiOptionId.HasValue ||
                resolution.Answer != null)
            {
                reason = "ReactionResolutionMismatch";
                return false;
            }

            IReadOnlyList<ReactionWindowSeatAnswer> registeredAnswers =
                answers.RegisteredAnswers;
            for (int i = 0; i < registeredAnswers.Count; i++)
            {
                if (registeredAnswers[i].Kind != ReactionWindowSeatAnswerKind.Pass)
                {
                    reason = "ReactionResolutionMismatch";
                    return false;
                }
            }

            return true;
        }

        private static bool TryValidateSelectedDeclaration(
            ReactionWindowSeatAnswerCollection answers,
            ReactionWindow reactionWindow,
            ReactionWindowSeatAnswerResolution resolution,
            out string reason)
        {
            reason = string.Empty;
            ReactionWindowCandidate candidate = resolution.Candidate;
            if (candidate == null || !resolution.SelectedSeat.HasValue ||
                !resolution.SelectedKind.HasValue || resolution.Answer == null ||
                candidate.Seat != resolution.SelectedSeat.Value ||
                candidate.Kind != resolution.SelectedKind.Value ||
                !candidate.IsPending ||
                !ContainsCandidate(reactionWindow, candidate))
            {
                reason = "ReactionCandidateMismatch";
                return false;
            }

            if (!answers.TryGetRegisteredAnswer(
                    candidate.Seat,
                    out ReactionWindowSeatAnswer registeredAnswer) ||
                !ReferenceEquals(registeredAnswer, resolution.Answer) ||
                !answers.TryGetDeclaredCandidate(
                    registeredAnswer,
                    out ReactionWindowCandidate declaredCandidate) ||
                !ReferenceEquals(declaredCandidate, candidate))
            {
                reason = "ReactionAnswerMismatch";
                return false;
            }

            if (candidate.Kind == ReactionKind.Chi)
            {
                if (!resolution.ChiOptionId.HasValue ||
                    registeredAnswer.ChiOptionId != resolution.ChiOptionId ||
                    !HasChiOption(candidate, resolution.ChiOptionId.Value))
                {
                    reason = "ChiOptionMissing";
                    return false;
                }
            }
            else if (resolution.ChiOptionId.HasValue ||
                     registeredAnswer.ChiOptionId.HasValue)
            {
                reason = "ReactionAnswerMismatch";
                return false;
            }

            return true;
        }

        private static bool AreAllCandidatesPending(ReactionWindow reactionWindow)
        {
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                if (reactionWindow.Candidates[i] == null ||
                    !reactionWindow.Candidates[i].IsPending)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsCandidate(
            ReactionWindow reactionWindow,
            ReactionWindowCandidate candidate)
        {
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                if (ReferenceEquals(reactionWindow.Candidates[i], candidate))
                    return true;
            }

            return false;
        }

        private static bool HasChiOption(
            ReactionWindowCandidate candidate,
            int optionId)
        {
            if (candidate.ChiDetail == null)
                return false;

            IReadOnlyList<ChiOption> options = candidate.ChiDetail.Options;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && options[i].OptionId == optionId)
                    return true;
            }

            return false;
        }

        private static bool HasSameSource(
            ReactionWindowSource left,
            ReactionWindowSource right)
        {
            if (left.Kind != right.Kind || left.ActorSeat != right.ActorSeat ||
                left.Tile != right.Tile || left.TurnIndex != right.TurnIndex ||
                left.Discard.HasValue != right.Discard.HasValue)
            {
                return false;
            }

            return !left.Discard.HasValue ||
                HasSameDiscard(left.Discard.Value, right.Discard.Value);
        }

        private static bool HasSameDiscard(
            DiscardRecord left,
            DiscardRecord right)
        {
            return left.Id == right.Id && left.ActorSeat == right.ActorSeat &&
                left.Tile == right.Tile && left.TurnIndex == right.TurnIndex &&
                left.Source == right.Source &&
                left.IsLastLiveWallDiscard == right.IsLastLiveWallDiscard;
        }
    }

    /// <summary>
    /// An immutable prepare result that can be committed once.  The contained
    /// meld is only a prepared value until <see cref="ReactionWindowSeatAnswerDeclarationService.Commit"/>
    /// succeeds.
    /// </summary>
    public sealed class PreparedReactionWindowSeatAnswerDeclaration
    {
        private readonly MahjongGameState gameState;
        private readonly ReactionWindowSeatAnswerPreparationSnapshot snapshot;

        internal PreparedReactionWindowSeatAnswerDeclaration(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            ReactionWindowSeatAnswerResolution resolution,
            PreparedMeldCall preparedMeldCall,
            ReactionWindowSeatAnswerPreparationSnapshot snapshot)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            ReactionWindow = reactionWindow ??
                throw new ArgumentNullException(nameof(reactionWindow));
            this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

            WindowId = reactionWindow.WindowId;
            Source = reactionWindow.Source;
            ResolutionType = resolution.Type;
            Candidate = resolution.Candidate;
            ReactionKind = resolution.SelectedKind;
            ChiOptionId = resolution.ChiOptionId;
            Answer = resolution.Answer;
            PreparedMeldCall = preparedMeldCall;
            Meld = preparedMeldCall?.Meld;
            WinDeclarationEvaluation = Candidate?.WinDeclarationEvaluation;
        }

        public ReactionWindow ReactionWindow { get; }
        public int WindowId { get; }
        public ReactionWindowSource Source { get; }
        public ReactionWindowSeatAnswerResolutionType ResolutionType { get; }
        public ReactionWindowCandidate Candidate { get; }
        public ReactionKind? ReactionKind { get; }
        public int? ChiOptionId { get; }
        public ReactionWindowSeatAnswer Answer { get; }
        public PlayerMeld Meld { get; }
        public WinDeclarationEvaluationResult WinDeclarationEvaluation { get; }
        public bool IsNoReaction =>
            ResolutionType == ReactionWindowSeatAnswerResolutionType.NoReaction;
        public bool IsCommitted { get; private set; }

        internal PreparedMeldCall PreparedMeldCall { get; }

        internal bool IsPreparedFor(MahjongGameState state)
        {
            return ReferenceEquals(gameState, state);
        }

        internal bool IsSnapshotCurrent(MahjongGameState state)
        {
            return snapshot.IsCurrent(state, ReactionWindow);
        }

        internal void MarkCommitted()
        {
            IsCommitted = true;
        }
    }

    public readonly struct ReactionWindowSeatAnswerPreparationResult
    {
        private ReactionWindowSeatAnswerPreparationResult(
            bool prepared,
            PreparedReactionWindowSeatAnswerDeclaration preparedDeclaration,
            string reason)
        {
            Prepared = prepared;
            PreparedDeclaration = preparedDeclaration;
            Reason = reason ?? string.Empty;
        }

        public bool Prepared { get; }
        public PreparedReactionWindowSeatAnswerDeclaration PreparedDeclaration { get; }
        public string Reason { get; }

        internal static ReactionWindowSeatAnswerPreparationResult Succeeded(
            PreparedReactionWindowSeatAnswerDeclaration preparedDeclaration)
        {
            return new ReactionWindowSeatAnswerPreparationResult(
                true,
                preparedDeclaration ??
                    throw new ArgumentNullException(nameof(preparedDeclaration)),
                string.Empty);
        }

        internal static ReactionWindowSeatAnswerPreparationResult Rejected(string reason)
        {
            return new ReactionWindowSeatAnswerPreparationResult(false, null, reason);
        }
    }

    public readonly struct ReactionWindowSeatAnswerCommitResult
    {
        private ReactionWindowSeatAnswerCommitResult(
            bool committed,
            ReactionWindowResolution resolution,
            string reason)
        {
            Committed = committed;
            Resolution = resolution;
            Reason = reason ?? string.Empty;
        }

        public bool Committed { get; }
        public ReactionWindowResolution Resolution { get; }
        public string Reason { get; }

        internal static ReactionWindowSeatAnswerCommitResult Succeeded(
            ReactionWindowResolution resolution)
        {
            return new ReactionWindowSeatAnswerCommitResult(true, resolution, string.Empty);
        }

        internal static ReactionWindowSeatAnswerCommitResult Rejected(string reason)
        {
            return new ReactionWindowSeatAnswerCommitResult(
                false,
                ReactionWindowResolution.None,
                reason);
        }
    }

    /// <summary>
    /// Captures the reaction-relevant game state after preparation.  A response
    /// window is interaction-locked, so any intervening change makes a prepared
    /// declaration stale rather than allowing a commit against mixed state.
    /// </summary>
    internal sealed class ReactionWindowSeatAnswerPreparationSnapshot
    {
        private readonly ReactionWindowState reactionWindowState;
        private readonly SeatId currentTurn;
        private readonly int turnIndex;
        private readonly TurnPhase turnPhase;
        private readonly bool hasCallOccurred;
        private readonly SelfKanCandidate pendingKakan;
        private readonly int wallCount;
        private readonly int deadWallCount;
        private readonly int remainingRinshanTileCount;
        private readonly SeatId[] activeSeats;
        private readonly SeatSlotSnapshot[] seatSlots;
        private readonly CandidateSnapshot[] candidates;
        private readonly PlayerSeatSnapshot[] playerSeats;
        private readonly DiscardRecord[] discards;
        private readonly DiscardClaimSnapshot[] discardClaims;

        public ReactionWindowSeatAnswerPreparationSnapshot(
            MahjongGameState gameState,
            ReactionWindow reactionWindow)
        {
            reactionWindowState = reactionWindow.State;
            currentTurn = gameState.CurrentTurn;
            turnIndex = gameState.TurnIndex;
            turnPhase = gameState.TurnPhase;
            hasCallOccurred = gameState.HasCallOccurred;
            pendingKakan = gameState.PendingKakan;
            wallCount = gameState.Wall.Count;
            deadWallCount = gameState.Wall.DeadWallCount;
            remainingRinshanTileCount = gameState.Wall.RemainingRinshanTileCount;

            activeSeats = CopySeats(gameState.ActiveTurnSeats);
            seatSlots = CopySeatSlots(gameState, activeSeats);
            candidates = CopyCandidates(reactionWindow.Candidates);
            playerSeats = CopyPlayerSeats(gameState, activeSeats);
            discards = CopyDiscards(gameState.Discards);
            discardClaims = CopyDiscardClaims(gameState.DiscardClaims);
        }

        public bool IsCurrent(
            MahjongGameState gameState,
            ReactionWindow expectedReactionWindow)
        {
            if (gameState == null || expectedReactionWindow == null ||
                !gameState.IsReactionWindowPending ||
                !ReferenceEquals(gameState.CurrentReactionWindow, expectedReactionWindow) ||
                expectedReactionWindow.State != reactionWindowState ||
                gameState.CurrentTurn != currentTurn ||
                gameState.TurnIndex != turnIndex ||
                gameState.TurnPhase != turnPhase ||
                gameState.HasCallOccurred != hasCallOccurred ||
                !ReferenceEquals(gameState.PendingKakan, pendingKakan) ||
                gameState.Wall.Count != wallCount ||
                gameState.Wall.DeadWallCount != deadWallCount ||
                gameState.Wall.RemainingRinshanTileCount != remainingRinshanTileCount ||
                !HasSameSeats(activeSeats, gameState.ActiveTurnSeats) ||
                !HasSameSeatSlots(seatSlots, gameState) ||
                !HasSameCandidates(candidates, expectedReactionWindow.Candidates) ||
                !HasSameDiscards(discards, gameState.Discards) ||
                !HasSameDiscardClaims(discardClaims, gameState.DiscardClaims))
            {
                return false;
            }

            for (int i = 0; i < playerSeats.Length; i++)
            {
                if (!playerSeats[i].Matches(
                        gameState.GetPlayerSeat(playerSeats[i].Seat)))
                {
                    return false;
                }
            }

            return true;
        }

        private static SeatId[] CopySeats(IReadOnlyList<SeatId> seats)
        {
            SeatId[] copiedSeats = new SeatId[seats.Count];
            for (int i = 0; i < seats.Count; i++)
                copiedSeats[i] = seats[i];

            return copiedSeats;
        }

        private static CandidateSnapshot[] CopyCandidates(
            IReadOnlyList<ReactionWindowCandidate> reactionCandidates)
        {
            CandidateSnapshot[] copiedCandidates =
                new CandidateSnapshot[reactionCandidates.Count];
            for (int i = 0; i < reactionCandidates.Count; i++)
            {
                copiedCandidates[i] = new CandidateSnapshot(reactionCandidates[i]);
            }

            return copiedCandidates;
        }

        private static SeatSlotSnapshot[] CopySeatSlots(
            MahjongGameState gameState,
            IReadOnlyList<SeatId> seats)
        {
            SeatSlotSnapshot[] copiedSlots = new SeatSlotSnapshot[seats.Count];
            for (int i = 0; i < seats.Count; i++)
                copiedSlots[i] = new SeatSlotSnapshot(gameState.GetSeatSlot(seats[i]));

            return copiedSlots;
        }

        private static PlayerSeatSnapshot[] CopyPlayerSeats(
            MahjongGameState gameState,
            IReadOnlyList<SeatId> seats)
        {
            PlayerSeatSnapshot[] copiedSeats = new PlayerSeatSnapshot[seats.Count];
            for (int i = 0; i < seats.Count; i++)
            {
                copiedSeats[i] = new PlayerSeatSnapshot(
                    gameState.GetPlayerSeat(seats[i]));
            }

            return copiedSeats;
        }

        private static DiscardRecord[] CopyDiscards(
            IReadOnlyList<DiscardRecord> sourceDiscards)
        {
            DiscardRecord[] copiedDiscards = new DiscardRecord[sourceDiscards.Count];
            for (int i = 0; i < sourceDiscards.Count; i++)
                copiedDiscards[i] = sourceDiscards[i];

            return copiedDiscards;
        }

        private static DiscardClaimSnapshot[] CopyDiscardClaims(
            IReadOnlyDictionary<int, DiscardClaim> sourceClaims)
        {
            DiscardClaimSnapshot[] copiedClaims =
                new DiscardClaimSnapshot[sourceClaims.Count];
            int index = 0;
            foreach (KeyValuePair<int, DiscardClaim> pair in sourceClaims)
            {
                copiedClaims[index++] = new DiscardClaimSnapshot(pair.Key, pair.Value);
            }

            return copiedClaims;
        }

        private static bool HasSameSeats(
            IReadOnlyList<SeatId> expectedSeats,
            IReadOnlyList<SeatId> actualSeats)
        {
            if (expectedSeats.Count != actualSeats.Count)
                return false;

            for (int i = 0; i < expectedSeats.Count; i++)
            {
                if (expectedSeats[i] != actualSeats[i])
                    return false;
            }

            return true;
        }

        private static bool HasSameCandidates(
            IReadOnlyList<CandidateSnapshot> expectedCandidates,
            IReadOnlyList<ReactionWindowCandidate> actualCandidates)
        {
            if (expectedCandidates.Count != actualCandidates.Count)
                return false;

            for (int i = 0; i < expectedCandidates.Count; i++)
            {
                if (!expectedCandidates[i].Matches(actualCandidates[i]))
                    return false;
            }

            return true;
        }

        private static bool HasSameSeatSlots(
            IReadOnlyList<SeatSlotSnapshot> expectedSlots,
            MahjongGameState gameState)
        {
            for (int i = 0; i < expectedSlots.Count; i++)
            {
                if (!expectedSlots[i].Matches(
                        gameState.GetSeatSlot(expectedSlots[i].Seat)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSameDiscards(
            IReadOnlyList<DiscardRecord> expectedDiscards,
            IReadOnlyList<DiscardRecord> actualDiscards)
        {
            if (expectedDiscards.Count != actualDiscards.Count)
                return false;

            for (int i = 0; i < expectedDiscards.Count; i++)
            {
                DiscardRecord expected = expectedDiscards[i];
                DiscardRecord actual = actualDiscards[i];
                if (expected.Id != actual.Id ||
                    expected.ActorSeat != actual.ActorSeat ||
                    expected.Tile != actual.Tile ||
                    expected.TurnIndex != actual.TurnIndex ||
                    expected.Source != actual.Source ||
                    expected.IsLastLiveWallDiscard != actual.IsLastLiveWallDiscard)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSameDiscardClaims(
            IReadOnlyList<DiscardClaimSnapshot> expectedClaims,
            IReadOnlyDictionary<int, DiscardClaim> actualClaims)
        {
            if (expectedClaims.Count != actualClaims.Count)
                return false;

            for (int i = 0; i < expectedClaims.Count; i++)
            {
                if (!expectedClaims[i].Matches(actualClaims))
                    return false;
            }

            return true;
        }

        private readonly struct CandidateSnapshot
        {
            public CandidateSnapshot(ReactionWindowCandidate candidate)
            {
                Candidate = candidate;
                ResponseState = candidate != null
                    ? candidate.ResponseState
                    : default;
            }

            private ReactionWindowCandidate Candidate { get; }
            private ReactionResponseState ResponseState { get; }

            public bool Matches(ReactionWindowCandidate candidate)
            {
                return ReferenceEquals(Candidate, candidate) &&
                    (candidate == null || candidate.ResponseState == ResponseState);
            }
        }

        private readonly struct SeatSlotSnapshot
        {
            private readonly PlayerId? playerId;
            private readonly ParticipantType? participantType;

            public SeatSlotSnapshot(SeatSlot seatSlot)
            {
                Seat = seatSlot.Wind;
                playerId = seatSlot.PlayerId;
                participantType = seatSlot.ParticipantType;
            }

            public SeatId Seat { get; }

            public bool Matches(SeatSlot seatSlot)
            {
                return seatSlot != null && seatSlot.Wind == Seat &&
                    seatSlot.PlayerId == playerId &&
                    seatSlot.ParticipantType == participantType;
            }
        }

        private readonly struct PlayerSeatSnapshot
        {
            private readonly Tile[] handTiles;
            private readonly PlayerMeld[] melds;
            private readonly Tile? drawnTile;
            private readonly bool isReachDeclared;
            private readonly bool isDoubleReachDeclared;
            private readonly int reachDeclaredTurnIndex;
            private readonly bool isIppatsuEligible;
            private readonly bool isTemporaryFuriten;
            private readonly bool isReachPassFuriten;

            public PlayerSeatSnapshot(PlayerSeat playerSeat)
            {
                Seat = playerSeat.SeatId;
                handTiles = CopyTiles(playerSeat.Hand.GetTiles());
                melds = CopyMelds(playerSeat.Melds);
                drawnTile = playerSeat.DrawnTile;
                isReachDeclared = playerSeat.IsReachDeclared;
                isDoubleReachDeclared = playerSeat.IsDoubleReachDeclared;
                reachDeclaredTurnIndex = playerSeat.ReachDeclaredTurnIndex;
                isIppatsuEligible = playerSeat.IsIppatsuEligible;
                isTemporaryFuriten = playerSeat.IsTemporaryFuriten;
                isReachPassFuriten = playerSeat.IsReachPassFuriten;
            }

            public SeatId Seat { get; }

            public bool Matches(PlayerSeat playerSeat)
            {
                return playerSeat != null && playerSeat.SeatId == Seat &&
                    HasSameTiles(handTiles, playerSeat.Hand.GetTiles()) &&
                    HasSameMelds(melds, playerSeat.Melds) &&
                    Nullable.Equals(drawnTile, playerSeat.DrawnTile) &&
                    isReachDeclared == playerSeat.IsReachDeclared &&
                    isDoubleReachDeclared == playerSeat.IsDoubleReachDeclared &&
                    reachDeclaredTurnIndex == playerSeat.ReachDeclaredTurnIndex &&
                    isIppatsuEligible == playerSeat.IsIppatsuEligible &&
                    isTemporaryFuriten == playerSeat.IsTemporaryFuriten &&
                    isReachPassFuriten == playerSeat.IsReachPassFuriten;
            }

            private static Tile[] CopyTiles(IReadOnlyList<Tile> sourceTiles)
            {
                Tile[] copiedTiles = new Tile[sourceTiles.Count];
                for (int i = 0; i < sourceTiles.Count; i++)
                    copiedTiles[i] = sourceTiles[i];

                return copiedTiles;
            }

            private static PlayerMeld[] CopyMelds(IReadOnlyList<PlayerMeld> sourceMelds)
            {
                PlayerMeld[] copiedMelds = new PlayerMeld[sourceMelds.Count];
                for (int i = 0; i < sourceMelds.Count; i++)
                    copiedMelds[i] = sourceMelds[i];

                return copiedMelds;
            }

            private static bool HasSameTiles(
                IReadOnlyList<Tile> expectedTiles,
                IReadOnlyList<Tile> actualTiles)
            {
                if (expectedTiles.Count != actualTiles.Count)
                    return false;

                for (int i = 0; i < expectedTiles.Count; i++)
                {
                    if (expectedTiles[i] != actualTiles[i])
                        return false;
                }

                return true;
            }

            private static bool HasSameMelds(
                IReadOnlyList<PlayerMeld> expectedMelds,
                IReadOnlyList<PlayerMeld> actualMelds)
            {
                if (expectedMelds.Count != actualMelds.Count)
                    return false;

                for (int i = 0; i < expectedMelds.Count; i++)
                {
                    if (!ReferenceEquals(expectedMelds[i], actualMelds[i]))
                        return false;
                }

                return true;
            }
        }

        private readonly struct DiscardClaimSnapshot
        {
            private readonly int key;
            private readonly int discardId;
            private readonly SeatId claimingSeat;
            private readonly PlayerMeld meld;

            public DiscardClaimSnapshot(int key, DiscardClaim claim)
            {
                this.key = key;
                discardId = claim.DiscardId;
                claimingSeat = claim.ClaimingSeat;
                meld = claim.Meld;
            }

            public bool Matches(IReadOnlyDictionary<int, DiscardClaim> claims)
            {
                return claims.TryGetValue(key, out DiscardClaim claim) &&
                    claim.DiscardId == discardId &&
                    claim.ClaimingSeat == claimingSeat &&
                    ReferenceEquals(claim.Meld, meld);
            }
        }
    }
}
