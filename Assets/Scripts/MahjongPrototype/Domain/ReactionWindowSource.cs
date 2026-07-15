using System;

namespace MahjongPrototype.Domain
{
    public enum ReactionWindowSourceKind
    {
        Discard = 1,
        Kakan = 2
    }

    /// <summary>
    /// The event to which a reaction window responds.  Kakan is intentionally not
    /// modelled as a discard: it has no river record or discard id.
    /// </summary>
    public readonly struct ReactionWindowSource
    {
        private ReactionWindowSource(
            ReactionWindowSourceKind kind,
            SeatId actorSeat,
            Tile tile,
            int turnIndex,
            DiscardRecord? discard)
        {
            Kind = kind;
            ActorSeat = actorSeat;
            Tile = tile;
            TurnIndex = turnIndex;
            Discard = discard;
        }

        public ReactionWindowSourceKind Kind { get; }
        public SeatId ActorSeat { get; }
        public Tile Tile { get; }
        public int TurnIndex { get; }
        public DiscardRecord? Discard { get; }
        public bool IsDiscard => Kind == ReactionWindowSourceKind.Discard;
        public bool IsKakan => Kind == ReactionWindowSourceKind.Kakan;

        public static ReactionWindowSource FromDiscard(DiscardRecord discard)
        {
            return new ReactionWindowSource(
                ReactionWindowSourceKind.Discard,
                discard.ActorSeat,
                discard.Tile,
                discard.TurnIndex,
                discard);
        }

        public static ReactionWindowSource FromKakan(
            SeatId actorSeat,
            Tile tile,
            int turnIndex)
        {
            if (!tile.IsValid)
                throw new ArgumentException("A kakan reaction tile must be valid.", nameof(tile));

            return new ReactionWindowSource(
                ReactionWindowSourceKind.Kakan,
                actorSeat,
                tile,
                turnIndex,
                null);
        }
    }
}
