using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class DiscardService
    {
        public DiscardResult DiscardTile(
            MahjongGameState gameState,
            SeatId actorSeat,
            int handIndex)
        {
            if (gameState == null)
                return DiscardResult.Failed("GameState is not available.");

            if (gameState.CurrentSeat != actorSeat)
                return DiscardResult.Failed("Only current seat can discard.");

            PlayerSeat playerSeat = gameState.GetPlayerSeat(actorSeat);
            if (!playerSeat.Hand.TryRemoveAt(handIndex, out Tile discardedTile))
                return DiscardResult.Failed("Hand index is out of range.");

            DiscardRecord record = new DiscardRecord(actorSeat, discardedTile, gameState.TurnIndex);
            gameState.AddDiscard(record);
            return DiscardResult.Discarded(record);
        }
    }
}
