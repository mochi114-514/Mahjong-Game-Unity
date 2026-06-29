using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DDrawnTileViewTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string Mahjong3DDrawnTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DDrawnTileView, Assembly-CSharp";
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
