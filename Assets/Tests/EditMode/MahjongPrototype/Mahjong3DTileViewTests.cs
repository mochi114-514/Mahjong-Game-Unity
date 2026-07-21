using System;
using System.Linq.Expressions;
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
        public void RaycastHover_TargetChangesNotifyOnceAndIgnoreInteractable()
        {
            GameObject inputObject = new GameObject("TileRaycastHoverInputTest");
            GameObject firstObject = new GameObject("FirstTile");
            GameObject secondObject = new GameObject("SecondTile");
            try
            {
                object input = inputObject.AddComponent(Type.GetType(
                    "MahjongPrototype.UI3D.Mahjong3DTileRaycastInput, Assembly-CSharp",
                    true));
                object first = firstObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                object second = secondObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                int firstEnterCount = 0;
                int firstExitCount = 0;
                int secondEnterCount = 0;
                int secondExitCount = 0;
                Subscribe(first, "HoverEntered", _ => firstEnterCount++);
                Subscribe(first, "HoverExited", _ => firstExitCount++);
                Subscribe(second, "HoverEntered", _ => secondEnterCount++);
                Subscribe(second, "HoverExited", _ => secondExitCount++);

                Assert.That(GetProperty(first, "Interactable"), Is.False);
                Invoke(input, "SetHoveredTile", first);
                Invoke(input, "SetHoveredTile", first);
                Invoke(input, "SetHoveredTile", second);
                Invoke(input, "SetHoveredTile", new object[] { null });

                Assert.That(firstEnterCount, Is.EqualTo(1));
                Assert.That(firstExitCount, Is.EqualTo(1));
                Assert.That(secondEnterCount, Is.EqualTo(1));
                Assert.That(secondExitCount, Is.EqualTo(1));
                Assert.That(GetProperty(first, "IsHovered"), Is.False);
                Assert.That(GetProperty(second, "IsHovered"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inputObject);
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void RaycastHover_OnDisableExitsCurrentTile()
        {
            GameObject inputObject = new GameObject("TileRaycastHoverDisableTest");
            GameObject tileObject = new GameObject("HoveredTile");
            try
            {
                object input = inputObject.AddComponent(Type.GetType(
                    "MahjongPrototype.UI3D.Mahjong3DTileRaycastInput, Assembly-CSharp",
                    true));
                object tile = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                int exitCount = 0;
                Subscribe(tile, "HoverExited", _ => exitCount++);

                Invoke(input, "SetHoveredTile", tile);
                Invoke(input, "OnDisable");

                Assert.That(exitCount, Is.EqualTo(1));
                Assert.That(GetProperty(tile, "IsHovered"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inputObject);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void OverrideMaterialMode_SetDimmedTrue_ReplacesSharedMaterials()
        {
            GameObject tileObject = new GameObject("Tile3DViewOverrideMaterialTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            Material originalMaterial = CreateTestMaterial("OriginalTileMaterial");
            Material dimmedMaterial = CreateTestMaterial("DimmedTileMaterial");
            try
            {
                Renderer renderer = PrepareRenderer(tileObject, dimRoot, rendererObject);
                renderer.sharedMaterials = new[] { originalMaterial };
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetPrivateField(tileView, "dimmedOverrideMaterial", dimmedMaterial);

                Invoke(tileView, "SetDimmed", true);

                Material[] materials = renderer.sharedMaterials;
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);
                Assert.That(materials.Length, Is.EqualTo(1));
                Assert.That(materials[0], Is.SameAs(dimmedMaterial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalMaterial);
                UnityEngine.Object.DestroyImmediate(dimmedMaterial);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void OverrideMaterialMode_SetDimmedFalse_RestoresSharedMaterials()
        {
            GameObject tileObject = new GameObject("Tile3DViewRestoreMaterialTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            Material originalMaterial = CreateTestMaterial("OriginalTileMaterial");
            Material dimmedMaterial = CreateTestMaterial("DimmedTileMaterial");
            try
            {
                Renderer renderer = PrepareRenderer(tileObject, dimRoot, rendererObject);
                renderer.sharedMaterials = new[] { originalMaterial };
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetPrivateField(tileView, "dimmedOverrideMaterial", dimmedMaterial);

                Invoke(tileView, "SetDimmed", true);
                Invoke(tileView, "SetDimmed", false);

                Material[] materials = renderer.sharedMaterials;
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
                Assert.That(materials.Length, Is.EqualTo(1));
                Assert.That(materials[0], Is.SameAs(originalMaterial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalMaterial);
                UnityEngine.Object.DestroyImmediate(dimmedMaterial);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void OverrideMaterialMode_ReplacesEveryMaterialSlot()
        {
            GameObject tileObject = new GameObject("Tile3DViewMultiMaterialTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            Material originalA = CreateTestMaterial("OriginalTileMaterialA");
            Material originalB = CreateTestMaterial("OriginalTileMaterialB");
            Material dimmedMaterial = CreateTestMaterial("DimmedTileMaterial");
            try
            {
                Renderer renderer = PrepareRenderer(tileObject, dimRoot, rendererObject);
                renderer.sharedMaterials = new[] { originalA, originalB };
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetPrivateField(tileView, "dimmedOverrideMaterial", dimmedMaterial);

                Invoke(tileView, "SetDimmed", true);

                Material[] materials = renderer.sharedMaterials;
                Assert.That(materials.Length, Is.EqualTo(2));
                Assert.That(materials[0], Is.SameAs(dimmedMaterial));
                Assert.That(materials[1], Is.SameAs(dimmedMaterial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalA);
                UnityEngine.Object.DestroyImmediate(originalB);
                UnityEngine.Object.DestroyImmediate(dimmedMaterial);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void Initialize_ClearsDimmedStateAndRestoresSharedMaterials()
        {
            GameObject tileObject = new GameObject("Tile3DViewInitializeRestoresMaterialTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            Material originalMaterial = CreateTestMaterial("OriginalTileMaterial");
            Material dimmedMaterial = CreateTestMaterial("DimmedTileMaterial");
            try
            {
                Renderer renderer = PrepareRenderer(tileObject, dimRoot, rendererObject);
                renderer.sharedMaterials = new[] { originalMaterial };
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetPrivateField(tileView, "dimmedOverrideMaterial", dimmedMaterial);

                Invoke(tileView, "SetDimmed", true);
                Invoke(tileView, "Initialize", 3);

                Material[] materials = renderer.sharedMaterials;
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.False);
                Assert.That(materials.Length, Is.EqualTo(1));
                Assert.That(materials[0], Is.SameAs(originalMaterial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalMaterial);
                UnityEngine.Object.DestroyImmediate(dimmedMaterial);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void OverrideMaterialMode_WithoutOverrideMaterial_Warns()
        {
            GameObject tileObject = new GameObject("Tile3DViewMissingOverrideMaterialTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            try
            {
                PrepareRenderer(tileObject, dimRoot, rendererObject);
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                LogAssert.Expect(
                    LogType.Warning,
                    "Mahjong3DTileView: Dimmed override material is not assigned.");

                Invoke(tileView, "SetDimmed", true);

                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void MaterialPropertyBlockTintMode_SetDimmedTrue_AppliesPropertyBlockToChildRenderer()
        {
            GameObject tileObject = new GameObject("Tile3DViewRootDimmedTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            try
            {
                Renderer renderer = PrepareRenderer(tileObject, dimRoot, rendererObject);
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetDimVisualMode(tileView, "MaterialPropertyBlockTint");

                Invoke(tileView, "SetDimmed", true);

                MaterialPropertyBlock propertyBlock = GetPropertyBlock(renderer);
                Assert.That(GetProperty(tileView, "IsDimmed"), Is.True);
                Assert.That(propertyBlock.isEmpty, Is.False);
                Color actualTint = propertyBlock.GetColor(BaseColorPropertyName);
                AssertColorApproximately(actualTint, ExpectedDimmedTint);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void MaterialPropertyBlockTintMode_SetDimmedFalse_ClearsPropertyBlock()
        {
            GameObject tileObject = new GameObject("Tile3DViewClearPropertyBlockTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rendererObject = new GameObject("TileBody");
            try
            {
                Renderer renderer = PrepareRenderer(tileObject, dimRoot, rendererObject);
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetDimVisualMode(tileView, "MaterialPropertyBlockTint");

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
        public void MaterialPropertyBlockTintMode_UsesExplicitRenderersBeforeDimTargetRoot()
        {
            GameObject tileObject = new GameObject("Tile3DViewExplicitRendererPriorityTest");
            GameObject dimRoot = new GameObject("TilePrefab");
            GameObject rootRendererObject = new GameObject("TileBody");
            GameObject explicitRendererObject = new GameObject("FrontFace");
            try
            {
                Renderer rootRenderer = PrepareRenderer(tileObject, dimRoot, rootRendererObject);
                explicitRendererObject.transform.SetParent(tileObject.transform);
                Renderer explicitRenderer = explicitRendererObject.AddComponent<MeshRenderer>();
                object tileView = CreateTileView(tileObject, dimRoot.transform);
                SetDimVisualMode(tileView, "MaterialPropertyBlockTint");
                SetPrivateField(tileView, "dimTargetRenderers", new[] { explicitRenderer });

                Invoke(tileView, "SetDimmed", true);

                MaterialPropertyBlock rootPropertyBlock = GetPropertyBlock(rootRenderer);
                MaterialPropertyBlock explicitPropertyBlock = GetPropertyBlock(explicitRenderer);
                Assert.That(rootPropertyBlock.isEmpty, Is.True);
                Assert.That(explicitPropertyBlock.isEmpty, Is.False);
                Color actualTint = explicitPropertyBlock.GetColor(BaseColorPropertyName);
                AssertColorApproximately(actualTint, ExpectedDimmedTint);
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

        private static Renderer PrepareRenderer(
            GameObject tileObject,
            GameObject dimRoot,
            GameObject rendererObject)
        {
            dimRoot.transform.SetParent(tileObject.transform);
            rendererObject.transform.SetParent(dimRoot.transform);
            return rendererObject.AddComponent<MeshRenderer>();
        }

        private static object CreateTileView(GameObject tileObject, Transform dimRoot)
        {
            object tileView = tileObject.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
            SetPrivateField(tileView, "dimTargetRoot", dimRoot);
            return tileView;
        }

        private static Material CreateTestMaterial(string materialName)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);

            Material material = new Material(shader)
            {
                name = materialName
            };
            return material;
        }

        private static MaterialPropertyBlock GetPropertyBlock(Renderer renderer)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            return propertyBlock;
        }

        private static void AssertColorApproximately(
            Color actual,
            Color expected,
            float tolerance = 0.0001f)
        {
            Assert.That(
                actual.r,
                Is.EqualTo(expected.r).Within(tolerance),
                "Red component differs.");
            Assert.That(
                actual.g,
                Is.EqualTo(expected.g).Within(tolerance),
                "Green component differs.");
            Assert.That(
                actual.b,
                Is.EqualTo(expected.b).Within(tolerance),
                "Blue component differs.");
            Assert.That(
                actual.a,
                Is.EqualTo(expected.a).Within(tolerance),
                "Alpha component differs.");
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

        private static void Subscribe(object target, string eventName, Action<object[]> callback)
        {
            EventInfo eventInfo = target.GetType().GetEvent(
                eventName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(eventInfo, Is.Not.Null);
            MethodInfo invokeMethod = eventInfo.EventHandlerType.GetMethod("Invoke");
            ParameterInfo[] eventParameters = invokeMethod.GetParameters();
            ParameterExpression[] parameters = new ParameterExpression[eventParameters.Length];
            Expression[] boxedParameters = new Expression[eventParameters.Length];
            for (int i = 0; i < eventParameters.Length; i++)
            {
                parameters[i] = Expression.Parameter(eventParameters[i].ParameterType);
                boxedParameters[i] = Expression.Convert(parameters[i], typeof(object));
            }

            MethodInfo callbackInvoke = typeof(Action<object[]>).GetMethod("Invoke");
            MethodCallExpression body = Expression.Call(
                Expression.Constant(callback),
                callbackInvoke,
                Expression.NewArrayInit(typeof(object), boxedParameters));
            Delegate handler = Expression.Lambda(
                eventInfo.EventHandlerType,
                body,
                parameters).Compile();
            eventInfo.AddEventHandler(target, handler);
        }

        private static void SetDimVisualMode(object target, string modeName)
        {
            FieldInfo field = target.GetType().GetField(
                "dimVisualMode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, Enum.Parse(field.FieldType, modeName));
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
