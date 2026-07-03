namespace MahjongPrototype.Domain
{
    public sealed class FuritenSeatEvaluationResult
    {
        private FuritenSeatEvaluationResult(
            SeatId seat,
            bool isEvaluated,
            bool isTenpai,
            bool isDiscardFuriten)
        {
            Seat = seat;
            IsEvaluated = isEvaluated;
            IsTenpai = isEvaluated && isTenpai;
            IsDiscardFuriten = IsTenpai && isDiscardFuriten;
        }

        public SeatId Seat { get; }
        public bool IsEvaluated { get; }
        public bool IsTenpai { get; }
        public bool IsDiscardFuriten { get; }
        public bool IsFuriten => IsDiscardFuriten;

        public static FuritenSeatEvaluationResult NotEvaluated(SeatId seat)
        {
            return new FuritenSeatEvaluationResult(seat, false, false, false);
        }

        public static FuritenSeatEvaluationResult Evaluated(
            SeatId seat,
            bool isTenpai,
            bool isDiscardFuriten)
        {
            return new FuritenSeatEvaluationResult(
                seat,
                true,
                isTenpai,
                isDiscardFuriten);
        }
    }
}
