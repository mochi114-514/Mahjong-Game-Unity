using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class Hand
    {
        private readonly List<Tile> tiles = new List<Tile>();

        public int Count => tiles.Count;

        public void Add(Tile tile)
        {
            tiles.Add(tile);
        }

        public bool TryRemoveAt(int index, out Tile tile)
        {
            if (index < 0 || index >= tiles.Count)
            {
                tile = default;
                return false;
            }

            tile = tiles[index];
            tiles.RemoveAt(index);
            return true;
        }

        public IReadOnlyList<Tile> GetTiles()
        {
            return tiles.ToArray();
        }

        public string ToDisplayString()
        {
            return string.Join(" ", Array.ConvertAll(tiles.ToArray(), tile => tile.ToString()));
        }
    }
}
