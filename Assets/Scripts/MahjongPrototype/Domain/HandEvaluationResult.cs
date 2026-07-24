using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class HandEvaluationResult
    {
        private static readonly IReadOnlyList<EvaluatedYaku> EmptyYakus =
            new List<EvaluatedYaku>().AsReadOnly();
        private static readonly IReadOnlyList<HandEvaluationCandidateResult> EmptyCandidateResults =
            new List<HandEvaluationCandidateResult>().AsReadOnly();

        public static HandEvaluationResult Empty { get; } =
            new HandEvaluationResult(EmptyYakus, EmptyCandidateResults);

        public HandEvaluationResult(IReadOnlyList<EvaluatedYaku> yakus)
            : this(yakus, EmptyCandidateResults)
        {
        }

        public HandEvaluationResult(
            IReadOnlyList<EvaluatedYaku> yakus,
            IReadOnlyList<HandEvaluationCandidateResult> candidateResults)
        {
            List<EvaluatedYaku> copiedYakus = new List<EvaluatedYaku>();
            if (yakus != null)
            {
                for (int i = 0; i < yakus.Count; i++)
                    copiedYakus.Add(yakus[i]);
            }

            CandidateResults = CopyCandidateResults(candidateResults);
            if (CandidateResults.Count > 0)
            {
                Yakus = EmptyYakus;
                TotalHan = 0;
                TotalYakumanMultiplier = 0;
                HasYakuman = ContainsCandidateWithYakuman(CandidateResults);
                HasYaku = ContainsCandidateWithYaku(CandidateResults);
                return;
            }

            Yakus = copiedYakus.Count == 0 ? EmptyYakus : copiedYakus.AsReadOnly();

            int totalHan = 0;
            int totalYakumanMultiplier = 0;
            for (int i = 0; i < copiedYakus.Count; i++)
            {
                EvaluatedYaku yaku = copiedYakus[i];
                totalYakumanMultiplier += yaku.YakumanMultiplier;
                if (yaku.YakumanMultiplier > 0)
                {
                    continue;
                }

                totalHan += (int)yaku.Han;
            }

            TotalYakumanMultiplier = totalYakumanMultiplier;
            TotalHan = TotalYakumanMultiplier > 0 ? 0 : totalHan;
            HasYakuman = TotalYakumanMultiplier > 0;
            HasYaku = TotalHan > 0 || TotalYakumanMultiplier > 0;
        }

        /// <summary>
        /// Top-level yaku output reserved for the future selected candidate.
        /// Candidate-based evaluations keep this empty until candidate selection is implemented.
        /// </summary>
        public IReadOnlyList<EvaluatedYaku> Yakus { get; }
        public IReadOnlyList<HandEvaluationCandidateResult> CandidateResults { get; }
        /// <summary>
        /// Top-level han output reserved for the future selected candidate.
        /// Candidate-based evaluations keep this 0 until candidate selection is implemented.
        /// </summary>
        public int TotalHan { get; }
        /// <summary>
        /// Legacy top-level yakuman total. Candidate-based evaluations keep this 0.
        /// </summary>
        public int TotalYakumanMultiplier { get; }
        public bool HasYakuman { get; }
        public bool HasYaku { get; }

        private static IReadOnlyList<HandEvaluationCandidateResult> CopyCandidateResults(
            IReadOnlyList<HandEvaluationCandidateResult> candidateResults)
        {
            if (candidateResults == null || candidateResults.Count == 0)
                return EmptyCandidateResults;

            List<HandEvaluationCandidateResult> copiedResults =
                new List<HandEvaluationCandidateResult>(candidateResults.Count);
            for (int i = 0; i < candidateResults.Count; i++)
            {
                if (candidateResults[i] != null)
                    copiedResults.Add(candidateResults[i]);
            }

            return copiedResults.Count == 0
                ? EmptyCandidateResults
                : copiedResults.AsReadOnly();
        }

        private static bool ContainsCandidateWithYaku(
            IReadOnlyList<HandEvaluationCandidateResult> candidateResults)
        {
            if (candidateResults == null)
                return false;

            for (int i = 0; i < candidateResults.Count; i++)
            {
                HandEvaluationCandidateResult result = candidateResults[i];
                if (result != null && result.HasYaku)
                    return true;
            }

            return false;
        }

        private static bool ContainsCandidateWithYakuman(
            IReadOnlyList<HandEvaluationCandidateResult> candidateResults)
        {
            if (candidateResults == null)
                return false;

            for (int i = 0; i < candidateResults.Count; i++)
            {
                HandEvaluationCandidateResult result = candidateResults[i];
                if (result != null && result.HasYakuman)
                    return true;
            }

            return false;
        }
    }
}
