using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Skills
{
    public readonly struct PendingSkillReservation
    {
        public PendingSkillReservation(
            SeatId ownerSeat,
            SkillEffectKind skillEffectKind,
            Tile targetTile,
            SeatId reservedOnTurnSeat,
            int reservedTurnIndex)
        {
            if (!targetTile.IsValid)
                throw new ArgumentException("Target tile must be valid.", nameof(targetTile));

            OwnerSeat = ownerSeat;
            SkillEffectKind = skillEffectKind;
            TargetTile = targetTile;
            ReservedOnTurnSeat = reservedOnTurnSeat;
            ReservedTurnIndex = reservedTurnIndex;
        }

        public SeatId OwnerSeat { get; }
        public SkillEffectKind SkillEffectKind { get; }
        public Tile TargetTile { get; }
        public SeatId ReservedOnTurnSeat { get; }
        public int ReservedTurnIndex { get; }

        public string ToLogText()
        {
            return $"{SkillEffectKind}:{TargetTile}:Reserved";
        }
    }
}
