using System;

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
    /// current seat, and turn.
    /// </summary>
    public sealed class DecisionRequest
    {
        public DecisionRequest(
            long requestId,
            DecisionKind kind,
            PlayerId playerId,
            SeatId actorSeat,
            int turnIndex)
        {
            RequestId = requestId;
            Kind = kind;
            PlayerId = playerId;
            ActorSeat = actorSeat;
            TurnIndex = turnIndex;
        }

        public long RequestId { get; }
        public DecisionKind Kind { get; }
        public PlayerId PlayerId { get; }
        public SeatId ActorSeat { get; }
        public int TurnIndex { get; }
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
        {
            RequestId = requestId;
            Kind = kind;
            PlayerId = playerId;
            ActorSeat = actorSeat;
            TurnIndex = turnIndex;
            Accepted = accepted;
        }

        public long RequestId { get; }
        public DecisionKind Kind { get; }
        public PlayerId PlayerId { get; }
        public SeatId ActorSeat { get; }
        public int TurnIndex { get; }
        public bool Accepted { get; }
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
