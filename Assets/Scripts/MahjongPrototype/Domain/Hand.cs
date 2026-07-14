using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class Hand
    {
        private readonly List<Tile> tiles = new List<Tile>();

        public int Count => tiles.Count;

        public int CountTilesByValue(Tile tile)
        {
            if (!tile.IsValid)
                return 0;

            int count = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == tile)
                    count++;
            }

            return count;
        }

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

        public bool TryRemoveTilesByValue(Tile tile, int requiredCount)
        {
            if (!tile.IsValid || requiredCount <= 0)
                return false;

            int matchingCount = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == tile)
                    matchingCount++;
            }

            if (matchingCount < requiredCount)
                return false;

            int removedCount = 0;
            for (int i = tiles.Count - 1; i >= 0 && removedCount < requiredCount; i--)
            {
                if (tiles[i] != tile)
                    continue;

                tiles.RemoveAt(i);
                removedCount++;
            }

            return true;
        }

        public bool TryRemoveTilesByValue(IReadOnlyList<Tile> requiredTiles)
        {
            if (!ContainsTilesByValue(requiredTiles))
                return false;

            for (int requiredIndex = 0; requiredIndex < requiredTiles.Count; requiredIndex++)
            {
                Tile requiredTile = requiredTiles[requiredIndex];
                for (int tileIndex = tiles.Count - 1; tileIndex >= 0; tileIndex--)
                {
                    if (tiles[tileIndex] != requiredTile)
                        continue;

                    tiles.RemoveAt(tileIndex);
                    break;
                }
            }

            return true;
        }

        public bool ContainsTilesByValue(IReadOnlyList<Tile> requiredTiles)
        {
            if (requiredTiles == null || requiredTiles.Count <= 0)
                return false;

            for (int requiredIndex = 0; requiredIndex < requiredTiles.Count; requiredIndex++)
            {
                Tile requiredTile = requiredTiles[requiredIndex];
                if (!requiredTile.IsValid)
                    return false;

                int requiredCount = 0;
                for (int i = 0; i < requiredTiles.Count; i++)
                {
                    if (requiredTiles[i] == requiredTile)
                        requiredCount++;
                }

                int matchingCount = 0;
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i] == requiredTile)
                        matchingCount++;
                }

                if (matchingCount < requiredCount)
                    return false;
            }

            return true;
        }

        public void SortByTypeIndex()
        {
            tiles.Sort(CompareByTypeIndex);
        }

        public IReadOnlyList<Tile> GetTiles()
        {
            return tiles.ToArray();
        }

        public string ToDisplayString()
        {
            return string.Join(" ", Array.ConvertAll(tiles.ToArray(), tile => tile.ToString()));
        }

        private static int CompareByTypeIndex(Tile left, Tile right)
        {
            return GetSortKey(left).CompareTo(GetSortKey(right));
        }

        private static int GetSortKey(Tile tile)
        {
            int typeIndex = tile.TypeIndex;
            return typeIndex < 0 ? int.MaxValue : typeIndex;
        }
    }
}
