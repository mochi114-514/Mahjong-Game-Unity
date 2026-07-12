using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum ReactionKind
    {
        Ron,
        Pon,
        Chi
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
        PonDeclared,
        ChiDeclared
    }

    public abstract class ReactionWindowCandidateDetail
    {
    }

    public sealed class ChiOption
    {
        private readonly IReadOnlyList<Tile> handTiles;
        private readonly IReadOnlyList<Tile> meldTiles;

        public ChiOption(
            int optionId,
            Tile calledTile,
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<Tile> meldTiles)
        {
            if (!calledTile.IsNumberTile)
                throw new ArgumentException("Called tile must be a valid number tile.", nameof(calledTile));
            if (handTiles == null)
                throw new ArgumentNullException(nameof(handTiles));
            if (meldTiles == null)
                throw new ArgumentNullException(nameof(meldTiles));
            if (handTiles.Count != 2)
                throw new ArgumentException("Chi hand tiles must contain exactly two tiles.", nameof(handTiles));
            if (meldTiles.Count != 3)
                throw new ArgumentException("Chi meld tiles must contain exactly three tiles.", nameof(meldTiles));

            Tile[] copiedHandTiles = CopyTiles(handTiles);
            Tile[] copiedMeldTiles = CopyTiles(meldTiles);
            ValidateTiles(calledTile, copiedHandTiles, copiedMeldTiles, optionId);

            OptionId = optionId;
            CalledTile = calledTile;
            this.handTiles = Array.AsReadOnly(copiedHandTiles);
            this.meldTiles = Array.AsReadOnly(copiedMeldTiles);
        }

        public int OptionId { get; }
        public Tile CalledTile { get; }
        public IReadOnlyList<Tile> HandTiles => handTiles;
        public IReadOnlyList<Tile> MeldTiles => meldTiles;

        private static Tile[] CopyTiles(IReadOnlyList<Tile> tiles)
        {
            Tile[] copiedTiles = new Tile[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
                copiedTiles[i] = tiles[i];

            return copiedTiles;
        }

        private static void ValidateTiles(
            Tile calledTile,
            Tile[] handTiles,
            Tile[] meldTiles,
            int optionId)
        {
            for (int i = 0; i < meldTiles.Length; i++)
            {
                Tile tile = meldTiles[i];
                if (!tile.IsNumberTile || tile.Suit != calledTile.Suit)
                {
                    throw new ArgumentException(
                        "Chi meld tiles must be number tiles of the called tile suit.",
                        nameof(meldTiles));
                }

                if (tile.Rank != meldTiles[0].Rank + i)
                {
                    throw new ArgumentException(
                        "Chi meld tiles must be an ascending consecutive sequence.",
                        nameof(meldTiles));
                }
            }

            if (optionId != meldTiles[0].Rank)
            {
                throw new ArgumentException(
                    "Chi option id must match the meld starting rank.",
                    nameof(optionId));
            }

            int calledTileIndex = -1;
            for (int i = 0; i < meldTiles.Length; i++)
            {
                if (meldTiles[i] == calledTile)
                {
                    calledTileIndex = i;
                    break;
                }
            }

            if (calledTileIndex < 0)
            {
                throw new ArgumentException(
                    "Chi meld tiles must include the called tile.",
                    nameof(meldTiles));
            }

            Tile[] expectedHandTiles = new Tile[2];
            int expectedHandTileIndex = 0;
            for (int i = 0; i < meldTiles.Length; i++)
            {
                if (i == calledTileIndex)
                    continue;

                expectedHandTiles[expectedHandTileIndex++] = meldTiles[i];
            }

            bool matchesMeldTiles =
                (handTiles[0] == expectedHandTiles[0] && handTiles[1] == expectedHandTiles[1]) ||
                (handTiles[0] == expectedHandTiles[1] && handTiles[1] == expectedHandTiles[0]);
            if (!matchesMeldTiles)
            {
                throw new ArgumentException(
                    "Chi hand tiles must match the non-called meld tiles.",
                    nameof(handTiles));
            }
        }
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

    public sealed class ChiReactionWindowCandidateDetail : ReactionWindowCandidateDetail
    {
        private readonly IReadOnlyList<ChiOption> options;

        public ChiReactionWindowCandidateDetail(
            Tile calledTile,
            IReadOnlyList<ChiOption> options)
        {
            if (!calledTile.IsNumberTile)
                throw new ArgumentException("Called tile must be a valid number tile.", nameof(calledTile));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Count <= 0)
                throw new ArgumentException("Chi options must not be empty.", nameof(options));

            List<ChiOption> copiedOptions = new List<ChiOption>(options.Count);
            HashSet<int> optionIds = new HashSet<int>();
            for (int i = 0; i < options.Count; i++)
            {
                ChiOption option = options[i];
                if (option == null)
                    throw new ArgumentException("Chi options must not contain null.", nameof(options));
                if (option.CalledTile != calledTile)
                {
                    throw new ArgumentException(
                        "All chi options must use the called tile.",
                        nameof(options));
                }
                if (!optionIds.Add(option.OptionId))
                    throw new ArgumentException("Chi option ids must be unique.", nameof(options));

                copiedOptions.Add(option);
            }

            CalledTile = calledTile;
            this.options = copiedOptions.AsReadOnly();
        }

        public Tile CalledTile { get; }
        public IReadOnlyList<ChiOption> Options => options;
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
                (kind == ReactionKind.Pon && !(detail is PonReactionWindowCandidateDetail)) ||
                (kind == ReactionKind.Chi && !(detail is ChiReactionWindowCandidateDetail)))
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

        public static ReactionWindowCandidate CreateChi(
            SeatId seat,
            Tile calledTile,
            IReadOnlyList<ChiOption> options)
        {
            return new ReactionWindowCandidate(
                seat,
                ReactionKind.Chi,
                new ChiReactionWindowCandidateDetail(calledTile, options));
        }

        public SeatId Seat { get; }
        public ReactionKind Kind { get; }
        public ReactionWindowCandidateDetail Detail { get; }
        public RonReactionWindowCandidateDetail RonDetail =>
            Detail as RonReactionWindowCandidateDetail;
        public PonReactionWindowCandidateDetail PonDetail =>
            Detail as PonReactionWindowCandidateDetail;
        public ChiReactionWindowCandidateDetail ChiDetail =>
            Detail as ChiReactionWindowCandidateDetail;
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
            FindPendingCandidate(ReactionKind.Ron) ??
            FindPendingCandidate(ReactionKind.Pon) ??
            FindPendingCandidate(ReactionKind.Chi);

        public ReactionWindowCandidate PendingRonCandidate =>
            FindPendingCandidate(ReactionKind.Ron);

        public ReactionWindowCandidate PendingPonCandidate =>
            PendingRonCandidate == null
                ? FindPendingCandidate(ReactionKind.Pon)
                : null;

        public ReactionWindowCandidate PendingChiCandidate =>
            PendingRonCandidate == null && PendingPonCandidate == null
                ? FindPendingCandidate(ReactionKind.Chi)
                : null;

        internal void CloseMeldCallsExcept(ReactionWindowCandidate declaredCandidate)
        {
            if (declaredCandidate == null)
                throw new ArgumentNullException(nameof(declaredCandidate));

            for (int i = 0; i < candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = candidates[i];
                if (candidate == declaredCandidate || !candidate.IsPending ||
                    (candidate.Kind != ReactionKind.Pon && candidate.Kind != ReactionKind.Chi))
                {
                    continue;
                }

                candidate.Decline();
            }
        }

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

        public static ReactionWindowResolution ChiDeclared(
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate,
            OpenMeld openMeld)
        {
            if (candidate == null || candidate.Kind != ReactionKind.Chi)
                throw new ArgumentException("A chi resolution requires a chi candidate.", nameof(candidate));
            if (openMeld == null || openMeld.Type != OpenMeldType.Chi)
                throw new ArgumentException("A chi resolution requires a chi open meld.", nameof(openMeld));

            return new ReactionWindowResolution(
                ReactionWindowResolutionType.ChiDeclared,
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
