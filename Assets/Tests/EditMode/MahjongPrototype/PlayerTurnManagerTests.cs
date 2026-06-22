using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerTurnManagerTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName = "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string TurnOrderServiceTypeName = "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";
        private const string PlayerTurnManagerTypeName = "MahjongPrototype.Services.PlayerTurnManager, Assembly-CSharp";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";

        [Test]
        public void InitializeRound_SetsCurrentSeatAndTurnIndex()
        {
            object gameState = CreateGameState("East", "South");
            object manager = CreatePlayerTurnManager();

            Invoke(manager, "InitializeRound", gameState, ParseSeat("South"));

            Assert.That(GetProperty(gameState, "CurrentSeat").ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
            Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDraw"));
        }

        [Test]
        public void EndTurnAndSelectNext_AdvancesSeatAndTurnIndex()
        {
            object activeSeats = CreateSeatList("East", "South", "West", "North");
            object gameState = CreateGameState("East", "South", "West", "North");
            object manager = CreatePlayerTurnManager();
            Invoke(manager, "InitializeRound", gameState, ParseSeat("East"));

            object nextSeat = Invoke(manager, "EndTurnAndSelectNext", gameState, activeSeats);

            Assert.That(nextSeat.ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "CurrentSeat").ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
        }

        [Test]
        public void EndTurnAndSelectNext_ReturnsSameSeat_WhenOnlyEastIsActive()
        {
            object activeSeats = CreateSeatList("East");
            object gameState = CreateGameState("East");
            object manager = CreatePlayerTurnManager();
            Invoke(manager, "InitializeRound", gameState, ParseSeat("East"));

            object nextSeat = Invoke(manager, "EndTurnAndSelectNext", gameState, activeSeats);

            Assert.That(nextSeat.ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(gameState, "CurrentSeat").ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
        }

        [Test]
        public void IsTurnActive_ReturnsTrueOnlyForCurrentSeat()
        {
            object gameState = CreateGameState("East", "South");
            object manager = CreatePlayerTurnManager();
            Invoke(manager, "InitializeRound", gameState, ParseSeat("East"));

            bool eastActive = (bool)Invoke(manager, "IsTurnActive", gameState, ParseSeat("East"));
            bool southActive = (bool)Invoke(manager, "IsTurnActive", gameState, ParseSeat("South"));

            Assert.That(eastActive, Is.True);
            Assert.That(southActive, Is.False);
        }

        [Test]
        public void RefreshPhaseFromState_UsesDrawnTileAsPhaseSource()
        {
            object gameState = CreateGameState("East");
            object manager = CreatePlayerTurnManager();
            object playerSeat = GetCurrentPlayerSeat(gameState);
            Invoke(manager, "InitializeRound", gameState, ParseSeat("East"));

            Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDraw"));

            Invoke(playerSeat, "SetDrawnTile", CreateTile("E"));
            Invoke(manager, "RefreshPhaseFromState", gameState);

            Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDiscard"));
        }

        [Test]
        public void GameFlow_UsesDrawnTileAsDiscardGuard()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);

                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                object manager = GetPrivateField(gameFlow, "playerTurnManager");

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDraw"));

                Invoke(gameFlow, "RequestDiscard", 0);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDraw"));

                Invoke(gameFlow, "RequestDraw");
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDiscard"));

                Invoke(gameFlow, "RequestDiscard", 0);
                playerSeat = GetCurrentPlayerSeat(gameState);

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
                Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDraw"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_StartNewRoundPlacesDrawnTile()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawStartTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                object manager = GetPrivateField(gameFlow, "playerTurnManager");

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDiscard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_SkipsWhenDrawnTileAlreadyExists()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawExistingTileTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object wall = GetProperty(gameState, "Wall");
                int wallCount = (int)GetProperty(wall, "Count");
                object drawnTile = GetProperty(GetCurrentPlayerSeat(gameState), "DrawnTile");

                Invoke(gameFlow, "StartTurn", GetProperty(gameState, "CurrentSeat"), GetProperty(gameState, "TurnIndex"));

                object playerSeat = GetCurrentPlayerSeat(gameState);
                Assert.That(GetProperty(playerSeat, "DrawnTile"), Is.EqualTo(drawnTile));
                Assert.That(GetProperty(wall, "Count"), Is.EqualTo(wallCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_SkipsWhenRoundEnded()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawRoundEndedTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                Invoke(playerSeat, "ClearDrawnTile");
                SetProperty(gameState, "IsRoundEnded", true);
                object wall = GetProperty(gameState, "Wall");
                int wallCount = (int)GetProperty(wall, "Count");

                Invoke(gameFlow, "StartTurn", GetProperty(gameState, "CurrentSeat"), GetProperty(gameState, "TurnIndex"));

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(wall, "Count"), Is.EqualTo(wallCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_SkipsDuringWinDecision()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawWinDecisionTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                Invoke(playerSeat, "ClearDrawnTile");
                Invoke(
                    gameFlow,
                    "SetWinDecisionPending",
                    true,
                    GetProperty(gameState, "CurrentSeat"),
                    GetProperty(gameState, "TurnIndex"));
                object wall = GetProperty(gameState, "Wall");
                int wallCount = (int)GetProperty(wall, "Count");

                Invoke(gameFlow, "StartTurn", GetProperty(gameState, "CurrentSeat"), GetProperty(gameState, "TurnIndex"));

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(wall, "Count"), Is.EqualTo(wallCount));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_DrawsAgainAfterDiscard()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawAfterDiscardTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                Invoke(gameFlow, "RequestDiscardDrawnTile");

                object playerSeat = GetCurrentPlayerSeat(gameState);
                object manager = GetPrivateField(gameFlow, "playerTurnManager");
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
                Assert.That(GetProperty(manager, "Phase").ToString(), Is.EqualTo("WaitingForDiscard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AutoDraw_RetryStartsWithDrawnTile()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowAutoDrawRetryTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true);
                Invoke(gameFlow, "StartNewRound");

                Invoke(gameFlow, "RetryPrototype");

                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GameFlow_FixedSelfWindSetsStateWithoutChangingSinglePlayerSeat()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowFixedSelfWindTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false);
                SetPrivateField(gameFlow, "randomizeSelfWind", false);
                SetPrivateField(gameFlow, "fixedSelfWind", ParseSeat("South"));

                Invoke(gameFlow, "StartNewRound");

                object gameState = GetProperty(gameFlow, "CurrentState");
                object activeSeats = GetProperty(gameState, "ActiveSeats");
                PropertyInfo itemProperty = activeSeats.GetType().GetProperty("Item");
                Assert.That(itemProperty, Is.Not.Null);

                Assert.That(GetProperty(gameState, "SelfWind").ToString(), Is.EqualTo("South"));
                Assert.That(GetProperty(gameState, "CurrentSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(activeSeats, "Count"), Is.EqualTo(1));
                Assert.That(itemProperty.GetValue(activeSeats, new object[] { 0 }).ToString(), Is.EqualTo("East"));

                object seatSlots = GetProperty(gameState, "SeatSlots");
                Assert.That(GetProperty(seatSlots, "Count"), Is.EqualTo(4));
                AssertSeatSlot(seatSlots, 0, "East", false);
                AssertSeatSlot(seatSlots, 1, "South", true);
                AssertSeatSlot(seatSlots, 2, "West", false);
                AssertSeatSlot(seatSlots, 3, "North", false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object AddConfiguredGameFlow(GameObject gameObject, bool enableAutoDraw)
        {
            object gameFlow = gameObject.AddComponent(GetMahjongGameFlowType());
            SetPrivateField(gameFlow, "enableDevLog", false);
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "initialHandTileCount", 1);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", enableAutoDraw);
            return gameFlow;
        }

        private static object CreatePlayerTurnManager()
        {
            Type managerType = Type.GetType(PlayerTurnManagerTypeName, true);
            Type turnOrderServiceType = Type.GetType(TurnOrderServiceTypeName, true);
            object turnOrderService = Activator.CreateInstance(turnOrderServiceType);
            return Activator.CreateInstance(managerType, turnOrderService);
        }

        private static object CreateGameState(params string[] seatNames)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { null });
            return Activator.CreateInstance(gameStateType, wall, CreateSeatList(seatNames));
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType("MahjongPrototype.Domain.Tile, Assembly-CSharp", true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object GetCurrentPlayerSeat(object gameState)
        {
            MethodInfo method = gameState.GetType().GetMethod("GetPlayerSeat");
            Assert.That(method, Is.Not.Null);
            return method.Invoke(gameState, new[] { GetProperty(gameState, "CurrentSeat") });
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

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void AssertSeatSlot(object seatSlots, int index, string wind, bool hasSelfPlayer)
        {
            PropertyInfo itemProperty = seatSlots.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);

            object slot = itemProperty.GetValue(seatSlots, new object[] { index });
            Assert.That(GetProperty(slot, "Wind").ToString(), Is.EqualTo(wind));
            Assert.That(GetProperty(slot, "HasSelfPlayer"), Is.EqualTo(hasSelfPlayer));
            Assert.That(GetProperty(slot, "IsEmpty"), Is.EqualTo(!hasSelfPlayer));
            Assert.That(GetProperty(slot, "StateLabel"), Is.EqualTo(hasSelfPlayer ? "Self" : "Empty"));
        }

        private static IList CreateSeatList(params string[] seatNames)
        {
            Type seatIdType = GetSeatIdType();
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(seatIdType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(ParseSeat(seatNames[i]));

            return list;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(GetSeatIdType(), seatName);
        }

        private static Type GetSeatIdType()
        {
            return Type.GetType(SeatIdTypeName, true);
        }

        private static Type GetMahjongGameFlowType()
        {
            return Type.GetType(MahjongGameFlowTypeName, true);
        }
    }
}
