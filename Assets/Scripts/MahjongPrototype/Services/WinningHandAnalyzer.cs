using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinningHandAnalyzer
    {
        private const int TileTypeCount = 34;
        private const int BaseHandTileCount = 13;
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
            return AnalyzeWithTile(handTiles, winningTile, Array.Empty<PlayerMeld>());
        }

        public WinningHandAnalysisResult AnalyzeWithTile(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            IReadOnlyList<PlayerMeld> melds)
        {
            if (!PlayerMeldRules.TryGetExpectedConcealedTileCount(
                    BaseHandTileCount,
                    melds,
                    out int expectedConcealedTileCount) ||
                !TryBuildTileCounts(
                    handTiles,
                    expectedConcealedTileCount,
                    melds,
                    out int[] concealedBaseCounts) ||
                !winningTile.IsValid)
                return WinningHandAnalysisResult.NotWin;

            int[] structuralBaseCounts = BuildCombinedStructuralTileCounts(
                concealedBaseCounts,
                melds);
            if (structuralBaseCounts == null)
                return WinningHandAnalysisResult.NotWin;

            List<Tile> completedTiles = new List<Tile>(handTiles.Count + 1);
            for (int i = 0; i < handTiles.Count; i++)
                completedTiles.Add(handTiles[i]);

            completedTiles.Add(winningTile);
            WinningHandAnalysisResult completedAnalysis = AnalyzeCompletedHand(
                completedTiles,
                melds);
            if (!completedAnalysis.CanWin)
                return completedAnalysis;

            List<StandardWinningInterpretation> interpretations =
                AnalyzeStandardWinningInterpretations(
                    completedAnalysis.StandardDecompositions,
                    winningTile,
                    structuralBaseCounts);

            return new WinningHandAnalysisResult(
                completedAnalysis.StandardDecompositions,
                completedAnalysis.SevenPairsAnalysis,
                completedAnalysis.ThirteenOrphansAnalysis,
                interpretations);
        }

        public WinningHandAnalysisResult AnalyzeCompletedHand(IReadOnlyList<Tile> tiles)
        {
            return AnalyzeCompletedHand(tiles, Array.Empty<PlayerMeld>());
        }

        public WinningHandAnalysisResult AnalyzeCompletedHand(
            IReadOnlyList<Tile> tiles,
            IReadOnlyList<PlayerMeld> melds)
        {
            if (!PlayerMeldRules.TryGetExpectedConcealedTileCount(
                    WinningHandTileCount,
                    melds,
                    out int expectedConcealedTileCount) ||
                !TryBuildTileCounts(
                    tiles,
                    expectedConcealedTileCount,
                    melds,
                    out int[] counts) ||
                !TryConvertPlayerMelds(melds, out List<HandMeld> fixedMelds))
                return WinningHandAnalysisResult.NotWin;

            List<StandardHandDecomposition> standardDecompositions =
                AnalyzeStandardHandDecompositions(counts, fixedMelds);
            SevenPairsAnalysis sevenPairsAnalysis = fixedMelds.Count == 0
                ? AnalyzeSevenPairs(counts)
                : SevenPairsAnalysis.NotWin;
            ThirteenOrphansAnalysis thirteenOrphansAnalysis = fixedMelds.Count == 0
                ? AnalyzeThirteenOrphans(counts)
                : ThirteenOrphansAnalysis.NotWin;

            return new WinningHandAnalysisResult(
                standardDecompositions,
                sevenPairsAnalysis,
                thirteenOrphansAnalysis);
        }

        private static bool TryBuildTileCounts(
            IReadOnlyList<Tile> tiles,
            int expectedTileCount,
            IReadOnlyList<PlayerMeld> melds,
            out int[] counts)
        {
            counts = new int[TileTypeCount];

            if (tiles == null || expectedTileCount < 0 || tiles.Count != expectedTileCount ||
                !TryValidatePlayerMelds(melds))
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

            int[] allTileCounts = (int[])counts.Clone();
            return PlayerMeldRules.TryAddPhysicalTileCounts(melds, allTileCounts, 4);
        }

        private static int[] BuildCombinedStructuralTileCounts(
            int[] concealedTileCounts,
            IReadOnlyList<PlayerMeld> melds)
        {
            int[] combinedTileCounts = (int[])concealedTileCounts.Clone();
            return PlayerMeldRules.TryAddStructuralTileCounts(melds, combinedTileCounts, 4)
                ? combinedTileCounts
                : null;
        }

        private static List<StandardWinningInterpretation> AnalyzeStandardWinningInterpretations(
            IReadOnlyList<StandardHandDecomposition> decompositions,
            Tile winningTile,
            int[] baseCounts)
        {
            List<StandardWinningInterpretation> interpretations =
                new List<StandardWinningInterpretation>();
            HashSet<string> seenKeys = new HashSet<string>();

            if (decompositions == null || !winningTile.IsValid)
                return interpretations;

            for (int i = 0; i < decompositions.Count; i++)
            {
                StandardHandDecomposition decomposition = decompositions[i];
                if (decomposition == null)
                    continue;

                AddPairWinningInterpretation(
                    decomposition,
                    winningTile,
                    baseCounts,
                    interpretations,
                    seenKeys);
                AddMeldWinningInterpretations(
                    decomposition,
                    winningTile,
                    baseCounts,
                    interpretations,
                    seenKeys);
            }

            return interpretations;
        }

        private static void AddPairWinningInterpretation(
            StandardHandDecomposition decomposition,
            Tile winningTile,
            int[] baseCounts,
            List<StandardWinningInterpretation> interpretations,
            HashSet<string> seenKeys)
        {
            if (decomposition.PairTile != winningTile ||
                !MatchesBaseCountsAfterRemovingWinningTile(decomposition, winningTile, baseCounts))
            {
                return;
            }

            AddStandardWinningInterpretation(
                decomposition,
                winningTile,
                WinningTilePlacement.Pair(),
                interpretations,
                seenKeys);
        }

        private static void AddMeldWinningInterpretations(
            StandardHandDecomposition decomposition,
            Tile winningTile,
            int[] baseCounts,
            List<StandardWinningInterpretation> interpretations,
            HashSet<string> seenKeys)
        {
            if (!MatchesBaseCountsAfterRemovingWinningTile(decomposition, winningTile, baseCounts))
                return;

            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld.IsFixed || !ContainsTile(meld, winningTile))
                    continue;

                WaitType waitType = DetermineWaitType(meld, winningTile);
                if (waitType == WaitType.None)
                    continue;

                AddStandardWinningInterpretation(
                    decomposition,
                    winningTile,
                    WinningTilePlacement.Meld(i, meld, waitType),
                    interpretations,
                    seenKeys);
            }
        }

        private static void AddStandardWinningInterpretation(
            StandardHandDecomposition decomposition,
            Tile winningTile,
            WinningTilePlacement placement,
            List<StandardWinningInterpretation> interpretations,
            HashSet<string> seenKeys)
        {
            string key = BuildWinningInterpretationKey(decomposition, winningTile, placement);
            if (!seenKeys.Add(key))
                return;

            interpretations.Add(
                new StandardWinningInterpretation(
                    decomposition,
                    winningTile,
                    placement));
        }

        private static bool MatchesBaseCountsAfterRemovingWinningTile(
            StandardHandDecomposition decomposition,
            Tile winningTile,
            int[] baseCounts)
        {
            int[] counts = BuildCountsFromDecomposition(decomposition);
            int winningTypeIndex = winningTile.TypeIndex;
            if (winningTypeIndex < 0 ||
                winningTypeIndex >= counts.Length ||
                counts[winningTypeIndex] <= 0)
            {
                return false;
            }

            counts[winningTypeIndex]--;
            return CountsEqual(counts, baseCounts);
        }

        private static int[] BuildCountsFromDecomposition(StandardHandDecomposition decomposition)
        {
            int[] counts = new int[TileTypeCount];
            counts[decomposition.PairTile.TypeIndex] += 2;

            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                for (int j = 0; j < meld.Tiles.Count; j++)
                    counts[meld.Tiles[j].TypeIndex]++;
            }

            return counts;
        }

        private static bool CountsEqual(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        private static bool ContainsTile(HandMeld meld, Tile tile)
        {
            if (meld == null)
                return false;

            for (int i = 0; i < meld.Tiles.Count; i++)
            {
                if (meld.Tiles[i] == tile)
                    return true;
            }

            return false;
        }

        private static WaitType DetermineWaitType(HandMeld meld, Tile winningTile)
        {
            if (meld.Type == MeldType.Triplet)
                return WaitType.Shanpon;

            if (meld.Type != MeldType.Sequence ||
                !winningTile.IsNumberTile ||
                meld.Tiles.Count != 3 ||
                meld.Tiles[0].Suit != winningTile.Suit)
            {
                return WaitType.None;
            }

            int startRank = meld.Tiles[0].Rank;
            int winningRank = winningTile.Rank;

            if (winningRank == startRank + 1)
                return WaitType.Kanchan;

            if ((startRank == 1 && winningRank == 3) ||
                (startRank == 7 && winningRank == 7))
            {
                return WaitType.Penchan;
            }

            return WaitType.Ryanmen;
        }

        private static List<StandardHandDecomposition> AnalyzeStandardHandDecompositions(
            int[] sourceCounts,
            IReadOnlyList<HandMeld> fixedMelds)
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
                    fixedMelds,
                    decompositions,
                    seenKeys);
            }

            return decompositions;
        }

        private static void SearchMelds(
            int[] counts,
            int pairIndex,
            List<HandMeld> currentMelds,
            IReadOnlyList<HandMeld> fixedMelds,
            List<StandardHandDecomposition> decompositions,
            HashSet<string> seenKeys)
        {
            int index = FindFirstRemainingTileIndex(counts);
            if (index < 0)
            {
                if (currentMelds.Count + fixedMelds.Count == 4)
                    AddDecomposition(
                        pairIndex,
                        currentMelds,
                        fixedMelds,
                        decompositions,
                        seenKeys);

                return;
            }

            if (currentMelds.Count + fixedMelds.Count >= 4)
                return;

            if (counts[index] >= 3)
            {
                counts[index] -= 3;
                currentMelds.Add(CreateTriplet(index));

                SearchMelds(
                    counts,
                    pairIndex,
                    currentMelds,
                    fixedMelds,
                    decompositions,
                    seenKeys);

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

                SearchMelds(
                    counts,
                    pairIndex,
                    currentMelds,
                    fixedMelds,
                    decompositions,
                    seenKeys);

                currentMelds.RemoveAt(currentMelds.Count - 1);
                counts[index]++;
                counts[index + 1]++;
                counts[index + 2]++;
            }
        }

        private static void AddDecomposition(
            int pairIndex,
            IReadOnlyList<HandMeld> melds,
            IReadOnlyList<HandMeld> fixedMelds,
            List<StandardHandDecomposition> decompositions,
            HashSet<string> seenKeys)
        {
            List<HandMeld> allMelds = new List<HandMeld>(
                fixedMelds.Count + melds.Count);
            for (int i = 0; i < fixedMelds.Count; i++)
                allMelds.Add(fixedMelds[i]);
            for (int i = 0; i < melds.Count; i++)
                allMelds.Add(melds[i]);

            string key = BuildDecompositionKey(pairIndex, allMelds);
            if (!seenKeys.Add(key))
                return;

            decompositions.Add(
                new StandardHandDecomposition(
                    TileFromTypeIndex(pairIndex),
                    allMelds));
        }

        private static bool TryValidatePlayerMelds(IReadOnlyList<PlayerMeld> melds)
        {
            if (!PlayerMeldRules.TryGetStructuralMeldCount(melds, out int meldCount) ||
                meldCount > 4)
            {
                return false;
            }

            if (melds == null)
                return true;

            for (int i = 0; i < melds.Count; i++)
            {
                PlayerMeld meld = melds[i];
                if (meld == null || meld.StructuralMeldCount != 1 ||
                    meld.StructuralTileCount != 3 ||
                    (meld.PhysicalTileCount != 3 && meld.PhysicalTileCount != 4))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryConvertPlayerMelds(
            IReadOnlyList<PlayerMeld> melds,
            out List<HandMeld> fixedMelds)
        {
            fixedMelds = new List<HandMeld>();
            if (!TryValidatePlayerMelds(melds))
                return false;
            if (melds == null)
                return true;

            for (int i = 0; i < melds.Count; i++)
            {
                PlayerMeld meld = melds[i];
                fixedMelds.Add(
                    new HandMeld(
                        meld.StructuralType,
                        meld.StructuralTiles,
                        true,
                        meld.IsOpen));
            }

            return true;
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

        private static string BuildDecompositionKey(StandardHandDecomposition decomposition)
        {
            return BuildDecompositionKey(
                decomposition.PairTile.TypeIndex,
                decomposition.Melds);
        }

        private static string BuildWinningInterpretationKey(
            StandardHandDecomposition decomposition,
            Tile winningTile,
            WinningTilePlacement placement)
        {
            string targetKey = placement.Type == WinningTilePlacementType.Pair
                ? "P" + decomposition.PairTile.TypeIndex
                : BuildMeldKey(placement.TargetMeld);

            return BuildDecompositionKey(decomposition) +
                   "|" +
                   winningTile.TypeIndex +
                   "|" +
                   placement.Type +
                   "|" +
                   targetKey +
                   "|" +
                   placement.WaitType;
        }

        private static string BuildMeldKey(HandMeld meld)
        {
            string prefix = meld.Type == MeldType.Sequence ? "S" : "T";
            if (meld.IsFixed)
                prefix += meld.IsOpen ? "O" : "C";
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
