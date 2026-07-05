using System.Collections.Generic;
using MahjongPrototype.Definitions;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class HandEvaluator
    {
        private readonly YakuDefinitionCatalog catalog;

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

            return new HandEvaluationCandidateResult(candidate, yakus);
        }

        private void EvaluateCommonYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            TryAddYaku(yakus, YakuKind.Reach, context.IsReachDeclared, context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.MenzenTsumo,
                context.WinType == WinType.Tsumo && context.IsClosed,
                context.IsClosed);
            TryAddYaku(yakus, YakuKind.Tanyao, IsTanyao(context), context.IsClosed);
        }

        private void EvaluateStandardCandidateYaku(
            HandEvaluationContext context,
            HandEvaluationCandidate candidate,
            List<EvaluatedYaku> yakus)
        {
            TryAddYaku(yakus, YakuKind.Pinfu, IsPinfu(context, candidate), context.IsClosed);
            EvaluatePeikouYaku(context, candidate, yakus);
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
