using System;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.Definitions
{
    [Serializable]
    public sealed class YakuDefinition
    {
        [SerializeField] private YakuKind kind;
        [SerializeField] private string displayName;
        [SerializeField] private HanValue closedHan;
        [SerializeField] private HanValue openHan;
        [SerializeField, Min(0)] private int yakumanMultiplier;
        [SerializeField] private bool isEnabled = true;

        public YakuDefinition()
        {
        }

        public YakuDefinition(
            YakuKind kind,
            string displayName,
            HanValue closedHan,
            HanValue openHan,
            int yakumanMultiplier,
            bool isEnabled)
        {
            if (yakumanMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(yakumanMultiplier));

            this.kind = kind;
            this.displayName = displayName;
            this.closedHan = closedHan;
            this.openHan = openHan;
            this.yakumanMultiplier = yakumanMultiplier;
            this.isEnabled = isEnabled;
        }

        public YakuDefinition(
            YakuKind kind,
            string displayName,
            HanValue closedHan,
            HanValue openHan,
            bool isYakuman,
            bool isEnabled)
            : this(
                kind,
                displayName,
                closedHan,
                openHan,
                isYakuman ? 1 : 0,
                isEnabled)
        {
        }

        public YakuKind Kind => kind;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
        public HanValue ClosedHan => closedHan;
        public HanValue OpenHan => openHan;
        public int YakumanMultiplier => yakumanMultiplier;
        public bool IsYakuman => YakumanMultiplier > 0;
        public bool IsEnabled => isEnabled;
    }
}
