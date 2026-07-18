namespace MahjongPrototype.Domain
{
    public readonly struct DiscardRecord
    {
        public DiscardRecord(SeatId actorSeat, Tile tile, int turnIndex)
            : this(actorSeat, tile, turnIndex, DiscardSource.Hand)
        {
        }

        public DiscardRecord(SeatId actorSeat, Tile tile, int turnIndex, DiscardSource source)
            : this(actorSeat, tile, turnIndex, source, false)
        {
        }

        public DiscardRecord(
            SeatId actorSeat,
            Tile tile,
            int turnIndex,
            DiscardSource source,
            bool isLastLiveWallDiscard)
            : this(
                0,
                actorSeat,
                tile,
                turnIndex,
                source,
                isLastLiveWallDiscard)
        {
        }

        public DiscardRecord(
            int id,
            SeatId actorSeat,
            Tile tile,
            int turnIndex,
            DiscardSource source,
            bool isLastLiveWallDiscard)
        {
            Id = id;
            ActorSeat = actorSeat;
            Tile = tile;
            TurnIndex = turnIndex;
            Source = source;
            IsLastLiveWallDiscard = isLastLiveWallDiscard;
        }

        public int Id { get; }
        public SeatId ActorSeat { get; }
        public Tile Tile { get; }
        public int TurnIndex { get; }
        public DiscardSource Source { get; }
        public bool IsLastLiveWallDiscard { get; }

        internal DiscardRecord WithId(int id)
        {
            return new DiscardRecord(
                id,
                ActorSeat,
                Tile,
                TurnIndex,
                Source,
                IsLastLiveWallDiscard);
        }

        public override string ToString()
        {
            return $"#{Id}:{ActorSeat}:{Tile}@{TurnIndex}";
        }
    }
}
