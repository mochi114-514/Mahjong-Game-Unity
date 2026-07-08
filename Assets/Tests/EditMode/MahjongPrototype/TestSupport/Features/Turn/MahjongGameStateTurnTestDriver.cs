using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class MahjongGameStateTurnTestDriver
    {
        private const string RoundResultTypeName =
            "MahjongPrototype.Domain.RoundResult, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object gameState;
        private readonly MahjongGameStateTestQuery query;

        private MahjongGameStateTurnTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            object gameState,
            MahjongGameStateTestQuery query)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
            this.gameState = gameState;
            this.query = query;
        }

        public static MahjongGameStateTurnTestDriver Create(params string[] occupiedSeatNames)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object gameState = dataFactory.CreateGameState(occupiedSeatNames);
            return new MahjongGameStateTurnTestDriver(
                reflection,
                dataFactory,
                gameState,
                MahjongGameStateTestQuery.ForState(
                    gameState,
                    reflection,
                    collections,
                    dataFactory));
        }

        public void BeginWinDecision(string seatName, int turnIndex)
        {
            reflection.Invoke(
                gameState,
                "BeginWinDecision",
                dataFactory.ParseSeat(seatName),
                turnIndex);
        }

        public void ClearWinDecision()
        {
            reflection.Invoke(gameState, "ClearWinDecision");
        }

        public void BeginExhaustiveDrawRoundResult(bool isFinalRound = false)
        {
            object result = reflection.InvokeStatic(
                reflection.RequireType(RoundResultTypeName),
                "CreateExhaustiveDraw",
                reflection.GetProperty(gameState, "WindProgress"),
                TurnIndex,
                isFinalRound);
            reflection.Invoke(gameState, "BeginRoundResult", result);
        }

        public void CompleteRoundResult(bool gameEnded)
        {
            reflection.Invoke(gameState, "CompleteRoundResult", gameEnded);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            dataFactory.SetDrawnTile(gameState, seatName, tileCode);
        }

        public void SetRoundEnded(bool value)
        {
            reflection.SetProperty(gameState, "IsRoundEnded", value);
        }

        public void SetSelfSeat(string seatName)
        {
            reflection.Invoke(gameState, "SetSelfSeat", dataFactory.ParseSeat(seatName));
        }

        public void AssignPlayerToSeat(string playerIdName, string seatName)
        {
            reflection.Invoke(
                gameState,
                "AssignPlayerToSeat",
                dataFactory.ParsePlayerId(playerIdName),
                dataFactory.ParseSeat(seatName));
        }

        public void SetCurrentTurn(string seatName)
        {
            dataFactory.SetCurrentTurn(gameState, seatName);
        }

        public void RebuildActiveTurnSeats()
        {
            reflection.Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
        }

        public bool IsWinDecisionPending => query.IsWinDecisionPending;

        public string WinDecisionSeatName => query.WinDecisionSeatName;

        public string WinDecisionTypeName => query.WinDecisionTypeNameOrNull;

        public string WinSourceSeatNameOrNull => query.WinSourceSeatNameOrNull;

        public int WinDecisionTurnIndex => query.WinDecisionTurnIndex;

        public string WinningTileCodeOrNull => query.WinningTileCodeOrNull;

        public bool IsRoundResultPending => query.IsRoundResultPending;

        public bool IsGameEnded => query.IsGameEnded;

        public bool CurrentRoundResultIsNull => query.CurrentRoundResultIsNull;

        public string TurnPhaseName => query.TurnPhaseName;

        public bool IsInteractionLocked => query.IsInteractionLocked;

        public string CurrentTurnName => query.CurrentTurnName;

        public int TurnIndex => query.TurnIndex;

        public string CurrentTurnPlayerIdName => query.CurrentTurnPlayerIdName;

        public string[] ActiveTurnSeatNames => query.ActiveTurnSeatNames;

        public string[] ActiveSeatNames => query.ActiveSeatNames;
    }
}
