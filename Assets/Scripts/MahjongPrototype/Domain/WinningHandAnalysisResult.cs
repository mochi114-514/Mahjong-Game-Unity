using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class WinningHandAnalysisResult
    {
        private static readonly IReadOnlyList<StandardHandDecomposition> EmptyStandardDecompositions =
            new List<StandardHandDecomposition>().AsReadOnly();

        public WinningHandAnalysisResult(
            IReadOnlyList<StandardHandDecomposition> standardDecompositions,
            SevenPairsAnalysis sevenPairsAnalysis,
            ThirteenOrphansAnalysis thirteenOrphansAnalysis)
        {
            StandardDecompositions = CopyStandardDecompositions(standardDecompositions);
            SevenPairsAnalysis = sevenPairsAnalysis ??
                                 MahjongPrototype.Domain.SevenPairsAnalysis.NotWin;
            ThirteenOrphansAnalysis = thirteenOrphansAnalysis ??
                                      MahjongPrototype.Domain.ThirteenOrphansAnalysis.NotWin;
            CanWin = StandardDecompositions.Count > 0 ||
                     SevenPairsAnalysis.IsWin ||
                     ThirteenOrphansAnalysis.IsWin;
        }

        public bool CanWin { get; }
        public IReadOnlyList<StandardHandDecomposition> StandardDecompositions { get; }
        public SevenPairsAnalysis SevenPairsAnalysis { get; }
        public ThirteenOrphansAnalysis ThirteenOrphansAnalysis { get; }

        public static WinningHandAnalysisResult NotWin { get; } =
            new WinningHandAnalysisResult(
                EmptyStandardDecompositions,
                MahjongPrototype.Domain.SevenPairsAnalysis.NotWin,
                MahjongPrototype.Domain.ThirteenOrphansAnalysis.NotWin);

        private static IReadOnlyList<StandardHandDecomposition> CopyStandardDecompositions(
            IReadOnlyList<StandardHandDecomposition> standardDecompositions)
        {
            if (standardDecompositions == null || standardDecompositions.Count == 0)
                return EmptyStandardDecompositions;

            List<StandardHandDecomposition> copiedDecompositions =
                new List<StandardHandDecomposition>(standardDecompositions.Count);
            for (int i = 0; i < standardDecompositions.Count; i++)
            {
                if (standardDecompositions[i] != null)
                    copiedDecompositions.Add(standardDecompositions[i]);
            }

            return copiedDecompositions.Count == 0
                ? EmptyStandardDecompositions
                : copiedDecompositions.AsReadOnly();
        }
    }
}
