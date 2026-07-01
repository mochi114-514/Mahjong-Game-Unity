using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinChecker
    {
        private const int TileTypeCount = 34;
        private const int WinningHandTileCount = 14;
        private const int FirstHonorTileIndex = 27;
        private const int RanksPerSuit = 9;
        private static readonly int[] ThirteenOrphansTypeIndices =
        {
            0, 8,
            9, 17,
            18, 26,
            27, 28, 29, 30, 31, 32, 33
        };

        public bool CanWinWithTile(IReadOnlyList<Tile> handTiles, Tile winningTile)
        {
            return CheckWinWithTile(handTiles, winningTile).CanWin;
        }

        public WinCheckResult CheckWinWithTile(IReadOnlyList<Tile> handTiles, Tile winningTile)
        {
            if (handTiles == null || !winningTile.IsValid)
                return WinCheckResult.NotWin;

            List<Tile> completedTiles = new List<Tile>(handTiles.Count + 1);
            for (int i = 0; i < handTiles.Count; i++)
                completedTiles.Add(handTiles[i]);

            completedTiles.Add(winningTile);
            return CheckCompletedHand(completedTiles);
        }

        public WinCheckResult CheckCompletedHand(IReadOnlyList<Tile> tiles)
        {
            if (!TryBuildTileCounts(tiles, out int[] counts))
                return WinCheckResult.NotWin;

            int[] standardCounts = (int[])counts.Clone();
            if (CanWinStandardHandFromCounts(standardCounts))
                return WinCheckResult.Win(WinningHandShape.Standard);

            if (CanWinSevenPairsFromCounts(counts))
                return WinCheckResult.Win(WinningHandShape.SevenPairs);

            if (CanWinThirteenOrphansFromCounts(counts))
                return WinCheckResult.Win(WinningHandShape.ThirteenOrphans);

            return WinCheckResult.NotWin;
        }

        public bool CanWinStandardHand(IReadOnlyList<Tile> tiles)
        {
            if (!TryBuildTileCounts(tiles, out int[] counts))
                return false;

            return CanWinStandardHandFromCounts(counts);
        }

        private static bool TryBuildTileCounts(IReadOnlyList<Tile> tiles, out int[] counts)
        {
            counts = new int[TileTypeCount];

            if (tiles == null || tiles.Count != WinningHandTileCount)
                return false;

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

            return true;
        }

        private static bool CanWinStandardHandFromCounts(int[] counts)
        {
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

        private static bool CanWinSevenPairsFromCounts(int[] counts)
        {
            int pairCount = 0;

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0)
                    continue;

                if (counts[i] != 2)
                    return false;

                pairCount++;
            }

            return pairCount == 7;
        }

        private static bool CanWinThirteenOrphansFromCounts(int[] counts)
        {
            int pairCount = 0;

            for (int i = 0; i < counts.Length; i++)
            {
                bool isRequired = IsThirteenOrphansTypeIndex(i);
                int count = counts[i];

                if (!isRequired)
                {
                    if (count != 0)
                        return false;

                    continue;
                }

                if (count <= 0)
                    return false;

                if (count == 2)
                {
                    pairCount++;
                    continue;
                }

                if (count == 1)
                    continue;

                return false;
            }

            return pairCount == 1;
        }

        private static bool IsThirteenOrphansTypeIndex(int typeIndex)
        {
            for (int i = 0; i < ThirteenOrphansTypeIndices.Length; i++)
            {
                if (ThirteenOrphansTypeIndices[i] == typeIndex)
                    return true;
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
