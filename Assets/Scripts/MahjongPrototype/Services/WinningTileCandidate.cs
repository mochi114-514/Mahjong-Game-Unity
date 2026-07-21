using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public readonly struct WinningTileCandidate
    {
        public WinningTileCandidate(Tile tile, int visibleRemainingCount)
        {
            if (!tile.IsValid)
                throw new ArgumentException("A winning candidate tile must be valid.", nameof(tile));

            Tile = tile;
            VisibleRemainingCount = Math.Max(0, Math.Min(4, visibleRemainingCount));
        }

        public Tile Tile { get; }
        public int VisibleRemainingCount { get; }
    }

    public sealed class ReachWinningCandidateEvaluation
    {
        private readonly IReadOnlyList<WinningTileCandidate> winningTiles;

        public ReachWinningCandidateEvaluation(
            ReachDiscardCandidate discardCandidate,
            IReadOnlyList<WinningTileCandidate> winningTiles)
        {
            DiscardCandidate = discardCandidate;
            this.winningTiles = CopyWinningTiles(winningTiles);
        }

        public ReachDiscardCandidate DiscardCandidate { get; }
        public IReadOnlyList<WinningTileCandidate> WinningTiles => winningTiles;

        internal static IReadOnlyList<WinningTileCandidate> CopyWinningTiles(
            IReadOnlyList<WinningTileCandidate> source)
        {
            List<WinningTileCandidate> copy = new List<WinningTileCandidate>(
                source != null ? source.Count : 0);
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                    copy.Add(source[i]);
            }

            return copy.AsReadOnly();
        }
    }

    public sealed class ReachWinningCandidateGroup
    {
        private readonly IReadOnlyList<ReachDiscardCandidate> discardCandidates;
        private readonly IReadOnlyList<WinningTileCandidate> winningTiles;

        public ReachWinningCandidateGroup(
            IReadOnlyList<ReachDiscardCandidate> discardCandidates,
            IReadOnlyList<WinningTileCandidate> winningTiles)
        {
            List<ReachDiscardCandidate> copiedDiscards = new List<ReachDiscardCandidate>(
                discardCandidates != null ? discardCandidates.Count : 0);
            if (discardCandidates != null)
            {
                for (int i = 0; i < discardCandidates.Count; i++)
                    copiedDiscards.Add(discardCandidates[i]);
            }

            this.discardCandidates = copiedDiscards.AsReadOnly();
            this.winningTiles = ReachWinningCandidateEvaluation.CopyWinningTiles(winningTiles);
        }

        public IReadOnlyList<ReachDiscardCandidate> DiscardCandidates => discardCandidates;
        public IReadOnlyList<WinningTileCandidate> WinningTiles => winningTiles;
    }
}
