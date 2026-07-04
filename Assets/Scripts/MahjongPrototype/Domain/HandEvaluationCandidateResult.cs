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
            bool hasYakuman = false;
            for (int i = 0; i < Yakus.Count; i++)
            {
                EvaluatedYaku yaku = Yakus[i];
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

        public HandEvaluationCandidate Candidate { get; }
        public IReadOnlyList<EvaluatedYaku> Yakus { get; }
        public int TotalHan { get; }
        public bool HasYakuman { get; }
        public bool HasYaku { get; }

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
