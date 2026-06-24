using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class PlayerTurnManager
    {
        private readonly TurnOrderService turnOrderService;

        public PlayerTurnManager(TurnOrderService turnOrderService)
        {
            this.turnOrderService = turnOrderService ?? throw new ArgumentNullException(nameof(turnOrderService));
        }

        public void InitializeRound(MahjongGameState gameState, SeatId firstSeat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentTurn = firstSeat;
            gameState.TurnIndex = 1;
            BeginTurn(gameState, firstSeat);
        }

        public void BeginTurn(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentTurn = seat;
        }

        public SeatId EndTurnAndSelectNext(
            MahjongGameState gameState,
            IReadOnlyList<SeatId> activeSeats)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            SeatId nextSeat = turnOrderService.GetNextSeat(activeSeats, gameState.CurrentTurn);
            gameState.CurrentTurn = nextSeat;
            gameState.TurnIndex++;
            BeginTurn(gameState, nextSeat);
            return nextSeat;
        }
    }
}
