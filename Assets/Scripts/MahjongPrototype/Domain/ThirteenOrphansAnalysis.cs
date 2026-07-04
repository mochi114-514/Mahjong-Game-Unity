using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class ThirteenOrphansAnalysis
    {
        private static readonly IReadOnlyList<Tile> EmptyRequiredTiles =
            new List<Tile>().AsReadOnly();

        private ThirteenOrphansAnalysis(
            bool isWin,
            IReadOnlyList<Tile> requiredTiles,
            Tile pairTile)
        {
            IsWin = isWin;
            RequiredTiles = requiredTiles;
            PairTile = pairTile;
        }

        public bool IsWin { get; }
        public IReadOnlyList<Tile> RequiredTiles { get; }
        public Tile PairTile { get; }

        public static ThirteenOrphansAnalysis NotWin { get; } =
            new ThirteenOrphansAnalysis(false, EmptyRequiredTiles, default(Tile));

        public static ThirteenOrphansAnalysis Win(
            IReadOnlyList<Tile> requiredTiles,
            Tile pairTile)
        {
            if (requiredTiles == null || requiredTiles.Count != 13)
            {
                throw new ArgumentException(
                    "Thirteen orphans analysis requires thirteen required tiles.",
                    nameof(requiredTiles));
            }

            if (!pairTile.IsValid)
                throw new ArgumentException("Pair tile must be valid.", nameof(pairTile));

            List<Tile> copiedRequiredTiles = new List<Tile>(requiredTiles.Count);
            for (int i = 0; i < requiredTiles.Count; i++)
            {
                if (!requiredTiles[i].IsValid)
                    throw new ArgumentException("Required tiles must be valid.", nameof(requiredTiles));

                copiedRequiredTiles.Add(requiredTiles[i]);
            }

            return new ThirteenOrphansAnalysis(
                true,
                copiedRequiredTiles.AsReadOnly(),
                pairTile);
        }
    }
}
