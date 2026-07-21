using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class ReachChecker
    {
        private const int RequiredHandTileCount = 13;
        private const int TileTypeCount = 34;
        private const int DrawnTileHandIndex = -1;

        private readonly WinningTileWaitEnumerator waitEnumerator;

        public ReachChecker()
            : this(new WinChecker())
        {
        }

        public ReachChecker(WinChecker winChecker)
        {
            waitEnumerator = new WinningTileWaitEnumerator(winChecker);
        }

        public ReachCheckResult CheckReach(IReadOnlyList<Tile> handTiles, Tile drawnTile)
        {
            if (!IsValidInput(handTiles, drawnTile))
                return ReachCheckResult.NotReady();

            List<ReachDiscardCandidate> candidates = new List<ReachDiscardCandidate>();

            for (int handIndex = 0; handIndex < handTiles.Count; handIndex++)
            {
                Tile discardTile = handTiles[handIndex];
                List<Tile> remainingTiles = BuildTilesAfterHandDiscard(handTiles, drawnTile, handIndex);
                if (IsTenpai(remainingTiles))
                {
                    candidates.Add(new ReachDiscardCandidate(
                        DiscardSource.Hand,
                        handIndex,
                        discardTile));
                }
            }

            if (IsTenpai(handTiles))
            {
                candidates.Add(new ReachDiscardCandidate(
                    DiscardSource.DrawnTile,
                    DrawnTileHandIndex,
                    drawnTile));
            }

            return ReachCheckResult.Ready(candidates);
        }

        private bool IsTenpai(IReadOnlyList<Tile> handTiles)
        {
            return waitEnumerator.EnumerateWinningTiles(handTiles).Count > 0;
        }

        private static bool IsValidInput(IReadOnlyList<Tile> handTiles, Tile drawnTile)
        {
            if (handTiles == null || handTiles.Count != RequiredHandTileCount || !drawnTile.IsValid)
                return false;

            int[] typeCounts = new int[TileTypeCount];
            typeCounts[drawnTile.TypeIndex]++;

            for (int i = 0; i < handTiles.Count; i++)
            {
                if (!handTiles[i].IsValid)
                    return false;

                int typeIndex = handTiles[i].TypeIndex;
                if (typeIndex < 0 || typeIndex >= TileTypeCount)
                    return false;

                typeCounts[typeIndex]++;
                if (typeCounts[typeIndex] > 4)
                    return false;
            }

            return true;
        }

        private static List<Tile> BuildTilesAfterHandDiscard(
            IReadOnlyList<Tile> handTiles,
            Tile drawnTile,
            int discardedHandIndex)
        {
            List<Tile> remainingTiles = new List<Tile>(RequiredHandTileCount);
            for (int i = 0; i < handTiles.Count; i++)
            {
                if (i != discardedHandIndex)
                    remainingTiles.Add(handTiles[i]);
            }

            remainingTiles.Add(drawnTile);
            return remainingTiles;
        }
    }
}
