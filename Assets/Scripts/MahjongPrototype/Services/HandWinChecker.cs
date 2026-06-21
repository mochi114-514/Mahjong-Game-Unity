using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class HandWinChecker
    {
        private const int TileTypeCount = 34;
        private const int WinningHandTileCount = 14;
        private const int FirstHonorTileIndex = 27;
        private const int RanksPerSuit = 9;

        public bool CanWinStandardHand(IReadOnlyList<Tile> tiles)
        {
            if (tiles == null || tiles.Count != WinningHandTileCount)
                return false;

            int[] counts = new int[TileTypeCount];
            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                int typeIndex = tile.TypeIndex;
                if (!tile.IsValid || typeIndex < 0 || typeIndex >= TileTypeCount)
                    return false;

                counts[typeIndex]++;
                if (counts[typeIndex] > 4)
                    return false;
            }

            for (int pairIndex = 0; pairIndex < TileTypeCount; pairIndex++)
            {
                if (counts[pairIndex] < 2)
                    continue;

                counts[pairIndex] -= 2;
                if (CanRemoveAllMelds(counts))
                    return true;

                counts[pairIndex] += 2;
            }

            return false;
        }

        private static bool CanRemoveAllMelds(int[] counts)
        {
            int index = FindFirstRemainingTileIndex(counts);
            if (index < 0)
                return true;

            if (counts[index] >= 3)
            {
                counts[index] -= 3;
                if (CanRemoveAllMelds(counts))
                    return true;

                counts[index] += 3;
            }

            if (CanStartSequence(index) &&
                counts[index + 1] > 0 &&
                counts[index + 2] > 0)
            {
                counts[index]--;
                counts[index + 1]--;
                counts[index + 2]--;

                if (CanRemoveAllMelds(counts))
                    return true;

                counts[index]++;
                counts[index + 1]++;
                counts[index + 2]++;
            }

            return false;
        }

        private static int FindFirstRemainingTileIndex(int[] counts)
        {
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > 0)
                    return i;
            }

            return -1;
        }

        private static bool CanStartSequence(int typeIndex)
        {
            return typeIndex >= 0 &&
                   typeIndex < FirstHonorTileIndex &&
                   typeIndex % RanksPerSuit <= RanksPerSuit - 3;
        }
    }
}
