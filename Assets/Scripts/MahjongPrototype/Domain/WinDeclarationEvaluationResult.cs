namespace MahjongPrototype.Domain
{
    public sealed class WinDeclarationEvaluationResult
    {
        public WinDeclarationEvaluationResult(
            WinCheckResult winCheckResult,
            HandEvaluationResult handEvaluationResult)
            : this(
                winCheckResult,
                handEvaluationResult,
                WinningHandAnalysisResult.NotWin)
        {
        }

        public WinDeclarationEvaluationResult(
            WinCheckResult winCheckResult,
            HandEvaluationResult handEvaluationResult,
            WinningHandAnalysisResult winningHandAnalysis)
        {
            WinCheckResult = winCheckResult;
            HandEvaluationResult = handEvaluationResult ?? HandEvaluationResult.Empty;
            WinningHandAnalysis = winningHandAnalysis ?? WinningHandAnalysisResult.NotWin;
            IsWinningShape = winCheckResult.CanWin;
            HasYaku = HandEvaluationResult.HasYaku;
            CanDeclareWin = IsWinningShape && HasYaku;
        }

        public bool IsWinningShape { get; }
        public bool HasYaku { get; }
        public bool CanDeclareWin { get; }
        public WinCheckResult WinCheckResult { get; }
        public HandEvaluationResult HandEvaluationResult { get; }
        public WinningHandAnalysisResult WinningHandAnalysis { get; }

        public static WinDeclarationEvaluationResult NotWinningShape(WinCheckResult winCheckResult)
        {
            return NotWinningShape(winCheckResult, WinningHandAnalysisResult.NotWin);
        }

        public static WinDeclarationEvaluationResult NotWinningShape(
            WinCheckResult winCheckResult,
            WinningHandAnalysisResult winningHandAnalysis)
        {
            return new WinDeclarationEvaluationResult(
                winCheckResult,
                HandEvaluationResult.Empty,
                winningHandAnalysis);
        }
    }
}
