using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class DiscardServiceTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string MahjongGameStateTypeName = "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string DiscardServiceTypeName = "MahjongPrototype.Services.DiscardService, Assembly-CSharp";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";

        [Test]
        public void DiscardTile_RecordsHandSourceAndExistingFields()
        {
            object gameState = CreateGameState("East");
            AddHandTile(gameState, "East", "1m");
            object service = CreateDiscardService();

            object result = Invoke(service, "DiscardTile", gameState, ParseSeat("East"), 0);
            object record = GetProperty(result, "Record");

            Assert.That(GetProperty(record, "Source").ToString(), Is.EqualTo("Hand"));
            Assert.That(GetProperty(record, "ActorSeat").ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(record, "Tile").ToString(), Is.EqualTo("1m"));
            Assert.That(GetProperty(record, "TurnIndex"), Is.EqualTo(1));
        }

        [Test]
        public void DiscardDrawnTile_RecordsDrawnTileSourceAndExistingFields()
        {
            object gameState = CreateGameState("East");
            object playerSeat = GetPlayerSeat(gameState, "East");
            Invoke(playerSeat, "SetDrawnTile", CreateTile("2m"));
            object service = CreateDiscardService();

            object result = Invoke(service, "DiscardDrawnTile", gameState, ParseSeat("East"));
            object record = GetProperty(result, "Record");

            Assert.That(GetProperty(record, "Source").ToString(), Is.EqualTo("DrawnTile"));
            Assert.That(GetProperty(record, "ActorSeat").ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(record, "Tile").ToString(), Is.EqualTo("2m"));
            Assert.That(GetProperty(record, "TurnIndex"), Is.EqualTo(1));
        }

        [Test]
        public void MultipleDiscards_PreserveOrder()
        {
            object gameState = CreateGameState("East");
            AddHandTile(gameState, "East", "1m");
            AddHandTile(gameState, "East", "2m");
            object service = CreateDiscardService();

            Invoke(service, "DiscardTile", gameState, ParseSeat("East"), 0);
            Invoke(service, "DiscardTile", gameState, ParseSeat("East"), 0);

            object discards = GetProperty(gameState, "Discards");
            Assert.That((int)GetProperty(discards, "Count"), Is.EqualTo(2));
            Assert.That(GetProperty(GetListItem(discards, 0), "Tile").ToString(), Is.EqualTo("1m"));
            Assert.That(GetProperty(GetListItem(discards, 1), "Tile").ToString(), Is.EqualTo("2m"));
        }

        [Test]
        public void RetryPrototype_ClearsDiscards()
        {
            GameObject gameObject = new GameObject("RetryClearsDiscardsTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject);
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                Invoke(gameFlow, "RequestDraw");
                Invoke(gameFlow, "RequestDiscard", 0);
                Assert.That((int)GetProperty(GetProperty(gameState, "Discards"), "Count"), Is.EqualTo(1));

                Invoke(gameFlow, "RetryPrototype");
                object newGameState = GetProperty(gameFlow, "CurrentState");

                Assert.That((int)GetProperty(GetProperty(newGameState, "Discards"), "Count"), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object AddConfiguredGameFlow(GameObject gameObject)
        {
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "enableDevLog", false);
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "initialHandTileCount", 1);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            return gameFlow;
        }

        private static object CreateDiscardService()
        {
            return Activator.CreateInstance(Type.GetType(DiscardServiceTypeName, true));
        }

        private static object CreateGameState(params string[] seatNames)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { 12345 });
            return Activator.CreateInstance(gameStateType, wall, CreateSeatList(seatNames));
        }

        private static void AddHandTile(object gameState, string seatName, string tileCode)
        {
            object playerSeat = GetPlayerSeat(gameState, seatName);
            object hand = GetProperty(playerSeat, "Hand");
            Invoke(hand, "Add", CreateTile(tileCode));
        }

        private static object GetPlayerSeat(object gameState, string seatName)
        {
            return Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
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
            Type seatIdType = Type.GetType(SeatIdTypeName, true);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(seatIdType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(ParseSeat(seatNames[i]));

            return list;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }
    }
}
