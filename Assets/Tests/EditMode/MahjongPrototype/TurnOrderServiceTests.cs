using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class TurnOrderServiceTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TurnOrderServiceTypeName = "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";

        [Test]
        public void GetNextSeat_ReturnsSameSeat_WhenOnlyOneSeatIsActive()
        {
            object result = GetNextSeat(CreateSeatList("East"), "East");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        [TestCase("East", "South")]
        [TestCase("South", "West")]
        [TestCase("West", "North")]
        [TestCase("North", "East")]
        public void GetNextSeat_ReturnsNextSeat_WhenMultipleSeatsAreActive(
            string currentSeat,
            string expectedSeat)
        {
            object result = GetNextSeat(CreateSeatList("East", "South", "West", "North"), currentSeat);

            Assert.That(result.ToString(), Is.EqualTo(expectedSeat));
        }

        [Test]
        public void GetNextSeat_ReturnsFirstActiveSeat_WhenCurrentSeatIsNotActive()
        {
            object result = GetNextSeat(CreateSeatList("East", "South"), "West");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        [Test]
        public void GetNextSeat_ReturnsEast_WhenActiveSeatsIsEmpty()
        {
            object result = GetNextSeat(CreateSeatList(), "East");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        [Test]
        public void GetNextSeat_ReturnsEast_WhenActiveSeatsIsNull()
        {
            object result = GetNextSeat(null, "East");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        private static object GetNextSeat(object activeSeats, string currentSeatName)
        {
            Type serviceType = Type.GetType(TurnOrderServiceTypeName, true);
            object service = Activator.CreateInstance(serviceType);
            MethodInfo method = serviceType.GetMethod("GetNextSeat");
            Assert.That(method, Is.Not.Null);

            return method.Invoke(service, new[] { activeSeats, ParseSeat(currentSeatName) });
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
    }

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
        public void GameFlow_UsesDrawnTileAsDiscardGuard()
        {
            GameObject gameObject = new GameObject("MahjongGameFlowTest");
            try
            {
                object gameFlow = gameObject.AddComponent(GetMahjongGameFlowType());
                SetPrivateField(gameFlow, "enableDevLog", false);
                SetPrivateField(gameFlow, "logWarnings", false);

                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");
                object playerSeat = GetCurrentPlayerSeat(gameState);

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);

                Invoke(gameFlow, "RequestDiscard", 0);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(1));
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);

                Invoke(gameFlow, "RequestDraw");
                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.True);

                Invoke(gameFlow, "RequestDiscard", 0);
                playerSeat = GetCurrentPlayerSeat(gameState);

                Assert.That(GetProperty(playerSeat, "HasDrawnTile"), Is.False);
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
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
