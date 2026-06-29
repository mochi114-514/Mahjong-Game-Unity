using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DTileViewTests
    {
        private const string Mahjong3DTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";

        [Test]
        public void SetDimmed_TogglesDimmedState()
        {
            GameObject tileObject = new GameObject("Tile3DViewDimmedTest");
            try
            {
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));

                Invoke(tileView, "SetDimmed", true);

                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);

                Invoke(tileView, "SetDimmed", false);

                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void Initialize_ClearsDimmedState()
        {
            GameObject tileObject = new GameObject("Tile3DViewInitializeClearsDimmedTest");
            try
            {
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));

                Invoke(tileView, "SetDimmed", true);
                Invoke(tileView, "Initialize", 3);

                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
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
    }
}
