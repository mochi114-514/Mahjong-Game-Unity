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

        public bool TryRequestDeclareWinForSeat(string seatName)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclareWinForSeat",
                dataFactory.ParseSeat(seatName));
        }

        public void RequestDeclineWin()
        {
            reflection.Invoke(GameFlow, "RequestDeclineWin");
        }

        public bool TryRequestDeclareRonForSeat(string seatName, int reactionWindowId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclareRonForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId);
        }

        public bool TryRequestDeclineRonForSeat(string seatName, int reactionWindowId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclineRonForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId);
        }

        public bool TryRequestDeclarePonForSeat(string seatName, int reactionWindowId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclarePonForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId);
        }

        public bool TryRequestDeclareDaiminkanForSeat(string seatName, int reactionWindowId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclareDaiminkanForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId);
        }

        public bool TryRequestDeclareAnkanForSeat(string seatName, string tileCode)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclareAnkanForSeat",
                dataFactory.ParseSeat(seatName),
                dataFactory.CreateTile(tileCode));
        }

        public object GetAnkanCandidatesForSeat(string seatName)
        {
            return reflection.Invoke(
                GameFlow,
                "GetAnkanCandidatesForSeat",
                dataFactory.ParseSeat(seatName));
        }

        public object GetSelfKanCandidatesForSeat(string seatName)
        {
            return reflection.Invoke(
                GameFlow,
                "GetSelfKanCandidatesForSeat",
                dataFactory.ParseSeat(seatName));
        }

        public bool TryRequestDeclareKakanForSeat(
            string seatName,
            string tileCode,
            int sourcePonMeldIndex)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclareKakanForSeat",
                dataFactory.ParseSeat(seatName),
                (int)reflection.GetProperty(dataFactory.CreateTile(tileCode), "TypeIndex"),
                sourcePonMeldIndex);
        }

        public bool TryRequestDeclineSelfKanForSeat(string seatName)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclineSelfKanForSeat",
                dataFactory.ParseSeat(seatName));
        }

        public bool TryRequestDeclareChiForSeat(
            string seatName,
            int reactionWindowId,
            int optionId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclareChiForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId,
                optionId);
        }

        public bool TryRequestDeclinePonForSeat(string seatName, int reactionWindowId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclinePonForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId);
        }

        public bool TryRequestDeclineMeldCallsForSeat(string seatName, int reactionWindowId)
        {
            return (bool)reflection.Invoke(
                GameFlow,
                "TryRequestDeclineMeldCallsForSeat",
                dataFactory.ParseSeat(seatName),
                reactionWindowId);
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

        public void ResolveAfterDraw(string seatName, bool isRinshanDraw = false)
        {
            reflection.Invoke(
                GameFlow,
                "ResolveAfterDraw",
                dataFactory.ParseSeat(seatName),
                isRinshanDraw);
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
