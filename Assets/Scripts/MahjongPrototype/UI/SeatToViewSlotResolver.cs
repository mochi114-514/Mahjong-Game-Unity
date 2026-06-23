using MahjongPrototype.Domain;

namespace MahjongPrototype.UI
{
    public static class SeatToViewSlotResolver
    {
        public static ViewSlot Resolve(SeatId selfSeat, SeatId targetSeat)
        {
            int diff = ((int)targetSeat - (int)selfSeat + 4) % 4;
            switch (diff)
            {
                case 0:
                    return ViewSlot.SelfBottom;
                case 1:
                    return ViewSlot.NextLeft;
                case 2:
                    return ViewSlot.AcrossTop;
                case 3:
                default:
                    return ViewSlot.PreviousRight;
            }
        }
    }
}
