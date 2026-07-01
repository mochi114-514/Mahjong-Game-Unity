using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class HandEvaluationResult
    {
        public static HandEvaluationResult Empty { get; } =
            new HandEvaluationResult(new List<EvaluatedYaku>());

        public HandEvaluationResult(IReadOnlyList<EvaluatedYaku> yakus)
        {
            List<EvaluatedYaku> copiedYakus = new List<EvaluatedYaku>();
            if (yakus != null)
            {
                for (int i = 0; i < yakus.Count; i++)
                    copiedYakus.Add(yakus[i]);
            }

            Yakus = copiedYakus.AsReadOnly();

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
        public int TotalHan { get; }
        public bool HasYakuman { get; }
        public bool HasYaku { get; }
    }
}
