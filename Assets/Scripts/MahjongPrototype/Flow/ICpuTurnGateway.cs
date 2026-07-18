using MahjongPrototype.Domain;

namespace MahjongPrototype
{
    public interface ICpuTurnGateway
    {
        bool RequestDrawForCpu(PlayerId playerId, SeatId seat, int turnIndex);
        bool RequestDiscardDrawnTileForCpu(PlayerId playerId, SeatId seat, int turnIndex);
        bool IsSameGameStateAndTurn(
            MahjongGameState gameState,
            PlayerId playerId,
            SeatId seat,
            int turnIndex);
    }
}
