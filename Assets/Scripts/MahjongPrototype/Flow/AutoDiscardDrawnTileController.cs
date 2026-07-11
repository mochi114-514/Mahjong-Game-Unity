using System;
using System.Collections;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Auto Discard Drawn Tile Controller")]
    public sealed class AutoDiscardDrawnTileController : MonoBehaviour
    {
        private Coroutine pendingCoroutine;
        private int operationVersion;

        public int OperationVersion => operationVersion;

        private void OnDisable()
        {
            CancelPending();
        }

        public bool TryStart(
            SeatId seat,
            int turnIndex,
            float delaySeconds,
            Func<SeatId, int, bool> canExecute,
            Func<SeatId, bool> execute)
        {
            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            CancelPending();
            if (delaySeconds <= 0f)
                return execute(seat);

            int startedOperationVersion = operationVersion;
            pendingCoroutine = StartCoroutine(Run(
                seat,
                turnIndex,
                startedOperationVersion,
                delaySeconds,
                canExecute,
                execute));
            return true;
        }

        public void CancelPending()
        {
            operationVersion++;
            if (pendingCoroutine == null)
                return;

            StopCoroutine(pendingCoroutine);
            pendingCoroutine = null;
        }

        public IEnumerator CreateRoutine(
            SeatId seat,
            int turnIndex,
            int expectedOperationVersion,
            float delaySeconds,
            Func<SeatId, int, bool> canExecute,
            Func<SeatId, bool> execute)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSeconds(delaySeconds);
            else
                yield return null;

            if (expectedOperationVersion != operationVersion ||
                !canExecute(seat, turnIndex))
            {
                ClearPendingIfCurrent(expectedOperationVersion);
                yield break;
            }

            ClearPendingIfCurrent(expectedOperationVersion);
            execute(seat);
        }

        private IEnumerator Run(
            SeatId seat,
            int turnIndex,
            int startedOperationVersion,
            float delaySeconds,
            Func<SeatId, int, bool> canExecute,
            Func<SeatId, bool> execute)
        {
            return CreateRoutine(
                seat,
                turnIndex,
                startedOperationVersion,
                delaySeconds,
                canExecute,
                execute);
        }

        private void ClearPendingIfCurrent(int expectedOperationVersion)
        {
            if (expectedOperationVersion == operationVersion)
                pendingCoroutine = null;
        }
    }
}
