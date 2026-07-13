using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DDrawnTileViewTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string Mahjong3DHandViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DHandView, Assembly-CSharp";
        private const string Mahjong3DDrawnTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DDrawnTileView, Assembly-CSharp";
        private const string Mahjong3DPlayerUiControllerTypeName =
            "MahjongPrototype.UI3D.Mahjong3DPlayerUiController, Assembly-CSharp";
        private const string Mahjong3DTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";

        [Test]
        public void SetReachCandidateInteractable_DimsWhenDrawnTileIsNotSelectable()
        {
            GameObject root = new GameObject("Drawn3DViewReachCandidateDimmedTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDrawnTileViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Invoke(view, "Render", CreateTile("1m"), true, true);
                Invoke(view, "SetReachCandidateInteractable", false);

                Component tileView = GetSingleTileView(root);
                Assert.That(GetProperty(tileView, "Interactable"), Is.False);
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetTileInteractable_ClearsReachCandidateDimming()
        {
            GameObject root = new GameObject("Drawn3DViewClearDimmedTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDrawnTileViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Invoke(view, "Render", CreateTile("1m"), true, true);
                Invoke(view, "SetReachCandidateInteractable", false);
                Invoke(view, "SetTileInteractable", true);

                Component tileView = GetSingleTileView(root);
                Assert.That(GetProperty(tileView, "Interactable"), Is.True);
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDrawnTile_FollowsTheHandEndAcrossDifferentSpawnRoots()
        {
            GameObject controllerRoot = new GameObject("PlayerUiControllerTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            GameObject handRoot = new GameObject("HandSpawnRoot");
            GameObject drawnRoot = new GameObject("DrawnSpawnRoot");
            GameObject handViewObject = new GameObject("HandView");
            GameObject drawnTileViewObject = new GameObject("DrawnTileView");
            try
            {
                handRoot.transform.position = new Vector3(10f, 5f, 2f);
                handRoot.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                drawnRoot.transform.position = new Vector3(-3f, 4f, -1f);
                drawnRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                object controller = controllerRoot.AddComponent(
                    Type.GetType(Mahjong3DPlayerUiControllerTypeName, true));
                object handView = handViewObject.AddComponent(Type.GetType(Mahjong3DHandViewTypeName, true));
                object drawnTileView = drawnTileViewObject.AddComponent(
                    Type.GetType(Mahjong3DDrawnTileViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));

                SetPrivateField(handView, "spawnRoot", handRoot.transform);
                SetPrivateField(handView, "tilePrefab", tilePrefab);
                SetPrivateField(handView, "spacing", 2f);
                SetPrivateField(drawnTileView, "spawnRoot", drawnRoot.transform);
                SetPrivateField(drawnTileView, "tilePrefab", tilePrefab);
                SetPrivateField(drawnTileView, "handGap", 1.25f);
                SetPrivateField(controller, "handView", handView);
                SetPrivateField(controller, "drawnTileView", drawnTileView);

                Invoke(
                    controller,
                    "RenderHand",
                    CreateTileList("1m", "2m", "3m"),
                    Seat("East"),
                    true,
                    true);
                Invoke(controller, "RenderDrawnTile", CreateTile("4m"), true, true);
                AssertDrawnTileWorldPosition(
                    drawnRoot,
                    handRoot.transform.TransformPoint(new Vector3(5.25f, 0f, 0f)));

                Invoke(
                    controller,
                    "RenderHand",
                    CreateTileList("1m", "2m"),
                    Seat("East"),
                    true,
                    true);
                AssertDrawnTileWorldPosition(
                    drawnRoot,
                    handRoot.transform.TransformPoint(new Vector3(3.25f, 0f, 0f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(handViewObject);
                UnityEngine.Object.DestroyImmediate(drawnTileViewObject);
                UnityEngine.Object.DestroyImmediate(handRoot);
                UnityEngine.Object.DestroyImmediate(drawnRoot);
                UnityEngine.Object.DestroyImmediate(controllerRoot);
            }
        }

        private static Component GetSingleTileView(GameObject root)
        {
            Component[] tileViews = root.GetComponentsInChildren(
                Type.GetType(Mahjong3DTileViewTypeName, true),
                true);
            Assert.That(tileViews.Length, Is.EqualTo(1));
            return tileViews[0];
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static IList CreateTileList(params string[] tileCodes)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            Type listType = typeof(List<>).MakeGenericType(tileType);
            IList tiles = (IList)Activator.CreateInstance(listType);
            for (int i = 0; i < tileCodes.Length; i++)
                tiles.Add(CreateTile(tileCodes[i]));

            return tiles;
        }

        private static object Seat(string name)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), name);
        }

        private static void AssertDrawnTileWorldPosition(GameObject drawnRoot, Vector3 expectedPosition)
        {
            Component[] tileViews = drawnRoot.GetComponentsInChildren(
                Type.GetType(Mahjong3DTileViewTypeName, true),
                true);
            Assert.That(tileViews.Length, Is.EqualTo(1));
            Assert.That(Vector3.Distance(tileViews[0].transform.position, expectedPosition), Is.LessThan(0.0001f));
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
