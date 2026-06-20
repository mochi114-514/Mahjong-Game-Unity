namespace MahjongPrototype.Domain
{
    public readonly struct DiscardRecord
    {
        public DiscardRecord(SeatId actorSeat, Tile tile, int turnIndex)
        {
            ActorSeat = actorSeat;
            Tile = tile;
            TurnIndex = turnIndex;
        }

        public SeatId ActorSeat { get; }
        public Tile Tile { get; }
        public int TurnIndex { get; }

        public override string ToString()
        {
            return $"{ActorSeat}:{Tile}@{TurnIndex}";
        }
    }
}
