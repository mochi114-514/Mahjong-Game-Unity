using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinDeclarationEvaluator
    {
        private readonly WinChecker winChecker;
        private readonly HandEvaluator handEvaluator;

        public WinDeclarationEvaluator(WinChecker winChecker, HandEvaluator handEvaluator)
        {
            this.winChecker = winChecker ?? throw new ArgumentNullException(nameof(winChecker));
            this.handEvaluator = handEvaluator ?? throw new ArgumentNullException(nameof(handEvaluator));
        }

        public WinDeclarationEvaluationResult EvaluateWithTile(
            WinDeclarationEvaluationContext context)
        {
            if (context == null || !context.WinningTile.IsValid)
                return WinDeclarationEvaluationResult.NotWinningShape(WinCheckResult.NotWin);

            WinningHandAnalysisResult analysis = winChecker.AnalyzeWithTileDetailed(
                context.HandTiles,
                context.WinningTile);
            WinCheckResult winCheckResult = WinChecker.ToWinCheckResult(analysis);
            if (!winCheckResult.CanWin)
            {
                return WinDeclarationEvaluationResult.NotWinningShape(
                    winCheckResult,
                    analysis);
            }

            HandEvaluationContext handContext = new HandEvaluationContext(
                context.HandTiles,
                context.WinningTile,
                context.WinType,
                winCheckResult.Shape,
                analysis,
                context.WinnerSeat,
                context.SourceSeat,
                context.RoundWind,
                context.SeatWind,
                context.IsReachDeclared,
                context.IsClosed,
                context.IsIppatsuEligible);
            HandEvaluationResult handEvaluationResult = handEvaluator.Evaluate(handContext);
            return new WinDeclarationEvaluationResult(
                winCheckResult,
                handEvaluationResult,
                analysis);
        }
    }
}
