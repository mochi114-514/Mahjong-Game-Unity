using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class ReachAutoDiscardGameFlowTestDriver : IDisposable
    {
        private readonly ReachGameFlowTestSupport support;
        private bool disposed;

        private ReachAutoDiscardGameFlowTestDriver(ReachGameFlowTestSupport support)
        {
            this.support = support;
        }

        public static ReachAutoDiscardGameFlowTestDriver Create(
            int participantCount = 1,
            float autoDiscardDelaySeconds = 0f)
        {
            return new ReachAutoDiscardGameFlowTestDriver(
                ReachGameFlowTestSupport.Create(
                    "ReachAutoDiscardGameFlowTest",
                    participantCount,
                    enableAutoDraw: false,
                    autoDiscardDelaySeconds: autoDiscardDelaySeconds));
        }

        public void DrawReachableHand() => support.DrawReachableHand();

        public void DeclareReachWithHandDiscard(int handIndex)
        {
            support.RequestDeclareReach();
            support.RequestDiscard(handIndex);
        }

        public void DeclareReachWithDrawnTileDiscard()
        {
            support.RequestDeclareReach();
            support.RequestDiscardDrawnTile();
        }

        public void RequestDiscard(int handIndex) => support.RequestDiscard(handIndex);
        public void RequestDeclineWin() => support.RequestDeclineWin();
        public void SetParticipantType(string seatName, string participantTypeName) =>
            support.SetParticipantType(seatName, participantTypeName);
        public void AddHandTiles(string seatName, params string[] tileCodes) =>
            support.AddHandTiles(seatName, tileCodes);
        public void ForceDrawForSeat(string seatName, string tileCode) =>
            support.ForceDrawForSeat(seatName, tileCode);
        public void DrawAndDiscardForSeat(string seatName, string tileCode) =>
            support.DrawAndDiscardForSeat(seatName, tileCode);
        public void ForceDraw(string tileCode) => support.ForceDraw(tileCode);
        public void RequestDraw() => support.RequestDraw();
        public object BeginAutoDiscardRoutine(string seatName) =>
            support.BeginAutoDiscardRoutine(seatName);
        public bool MoveNext(object routine) => support.MoveNext(routine);
        public string CurrentYieldTypeName(object routine) => support.CurrentYieldTypeName(routine);

        public bool IsReachDeclared(string seatName) => support.IsReachDeclared(seatName);
        public bool HasDrawnTile(string seatName) => support.HasDrawnTile(seatName);
        public string DrawnTileCode(string seatName) => support.DrawnTileCode(seatName);
        public bool IsWinDecisionPending => support.IsWinDecisionPending;
        public string WinDecisionTypeName => support.WinDecisionTypeName;
        public string WinDecisionSeatName => support.WinDecisionSeatName;
        public string WinSourceSeatName => support.WinSourceSeatName;
        public string CurrentTurnName => support.CurrentTurnName;
        public string TurnPhaseName => support.TurnPhaseName;
        public int TurnIndex => support.TurnIndex;
        public int DiscardCount => support.DiscardCount;
        public string LastDiscardActorSeatName => support.LastDiscardActorSeatName;
        public string LastDiscardSourceName => support.LastDiscardSourceName;
        public string LastDiscardTileCode => support.LastDiscardTileCode;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }
    }
}
