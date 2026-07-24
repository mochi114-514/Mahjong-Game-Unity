using System;

namespace MahjongPrototype.Domain
{
    public readonly struct EvaluatedYaku
    {
        public EvaluatedYaku(
            YakuKind kind,
            string displayName,
            HanValue han,
            int yakumanMultiplier)
        {
            if (yakumanMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(yakumanMultiplier));

            Kind = kind;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
            Han = yakumanMultiplier > 0 ? HanValue.None : han;
            YakumanMultiplier = yakumanMultiplier;
        }

        public EvaluatedYaku(
            YakuKind kind,
            string displayName,
            HanValue han,
            bool isYakuman)
            : this(
                kind,
                displayName,
                han,
                isYakuman ? 1 : 0)
        {
        }

        public YakuKind Kind { get; }
        public string DisplayName { get; }
        public HanValue Han { get; }
        public int YakumanMultiplier { get; }
        public bool IsYakuman => YakumanMultiplier > 0;
    }
}
