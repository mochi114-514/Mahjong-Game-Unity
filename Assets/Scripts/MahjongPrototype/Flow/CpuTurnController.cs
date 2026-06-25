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
            MahjongGameFlow gameFlow,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            CancelPendingTurn();

            if (!IsSameCpuTurn(gameFlow, gameState, seat, turnIndex))
                return;

            int startedOperationVersion = operationVersion;
            StartCoroutine(RunCpuTurn(
                gameFlow,
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

        private IEnumerator RunCpuTurn(
            MahjongGameFlow gameFlow,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex,
            int startedOperationVersion)
        {
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (!playerSeat.HasDrawnTile)
            {
                if (!gameFlow.TryRequestDrawForSeat(seat))
                    yield break;
            }

            if (!IsSameCpuTurn(
                    gameFlow,
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
                    gameFlow,
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
            gameFlow.TryRequestDiscardDrawnTileForSeat(seat);
        }

        private bool IsSameCpuTurn(
            MahjongGameFlow gameFlow,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex)
        {
            return IsSameCpuTurn(
                gameFlow,
                gameState,
                seat,
                turnIndex,
                operationVersion);
        }

        private bool IsSameCpuTurn(
            MahjongGameFlow gameFlow,
            MahjongGameState gameState,
            SeatId seat,
            int turnIndex,
            int startedOperationVersion)
        {
            if (gameFlow == null ||
                gameState == null ||
                startedOperationVersion != operationVersion ||
                !ReferenceEquals(gameFlow.CurrentState, gameState) ||
                gameState.IsRoundEnded ||
                gameState.IsWinDecisionPending ||
                gameState.CurrentTurn != seat ||
                gameState.TurnIndex != turnIndex)
            {
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            return slot.HasPlayer && slot.ParticipantType == ParticipantType.Cpu;
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
