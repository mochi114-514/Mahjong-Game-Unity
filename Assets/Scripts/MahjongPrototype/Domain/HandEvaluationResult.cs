using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class HandEvaluationResult
    {
        private static readonly IReadOnlyList<HandEvaluationCandidateResult> EmptyCandidateResults =
            new List<HandEvaluationCandidateResult>().AsReadOnly();

        public static HandEvaluationResult Empty { get; } =
            new HandEvaluationResult(new List<EvaluatedYaku>(), EmptyCandidateResults);

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

            Yakus = copiedYakus.AsReadOnly();
            CandidateResults = CopyCandidateResults(candidateResults);

            int totalHan = 0;
            bool hasYakuman = false;
            for (int i = 0; i < copiedYakus.Count; i++)
            {
                EvaluatedYaku yaku = copiedYakus[i];
                if (yaku.IsYakuman)
                {
                    hasYakuman = true;
                    continue;
                }

                totalHan += (int)yaku.Han;
            }

            TotalHan = totalHan;
            HasYakuman = hasYakuman;
            HasYaku = HasYakuman || TotalHan > 0;
        }

        public IReadOnlyList<EvaluatedYaku> Yakus { get; }
        public IReadOnlyList<HandEvaluationCandidateResult> CandidateResults { get; }
        public int TotalHan { get; }
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
    }
}
