using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class NoYakuTenpaiEvaluator
    {
        private readonly WinDeclarationEvaluator winDeclarationEvaluator;
        private readonly WinningTileWaitEnumerator waitEnumerator;

        public NoYakuTenpaiEvaluator(WinDeclarationEvaluator winDeclarationEvaluator)
        {
            this.winDeclarationEvaluator = winDeclarationEvaluator;
            waitEnumerator = new WinningTileWaitEnumerator();
        }

        public NoYakuTenpaiEvaluationResult Evaluate(
            IReadOnlyList<Tile> handTiles,
            SeatId winnerSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed)
        {
            return Evaluate(
                handTiles,
                winnerSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                null);
        }

        public NoYakuTenpaiEvaluationResult Evaluate(
            IReadOnlyList<Tile> handTiles,
            SeatId winnerSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed,
            IReadOnlyList<PlayerMeld> melds)
        {
            if (winDeclarationEvaluator == null)
                return NoYakuTenpaiEvaluationResult.NotEvaluated;

            if (!waitEnumerator.TryEnumerateWinningTiles(
                    handTiles,
                    melds,
                    out IReadOnlyList<Tile> winningTiles))
            {
                return NoYakuTenpaiEvaluationResult.NotTenpai;
            }

            bool hasWinningShapeWait = false;
            for (int i = 0; i < winningTiles.Count; i++)
            {
                Tile winningTile = winningTiles[i];
                WinDeclarationEvaluationResult result =
                    winDeclarationEvaluator.EvaluateWithTile(
                        new WinDeclarationEvaluationContext(
                            handTiles,
                            winningTile,
                            WinType.Ron,
                            winnerSeat,
                            null,
                            roundWind,
                            seatWind,
                            isReachDeclared,
                            isClosed,
                            false,
                            false,
                            false,
                            false,
                            false,
                            melds));

                if (!result.IsWinningShape)
                    continue;

                hasWinningShapeWait = true;
                if (result.HasYaku)
                    return NoYakuTenpaiEvaluationResult.Tenpai(true);
            }

            return hasWinningShapeWait
                ? NoYakuTenpaiEvaluationResult.Tenpai(false)
                : NoYakuTenpaiEvaluationResult.NotTenpai;
        }
    }
}
