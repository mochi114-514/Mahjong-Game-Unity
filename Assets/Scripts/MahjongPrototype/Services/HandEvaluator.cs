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

            List<EvaluatedYaku> yakus = EvaluateTopLevelYakus(context);
            List<HandEvaluationCandidateResult> candidateResults =
                EvaluateCandidateResults(context);

            if (yakus.Count <= 0 && candidateResults.Count <= 0)
                return HandEvaluationResult.Empty;

            return new HandEvaluationResult(yakus, candidateResults);
        }

        private List<EvaluatedYaku> EvaluateTopLevelYakus(HandEvaluationContext context)
        {
            List<EvaluatedYaku> yakus = new List<EvaluatedYaku>();
            TryAddYaku(yakus, YakuKind.Reach, context.IsReachDeclared, context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.MenzenTsumo,
                context.WinType == WinType.Tsumo && context.IsClosed,
                context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.SevenPairs,
                context.Shape == WinningHandShape.SevenPairs,
                context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.KokushiMusou,
                context.Shape == WinningHandShape.ThirteenOrphans,
                context.IsClosed);
            TryAddYaku(yakus, YakuKind.Tanyao, IsTanyao(context), context.IsClosed);

            return yakus;
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
            TryAddYaku(yakus, YakuKind.Reach, context.IsReachDeclared, context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.MenzenTsumo,
                context.WinType == WinType.Tsumo && context.IsClosed,
                context.IsClosed);
            TryAddYaku(yakus, YakuKind.Tanyao, IsTanyao(context), context.IsClosed);

            TryAddYaku(
                yakus,
                YakuKind.SevenPairs,
                candidate.Type == HandEvaluationCandidateType.SevenPairs &&
                candidate.SevenPairsAnalysis != null &&
                candidate.SevenPairsAnalysis.IsWin,
                context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.KokushiMusou,
                candidate.Type == HandEvaluationCandidateType.ThirteenOrphans &&
                candidate.ThirteenOrphansAnalysis != null &&
                candidate.ThirteenOrphansAnalysis.IsWin,
                context.IsClosed);

            return new HandEvaluationCandidateResult(candidate, yakus);
        }

        private void TryAddYaku(
            List<EvaluatedYaku> yakus,
            YakuKind kind,
            bool condition,
            bool isClosed)
        {
            if (!condition || !catalog.TryGet(kind, out YakuDefinition definition))
                return;

            HanValue han = ResolveHan(definition, isClosed);
            if (!definition.IsYakuman && han == HanValue.None)
                return;

            yakus.Add(new EvaluatedYaku(
                definition.Kind,
                definition.DisplayName,
                definition.IsYakuman ? HanValue.None : han,
                definition.IsYakuman));
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
    }
}
