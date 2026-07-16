using System;
using System.Collections.Generic;
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
            object[] invocationArgs = args ?? Array.Empty<object>();
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            List<MethodInfo> matchingMethods = new List<MethodInfo>();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName || method.ContainsGenericParameters ||
                    !AreRuntimeArgumentsCompatible(method.GetParameters(), invocationArgs))
                {
                    continue;
                }

                matchingMethods.Add(method);
            }

            if (matchingMethods.Count == 0)
            {
                Assert.Fail(
                    $"Method not found: {target.GetType().FullName}.{methodName}" +
                    $"({DescribeArgumentTypes(invocationArgs)}).");
                return null;
            }
            if (matchingMethods.Count != 1)
            {
                Assert.Fail(
                    $"Ambiguous method match: {target.GetType().FullName}.{methodName}" +
                    $"({DescribeArgumentTypes(invocationArgs)}). Candidates: " +
                    DescribeMethods(matchingMethods));
                return null;
            }

            return matchingMethods[0].Invoke(target, invocationArgs);
        }

        private static bool AreRuntimeArgumentsCompatible(
            ParameterInfo[] parameters,
            object[] arguments)
        {
            if (parameters.Length != arguments.Length)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                    return false;

                object argument = arguments[i];
                if (argument == null)
                {
                    if (parameterType.IsValueType &&
                        Nullable.GetUnderlyingType(parameterType) == null)
                    {
                        return false;
                    }

                    continue;
                }

                if (!parameterType.IsInstanceOfType(argument))
                    return false;
            }

            return true;
        }

        private static string DescribeArgumentTypes(object[] arguments)
        {
            string[] argumentTypes = new string[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                argumentTypes[i] = arguments[i]?.GetType().FullName ?? "null";

            return string.Join(", ", argumentTypes);
        }

        private static string DescribeMethods(IReadOnlyList<MethodInfo> methods)
        {
            string[] descriptions = new string[methods.Count];
            for (int i = 0; i < methods.Count; i++)
            {
                ParameterInfo[] parameters = methods[i].GetParameters();
                string[] parameterTypes = new string[parameters.Length];
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                    parameterTypes[parameterIndex] = parameters[parameterIndex].ParameterType.FullName;

                descriptions[i] = $"{methods[i].Name}({string.Join(", ", parameterTypes)})";
            }

            return string.Join("; ", descriptions);
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
