using System.Collections;
using MahjongPrototype.Domain;
using MahjongPrototype.Logging;
using UnityEngine;

namespace MahjongPrototype
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/CPU Turn Controller")]
    public sealed class CpuTurnController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float cpuDiscardDelaySeconds = 0.75f;

        private int operationVersion;
        private int activeOperationVersion;

        public float CpuDiscardDelaySeconds => cpuDiscardDelaySeconds;
        public int OperationVersion => operationVersion;
        public bool IsCpuTurnRunning =>
            activeOperationVersion != 0 && activeOperationVersion == operationVersion;

        private void OnDisable()
        {
            CancelPendingTurn();
        }

        public void TryStartCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            PlayerId playerId,
            SeatId seat,
            int turnIndex)
        {
            CancelPendingTurn();

            if (!IsSameCpuTurn(gateway, gameState, playerId, seat, turnIndex))
                return;

            int startedOperationVersion = operationVersion;
            activeOperationVersion = startedOperationVersion;
            StartCoroutine(RunCpuTurn(
                gateway,
                gameState,
                playerId,
                seat,
                turnIndex,
                startedOperationVersion));
        }

        public void CancelPendingTurn()
        {
            operationVersion++;
            activeOperationVersion = 0;
            StopAllCoroutines();
        }

        private IEnumerator RunCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            PlayerId playerId,
            SeatId seat,
            int turnIndex,
            int startedOperationVersion)
        {
            try
            {
                PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
                if (!playerSeat.HasDrawnTile)
                {
                    if (!gateway.RequestDrawForCpu(playerId, seat, turnIndex))
                        yield break;
                }

                if (!IsSameCpuTurn(
                        gateway,
                        gameState,
                        playerId,
                        seat,
                        turnIndex,
                        startedOperationVersion))
                {
                    LogPausedWinDecision(gameState, seat, turnIndex);
                    yield break;
                }

                if (cpuDiscardDelaySeconds > 0f)
                    yield return new WaitForSeconds(cpuDiscardDelaySeconds);
                else
                    yield return null;

                if (!IsSameCpuTurn(
                        gateway,
                        gameState,
                        playerId,
                        seat,
                        turnIndex,
                        startedOperationVersion))
                {
                    yield break;
                }

                if (!gameState.GetPlayerSeat(seat).HasDrawnTile)
                    yield break;

                // PROTOTYPE: The first CPU implementation always discards its drawn tile.
                gateway.RequestDiscardDrawnTileForCpu(playerId, seat, turnIndex);
            }
            finally
            {
                if (activeOperationVersion == startedOperationVersion)
                    activeOperationVersion = 0;
            }
        }

        private bool IsSameCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            PlayerId playerId,
            SeatId seat,
            int turnIndex)
        {
            return IsSameCpuTurn(
                gateway,
                gameState,
                playerId,
                seat,
                turnIndex,
                operationVersion);
        }

        private bool IsSameCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            PlayerId playerId,
            SeatId seat,
            int turnIndex,
            int startedOperationVersion)
        {
            if (gateway == null ||
                gameState == null ||
                startedOperationVersion != operationVersion ||
                !gateway.IsSameGameStateAndTurn(gameState, playerId, seat, turnIndex) ||
                (gameState.TurnPhase != TurnPhase.WaitingForDraw &&
                    gameState.TurnPhase != TurnPhase.WaitingForDiscard))
            {
                return false;
            }

            return true;
        }

        private static void LogPausedWinDecision(
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            if (gameState == null || !gameState.IsWinDecisionPending)
                return;

            DevLog.Record(
                "CPU",
                "CpuTurnPaused",
                "Win decision is pending. CPU auto discard was not started.",
                seat: seat,
                wallCount: gameState.Wall.Count,
                turnIndex: turnIndex);
        }
    }
}
