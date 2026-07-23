using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class CpuTurnGameFlowTestDriver : IDisposable
    {
        private readonly TurnGameFlowTestSupport support;
        private readonly List<string> eventOrder = new List<string>();
        private object cpuDiscardRecord;
        private bool disposed;

        private CpuTurnGameFlowTestDriver(TurnGameFlowTestSupport support)
        {
            this.support = support;
        }

        public static CpuTurnGameFlowTestDriver Create()
        {
            return Create(
                "CpuTurnGameFlowTest",
                initialHandTileCount: 1,
                enableAutoDraw: true);
        }

        public static CpuTurnGameFlowTestDriver CreateForCpuWinningTsumo(bool enableAutoDraw)
        {
            return Create(
                "CpuWinningTsumoGameFlowTest",
                initialHandTileCount: 0,
                enableAutoDraw: enableAutoDraw);
        }

        public static CpuTurnGameFlowTestDriver CreateForCpuNineTerminals()
        {
            return Create(
                "CpuNineTerminalsGameFlowTest",
                initialHandTileCount: 0,
                enableAutoDraw: true);
        }

        public static CpuTurnGameFlowTestDriver CreateForOpeningCpuTurn()
        {
            return Create(
                "OpeningCpuTurnGameFlowTest",
                initialHandTileCount: 1,
                enableAutoDraw: false,
                fixedSelfSeatName: "West");
        }

        private static CpuTurnGameFlowTestDriver Create(
            string rootName,
            int initialHandTileCount,
            bool enableAutoDraw,
            string fixedSelfSeatName = "East")
        {
            CpuTurnGameFlowTestDriver driver = new CpuTurnGameFlowTestDriver(
                TurnGameFlowTestSupport.Create(
                    rootName,
                    participantCount: 2,
                    initialHandTileCount: initialHandTileCount,
                    enableAutoDraw: enableAutoDraw,
                    fixedSelfSeatName: fixedSelfSeatName,
                    addEventNotifier: true));
            driver.support.SetCpuDiscardDelay(0f);
            return driver;
        }

        public void StartNewRound() => support.StartNewRound();
        public void RequestSelfDiscardDrawnTile() => support.RequestDiscardDrawnTile();
        public void PumpDecisionCoordinator() => support.PumpDecisionCoordinator();
        public void RetryPrototype() => support.RetryPrototype();
        public void SetRoundEnded(bool value) => support.SetRoundEnded(value);

        public void SetPlayer2ParticipantType(string participantTypeName)
        {
            support.SetParticipantTypeForPlayerId("Player2", participantTypeName);
        }

        public void BeginWinDecisionForPlayer2()
        {
            support.Reflection.Invoke(
                support.CurrentState,
                "BeginWinDecision",
                support.DataFactory.ParseSeat(Player2SeatName),
                support.TurnIndex);
        }

        public void PreparePlayer2WinningTsumoOnNextDraw()
        {
            support.AddHandTilesForPlayerId(
                "Player2",
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");
            support.RequestForceDrawSkillForSeat(Player2SeatName, "C");
        }

        public void PreparePlayer2NineTerminalsOnNextDraw()
        {
            support.AddHandTilesForPlayerId(
                "Player2",
                "1m", "9m", "1p", "9p", "1s", "9s",
                "E", "S", "W",
                "2m", "3m", "4m", "5m");
            support.RequestForceDrawSkillForSeat(Player2SeatName, "P");
        }

        public void SetSelfDrawnTile(string tileCode)
        {
            support.SetDrawnTile(support.SelfSeatName, tileCode);
        }

        public void SubscribeCpuTurnEventTrace()
        {
            eventOrder.Clear();
            cpuDiscardRecord = null;

            support.AddSingleArgumentEventHandler(
                "TileDrawn",
                drawResult =>
                {
                    if (support.Reflection.GetProperty(drawResult, "Seat").ToString() == Player2SeatName &&
                        support.Reflection.GetProperty(drawResult, "Purpose").ToString() == "TurnDraw")
                    {
                        eventOrder.Add("CpuTileDrawn");
                    }
                });
            support.AddSingleArgumentEventHandler(
                "TileDiscarded",
                discardRecord =>
                {
                    if (support.Reflection.GetProperty(discardRecord, "ActorSeat").ToString() == Player2SeatName)
                    {
                        cpuDiscardRecord = discardRecord;
                        eventOrder.Add("CpuTileDiscarded");
                    }
                });
            support.AddTwoArgumentEventHandler(
                "TurnStarted",
                (seat, _) =>
                {
                    if (seat.ToString() == SelfSeatName && eventOrder.Contains("CpuTileDiscarded"))
                        eventOrder.Add("NextTurnStarted");
                });
        }

        public int EventIndex(string eventName) => eventOrder.IndexOf(eventName);

        public IEnumerator WaitForCpuDiscardAndNextTurn(
            int expectedNextTurnIndex,
            int maxFrames = 30)
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                // MahjongGameFlow normally pumps here from Update. Pumping at
                // the same authority boundary makes a queued provider reply
                // observable without fabricating any decision response.
                PumpDecisionCoordinator();

                if (HasCpuDiscardRecord &&
                    CurrentTurnName == SelfSeatName &&
                    TurnIndex == expectedNextTurnIndex)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(
                $"CPU turn did not complete within {maxFrames} frames. {DescribeCpuTurnState()}");
        }

        public object CurrentStateToken => support.StateToken;
        public string SelfSeatName => support.SelfSeatName;
        public string Player2SeatName => support.SeatByPlayerId("Player2");
        public string CurrentTurnName => support.CurrentTurnName;
        public int TurnIndex => support.TurnIndex;
        public bool Player2HasDrawnTile => support.HasDrawnTileForPlayerId("Player2");
        public int DiscardCount => support.DiscardCount;
        public bool IsRoundEnded => support.IsRoundEnded;
        public bool IsRoundResultPending => support.IsRoundResultPending;
        public string TurnPhaseName => support.TurnPhaseName;
        public string RoundResultTypeName => support.RoundResultTypeName;
        public string RoundResultWinnerSeatName => support.RoundResultWinnerSeatName;
        public bool HasCpuDiscardRecord => cpuDiscardRecord != null;
        public string CpuDiscardActorSeatName =>
            support.Reflection.GetProperty(cpuDiscardRecord, "ActorSeat").ToString();
        public string CpuDiscardSourceName =>
            support.Reflection.GetProperty(cpuDiscardRecord, "Source").ToString();

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }

        private string DescribeCpuTurnState()
        {
            object coordinator = support.Reflection.GetProperty(
                support.GameFlow,
                "DecisionCoordinator");
            int coordinatorPendingCount = coordinator != null
                ? (int)support.Reflection.GetProperty(coordinator, "PendingCount")
                : -1;
            int queuedResponseCount = coordinator != null
                ? (int)support.Reflection.GetProperty(
                    coordinator,
                    "QueuedResponseCount")
                : -1;
            object controller = support.Reflection.GetPrivateField(
                support.GameFlow,
                "cpuTurnController");
            string controllerState = controller == null
                ? "missing"
                : $"running={support.Reflection.GetProperty(controller, "IsCpuTurnRunning")}; " +
                  $"operationVersion={support.Reflection.GetProperty(controller, "OperationVersion")}";

            return
                $"phase={TurnPhaseName}; currentSeat={CurrentTurnName}; turnIndex={TurnIndex}; " +
                $"cpuSeat={Player2SeatName}; cpuHasDrawnTile={Player2HasDrawnTile}; " +
                $"cpuDiscardRecorded={HasCpuDiscardRecord}; discardCount={DiscardCount}; " +
                $"pendingDecisions={DescribePendingDecisionKinds()}; " +
                $"decisionCoordinatorPending={coordinatorPendingCount}; " +
                $"queuedResponses={queuedResponseCount}; cpuController={controllerState}; " +
                $"events=[{string.Join(",", eventOrder)}]";
        }

        private string DescribePendingDecisionKinds()
        {
            List<string> kinds = new List<string>();
            CollectPendingDecisionKinds(
                support.Reflection.GetPrivateField(
                    support.GameFlow,
                    "pendingDecisionRequestsById"),
                false,
                kinds);
            CollectPendingDecisionKinds(
                support.Reflection.GetPrivateField(
                    support.GameFlow,
                    "pendingReactionDecisionsByRequestId"),
                true,
                kinds);
            return kinds.Count <= 0 ? "none" : string.Join(",", kinds);
        }

        private void CollectPendingDecisionKinds(
            object dictionaryObject,
            bool isReactionPending,
            List<string> kinds)
        {
            if (!(dictionaryObject is IDictionary dictionary))
                return;

            foreach (DictionaryEntry entry in dictionary)
            {
                object request = isReactionPending
                    ? support.Reflection.GetProperty(entry.Value, "Request")
                    : entry.Value;
                kinds.Add(support.Reflection.GetProperty(request, "Kind").ToString());
            }
        }
    }
}
