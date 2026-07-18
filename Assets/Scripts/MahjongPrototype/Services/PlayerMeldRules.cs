using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    internal static class PlayerMeldRules
    {
        public static bool TryGetStructuralMeldCount(
            IReadOnlyList<PlayerMeld> melds,
            out int meldCount)
        {
            meldCount = 0;
            if (melds == null)
                return true;

            for (int i = 0; i < melds.Count; i++)
            {
                PlayerMeld meld = melds[i];
                if (meld == null)
                    return false;

                meldCount += meld.StructuralMeldCount;
            }

            return true;
        }

        public static bool TryGetExpectedConcealedTileCount(
            int fullHandTileCount,
            IReadOnlyList<PlayerMeld> melds,
            out int expectedTileCount)
        {
            expectedTileCount = fullHandTileCount;
            if (melds == null)
                return expectedTileCount >= 0;

            for (int i = 0; i < melds.Count; i++)
            {
                PlayerMeld meld = melds[i];
                if (meld == null)
                    return false;

                expectedTileCount -= meld.StructuralTileCount;
            }

            return expectedTileCount >= 0;
        }

        public static bool TryAddPhysicalTileCounts(
            IReadOnlyList<PlayerMeld> melds,
            int[] typeCounts,
            int maximumPerType)
        {
            return TryAddTileCounts(melds, typeCounts, maximumPerType, false);
        }

        public static bool TryAddStructuralTileCounts(
            IReadOnlyList<PlayerMeld> melds,
            int[] typeCounts,
            int maximumPerType)
        {
            return TryAddTileCounts(melds, typeCounts, maximumPerType, true);
        }

        private static bool TryAddTileCounts(
            IReadOnlyList<PlayerMeld> melds,
            int[] typeCounts,
            int maximumPerType,
            bool useStructuralTiles)
        {
            if (typeCounts == null || maximumPerType <= 0)
                return false;
            if (melds == null)
                return true;

            for (int i = 0; i < melds.Count; i++)
            {
                PlayerMeld meld = melds[i];
                if (meld == null)
                    return false;

                IReadOnlyList<Tile> tiles = useStructuralTiles
                    ? meld.StructuralTiles
                    : meld.PhysicalTiles;
                for (int j = 0; j < tiles.Count; j++)
                {
                    Tile tile = tiles[j];
                    int typeIndex = tile.TypeIndex;
                    if (!tile.IsValid || typeIndex < 0 || typeIndex >= typeCounts.Length)
                        return false;

                    typeCounts[typeIndex]++;
                    if (typeCounts[typeIndex] > maximumPerType)
                        return false;
                }
            }

            return true;
        }
    }
}
