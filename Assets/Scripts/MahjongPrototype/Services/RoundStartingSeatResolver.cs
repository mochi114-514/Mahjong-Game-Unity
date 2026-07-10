using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class RoundStartingSeatResolver
    {
        public SeatId Resolve(IReadOnlyList<SeatId> activeTurnSeats)
        {
            if (activeTurnSeats == null)
                throw new ArgumentNullException(nameof(activeTurnSeats));

            if (activeTurnSeats.Count <= 0)
            {
                throw new InvalidOperationException(
                    "Cannot resolve a round starting seat because no active turn seats are available.");
            }

            for (int i = 0; i < activeTurnSeats.Count; i++)
            {
                if (activeTurnSeats[i] == SeatId.East)
                    return SeatId.East;
            }

            return activeTurnSeats[0];
        }
    }
}
