using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public enum PlayerMeldType
    {
        Chi = 1,
        Pon = 2,
        Daiminkan = 3,
        Ankan = 4,
        Kakan = 5
    }

    /// <summary>
    /// A meld retained as part of a player's round state.  Physical tiles describe
    /// the owned tiles, while structural tiles describe the single three-tile group
    /// used by winning-hand analysis.
    /// </summary>
    public sealed class PlayerMeld
    {
        private readonly IReadOnlyList<Tile> physicalTiles;
        private readonly IReadOnlyList<Tile> structuralTiles;

        private PlayerMeld(
            PlayerMeldType type,
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat,
            SeatId? sourceSeat,
            Tile? acquiredTile,
            int? sourceDiscardId)
        {
            List<Tile> copiedTiles = CopyAndSortTiles(tiles);
            Validate(type, copiedTiles, ownerSeat, sourceSeat, acquiredTile, sourceDiscardId);

            Type = type;
            OwnerSeat = ownerSeat;
            SourceSeat = sourceSeat;
            AcquiredTile = acquiredTile;
            SourceDiscardId = sourceDiscardId;
            IsOpen = type != PlayerMeldType.Ankan;
            physicalTiles = copiedTiles.AsReadOnly();
            structuralTiles = CreateStructuralTiles(copiedTiles).AsReadOnly();
        }

        public PlayerMeldType Type { get; }
        public IReadOnlyList<Tile> PhysicalTiles => physicalTiles;
        public IReadOnlyList<Tile> StructuralTiles => structuralTiles;
        public SeatId OwnerSeat { get; }
        public SeatId? SourceSeat { get; }
        public Tile? AcquiredTile { get; }
        public int? SourceDiscardId { get; }
        public bool IsOpen { get; }
        public bool PreservesClosedHand => !IsOpen;
        public bool HasDiscardSource =>
            SourceSeat.HasValue && AcquiredTile.HasValue && SourceDiscardId.HasValue;
        public bool IsKan =>
            Type == PlayerMeldType.Daiminkan ||
            Type == PlayerMeldType.Ankan ||
            Type == PlayerMeldType.Kakan;
        public int PhysicalTileCount => physicalTiles.Count;
        public int StructuralTileCount => structuralTiles.Count;
        public int StructuralMeldCount => 1;
        public MeldType StructuralType => Type == PlayerMeldType.Chi
            ? MeldType.Sequence
            : MeldType.Triplet;

        public static PlayerMeld CreateChi(
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat,
            SeatId sourceSeat,
            Tile acquiredTile,
            int sourceDiscardId)
        {
            return new PlayerMeld(
                PlayerMeldType.Chi,
                tiles,
                ownerSeat,
                sourceSeat,
                acquiredTile,
                sourceDiscardId);
        }

        public static PlayerMeld CreatePon(
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat,
            SeatId sourceSeat,
            Tile acquiredTile,
            int sourceDiscardId)
        {
            return new PlayerMeld(
                PlayerMeldType.Pon,
                tiles,
                ownerSeat,
                sourceSeat,
                acquiredTile,
                sourceDiscardId);
        }

        public static PlayerMeld CreateDaiminkan(
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat,
            SeatId sourceSeat,
            Tile acquiredTile,
            int sourceDiscardId)
        {
            return new PlayerMeld(
                PlayerMeldType.Daiminkan,
                tiles,
                ownerSeat,
                sourceSeat,
                acquiredTile,
                sourceDiscardId);
        }

        public static PlayerMeld CreateAnkan(
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat)
        {
            return new PlayerMeld(
                PlayerMeldType.Ankan,
                tiles,
                ownerSeat,
                null,
                null,
                null);
        }

        public static PlayerMeld CreateKakan(
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat,
            SeatId sourceSeat,
            Tile acquiredTile,
            int sourceDiscardId)
        {
            return new PlayerMeld(
                PlayerMeldType.Kakan,
                tiles,
                ownerSeat,
                sourceSeat,
                acquiredTile,
                sourceDiscardId);
        }

        private static List<Tile> CopyAndSortTiles(IReadOnlyList<Tile> tiles)
        {
            if (tiles == null)
                throw new ArgumentNullException(nameof(tiles));

            List<Tile> copiedTiles = new List<Tile>(tiles.Count);
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!tiles[i].IsValid)
                    throw new ArgumentException("Meld tiles must be valid.", nameof(tiles));

                copiedTiles.Add(tiles[i]);
            }

            copiedTiles.Sort((left, right) => left.TypeIndex.CompareTo(right.TypeIndex));
            return copiedTiles;
        }

        private static List<Tile> CreateStructuralTiles(IReadOnlyList<Tile> physicalTiles)
        {
            return new List<Tile>
            {
                physicalTiles[0],
                physicalTiles[1],
                physicalTiles[2]
            };
        }

        private static void Validate(
            PlayerMeldType type,
            IReadOnlyList<Tile> tiles,
            SeatId ownerSeat,
            SeatId? sourceSeat,
            Tile? acquiredTile,
            int? sourceDiscardId)
        {
            bool isDiscardDerived = type != PlayerMeldType.Ankan;
            ValidateSource(
                isDiscardDerived,
                ownerSeat,
                sourceSeat,
                acquiredTile,
                sourceDiscardId);

            switch (type)
            {
                case PlayerMeldType.Chi:
                    ValidateChiTiles(tiles, acquiredTile.Value);
                    return;
                case PlayerMeldType.Pon:
                    ValidateIdenticalTiles(tiles, 3, acquiredTile.Value);
                    return;
                case PlayerMeldType.Daiminkan:
                case PlayerMeldType.Kakan:
                    ValidateIdenticalTiles(tiles, 4, acquiredTile.Value);
                    return;
                case PlayerMeldType.Ankan:
                    ValidateIdenticalTiles(tiles, 4, null);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static void ValidateSource(
            bool isDiscardDerived,
            SeatId ownerSeat,
            SeatId? sourceSeat,
            Tile? acquiredTile,
            int? sourceDiscardId)
        {
            if (!isDiscardDerived)
            {
                if (sourceSeat.HasValue || acquiredTile.HasValue || sourceDiscardId.HasValue)
                {
                    throw new ArgumentException(
                        "A concealed kan cannot reference a called discard.");
                }

                return;
            }

            if (!sourceSeat.HasValue || sourceSeat.Value == ownerSeat ||
                !acquiredTile.HasValue || !acquiredTile.Value.IsValid ||
                !sourceDiscardId.HasValue || sourceDiscardId.Value <= 0)
            {
                throw new ArgumentException(
                    "A discard-derived meld requires a different source seat, an acquired tile, and a positive discard ID.");
            }
        }

        private static void ValidateChiTiles(IReadOnlyList<Tile> tiles, Tile acquiredTile)
        {
            if (tiles.Count != 3)
                throw new ArgumentException("A chi must contain exactly three physical tiles.", nameof(tiles));

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

            bool containsAcquiredTile = false;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == acquiredTile)
                    containsAcquiredTile = true;
            }

            if (!containsAcquiredTile)
                throw new ArgumentException("Chi tiles must include the acquired tile.", nameof(tiles));
        }

        private static void ValidateIdenticalTiles(
            IReadOnlyList<Tile> tiles,
            int expectedCount,
            Tile? acquiredTile)
        {
            if (tiles.Count != expectedCount)
            {
                throw new ArgumentException(
                    $"This meld must contain exactly {expectedCount} physical tiles.",
                    nameof(tiles));
            }

            Tile expectedTile = tiles[0];
            for (int i = 1; i < tiles.Count; i++)
            {
                if (tiles[i] != expectedTile)
                    throw new ArgumentException("Triplet and kan tiles must be identical.", nameof(tiles));
            }

            if (acquiredTile.HasValue && acquiredTile.Value != expectedTile)
                throw new ArgumentException("The acquired tile must match the meld tiles.", nameof(acquiredTile));
        }
    }

    public readonly struct DiscardClaim
    {
        public DiscardClaim(PlayerMeld meld)
        {
            Meld = meld ?? throw new ArgumentNullException(nameof(meld));
            if (!meld.HasDiscardSource)
                throw new ArgumentException("A discard claim requires a discard-derived meld.", nameof(meld));

            DiscardId = meld.SourceDiscardId.Value;
            ClaimingSeat = meld.OwnerSeat;
        }

        public int DiscardId { get; }
        public SeatId ClaimingSeat { get; }
        public PlayerMeld Meld { get; }
    }
}
