namespace MahjongPrototype.Domain
{
    public sealed class WinDeclarationEvaluationResult
    {
        public WinDeclarationEvaluationResult(
            WinCheckResult winCheckResult,
            HandEvaluationResult handEvaluationResult)
        {
            WinCheckResult = winCheckResult;
            HandEvaluationResult = handEvaluationResult ?? HandEvaluationResult.Empty;
            IsWinningShape = winCheckResult.CanWin;
            HasYaku = HandEvaluationResult.HasYaku;
            CanDeclareWin = IsWinningShape && HasYaku;
        }

        public bool IsWinningShape { get; }
        public bool HasYaku { get; }
        public bool CanDeclareWin { get; }
        public WinCheckResult WinCheckResult { get; }
        public HandEvaluationResult HandEvaluationResult { get; }

        public static WinDeclarationEvaluationResult NotWinningShape(WinCheckResult winCheckResult)
        {
            return new WinDeclarationEvaluationResult(winCheckResult, HandEvaluationResult.Empty);
        }
    }
}
