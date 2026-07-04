using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class WinningHandAnalysisResult
    {
        private static readonly IReadOnlyList<StandardHandDecomposition> EmptyStandardDecompositions =
            new List<StandardHandDecomposition>().AsReadOnly();
        private static readonly IReadOnlyList<StandardWinningInterpretation> EmptyStandardWinningInterpretations =
            new List<StandardWinningInterpretation>().AsReadOnly();

        public WinningHandAnalysisResult(
            IReadOnlyList<StandardHandDecomposition> standardDecompositions,
            SevenPairsAnalysis sevenPairsAnalysis,
            ThirteenOrphansAnalysis thirteenOrphansAnalysis)
            : this(
                standardDecompositions,
                sevenPairsAnalysis,
                thirteenOrphansAnalysis,
                EmptyStandardWinningInterpretations)
        {
        }

        public WinningHandAnalysisResult(
            IReadOnlyList<StandardHandDecomposition> standardDecompositions,
            SevenPairsAnalysis sevenPairsAnalysis,
            ThirteenOrphansAnalysis thirteenOrphansAnalysis,
            IReadOnlyList<StandardWinningInterpretation> standardWinningInterpretations)
        {
            StandardDecompositions = CopyStandardDecompositions(standardDecompositions);
            SevenPairsAnalysis = sevenPairsAnalysis ??
                                 MahjongPrototype.Domain.SevenPairsAnalysis.NotWin;
            ThirteenOrphansAnalysis = thirteenOrphansAnalysis ??
                                      MahjongPrototype.Domain.ThirteenOrphansAnalysis.NotWin;
            StandardWinningInterpretations =
                CopyStandardWinningInterpretations(standardWinningInterpretations);
            CanWin = StandardDecompositions.Count > 0 ||
                     SevenPairsAnalysis.IsWin ||
                     ThirteenOrphansAnalysis.IsWin;
        }

        public bool CanWin { get; }
        public IReadOnlyList<StandardHandDecomposition> StandardDecompositions { get; }
        public IReadOnlyList<StandardWinningInterpretation> StandardWinningInterpretations { get; }
        public SevenPairsAnalysis SevenPairsAnalysis { get; }
        public ThirteenOrphansAnalysis ThirteenOrphansAnalysis { get; }

        public static WinningHandAnalysisResult NotWin { get; } =
            new WinningHandAnalysisResult(
                EmptyStandardDecompositions,
                MahjongPrototype.Domain.SevenPairsAnalysis.NotWin,
                MahjongPrototype.Domain.ThirteenOrphansAnalysis.NotWin,
                EmptyStandardWinningInterpretations);

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

        private static IReadOnlyList<StandardWinningInterpretation> CopyStandardWinningInterpretations(
            IReadOnlyList<StandardWinningInterpretation> standardWinningInterpretations)
        {
            if (standardWinningInterpretations == null || standardWinningInterpretations.Count == 0)
                return EmptyStandardWinningInterpretations;

            List<StandardWinningInterpretation> copiedInterpretations =
                new List<StandardWinningInterpretation>(standardWinningInterpretations.Count);
            for (int i = 0; i < standardWinningInterpretations.Count; i++)
            {
                if (standardWinningInterpretations[i] != null)
                    copiedInterpretations.Add(standardWinningInterpretations[i]);
            }

            return copiedInterpretations.Count == 0
                ? EmptyStandardWinningInterpretations
                : copiedInterpretations.AsReadOnly();
        }
    }
}
