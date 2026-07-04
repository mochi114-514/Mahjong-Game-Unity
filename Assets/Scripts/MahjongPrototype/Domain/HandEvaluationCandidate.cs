using System;

namespace MahjongPrototype.Domain
{
    public sealed class HandEvaluationCandidate
    {
        private HandEvaluationCandidate(
            HandEvaluationCandidateType type,
            StandardWinningInterpretation standardInterpretation,
            SevenPairsAnalysis sevenPairsAnalysis,
            ThirteenOrphansAnalysis thirteenOrphansAnalysis)
        {
            ValidateExclusiveAnalysis(
                type,
                standardInterpretation,
                sevenPairsAnalysis,
                thirteenOrphansAnalysis);

            Type = type;
            StandardInterpretation = standardInterpretation;
            SevenPairsAnalysis = sevenPairsAnalysis;
            ThirteenOrphansAnalysis = thirteenOrphansAnalysis;
        }

        public HandEvaluationCandidateType Type { get; }
        public StandardWinningInterpretation StandardInterpretation { get; }
        public SevenPairsAnalysis SevenPairsAnalysis { get; }
        public ThirteenOrphansAnalysis ThirteenOrphansAnalysis { get; }

        public static HandEvaluationCandidate Standard(
            StandardWinningInterpretation interpretation)
        {
            if (interpretation == null)
                throw new ArgumentNullException(nameof(interpretation));

            return new HandEvaluationCandidate(
                HandEvaluationCandidateType.Standard,
                interpretation,
                null,
                null);
        }

        public static HandEvaluationCandidate SevenPairs(
            SevenPairsAnalysis analysis)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            if (!analysis.IsWin)
                throw new ArgumentException("Seven pairs candidate requires a winning analysis.", nameof(analysis));

            return new HandEvaluationCandidate(
                HandEvaluationCandidateType.SevenPairs,
                null,
                analysis,
                null);
        }

        public static HandEvaluationCandidate ThirteenOrphans(
            ThirteenOrphansAnalysis analysis)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            if (!analysis.IsWin)
            {
                throw new ArgumentException(
                    "Thirteen orphans candidate requires a winning analysis.",
                    nameof(analysis));
            }

            return new HandEvaluationCandidate(
                HandEvaluationCandidateType.ThirteenOrphans,
                null,
                null,
                analysis);
        }

        private static void ValidateExclusiveAnalysis(
            HandEvaluationCandidateType type,
            StandardWinningInterpretation standardInterpretation,
            SevenPairsAnalysis sevenPairsAnalysis,
            ThirteenOrphansAnalysis thirteenOrphansAnalysis)
        {
            int analysisCount = 0;
            if (standardInterpretation != null)
                analysisCount++;
            if (sevenPairsAnalysis != null)
                analysisCount++;
            if (thirteenOrphansAnalysis != null)
                analysisCount++;

            if (analysisCount != 1 || type == HandEvaluationCandidateType.None)
                throw new ArgumentException("A hand evaluation candidate must contain exactly one analysis.");

            if (type == HandEvaluationCandidateType.Standard && standardInterpretation != null)
                return;
            if (type == HandEvaluationCandidateType.SevenPairs && sevenPairsAnalysis != null)
                return;
            if (type == HandEvaluationCandidateType.ThirteenOrphans && thirteenOrphansAnalysis != null)
                return;

            throw new ArgumentException("Candidate type must match its stored analysis.");
        }
    }
}
