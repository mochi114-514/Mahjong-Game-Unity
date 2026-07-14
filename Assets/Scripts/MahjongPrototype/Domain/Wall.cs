using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class Wall
    {
        private const int DeadWallTileCount = 14;
        private const int RinshanTileCount = 4;

        private readonly List<Tile> tiles;
        private readonly List<Tile> deadWallTiles;
        private int remainingRinshanTileCount;

        private Wall(List<Tile> tiles, List<Tile> deadWallTiles)
        {
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            this.deadWallTiles = deadWallTiles ?? throw new ArgumentNullException(nameof(deadWallTiles));
            if (deadWallTiles.Count != DeadWallTileCount)
                throw new ArgumentException("The dead wall must contain exactly 14 tiles.", nameof(deadWallTiles));

            remainingRinshanTileCount = RinshanTileCount;
        }

        public int Count => tiles.Count;
        public int DeadWallCount => deadWallTiles.Count;
        public int RemainingRinshanTileCount => remainingRinshanTileCount;
        public bool CanDrawRinshan => remainingRinshanTileCount > 0 && tiles.Count > 0;

        public static Wall CreateStandardShuffled(int? seed = null)
        {
            List<Tile> generated = new List<Tile>(136);

            AddSuit(generated, TileSuit.Man);
            AddSuit(generated, TileSuit.Pin);
            AddSuit(generated, TileSuit.Sou);
            AddHonors(generated);

            Shuffle(generated, seed.HasValue ? new Random(seed.Value) : new Random());
            List<Tile> deadWall = generated.GetRange(0, DeadWallTileCount);
            generated.RemoveRange(0, DeadWallTileCount);
            return new Wall(generated, deadWall);
        }

        public bool Contains(Tile tile)
        {
            return tiles.Contains(tile);
        }

        public bool TryTakeSpecific(Tile targetTile, out Tile tile)
        {
            int index = tiles.IndexOf(targetTile);
            if (index < 0)
            {
                tile = default;
                return false;
            }

            tile = tiles[index];
            tiles.RemoveAt(index);
            return true;
        }

        public bool TryTakeNext(out Tile tile)
        {
            if (tiles.Count <= 0)
            {
                tile = default;
                return false;
            }

            int lastIndex = tiles.Count - 1;
            tile = tiles[lastIndex];
            tiles.RemoveAt(lastIndex);
            return true;
        }

        public bool TryTakeRinshan(out Tile tile)
        {
            if (!CanDrawRinshan)
            {
                tile = default;
                return false;
            }

            tile = deadWallTiles[0];
            deadWallTiles.RemoveAt(0);

            int replacementIndex = tiles.Count - 1;
            Tile replacementTile = tiles[replacementIndex];
            tiles.RemoveAt(replacementIndex);
            deadWallTiles.Add(replacementTile);
            remainingRinshanTileCount--;
            return true;
        }

        public IReadOnlyList<Tile> GetSnapshot()
        {
            return tiles.ToArray();
        }

        public IReadOnlyList<Tile> GetDeadWallSnapshot()
        {
            return deadWallTiles.ToArray();
        }

        public IReadOnlyList<Tile> GetRinshanSnapshot()
        {
            Tile[] snapshot = new Tile[remainingRinshanTileCount];
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i] = deadWallTiles[i];

            return snapshot;
        }

        private static void AddSuit(List<Tile> target, TileSuit suit)
        {
            for (int number = 1; number <= 9; number++)
            {
                Tile tile = Tile.CreateNumber(suit, number);
                AddFourCopies(target, tile);
            }
        }

        private static void AddHonors(List<Tile> target)
        {
            AddFourCopies(target, Tile.CreateHonor(HonorKind.East));
            AddFourCopies(target, Tile.CreateHonor(HonorKind.South));
            AddFourCopies(target, Tile.CreateHonor(HonorKind.West));
            AddFourCopies(target, Tile.CreateHonor(HonorKind.North));
            AddFourCopies(target, Tile.CreateHonor(HonorKind.White));
            AddFourCopies(target, Tile.CreateHonor(HonorKind.Green));
            AddFourCopies(target, Tile.CreateHonor(HonorKind.Red));
        }

        private static void AddFourCopies(List<Tile> target, Tile tile)
        {
            for (int i = 0; i < 4; i++)
                target.Add(tile);
        }

        private static void Shuffle(List<Tile> target, Random random)
        {
            for (int i = target.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                Tile tmp = target[i];
                target[i] = target[swapIndex];
                target[swapIndex] = tmp;
            }
        }
    }
}
