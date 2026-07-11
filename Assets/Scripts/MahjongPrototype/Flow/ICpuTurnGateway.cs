using MahjongPrototype.Domain;

namespace MahjongPrototype
{
    public interface ICpuTurnGateway
    {
        bool RequestDrawForCpu(SeatId seat);
        bool RequestDiscardDrawnTileForCpu(SeatId seat);
        bool RequestDeclareWinForCpu(SeatId seat);
        bool IsSameGameStateAndTurn(MahjongGameState gameState, SeatId seat, int turnIndex);
    }
}
