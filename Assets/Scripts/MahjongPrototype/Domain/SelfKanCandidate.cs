using System;

namespace MahjongPrototype.Domain
{
    public enum SelfKanKind
    {
        Ankan = 1,
        Kakan = 2
    }

    public enum SelfKanTileLocation
    {
        Hand = 1,
        DrawnTile = 2
    }

    /// <summary>
    /// Snapshot of a self-turn kan choice.  It deliberately retains the source
    /// pon index and turn so a stale UI choice cannot be committed later.
    /// </summary>
    public sealed class SelfKanCandidate
    {
        public SelfKanCandidate(
            SelfKanKind kind,
            SeatId seat,
            Tile tile,
            SelfKanTileLocation addedTileLocation,
            int turnIndex,
            int sourcePonMeldIndex = -1,
            PlayerMeld sourcePon = null)
        {
            if (!tile.IsValid)
                throw new ArgumentException("A self-kan tile must be valid.", nameof(tile));
            if (kind == SelfKanKind.Kakan && sourcePonMeldIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourcePonMeldIndex),
                    "A kakan requires its source pon meld index.");
            }
            if (kind == SelfKanKind.Ankan && sourcePonMeldIndex >= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourcePonMeldIndex),
                    "Ankan must not reference a source pon meld.");
            }

            Kind = kind;
            Seat = seat;
            Tile = tile;
            AddedTileLocation = addedTileLocation;
            TurnIndex = turnIndex;
            SourcePonMeldIndex = sourcePonMeldIndex;
            SourcePon = sourcePon;
        }

        public SelfKanKind Kind { get; }
        public SeatId Seat { get; }
        public Tile Tile { get; }
        public SelfKanTileLocation AddedTileLocation { get; }
        public int TurnIndex { get; }
        public int SourcePonMeldIndex { get; }
        public PlayerMeld SourcePon { get; }

        public bool Matches(SelfKanCandidate other)
        {
            return other != null && Kind == other.Kind && Seat == other.Seat &&
                Tile == other.Tile && AddedTileLocation == other.AddedTileLocation &&
                TurnIndex == other.TurnIndex &&
                SourcePonMeldIndex == other.SourcePonMeldIndex;
        }
    }

    public sealed class SelfKanDecision
    {
        private readonly SelfKanCandidate[] candidates;

        public SelfKanDecision(SeatId seat, int turnIndex, SelfKanCandidate[] candidates)
        {
            Seat = seat;
            TurnIndex = turnIndex;
            this.candidates = candidates ?? Array.Empty<SelfKanCandidate>();
        }

        public SeatId Seat { get; }
        public int TurnIndex { get; }
        public System.Collections.Generic.IReadOnlyList<SelfKanCandidate> Candidates => candidates;
    }
}
