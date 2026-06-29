using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongReachDecisionControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongReachDecisionController, Assembly-CSharp";
        private const string MahjongGameFlowTypeName =
            "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string MahjongPrototypeUiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string MahjongEventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";
        private const string SeatIdTypeName =
            "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName =
            "MahjongPrototype.Domain.Tile, Assembly-CSharp";

        [Test]
        public void SetVisibleTrue_OnInactiveReachDecisionArea_RemainsVisibleAfterOnEnable()
        {
            GameObject reachDecisionArea = new GameObject("ReachDecisionArea");
            GameObject reachButtonObject = new GameObject("ReachButton");
            GameObject declineButtonObject = new GameObject("DeclineReachButton");
            reachButtonObject.transform.SetParent(reachDecisionArea.transform);
            declineButtonObject.transform.SetParent(reachDecisionArea.transform);
            Button reachButton = reachButtonObject.AddComponent<Button>();
            Button declineButton = declineButtonObject.AddComponent<Button>();
            reachButton.interactable = true;
            declineButton.interactable = true;
            reachDecisionArea.SetActive(false);

            try
            {
                Component controller = reachDecisionArea.AddComponent(
                    Type.GetType(ControllerTypeName, true));

                Invoke(controller, "SetVisible", true);

                Assert.That(reachDecisionArea.activeSelf, Is.True);
                Assert.That(reachButton.interactable, Is.True);
                Assert.That(declineButton.interactable, Is.True);

                Invoke(controller, "OnEnable");

                Assert.That(reachDecisionArea.activeSelf, Is.True);
                Assert.That(reachButton.interactable, Is.True);
                Assert.That(declineButton.interactable, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reachDecisionArea);
            }
        }

        [Test]
        public void UiManagerRefreshReachDecision_ShowsSelfPendingReachDecisionArea()
        {
            GameObject gameObject = new GameObject("ReachDecisionUiManagerTest");
            GameObject uiObject = new GameObject("MahjongUiManager");
            GameObject reachDecisionArea = new GameObject("ReachDecisionArea");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject);
                object gameState = DrawReachableHand(gameFlow);

                uiObject.transform.SetParent(gameObject.transform);
                reachDecisionArea.transform.SetParent(uiObject.transform);
                reachDecisionArea.SetActive(false);
                Component reachDecisionController = reachDecisionArea.AddComponent(
                    Type.GetType(ControllerTypeName, true));
                Component uiManager = uiObject.AddComponent(
                    Type.GetType(MahjongPrototypeUiManagerTypeName, true));
                SetPrivateField(uiManager, "gameFlow", gameFlow);
                SetPrivateField(uiManager, "reachDecisionController", reachDecisionController);

                Invoke(uiManager, "RefreshReachDecision", gameState);

                Assert.That(GetProperty(gameState, "IsReachDecisionPending"), Is.True);
                Assert.That(reachDecisionArea.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object DrawReachableHand(object gameFlow)
        {
            Invoke(gameFlow, "StartNewRound");
            object gameState = GetProperty(gameFlow, "CurrentState");
            object playerSeat = GetPlayerSeat(gameState, "East");
            AddHandTiles(
                playerSeat,
                "1m", "2m", "3m",
                "2p", "3p", "4p",
                "7s", "8s", "9s",
                "E", "E", "E",
                "5m");

            Invoke(gameFlow, "RequestForceDrawSkill", "6m");
            Invoke(gameFlow, "RequestDraw");
            return gameState;
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject)
        {
            gameObject.AddComponent(Type.GetType(MahjongEventNotifierTypeName, true));
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            return gameFlow;
        }

        private static void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                Invoke(hand, "Add", CreateTile(tileCodes[i]));
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
    }
}
