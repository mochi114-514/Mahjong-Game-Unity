using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class ReachChecker
    {
        private const int RequiredHandTileCount = 13;
        private const int TileTypeCount = 34;
        private const int FirstPinTypeIndex = 9;
        private const int FirstSouTypeIndex = 18;
        private const int FirstHonorTypeIndex = 27;
        private const int DrawnTileHandIndex = -1;

        private readonly WinChecker winChecker;

        public ReachChecker()
            : this(new WinChecker())
        {
        }

        public ReachChecker(WinChecker winChecker)
        {
            this.winChecker = winChecker ?? new WinChecker();
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
            for (int typeIndex = 0; typeIndex < TileTypeCount; typeIndex++)
            {
                Tile winningTile = CreateTileFromTypeIndex(typeIndex);
                if (winChecker.CanWinWithTile(handTiles, winningTile))
                    return true;
            }

            return false;
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

        private static Tile CreateTileFromTypeIndex(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= TileTypeCount)
                return default;

            if (typeIndex < FirstPinTypeIndex)
                return Tile.CreateNumber(TileSuit.Man, typeIndex + 1);

            if (typeIndex < FirstSouTypeIndex)
                return Tile.CreateNumber(TileSuit.Pin, typeIndex - FirstPinTypeIndex + 1);

            if (typeIndex < FirstHonorTypeIndex)
                return Tile.CreateNumber(TileSuit.Sou, typeIndex - FirstSouTypeIndex + 1);

            switch (typeIndex)
            {
                case 27:
                    return Tile.CreateHonor(HonorKind.East);
                case 28:
                    return Tile.CreateHonor(HonorKind.South);
                case 29:
                    return Tile.CreateHonor(HonorKind.West);
                case 30:
                    return Tile.CreateHonor(HonorKind.North);
                case 31:
                    return Tile.CreateHonor(HonorKind.White);
                case 32:
                    return Tile.CreateHonor(HonorKind.Green);
                case 33:
                    return Tile.CreateHonor(HonorKind.Red);
                default:
                    return default;
            }
        }
    }
}
