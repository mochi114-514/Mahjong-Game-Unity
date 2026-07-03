using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MahjongPrototype.Domain
{
    public sealed class FuritenEvaluationResultSet
    {
        private readonly ReadOnlyCollection<FuritenSeatEvaluationResult> results;

        public FuritenEvaluationResultSet(IEnumerable<FuritenSeatEvaluationResult> results)
        {
            List<FuritenSeatEvaluationResult> copiedResults =
                new List<FuritenSeatEvaluationResult>();

            if (results != null)
            {
                foreach (FuritenSeatEvaluationResult result in results)
                {
                    if (result == null || ContainsSeat(copiedResults, result.Seat))
                        continue;

                    copiedResults.Add(result);
                }
            }

            this.results = copiedResults.AsReadOnly();
        }

        public static FuritenEvaluationResultSet Empty { get; } =
            new FuritenEvaluationResultSet(null);

        public IReadOnlyList<FuritenSeatEvaluationResult> Results => results;
        public int Count => results.Count;

        public bool TryGet(
            SeatId seat,
            out FuritenSeatEvaluationResult result)
        {
            for (int i = 0; i < results.Count; i++)
            {
                FuritenSeatEvaluationResult current = results[i];
                if (current.Seat == seat)
                {
                    result = current;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool ContainsSeat(
            List<FuritenSeatEvaluationResult> results,
            SeatId seat)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Seat == seat)
                    return true;
            }

            return false;
        }
    }
}
