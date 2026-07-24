using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class HandEvaluationCandidateResult
    {
        private static readonly IReadOnlyList<EvaluatedYaku> EmptyYakus =
            new List<EvaluatedYaku>().AsReadOnly();

        public HandEvaluationCandidateResult(
            HandEvaluationCandidate candidate,
            IReadOnlyList<EvaluatedYaku> yakus)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            Yakus = CopyYakus(yakus);

            int totalHan = 0;
            int totalYakumanMultiplier = 0;
            for (int i = 0; i < Yakus.Count; i++)
            {
                EvaluatedYaku yaku = Yakus[i];
                totalYakumanMultiplier += yaku.YakumanMultiplier;
                if (yaku.YakumanMultiplier > 0)
                {
                    continue;
                }

                totalHan += (int)yaku.Han;
            }

            TotalYakumanMultiplier = totalYakumanMultiplier;
            TotalHan = TotalYakumanMultiplier > 0 ? 0 : totalHan;
        }

        public HandEvaluationCandidate Candidate { get; }
        public IReadOnlyList<EvaluatedYaku> Yakus { get; }
        public int TotalHan { get; }
        public int TotalYakumanMultiplier { get; }
        public bool HasYakuman => TotalYakumanMultiplier > 0;
        public bool HasYaku => TotalHan > 0 || TotalYakumanMultiplier > 0;

        private static IReadOnlyList<EvaluatedYaku> CopyYakus(
            IReadOnlyList<EvaluatedYaku> yakus)
        {
            if (yakus == null || yakus.Count == 0)
                return EmptyYakus;

            List<EvaluatedYaku> copiedYakus = new List<EvaluatedYaku>(yakus.Count);
            for (int i = 0; i < yakus.Count; i++)
                copiedYakus.Add(yakus[i]);

            return copiedYakus.AsReadOnly();
        }
    }
}
