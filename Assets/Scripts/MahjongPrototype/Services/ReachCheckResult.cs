using System.Collections.Generic;

namespace MahjongPrototype.Services
{
    public sealed class ReachCheckResult
    {
        private static readonly IReadOnlyList<ReachDiscardCandidate> EmptyCandidates =
            new List<ReachDiscardCandidate>().AsReadOnly();

        private ReachCheckResult(IReadOnlyList<ReachDiscardCandidate> candidates)
        {
            Candidates = candidates;
        }

        public bool CanReach => Candidates.Count > 0;
        public IReadOnlyList<ReachDiscardCandidate> Candidates { get; }

        public static ReachCheckResult NotReady()
        {
            return new ReachCheckResult(EmptyCandidates);
        }

        public static ReachCheckResult Ready(IReadOnlyList<ReachDiscardCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return NotReady();

            List<ReachDiscardCandidate> copiedCandidates =
                new List<ReachDiscardCandidate>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                copiedCandidates.Add(candidates[i]);

            return new ReachCheckResult(copiedCandidates.AsReadOnly());
        }
    }
}
