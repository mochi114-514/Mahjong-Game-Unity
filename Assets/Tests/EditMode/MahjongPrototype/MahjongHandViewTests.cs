using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongHandViewTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string ViewSlotTypeName = "MahjongPrototype.UI.ViewSlot, Assembly-CSharp";
        private const string TileButtonViewTypeName = "MahjongPrototype.UI.TileButtonView, Assembly-CSharp";
        private const string MahjongHandViewTypeName = "MahjongPrototype.UI.MahjongHandView, Assembly-CSharp";
        private const string TextMeshProUguiTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

        [Test]
        public void Render_FaceUp_UsesTileTextAndInteractable()
        {
            GameObject root = new GameObject("HandViewFaceUpTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);

                Invoke(
                    view,
                    "Render",
                    CreateTileList(CreateTile("1m"), CreateTile("2m")),
                    ParseSeat("North"),
                    ParseViewSlot("SelfBottom"),
                    true,
                    true);

                Assert.That(container.childCount, Is.EqualTo(2));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("1m"));
                Assert.That(GetTileButton(container.GetChild(0)).interactable, Is.True);
                Assert.That(GetProperty(view, "DataSeat").ToString(), Is.EqualTo("North"));
                Assert.That(GetProperty(view, "FaceUp"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Render_FaceDown_HidesTileTextAndDisablesInteraction()
        {
            GameObject root = new GameObject("HandViewFaceDownTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);

                Invoke(
                    view,
                    "Render",
                    CreateTileList(CreateTile("9m")),
                    ParseSeat("East"),
                    ParseViewSlot("NextLeft"),
                    false,
                    true);

                Assert.That(container.childCount, Is.EqualTo(1));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.Empty);
                Assert.That(GetTileButton(container.GetChild(0)).interactable, Is.False);
                Assert.That(GetProperty(view, "ViewSlot").ToString(), Is.EqualTo("NextLeft"));
                Assert.That(GetProperty(view, "FaceUp"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateView(GameObject root, GameObject prefab, out RectTransform container)
        {
            Type viewType = Type.GetType(MahjongHandViewTypeName, true);
            object view = root.AddComponent(viewType);

            GameObject containerObject = new GameObject("SelfBottomHandContainer", typeof(RectTransform));
            container = containerObject.GetComponent<RectTransform>();
            container.SetParent(root.transform, false);

            object tileButtonPrefab = prefab.GetComponent(Type.GetType(TileButtonViewTypeName, true));
            Invoke(view, "Configure", container, tileButtonPrefab);
            return view;
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

        private static object CreateTileList(params object[] tiles)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(tileType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < tiles.Length; i++)
                list.Add(tiles[i]);

            return list;
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

        private static object ParseViewSlot(string slotName)
        {
            return Enum.Parse(Type.GetType(ViewSlotTypeName, true), slotName);
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
