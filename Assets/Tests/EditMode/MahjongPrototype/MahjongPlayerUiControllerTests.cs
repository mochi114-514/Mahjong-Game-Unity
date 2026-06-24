using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongPlayerUiControllerTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string DiscardRecordTypeName = "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string MahjongGameStateTypeName = "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string ViewSlotTypeName = "MahjongPrototype.UI.ViewSlot, Assembly-CSharp";
        private const string TileButtonViewTypeName = "MahjongPrototype.UI.TileButtonView, Assembly-CSharp";
        private const string MahjongHandViewTypeName = "MahjongPrototype.UI.MahjongHandView, Assembly-CSharp";
        private const string MahjongDiscardRiverViewTypeName = "MahjongPrototype.UI.MahjongDiscardRiverView, Assembly-CSharp";
        private const string MahjongDrawnTileViewTypeName = "MahjongPrototype.UI.MahjongDrawnTileView, Assembly-CSharp";
        private const string MahjongSeatWindViewTypeName = "MahjongPrototype.UI.MahjongSeatWindView, Assembly-CSharp";
        private const string MahjongPlayerUiControllerTypeName = "MahjongPrototype.UI.MahjongPlayerUiController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

        [Test]
        public void RenderAndClearWind_DelegateToSeatWindView()
        {
            GameObject root = new GameObject("PlayerUiControllerSeatWindTest");
            try
            {
                object seatWindView = CreateSeatWindView(root, out Component windText);
                object controller = CreateController(
                    root,
                    null,
                    "SelfBottom",
                    seatWindView: seatWindView);

                Invoke(controller, "RenderWind", ParseSeat("North"));

                Assert.That(GetProperty(windText, "text"), Is.EqualTo("North"));

                Invoke(controller, "ClearWind");

                Assert.That(GetProperty(windText, "text"), Is.EqualTo(string.Empty));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderHand_DelegatesToHandViewWithSeatAndViewSlot()
        {
            GameObject root = new GameObject("PlayerUiControllerHandTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                AddHandTile(gameState, "North", "8m");

                object handView = CreateHandView(root, prefab, out RectTransform container);
                object controller = CreateController(root, null, "SelfBottom", null, handView);

                Invoke(controller, "RenderHand", GetHandTiles(gameState, "North"), ParseSeat("North"), true, true);

                Assert.That(container.childCount, Is.EqualTo(1));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("8m"));
                Assert.That(GetTileButton(container.GetChild(0)).interactable, Is.True);
                Assert.That(GetProperty(handView, "DataSeat").ToString(), Is.EqualTo("North"));
                Assert.That(GetProperty(handView, "ViewSlot").ToString(), Is.EqualTo("SelfBottom"));

                Invoke(controller, "SetHandInteractable", false);

                Assert.That(GetTileButton(container.GetChild(0)).interactable, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearHand_DelegatesToHandView()
        {
            GameObject root = new GameObject("PlayerUiControllerClearHandTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                AddHandTile(gameState, "North", "8m");

                object handView = CreateHandView(root, prefab, out RectTransform container);
                object controller = CreateController(root, null, "SelfBottom", null, handView);

                Invoke(controller, "RenderHand", GetHandTiles(gameState, "North"), ParseSeat("North"), true, true);
                Assert.That(container.childCount, Is.EqualTo(1));

                Invoke(controller, "ClearHand");

                Assert.That(container.childCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HandTileClicked_ForwardsRenderedSeatAndHandIndexOnce()
        {
            GameObject root = new GameObject("PlayerUiControllerHandClickTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                AddHandTile(gameState, "North", "8m");

                object handView = CreateHandView(root, prefab, out RectTransform container);
                object controller = CreateController(root, null, "SelfBottom", null, handView);
                int clickCount = 0;
                object clickedSeat = null;
                int clickedIndex = -1;
                EventInfo eventInfo = controller.GetType().GetEvent("HandTileClicked");
                Assert.That(eventInfo, Is.Not.Null);
                Delegate handler = CreateHandTileClickedHandler(
                    eventInfo,
                    (seat, handIndex) =>
                    {
                        clickCount++;
                        clickedSeat = seat;
                        clickedIndex = handIndex;
                    });
                eventInfo.AddEventHandler(controller, handler);

                Invoke(controller, "RenderHand", GetHandTiles(gameState, "North"), ParseSeat("North"), true, true);
                GetTileButton(container.GetChild(0)).onClick.Invoke();

                Assert.That(clickCount, Is.EqualTo(1));
                Assert.That(clickedSeat.ToString(), Is.EqualTo("North"));
                Assert.That(clickedIndex, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_DelegatesToDiscardRiverViewWithDataSeat()
        {
            GameObject root = new GameObject("PlayerUiControllerDiscardRiverTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                AddDiscard(gameState, "East", "1m", 1);
                AddDiscard(gameState, "North", "9m", 2);
                AddDiscard(gameState, "North", "7p", 3);

                object discardRiverView = CreateDiscardRiverView(root, prefab, out RectTransform container);
                object controller = CreateController(root, discardRiverView, "SelfBottom");

                Invoke(controller, "RenderDiscardRiver", GetProperty(gameState, "Discards"), ParseSeat("North"));

                Assert.That(container.childCount, Is.EqualTo(2));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("9m"));
                Assert.That(GetTileLabelText(container.GetChild(1)), Is.EqualTo("7p"));
                Assert.That(GetProperty(controller, "ViewSlot").ToString(), Is.EqualTo("SelfBottom"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearDiscardRiver_DelegatesToDiscardRiverView()
        {
            GameObject root = new GameObject("PlayerUiControllerClearDiscardRiverTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                AddDiscard(gameState, "North", "9m", 1);

                object discardRiverView = CreateDiscardRiverView(root, prefab, out RectTransform container);
                object controller = CreateController(root, discardRiverView, "SelfBottom");

                Invoke(controller, "RenderDiscardRiver", GetProperty(gameState, "Discards"), ParseSeat("North"));
                Assert.That(container.childCount, Is.EqualTo(1));

                Invoke(controller, "ClearDiscardRiver");

                Assert.That(container.childCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDrawnTile_DelegatesToDrawnTileViewWithDataSeat()
        {
            GameObject root = new GameObject("PlayerUiControllerDrawnTileTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                SetDrawnTile(gameState, "North", "5p");

                object drawnTileView = CreateDrawnTileView(root, prefab, out RectTransform container);
                object controller = CreateController(root, null, "SelfBottom", drawnTileView);

                Invoke(controller, "RenderDrawnTile", GetDrawnTile(gameState, "North"));

                Assert.That(container.childCount, Is.EqualTo(1));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("5p"));

                Invoke(controller, "SetDrawnTileInteractable", false);

                Assert.That(GetTileButton(container.GetChild(0)).interactable, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearDrawnTile_DelegatesToDrawnTileView()
        {
            GameObject root = new GameObject("PlayerUiControllerClearDrawnTileTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                SetDrawnTile(gameState, "North", "5p");

                object drawnTileView = CreateDrawnTileView(root, prefab, out RectTransform container);
                object controller = CreateController(root, null, "SelfBottom", drawnTileView);

                Invoke(controller, "RenderDrawnTile", GetDrawnTile(gameState, "North"));
                Assert.That(container.childCount, Is.EqualTo(1));

                Invoke(controller, "ClearDrawnTile");

                Assert.That(container.childCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DrawnTileClicked_ForwardsDrawnTileViewClick()
        {
            GameObject root = new GameObject("PlayerUiControllerDrawnTileClickTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object gameState = CreateGameState("North");
                SetDrawnTile(gameState, "North", "5p");

                object drawnTileView = CreateDrawnTileView(root, prefab, out RectTransform container);
                object controller = CreateController(root, null, "SelfBottom", drawnTileView);
                bool wasClicked = false;
                EventInfo eventInfo = controller.GetType().GetEvent("DrawnTileClicked");
                Assert.That(eventInfo, Is.Not.Null);
                eventInfo.AddEventHandler(controller, (Action)(() => wasClicked = true));

                Invoke(controller, "RenderDrawnTile", GetDrawnTile(gameState, "North"));
                GetTileButton(container.GetChild(0)).onClick.Invoke();

                Assert.That(wasClicked, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateController(
            GameObject root,
            object discardRiverView,
            string viewSlot,
            object drawnTileView = null,
            object handView = null,
            object seatWindView = null)
        {
            Type controllerType = Type.GetType(MahjongPlayerUiControllerTypeName, true);
            object controller = root.AddComponent(controllerType);

            SetField(controller, "viewSlot", ParseViewSlot(viewSlot));
            SetField(controller, "handView", handView);
            SetField(controller, "discardRiverView", discardRiverView);
            SetField(controller, "drawnTileView", drawnTileView);
            SetField(controller, "seatWindView", seatWindView);
            Invoke(controller, "SubscribeViewEvents");
            return controller;
        }

        private static object CreateSeatWindView(GameObject root, out Component windText)
        {
            Type viewType = Type.GetType(MahjongSeatWindViewTypeName, true);
            object view = root.AddComponent(viewType);

            GameObject labelObject = new GameObject("WindLabel", typeof(RectTransform));
            labelObject.transform.SetParent(root.transform, false);
            windText = labelObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
            SetField(view, "windText", windText);
            return view;
        }

        private static object CreateHandView(GameObject root, GameObject prefab, out RectTransform container)
        {
            Type viewType = Type.GetType(MahjongHandViewTypeName, true);
            object view = root.AddComponent(viewType);

            GameObject containerObject = new GameObject("HandContainer", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(root.transform, false);

            object tileButtonPrefab = prefab.GetComponent(Type.GetType(TileButtonViewTypeName, true));
            Invoke(view, "Configure", container, tileButtonPrefab);
            return view;
        }

        private static object CreateDiscardRiverView(GameObject root, GameObject prefab, out RectTransform container)
        {
            Type viewType = Type.GetType(MahjongDiscardRiverViewTypeName, true);
            object view = root.AddComponent(viewType);

            GameObject containerObject = new GameObject("SelfBottomDiscardRiverContainer", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(root.transform, false);

            object tileButtonPrefab = prefab.GetComponent(Type.GetType(TileButtonViewTypeName, true));
            Invoke(view, "Configure", container, tileButtonPrefab);
            return view;
        }

        private static object CreateDrawnTileView(GameObject root, GameObject prefab, out RectTransform container)
        {
            Type viewType = Type.GetType(MahjongDrawnTileViewTypeName, true);
            object view = root.AddComponent(viewType);

            GameObject containerObject = new GameObject("DrawnTileContainer", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(root.transform, false);

            object tileButtonPrefab = prefab.GetComponent(Type.GetType(TileButtonViewTypeName, true));
            Invoke(view, "Configure", container, tileButtonPrefab);
            return view;
        }

        private static object CreateGameState(string selfSeat)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { null });
            object gameState = Activator.CreateInstance(gameStateType, wall);
            Invoke(gameState, "SetSelfSeat", ParseSeat(selfSeat));
            Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
            return gameState;
        }

        private static void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
            Invoke(playerSeat, "SetDrawnTile", CreateTile(tileCode));
        }

        private static object GetDrawnTile(object gameState, string seatName)
        {
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
            return GetProperty(playerSeat, "DrawnTile");
        }

        private static void AddHandTile(object gameState, string seatName, string tileCode)
        {
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
            object hand = GetProperty(playerSeat, "Hand");
            Invoke(hand, "Add", CreateTile(tileCode));
        }

        private static object GetHandTiles(object gameState, string seatName)
        {
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
            object hand = GetProperty(playerSeat, "Hand");
            return Invoke(hand, "GetTiles");
        }

        private static void AddDiscard(object gameState, string actorSeat, string tileCode, int turnIndex)
        {
            Invoke(gameState, "AddDiscard", CreateDiscardRecord(actorSeat, tileCode, turnIndex));
        }

        private static object CreateDiscardRecord(string actorSeat, string tileCode, int turnIndex)
        {
            Type recordType = Type.GetType(DiscardRecordTypeName, true);
            ConstructorInfo constructor = recordType.GetConstructor(new[]
            {
                Type.GetType(SeatIdTypeName, true),
                Type.GetType(TileTypeName, true),
                typeof(int)
            });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new[] { ParseSeat(actorSeat), CreateTile(tileCode), turnIndex });
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

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static object ParseViewSlot(string viewSlot)
        {
            return Enum.Parse(Type.GetType(ViewSlotTypeName, true), viewSlot);
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

        private static Delegate CreateHandTileClickedHandler(
            EventInfo eventInfo,
            Action<object, int> callback)
        {
            Type delegateType = eventInfo.EventHandlerType;
            Assert.That(delegateType, Is.Not.Null);
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            Assert.That(invokeMethod, Is.Not.Null);
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(2));

            ParameterExpression seatParameter = Expression.Parameter(parameters[0].ParameterType, "seat");
            ParameterExpression handIndexParameter = Expression.Parameter(parameters[1].ParameterType, "handIndex");
            InvocationExpression callbackInvocation = Expression.Invoke(
                Expression.Constant(callback),
                Expression.Convert(seatParameter, typeof(object)),
                handIndexParameter);
            return Expression.Lambda(
                delegateType,
                callbackInvocation,
                seatParameter,
                handIndexParameter).Compile();
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = null;
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo candidate = methods[i];
                if (candidate.Name != methodName)
                    continue;

                if (candidate.GetParameters().Length != args.Length)
                    continue;

                method = candidate;
                break;
            }

            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }
    }
}
