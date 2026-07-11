using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum OpenMeldType
    {
        Pon
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
            if (type != OpenMeldType.Pon)
                throw new ArgumentOutOfRangeException(nameof(type));
            if (tiles == null || tiles.Count != 3)
                throw new ArgumentException("A pon must contain exactly three tiles.", nameof(tiles));
            if (!calledTile.IsValid || sourceDiscardId <= 0)
                throw new ArgumentException("A pon must reference a valid called discard.");

            List<Tile> copiedTiles = new List<Tile>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!tiles[i].IsValid || tiles[i] != calledTile)
                    throw new ArgumentException("All pon tiles must equal the called tile.", nameof(tiles));

                copiedTiles.Add(tiles[i]);
            }

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
