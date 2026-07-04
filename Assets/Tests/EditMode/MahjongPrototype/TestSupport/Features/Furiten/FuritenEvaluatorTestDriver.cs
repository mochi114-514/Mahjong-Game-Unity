using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Furiten
{
    internal sealed class FuritenEvaluatorTestDriver
    {
        private const string FuritenEvaluatorTypeName =
            "MahjongPrototype.Services.FuritenEvaluator, Assembly-CSharp";
        private const string WinCheckerTypeName =
            "MahjongPrototype.Services.WinChecker, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;

        private FuritenEvaluatorTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
        }

        public static FuritenEvaluatorTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new FuritenEvaluatorTestDriver(reflection, collections, dataFactory);
        }

        public object CreateGameState(params string[] seatNames)
        {
            return dataFactory.CreateGameState(seatNames);
        }

        public void SetParticipantType(object gameState, string seatName, string participantTypeName)
        {
            dataFactory.SetParticipantType(gameState, seatName, participantTypeName);
        }

        public void AssignHand(object gameState, string seatName, params string[] tileCodes)
        {
            dataFactory.AddHandTiles(dataFactory.GetPlayerSeat(gameState, seatName), tileCodes);
        }

        public void AssignHandText(object gameState, string seatName, string handText)
        {
            dataFactory.AddHandTilesFromText(
                dataFactory.GetPlayerSeat(gameState, seatName),
                handText);
        }

        public void AddHandTile(object gameState, string seatName, object tile)
        {
            dataFactory.AddHandTile(dataFactory.GetPlayerSeat(gameState, seatName), tile);
        }

        public object CreateInvalidTile()
        {
            return dataFactory.CreateInvalidTile();
        }

        public void AddDiscard(object gameState, string seatName, string tileCode, int turnIndex)
        {
            dataFactory.AddDiscard(gameState, seatName, tileCode, turnIndex);
        }

        public void SetCurrentTurn(object gameState, string seatName)
        {
            dataFactory.SetCurrentTurn(gameState, seatName);
        }

        public void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            dataFactory.SetDrawnTile(gameState, seatName, tileCode);
        }

        public void DeclareReach(object gameState, string seatName, int turnIndex)
        {
            reflection.Invoke(dataFactory.GetPlayerSeat(gameState, seatName), "DeclareReach", turnIndex);
        }

        public void MarkTemporaryFuriten(object gameState, string seatName)
        {
            reflection.Invoke(dataFactory.GetPlayerSeat(gameState, seatName), "MarkTemporaryFuriten");
        }

        public void MarkReachPassFuriten(object gameState, string seatName)
        {
            reflection.Invoke(dataFactory.GetPlayerSeat(gameState, seatName), "MarkReachPassFuriten");
        }

        public void ClearTemporaryFuriten(object gameState, string seatName)
        {
            reflection.Invoke(dataFactory.GetPlayerSeat(gameState, seatName), "ClearTemporaryFuriten");
        }

        public object EvaluateAll(object gameState)
        {
            object evaluator = reflection.CreateInstance(reflection.RequireType(FuritenEvaluatorTypeName));
            return reflection.Invoke(evaluator, "EvaluateAll", gameState);
        }

        public object EvaluateSeat(object gameState, string seatName)
        {
            return GetSeatResult(EvaluateAll(gameState), seatName);
        }

        public bool CanWinWithTile(string[] handTiles, string winningTileCode)
        {
            object winChecker = reflection.CreateInstance(reflection.RequireType(WinCheckerTypeName));
            return (bool)reflection.Invoke(
                winChecker,
                "CanWinWithTile",
                dataFactory.CreateTileArray(handTiles),
                dataFactory.CreateTile(winningTileCode));
        }

        public int ResultCount(object resultSet)
        {
            return (int)reflection.GetProperty(resultSet, "Count");
        }

        public bool TryGetSeatResult(object resultSet, string seatName, out object result)
        {
            object[] args = { dataFactory.ParseSeat(seatName), null };
            bool found = (bool)reflection.Invoke(resultSet, "TryGet", args);
            result = args[1];
            return found;
        }

        public object GetSeatResult(object resultSet, string seatName)
        {
            bool found = TryGetSeatResult(resultSet, seatName, out object result);
            NUnit.Framework.Assert.That(found, NUnit.Framework.Is.True);
            NUnit.Framework.Assert.That(result, NUnit.Framework.Is.Not.Null);
            return result;
        }

        public bool IsEvaluated(object result)
        {
            return (bool)reflection.GetProperty(result, "IsEvaluated");
        }

        public bool IsTenpai(object result)
        {
            return (bool)reflection.GetProperty(result, "IsTenpai");
        }

        public bool IsDiscardFuriten(object result)
        {
            return (bool)reflection.GetProperty(result, "IsDiscardFuriten");
        }

        public bool IsTemporaryFuriten(object result)
        {
            return (bool)reflection.GetProperty(result, "IsTemporaryFuriten");
        }

        public bool IsReachPassFuriten(object result)
        {
            return (bool)reflection.GetProperty(result, "IsReachPassFuriten");
        }

        public bool IsFuriten(object result)
        {
            return (bool)reflection.GetProperty(result, "IsFuriten");
        }

        public bool IsSeatTemporaryFuriten(object gameState, string seatName)
        {
            return (bool)reflection.GetProperty(
                dataFactory.GetPlayerSeat(gameState, seatName),
                "IsTemporaryFuriten");
        }

        public bool IsSeatReachPassFuriten(object gameState, string seatName)
        {
            return (bool)reflection.GetProperty(
                dataFactory.GetPlayerSeat(gameState, seatName),
                "IsReachPassFuriten");
        }

        public string HandDisplayString(object gameState, string seatName)
        {
            return dataFactory.HandDisplayString(gameState, seatName);
        }

        public string DiscardSnapshot(object gameState)
        {
            object discards = reflection.GetProperty(gameState, "Discards");
            int count = collections.Count(discards);
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
                snapshot += "|" + collections.Item(discards, i);

            return snapshot;
        }

        public string GameStateSnapshot(object gameState)
        {
            return string.Join(
                "|",
                reflection.GetProperty(gameState, "CurrentTurn"),
                reflection.GetProperty(gameState, "TurnIndex"),
                reflection.GetProperty(gameState, "IsRoundEnded"),
                reflection.GetProperty(gameState, "IsWinDecisionPending"),
                reflection.GetProperty(gameState, "IsReachDecisionPending"),
                reflection.GetProperty(gameState, "IsReachDiscardSelectionPending"),
                SeatSlotsSnapshot(gameState),
                ReachStateSnapshot(gameState, "East"),
                ReachStateSnapshot(gameState, "South"));
        }

        private string SeatSlotsSnapshot(object gameState)
        {
            object seatSlots = reflection.GetProperty(gameState, "SeatSlots");
            int count = collections.Count(seatSlots);
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
            {
                object slot = collections.Item(seatSlots, i);
                object playerId = reflection.GetProperty(slot, "PlayerId");
                object participantType = reflection.GetProperty(slot, "ParticipantType");
                snapshot += "|" +
                    reflection.GetProperty(slot, "Wind") + ":" +
                    (playerId == null ? "Empty" : playerId.ToString()) + ":" +
                    (participantType == null ? "None" : participantType.ToString());
            }

            return snapshot;
        }

        private string ReachStateSnapshot(object gameState, string seatName)
        {
            object playerSeat = dataFactory.GetPlayerSeat(gameState, seatName);
            return seatName + ":" +
                reflection.GetProperty(playerSeat, "IsReachDeclared") + ":" +
                reflection.GetProperty(playerSeat, "ReachDeclaredTurnIndex") + ":" +
                reflection.GetProperty(playerSeat, "IsTemporaryFuriten") + ":" +
                reflection.GetProperty(playerSeat, "IsReachPassFuriten");
        }

    }
}
