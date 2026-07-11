using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum ReactionKind
    {
        Ron
    }

    public enum ReactionResponseState
    {
        Pending,
        Declined,
        Declared
    }

    public enum ReactionWindowResolutionType
    {
        None,
        NoReaction,
        RonDeclared
    }

    public sealed class ReactionWindowCandidate
    {
        public ReactionWindowCandidate(
            SeatId seat,
            ReactionKind kind,
            WinDeclarationEvaluationResult winDeclarationEvaluation)
        {
            Seat = seat;
            Kind = kind;
            WinDeclarationEvaluation = winDeclarationEvaluation;
            ResponseState = ReactionResponseState.Pending;
        }

        public SeatId Seat { get; }
        public ReactionKind Kind { get; }
        public WinDeclarationEvaluationResult WinDeclarationEvaluation { get; }
        public ReactionResponseState ResponseState { get; private set; }
        public bool IsPending => ResponseState == ReactionResponseState.Pending;

        internal void Declare()
        {
            ResponseState = ReactionResponseState.Declared;
        }

        internal void Decline()
        {
            ResponseState = ReactionResponseState.Declined;
        }
    }

    public sealed class ReactionWindow
    {
        private readonly List<ReactionWindowCandidate> candidates;

        public ReactionWindow(
            int windowId,
            DiscardRecord sourceDiscard,
            int turnIndex,
            IReadOnlyList<ReactionWindowCandidate> candidates)
        {
            WindowId = windowId;
            SourceDiscard = sourceDiscard;
            TurnIndex = turnIndex;
            this.candidates = new List<ReactionWindowCandidate>();

            if (candidates == null)
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null)
                    this.candidates.Add(candidates[i]);
            }
        }

        public int WindowId { get; }
        public DiscardRecord SourceDiscard { get; }
        public int TurnIndex { get; }
        public IReadOnlyList<ReactionWindowCandidate> Candidates => candidates;

        public ReactionWindowCandidate PendingRonCandidate
        {
            get
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    ReactionWindowCandidate candidate = candidates[i];
                    if (candidate.Kind == ReactionKind.Ron && candidate.IsPending)
                        return candidate;
                }

                return null;
            }
        }
    }

    public readonly struct ReactionWindowResolution
    {
        private ReactionWindowResolution(
            ReactionWindowResolutionType type,
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate)
        {
            Type = type;
            SourceDiscard = sourceDiscard;
            Candidate = candidate;
        }

        public static ReactionWindowResolution None => new ReactionWindowResolution(
            ReactionWindowResolutionType.None,
            default,
            null);

        public ReactionWindowResolutionType Type { get; }
        public DiscardRecord SourceDiscard { get; }
        public ReactionWindowCandidate Candidate { get; }
        public bool IsResolved => Type != ReactionWindowResolutionType.None;

        public static ReactionWindowResolution NoReaction(DiscardRecord sourceDiscard)
        {
            return new ReactionWindowResolution(
                ReactionWindowResolutionType.NoReaction,
                sourceDiscard,
                null);
        }

        public static ReactionWindowResolution RonDeclared(
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            return new ReactionWindowResolution(
                ReactionWindowResolutionType.RonDeclared,
                sourceDiscard,
                candidate);
        }
    }

    public readonly struct ReactionWindowAnswerResult
    {
        private const int NoWindowId = 0;

        private ReactionWindowAnswerResult(
            bool accepted,
            int windowId,
            ReactionWindowCandidate candidate,
            ReactionWindowResolution resolution,
            string reason)
        {
            Accepted = accepted;
            WindowId = windowId;
            Candidate = candidate;
            Resolution = resolution;
            Reason = reason ?? string.Empty;
        }

        public static ReactionWindowAnswerResult Rejected(string reason)
        {
            return new ReactionWindowAnswerResult(
                false,
                NoWindowId,
                null,
                ReactionWindowResolution.None,
                reason);
        }

        public static ReactionWindowAnswerResult AcceptedAnswer(
            int windowId,
            ReactionWindowCandidate candidate,
            ReactionWindowResolution resolution)
        {
            return new ReactionWindowAnswerResult(
                true,
                windowId,
                candidate,
                resolution,
                string.Empty);
        }

        public bool Accepted { get; }
        public int WindowId { get; }
        public ReactionWindowCandidate Candidate { get; }
        public ReactionWindowResolution Resolution { get; }
        public string Reason { get; }
    }
}
