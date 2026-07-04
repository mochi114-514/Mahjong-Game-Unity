namespace MahjongPrototype.Domain
{
    public sealed class FuritenSeatEvaluationResult
    {
        private FuritenSeatEvaluationResult(
            SeatId seat,
            bool isEvaluated,
            bool isTenpai,
            bool isDiscardFuriten,
            bool isTemporaryFuriten,
            bool isReachPassFuriten)
        {
            Seat = seat;
            IsEvaluated = isEvaluated;
            IsTenpai = isEvaluated && isTenpai;
            IsDiscardFuriten = IsTenpai && isDiscardFuriten;
            IsTemporaryFuriten = IsTenpai && isTemporaryFuriten;
            IsReachPassFuriten = IsTenpai && isReachPassFuriten;
        }

        public SeatId Seat { get; }
        public bool IsEvaluated { get; }
        public bool IsTenpai { get; }
        public bool IsDiscardFuriten { get; }
        public bool IsTemporaryFuriten { get; }
        public bool IsReachPassFuriten { get; }
        public bool IsFuriten =>
            IsDiscardFuriten ||
            IsTemporaryFuriten ||
            IsReachPassFuriten;

        public static FuritenSeatEvaluationResult NotEvaluated(SeatId seat)
        {
            return new FuritenSeatEvaluationResult(seat, false, false, false, false, false);
        }

        public static FuritenSeatEvaluationResult Evaluated(
            SeatId seat,
            bool isTenpai,
            bool isDiscardFuriten)
        {
            return Evaluated(seat, isTenpai, isDiscardFuriten, false, false);
        }

        public static FuritenSeatEvaluationResult Evaluated(
            SeatId seat,
            bool isTenpai,
            bool isDiscardFuriten,
            bool isTemporaryFuriten,
            bool isReachPassFuriten)
        {
            return new FuritenSeatEvaluationResult(
                seat,
                true,
                isTenpai,
                isDiscardFuriten,
                isTemporaryFuriten,
                isReachPassFuriten);
        }
    }
}
