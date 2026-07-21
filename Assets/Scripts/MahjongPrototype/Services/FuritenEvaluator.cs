using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class FuritenEvaluator
    {
        private const int TileTypeCount = 34;

        private readonly WinningTileWaitEnumerator waitEnumerator;

        public FuritenEvaluator()
            : this(new WinChecker())
        {
        }

        public FuritenEvaluator(WinChecker winChecker)
        {
            waitEnumerator = new WinningTileWaitEnumerator(winChecker);
        }

        public FuritenEvaluationResultSet EvaluateAll(MahjongGameState gameState)
        {
            if (gameState == null)
                return FuritenEvaluationResultSet.Empty;

            List<FuritenSeatEvaluationResult> results =
                new List<FuritenSeatEvaluationResult>();
            IReadOnlyList<SeatSlot> seatSlots = gameState.SeatSlots;

            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (!slot.HasPlayer)
                    continue;

                SeatId seat = slot.Wind;
                PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
                results.Add(EvaluateSeat(gameState, seat, playerSeat));
            }

            return new FuritenEvaluationResultSet(results);
        }

        private FuritenSeatEvaluationResult EvaluateSeat(
            MahjongGameState gameState,
            SeatId seat,
            PlayerSeat playerSeat)
        {
            if (playerSeat == null || playerSeat.HasDrawnTile)
                return FuritenSeatEvaluationResult.NotEvaluated(seat);

            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            if (!waitEnumerator.TryEnumerateWinningTiles(
                    handTiles,
                    playerSeat.Melds,
                    out IReadOnlyList<Tile> winningTiles))
            {
                return FuritenSeatEvaluationResult.NotEvaluated(seat);
            }

            bool[] selfDiscardedTypeIndices = BuildSelfDiscardedTypeIndices(
                gameState.Discards,
                seat);
            bool isTenpai = false;
            bool isDiscardFuriten = false;

            for (int i = 0; i < winningTiles.Count; i++)
            {
                isTenpai = true;
                if (selfDiscardedTypeIndices[winningTiles[i].TypeIndex])
                    isDiscardFuriten = true;
            }

            return FuritenSeatEvaluationResult.Evaluated(
                seat,
                isTenpai,
                isDiscardFuriten,
                playerSeat.IsTemporaryFuriten,
                playerSeat.IsReachPassFuriten);
        }

        private static bool[] BuildSelfDiscardedTypeIndices(
            IReadOnlyList<DiscardRecord> discards,
            SeatId seat)
        {
            bool[] selfDiscardedTypeIndices = new bool[TileTypeCount];
            if (discards == null)
                return selfDiscardedTypeIndices;

            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord discard = discards[i];
                if (discard.ActorSeat != seat)
                    continue;

                int typeIndex = discard.Tile.TypeIndex;
                if (!discard.Tile.IsValid || typeIndex < 0 || typeIndex >= TileTypeCount)
                    continue;

                selfDiscardedTypeIndices[typeIndex] = true;
            }

            return selfDiscardedTypeIndices;
        }
    }
}
