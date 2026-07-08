using MahjongPrototype.Tests.TestSupport.Core;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongGameFlowTestCommands
    {
        private readonly MahjongGameFlowTestHarness harness;
        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;

        public MahjongGameFlowTestCommands(MahjongGameFlowTestHarness harness)
        {
            this.harness = harness;
            reflection = harness.Reflection;
            dataFactory = harness.DataFactory;
        }

        public void StartNewRound()
        {
            reflection.Invoke(GameFlow, "StartNewRound");
        }

        public void RetryPrototype()
        {
            reflection.Invoke(GameFlow, "RetryPrototype");
        }

        public void RequestDraw()
        {
            reflection.Invoke(GameFlow, "RequestDraw");
        }

        public void RequestDiscard(int handIndex)
        {
            reflection.Invoke(GameFlow, "RequestDiscard", handIndex);
        }

        public void RequestDiscardDrawnTile()
        {
            reflection.Invoke(GameFlow, "RequestDiscardDrawnTile");
        }

        public void RequestForceDrawSkill(string tileCode)
        {
            reflection.Invoke(GameFlow, "RequestForceDrawSkill", tileCode);
        }

        public void RequestForceDrawSkillForSeat(string seatName, string tileCode)
        {
            reflection.Invoke(
                GameFlow,
                "RequestForceDrawSkillForSeat",
                dataFactory.ParseSeat(seatName),
                tileCode);
        }

        public bool TryRequestDrawForSeat(string seatName)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDrawForSeat",
                dataFactory.ParseSeat(seatName));
        }

        public bool TryRequestDiscardDrawnTileForSeat(string seatName)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDiscardDrawnTileForSeat",
                dataFactory.ParseSeat(seatName));
        }

        public void RequestDeclareWin()
        {
            reflection.Invoke(GameFlow, "RequestDeclareWin");
        }

        public void RequestDeclineWin()
        {
            reflection.Invoke(GameFlow, "RequestDeclineWin");
        }

        public void RequestAdvanceFromRoundResult()
        {
            reflection.Invoke(GameFlow, "RequestAdvanceFromRoundResult");
        }

        public void StartTurn(string seatName, int turnIndex)
        {
            reflection.Invoke(GameFlow, "StartTurn", dataFactory.ParseSeat(seatName), turnIndex);
        }

        public void CheckWinPrototype()
        {
            reflection.Invoke(GameFlow, "CheckWinPrototype");
        }

        public void DealInitialHands()
        {
            reflection.Invoke(GameFlow, "DealInitialHands");
        }

        public void SetWinDecisionPending(string seatName, int turnIndex)
        {
            reflection.Invoke(GameFlow, "SetWinDecisionPending", true, dataFactory.ParseSeat(seatName), turnIndex);
        }

        private object GameFlow => harness.GameFlow;
    }
}
