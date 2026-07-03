using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenEvaluatorTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string PlayerIdTypeName = "MahjongPrototype.Domain.PlayerId, Assembly-CSharp";
        private const string ParticipantTypeName =
            "MahjongPrototype.Domain.ParticipantType, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName =
            "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string DiscardRecordTypeName =
            "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string WinCheckerTypeName =
            "MahjongPrototype.Services.WinChecker, Assembly-CSharp";
        private const string FuritenEvaluatorTypeName =
            "MahjongPrototype.Services.FuritenEvaluator, Assembly-CSharp";

        private const string SingleWaitHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";
        private const string MultiWaitHand =
            "1p 2p 3p 1s 2s 3s E E E P P 4m 5m";
        private const string SevenPairsWaitHand =
            "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C";
        private const string KokushiThirteenWaitHand =
            "1m 9m 1p 9p 1s 9s E S W N P F C";
        private const string NonTenpaiHand =
            "1m 4m 7m 2p 5p 8p 3s 6s 9s E S W N";

        [Test]
        public void EvaluateAll_TwoPlayers_ReturnsTwoResults()
        {
            object gameState = CreateGameState("East", "South");

            object resultSet = EvaluateAll(gameState);

            Assert.That(GetProperty(resultSet, "Count"), Is.EqualTo(2));
            Assert.That(TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "South", out _), Is.True);
        }

        [Test]
        public void EvaluateAll_ThreePlayers_ReturnsThreeResults()
        {
            object gameState = CreateGameState("East", "South", "West");

            object resultSet = EvaluateAll(gameState);

            Assert.That(GetProperty(resultSet, "Count"), Is.EqualTo(3));
            Assert.That(TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "South", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "West", out _), Is.True);
        }

        [Test]
        public void EvaluateAll_FourPlayers_ReturnsFourResults()
        {
            object gameState = CreateGameState("East", "South", "West", "North");

            object resultSet = EvaluateAll(gameState);

            Assert.That(GetProperty(resultSet, "Count"), Is.EqualTo(4));
            Assert.That(TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "South", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "West", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "North", out _), Is.True);
        }

        [Test]
        public void EvaluateAll_ExcludesEmptySeats()
        {
            object gameState = CreateGameState("East", "North");

            object resultSet = EvaluateAll(gameState);

            Assert.That(GetProperty(resultSet, "Count"), Is.EqualTo(2));
            Assert.That(TryGetSeatResult(resultSet, "East", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "North", out _), Is.True);
            Assert.That(TryGetSeatResult(resultSet, "South", out _), Is.False);
            Assert.That(TryGetSeatResult(resultSet, "West", out _), Is.False);
        }

        [Test]
        public void EvaluateAll_EvaluatesParticipantTypesWithSameRules()
        {
            object gameState = CreateGameState("East", "South", "West");
            SetParticipantType(gameState, "West", "RemoteHuman");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddHandTiles(gameState, "South", SingleWaitHand);
            AddHandTiles(gameState, "West", SingleWaitHand);
            AddDiscard(gameState, "East", "C", 1);
            AddDiscard(gameState, "South", "C", 2);
            AddDiscard(gameState, "West", "C", 3);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, true);
            AssertSeatResult(resultSet, "South", true, true, true);
            AssertSeatResult(resultSet, "West", true, true, true);
        }

        [Test]
        public void EvaluateAll_TenpaiAndOwnDiscardContainsWait_IsDiscardFuriten()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddDiscard(gameState, "East", "C", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, true);
        }

        [Test]
        public void EvaluateAll_TenpaiButOwnDiscardDoesNotContainWait_IsNotFuriten()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddDiscard(gameState, "East", "9m", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, false);
        }

        [Test]
        public void EvaluateAll_OtherDiscardOnlyDoesNotCauseFuriten()
        {
            object gameState = CreateGameState("East", "South");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddDiscard(gameState, "South", "C", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, false);
        }

        [Test]
        public void EvaluateAll_SameStateCanDifferBySeat()
        {
            object gameState = CreateGameState("East", "South");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddHandTiles(gameState, "South", SingleWaitHand);
            AddDiscard(gameState, "East", "C", 1);
            AddDiscard(gameState, "South", "9m", 2);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, true);
            AssertSeatResult(resultSet, "South", true, true, false);
        }

        [Test]
        public void EvaluateAll_MultiWaitWithOneOwnDiscardedWait_IsDiscardFuriten()
        {
            Assert.That(CanWinWithTile(MultiWaitHand, "3m"), Is.True);
            Assert.That(CanWinWithTile(MultiWaitHand, "6m"), Is.True);
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", MultiWaitHand);
            AddDiscard(gameState, "East", "3m", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, true);
        }

        [Test]
        public void EvaluateAll_MultiWaitWithoutOwnDiscardedWait_IsNotFuriten()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", MultiWaitHand);
            AddDiscard(gameState, "East", "C", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, false);
        }

        [Test]
        public void EvaluateAll_NotTenpai_IsNotFuriten()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", NonTenpaiHand);
            AddDiscard(gameState, "East", "E", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, false, false);
        }

        [Test]
        public void EvaluateAll_SevenPairsWaitCanBeDiscardFuriten()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", SevenPairsWaitHand);
            AddDiscard(gameState, "East", "C", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, true);
        }

        [Test]
        public void EvaluateAll_ThirteenOrphansWaitCanBeDiscardFuriten()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", KokushiThirteenWaitHand);
            AddDiscard(gameState, "East", "E", 1);

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", true, true, true);
        }

        [Test]
        public void EvaluateAll_TwelveTileHand_IsNotEvaluated()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E");

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", false, false, false);
        }

        [Test]
        public void EvaluateAll_FourteenTileHand_IsNotEvaluated()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", SingleWaitHand + " 1m");

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", false, false, false);
        }

        [Test]
        public void EvaluateAll_SeatWithDrawnTile_IsNotEvaluated()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", SingleWaitHand);
            Invoke(GetPlayerSeat(gameState, "East"), "SetDrawnTile", CreateTile("1m"));

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", false, false, false);
        }

        [Test]
        public void EvaluateAll_InvalidTileInHand_IsNotEvaluated()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E");
            AddHandTile(gameState, "East", CreateInvalidTile());

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", false, false, false);
        }

        [Test]
        public void EvaluateAll_FiveCopiesInHand_IsNotEvaluated()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(
                gameState,
                "East",
                "1m 1m 1m 1m 1m 2p 3p 4p 5s 6s 7s E E");

            object resultSet = EvaluateAll(gameState);

            AssertSeatResult(resultSet, "East", false, false, false);
        }

        [Test]
        public void EvaluateAll_NullGameState_ReturnsEmptyResultSet()
        {
            object resultSet = EvaluateAll(null);

            Assert.That(GetProperty(resultSet, "Count"), Is.EqualTo(0));
        }

        [Test]
        public void EvaluateAll_DoesNotChangeHandContentsOrOrder()
        {
            object gameState = CreateGameState("East");
            AddHandTiles(gameState, "East", MultiWaitHand);
            AddDiscard(gameState, "East", "3m", 1);
            string before = GetHandDisplayString(gameState, "East");

            EvaluateAll(gameState);

            Assert.That(GetHandDisplayString(gameState, "East"), Is.EqualTo(before));
        }

        [Test]
        public void EvaluateAll_DoesNotChangeDiscardHistory()
        {
            object gameState = CreateGameState("East", "South");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddDiscard(gameState, "South", "C", 1);
            AddDiscard(gameState, "East", "9m", 2);
            string before = SnapshotDiscards(gameState);

            EvaluateAll(gameState);

            Assert.That(SnapshotDiscards(gameState), Is.EqualTo(before));
        }

        [Test]
        public void EvaluateAll_DoesNotChangeTurnSeatsOrReachState()
        {
            object gameState = CreateGameState("East", "South");
            AddHandTiles(gameState, "East", SingleWaitHand);
            AddHandTiles(gameState, "South", SingleWaitHand);
            SetProperty(gameState, "CurrentTurn", ParseSeat("South"));
            SetProperty(gameState, "TurnIndex", 42);
            Invoke(GetPlayerSeat(gameState, "South"), "DeclareReach", 17);
            string before = SnapshotGameState(gameState);

            EvaluateAll(gameState);

            Assert.That(SnapshotGameState(gameState), Is.EqualTo(before));
        }

        private static object EvaluateAll(object gameState)
        {
            object evaluator = Activator.CreateInstance(Type.GetType(FuritenEvaluatorTypeName, true));
            return Invoke(evaluator, "EvaluateAll", gameState);
        }

        private static bool CanWinWithTile(string handText, string winningTileCode)
        {
            object winChecker = Activator.CreateInstance(Type.GetType(WinCheckerTypeName, true));
            return (bool)Invoke(
                winChecker,
                "CanWinWithTile",
                CreateTileArray(handText),
                CreateTile(winningTileCode));
        }

        private static object CreateGameState(params string[] seatNames)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { 12345 });
            object gameState = Activator.CreateInstance(gameStateType, wall);
            AssignPlayersToSeats(gameState, seatNames);
            return gameState;
        }

        private static void AssignPlayersToSeats(object gameState, string[] seatNames)
        {
            for (int i = 0; i < seatNames.Length; i++)
            {
                Invoke(
                    gameState,
                    "AssignPlayerToSeat",
                    ParsePlayerId($"Player{i + 1}"),
                    ParseSeat(seatNames[i]));
            }

            Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
        }

        private static void SetParticipantType(
            object gameState,
            string seatName,
            string participantTypeName)
        {
            Invoke(
                gameState,
                "SetParticipantType",
                ParseSeat(seatName),
                Enum.Parse(Type.GetType(ParticipantTypeName, true), participantTypeName));
        }

        private static void AddHandTiles(object gameState, string seatName, string handText)
        {
            string[] codes = SplitCodes(handText);
            for (int i = 0; i < codes.Length; i++)
                AddHandTile(gameState, seatName, CreateTile(codes[i]));
        }

        private static void AddHandTile(object gameState, string seatName, object tile)
        {
            object playerSeat = GetPlayerSeat(gameState, seatName);
            Invoke(GetProperty(playerSeat, "Hand"), "Add", tile);
        }

        private static void AddDiscard(
            object gameState,
            string seatName,
            string tileCode,
            int turnIndex)
        {
            object record = Activator.CreateInstance(
                Type.GetType(DiscardRecordTypeName, true),
                ParseSeat(seatName),
                CreateTile(tileCode),
                turnIndex);
            Invoke(gameState, "AddDiscard", record);
        }

        private static object GetPlayerSeat(object gameState, string seatName)
        {
            return Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
        }

        private static string GetHandDisplayString(object gameState, string seatName)
        {
            object hand = GetProperty(GetPlayerSeat(gameState, seatName), "Hand");
            return (string)Invoke(hand, "ToDisplayString");
        }

        private static void AssertSeatResult(
            object resultSet,
            string seatName,
            bool expectedIsEvaluated,
            bool expectedIsTenpai,
            bool expectedIsDiscardFuriten)
        {
            object result = GetSeatResult(resultSet, seatName);

            Assert.That(GetProperty(result, "IsEvaluated"), Is.EqualTo(expectedIsEvaluated));
            Assert.That(GetProperty(result, "IsTenpai"), Is.EqualTo(expectedIsTenpai));
            Assert.That(GetProperty(result, "IsDiscardFuriten"), Is.EqualTo(expectedIsDiscardFuriten));
            Assert.That(GetProperty(result, "IsFuriten"), Is.EqualTo(expectedIsDiscardFuriten));
        }

        private static object GetSeatResult(object resultSet, string seatName)
        {
            bool found = TryGetSeatResult(resultSet, seatName, out object result);
            Assert.That(found, Is.True);
            Assert.That(result, Is.Not.Null);
            return result;
        }

        private static bool TryGetSeatResult(
            object resultSet,
            string seatName,
            out object result)
        {
            MethodInfo method = resultSet.GetType().GetMethod("TryGet");
            Assert.That(method, Is.Not.Null);

            object[] args = { ParseSeat(seatName), null };
            bool found = (bool)method.Invoke(resultSet, args);
            result = args[1];
            return found;
        }

        private static Array CreateTileArray(string handText)
        {
            string[] codes = SplitCodes(handText);
            Type tileType = Type.GetType(TileTypeName, true);
            Array tiles = Array.CreateInstance(tileType, codes.Length);

            for (int i = 0; i < codes.Length; i++)
                tiles.SetValue(CreateTile(codes[i]), i);

            return tiles;
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object CreateInvalidTile()
        {
            return Activator.CreateInstance(Type.GetType(TileTypeName, true));
        }

        private static string SnapshotDiscards(object gameState)
        {
            object discards = GetProperty(gameState, "Discards");
            int count = (int)GetProperty(discards, "Count");
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
                snapshot += "|" + GetListItem(discards, i);

            return snapshot;
        }

        private static string SnapshotGameState(object gameState)
        {
            return string.Join(
                "|",
                GetProperty(gameState, "CurrentTurn"),
                GetProperty(gameState, "TurnIndex"),
                GetProperty(gameState, "IsRoundEnded"),
                GetProperty(gameState, "IsWinDecisionPending"),
                GetProperty(gameState, "IsReachDecisionPending"),
                GetProperty(gameState, "IsReachDiscardSelectionPending"),
                SnapshotSeatSlots(gameState),
                SnapshotReachState(gameState, "East"),
                SnapshotReachState(gameState, "South"));
        }

        private static string SnapshotSeatSlots(object gameState)
        {
            object seatSlots = GetProperty(gameState, "SeatSlots");
            int count = (int)GetProperty(seatSlots, "Count");
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
            {
                object slot = GetListItem(seatSlots, i);
                object playerId = GetProperty(slot, "PlayerId");
                object participantType = GetProperty(slot, "ParticipantType");
                snapshot += "|" +
                    GetProperty(slot, "Wind") + ":" +
                    (playerId == null ? "Empty" : playerId.ToString()) + ":" +
                    (participantType == null ? "None" : participantType.ToString());
            }

            return snapshot;
        }

        private static string SnapshotReachState(object gameState, string seatName)
        {
            object playerSeat = GetPlayerSeat(gameState, seatName);
            return seatName + ":" +
                GetProperty(playerSeat, "IsReachDeclared") + ":" +
                GetProperty(playerSeat, "ReachDeclaredTurnIndex");
        }

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            property.SetValue(target, value);
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static object ParsePlayerId(string playerId)
        {
            return Enum.Parse(Type.GetType(PlayerIdTypeName, true), playerId);
        }

        private static string[] SplitCodes(string handText)
        {
            return handText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
