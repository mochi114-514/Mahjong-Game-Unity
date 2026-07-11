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

        public float CpuDiscardDelaySeconds => cpuDiscardDelaySeconds;

        private void OnDisable()
        {
            CancelPendingTurn();
        }

        public void TryStartCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            CancelPendingTurn();

            if (!IsSameCpuTurn(gateway, gameState, seat, turnIndex))
                return;

            int startedOperationVersion = operationVersion;
            StartCoroutine(RunCpuTurn(
                gateway,
                gameState,
                seat,
                turnIndex,
                startedOperationVersion));
        }

        public void CancelPendingTurn()
        {
            operationVersion++;
            StopAllCoroutines();
        }

        public bool TryRespondToWinDecision(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            if (gateway == null ||
                gameState == null ||
                !gateway.IsSameGameStateAndTurn(gameState, seat, turnIndex) ||
                gameState.IsRoundEnded ||
                !gameState.IsWinDecisionPending ||
                gameState.WinDecisionSeat != seat ||
                gameState.WinDecisionTurnIndex != turnIndex)
            {
                return false;
            }

            // PROTOTYPE: CPU declares every legal self-draw win decision.
            return gateway.RequestDeclareWinForCpu(seat);
        }

        private IEnumerator RunCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex,
            int startedOperationVersion)
        {
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (!playerSeat.HasDrawnTile)
            {
                if (!gateway.RequestDrawForCpu(seat))
                    yield break;
            }

            if (!IsSameCpuTurn(
                    gateway,
                    gameState,
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
                    seat,
                    turnIndex,
                    startedOperationVersion))
            {
                yield break;
            }

            if (!gameState.GetPlayerSeat(seat).HasDrawnTile)
                yield break;

            // PROTOTYPE: The first CPU implementation always discards its drawn tile.
            gateway.RequestDiscardDrawnTileForCpu(seat);
        }

        private bool IsSameCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            return IsSameCpuTurn(
                gateway,
                gameState,
                seat,
                turnIndex,
                operationVersion);
        }

        private bool IsSameCpuTurn(
            ICpuTurnGateway gateway,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex,
            int startedOperationVersion)
        {
            if (gateway == null ||
                gameState == null ||
                startedOperationVersion != operationVersion ||
                !gateway.IsSameGameStateAndTurn(gameState, seat, turnIndex) ||
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
