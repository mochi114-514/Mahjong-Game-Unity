using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongUiDisplayControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongUiDisplayController, Assembly-CSharp";
        private const string SeatIdTypeName =
            "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string WallTypeName =
            "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName =
            "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        [Test]
        public void Refresh_WithAssignedTexts_UpdatesLabelsEvenWhenObjectNamesDiffer()
        {
            GameObject root = new GameObject("DisplayControllerTest");
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Labels labels = CreateLabels(root.transform);
                AssignLabels(controller, labels);
                object gameState = CreateGameState("East");

                Invoke(controller, "Refresh", gameState);

                Assert.That(GetProperty(labels.CurrentTurnText, "text"), Is.EqualTo("CurrentTurn: East"));
                Assert.That(GetProperty(labels.TurnIndexText, "text"), Is.EqualTo("Turn: 1"));
                Assert.That(GetProperty(labels.WallCountText, "text").ToString(), Does.StartWith("Wall: "));
                Assert.That(GetProperty(labels.ActiveSkillText, "text"), Is.EqualTo("Skill: none"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Refresh_WithoutAssignedTexts_WarnsAndDoesNotAutoFindNamedChildren()
        {
            GameObject root = new GameObject("DisplayControllerMissingTextTest");
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Component currentTurnChild = CreateLabel(root.transform, "CurrentTurnText");
                object gameState = CreateGameState("East");

                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongUiDisplayController: One or more status TMP_Text references are not assigned.");

                Invoke(controller, "Refresh", gameState);

                Assert.That(GetProperty(currentTurnChild, "text"), Is.Not.EqualTo("CurrentTurn: East"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Labels CreateLabels(Transform parent)
        {
            return new Labels
            {
                CurrentTurnText = CreateLabel(parent, "RenamedCurrentTurn"),
                TurnIndexText = CreateLabel(parent, "RenamedTurnIndex"),
                WallCountText = CreateLabel(parent, "RenamedWallCount"),
                ActiveSkillText = CreateLabel(parent, "RenamedActiveSkill")
            };
        }

        private static Component CreateLabel(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
        }

        private static void AssignLabels(Component controller, Labels labels)
        {
            SetPrivateField(controller, "currentTurnText", labels.CurrentTurnText);
            SetPrivateField(controller, "turnIndexText", labels.TurnIndexText);
            SetPrivateField(controller, "wallCountText", labels.WallCountText);
            SetPrivateField(controller, "activeSkillText", labels.ActiveSkillText);
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
            Type playerIdType = Type.GetType("MahjongPrototype.Domain.PlayerId, Assembly-CSharp", true);
            for (int i = 0; i < seatNames.Length; i++)
            {
                Invoke(
                    gameState,
                    "AssignPlayerToSeat",
                    Enum.Parse(playerIdType, $"Player{i + 1}"),
                    ParseSeat(seatNames[i]));
            }

            Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
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

        private sealed class Labels
        {
            public Component CurrentTurnText;
            public Component TurnIndexText;
            public Component WallCountText;
            public Component ActiveSkillText;
        }
    }
}
