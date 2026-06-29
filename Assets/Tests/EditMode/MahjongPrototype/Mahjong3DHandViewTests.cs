using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DHandViewTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string Mahjong3DHandViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DHandView, Assembly-CSharp";
        private const string Mahjong3DTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";

        [Test]
        public void SetTileInteractableByIndices_OnlyEnablesMatchingTiles()
        {
            GameObject root = new GameObject("Hand3DViewReachCandidatesTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DHandViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Invoke(
                    view,
                    "RenderHand",
                    CreateTileList(CreateTile("1m"), CreateTile("2m"), CreateTile("3m")),
                    true,
                    true);

                Invoke(view, "SetTileInteractableByIndices", new System.Collections.Generic.List<int> { 2 });

                Component[] tileViews = root.GetComponentsInChildren(Type.GetType(Mahjong3DTileViewTypeName, true));
                Assert.That(tileViews.Length, Is.EqualTo(3));
                Assert.That(GetProperty(tileViews[0], "Interactable"), Is.False);
                Assert.That(GetProperty(tileViews[1], "Interactable"), Is.False);
                Assert.That(GetProperty(tileViews[2], "Interactable"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
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
