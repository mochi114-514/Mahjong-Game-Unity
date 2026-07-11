using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum ReactionKind
    {
        Ron,
        Pon
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
        RonDeclared,
        PonDeclared
    }

    public abstract class ReactionWindowCandidateDetail
    {
    }

    public sealed class RonReactionWindowCandidateDetail : ReactionWindowCandidateDetail
    {
        public RonReactionWindowCandidateDetail(WinDeclarationEvaluationResult evaluation)
        {
            Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        }

        public WinDeclarationEvaluationResult Evaluation { get; }
    }

    public sealed class PonReactionWindowCandidateDetail : ReactionWindowCandidateDetail
    {
        public PonReactionWindowCandidateDetail(Tile calledTile)
        {
            if (!calledTile.IsValid)
                throw new ArgumentException("Called tile must be valid.", nameof(calledTile));

            CalledTile = calledTile;
        }

        public Tile CalledTile { get; }
    }

    public sealed class ReactionWindowCandidate
    {
        // Compatibility constructor retained for existing ron call sites and tests.
        public ReactionWindowCandidate(
            SeatId seat,
            ReactionKind kind,
            WinDeclarationEvaluationResult winDeclarationEvaluation)
            : this(
                seat,
                kind,
                kind == ReactionKind.Ron
                    ? new RonReactionWindowCandidateDetail(winDeclarationEvaluation)
                    : throw new ArgumentException("Use the pon factory for a pon candidate.", nameof(kind)))
        {
        }

        public ReactionWindowCandidate(
            SeatId seat,
            ReactionKind kind,
            ReactionWindowCandidateDetail detail)
        {
            if (detail == null)
                throw new ArgumentNullException(nameof(detail));
            if ((kind == ReactionKind.Ron && !(detail is RonReactionWindowCandidateDetail)) ||
                (kind == ReactionKind.Pon && !(detail is PonReactionWindowCandidateDetail)))
            {
                throw new ArgumentException("Candidate detail does not match reaction kind.", nameof(detail));
            }

            Seat = seat;
            Kind = kind;
            Detail = detail;
            ResponseState = ReactionResponseState.Pending;
        }

        public static ReactionWindowCandidate CreatePon(SeatId seat, Tile calledTile)
        {
            return new ReactionWindowCandidate(
                seat,
                ReactionKind.Pon,
                new PonReactionWindowCandidateDetail(calledTile));
        }

        public SeatId Seat { get; }
        public ReactionKind Kind { get; }
        public ReactionWindowCandidateDetail Detail { get; }
        public RonReactionWindowCandidateDetail RonDetail =>
            Detail as RonReactionWindowCandidateDetail;
        public PonReactionWindowCandidateDetail PonDetail =>
            Detail as PonReactionWindowCandidateDetail;
        // Compatibility projection for the existing win declaration and result paths.
        public WinDeclarationEvaluationResult WinDeclarationEvaluation => RonDetail?.Evaluation;
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

        public ReactionWindowCandidate PendingCandidate =>
            FindPendingCandidate(ReactionKind.Ron) ?? FindPendingCandidate(ReactionKind.Pon);

        public ReactionWindowCandidate PendingRonCandidate =>
            FindPendingCandidate(ReactionKind.Ron);

        public ReactionWindowCandidate PendingPonCandidate =>
            PendingRonCandidate == null
                ? FindPendingCandidate(ReactionKind.Pon)
                : null;

        private ReactionWindowCandidate FindPendingCandidate(ReactionKind kind)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = candidates[i];
                if (candidate.Kind == kind && candidate.IsPending)
                    return candidate;
            }

            return null;
        }
    }

    public readonly struct ReactionWindowResolution
    {
        private ReactionWindowResolution(
            ReactionWindowResolutionType type,
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate,
            OpenMeld openMeld)
        {
            Type = type;
            SourceDiscard = sourceDiscard;
            Candidate = candidate;
            OpenMeld = openMeld;
        }

        public static ReactionWindowResolution None => new ReactionWindowResolution(
            ReactionWindowResolutionType.None,
            default,
            null,
            null);

        public ReactionWindowResolutionType Type { get; }
        public DiscardRecord SourceDiscard { get; }
        public ReactionWindowCandidate Candidate { get; }
        public OpenMeld OpenMeld { get; }
        public bool IsResolved => Type != ReactionWindowResolutionType.None;

        public static ReactionWindowResolution NoReaction(DiscardRecord sourceDiscard)
        {
            return new ReactionWindowResolution(
                ReactionWindowResolutionType.NoReaction,
                sourceDiscard,
                null,
                null);
        }

        public static ReactionWindowResolution Pending(DiscardRecord sourceDiscard)
        {
            return new ReactionWindowResolution(
                ReactionWindowResolutionType.None,
                sourceDiscard,
                null,
                null);
        }

        public static ReactionWindowResolution RonDeclared(
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate)
        {
            if (candidate == null || candidate.Kind != ReactionKind.Ron)
                throw new ArgumentException("A ron resolution requires a ron candidate.", nameof(candidate));

            return new ReactionWindowResolution(
                ReactionWindowResolutionType.RonDeclared,
                sourceDiscard,
                candidate,
                null);
        }

        public static ReactionWindowResolution PonDeclared(
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate,
            OpenMeld openMeld)
        {
            if (candidate == null || candidate.Kind != ReactionKind.Pon)
                throw new ArgumentException("A pon resolution requires a pon candidate.", nameof(candidate));
            if (openMeld == null)
                throw new ArgumentNullException(nameof(openMeld));

            return new ReactionWindowResolution(
                ReactionWindowResolutionType.PonDeclared,
                sourceDiscard,
                candidate,
                openMeld);
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
