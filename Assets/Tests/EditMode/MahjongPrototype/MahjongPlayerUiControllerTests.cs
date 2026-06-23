using System;
using System.Collections;
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
        private const string MahjongDiscardRiverViewTypeName = "MahjongPrototype.UI.MahjongDiscardRiverView, Assembly-CSharp";
        private const string MahjongDrawnTileViewTypeName = "MahjongPrototype.UI.MahjongDrawnTileView, Assembly-CSharp";
        private const string MahjongPlayerUiControllerTypeName = "MahjongPrototype.UI.MahjongPlayerUiController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

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

                Invoke(controller, "RenderDiscardRiver", gameState, ParseSeat("North"));

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

                Invoke(controller, "RenderDrawnTile", gameState, ParseSeat("North"));

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

                Invoke(controller, "RenderDrawnTile", gameState, ParseSeat("North"));
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
            object drawnTileView = null)
        {
            Type controllerType = Type.GetType(MahjongPlayerUiControllerTypeName, true);
            object controller = root.AddComponent(controllerType);

            SetField(controller, "viewSlot", ParseViewSlot(viewSlot));
            SetField(controller, "discardRiverView", discardRiverView);
            SetField(controller, "drawnTileView", drawnTileView);
            Invoke(controller, "ConfigureMissingViews", null, discardRiverView, drawnTileView);
            return controller;
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
            object gameState = Activator.CreateInstance(gameStateType, wall, CreateSeatList(selfSeat));
            Invoke(gameState, "SetSelfSeat", ParseSeat(selfSeat));
            Invoke(gameState, "SetActiveSeats", CreateSeatList(selfSeat));
            return gameState;
        }

        private static void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
            Invoke(playerSeat, "SetDrawnTile", CreateTile(tileCode));
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
