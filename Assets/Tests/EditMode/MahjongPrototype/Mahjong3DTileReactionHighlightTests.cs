using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DTileReactionHighlightTests
    {
        private const string HighlightTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileReactionHighlight, Assembly-CSharp";
        private const string TileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";
        private const string RiverVariantPath = "Assets/Prefab/Tiles/3DTile_RiverHighlight.prefab";
        private const string ScenePath = "Assets/Scenes/Mahjong Prototype.unity";
        private const string HighlightShaderName = "Mahjong Prototype/Reaction Highlight Shell";

        [Test]
        public void StartHighlight_EnablesDedicatedRenderer()
        {
            HighlightTestFixture fixture = CreateFixture();
            try
            {
                Invoke(fixture.Highlight, "StartHighlight");

                Assert.That(fixture.Renderer.enabled, Is.True);
                Assert.That(fixture.Renderer.transform.localScale, Is.EqualTo(Vector3.one * 1.02f));
                Assert.That(GetProperty(fixture.Highlight, "IsHighlighted"), Is.True);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void StopHighlight_RestoresRendererAndMaterialPropertyBlock()
        {
            HighlightTestFixture fixture = CreateFixture();
            try
            {
                MaterialPropertyBlock baselineBlock = new MaterialPropertyBlock();
                baselineBlock.SetFloat("_Alpha", 0.42f);
                fixture.Renderer.SetPropertyBlock(baselineBlock);

                Invoke(fixture.Highlight, "StartHighlight");
                Invoke(fixture.Highlight, "StopHighlight");

                MaterialPropertyBlock restoredBlock = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(restoredBlock);
                Assert.That(fixture.Renderer.enabled, Is.False);
                Assert.That(restoredBlock.GetFloat("_Alpha"), Is.EqualTo(0.42f).Within(0.0001f));
                Assert.That(fixture.Renderer.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(GetProperty(fixture.Highlight, "IsHighlighted"), Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void OnDisable_RestoresRendererAndMaterialPropertyBlock()
        {
            HighlightTestFixture fixture = CreateFixture();
            try
            {
                MaterialPropertyBlock baselineBlock = new MaterialPropertyBlock();
                baselineBlock.SetFloat("_Alpha", 0.37f);
                fixture.Renderer.SetPropertyBlock(baselineBlock);
                Invoke(fixture.Highlight, "StartHighlight");

                Invoke(fixture.Highlight, "OnDisable");

                MaterialPropertyBlock restoredBlock = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(restoredBlock);
                Assert.That(fixture.Renderer.enabled, Is.False);
                Assert.That(restoredBlock.GetFloat("_Alpha"), Is.EqualTo(0.37f).Within(0.0001f));
                Assert.That(fixture.Renderer.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(GetProperty(fixture.Highlight, "IsHighlighted"), Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void RiverVariant_DimTargetsExcludeReactionHighlightShell()
        {
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(RiverVariantPath);
            Assert.That(variant, Is.Not.Null);

            Component tileView = variant.GetComponent(Type.GetType(TileViewTypeName, true));
            MeshRenderer shellRenderer = variant.transform.Find("Tile Prefab/ReactionHighlightShell")
                .GetComponent<MeshRenderer>();
            SerializedObject serializedTileView = new SerializedObject(tileView);
            SerializedProperty dimTargets = serializedTileView.FindProperty("dimTargetRenderers");

            Assert.That(dimTargets.arraySize, Is.EqualTo(3));
            for (int i = 0; i < dimTargets.arraySize; i++)
            {
                UnityEngine.Object target = dimTargets.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(target, Is.Not.SameAs(shellRenderer));
            }
        }

        [Test]
        public void SetDimmed_WithExplicitTargets_DoesNotChangeReactionHighlightRenderer()
        {
            GameObject root = new GameObject("ReactionHighlightDimTargetTest");
            Material originalMaterial = CreateTestMaterial();
            Material dimmedMaterial = CreateTestMaterial();
            try
            {
                MeshRenderer tileBody = CreateChildRenderer(root.transform, "TileBody", originalMaterial);
                MeshRenderer frontFace = CreateChildRenderer(root.transform, "FrontFace", originalMaterial);
                MeshRenderer backFace = CreateChildRenderer(root.transform, "BackFace", originalMaterial);
                MeshRenderer shell = CreateChildRenderer(root.transform, "ReactionHighlightShell", originalMaterial);
                Component tileView = root.AddComponent(Type.GetType(TileViewTypeName, true));
                SetPrivateField(tileView, "dimTargetRenderers", new Renderer[] { tileBody, frontFace, backFace });
                SetPrivateField(tileView, "dimmedOverrideMaterial", dimmedMaterial);

                Invoke(tileView, "SetDimmed", true);

                Assert.That(tileBody.sharedMaterial, Is.SameAs(dimmedMaterial));
                Assert.That(frontFace.sharedMaterial, Is.SameAs(dimmedMaterial));
                Assert.That(backFace.sharedMaterial, Is.SameAs(dimmedMaterial));
                Assert.That(shell.sharedMaterial, Is.SameAs(originalMaterial));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(originalMaterial);
                UnityEngine.Object.DestroyImmediate(dimmedMaterial);
            }
        }

        [Test]
        public void RiverVariant_ReactionHighlightShellUsesTileBodySharedMesh()
        {
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(RiverVariantPath);
            Assert.That(variant, Is.Not.Null);

            MeshFilter tileBody = variant.transform.Find("Tile Prefab/TileBody").GetComponent<MeshFilter>();
            MeshFilter shell = variant.transform.Find("Tile Prefab/ReactionHighlightShell").GetComponent<MeshFilter>();
            MeshRenderer shellRenderer = shell.GetComponent<MeshRenderer>();

            Assert.That(shell.sharedMesh, Is.SameAs(tileBody.sharedMesh));
            Assert.That(shellRenderer.enabled, Is.False);
        }

        [Test]
        public void Scene_DiscardRiversReferenceTheRiverHighlightVariant()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Type riverViewType = Type.GetType(
                "MahjongPrototype.UI3D.Mahjong3DDiscardRiverView, Assembly-CSharp",
                true);
            int riverCount = 0;
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    Component[] riverViews = roots[i].GetComponentsInChildren(riverViewType, true);
                    for (int j = 0; j < riverViews.Length; j++)
                    {
                        SerializedProperty tilePrefab = new SerializedObject(riverViews[j])
                            .FindProperty("tilePrefab");
                        Assert.That(tilePrefab.objectReferenceValue, Is.Not.Null);
                        riverCount++;
                    }
                }

                Assert.That(riverCount, Is.EqualTo(4));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static HighlightTestFixture CreateFixture()
        {
            GameObject root = new GameObject("ReactionHighlightTestRoot");
            GameObject shell = new GameObject("ReactionHighlightShell");
            shell.transform.SetParent(root.transform, false);
            MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateTestMaterial();
            renderer.enabled = false;

            Component highlight = root.AddComponent(Type.GetType(HighlightTypeName, true));
            SetPrivateField(highlight, "highlightRenderer", renderer);
            SetPrivateField(highlight, "shellTransform", shell.transform);

            return new HighlightTestFixture(root, renderer, highlight);
        }

        private static Material CreateTestMaterial()
        {
            Shader shader = Shader.Find(HighlightShaderName);
            Assert.That(shader, Is.Not.Null);
            return new Material(shader);
        }

        private static MeshRenderer CreateChildRenderer(Transform parent, string name, Material material)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name == methodName && method.GetParameters().Length == arguments.Length)
                {
                    method.Invoke(target, arguments);
                    return;
                }
            }

            Assert.Fail($"Missing method: {methodName}");
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Missing property: {propertyName}");
            return property.GetValue(target);
        }

        private sealed class HighlightTestFixture : IDisposable
        {
            private readonly GameObject root;

            public HighlightTestFixture(GameObject root, MeshRenderer renderer, Component highlight)
            {
                this.root = root;
                Renderer = renderer;
                Highlight = (Behaviour)highlight;
            }

            public MeshRenderer Renderer { get; }

            public Behaviour Highlight { get; }

            public void Dispose()
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
