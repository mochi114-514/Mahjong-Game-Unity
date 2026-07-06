using System.Collections.Generic;
using MahjongPrototype.Definitions;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class HandEvaluator
    {
        private readonly YakuDefinitionCatalog catalog;
        private const int CompleteDragonMask = 7;
        private const int CompleteIttsuuSequenceMask = 7;

        public HandEvaluator(YakuDefinitionCatalog catalog)
        {
            this.catalog = catalog;
        }

        public HandEvaluationResult Evaluate(HandEvaluationContext context)
        {
            if (context == null || catalog == null)
                return HandEvaluationResult.Empty;

            List<HandEvaluationCandidateResult> candidateResults =
                EvaluateCandidateResults(context);

            if (candidateResults.Count <= 0)
                return HandEvaluationResult.Empty;

            return new HandEvaluationResult(
                new List<EvaluatedYaku>(),
                candidateResults);
        }

        private List<HandEvaluationCandidateResult> EvaluateCandidateResults(
            HandEvaluationContext context)
        {
            List<HandEvaluationCandidateResult> results =
                new List<HandEvaluationCandidateResult>();
            List<HandEvaluationCandidate> candidates = BuildCandidates(context.WinningHandAnalysis);

            for (int i = 0; i < candidates.Count; i++)
                results.Add(EvaluateCandidate(context, candidates[i]));

            return results;
        }

        private static List<HandEvaluationCandidate> BuildCandidates(
            WinningHandAnalysisResult analysis)
        {
            List<HandEvaluationCandidate> candidates = new List<HandEvaluationCandidate>();
            if (analysis == null || !analysis.CanWin)
                return candidates;

            for (int i = 0; i < analysis.StandardWinningInterpretations.Count; i++)
                candidates.Add(HandEvaluationCandidate.Standard(analysis.StandardWinningInterpretations[i]));

            if (analysis.SevenPairsAnalysis.IsWin)
                candidates.Add(HandEvaluationCandidate.SevenPairs(analysis.SevenPairsAnalysis));

            if (analysis.ThirteenOrphansAnalysis.IsWin)
                candidates.Add(HandEvaluationCandidate.ThirteenOrphans(analysis.ThirteenOrphansAnalysis));

            return candidates;
        }

        private HandEvaluationCandidateResult EvaluateCandidate(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate)
        {
            List<EvaluatedYaku> yakus = new List<EvaluatedYaku>();
            EvaluateCommonYaku(context, candidate, yakus);
            EvaluateTileCompositionYaku(context, candidate, yakus);

            switch (candidate.Type)
            {
                case HandEvaluationCandidateType.Standard:
                    EvaluateStandardCandidateYaku(context, candidate, yakus);
                    break;
                case HandEvaluationCandidateType.SevenPairs:
                    EvaluateSevenPairsCandidateYaku(context, candidate, yakus);
                    break;
                case HandEvaluationCandidateType.ThirteenOrphans:
                    EvaluateThirteenOrphansCandidateYaku(context, candidate, yakus);
                    break;
            }

            List<EvaluatedYaku> finalizedYakus = KeepOnlyYakumanWhenPresent(yakus);

            return new HandEvaluationCandidateResult(candidate, finalizedYakus);
        }

        private void EvaluateCommonYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            EvaluateReachYaku(context, yakus);
            TryAddYaku(
                yakus,
                YakuKind.Ippatsu,
                context.IsReachDeclared &&
                context.IsIppatsuEligible &&
                context.IsClosed,
                context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.MenzenTsumo,
                context.WinType == WinType.Tsumo && context.IsClosed,
                context.IsClosed);
            TryAddYaku(yakus, YakuKind.Tanyao, IsTanyao(context), context.IsClosed);
        }

        private void EvaluateReachYaku(
            HandEvaluationContext context,
            List<EvaluatedYaku> yakus)
        {
            if (context == null || !context.IsReachDeclared || !context.IsClosed)
                return;

            if (context.IsDoubleReachDeclared &&
                TryAddYaku(yakus, YakuKind.DoubleReach, true, context.IsClosed))
            {
                return;
            }

            TryAddYaku(yakus, YakuKind.Reach, true, context.IsClosed);
        }

        private void EvaluateStandardCandidateYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            TryAddYaku(yakus, YakuKind.Pinfu, IsPinfu(context, candidate), context.IsClosed);
            EvaluatePeikouYaku(context, candidate, yakus);
            EvaluateYakuhaiYaku(context, candidate, yakus);
            EvaluateDragonGroupYaku(context, candidate, yakus);
            EvaluateSanshokuDoujunYaku(context, candidate, yakus);
            EvaluateSanshokuDoukouYaku(context, candidate, yakus);
            EvaluateIttsuuYaku(context, candidate, yakus);
            EvaluateChantaYaku(context, candidate, yakus);
        }

        private void EvaluateTileCompositionYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                (candidate.Type != HandEvaluationCandidateType.Standard &&
                 candidate.Type != HandEvaluationCandidateType.SevenPairs))
            {
                return;
            }

            if (!TryAnalyzeTileComposition(
                    context,
                    out int numberSuitMask,
                    out bool hasHonor,
                    out bool allTilesAreGreen))
            {
                return;
            }

            EvaluateFlushYaku(
                context,
                numberSuitMask,
                hasHonor,
                yakus);

            EvaluateRyuuiisouYaku(
                context,
                allTilesAreGreen,
                yakus);
        }

        private void EvaluateSevenPairsCandidateYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            TryAddYaku(
                yakus,
                YakuKind.SevenPairs,
                candidate.Type == HandEvaluationCandidateType.SevenPairs &&
                candidate.SevenPairsAnalysis != null &&
                candidate.SevenPairsAnalysis.IsWin,
                context.IsClosed);
        }

        private void EvaluateThirteenOrphansCandidateYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            TryAddYaku(
                yakus,
                YakuKind.KokushiMusou,
                candidate.Type == HandEvaluationCandidateType.ThirteenOrphans &&
                candidate.ThirteenOrphansAnalysis != null &&
                candidate.ThirteenOrphansAnalysis.IsWin,
                context.IsClosed);
        }

        private bool TryAddYaku(
            List<EvaluatedYaku> yakus,
            YakuKind kind,
            bool condition,
            bool isClosed)
        {
            if (!condition || !catalog.TryGet(kind, out YakuDefinition definition))
                return false;

            HanValue han = ResolveHan(definition, isClosed);
            if (!definition.IsYakuman && han == HanValue.None)
                return false;

            yakus.Add(new EvaluatedYaku(
                definition.Kind,
                definition.DisplayName,
                definition.IsYakuman ? HanValue.None : han,
                definition.IsYakuman));
            return true;
        }

        private static HanValue ResolveHan(YakuDefinition definition, bool isClosed)
        {
            return isClosed ? definition.ClosedHan : definition.OpenHan;
        }

        private static List<EvaluatedYaku> KeepOnlyYakumanWhenPresent(
            List<EvaluatedYaku> yakus)
        {
            if (yakus == null || yakus.Count == 0)
                return yakus ?? new List<EvaluatedYaku>();

            bool hasYakuman = false;
            for (int i = 0; i < yakus.Count; i++)
            {
                if (yakus[i].IsYakuman)
                {
                    hasYakuman = true;
                    break;
                }
            }

            if (!hasYakuman)
                return yakus;

            List<EvaluatedYaku> yakumanOnly = new List<EvaluatedYaku>();
            for (int i = 0; i < yakus.Count; i++)
            {
                EvaluatedYaku yaku = yakus[i];
                if (yaku.IsYakuman)
                    yakumanOnly.Add(yaku);
            }

            return yakumanOnly;
        }

        private static bool IsTanyao(HandEvaluationContext context)
        {
            if (context.HandTiles == null || !IsSimpleNumberTile(context.WinningTile))
                return false;

            for (int i = 0; i < context.HandTiles.Count; i++)
            {
                if (!IsSimpleNumberTile(context.HandTiles[i]))
                    return false;
            }

            return true;
        }

        private static bool IsSimpleNumberTile(Tile tile)
        {
            return tile.IsNumberTile && tile.Rank >= 2 && tile.Rank <= 8;
        }

        private static bool TryAnalyzeTileComposition(
            HandEvaluationContext context,
            out int numberSuitMask,
            out bool hasHonor,
            out bool allTilesAreGreen)
        {
            numberSuitMask = 0;
            hasHonor = false;
            allTilesAreGreen = true;

            if (context == null ||
                context.HandTiles == null ||
                !context.WinningTile.IsValid)
            {
                return false;
            }

            for (int i = 0; i < context.HandTiles.Count; i++)
            {
                if (!TryAnalyzeTileCompositionTile(
                        context.HandTiles[i],
                        ref numberSuitMask,
                        ref hasHonor,
                        ref allTilesAreGreen))
                {
                    return false;
                }
            }

            return TryAnalyzeTileCompositionTile(
                context.WinningTile,
                ref numberSuitMask,
                ref hasHonor,
                ref allTilesAreGreen);
        }

        private static bool TryAnalyzeTileCompositionTile(
            Tile tile,
            ref int numberSuitMask,
            ref bool hasHonor,
            ref bool allTilesAreGreen)
        {
            if (!tile.IsValid)
                return false;

            if (tile.IsNumberTile)
                numberSuitMask |= ToSuitMask(tile.Suit);

            if (tile.IsHonorTile)
                hasHonor = true;

            if (!IsGreenTile(tile))
                allTilesAreGreen = false;

            return true;
        }

        private void EvaluateFlushYaku(
            HandEvaluationContext context,
            int numberSuitMask,
            bool hasHonor,
            List<EvaluatedYaku> yakus)
        {
            if (!HasExactlyOneNumberSuit(numberSuitMask))
                return;

            if (hasHonor)
            {
                TryAddYaku(
                    yakus,
                    YakuKind.Honitsu,
                    true,
                    context.IsClosed);
                return;
            }

            TryAddYaku(
                yakus,
                YakuKind.Chinitsu,
                true,
                context.IsClosed);
        }

        private void EvaluateRyuuiisouYaku(
            HandEvaluationContext context,
            bool allTilesAreGreen,
            List<EvaluatedYaku> yakus)
        {
            TryAddYaku(
                yakus,
                YakuKind.Ryuuiisou,
                allTilesAreGreen,
                context.IsClosed);
        }

        private static bool IsPinfu(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate)
        {
            if (context == null || candidate == null)
                return false;

            if (!context.IsClosed)
                return false;

            if (candidate.Type != HandEvaluationCandidateType.Standard)
                return false;

            StandardWinningInterpretation interpretation =
                candidate.StandardInterpretation;
            if (interpretation == null || interpretation.WaitType != WaitType.Ryanmen)
                return false;

            StandardHandDecomposition decomposition = interpretation.Decomposition;
            if (decomposition == null || !AreAllMeldsSequences(decomposition))
                return false;

            return !IsValuePair(
                decomposition.PairTile,
                context.SeatWind,
                context.RoundWind);
        }

        private static bool AreAllMeldsSequences(
            StandardHandDecomposition decomposition)
        {
            if (decomposition == null ||
                decomposition.Melds == null ||
                decomposition.Melds.Count != 4)
            {
                return false;
            }

            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null || meld.Type != MeldType.Sequence)
                    return false;
            }

            return true;
        }

        private void EvaluatePeikouYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                !context.IsClosed ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (decomposition == null ||
                decomposition.Melds == null ||
                decomposition.Melds.Count != 4)
            {
                return;
            }

            int identicalSequencePairCount =
                CountIdenticalSequencePairs(decomposition);

            if (identicalSequencePairCount >= 2 &&
                TryAddYaku(
                    yakus,
                    YakuKind.Ryanpeikou,
                    true,
                    context.IsClosed))
            {
                return;
            }

            TryAddYaku(
                yakus,
                YakuKind.Iipeikou,
                identicalSequencePairCount >= 1,
                context.IsClosed);
        }

        private static int CountIdenticalSequencePairs(
            StandardHandDecomposition decomposition)
        {
            Dictionary<int, int> sequenceCounts = new Dictionary<int, int>();

            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Type != MeldType.Sequence ||
                    meld.Tiles == null ||
                    meld.Tiles.Count != 3)
                {
                    continue;
                }

                int key = meld.Tiles[0].TypeIndex;

                if (!sequenceCounts.TryGetValue(key, out int count))
                    count = 0;

                sequenceCounts[key] = count + 1;
            }

            int pairCount = 0;
            foreach (KeyValuePair<int, int> entry in sequenceCounts)
                pairCount += entry.Value / 2;

            return pairCount;
        }

        private void EvaluateYakuhaiYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (decomposition == null || decomposition.Melds == null)
                return;

            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Type != MeldType.Triplet ||
                    meld.Tiles == null ||
                    meld.Tiles.Count <= 0)
                {
                    continue;
                }

                Tile representativeTile = meld.Tiles[0];
                if (!representativeTile.IsHonorTile)
                    continue;

                AddYakuhaiForHonor(context, representativeTile.Honor, yakus);
            }
        }

        private void EvaluateDragonGroupYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            if (!TryGetDragonGroupMasks(
                    candidate,
                    out int dragonTripletMask,
                    out int dragonPairMask))
            {
                return;
            }

            if (dragonTripletMask == CompleteDragonMask)
            {
                TryAddYaku(
                    yakus,
                    YakuKind.Daisangen,
                    true,
                    context.IsClosed);
                return;
            }

            if (CountBits(dragonTripletMask) == 2 &&
                dragonPairMask != 0 &&
                (dragonTripletMask & dragonPairMask) == 0 &&
                (dragonTripletMask | dragonPairMask) == CompleteDragonMask)
            {
                TryAddYaku(
                    yakus,
                    YakuKind.Shousangen,
                    true,
                    context.IsClosed);
            }
        }

        private static bool TryGetDragonGroupMasks(
            HandEvaluationCandidate candidate,
            out int dragonTripletMask,
            out int dragonPairMask)
        {
            dragonTripletMask = 0;
            dragonPairMask = 0;

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (decomposition == null || decomposition.Melds == null)
                return false;

            dragonPairMask = ToDragonMask(decomposition.PairTile);

            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Type != MeldType.Triplet ||
                    meld.Tiles == null ||
                    meld.Tiles.Count <= 0)
                {
                    continue;
                }

                dragonTripletMask |= ToDragonMask(meld.Tiles[0]);
            }

            return true;
        }

        private void AddYakuhaiForHonor(
            HandEvaluationContext context,
            HonorKind honor,
            List<EvaluatedYaku> yakus)
        {
            if (honor == ToHonorKind(context.SeatWind))
                TryAddYaku(yakus, YakuKind.YakuhaiSeatWind, true, context.IsClosed);

            if (honor == ToHonorKind(context.RoundWind))
                TryAddYaku(yakus, YakuKind.YakuhaiRoundWind, true, context.IsClosed);

            switch (honor)
            {
                case HonorKind.White:
                    TryAddYaku(yakus, YakuKind.YakuhaiWhiteDragon, true, context.IsClosed);
                    break;
                case HonorKind.Green:
                    TryAddYaku(yakus, YakuKind.YakuhaiGreenDragon, true, context.IsClosed);
                    break;
                case HonorKind.Red:
                    TryAddYaku(yakus, YakuKind.YakuhaiRedDragon, true, context.IsClosed);
                    break;
            }
        }

        private void EvaluateSanshokuDoujunYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (decomposition == null || decomposition.Melds == null)
                return;

            int[] suitMasksByStartRank = new int[10];
            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Type != MeldType.Sequence ||
                    meld.Tiles == null ||
                    meld.Tiles.Count != 3)
                {
                    continue;
                }

                Tile representativeTile = meld.Tiles[0];
                if (!representativeTile.IsNumberTile)
                    continue;

                suitMasksByStartRank[representativeTile.Rank] |=
                    ToSuitMask(representativeTile.Suit);
            }

            for (int rank = 1; rank <= 7; rank++)
            {
                if (HasAllNumberSuits(suitMasksByStartRank[rank]))
                {
                    TryAddYaku(
                        yakus,
                        YakuKind.SanshokuDoujun,
                        true,
                        context.IsClosed);
                    return;
                }
            }
        }

        private void EvaluateSanshokuDoukouYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (decomposition == null || decomposition.Melds == null)
                return;

            int[] suitMasksByRank = new int[10];
            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Type != MeldType.Triplet ||
                    meld.Tiles == null ||
                    meld.Tiles.Count <= 0)
                {
                    continue;
                }

                Tile representativeTile = meld.Tiles[0];
                if (!representativeTile.IsNumberTile)
                    continue;

                suitMasksByRank[representativeTile.Rank] |=
                    ToSuitMask(representativeTile.Suit);
            }

            for (int rank = 1; rank <= 9; rank++)
            {
                if (HasAllNumberSuits(suitMasksByRank[rank]))
                {
                    TryAddYaku(
                        yakus,
                        YakuKind.SanshokuDoukou,
                        true,
                        context.IsClosed);
                    return;
                }
            }
        }

        private void EvaluateIttsuuYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (decomposition == null || decomposition.Melds == null)
                return;

            int manSequenceMask = 0;
            int pinSequenceMask = 0;
            int souSequenceMask = 0;
            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Type != MeldType.Sequence ||
                    meld.Tiles == null ||
                    meld.Tiles.Count != 3)
                {
                    continue;
                }

                Tile representativeTile = meld.Tiles[0];
                if (!representativeTile.IsNumberTile)
                    continue;

                int sequenceMask = ToIttsuuSequenceMask(representativeTile.Rank);
                if (sequenceMask == 0)
                    continue;

                switch (representativeTile.Suit)
                {
                    case TileSuit.Man:
                        manSequenceMask |= sequenceMask;
                        break;
                    case TileSuit.Pin:
                        pinSequenceMask |= sequenceMask;
                        break;
                    case TileSuit.Sou:
                        souSequenceMask |= sequenceMask;
                        break;
                }
            }

            if (HasCompleteIttsuu(manSequenceMask) ||
                HasCompleteIttsuu(pinSequenceMask) ||
                HasCompleteIttsuu(souSequenceMask))
            {
                TryAddYaku(
                    yakus,
                    YakuKind.Ittsuu,
                    true,
                    context.IsClosed);
            }
        }

        private void EvaluateChantaYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            if (context == null ||
                candidate == null ||
                candidate.Type != HandEvaluationCandidateType.Standard)
            {
                return;
            }

            StandardHandDecomposition decomposition =
                candidate.StandardInterpretation?.Decomposition;

            if (!TryAnalyzeChantaShape(decomposition, out bool hasHonor))
                return;

            if (hasHonor)
            {
                TryAddYaku(
                    yakus,
                    YakuKind.Chanta,
                    true,
                    context.IsClosed);
                return;
            }

            TryAddYaku(
                yakus,
                YakuKind.Junchan,
                true,
                context.IsClosed);
        }

        private static bool TryAnalyzeChantaShape(
            StandardHandDecomposition decomposition,
            out bool hasHonor)
        {
            hasHonor = false;

            if (decomposition == null ||
                decomposition.Melds == null ||
                decomposition.Melds.Count != 4)
            {
                return false;
            }

            Tile pairTile = decomposition.PairTile;
            if (!IsTerminalOrHonor(pairTile))
                return false;

            if (pairTile.IsHonorTile)
                hasHonor = true;

            bool hasSequence = false;
            for (int i = 0; i < decomposition.Melds.Count; i++)
            {
                HandMeld meld = decomposition.Melds[i];
                if (meld == null ||
                    meld.Tiles == null ||
                    meld.Tiles.Count != 3)
                {
                    return false;
                }

                Tile representativeTile = meld.Tiles[0];
                switch (meld.Type)
                {
                    case MeldType.Sequence:
                        if (!representativeTile.IsNumberTile ||
                            (representativeTile.Rank != 1 &&
                             representativeTile.Rank != 7))
                        {
                            return false;
                        }

                        hasSequence = true;
                        break;
                    case MeldType.Triplet:
                        if (!IsTerminalOrHonor(representativeTile))
                            return false;

                        if (representativeTile.IsHonorTile)
                            hasHonor = true;
                        break;
                    default:
                        return false;
                }
            }

            return hasSequence;
        }

        private static int ToSuitMask(TileSuit suit)
        {
            switch (suit)
            {
                case TileSuit.Man:
                    return 1;
                case TileSuit.Pin:
                    return 2;
                case TileSuit.Sou:
                    return 4;
                default:
                    return 0;
            }
        }

        private static bool HasAllNumberSuits(int suitMask)
        {
            return (suitMask & 7) == 7;
        }

        private static bool HasExactlyOneNumberSuit(int suitMask)
        {
            return suitMask == 1 || suitMask == 2 || suitMask == 4;
        }

        private static bool IsTerminalOrHonor(Tile tile)
        {
            return tile.IsHonorTile ||
                   (tile.IsNumberTile && (tile.Rank == 1 || tile.Rank == 9));
        }

        private static bool IsGreenTile(Tile tile)
        {
            if (tile.IsHonorTile)
                return tile.Honor == HonorKind.Green;

            if (!tile.IsNumberTile || tile.Suit != TileSuit.Sou)
                return false;

            switch (tile.Rank)
            {
                case 2:
                case 3:
                case 4:
                case 6:
                case 8:
                    return true;
                default:
                    return false;
            }
        }

        private static int ToDragonMask(Tile tile)
        {
            return tile.IsHonorTile ? ToDragonMask(tile.Honor) : 0;
        }

        private static int ToDragonMask(HonorKind honor)
        {
            switch (honor)
            {
                case HonorKind.White:
                    return 1;
                case HonorKind.Green:
                    return 2;
                case HonorKind.Red:
                    return 4;
                default:
                    return 0;
            }
        }

        private static int CountBits(int mask)
        {
            int count = 0;
            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }

            return count;
        }

        private static int ToIttsuuSequenceMask(int startRank)
        {
            switch (startRank)
            {
                case 1:
                    return 1;
                case 4:
                    return 2;
                case 7:
                    return 4;
                default:
                    return 0;
            }
        }

        private static bool HasCompleteIttsuu(int sequenceMask)
        {
            return (sequenceMask & CompleteIttsuuSequenceMask) ==
                   CompleteIttsuuSequenceMask;
        }

        private static bool IsValuePair(
            Tile pairTile,
            SeatId seatWind,
            RoundWind roundWind)
        {
            if (!pairTile.IsValid)
                return true;

            if (pairTile.IsNumberTile)
                return false;

            if (!pairTile.IsHonorTile)
                return true;

            switch (pairTile.Honor)
            {
                case HonorKind.White:
                case HonorKind.Green:
                case HonorKind.Red:
                    return true;
            }

            HonorKind seatWindHonor = ToHonorKind(seatWind);
            HonorKind roundWindHonor = ToHonorKind(roundWind);
            return pairTile.Honor == seatWindHonor ||
                   pairTile.Honor == roundWindHonor;
        }

        private static HonorKind ToHonorKind(SeatId seatWind)
        {
            switch (seatWind)
            {
                case SeatId.East:
                    return HonorKind.East;
                case SeatId.South:
                    return HonorKind.South;
                case SeatId.West:
                    return HonorKind.West;
                case SeatId.North:
                    return HonorKind.North;
                default:
                    return HonorKind.None;
            }
        }

        private static HonorKind ToHonorKind(RoundWind roundWind)
        {
            switch (roundWind)
            {
                case RoundWind.East:
                    return HonorKind.East;
                case RoundWind.South:
                    return HonorKind.South;
                default:
                    return HonorKind.None;
            }
        }
    }
}
