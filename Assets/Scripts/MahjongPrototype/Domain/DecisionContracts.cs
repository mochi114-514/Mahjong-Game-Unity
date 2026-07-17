using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum DecisionKind
    {
        WinDeclaration = 0,
        Reaction = 1,
        Reach = 2,
        SelfKan = 3
    }

    /// <summary>
    /// A request for a choice that the authority has already determined is
    /// available. It contains no rule evaluation and is scoped to a player,
    /// actor seat, and relevant turn. A reaction actor is not necessarily the
    /// current turn seat.
    /// </summary>
    public sealed class DecisionRequest
    {
        public DecisionRequest(
            long requestId,
            DecisionKind kind,
            PlayerId playerId,
            SeatId actorSeat,
            int turnIndex)
            : this(
                requestId,
                kind,
                playerId,
                actorSeat,
                turnIndex,
                null)
        {
        }

        public DecisionRequest(
            long requestId,
            DecisionKind kind,
            PlayerId playerId,
            SeatId actorSeat,
            int turnIndex,
            ReactionDecisionRequest reaction)
        {
            RequestId = requestId;
            Kind = kind;
            PlayerId = playerId;
            ActorSeat = actorSeat;
            TurnIndex = turnIndex;
            Reaction = reaction;
        }

        public long RequestId { get; }
        public DecisionKind Kind { get; }
        public PlayerId PlayerId { get; }
        public SeatId ActorSeat { get; }
        public int TurnIndex { get; }
        /// <summary>
        /// Immutable data for a reaction response. It is deliberately a value
        /// projection rather than a mutable ReactionWindow or candidate.
        /// </summary>
        public ReactionDecisionRequest Reaction { get; }

        public bool TryValidateResponsePayload(
            DecisionResponse response,
            out string reason)
        {
            reason = string.Empty;
            if (Kind != DecisionKind.Reaction)
                return true;

            if (Reaction == null)
            {
                reason = "ReactionDecisionRequestMissing";
                return false;
            }

            if (response == null || response.Reaction == null)
            {
                reason = "ReactionDecisionResponseMissing";
                return false;
            }

            return Reaction.TryValidateResponse(response.Reaction, out reason);
        }
    }

    public sealed class DecisionResponse
    {
        public DecisionResponse(
            long requestId,
            DecisionKind kind,
            PlayerId playerId,
            SeatId actorSeat,
            int turnIndex,
            bool accepted)
            : this(
                requestId,
                kind,
                playerId,
                actorSeat,
                turnIndex,
                accepted,
                null)
        {
        }

        public DecisionResponse(
            long requestId,
            DecisionKind kind,
            PlayerId playerId,
            SeatId actorSeat,
            int turnIndex,
            bool accepted,
            ReactionDecisionResponse reaction)
        {
            RequestId = requestId;
            Kind = kind;
            PlayerId = playerId;
            ActorSeat = actorSeat;
            TurnIndex = turnIndex;
            Accepted = accepted;
            Reaction = reaction;
        }

        public long RequestId { get; }
        public DecisionKind Kind { get; }
        public PlayerId PlayerId { get; }
        public SeatId ActorSeat { get; }
        public int TurnIndex { get; }
        public bool Accepted { get; }
        public ReactionDecisionResponse Reaction { get; }
    }

    /// <summary>
    /// Immutable, provider-facing projection of one seat's choices in a
    /// reaction window. It intentionally contains no mutable domain objects.
    /// </summary>
    public sealed class ReactionDecisionRequest
    {
        private readonly IReadOnlyList<ReactionDecisionOption> options;

        public ReactionDecisionRequest(
            int windowId,
            ReactionWindowSourceKind sourceKind,
            SeatId sourceSeat,
            Tile sourceTile,
            int sourceTurnIndex,
            IReadOnlyList<ReactionDecisionOption> options)
        {
            if (windowId <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowId));
            if (!sourceTile.IsValid)
                throw new ArgumentException("Reaction source tile must be valid.", nameof(sourceTile));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            List<ReactionDecisionOption> copiedOptions =
                new List<ReactionDecisionOption>(options.Count);
            HashSet<ReactionWindowSeatAnswerKind> kinds =
                new HashSet<ReactionWindowSeatAnswerKind>();
            for (int i = 0; i < options.Count; i++)
            {
                ReactionDecisionOption option = options[i];
                if (option == null)
                    throw new ArgumentException("Reaction options must not contain null.", nameof(options));
                if (!kinds.Add(option.Kind))
                    throw new ArgumentException("Reaction option kinds must be unique.", nameof(options));

                copiedOptions.Add(option);
            }

            if (!kinds.Contains(ReactionWindowSeatAnswerKind.Pass))
            {
                throw new ArgumentException(
                    "A reaction request must allow pass.",
                    nameof(options));
            }

            WindowId = windowId;
            SourceKind = sourceKind;
            SourceSeat = sourceSeat;
            SourceTile = sourceTile;
            SourceTurnIndex = sourceTurnIndex;
            this.options = copiedOptions.AsReadOnly();
        }

        public int WindowId { get; }
        public ReactionWindowSourceKind SourceKind { get; }
        public SeatId SourceSeat { get; }
        public Tile SourceTile { get; }
        public int SourceTurnIndex { get; }
        public IReadOnlyList<ReactionDecisionOption> Options => options;

        public bool Allows(ReactionWindowSeatAnswerKind kind)
        {
            return FindOption(kind) != null;
        }

        public IReadOnlyList<ReactionDecisionChiOption> GetChiOptions()
        {
            ReactionDecisionOption chi = FindOption(ReactionWindowSeatAnswerKind.Chi);
            return chi != null
                ? chi.ChiOptions
                : Array.Empty<ReactionDecisionChiOption>();
        }

        public bool TryValidateResponse(
            ReactionDecisionResponse response,
            out string reason)
        {
            reason = string.Empty;
            if (response == null)
            {
                reason = "ReactionDecisionResponseMissing";
                return false;
            }

            if (response.WindowId != WindowId)
            {
                reason = "ReactionWindowMismatch";
                return false;
            }

            ReactionDecisionOption option = FindOption(response.Kind);
            if (option == null)
            {
                reason = "ReactionKindUnavailable";
                return false;
            }

            if (response.Kind != ReactionWindowSeatAnswerKind.Chi)
            {
                if (response.ChiOptionId.HasValue)
                {
                    reason = "ChiOptionNotAllowed";
                    return false;
                }

                return true;
            }

            if (!response.ChiOptionId.HasValue ||
                !option.ContainsChiOption(response.ChiOptionId.Value))
            {
                reason = "ChiOptionMissing";
                return false;
            }

            return true;
        }

        private ReactionDecisionOption FindOption(ReactionWindowSeatAnswerKind kind)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Kind == kind)
                    return options[i];
            }

            return null;
        }
    }

    /// <summary>
    /// One response type available to a seat. Chi carries copied option data;
    /// all other answer types carry no option payload.
    /// </summary>
    public sealed class ReactionDecisionOption
    {
        private readonly IReadOnlyList<ReactionDecisionChiOption> chiOptions;

        public ReactionDecisionOption(
            ReactionWindowSeatAnswerKind kind,
            IReadOnlyList<ReactionDecisionChiOption> chiOptions = null)
        {
            if (!IsSupported(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            List<ReactionDecisionChiOption> copiedOptions =
                new List<ReactionDecisionChiOption>();
            if (chiOptions != null)
            {
                HashSet<int> optionIds = new HashSet<int>();
                for (int i = 0; i < chiOptions.Count; i++)
                {
                    ReactionDecisionChiOption option = chiOptions[i];
                    if (option == null)
                    {
                        throw new ArgumentException(
                            "Chi options must not contain null.",
                            nameof(chiOptions));
                    }
                    if (!optionIds.Add(option.OptionId))
                    {
                        throw new ArgumentException(
                            "Chi option ids must be unique.",
                            nameof(chiOptions));
                    }

                    copiedOptions.Add(option);
                }
            }

            if (kind == ReactionWindowSeatAnswerKind.Chi &&
                copiedOptions.Count <= 0)
            {
                throw new ArgumentException(
                    "A chi reaction option must include at least one chi option.",
                    nameof(chiOptions));
            }
            if (kind != ReactionWindowSeatAnswerKind.Chi &&
                copiedOptions.Count > 0)
            {
                throw new ArgumentException(
                    "Only a chi reaction option can include chi options.",
                    nameof(chiOptions));
            }

            Kind = kind;
            this.chiOptions = copiedOptions.AsReadOnly();
        }

        public ReactionWindowSeatAnswerKind Kind { get; }
        public IReadOnlyList<ReactionDecisionChiOption> ChiOptions => chiOptions;

        internal bool ContainsChiOption(int optionId)
        {
            for (int i = 0; i < chiOptions.Count; i++)
            {
                if (chiOptions[i].OptionId == optionId)
                    return true;
            }

            return false;
        }

        private static bool IsSupported(ReactionWindowSeatAnswerKind kind)
        {
            switch (kind)
            {
                case ReactionWindowSeatAnswerKind.Pass:
                case ReactionWindowSeatAnswerKind.Ron:
                case ReactionWindowSeatAnswerKind.Pon:
                case ReactionWindowSeatAnswerKind.Chi:
                case ReactionWindowSeatAnswerKind.Daiminkan:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Copy of one chi option suitable for a provider/UI. The copied tile lists
    /// cannot expose a ReactionWindowCandidate's mutable response state.
    /// </summary>
    public sealed class ReactionDecisionChiOption
    {
        private readonly IReadOnlyList<Tile> handTiles;
        private readonly IReadOnlyList<Tile> meldTiles;

        public ReactionDecisionChiOption(
            int optionId,
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<Tile> meldTiles)
        {
            if (handTiles == null)
                throw new ArgumentNullException(nameof(handTiles));
            if (meldTiles == null)
                throw new ArgumentNullException(nameof(meldTiles));

            this.handTiles = CopyTiles(handTiles);
            this.meldTiles = CopyTiles(meldTiles);
            OptionId = optionId;
        }

        public int OptionId { get; }
        public IReadOnlyList<Tile> HandTiles => handTiles;
        public IReadOnlyList<Tile> MeldTiles => meldTiles;

        private static IReadOnlyList<Tile> CopyTiles(IReadOnlyList<Tile> tiles)
        {
            List<Tile> copiedTiles = new List<Tile>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++)
                copiedTiles.Add(tiles[i]);

            return copiedTiles.AsReadOnly();
        }
    }

    /// <summary>
    /// Immutable Reaction payload of a decision response. Pass is represented
    /// explicitly rather than by a false generic Accepted flag.
    /// </summary>
    public sealed class ReactionDecisionResponse
    {
        public ReactionDecisionResponse(
            int windowId,
            ReactionWindowSeatAnswerKind kind,
            int? chiOptionId = null)
        {
            WindowId = windowId;
            Kind = kind;
            ChiOptionId = chiOptionId;
        }

        public int WindowId { get; }
        public ReactionWindowSeatAnswerKind Kind { get; }
        public int? ChiOptionId { get; }
    }

    public interface IDecisionProvider
    {
        DecisionProviderRoute Route { get; }
        bool IsAvailable { get; }
        void RequestDecision(
            DecisionRequest request,
            Func<DecisionResponse, DecisionResponseResult> respond);
        void CancelDecision(long requestId);
    }

    public readonly struct DecisionResponseResult
    {
        private DecisionResponseResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public bool Accepted { get; }
        public string Reason { get; }

        public static DecisionResponseResult Succeeded()
        {
            return new DecisionResponseResult(true, null);
        }

        public static DecisionResponseResult Rejected(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A decision response rejection reason is required.", nameof(reason));

            return new DecisionResponseResult(false, reason);
        }
    }

    public interface IMahjongAuthorityDecisionPort
    {
        DecisionResponseResult TryExecuteDecisionResponse(DecisionResponse response);
    }
}
