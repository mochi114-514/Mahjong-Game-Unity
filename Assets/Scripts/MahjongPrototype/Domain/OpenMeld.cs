using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum OpenMeldType
    {
        Pon,
        Chi
    }

    /// <summary>
    /// A called meld that remains part of a player's round state.  This is deliberately
    /// separate from <see cref="HandMeld"/>, which represents one interpretation of a hand.
    /// </summary>
    public sealed class OpenMeld
    {
        public OpenMeld(
            OpenMeldType type,
            IReadOnlyList<Tile> tiles,
            SeatId callerSeat,
            SeatId sourceSeat,
            Tile calledTile,
            int sourceDiscardId)
        {
            if (type != OpenMeldType.Pon && type != OpenMeldType.Chi)
                throw new ArgumentOutOfRangeException(nameof(type));
            if (tiles == null || tiles.Count != 3)
                throw new ArgumentException("An open meld must contain exactly three tiles.", nameof(tiles));
            if (!calledTile.IsValid || sourceDiscardId <= 0)
                throw new ArgumentException("An open meld must reference a valid called discard.");

            List<Tile> copiedTiles = new List<Tile>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!tiles[i].IsValid)
                    throw new ArgumentException("Open meld tiles must be valid.", nameof(tiles));

                copiedTiles.Add(tiles[i]);
            }

            if (type == OpenMeldType.Pon)
                ValidatePonTiles(copiedTiles, calledTile);
            else
                ValidateChiTiles(copiedTiles, calledTile);

            Type = type;
            Tiles = copiedTiles.AsReadOnly();
            CallerSeat = callerSeat;
            SourceSeat = sourceSeat;
            CalledTile = calledTile;
            SourceDiscardId = sourceDiscardId;
        }

        public OpenMeldType Type { get; }
        public IReadOnlyList<Tile> Tiles { get; }
        public SeatId CallerSeat { get; }
        public SeatId SourceSeat { get; }
        public Tile CalledTile { get; }
        public int SourceDiscardId { get; }

        private static void ValidatePonTiles(IReadOnlyList<Tile> tiles, Tile calledTile)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != calledTile)
                    throw new ArgumentException("All pon tiles must equal the called tile.", nameof(tiles));
            }
        }

        private static void ValidateChiTiles(List<Tile> tiles, Tile calledTile)
        {
            if (!calledTile.IsNumberTile)
                throw new ArgumentException("A chi must call a number tile.", nameof(calledTile));

            tiles.Sort((left, right) => left.TypeIndex.CompareTo(right.TypeIndex));
            Tile first = tiles[0];
            Tile second = tiles[1];
            Tile third = tiles[2];
            if (!first.IsNumberTile || !second.IsNumberTile || !third.IsNumberTile ||
                first.Suit != second.Suit || first.Suit != third.Suit ||
                second.Rank != first.Rank + 1 || third.Rank != first.Rank + 2)
            {
                throw new ArgumentException(
                    "Chi tiles must form a same-suit consecutive sequence.",
                    nameof(tiles));
            }

            bool containsCalledTile = false;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == calledTile)
                {
                    containsCalledTile = true;
                    break;
                }
            }

            if (!containsCalledTile)
                throw new ArgumentException("Chi tiles must include the called tile.", nameof(tiles));
        }
    }

    public readonly struct DiscardClaim
    {
        public DiscardClaim(int discardId, SeatId callerSeat, OpenMeld openMeld)
        {
            DiscardId = discardId;
            CallerSeat = callerSeat;
            OpenMeld = openMeld ?? throw new ArgumentNullException(nameof(openMeld));
        }

        public int DiscardId { get; }
        public SeatId CallerSeat { get; }
        public OpenMeld OpenMeld { get; }
    }
}
