using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class MahjongGameStateTurnTestDriver
    {
        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object gameState;

        private MahjongGameStateTurnTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            object gameState)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.gameState = gameState;
        }

        public static MahjongGameStateTurnTestDriver Create(params string[] occupiedSeatNames)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new MahjongGameStateTurnTestDriver(
                reflection,
                collections,
                dataFactory,
                dataFactory.CreateGameState(occupiedSeatNames));
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

        public bool IsWinDecisionPending =>
            (bool)reflection.GetProperty(gameState, "IsWinDecisionPending");

        public string WinDecisionSeatName =>
            reflection.GetProperty(gameState, "WinDecisionSeat").ToString();

        public string WinDecisionTypeName => NullablePropertyString("WinDecisionType");

        public string WinSourceSeatNameOrNull => NullablePropertyString("WinSourceSeat");

        public int WinDecisionTurnIndex =>
            (int)reflection.GetProperty(gameState, "WinDecisionTurnIndex");

        public string WinningTileCodeOrNull => NullablePropertyString("WinningTile");

        public string TurnPhaseName =>
            reflection.GetProperty(gameState, "TurnPhase").ToString();

        public bool IsInteractionLocked =>
            (bool)reflection.GetProperty(gameState, "IsInteractionLocked");

        public string CurrentTurnName =>
            reflection.GetProperty(gameState, "CurrentTurn").ToString();

        public int TurnIndex =>
            (int)reflection.GetProperty(gameState, "TurnIndex");

        public string CurrentTurnPlayerIdName =>
            reflection.GetProperty(gameState, "CurrentTurnPlayerId").ToString();

        public string[] ActiveTurnSeatNames =>
            SeatNames(reflection.GetProperty(gameState, "ActiveTurnSeats"));

        public string[] ActiveSeatNames =>
            SeatNames(reflection.GetProperty(gameState, "ActiveSeats"));

        private string[] SeatNames(object seats)
        {
            int count = collections.Count(seats);
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = collections.Item(seats, i).ToString();

            return names;
        }

        private string NullablePropertyString(string propertyName)
        {
            object value = reflection.GetProperty(gameState, propertyName);
            return value == null ? null : value.ToString();
        }
    }
}
