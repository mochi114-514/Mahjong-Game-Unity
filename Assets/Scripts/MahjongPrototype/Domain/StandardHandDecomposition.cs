using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class StandardHandDecomposition
    {
        public StandardHandDecomposition(Tile pairTile, IReadOnlyList<HandMeld> melds)
        {
            if (!pairTile.IsValid)
                throw new ArgumentException("Pair tile must be valid.", nameof(pairTile));

            if (melds == null || melds.Count != 4)
                throw new ArgumentException("A standard hand decomposition must contain four melds.", nameof(melds));

            List<HandMeld> copiedMelds = new List<HandMeld>(melds.Count);
            for (int i = 0; i < melds.Count; i++)
            {
                if (melds[i] == null)
                    throw new ArgumentException("Melds must not contain null.", nameof(melds));

                copiedMelds.Add(melds[i]);
            }

            PairTile = pairTile;
            Melds = copiedMelds.AsReadOnly();
        }

        public Tile PairTile { get; }
        public IReadOnlyList<HandMeld> Melds { get; }
    }
}
