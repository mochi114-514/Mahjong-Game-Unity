namespace MahjongPrototype.Domain
{
    public sealed class NoYakuTenpaiEvaluationResult
    {
        public static NoYakuTenpaiEvaluationResult NotEvaluated { get; } =
            new NoYakuTenpaiEvaluationResult(false, false, false);

        public static NoYakuTenpaiEvaluationResult NotTenpai { get; } =
            new NoYakuTenpaiEvaluationResult(true, false, false);

        public NoYakuTenpaiEvaluationResult(
            bool isEvaluated,
            bool isTenpai,
            bool hasAnyYakuWait)
        {
            IsEvaluated = isEvaluated;
            IsTenpai = isTenpai;
            HasAnyYakuWait = hasAnyYakuWait;
        }

        public bool IsEvaluated { get; }
        public bool IsTenpai { get; }
        public bool HasAnyYakuWait { get; }
        public bool ShouldShowZeroHanTenpai => IsEvaluated && IsTenpai && !HasAnyYakuWait;

        public static NoYakuTenpaiEvaluationResult Tenpai(bool hasAnyYakuWait)
        {
            return new NoYakuTenpaiEvaluationResult(true, true, hasAnyYakuWait);
        }
    }
}
