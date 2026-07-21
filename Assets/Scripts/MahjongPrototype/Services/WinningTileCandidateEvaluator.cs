using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinningTileCandidateEvaluator
    {
        private static readonly IReadOnlyList<WinningTileCandidate> EmptyWinningTiles =
            new List<WinningTileCandidate>().AsReadOnly();

        private readonly WinningTileWaitEnumerator waitEnumerator;
        private readonly VisibleRemainingTileCounter visibleRemainingTileCounter;

        public WinningTileCandidateEvaluator()
            : this(new WinningTileWaitEnumerator(), new VisibleRemainingTileCounter())
        {
        }

        public WinningTileCandidateEvaluator(
            WinningTileWaitEnumerator waitEnumerator,
            VisibleRemainingTileCounter visibleRemainingTileCounter)
        {
            this.waitEnumerator = waitEnumerator ?? new WinningTileWaitEnumerator();
            this.visibleRemainingTileCounter =
                visibleRemainingTileCounter ?? new VisibleRemainingTileCounter();
        }

        public IReadOnlyList<WinningTileCandidate> EvaluateCurrentHand(
            MahjongGameState gameState)
        {
            return gameState == null
                ? EmptyWinningTiles
                : EvaluateCurrentHand(gameState, gameState.SelfSeat);
        }

        public IReadOnlyList<WinningTileCandidate> EvaluateCurrentHand(
            MahjongGameState gameState,
            SeatId localSeat)
        {
            if (!TryGetOccupiedPlayer(gameState, localSeat, out PlayerSeat player))
                return EmptyWinningTiles;

            return EvaluateTiles(
                gameState,
                localSeat,
                player.Hand.GetTiles(),
                player.Melds);
        }

        public IReadOnlyList<WinningTileCandidate> EvaluateAfterDiscard(
            MahjongGameState gameState,
            ReachDiscardCandidate discardCandidate)
        {
            return gameState == null
                ? EmptyWinningTiles
                : EvaluateAfterDiscard(gameState, gameState.SelfSeat, discardCandidate);
        }

        public IReadOnlyList<WinningTileCandidate> EvaluateAfterDiscard(
            MahjongGameState gameState,
            SeatId localSeat,
            ReachDiscardCandidate discardCandidate)
        {
            if (!TryGetOccupiedPlayer(gameState, localSeat, out PlayerSeat player) ||
                !TryBuildTilesAfterDiscard(player, discardCandidate, out IReadOnlyList<Tile> handTiles))
            {
                return EmptyWinningTiles;
            }

            return EvaluateTiles(gameState, localSeat, handTiles, player.Melds);
        }

        public IReadOnlyList<ReachWinningCandidateEvaluation> EvaluateReachCandidates(
            MahjongGameState gameState,
            IReadOnlyList<ReachDiscardCandidate> discardCandidates)
        {
            return gameState == null
                ? new List<ReachWinningCandidateEvaluation>().AsReadOnly()
                : EvaluateReachCandidates(gameState, gameState.SelfSeat, discardCandidates);
        }

        public IReadOnlyList<ReachWinningCandidateEvaluation> EvaluateReachCandidates(
            MahjongGameState gameState,
            SeatId localSeat,
            IReadOnlyList<ReachDiscardCandidate> discardCandidates)
        {
            List<ReachWinningCandidateEvaluation> results =
                new List<ReachWinningCandidateEvaluation>();
            if (gameState == null || discardCandidates == null)
                return results.AsReadOnly();

            for (int i = 0; i < discardCandidates.Count; i++)
            {
                ReachDiscardCandidate discardCandidate = discardCandidates[i];
                IReadOnlyList<WinningTileCandidate> winningTiles = EvaluateAfterDiscard(
                    gameState,
                    localSeat,
                    discardCandidate);
                results.Add(new ReachWinningCandidateEvaluation(
                    discardCandidate,
                    winningTiles));
            }

            return results.AsReadOnly();
        }

        public IReadOnlyList<ReachWinningCandidateGroup> GroupReachCandidates(
            MahjongGameState gameState,
            IReadOnlyList<ReachDiscardCandidate> discardCandidates)
        {
            return gameState == null
                ? new List<ReachWinningCandidateGroup>().AsReadOnly()
                : GroupReachCandidates(gameState, gameState.SelfSeat, discardCandidates);
        }

        public IReadOnlyList<ReachWinningCandidateGroup> GroupReachCandidates(
            MahjongGameState gameState,
            SeatId localSeat,
            IReadOnlyList<ReachDiscardCandidate> discardCandidates)
        {
            IReadOnlyList<ReachWinningCandidateEvaluation> evaluations =
                EvaluateReachCandidates(gameState, localSeat, discardCandidates);
            List<MutableReachGroup> mutableGroups = new List<MutableReachGroup>();

            for (int i = 0; i < evaluations.Count; i++)
            {
                ReachWinningCandidateEvaluation evaluation = evaluations[i];
                int groupIndex = FindGroupIndex(mutableGroups, evaluation.WinningTiles);
                if (groupIndex < 0)
                {
                    mutableGroups.Add(new MutableReachGroup(
                        evaluation.DiscardCandidate,
                        evaluation.WinningTiles));
                }
                else
                {
                    mutableGroups[groupIndex].DiscardCandidates.Add(
                        evaluation.DiscardCandidate);
                }
            }

            List<ReachWinningCandidateGroup> groups =
                new List<ReachWinningCandidateGroup>(mutableGroups.Count);
            for (int i = 0; i < mutableGroups.Count; i++)
            {
                MutableReachGroup group = mutableGroups[i];
                groups.Add(new ReachWinningCandidateGroup(
                    group.DiscardCandidates,
                    group.WinningTiles));
            }

            return groups.AsReadOnly();
        }

        private IReadOnlyList<WinningTileCandidate> EvaluateTiles(
            MahjongGameState gameState,
            SeatId localSeat,
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<PlayerMeld> melds)
        {
            IReadOnlyList<Tile> waits = waitEnumerator.EnumerateWinningTiles(handTiles, melds);
            if (waits.Count <= 0)
                return EmptyWinningTiles;

            int[] visibleRemainingCounts =
                visibleRemainingTileCounter.BuildVisibleRemainingCounts(gameState, localSeat);
            List<WinningTileCandidate> candidates =
                new List<WinningTileCandidate>(waits.Count);
            for (int i = 0; i < waits.Count; i++)
            {
                Tile tile = waits[i];
                candidates.Add(new WinningTileCandidate(
                    tile,
                    visibleRemainingCounts[tile.TypeIndex]));
            }

            return candidates.AsReadOnly();
        }

        private static bool TryBuildTilesAfterDiscard(
            PlayerSeat player,
            ReachDiscardCandidate discardCandidate,
            out IReadOnlyList<Tile> handTiles)
        {
            handTiles = null;
            if (player == null || !player.DrawnTile.HasValue || !discardCandidate.Tile.IsValid)
                return false;

            IReadOnlyList<Tile> currentHand = player.Hand.GetTiles();
            if (discardCandidate.Source == DiscardSource.DrawnTile)
            {
                if (discardCandidate.Tile != player.DrawnTile.Value)
                    return false;

                handTiles = currentHand;
                return true;
            }

            if (discardCandidate.Source != DiscardSource.Hand ||
                discardCandidate.HandIndex < 0 ||
                discardCandidate.HandIndex >= currentHand.Count ||
                currentHand[discardCandidate.HandIndex] != discardCandidate.Tile)
            {
                return false;
            }

            List<Tile> remainingTiles = new List<Tile>(currentHand.Count);
            for (int i = 0; i < currentHand.Count; i++)
            {
                if (i != discardCandidate.HandIndex)
                    remainingTiles.Add(currentHand[i]);
            }

            remainingTiles.Add(player.DrawnTile.Value);
            handTiles = remainingTiles.AsReadOnly();
            return true;
        }

        private static bool TryGetOccupiedPlayer(
            MahjongGameState gameState,
            SeatId seat,
            out PlayerSeat player)
        {
            player = null;
            if (gameState == null)
                return false;

            IReadOnlyList<SeatId> occupiedSeats = gameState.OccupiedSeats;
            for (int i = 0; i < occupiedSeats.Count; i++)
            {
                if (occupiedSeats[i] != seat)
                    continue;

                player = gameState.GetPlayerSeat(seat);
                return true;
            }

            return false;
        }

        private static int FindGroupIndex(
            IReadOnlyList<MutableReachGroup> groups,
            IReadOnlyList<WinningTileCandidate> winningTiles)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (HasSameWaits(groups[i].WinningTiles, winningTiles))
                    return i;
            }

            return -1;
        }

        private static bool HasSameWaits(
            IReadOnlyList<WinningTileCandidate> left,
            IReadOnlyList<WinningTileCandidate> right)
        {
            if (left.Count != right.Count)
                return false;

            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].Tile.TypeIndex != right[i].Tile.TypeIndex)
                    return false;
            }

            return true;
        }

        private sealed class MutableReachGroup
        {
            public MutableReachGroup(
                ReachDiscardCandidate discardCandidate,
                IReadOnlyList<WinningTileCandidate> winningTiles)
            {
                DiscardCandidates = new List<ReachDiscardCandidate> { discardCandidate };
                WinningTiles = winningTiles;
            }

            public List<ReachDiscardCandidate> DiscardCandidates { get; }
            public IReadOnlyList<WinningTileCandidate> WinningTiles { get; }
        }
    }
}
