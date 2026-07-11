using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class TurnFlowServiceTestDriver
    {
        private const string TurnOrderServiceTypeName =
            "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";
        private const string PlayerTurnManagerTypeName =
            "MahjongPrototype.Services.PlayerTurnManager, Assembly-CSharp";
        private const string TurnFlowServiceTypeName =
            "MahjongPrototype.Services.TurnFlowService, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object gameState;
        private readonly object service;

        private TurnFlowServiceTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            object gameState,
            object service)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
            this.gameState = gameState;
            this.service = service;
        }

        public static TurnFlowServiceTestDriver Create(params string[] occupiedSeatNames)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object turnManager = reflection.CreateInstance(
                reflection.RequireType(PlayerTurnManagerTypeName),
                reflection.CreateInstance(reflection.RequireType(TurnOrderServiceTypeName)));
            object service = reflection.CreateInstance(
                reflection.RequireType(TurnFlowServiceTypeName),
                turnManager);
            object gameState = dataFactory.CreateGameState(occupiedSeatNames);
            return new TurnFlowServiceTestDriver(reflection, dataFactory, gameState, service);
        }

        public void InitializeRound(string seatName)
        {
            reflection.SetProperty(gameState, "CurrentTurn", dataFactory.ParseSeat(seatName));
            reflection.SetProperty(gameState, "TurnIndex", 1);
        }

        public string AdvanceTurn()
        {
            return reflection.Invoke(service, "AdvanceTurn", gameState).ToString();
        }

        public void SetParticipantType(string seatName, string participantTypeName)
        {
            dataFactory.SetParticipantType(gameState, seatName, participantTypeName);
        }

        public void DeclareReach(string seatName)
        {
            object playerSeat = dataFactory.GetPlayerSeat(gameState, seatName);
            reflection.Invoke(playerSeat, "DeclareReach", TurnIndex);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            dataFactory.SetDrawnTile(gameState, seatName, tileCode);
        }

        public bool IsSameCurrentTurn(string seatName, int turnIndex)
        {
            return (bool)reflection.Invoke(
                service,
                "IsSameCurrentTurn",
                gameState,
                dataFactory.ParseSeat(seatName),
                turnIndex);
        }

        public bool CanContinueAutomaticProcessing(string seatName, int turnIndex)
        {
            return (bool)reflection.Invoke(
                service,
                "CanContinueAutomaticProcessing",
                gameState,
                dataFactory.ParseSeat(seatName),
                turnIndex);
        }

        public object BuildAutomationPolicy(string seatName, bool enableAutoDraw)
        {
            return reflection.Invoke(
                service,
                "BuildAutomationPolicy",
                gameState,
                dataFactory.ParseSeat(seatName),
                enableAutoDraw);
        }

        public bool ShouldAutoDiscardDrawnTileAfterDraw(string seatName, bool enableAutoDraw)
        {
            return (bool)reflection.Invoke(
                service,
                "ShouldAutoDiscardDrawnTileAfterDraw",
                gameState,
                dataFactory.ParseSeat(seatName),
                enableAutoDraw);
        }

        public string CurrentTurnName => reflection.GetProperty(gameState, "CurrentTurn").ToString();

        public int TurnIndex => (int)reflection.GetProperty(gameState, "TurnIndex");

        public bool PolicyIsCpu(object policy) => (bool)reflection.GetProperty(policy, "IsCpu");

        public bool PolicyAutoDrawAtTurnStart(object policy) =>
            (bool)reflection.GetProperty(policy, "AutoDrawAtTurnStart");

        public bool PolicyAutoDiscardDrawnTileAfterDraw(object policy) =>
            (bool)reflection.GetProperty(policy, "AutoDiscardDrawnTileAfterDraw");

        public bool PolicyUseCpuController(object policy) =>
            (bool)reflection.GetProperty(policy, "UseCpuController");
    }
}
