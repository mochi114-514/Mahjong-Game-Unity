namespace MahjongPrototype.Domain
{
    public readonly struct TurnDrawRecord
    {
        public TurnDrawRecord(
            SeatId actorSeat,
            Tile tile,
            int turnIndex,
            bool isLastLiveWallDraw)
        {
            ActorSeat = actorSeat;
            Tile = tile;
            TurnIndex = turnIndex;
            IsLastLiveWallDraw = isLastLiveWallDraw;
        }

        public SeatId ActorSeat { get; }
        public Tile Tile { get; }
        public int TurnIndex { get; }
        public bool IsLastLiveWallDraw { get; }
    }
}
