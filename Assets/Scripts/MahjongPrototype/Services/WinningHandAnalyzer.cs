using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinningHandAnalyzer
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

        public WinningHandAnalysisResult AnalyzeWithTile(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile)
        {
            if (handTiles == null || !winningTile.IsValid)
                return WinningHandAnalysisResult.NotWin;

            List<Tile> completedTiles = new List<Tile>(handTiles.Count + 1);
            for (int i = 0; i < handTiles.Count; i++)
                completedTiles.Add(handTiles[i]);

            completedTiles.Add(winningTile);
            return AnalyzeCompletedHand(completedTiles);
        }

        public WinningHandAnalysisResult AnalyzeCompletedHand(IReadOnlyList<Tile> tiles)
        {
            if (!TryBuildTileCounts(tiles, out int[] counts))
                return WinningHandAnalysisResult.NotWin;

            List<StandardHandDecomposition> standardDecompositions =
                AnalyzeStandardHandDecompositions(counts);
            SevenPairsAnalysis sevenPairsAnalysis = AnalyzeSevenPairs(counts);
            ThirteenOrphansAnalysis thirteenOrphansAnalysis = AnalyzeThirteenOrphans(counts);

            return new WinningHandAnalysisResult(
                standardDecompositions,
                sevenPairsAnalysis,
                thirteenOrphansAnalysis);
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

        private static List<StandardHandDecomposition> AnalyzeStandardHandDecompositions(
            int[] sourceCounts)
        {
            List<StandardHandDecomposition> decompositions =
                new List<StandardHandDecomposition>();
            HashSet<string> seenKeys = new HashSet<string>();

            for (int pairIndex = 0; pairIndex < TileTypeCount; pairIndex++)
            {
                if (sourceCounts[pairIndex] < 2)
                    continue;

                int[] counts = (int[])sourceCounts.Clone();
                counts[pairIndex] -= 2;

                SearchMelds(
                    counts,
                    pairIndex,
                    new List<HandMeld>(),
                    decompositions,
                    seenKeys);
            }

            return decompositions;
        }

        private static void SearchMelds(
            int[] counts,
            int pairIndex,
            List<HandMeld> currentMelds,
            List<StandardHandDecomposition> decompositions,
            HashSet<string> seenKeys)
        {
            int index = FindFirstRemainingTileIndex(counts);
            if (index < 0)
            {
                if (currentMelds.Count == 4)
                    AddDecomposition(pairIndex, currentMelds, decompositions, seenKeys);

                return;
            }

            if (currentMelds.Count >= 4)
                return;

            if (counts[index] >= 3)
            {
                counts[index] -= 3;
                currentMelds.Add(CreateTriplet(index));

                SearchMelds(counts, pairIndex, currentMelds, decompositions, seenKeys);

                currentMelds.RemoveAt(currentMelds.Count - 1);
                counts[index] += 3;
            }

            if (CanStartSequence(index) &&
                counts[index + 1] > 0 &&
                counts[index + 2] > 0)
            {
                counts[index]--;
                counts[index + 1]--;
                counts[index + 2]--;
                currentMelds.Add(CreateSequence(index));

                SearchMelds(counts, pairIndex, currentMelds, decompositions, seenKeys);

                currentMelds.RemoveAt(currentMelds.Count - 1);
                counts[index]++;
                counts[index + 1]++;
                counts[index + 2]++;
            }
        }

        private static void AddDecomposition(
            int pairIndex,
            IReadOnlyList<HandMeld> melds,
            List<StandardHandDecomposition> decompositions,
            HashSet<string> seenKeys)
        {
            string key = BuildDecompositionKey(pairIndex, melds);
            if (!seenKeys.Add(key))
                return;

            decompositions.Add(
                new StandardHandDecomposition(
                    TileFromTypeIndex(pairIndex),
                    melds));
        }

        private static SevenPairsAnalysis AnalyzeSevenPairs(int[] counts)
        {
            List<Tile> pairTiles = new List<Tile>();

            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] == 0)
                    continue;

                if (counts[i] != 2)
                    return SevenPairsAnalysis.NotWin;

                pairTiles.Add(TileFromTypeIndex(i));
            }

            return pairTiles.Count == 7
                ? SevenPairsAnalysis.Win(pairTiles)
                : SevenPairsAnalysis.NotWin;
        }

        private static ThirteenOrphansAnalysis AnalyzeThirteenOrphans(int[] counts)
        {
            int pairCount = 0;
            Tile pairTile = default(Tile);
            List<Tile> requiredTiles = new List<Tile>(ThirteenOrphansTypeIndices.Length);

            for (int i = 0; i < counts.Length; i++)
            {
                bool isRequired = IsThirteenOrphansTypeIndex(i);
                int count = counts[i];

                if (!isRequired)
                {
                    if (count != 0)
                        return ThirteenOrphansAnalysis.NotWin;

                    continue;
                }

                if (count <= 0)
                    return ThirteenOrphansAnalysis.NotWin;

                if (count == 2)
                {
                    pairCount++;
                    pairTile = TileFromTypeIndex(i);
                }
                else if (count != 1)
                {
                    return ThirteenOrphansAnalysis.NotWin;
                }

                requiredTiles.Add(TileFromTypeIndex(i));
            }

            return pairCount == 1 && requiredTiles.Count == ThirteenOrphansTypeIndices.Length
                ? ThirteenOrphansAnalysis.Win(requiredTiles, pairTile)
                : ThirteenOrphansAnalysis.NotWin;
        }

        private static HandMeld CreateTriplet(int typeIndex)
        {
            Tile tile = TileFromTypeIndex(typeIndex);
            return new HandMeld(
                MeldType.Triplet,
                new[] { tile, tile, tile });
        }

        private static HandMeld CreateSequence(int startTypeIndex)
        {
            return new HandMeld(
                MeldType.Sequence,
                new[]
                {
                    TileFromTypeIndex(startTypeIndex),
                    TileFromTypeIndex(startTypeIndex + 1),
                    TileFromTypeIndex(startTypeIndex + 2)
                });
        }

        private static string BuildDecompositionKey(
            int pairIndex,
            IReadOnlyList<HandMeld> melds)
        {
            List<string> meldKeys = new List<string>(melds.Count);
            for (int i = 0; i < melds.Count; i++)
                meldKeys.Add(BuildMeldKey(melds[i]));

            meldKeys.Sort(System.StringComparer.Ordinal);
            return pairIndex + "|" + string.Join("|", meldKeys.ToArray());
        }

        private static string BuildMeldKey(HandMeld meld)
        {
            string prefix = meld.Type == MeldType.Sequence ? "S" : "T";
            return prefix + meld.Tiles[0].TypeIndex;
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

        private static bool IsThirteenOrphansTypeIndex(int typeIndex)
        {
            for (int i = 0; i < ThirteenOrphansTypeIndices.Length; i++)
            {
                if (ThirteenOrphansTypeIndices[i] == typeIndex)
                    return true;
            }

            return false;
        }

        private static Tile TileFromTypeIndex(int typeIndex)
        {
            if (typeIndex >= 0 && typeIndex < FirstHonorTileIndex)
            {
                int suitIndex = typeIndex / RanksPerSuit;
                int rank = typeIndex % RanksPerSuit + 1;
                switch (suitIndex)
                {
                    case 0:
                        return Tile.CreateNumber(TileSuit.Man, rank);
                    case 1:
                        return Tile.CreateNumber(TileSuit.Pin, rank);
                    default:
                        return Tile.CreateNumber(TileSuit.Sou, rank);
                }
            }

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
