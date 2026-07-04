using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class HandMeld
    {
        public HandMeld(MeldType type, IReadOnlyList<Tile> tiles)
        {
            List<Tile> copiedTiles = CopyAndSortTiles(tiles);
            if (!IsValidMeld(type, copiedTiles))
                throw new ArgumentException("Tiles do not form the requested meld.", nameof(tiles));

            Type = type;
            Tiles = copiedTiles.AsReadOnly();
        }

        public MeldType Type { get; }
        public IReadOnlyList<Tile> Tiles { get; }

        private static List<Tile> CopyAndSortTiles(IReadOnlyList<Tile> tiles)
        {
            if (tiles == null)
                return new List<Tile>();

            List<Tile> copiedTiles = new List<Tile>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++)
                copiedTiles.Add(tiles[i]);

            copiedTiles.Sort(CompareByTypeIndex);
            return copiedTiles;
        }

        private static bool IsValidMeld(MeldType type, IReadOnlyList<Tile> tiles)
        {
            if (tiles == null || tiles.Count != 3)
                return false;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (!tiles[i].IsValid)
                    return false;
            }

            switch (type)
            {
                case MeldType.Sequence:
                    return IsValidSequence(tiles);
                case MeldType.Triplet:
                    return IsValidTriplet(tiles);
                default:
                    return false;
            }
        }

        private static bool IsValidSequence(IReadOnlyList<Tile> tiles)
        {
            Tile first = tiles[0];
            Tile second = tiles[1];
            Tile third = tiles[2];

            return first.IsNumberTile &&
                   second.IsNumberTile &&
                   third.IsNumberTile &&
                   first.Suit == second.Suit &&
                   first.Suit == third.Suit &&
                   second.Rank == first.Rank + 1 &&
                   third.Rank == first.Rank + 2;
        }

        private static bool IsValidTriplet(IReadOnlyList<Tile> tiles)
        {
            return tiles[0] == tiles[1] && tiles[0] == tiles[2];
        }

        private static int CompareByTypeIndex(Tile left, Tile right)
        {
            return left.TypeIndex.CompareTo(right.TypeIndex);
        }
    }
}
