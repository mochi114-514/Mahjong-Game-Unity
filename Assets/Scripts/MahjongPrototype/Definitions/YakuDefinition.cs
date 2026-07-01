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
        [SerializeField] private bool isYakuman;
        [SerializeField] private bool isEnabled = true;

        public YakuDefinition()
        {
        }

        public YakuDefinition(
            YakuKind kind,
            string displayName,
            HanValue closedHan,
            HanValue openHan,
            bool isYakuman,
            bool isEnabled)
        {
            this.kind = kind;
            this.displayName = displayName;
            this.closedHan = closedHan;
            this.openHan = openHan;
            this.isYakuman = isYakuman;
            this.isEnabled = isEnabled;
        }

        public YakuKind Kind => kind;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
        public HanValue ClosedHan => closedHan;
        public HanValue OpenHan => openHan;
        public bool IsYakuman => isYakuman;
        public bool IsEnabled => isEnabled;
    }
}
