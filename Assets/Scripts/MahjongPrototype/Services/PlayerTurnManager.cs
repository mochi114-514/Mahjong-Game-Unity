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
            Phase = TurnPhase.NotStarted;
        }

        public TurnPhase Phase { get; private set; }

        public void InitializeRound(MahjongGameState gameState, SeatId firstSeat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentSeat = firstSeat;
            gameState.TurnIndex = 1;
            BeginTurn(gameState, firstSeat);
        }

        public void BeginTurn(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            gameState.CurrentSeat = seat;
            RefreshPhaseFromState(gameState);
        }

        public void RefreshPhaseFromState(MahjongGameState gameState)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            Phase = gameState.IsRoundEnded
                ? TurnPhase.RoundEnded
                : gameState.GetPlayerSeat(gameState.CurrentSeat).HasDrawnTile
                    ? TurnPhase.WaitingForDiscard
                    : TurnPhase.WaitingForDraw;
        }

        public SeatId EndTurnAndSelectNext(
            MahjongGameState gameState,
            IReadOnlyList<SeatId> activeSeats)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            SeatId nextSeat = turnOrderService.GetNextSeat(activeSeats, gameState.CurrentSeat);
            gameState.CurrentSeat = nextSeat;
            gameState.TurnIndex++;
            BeginTurn(gameState, nextSeat);
            return nextSeat;
        }

        public bool IsTurnActive(MahjongGameState gameState, SeatId seat)
        {
            return gameState != null &&
                !gameState.IsRoundEnded &&
                Phase != TurnPhase.NotStarted &&
                Phase != TurnPhase.WinDecision &&
                Phase != TurnPhase.RoundEnded &&
                gameState.CurrentSeat == seat;
        }

        public void MarkWinDecision()
        {
            Phase = TurnPhase.WinDecision;
        }

        public void MarkRoundEnded()
        {
            Phase = TurnPhase.RoundEnded;
        }
    }
}
