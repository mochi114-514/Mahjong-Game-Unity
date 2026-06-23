using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongHandBoardViewTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string PlayerIdTypeName = "MahjongPrototype.Domain.PlayerId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName = "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string TileButtonViewTypeName = "MahjongPrototype.UI.TileButtonView, Assembly-CSharp";
        private const string MahjongHandViewTypeName = "MahjongPrototype.UI.MahjongHandView, Assembly-CSharp";
        private const string MahjongHandBoardViewTypeName = "MahjongPrototype.UI.MahjongHandBoardView, Assembly-CSharp";
        private const string TextMeshProUguiTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

        [Test]
        public void Render_WithOnlySelfBottomView_RendersSelfSeatHand()
        {
            GameObject root = new GameObject("HandBoardSelfBottomTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                AddHandTile(gameState, "North", "8m");

                object boardView = CreateBoardView(root, prefab, out object handView, out RectTransform container);

                Invoke(boardView, "Render", gameState, CreateSeatList("North"), true);

                Assert.That(container.childCount, Is.EqualTo(1));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("8m"));
                Assert.That(GetTileButton(container.GetChild(0)).interactable, Is.True);
                Assert.That(GetProperty(handView, "DataSeat").ToString(), Is.EqualTo("North"));
                Assert.That(GetProperty(handView, "ViewSlot").ToString(), Is.EqualTo("SelfBottom"));
                Assert.That(GetProperty(handView, "FaceUp"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Render_UsesDisplaySeatsInsteadOfActiveTurnSeats()
        {
            GameObject root = new GameObject("HandBoardDisplaySeatsTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                Invoke(gameState, "SetActiveSeats", CreateSeatList("East"));
                AddHandTile(gameState, "North", "8m");

                object boardView = CreateBoardView(root, prefab, out object handView, out RectTransform container);

                Invoke(boardView, "Render", gameState, CreateSeatList("North"), true);

                Assert.That(container.childCount, Is.EqualTo(1));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("8m"));
                Assert.That(GetProperty(handView, "DataSeat").ToString(), Is.EqualTo("North"));
                Assert.That(GetProperty(handView, "ViewSlot").ToString(), Is.EqualTo("SelfBottom"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase("East")]
        [TestCase("South")]
        [TestCase("West")]
        [TestCase("North")]
        public void OccupiedSeats_ReturnsSelfSeatOnlyInSinglePlayerState(string selfSeat)
        {
            object gameState = CreateGameState(selfSeat);

            AssertSeatList(GetProperty(gameState, "OccupiedSeats"), selfSeat);
            AssertSeatList(Invoke(gameState, "GetOccupiedSeats"), selfSeat);
        }

        [Test]
        public void OccupiedSeats_ReturnsOccupiedSeatSlotsInWindOrder()
        {
            object gameState = CreateGameState("South");

            Invoke(gameState, "AssignPlayerToSeat", ParsePlayerId("Player4"), ParseSeat("East"));
            Invoke(gameState, "AssignPlayerToSeat", ParsePlayerId("Player2"), ParseSeat("West"));
            Invoke(gameState, "AssignPlayerToSeat", ParsePlayerId("Player3"), ParseSeat("North"));

            AssertSeatList(GetProperty(gameState, "OccupiedSeats"), "East", "South", "West", "North");
        }

        [Test]
        public void OccupiedSeats_UpdatesWhenSelfSeatMoves()
        {
            object gameState = CreateGameState("South");

            Invoke(gameState, "SetSelfSeat", ParseSeat("West"));

            AssertSeatList(GetProperty(gameState, "OccupiedSeats"), "West");
        }

        private static object CreateBoardView(
            GameObject root,
            GameObject prefab,
            out object handView,
            out RectTransform container)
        {
            Type handViewType = Type.GetType(MahjongHandViewTypeName, true);
            Type boardViewType = Type.GetType(MahjongHandBoardViewTypeName, true);

            handView = root.AddComponent(handViewType);
            object boardView = root.AddComponent(boardViewType);

            GameObject containerObject = new GameObject("SelfBottomHandContainer", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(root.transform, false);

            object tileButtonPrefab = prefab.GetComponent(Type.GetType(TileButtonViewTypeName, true));
            Invoke(handView, "Configure", container, tileButtonPrefab);
            Invoke(boardView, "ConfigureMissingReferences", handView);
            return boardView;
        }

        private static object CreateGameState(string selfSeat)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { null });
            object gameState = Activator.CreateInstance(gameStateType, wall, CreateSeatList(selfSeat));
            Invoke(gameState, "SetSelfSeat", ParseSeat(selfSeat));
            Invoke(gameState, "SetActiveSeats", CreateSeatList(selfSeat));
            return gameState;
        }

        private static void AddHandTile(object gameState, string seatName, string tileCode)
        {
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
            object hand = GetProperty(playerSeat, "Hand");
            Invoke(hand, "Add", CreateTile(tileCode));
        }

        private static GameObject CreateTileButtonPrefab()
        {
            GameObject prefab = new GameObject("TileButtonPrefab", typeof(RectTransform));
            prefab.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 45f);
            prefab.AddComponent<Image>();
            prefab.AddComponent<Button>();

            GameObject label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(prefab.transform, false);
            label.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));

            prefab.AddComponent(Type.GetType(TileButtonViewTypeName, true));
            return prefab;
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

        private static object ParsePlayerId(string playerId)
        {
            return Enum.Parse(Type.GetType(PlayerIdTypeName, true), playerId);
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static string GetTileLabelText(Transform tileTransform)
        {
            Type textType = Type.GetType(TmpTextTypeName, true);
            Component textComponent = tileTransform.GetComponentInChildren(textType, true);
            Assert.That(textComponent, Is.Not.Null);

            PropertyInfo textProperty = textType.GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(textComponent);
        }

        private static Button GetTileButton(Transform tileTransform)
        {
            Button button = tileTransform.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            return button;
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void AssertSeatList(object seats, params string[] expectedSeatNames)
        {
            PropertyInfo countProperty = seats.GetType().GetProperty("Count");
            PropertyInfo itemProperty = seats.GetType().GetProperty("Item");
            Assert.That(countProperty, Is.Not.Null);
            Assert.That(itemProperty, Is.Not.Null);

            Assert.That(countProperty.GetValue(seats), Is.EqualTo(expectedSeatNames.Length));
            for (int i = 0; i < expectedSeatNames.Length; i++)
            {
                object seat = itemProperty.GetValue(seats, new object[] { i });
                Assert.That(seat.ToString(), Is.EqualTo(expectedSeatNames[i]));
            }
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }
    }
}
