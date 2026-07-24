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
            int selectedYakumanMultiplier = 0;
            HandEvaluationCandidateResult selectedNormalCandidate = null;

            for (int i = 0; i < evaluationResult.CandidateResults.Count; i++)
            {
                HandEvaluationCandidateResult candidate = evaluationResult.CandidateResults[i];
                if (candidate == null || !candidate.HasYaku)
                    continue;

                if (candidate.HasYakuman)
                {
                    if (selectedYakumanCandidate == null ||
                        candidate.TotalYakumanMultiplier > selectedYakumanMultiplier)
                    {
                        selectedYakumanCandidate = candidate;
                        selectedYakumanMultiplier = candidate.TotalYakumanMultiplier;
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
    }
}
