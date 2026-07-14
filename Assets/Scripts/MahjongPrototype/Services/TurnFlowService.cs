using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class TurnFlowService
    {
        private readonly PlayerTurnManager playerTurnManager;

        public TurnFlowService(PlayerTurnManager playerTurnManager)
        {
            this.playerTurnManager = playerTurnManager ??
                throw new ArgumentNullException(nameof(playerTurnManager));
        }

        public SeatId AdvanceTurn(MahjongGameState gameState)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            return playerTurnManager.EndTurnAndSelectNext(
                gameState,
                gameState.ActiveTurnSeats);
        }

        public void BeginTurnAfterCall(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            playerTurnManager.BeginTurnAfterCall(gameState, seat);
        }

        public void BeginTurnAfterKan(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            playerTurnManager.BeginTurnAfterKan(gameState, seat);
        }

        public bool IsSameCurrentTurn(
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            return gameState != null &&
                !gameState.IsRoundEnded &&
                !gameState.IsWinDecisionPending &&
                !gameState.IsReactionWindowPending &&
                gameState.CurrentTurn == seat &&
                gameState.TurnIndex == turnIndex;
        }

        public bool CanContinueAutomaticProcessing(
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            return IsSameCurrentTurn(gameState, seat, turnIndex) &&
                (gameState.TurnPhase == TurnPhase.WaitingForDraw ||
                    gameState.TurnPhase == TurnPhase.WaitingForDiscard);
        }

        public TurnAutomationPolicy BuildAutomationPolicy(
            MahjongGameState gameState,
            SeatId seat,
            bool enableAutoDraw)
        {
            if (gameState == null)
                return TurnAutomationPolicy.None;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            bool isCpu = IsCpu(gameState, seat);
            bool isReachDeclared = playerSeat != null && playerSeat.IsReachDeclared;

            return new TurnAutomationPolicy(
                isCpu,
                enableAutoDraw || isReachDeclared,
                isReachDeclared,
                isCpu);
        }

        public bool ShouldAutoDiscardDrawnTileAfterDraw(
            MahjongGameState gameState,
            SeatId seat,
            bool enableAutoDraw)
        {
            if (gameState == null ||
                gameState.IsRoundEnded ||
                gameState.IsWinDecisionPending ||
                gameState.IsReactionWindowPending ||
                gameState.IsReachDecisionPending ||
                gameState.IsReachDiscardSelectionPending ||
                gameState.CurrentTurn != seat ||
                gameState.TurnPhase != TurnPhase.WaitingForDiscard)
            {
                return false;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat == null || !playerSeat.HasDrawnTile)
                return false;

            return BuildAutomationPolicy(gameState, seat, enableAutoDraw)
                .AutoDiscardDrawnTileAfterDraw;
        }

        public bool IsCpu(MahjongGameState gameState, SeatId seat)
        {
            return GetParticipantType(gameState, seat) == ParticipantType.Cpu;
        }

        public bool IsLocalHuman(MahjongGameState gameState, SeatId seat)
        {
            return GetParticipantType(gameState, seat) == ParticipantType.LocalHuman;
        }

        public bool IsRemoteHuman(MahjongGameState gameState, SeatId seat)
        {
            return GetParticipantType(gameState, seat) == ParticipantType.RemoteHuman;
        }

        private static ParticipantType? GetParticipantType(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                return null;

            SeatSlot slot = gameState.GetSeatSlot(seat);
            return slot.HasPlayer ? slot.ParticipantType : (ParticipantType?)null;
        }
    }

    public readonly struct TurnAutomationPolicy
    {
        public static TurnAutomationPolicy None =>
            new TurnAutomationPolicy(false, false, false, false);

        public TurnAutomationPolicy(
            bool isCpu,
            bool autoDrawAtTurnStart,
            bool autoDiscardDrawnTileAfterDraw,
            bool useCpuController)
        {
            IsCpu = isCpu;
            AutoDrawAtTurnStart = autoDrawAtTurnStart;
            AutoDiscardDrawnTileAfterDraw = autoDiscardDrawnTileAfterDraw;
            UseCpuController = useCpuController;
        }

        public bool IsCpu { get; }
        public bool AutoDrawAtTurnStart { get; }
        public bool AutoDiscardDrawnTileAfterDraw { get; }
        public bool UseCpuController { get; }
    }
}
