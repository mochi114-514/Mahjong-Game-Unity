using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    /// <summary>
    /// Enumerates every tile type that completes a valid winning shape for a
    /// thirteen-tile-equivalent concealed hand and its declared melds.
    /// </summary>
    public sealed class WinningTileWaitEnumerator
    {
        private const int BaseHandTileCount = 13;
        private const int TileTypeCount = 34;
        private const int FirstPinTypeIndex = 9;
        private const int FirstSouTypeIndex = 18;
        private const int FirstHonorTypeIndex = 27;

        private static readonly IReadOnlyList<Tile> EmptyTiles =
            new List<Tile>().AsReadOnly();

        private readonly WinChecker winChecker;

        public WinningTileWaitEnumerator()
            : this(new WinChecker())
        {
        }

        public WinningTileWaitEnumerator(WinChecker winChecker)
        {
            this.winChecker = winChecker ?? new WinChecker();
        }

        public IReadOnlyList<Tile> EnumerateWinningTiles(
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<PlayerMeld> melds = null)
        {
            return TryEnumerateWinningTiles(handTiles, melds, out IReadOnlyList<Tile> waits)
                ? waits
                : EmptyTiles;
        }

        public bool TryEnumerateWinningTiles(
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<PlayerMeld> melds,
            out IReadOnlyList<Tile> waits)
        {
            waits = EmptyTiles;
            if (!TryBuildTypeCounts(handTiles, melds, out int[] typeCounts))
                return false;

            List<Tile> winningTiles = new List<Tile>();
            for (int typeIndex = 0; typeIndex < TileTypeCount; typeIndex++)
            {
                if (typeCounts[typeIndex] >= 4)
                    continue;

                Tile winningTile = CreateTileFromTypeIndex(typeIndex);
                if (winChecker.CanWinWithTile(handTiles, winningTile, melds))
                    winningTiles.Add(winningTile);
            }

            waits = winningTiles.Count > 0
                ? winningTiles.AsReadOnly()
                : EmptyTiles;
            return true;
        }

        private static bool TryBuildTypeCounts(
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<PlayerMeld> melds,
            out int[] typeCounts)
        {
            typeCounts = new int[TileTypeCount];

            if (!PlayerMeldRules.TryGetExpectedConcealedTileCount(
                    BaseHandTileCount,
                    melds,
                    out int expectedConcealedTileCount) ||
                handTiles == null || handTiles.Count != expectedConcealedTileCount)
            {
                return false;
            }

            for (int i = 0; i < handTiles.Count; i++)
            {
                Tile tile = handTiles[i];
                int typeIndex = tile.TypeIndex;
                if (!tile.IsValid || typeIndex < 0 || typeIndex >= TileTypeCount)
                    return false;

                typeCounts[typeIndex]++;
                if (typeCounts[typeIndex] > 4)
                    return false;
            }

            return PlayerMeldRules.TryAddPhysicalTileCounts(melds, typeCounts, 4);
        }

        private static Tile CreateTileFromTypeIndex(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= TileTypeCount)
                return default;

            if (typeIndex < FirstPinTypeIndex)
                return Tile.CreateNumber(TileSuit.Man, typeIndex + 1);

            if (typeIndex < FirstSouTypeIndex)
                return Tile.CreateNumber(TileSuit.Pin, typeIndex - FirstPinTypeIndex + 1);

            if (typeIndex < FirstHonorTypeIndex)
                return Tile.CreateNumber(TileSuit.Sou, typeIndex - FirstSouTypeIndex + 1);

            switch (typeIndex)
            {
                case 27:
                    return Tile.CreateHonor(HonorKind.East);
                case 28:
                    return Tile.CreateHonor(HonorKind.South);
                case 29:
                    return Tile.CreateHonor(HonorKind.West);
                case 30:
                    return Tile.CreateHonor(HonorKind.North);
                case 31:
                    return Tile.CreateHonor(HonorKind.White);
                case 32:
                    return Tile.CreateHonor(HonorKind.Green);
                case 33:
                    return Tile.CreateHonor(HonorKind.Red);
                default:
                    return default;
            }
        }
    }
}
