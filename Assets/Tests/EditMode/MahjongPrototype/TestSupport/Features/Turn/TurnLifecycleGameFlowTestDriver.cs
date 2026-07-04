using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class TurnLifecycleGameFlowTestDriver : IDisposable
    {
        private readonly TurnGameFlowTestSupport support;
        private bool disposed;

        private TurnLifecycleGameFlowTestDriver(TurnGameFlowTestSupport support)
        {
            this.support = support;
        }

        public static TurnLifecycleGameFlowTestDriver Create(
            int participantCount = 1,
            bool enableAutoDraw = false,
            string fixedSelfSeatName = "East")
        {
            return new TurnLifecycleGameFlowTestDriver(
                TurnGameFlowTestSupport.Create(
                    "TurnLifecycleGameFlowTest",
                    participantCount: participantCount,
                    initialHandTileCount: 1,
                    enableAutoDraw: enableAutoDraw,
                    fixedSelfSeatName: fixedSelfSeatName));
        }

        public void StartNewRound() => support.StartNewRound();
        public void RetryPrototype() => support.RetryPrototype();
        public void RequestDiscardDrawnTile() => support.RequestDiscardDrawnTile();
        public void StartCurrentTurnAgain() => support.StartCurrentTurnAgain();
        public void ClearCurrentPlayerDrawnTile() => support.ClearCurrentPlayerDrawnTile();
        public void SetRoundEnded(bool value) => support.SetRoundEnded(value);
        public void BeginWinDecisionForCurrentTurn() => support.BeginWinDecisionForCurrentTurn();
        public void SetFixedSelfSeat(string seatName) => support.SetFixedSelfSeat(seatName);

        public bool CurrentPlayerHasDrawnTile => support.CurrentPlayerHasDrawnTile;
        public string CurrentPlayerDrawnTileCodeOrNull => support.CurrentPlayerDrawnTileCodeOrNull;
        public string TurnPhaseName => support.TurnPhaseName;
        public int TurnIndex => support.TurnIndex;
        public int WallCount => support.WallCount;
        public string[] OccupiedSeatNames => support.OccupiedSeatNames;
        public string[] ActiveTurnSeatNames => support.ActiveTurnSeatNames;
        public string[] ActiveSeatNames => support.ActiveSeatNames;
        public string CurrentTurnName => support.CurrentTurnName;
        public string CurrentTurnPlayerIdName => support.CurrentTurnPlayerIdName;
        public string SelfSeatName => support.SelfSeatName;
        public string SelfWindName => support.SelfWindName;
        public string SelfPlayerIdName => support.SelfPlayerIdName;
        public bool IsSelfTurn => support.IsSelfTurn;
        public int SeatSlotCount => support.SeatSlotCount;
        public string SelfSeatSlotWindName => support.SelfSeatSlotWindName;
        public string CurrentTurnSlotWindName => support.CurrentTurnSlotWindName;
        public string SelfParticipantTypeNameOrNull =>
            support.ParticipantTypeNameOrNull(support.SelfSeatName);
        public string Player2ParticipantTypeNameOrNull =>
            support.ParticipantTypeNameOrNullForPlayerId("Player2");
        public string SouthParticipantTypeNameOrNull =>
            support.ParticipantTypeNameOrNull("South");

        public string SeatByPlayerId(string playerIdName) =>
            support.SeatByPlayerId(playerIdName);
        public bool IsSelfSeat(string seatName) => support.IsSelfSeat(seatName);
        public string SeatSlotWindAt(int index) => support.SeatSlotWindAt(index);
        public string SeatSlotPlayerIdNameOrNullAt(int index) =>
            support.SeatSlotPlayerIdNameOrNullAt(index);
        public bool SeatSlotHasPlayerAt(int index) => support.SeatSlotHasPlayerAt(index);
        public bool SeatSlotIsEmptyAt(int index) => support.SeatSlotIsEmptyAt(index);
        public string SeatSlotStateLabelAt(int index) => support.SeatSlotStateLabelAt(index);

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }
    }
}
