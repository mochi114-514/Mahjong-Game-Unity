using System;

namespace MahjongPrototype.Domain
{
    public sealed class PlayerSeat
    {
        private Tile? drawnTile;

        public PlayerSeat(SeatId seatId)
        {
            SeatId = seatId;
            Hand = new Hand();
        }

        public SeatId SeatId { get; }
        public Hand Hand { get; }
        public bool HasDrawnTile => drawnTile.HasValue;
        public Tile? DrawnTile => drawnTile;

        public void SetDrawnTile(Tile tile)
        {
            if (!tile.IsValid)
                throw new ArgumentException("Drawn tile must be valid.", nameof(tile));

            if (drawnTile.HasValue)
                throw new InvalidOperationException("Drawn tile already exists.");

            drawnTile = tile;
        }

        public bool TryTakeDrawnTile(out Tile tile)
        {
            if (!drawnTile.HasValue)
            {
                tile = default;
                return false;
            }

            tile = drawnTile.Value;
            drawnTile = null;
            return true;
        }

        public bool CommitDrawnTileToHand()
        {
            if (!TryTakeDrawnTile(out Tile tile))
                return false;

            Hand.Add(tile);
            return true;
        }

        public void ClearDrawnTile()
        {
            drawnTile = null;
        }
    }
}
