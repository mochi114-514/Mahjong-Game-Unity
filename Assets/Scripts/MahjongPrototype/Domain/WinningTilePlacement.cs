using System;

namespace MahjongPrototype.Domain
{
    public sealed class WinningTilePlacement
    {
        public const int NoTargetMeldIndex = -1;

        private WinningTilePlacement(
            WinningTilePlacementType type,
            int targetMeldIndex,
            WaitType waitType,
            HandMeld targetMeld)
        {
            if (!IsValidState(type, targetMeldIndex, waitType, targetMeld))
                throw new ArgumentException("Invalid winning tile placement.");

            Type = type;
            TargetMeldIndex = targetMeldIndex;
            WaitType = waitType;
            TargetMeld = targetMeld;
        }

        public WinningTilePlacementType Type { get; }
        public int TargetMeldIndex { get; }
        public WaitType WaitType { get; }
        public HandMeld TargetMeld { get; }

        public static WinningTilePlacement Pair()
        {
            return new WinningTilePlacement(
                WinningTilePlacementType.Pair,
                NoTargetMeldIndex,
                WaitType.Tanki,
                null);
        }

        public static WinningTilePlacement Meld(
            int targetMeldIndex,
            HandMeld targetMeld,
            WaitType waitType)
        {
            return new WinningTilePlacement(
                WinningTilePlacementType.Meld,
                targetMeldIndex,
                waitType,
                targetMeld);
        }

        private static bool IsValidState(
            WinningTilePlacementType type,
            int targetMeldIndex,
            WaitType waitType,
            HandMeld targetMeld)
        {
            switch (type)
            {
                case WinningTilePlacementType.Pair:
                    return targetMeldIndex == NoTargetMeldIndex &&
                           waitType == WaitType.Tanki &&
                           targetMeld == null;
                case WinningTilePlacementType.Meld:
                    return targetMeldIndex >= 0 &&
                           targetMeldIndex <= 3 &&
                           IsValidMeldWait(targetMeld, waitType);
                default:
                    return false;
            }
        }

        private static bool IsValidMeldWait(HandMeld targetMeld, WaitType waitType)
        {
            if (targetMeld == null)
                return false;

            switch (targetMeld.Type)
            {
                case MeldType.Triplet:
                    return waitType == WaitType.Shanpon;
                case MeldType.Sequence:
                    return waitType == WaitType.Ryanmen ||
                           waitType == WaitType.Kanchan ||
                           waitType == WaitType.Penchan;
                default:
                    return false;
            }
        }
    }
}
