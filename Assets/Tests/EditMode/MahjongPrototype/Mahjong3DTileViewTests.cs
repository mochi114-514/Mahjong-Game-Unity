using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DTileViewTests
    {
        private const string Mahjong3DTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";
        private const string BaseColorPropertyName = "_BaseColor";
        private static readonly Color ExpectedDimmedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

        [Test]
        public void SetDimmed_WithDimTargetRoot_AppliesPropertyBlockToChildRenderer()
        {
            GameObject tileObject = new GameObject("Tile3DViewRootDimmedTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            try
            {
                dimRoot.transform.SetParent(tileObject.transform);
                rendererObject.transform.SetParent(dimRoot.transform);
                Renderer renderer = rendererObject.AddComponent<MeshRenderer>();
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(tileView, "dimTargetRoot", dimRoot.transform);

                Invoke(tileView, "SetDimmed", true);

                MaterialPropertyBlock propertyBlock = GetPropertyBlock(renderer);
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);
                Assert.That(propertyBlock.isEmpty, Is.False);
                Assert.That(propertyBlock.GetColor(BaseColorPropertyName), Is.EqualTo(ExpectedDimmedTint));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void SetDimmedFalse_ClearsPropertyBlock()
        {
            GameObject tileObject = new GameObject("Tile3DViewClearPropertyBlockTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            try
            {
                dimRoot.transform.SetParent(tileObject.transform);
                rendererObject.transform.SetParent(dimRoot.transform);
                Renderer renderer = rendererObject.AddComponent<MeshRenderer>();
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(tileView, "dimTargetRoot", dimRoot.transform);

                Invoke(tileView, "SetDimmed", true);
                Invoke(tileView, "SetDimmed", false);

                MaterialPropertyBlock propertyBlock = GetPropertyBlock(renderer);
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
                Assert.That(propertyBlock.isEmpty, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void Initialize_ClearsDimmedStateAndPropertyBlock()
        {
            GameObject tileObject = new GameObject("Tile3DViewInitializeClearsDimmedTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            try
            {
                dimRoot.transform.SetParent(tileObject.transform);
                rendererObject.transform.SetParent(dimRoot.transform);
                Renderer renderer = rendererObject.AddComponent<MeshRenderer>();
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(tileView, "dimTargetRoot", dimRoot.transform);

                Invoke(tileView, "SetDimmed", true);
                Invoke(tileView, "Initialize", 3);

                MaterialPropertyBlock propertyBlock = GetPropertyBlock(renderer);
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
                Assert.That(propertyBlock.isEmpty, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void SetDimmed_UsesExplicitRenderersBeforeDimTargetRoot()
        {
            GameObject tileObject = new GameObject("Tile3DViewExplicitRendererPriorityTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rootRendererObject = new GameObject("TileBody");
            GameObject explicitRendererObject = new GameObject("FrontFace");
            try
            {
                dimRoot.transform.SetParent(tileObject.transform);
                rootRendererObject.transform.SetParent(dimRoot.transform);
                explicitRendererObject.transform.SetParent(tileObject.transform);
                Renderer rootRenderer = rootRendererObject.AddComponent<MeshRenderer>();
                Renderer explicitRenderer = explicitRendererObject.AddComponent<MeshRenderer>();
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(tileView, "dimTargetRoot", dimRoot.transform);
                SetPrivateField(tileView, "dimTargetRenderers", new[] { explicitRenderer });

                Invoke(tileView, "SetDimmed", true);

                MaterialPropertyBlock rootPropertyBlock = GetPropertyBlock(rootRenderer);
                MaterialPropertyBlock explicitPropertyBlock = GetPropertyBlock(explicitRenderer);
                Assert.That(rootPropertyBlock.isEmpty, Is.True);
                Assert.That(explicitPropertyBlock.isEmpty, Is.False);
                Assert.That(explicitPropertyBlock.GetColor(BaseColorPropertyName), Is.EqualTo(ExpectedDimmedTint));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void SetDimmedTrue_WithoutTargets_WarnsOnce()
        {
            GameObject tileObject = new GameObject("Tile3DViewMissingDimTargetTest");
            try
            {
                object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                LogAssert.Expect(
                    LogType.Warning,
                    "Mahjong3DTileView: Dim target root/renderers are not assigned or no Renderer was found under DimTargetRoot.");

                Invoke(tileView, "SetDimmed", true);

                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        private static MaterialPropertyBlock GetPropertyBlock(Renderer renderer)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            return propertyBlock;
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
