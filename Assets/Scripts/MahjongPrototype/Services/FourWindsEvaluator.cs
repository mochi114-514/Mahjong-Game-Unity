using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class FourWindsEvaluator
    {
        private const int RequiredPlayerCount = 4;
        private const int RequiredDiscardCount = 4;

        public bool IsSatisfied(
            IReadOnlyList<SeatId> activeSeats,
            IReadOnlyList<DiscardRecord> discards,
            bool hasCallOccurred)
        {
            if (hasCallOccurred ||
                activeSeats == null ||
                activeSeats.Count != RequiredPlayerCount ||
                discards == null ||
                discards.Count != RequiredDiscardCount)
            {
                return false;
            }

            HashSet<SeatId> activeSeatSet = new HashSet<SeatId>(activeSeats);
            if (activeSeatSet.Count != RequiredPlayerCount)
                return false;

            Tile firstTile = discards[0].Tile;
            if (!IsWindTile(firstTile))
                return false;

            HashSet<SeatId> discardedSeats = new HashSet<SeatId>();
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord discard = discards[i];
                if (!activeSeatSet.Contains(discard.ActorSeat) ||
                    !discardedSeats.Add(discard.ActorSeat) ||
                    discard.Tile.TypeIndex != firstTile.TypeIndex)
                {
                    return false;
                }
            }

            return discardedSeats.Count == RequiredPlayerCount;
        }

        private static bool IsWindTile(Tile tile)
        {
            return tile.Honor == HonorKind.East ||
                tile.Honor == HonorKind.South ||
                tile.Honor == HonorKind.West ||
                tile.Honor == HonorKind.North;
        }
    }
}
