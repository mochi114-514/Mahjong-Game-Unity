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
        {
            ActorSeat = actorSeat;
            Tile = tile;
            TurnIndex = turnIndex;
            Source = source;
            IsLastLiveWallDiscard = isLastLiveWallDiscard;
        }

        public SeatId ActorSeat { get; }
        public Tile Tile { get; }
        public int TurnIndex { get; }
        public DiscardSource Source { get; }
        public bool IsLastLiveWallDiscard { get; }

        public override string ToString()
        {
            return $"{ActorSeat}:{Tile}@{TurnIndex}";
        }
    }
}
