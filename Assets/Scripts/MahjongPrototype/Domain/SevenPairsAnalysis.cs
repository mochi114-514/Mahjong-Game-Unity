using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class SevenPairsAnalysis
    {
        private static readonly IReadOnlyList<Tile> EmptyPairTiles =
            new List<Tile>().AsReadOnly();

        private SevenPairsAnalysis(bool isWin, IReadOnlyList<Tile> pairTiles)
        {
            IsWin = isWin;
            PairTiles = pairTiles;
        }

        public bool IsWin { get; }
        public IReadOnlyList<Tile> PairTiles { get; }

        public static SevenPairsAnalysis NotWin { get; } =
            new SevenPairsAnalysis(false, EmptyPairTiles);

        public static SevenPairsAnalysis Win(IReadOnlyList<Tile> pairTiles)
        {
            if (pairTiles == null || pairTiles.Count != 7)
                throw new ArgumentException("Seven pairs analysis requires seven pair tiles.", nameof(pairTiles));

            List<Tile> copiedPairTiles = new List<Tile>(pairTiles.Count);
            for (int i = 0; i < pairTiles.Count; i++)
            {
                if (!pairTiles[i].IsValid)
                    throw new ArgumentException("Pair tiles must be valid.", nameof(pairTiles));

                copiedPairTiles.Add(pairTiles[i]);
            }

            return new SevenPairsAnalysis(true, copiedPairTiles.AsReadOnly());
        }
    }
}
