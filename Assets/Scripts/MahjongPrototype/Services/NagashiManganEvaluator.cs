using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class NagashiManganEvaluator
    {
        private static readonly IReadOnlyList<SeatId> EmptySeats =
            new List<SeatId>().AsReadOnly();

        public IReadOnlyList<SeatId> Evaluate(
            IReadOnlyList<SeatId> activeSeats,
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims)
        {
            if (activeSeats == null || activeSeats.Count == 0 || discards == null)
                return EmptySeats;

            HashSet<SeatId> evaluatedSeats = new HashSet<SeatId>();
            List<SeatId> satisfiedSeats = new List<SeatId>();
            for (int i = 0; i < activeSeats.Count; i++)
            {
                SeatId seat = activeSeats[i];
                if (!Enum.IsDefined(typeof(SeatId), seat) ||
                    !evaluatedSeats.Add(seat) ||
                    !IsSatisfied(seat, discards, discardClaims))
                {
                    continue;
                }

                satisfiedSeats.Add(seat);
            }

            return satisfiedSeats.Count == 0
                ? EmptySeats
                : satisfiedSeats.AsReadOnly();
        }

        private static bool IsSatisfied(
            SeatId seat,
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims)
        {
            bool hasDiscard = false;
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord discard = discards[i];
                if (discard.ActorSeat != seat)
                    continue;

                hasDiscard = true;
                if (!IsTerminalOrHonor(discard.Tile) ||
                    (discardClaims != null && discardClaims.ContainsKey(discard.Id)))
                {
                    return false;
                }
            }

            return hasDiscard;
        }

        private static bool IsTerminalOrHonor(Tile tile)
        {
            return tile.IsHonorTile ||
                (tile.IsNumberTile && (tile.Rank == 1 || tile.Rank == 9));
        }
    }
}
