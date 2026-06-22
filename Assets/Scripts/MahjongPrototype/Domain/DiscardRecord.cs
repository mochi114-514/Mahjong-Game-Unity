namespace MahjongPrototype.Domain
{
    public readonly struct DiscardRecord
    {
        public DiscardRecord(SeatId actorSeat, Tile tile, int turnIndex)
            : this(actorSeat, tile, turnIndex, DiscardSource.Hand)
        {
        }

        public DiscardRecord(SeatId actorSeat, Tile tile, int turnIndex, DiscardSource source)
        {
            ActorSeat = actorSeat;
            Tile = tile;
            TurnIndex = turnIndex;
            Source = source;
        }

        public SeatId ActorSeat { get; }
        public Tile Tile { get; }
        public int TurnIndex { get; }
        public DiscardSource Source { get; }

        public override string ToString()
        {
            return $"{ActorSeat}:{Tile}@{TurnIndex}";
        }
    }
}
