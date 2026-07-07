using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinningCandidateSelector
    {
        public HandEvaluationCandidateResult Select(HandEvaluationResult evaluationResult)
        {
            if (evaluationResult == null ||
                evaluationResult.CandidateResults == null ||
                evaluationResult.CandidateResults.Count <= 0)
            {
                return null;
            }

            HandEvaluationCandidateResult selectedYakumanCandidate = null;
            int selectedYakumanCount = 0;
            HandEvaluationCandidateResult selectedNormalCandidate = null;

            for (int i = 0; i < evaluationResult.CandidateResults.Count; i++)
            {
                HandEvaluationCandidateResult candidate = evaluationResult.CandidateResults[i];
                if (candidate == null || !candidate.HasYaku)
                    continue;

                if (candidate.HasYakuman)
                {
                    int yakumanCount = CountYakuman(candidate);
                    if (selectedYakumanCandidate == null || yakumanCount > selectedYakumanCount)
                    {
                        selectedYakumanCandidate = candidate;
                        selectedYakumanCount = yakumanCount;
                    }

                    continue;
                }

                if (selectedNormalCandidate == null ||
                    candidate.TotalHan > selectedNormalCandidate.TotalHan)
                {
                    selectedNormalCandidate = candidate;
                }
            }

            return selectedYakumanCandidate ?? selectedNormalCandidate;
        }

        private static int CountYakuman(HandEvaluationCandidateResult candidate)
        {
            int count = 0;
            if (candidate.Yakus == null)
                return count;

            for (int i = 0; i < candidate.Yakus.Count; i++)
            {
                if (candidate.Yakus[i].IsYakuman)
                    count++;
            }

            return count;
        }
    }
}
