using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongUiCommandRouterTests
    {
        private const string SeatIdTypeName =
            "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string MahjongGameFlowTypeName =
            "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string MahjongEventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";
        private const string MahjongUiCommandRouterTypeName =
            "MahjongPrototype.UI.MahjongUiCommandRouter, Assembly-CSharp";
        private const string MahjongUiInputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string Mahjong3DPlayerAreaPresenterTypeName =
            "MahjongPrototype.UI3D.Mahjong3DPlayerAreaPresenter, Assembly-CSharp";

        [Test]
        public void RefreshSubscriptions_DoesNotSubscribeDirectlyTo3DTileClicks()
        {
            GameObject root = new GameObject("CommandRouter3DSubscriptionTest");
            root.SetActive(false);
            try
            {
                object gameFlow = CreateConfiguredGameFlow(root);
                object inputController = root.AddComponent(Type.GetType(MahjongUiInputControllerTypeName, true));
                object presenter = root.AddComponent(Type.GetType(Mahjong3DPlayerAreaPresenterTypeName, true));
                object router = root.AddComponent(Type.GetType(MahjongUiCommandRouterTypeName, true));
                SetPrivateField(router, "gameFlow", gameFlow);
                SetPrivateField(router, "inputController", inputController);

                Invoke(router, "RefreshSubscriptions");
                Invoke(router, "RefreshSubscriptions");

                Assert.That(CountEventSubscriberTarget(presenter, "HandTileClicked", router), Is.Zero);
                Assert.That(CountEventSubscriberTarget(presenter, "DrawnTileClicked", router), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConfirmedHandTileSelection_RoutesDiscardCommand()
        {
            GameObject root = new GameObject("CommandRouter3DHandDiscardTest");
            root.SetActive(false);
            try
            {
                object gameFlow = CreateConfiguredGameFlow(root);
                object inputController = root.AddComponent(Type.GetType(MahjongUiInputControllerTypeName, true));
                object presenter = root.AddComponent(Type.GetType(Mahjong3DPlayerAreaPresenterTypeName, true));
                object router = root.AddComponent(Type.GetType(MahjongUiCommandRouterTypeName, true));
                SetPrivateField(router, "gameFlow", gameFlow);
                SetPrivateField(router, "inputController", inputController);
                Invoke(router, "RefreshSubscriptions");

                Invoke(gameFlow, "StartNewRound");
                Invoke(gameFlow, "RequestDraw");
                object gameState = GetProperty(gameFlow, "CurrentState");

                bool accepted = (bool)Invoke(
                    router,
                    "TryDiscardHandFromTileSelection",
                    ParseSeat("East"),
                    0);

                Assert.That(accepted, Is.True);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConfirmedDrawnTileSelection_RoutesDrawnTileDiscardCommand()
        {
            GameObject root = new GameObject("CommandRouter3DDrawnDiscardTest");
            root.SetActive(false);
            try
            {
                object gameFlow = CreateConfiguredGameFlow(root);
                object inputController = root.AddComponent(Type.GetType(MahjongUiInputControllerTypeName, true));
                object presenter = root.AddComponent(Type.GetType(Mahjong3DPlayerAreaPresenterTypeName, true));
                object router = root.AddComponent(Type.GetType(MahjongUiCommandRouterTypeName, true));
                SetPrivateField(router, "gameFlow", gameFlow);
                SetPrivateField(router, "inputController", inputController);
                Invoke(router, "RefreshSubscriptions");

                Invoke(gameFlow, "StartNewRound");
                Invoke(gameFlow, "RequestDraw");
                object gameState = GetProperty(gameFlow, "CurrentState");

                bool accepted = (bool)Invoke(router, "TryDiscardDrawnTileFromTileSelection");

                Assert.That(accepted, Is.True);
                object firstDiscard = GetListItem(GetProperty(gameState, "Discards"), 0);
                Assert.That(GetProperty(firstDiscard, "Source").ToString(), Is.EqualTo("DrawnTile"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ConfirmedHandSelection_DuringTsumoDecision_UsesTheSingleDiscardIntent()
        {
            GameObject root = new GameObject("CommandRouter3DImplicitDecisionDiscardTest");
            root.SetActive(false);
            try
            {
                object gameFlow = CreateConfiguredGameFlow(root);
                object inputController = root.AddComponent(Type.GetType(MahjongUiInputControllerTypeName, true));
                object presenter = root.AddComponent(Type.GetType(Mahjong3DPlayerAreaPresenterTypeName, true));
                object router = root.AddComponent(Type.GetType(MahjongUiCommandRouterTypeName, true));
                SetPrivateField(router, "gameFlow", gameFlow);
                SetPrivateField(router, "inputController", inputController);
                Invoke(router, "RefreshSubscriptions");

                Invoke(gameFlow, "StartNewRound");
                Invoke(gameFlow, "RequestDraw");
                object gameState = GetProperty(gameFlow, "CurrentState");
                Invoke(
                    gameState,
                    "BeginWinDecision",
                    ParseSeat("East"),
                    (int)GetProperty(gameState, "TurnIndex"));

                bool accepted = (bool)Invoke(
                    router,
                    "TryDiscardHandFromTileSelection",
                    ParseSeat("East"),
                    0);

                Assert.That(accepted, Is.True);
                Assert.That((bool)GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetListCount(GetProperty(gameState, "Discards")), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject)
        {
            gameObject.AddComponent(Type.GetType(MahjongEventNotifierTypeName, true));
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "initialHandTileCount", 1);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            return gameFlow;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static void RaiseEvent(object target, string eventName, params object[] args)
        {
            Delegate handler = GetEventDelegate(target, eventName);
            Assert.That(handler, Is.Not.Null);
            handler.DynamicInvoke(args);
        }

        private static int CountEventSubscriberTarget(object target, string eventName, object subscriberTarget)
        {
            Delegate handler = GetEventDelegate(target, eventName);
            if (handler == null)
                return 0;

            int count = 0;
            Delegate[] delegates = handler.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                if (ReferenceEquals(delegates[i].Target, subscriberTarget))
                    count++;
            }

            return count;
        }

        private static Delegate GetEventDelegate(object target, string eventName)
        {
            FieldInfo field = target.GetType().GetField(
                eventName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(target) as Delegate;
        }

        private static int GetListCount(object list)
        {
            return (int)GetProperty(list, "Count");
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
    }
}
