using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class FourReachesEvaluator
    {
        private const int RequiredPlayerCount = 4;

        public bool IsSatisfied(
            IReadOnlyList<SeatId> activeSeats,
            IReadOnlyList<PlayerSeat> playerSeats,
            IReadOnlyList<DiscardRecord> discards,
            DiscardRecord? resolvedDiscard)
        {
            if (activeSeats == null ||
                activeSeats.Count != RequiredPlayerCount ||
                playerSeats == null ||
                playerSeats.Count != RequiredPlayerCount ||
                discards == null ||
                discards.Count <= 0 ||
                !resolvedDiscard.HasValue)
            {
                return false;
            }

            HashSet<SeatId> activeSeatSet = new HashSet<SeatId>(activeSeats);
            if (activeSeatSet.Count != RequiredPlayerCount)
                return false;

            Dictionary<SeatId, PlayerSeat> playerSeatsBySeat =
                new Dictionary<SeatId, PlayerSeat>();
            for (int i = 0; i < playerSeats.Count; i++)
            {
                PlayerSeat playerSeat = playerSeats[i];
                if (playerSeat == null ||
                    !activeSeatSet.Contains(playerSeat.SeatId) ||
                    playerSeatsBySeat.ContainsKey(playerSeat.SeatId))
                {
                    return false;
                }

                playerSeatsBySeat.Add(playerSeat.SeatId, playerSeat);
            }

            if (playerSeatsBySeat.Count != RequiredPlayerCount)
                return false;

            DiscardRecord discard = resolvedDiscard.Value;
            if (!IsSameDiscard(discard, discards[discards.Count - 1]) ||
                !playerSeatsBySeat.TryGetValue(discard.ActorSeat, out PlayerSeat actorSeat) ||
                actorSeat.ReachDeclaredTurnIndex != discard.TurnIndex)
            {
                return false;
            }

            foreach (SeatId seat in activeSeatSet)
            {
                if (!playerSeatsBySeat.TryGetValue(seat, out PlayerSeat playerSeat) ||
                    !playerSeat.IsReachDeclared)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSameDiscard(DiscardRecord left, DiscardRecord right)
        {
            return left.Id == right.Id &&
                left.ActorSeat == right.ActorSeat &&
                left.Tile == right.Tile &&
                left.TurnIndex == right.TurnIndex &&
                left.Source == right.Source &&
                left.IsLastLiveWallDiscard == right.IsLastLiveWallDiscard;
        }
    }
}
