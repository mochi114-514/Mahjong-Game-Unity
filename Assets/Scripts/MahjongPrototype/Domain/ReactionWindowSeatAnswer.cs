using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    /// <summary>
    /// A seat-level response to a reaction window.  This is intentionally
    /// separate from <see cref="ReactionKind"/>, because pass is a response,
    /// not a reaction candidate.
    /// </summary>
    public enum ReactionWindowSeatAnswerKind
    {
        Pass = 0,
        Ron = 1,
        Pon = 2,
        Chi = 3,
        Daiminkan = 4
    }

    /// <summary>
    /// An immutable response submitted by one eligible seat.  Validation
    /// against a concrete reaction window is performed by
    /// <see cref="ReactionWindowSeatAnswerCollection"/> when it is registered.
    /// </summary>
    public sealed class ReactionWindowSeatAnswer
    {
        public ReactionWindowSeatAnswer(
            int windowId,
            SeatId seat,
            ReactionWindowSeatAnswerKind kind)
            : this(windowId, seat, kind, null)
        {
        }

        public ReactionWindowSeatAnswer(
            int windowId,
            SeatId seat,
            ReactionWindowSeatAnswerKind kind,
            int? chiOptionId)
        {
            WindowId = windowId;
            Seat = seat;
            Kind = kind;
            ChiOptionId = chiOptionId;
        }

        public int WindowId { get; }
        public SeatId Seat { get; }
        public ReactionWindowSeatAnswerKind Kind { get; }
        public int? ChiOptionId { get; }

        public static ReactionWindowSeatAnswer Pass(int windowId, SeatId seat)
        {
            return new ReactionWindowSeatAnswer(
                windowId,
                seat,
                ReactionWindowSeatAnswerKind.Pass);
        }

        public static ReactionWindowSeatAnswer Ron(int windowId, SeatId seat)
        {
            return new ReactionWindowSeatAnswer(
                windowId,
                seat,
                ReactionWindowSeatAnswerKind.Ron);
        }

        public static ReactionWindowSeatAnswer Pon(int windowId, SeatId seat)
        {
            return new ReactionWindowSeatAnswer(
                windowId,
                seat,
                ReactionWindowSeatAnswerKind.Pon);
        }

        public static ReactionWindowSeatAnswer Chi(
            int windowId,
            SeatId seat,
            int optionId)
        {
            return new ReactionWindowSeatAnswer(
                windowId,
                seat,
                ReactionWindowSeatAnswerKind.Chi,
                optionId);
        }

        public static ReactionWindowSeatAnswer Daiminkan(int windowId, SeatId seat)
        {
            return new ReactionWindowSeatAnswer(
                windowId,
                seat,
                ReactionWindowSeatAnswerKind.Daiminkan);
        }
    }

    /// <summary>
    /// Holds the one allowed response for every seat that has at least one
    /// candidate in a reaction window.  It does not alter candidate response
    /// state, window state, or any game state.
    /// </summary>
    public sealed class ReactionWindowSeatAnswerCollection
    {
        private readonly IReadOnlyList<SeatId> targetSeats;
        private readonly Dictionary<SeatId, List<ReactionWindowCandidate>>
            candidatesBySeat;
        private readonly Dictionary<SeatId, Dictionary<ReactionKind, ReactionWindowCandidate>>
            reactionCandidatesBySeat;
        private readonly Dictionary<SeatId, Dictionary<int, ReactionWindowCandidate>>
            chiCandidatesBySeat;
        private readonly Dictionary<SeatId, ReactionWindowSeatAnswer> answersBySeat;

        public ReactionWindowSeatAnswerCollection(ReactionWindow reactionWindow)
        {
            ReactionWindow = reactionWindow ??
                throw new ArgumentNullException(nameof(reactionWindow));

            candidatesBySeat =
                new Dictionary<SeatId, List<ReactionWindowCandidate>>();
            reactionCandidatesBySeat =
                new Dictionary<SeatId, Dictionary<ReactionKind, ReactionWindowCandidate>>();
            chiCandidatesBySeat =
                new Dictionary<SeatId, Dictionary<int, ReactionWindowCandidate>>();
            answersBySeat = new Dictionary<SeatId, ReactionWindowSeatAnswer>();

            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate == null)
                    continue;

                if (!candidatesBySeat.TryGetValue(candidate.Seat, out List<ReactionWindowCandidate> candidates))
                {
                    candidates = new List<ReactionWindowCandidate>();
                    candidatesBySeat.Add(candidate.Seat, candidates);
                }

                candidates.Add(candidate);
                RegisterCandidate(candidate);
            }

            List<SeatId> orderedTargetSeats = new List<SeatId>(candidatesBySeat.Keys);
            orderedTargetSeats.Sort();
            targetSeats = orderedTargetSeats.AsReadOnly();
        }

        public ReactionWindow ReactionWindow { get; }
        public int WindowId => ReactionWindow.WindowId;
        public IReadOnlyList<SeatId> TargetSeats => targetSeats;
        public bool HasUnansweredSeats => answersBySeat.Count < targetSeats.Count;
        public bool AreAllSeatsAnswered => !HasUnansweredSeats;

        public IReadOnlyList<ReactionWindowSeatAnswer> RegisteredAnswers
        {
            get
            {
                List<ReactionWindowSeatAnswer> orderedAnswers =
                    new List<ReactionWindowSeatAnswer>(answersBySeat.Count);
                for (int i = 0; i < targetSeats.Count; i++)
                {
                    SeatId seat = targetSeats[i];
                    if (answersBySeat.TryGetValue(seat, out ReactionWindowSeatAnswer answer))
                        orderedAnswers.Add(answer);
                }

                return orderedAnswers.AsReadOnly();
            }
        }

        public bool IsTargetSeat(SeatId seat)
        {
            return candidatesBySeat.ContainsKey(seat);
        }

        public bool TryGetRegisteredAnswer(
            SeatId seat,
            out ReactionWindowSeatAnswer answer)
        {
            return answersBySeat.TryGetValue(seat, out answer);
        }

        public ReactionWindowSeatAnswerRegistrationResult TryRegister(
            ReactionWindowSeatAnswer answer)
        {
            if (answer == null)
                return ReactionWindowSeatAnswerRegistrationResult.Rejected("ReactionAnswerMissing");

            if (answer.WindowId != WindowId)
                return ReactionWindowSeatAnswerRegistrationResult.Rejected("ReactionWindowMismatch");

            if (!IsTargetSeat(answer.Seat))
                return ReactionWindowSeatAnswerRegistrationResult.Rejected("NotReactionCandidateSeat");

            if (answersBySeat.ContainsKey(answer.Seat))
                return ReactionWindowSeatAnswerRegistrationResult.Rejected("ReactionSeatAlreadyAnswered");

            if (answer.Kind == ReactionWindowSeatAnswerKind.Pass)
            {
                if (answer.ChiOptionId.HasValue)
                {
                    return ReactionWindowSeatAnswerRegistrationResult.Rejected(
                        "ChiOptionNotAllowed");
                }

                return Register(answer);
            }

            if (!TryMapReactionKind(answer.Kind, out ReactionKind reactionKind))
            {
                return ReactionWindowSeatAnswerRegistrationResult.Rejected(
                    "ReactionKindUnsupported");
            }

            if (reactionKind != ReactionKind.Chi && answer.ChiOptionId.HasValue)
            {
                return ReactionWindowSeatAnswerRegistrationResult.Rejected(
                    "ChiOptionNotAllowed");
            }

            if (reactionKind == ReactionKind.Chi)
            {
                if (!answer.ChiOptionId.HasValue ||
                    !TryFindChiCandidate(
                        answer.Seat,
                        answer.ChiOptionId.Value,
                        out _))
                {
                    return ReactionWindowSeatAnswerRegistrationResult.Rejected(
                        "ChiOptionMissing");
                }
            }
            else if (!TryFindCandidate(answer.Seat, reactionKind, out _))
            {
                return ReactionWindowSeatAnswerRegistrationResult.Rejected(
                    "ReactionKindUnavailable");
            }

            return Register(answer);
        }

        internal bool TryGetDeclaredCandidate(
            ReactionWindowSeatAnswer answer,
            out ReactionWindowCandidate candidate)
        {
            candidate = null;
            if (answer == null || answer.WindowId != WindowId ||
                answer.Kind == ReactionWindowSeatAnswerKind.Pass ||
                !answersBySeat.TryGetValue(answer.Seat, out ReactionWindowSeatAnswer registeredAnswer) ||
                !ReferenceEquals(registeredAnswer, answer) ||
                !TryMapReactionKind(answer.Kind, out ReactionKind reactionKind))
            {
                return false;
            }

            if (reactionKind == ReactionKind.Chi)
            {
                return answer.ChiOptionId.HasValue &&
                    TryFindChiCandidate(
                        answer.Seat,
                        answer.ChiOptionId.Value,
                        out candidate);
            }

            return TryFindCandidate(answer.Seat, reactionKind, out candidate);
        }

        private ReactionWindowSeatAnswerRegistrationResult Register(
            ReactionWindowSeatAnswer answer)
        {
            answersBySeat.Add(answer.Seat, answer);
            return ReactionWindowSeatAnswerRegistrationResult.AcceptedAnswer(answer);
        }

        private bool TryFindCandidate(
            SeatId seat,
            ReactionKind kind,
            out ReactionWindowCandidate candidate)
        {
            candidate = null;
            if (!reactionCandidatesBySeat.TryGetValue(
                    seat,
                    out Dictionary<ReactionKind, ReactionWindowCandidate> candidates))
            {
                return false;
            }

            return candidates.TryGetValue(kind, out candidate);
        }

        private bool TryFindChiCandidate(
            SeatId seat,
            int optionId,
            out ReactionWindowCandidate candidate)
        {
            candidate = null;
            return chiCandidatesBySeat.TryGetValue(
                    seat,
                    out Dictionary<int, ReactionWindowCandidate> candidates) &&
                candidates.TryGetValue(optionId, out candidate);
        }

        private void RegisterCandidate(ReactionWindowCandidate candidate)
        {
            switch (candidate.Kind)
            {
                case ReactionKind.Ron:
                case ReactionKind.Pon:
                case ReactionKind.Daiminkan:
                    RegisterSingleCandidate(candidate);
                    return;
                case ReactionKind.Chi:
                    RegisterChiCandidate(candidate);
                    return;
                default:
                    throw new ArgumentException(
                        "Reaction window candidates must use a supported reaction kind.",
                        nameof(candidate));
            }
        }

        private void RegisterSingleCandidate(ReactionWindowCandidate candidate)
        {
            if (!reactionCandidatesBySeat.TryGetValue(
                    candidate.Seat,
                    out Dictionary<ReactionKind, ReactionWindowCandidate> candidates))
            {
                candidates = new Dictionary<ReactionKind, ReactionWindowCandidate>();
                reactionCandidatesBySeat.Add(candidate.Seat, candidates);
            }

            if (candidates.ContainsKey(candidate.Kind))
            {
                throw new ArgumentException(
                    "Reaction window candidates must not duplicate a seat and reaction kind.",
                    nameof(candidate));
            }

            candidates.Add(candidate.Kind, candidate);
        }

        private void RegisterChiCandidate(ReactionWindowCandidate candidate)
        {
            if (candidate.ChiDetail == null)
            {
                throw new ArgumentException(
                    "A chi reaction candidate must include chi details.",
                    nameof(candidate));
            }
            if (!chiCandidatesBySeat.TryGetValue(
                    candidate.Seat,
                    out Dictionary<int, ReactionWindowCandidate> candidates))
            {
                candidates = new Dictionary<int, ReactionWindowCandidate>();
                chiCandidatesBySeat.Add(candidate.Seat, candidates);
            }

            IReadOnlyList<ChiOption> options = candidate.ChiDetail.Options;
            for (int i = 0; i < options.Count; i++)
            {
                ChiOption option = options[i];
                if (option == null || candidates.ContainsKey(option.OptionId))
                {
                    throw new ArgumentException(
                        "Reaction window candidates must not duplicate a seat and chi option id.",
                        nameof(candidate));
                }

                candidates.Add(option.OptionId, candidate);
            }
        }

        private static bool TryMapReactionKind(
            ReactionWindowSeatAnswerKind answerKind,
            out ReactionKind reactionKind)
        {
            switch (answerKind)
            {
                case ReactionWindowSeatAnswerKind.Ron:
                    reactionKind = ReactionKind.Ron;
                    return true;
                case ReactionWindowSeatAnswerKind.Pon:
                    reactionKind = ReactionKind.Pon;
                    return true;
                case ReactionWindowSeatAnswerKind.Chi:
                    reactionKind = ReactionKind.Chi;
                    return true;
                case ReactionWindowSeatAnswerKind.Daiminkan:
                    reactionKind = ReactionKind.Daiminkan;
                    return true;
                default:
                    reactionKind = default;
                    return false;
            }
        }
    }

    public readonly struct ReactionWindowSeatAnswerRegistrationResult
    {
        private ReactionWindowSeatAnswerRegistrationResult(
            bool accepted,
            int windowId,
            ReactionWindowSeatAnswer answer,
            string reason)
        {
            Accepted = accepted;
            WindowId = windowId;
            Answer = answer;
            Reason = reason ?? string.Empty;
        }

        public bool Accepted { get; }
        public int WindowId { get; }
        public ReactionWindowSeatAnswer Answer { get; }
        public string Reason { get; }

        public static ReactionWindowSeatAnswerRegistrationResult AcceptedAnswer(
            ReactionWindowSeatAnswer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));

            return new ReactionWindowSeatAnswerRegistrationResult(
                true,
                answer.WindowId,
                answer,
                string.Empty);
        }

        public static ReactionWindowSeatAnswerRegistrationResult Rejected(string reason)
        {
            return new ReactionWindowSeatAnswerRegistrationResult(
                false,
                0,
                null,
                reason);
        }
    }

    public enum ReactionWindowSeatAnswerResolutionType
    {
        PendingAnswers = 0,
        NoReaction = 1,
        DeclarationSelected = 2
    }

    /// <summary>
    /// A pure priority decision for a fully collected set of seat responses.
    /// It intentionally contains no meld or commit result.
    /// </summary>
    public readonly struct ReactionWindowSeatAnswerResolution
    {
        private ReactionWindowSeatAnswerResolution(
            ReactionWindowSeatAnswerResolutionType type,
            int windowId,
            ReactionWindowSource source,
            SeatId? selectedSeat,
            ReactionKind? selectedKind,
            ReactionWindowCandidate candidate,
            int? chiOptionId,
            ReactionWindowSeatAnswer answer)
        {
            Type = type;
            WindowId = windowId;
            Source = source;
            SelectedSeat = selectedSeat;
            SelectedKind = selectedKind;
            Candidate = candidate;
            ChiOptionId = chiOptionId;
            Answer = answer;
        }

        public ReactionWindowSeatAnswerResolutionType Type { get; }
        public int WindowId { get; }
        public ReactionWindowSource Source { get; }
        public SeatId? SelectedSeat { get; }
        public ReactionKind? SelectedKind { get; }
        public ReactionWindowCandidate Candidate { get; }
        public int? ChiOptionId { get; }
        public ReactionWindowSeatAnswer Answer { get; }
        public bool IsPending => Type == ReactionWindowSeatAnswerResolutionType.PendingAnswers;
        public bool IsNoReaction => Type == ReactionWindowSeatAnswerResolutionType.NoReaction;
        public bool HasSelectedDeclaration =>
            Type == ReactionWindowSeatAnswerResolutionType.DeclarationSelected;

        public static ReactionWindowSeatAnswerResolution PendingAnswers(
            int windowId,
            ReactionWindowSource source)
        {
            return new ReactionWindowSeatAnswerResolution(
                ReactionWindowSeatAnswerResolutionType.PendingAnswers,
                windowId,
                source,
                null,
                null,
                null,
                null,
                null);
        }

        public static ReactionWindowSeatAnswerResolution NoReaction(
            int windowId,
            ReactionWindowSource source)
        {
            return new ReactionWindowSeatAnswerResolution(
                ReactionWindowSeatAnswerResolutionType.NoReaction,
                windowId,
                source,
                null,
                null,
                null,
                null,
                null);
        }

        public static ReactionWindowSeatAnswerResolution DeclarationSelected(
            int windowId,
            ReactionWindowSource source,
            ReactionWindowSeatAnswer answer,
            ReactionWindowCandidate candidate)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));
            if (candidate.Seat != answer.Seat)
                throw new ArgumentException("The selected candidate seat must match the answer seat.", nameof(candidate));
            if (!TryGetReactionKind(answer.Kind, out ReactionKind selectedKind) ||
                candidate.Kind != selectedKind)
            {
                throw new ArgumentException(
                    "The selected candidate must match the declared reaction kind.",
                    nameof(candidate));
            }
            if (selectedKind == ReactionKind.Chi && !answer.ChiOptionId.HasValue)
            {
                throw new ArgumentException(
                    "A selected chi declaration requires a chi option id.",
                    nameof(answer));
            }
            if (selectedKind != ReactionKind.Chi && answer.ChiOptionId.HasValue)
            {
                throw new ArgumentException(
                    "Only a selected chi declaration can include a chi option id.",
                    nameof(answer));
            }

            return new ReactionWindowSeatAnswerResolution(
                ReactionWindowSeatAnswerResolutionType.DeclarationSelected,
                windowId,
                source,
                answer.Seat,
                selectedKind,
                candidate,
                answer.ChiOptionId,
                answer);
        }

        private static bool TryGetReactionKind(
            ReactionWindowSeatAnswerKind answerKind,
            out ReactionKind reactionKind)
        {
            switch (answerKind)
            {
                case ReactionWindowSeatAnswerKind.Ron:
                    reactionKind = ReactionKind.Ron;
                    return true;
                case ReactionWindowSeatAnswerKind.Pon:
                    reactionKind = ReactionKind.Pon;
                    return true;
                case ReactionWindowSeatAnswerKind.Chi:
                    reactionKind = ReactionKind.Chi;
                    return true;
                case ReactionWindowSeatAnswerKind.Daiminkan:
                    reactionKind = ReactionKind.Daiminkan;
                    return true;
                default:
                    reactionKind = default;
                    return false;
            }
        }
    }
}
