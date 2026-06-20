using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class Wall
    {
        private readonly List<Tile> tiles;

        private Wall(List<Tile> tiles)
        {
            this.tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
        }

        public int Count => tiles.Count;

        public static Wall CreateStandardShuffled(int? seed = null)
        {
            List<Tile> generated = new List<Tile>(136);

            AddSuit(generated, TileSuit.Man);
            AddSuit(generated, TileSuit.Pin);
            AddSuit(generated, TileSuit.Sou);
            AddHonors(generated);

            Shuffle(generated, seed.HasValue ? new Random(seed.Value) : new Random());
            return new Wall(generated);
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

        public IReadOnlyList<Tile> GetSnapshot()
        {
            return tiles.ToArray();
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
