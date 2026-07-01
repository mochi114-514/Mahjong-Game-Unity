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

            WinCheckResult winCheckResult = winChecker.CheckWinWithTile(
                context.HandTiles,
                context.WinningTile);
            if (!winCheckResult.CanWin)
                return WinDeclarationEvaluationResult.NotWinningShape(winCheckResult);

            HandEvaluationContext handContext = new HandEvaluationContext(
                context.HandTiles,
                context.WinningTile,
                context.WinType,
                winCheckResult.Shape,
                context.WinnerSeat,
                context.SourceSeat,
                context.RoundWind,
                context.SeatWind,
                context.IsReachDeclared,
                context.IsClosed);
            HandEvaluationResult handEvaluationResult = handEvaluator.Evaluate(handContext);
            return new WinDeclarationEvaluationResult(winCheckResult, handEvaluationResult);
        }
    }
}
