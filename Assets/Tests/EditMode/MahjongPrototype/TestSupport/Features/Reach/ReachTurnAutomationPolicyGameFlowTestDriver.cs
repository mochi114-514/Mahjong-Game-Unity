using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class ReachTurnAutomationPolicyGameFlowTestDriver : IDisposable
    {
        private readonly ReachGameFlowTestSupport support;
        private bool disposed;

        private ReachTurnAutomationPolicyGameFlowTestDriver(ReachGameFlowTestSupport support)
        {
            this.support = support;
        }

        public static ReachTurnAutomationPolicyGameFlowTestDriver Create(
            int participantCount,
            bool enableAutoDraw)
        {
            return new ReachTurnAutomationPolicyGameFlowTestDriver(
                ReachGameFlowTestSupport.Create(
                    "ReachTurnAutomationPolicyGameFlowTest",
                    participantCount,
                    enableAutoDraw));
        }

        public void StartNewRound() => support.StartNewRound();
        public void DrawReachableHand() => support.DrawReachableHand();

        public void DeclareReachWithHandDiscard(int handIndex)
        {
            support.RequestDeclareReach();
            support.RequestDiscard(handIndex);
        }

        public object BuildTurnAutomationPolicy(string seatName) =>
            support.BuildTurnAutomationPolicy(seatName);
        public bool PolicyIsCpu(object policy) => support.PolicyIsCpu(policy);
        public bool PolicyAutoDrawAtTurnStart(object policy) =>
            support.PolicyAutoDrawAtTurnStart(policy);
        public bool PolicyAutoDiscardDrawnTileAfterDraw(object policy) =>
            support.PolicyAutoDiscardDrawnTileAfterDraw(policy);
        public bool PolicyUseCpuController(object policy) => support.PolicyUseCpuController(policy);
        public void SetParticipantType(string seatName, string participantTypeName) =>
            support.SetParticipantType(seatName, participantTypeName);
        public void ForceDrawForSeat(string seatName, string tileCode) =>
            support.ForceDrawForSeat(seatName, tileCode);
        public void DrawAndDiscardForSeat(string seatName, string tileCode) =>
            support.DrawAndDiscardForSeat(seatName, tileCode);

        public string CurrentTurnName => support.CurrentTurnName;
        public string TurnPhaseName => support.TurnPhaseName;
        public int TurnIndex => support.TurnIndex;
        public int DiscardCount => support.DiscardCount;
        public bool IsReachDeclared(string seatName) => support.IsReachDeclared(seatName);
        public bool HasDrawnTile(string seatName) => support.HasDrawnTile(seatName);
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
