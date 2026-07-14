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

            gameState.TurnIndex = 1;
            BeginTurn(gameState, firstSeat);
        }

        public void BeginTurn(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentTurn = seat;
            gameState.EnterWaitingForDraw();
        }

        public SeatId EndTurnAndSelectNext(
            MahjongGameState gameState,
            IReadOnlyList<SeatId> activeSeats)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            SeatId nextSeat = turnOrderService.GetNextSeat(activeSeats, gameState.CurrentTurn);
            gameState.TurnIndex++;
            BeginTurn(gameState, nextSeat);
            return nextSeat;
        }

        public void BeginTurnAfterCall(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentTurn = seat;
            gameState.TurnIndex++;
            gameState.EnterWaitingForDiscardAfterCall();
        }

        public void BeginTurnAfterKan(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentTurn = seat;
            gameState.TurnIndex++;
            gameState.EnterWaitingForRinshanDraw();
        }
    }
}
