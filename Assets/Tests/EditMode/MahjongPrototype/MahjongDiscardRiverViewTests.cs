using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongDiscardRiverViewTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string DiscardRecordTypeName = "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string TileButtonViewTypeName = "MahjongPrototype.UI.TileButtonView, Assembly-CSharp";
        private const string MahjongDiscardRiverViewTypeName = "MahjongPrototype.UI.MahjongDiscardRiverView, Assembly-CSharp";
        private const string TextMeshProUguiTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

        [Test]
        public void Rebuild_CreatesOnlyEastTilesInDiscardOrder()
        {
            GameObject root = new GameObject("DiscardRiverViewTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);
                object discards = CreateDiscardList(
                    CreateDiscardRecord("East", "1m", 1),
                    CreateDiscardRecord("South", "2m", 2),
                    CreateDiscardRecord("East", "3m", 3));

                Invoke(view, "Rebuild", discards);

                Assert.That(container.childCount, Is.EqualTo(2));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("1m"));
                Assert.That(GetTileLabelText(container.GetChild(1)), Is.EqualTo("3m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Rebuild_WithNorthDataSeat_UsesSelfBottomRotation()
        {
            GameObject root = new GameObject("DiscardRiverNorthSelfBottomTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);

                Invoke(
                    view,
                    "Rebuild",
                    CreateDiscardList(
                        CreateDiscardRecord("East", "1m", 1),
                        CreateDiscardRecord("North", "9m", 2),
                        CreateDiscardRecord("North", "7p", 3)),
                    ParseSeat("North"));

                Assert.That(container.childCount, Is.EqualTo(2));
                Assert.That(GetTileLabelText(container.GetChild(0)), Is.EqualTo("9m"));
                Assert.That(GetTileLabelText(container.GetChild(1)), Is.EqualTo("7p"));

                RectTransform firstTile = (RectTransform)container.GetChild(0);
                Assert.That(firstTile.localEulerAngles.z, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Rebuild_PreservesPrefabSizeAndWrapsSeventhTile()
        {
            GameObject root = new GameObject("DiscardRiverPositionTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);

                Invoke(
                    view,
                    "Rebuild",
                    CreateDiscardList(
                        CreateDiscardRecord("East", "1m", 1),
                        CreateDiscardRecord("East", "2m", 2),
                        CreateDiscardRecord("East", "3m", 3),
                        CreateDiscardRecord("East", "4m", 4),
                        CreateDiscardRecord("East", "5m", 5),
                        CreateDiscardRecord("East", "6m", 6),
                        CreateDiscardRecord("East", "7m", 7)));

                Assert.That(container.childCount, Is.EqualTo(7));
                RectTransform firstTile = (RectTransform)container.GetChild(0);
                RectTransform seventhTile = (RectTransform)container.GetChild(6);
                Assert.That(firstTile.sizeDelta, Is.EqualTo(new Vector2(40f, 45f)));
                Assert.That(firstTile.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(seventhTile.sizeDelta, Is.EqualTo(new Vector2(40f, 45f)));
                Assert.That(seventhTile.anchoredPosition, Is.EqualTo(new Vector2(0f, -48f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Rebuild_DoesNotAddGridLayoutGroup()
        {
            GameObject root = new GameObject("DiscardRiverNoGridTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);

                Invoke(view, "Rebuild", CreateDiscardList(CreateDiscardRecord("East", "1m", 1)));

                Assert.That(container.GetComponent<GridLayoutGroup>(), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Rebuild_DisablesExistingGridLayoutGroup()
        {
            GameObject root = new GameObject("DiscardRiverExistingGridTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);
                GridLayoutGroup gridLayout = container.gameObject.AddComponent<GridLayoutGroup>();
                Assert.That(gridLayout.enabled, Is.True);

                Invoke(view, "Rebuild", CreateDiscardList(CreateDiscardRecord("East", "1m", 1)));

                Assert.That(gridLayout.enabled, Is.False);
                Assert.That(container.childCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Rebuild_ClearsPreviousTiles()
        {
            GameObject root = new GameObject("DiscardRiverClearTest");
            GameObject prefab = CreateTileButtonPrefab();
            try
            {
                object view = CreateView(root, prefab, out RectTransform container);

                Invoke(
                    view,
                    "Rebuild",
                    CreateDiscardList(
                        CreateDiscardRecord("East", "1m", 1),
                        CreateDiscardRecord("East", "2m", 2)));
                Assert.That(container.childCount, Is.EqualTo(2));

                Invoke(view, "Rebuild", CreateDiscardList());

                Assert.That(container.childCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateView(GameObject root, GameObject prefab, out RectTransform container)
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

        private static string GetTileLabelText(Transform tileTransform)
        {
            Type textType = Type.GetType(TmpTextTypeName, true);
            Component textComponent = tileTransform.GetComponentInChildren(textType, true);
            Assert.That(textComponent, Is.Not.Null);

            PropertyInfo textProperty = textType.GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(textComponent);
        }

        private static object CreateDiscardList(params object[] records)
        {
            Type recordType = Type.GetType(DiscardRecordTypeName, true);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(recordType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < records.Length; i++)
                list.Add(records[i]);

            return list;
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
