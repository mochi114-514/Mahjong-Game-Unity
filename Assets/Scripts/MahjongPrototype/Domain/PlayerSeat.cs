namespace MahjongPrototype.Domain
{
    public sealed class PlayerSeat
    {
        public PlayerSeat(SeatId seatId)
        {
            SeatId = seatId;
            Hand = new Hand();
        }

        public SeatId SeatId { get; }
        public Hand Hand { get; }
    }
}
