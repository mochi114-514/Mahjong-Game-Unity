using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Skills
{
    public sealed class ActiveSkillEffect
    {
        public ActiveSkillEffect(
            SkillEffectKind kind,
            SeatId ownerSeat,
            Tile targetTile,
            SkillEffectDuration duration)
        {
            Kind = kind;
            OwnerSeat = ownerSeat;
            TargetTile = targetTile;
            Duration = duration;
            EffectId = Guid.NewGuid().ToString("N");
        }

        public string EffectId { get; }
        public SkillEffectKind Kind { get; }
        public SeatId OwnerSeat { get; }
        public Tile TargetTile { get; }
        public SkillEffectDuration Duration { get; }

        public string ToLogText()
        {
            return $"{Kind}:{TargetTile}:{Duration}";
        }
    }
}
