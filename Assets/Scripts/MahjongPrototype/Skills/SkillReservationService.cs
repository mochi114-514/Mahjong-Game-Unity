using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Skills
{
    public sealed class SkillReservationService
    {
        private readonly Dictionary<SeatId, PendingSkillReservation> reservations =
            new Dictionary<SeatId, PendingSkillReservation>();

        public bool Reserve(PendingSkillReservation reservation, out string reason)
        {
            if (reservations.ContainsKey(reservation.OwnerSeat))
            {
                reason = "Skill reservation already exists.";
                return false;
            }

            reservations[reservation.OwnerSeat] = reservation;
            reason = string.Empty;
            return true;
        }

        public bool HasReservation(SeatId ownerSeat)
        {
            return reservations.ContainsKey(ownerSeat);
        }

        public bool TryConsumeForTurn(SeatId currentTurn, out PendingSkillReservation reservation)
        {
            if (!reservations.TryGetValue(currentTurn, out reservation))
                return false;

            reservations.Remove(currentTurn);
            return true;
        }

        public void Clear()
        {
            reservations.Clear();
        }
    }
}
