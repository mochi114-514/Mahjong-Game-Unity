using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class ReachDecisionGameFlowTestDriver : IDisposable
    {
        private readonly ReachGameFlowTestSupport support;
        private bool disposed;

        private ReachDecisionGameFlowTestDriver(ReachGameFlowTestSupport support)
        {
            this.support = support;
        }

        public static ReachDecisionGameFlowTestDriver Create(int participantCount = 1)
        {
            return new ReachDecisionGameFlowTestDriver(
                ReachGameFlowTestSupport.Create(
                    "ReachDecisionGameFlowTest",
                    participantCount));
        }

        public void DrawReachableHand() => support.DrawReachableHand();
        public void DrawWinningHand() => support.DrawWinningHand();
        public void RequestDeclareReach() => support.RequestDeclareReach();
        public void RequestCancelReachDiscardSelection() => support.RequestCancelReachDiscardSelection();
        public void RequestDeclineReach() => support.RequestDeclineReach();
        public void RequestDiscard(int handIndex) => support.RequestDiscard(handIndex);
        public void RequestDiscardDrawnTile() => support.RequestDiscardDrawnTile();
        public bool ShouldAutoDiscardDrawnTileAfterDraw(string seatName) =>
            support.ShouldAutoDiscardDrawnTileAfterDraw(seatName);
        public void TryAutoDiscardDrawnTileAfterDraw(string seatName) =>
            support.TryAutoDiscardDrawnTileAfterDraw(seatName);

        public bool IsWinDecisionPending => support.IsWinDecisionPending;
        public bool IsReachDecisionPending => support.IsReachDecisionPending;
        public bool IsReachDiscardSelectionPending => support.IsReachDiscardSelectionPending;
        public string TurnPhaseName => support.TurnPhaseName;
        public int ReachDiscardCandidateCount => support.ReachDiscardCandidateCount;
        public int DiscardCount => support.DiscardCount;

        public bool IsReachDeclared(string seatName) => support.IsReachDeclared(seatName);
        public bool IsIppatsuEligible(string seatName) => support.IsIppatsuEligible(seatName);
        public int ReachDeclaredTurnIndex(string seatName) => support.ReachDeclaredTurnIndex(seatName);
        public string DiscardSourceNameAt(int index) => support.DiscardSourceNameAt(index);
        public string DiscardTileCodeAt(int index) => support.DiscardTileCodeAt(index);
        public void SetParticipantType(string seatName, string participantTypeName) =>
            support.SetParticipantType(seatName, participantTypeName);
        public bool HasDrawnTile(string seatName) => support.HasDrawnTile(seatName);

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }
    }
}
