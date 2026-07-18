using System;

namespace MahjongPrototype.Domain
{
    public enum MahjongAuthorityCommandKind
    {
        Draw = 0,
        DiscardHand = 1,
        DiscardDrawnTile = 2,
        DeclareWin = 3,
        CancelReachDiscardSelection = 4,
        ForceDrawSkill = 5
    }

    /// <summary>
    /// An active player operation addressed to the match authority. The actor
    /// identity is stable while seat and turn values make stale submissions
    /// rejectable at the authority boundary.
    /// </summary>
    public readonly struct MahjongAuthorityCommand
    {
        public MahjongAuthorityCommand(
            MahjongAuthorityCommandKind kind,
            PlayerId playerId,
            SeatId actorSeat,
            int turnIndex,
            int handIndex = -1,
            string textPayload = null)
        {
            Kind = kind;
            PlayerId = playerId;
            ActorSeat = actorSeat;
            TurnIndex = turnIndex;
            HandIndex = handIndex;
            TextPayload = textPayload;
        }

        public MahjongAuthorityCommandKind Kind { get; }
        public PlayerId PlayerId { get; }
        public SeatId ActorSeat { get; }
        public int TurnIndex { get; }
        public int HandIndex { get; }
        public string TextPayload { get; }
    }

    public readonly struct MahjongAuthorityCommandResult
    {
        private MahjongAuthorityCommandResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public bool Accepted { get; }
        public string Reason { get; }

        public static MahjongAuthorityCommandResult Succeeded()
        {
            return new MahjongAuthorityCommandResult(true, null);
        }

        public static MahjongAuthorityCommandResult Rejected(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A command rejection reason is required.", nameof(reason));

            return new MahjongAuthorityCommandResult(false, reason);
        }
    }

    public interface IMahjongAuthorityCommandPort
    {
        MahjongAuthorityCommandResult TryExecuteCommand(MahjongAuthorityCommand command);
    }
}
