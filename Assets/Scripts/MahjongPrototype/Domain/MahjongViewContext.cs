using System;

namespace MahjongPrototype.Domain
{
    /// <summary>
    /// Per-client viewing identity. It derives the local seat from the current
    /// round assignment rather than persisting a seat across rounds.
    /// </summary>
    public sealed class MahjongViewContext
    {
        public MahjongViewContext(PlayerId localPlayerId)
        {
            LocalPlayerId = localPlayerId;
        }

        public PlayerId LocalPlayerId { get; }

        public bool TryGetSelfSeat(MahjongGameState gameState, out SeatId selfSeat)
        {
            selfSeat = default;
            if (gameState == null)
                return false;

            try
            {
                selfSeat = gameState.GetSeatByPlayerId(LocalPlayerId);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public bool IsSelfSeat(MahjongGameState gameState, SeatId seat)
        {
            return TryGetSelfSeat(gameState, out SeatId selfSeat) && selfSeat == seat;
        }

        public bool IsSelfPlayer(PlayerId? playerId)
        {
            return playerId.HasValue && playerId.Value == LocalPlayerId;
        }
    }
}
