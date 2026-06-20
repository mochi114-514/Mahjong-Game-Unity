using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class TurnOrderService
    {
        public SeatId GetNextSeat(IReadOnlyList<SeatId> activeSeats, SeatId currentSeat)
        {
            if (activeSeats == null || activeSeats.Count <= 0)
                return SeatId.East;

            int currentIndex = -1;
            for (int i = 0; i < activeSeats.Count; i++)
            {
                if (activeSeats[i] == currentSeat)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
                return activeSeats[0];

            int nextIndex = (currentIndex + 1) % activeSeats.Count;
            return activeSeats[nextIndex];
        }
    }
}
