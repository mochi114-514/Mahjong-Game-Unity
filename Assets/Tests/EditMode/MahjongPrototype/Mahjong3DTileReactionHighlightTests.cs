using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
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
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string PlayerAreaPresenterTypeName =
            "MahjongPrototype.UI3D.Mahjong3DPlayerAreaPresenter, Assembly-CSharp";
        private const string PlayerUiControllerTypeName =
            "MahjongPrototype.UI3D.Mahjong3DPlayerUiController, Assembly-CSharp";
        private const string DiscardRiverViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DDiscardRiverView, Assembly-CSharp";
        private const string TileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string DiscardRecordTypeName =
            "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string DiscardClaimTypeName =
            "MahjongPrototype.Domain.DiscardClaim, Assembly-CSharp";
        private const string DiscardSourceTypeName =
            "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";
        private const string ReactionDecisionRequestTypeName =
            "MahjongPrototype.Domain.ReactionDecisionRequest, Assembly-CSharp";
        private const string ReactionDecisionOptionTypeName =
            "MahjongPrototype.Domain.ReactionDecisionOption, Assembly-CSharp";
        private const string ReactionDecisionChiOptionTypeName =
            "MahjongPrototype.Domain.ReactionDecisionChiOption, Assembly-CSharp";
        private const string ReactionWindowSourceKindTypeName =
            "MahjongPrototype.Domain.ReactionWindowSourceKind, Assembly-CSharp";
        private const string ReactionWindowSeatAnswerKindTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerKind, Assembly-CSharp";
        private const string MatchRosterTypeName =
            "MahjongPrototype.Domain.MatchRoster, Assembly-CSharp";
        private const string MatchParticipantTypeName =
            "MahjongPrototype.Domain.MatchParticipant, Assembly-CSharp";
        private const string ParticipantKindTypeName =
            "MahjongPrototype.Domain.ParticipantKind, Assembly-CSharp";
        private const string DecisionProviderRegistryTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistry, Assembly-CSharp";
        private const string DecisionProviderRegistrationTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistration, Assembly-CSharp";
        private const string DecisionProviderRouteTypeName =
            "MahjongPrototype.Domain.DecisionProviderRoute, Assembly-CSharp";
        private const string LocalUiDecisionProviderTypeName =
            "MahjongPrototype.LocalUiDecisionProvider, Assembly-CSharp";
        private const string ReactionWindowCandidateTypeName =
            "MahjongPrototype.Domain.ReactionWindowCandidate, Assembly-CSharp";
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";
        private const string ReactionDecisionResponseTypeName =
            "MahjongPrototype.Domain.ReactionDecisionResponse, Assembly-CSharp";
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
                Assert.That(fixture.FaceRenderer.enabled, Is.True);
                Assert.That(fixture.Renderer.transform.localScale, Is.EqualTo(Vector3.one * 1.03f));
                Assert.That(fixture.FaceRenderer.transform.localScale, Is.EqualTo(Vector3.one));
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
                MaterialPropertyBlock faceBaselineBlock = new MaterialPropertyBlock();
                faceBaselineBlock.SetFloat("_Alpha", 0.27f);
                fixture.FaceRenderer.SetPropertyBlock(faceBaselineBlock);

                Invoke(fixture.Highlight, "StartHighlight");
                Invoke(fixture.Highlight, "StopHighlight");

                MaterialPropertyBlock restoredBlock = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(restoredBlock);
                MaterialPropertyBlock restoredFaceBlock = new MaterialPropertyBlock();
                fixture.FaceRenderer.GetPropertyBlock(restoredFaceBlock);
                Assert.That(fixture.Renderer.enabled, Is.False);
                Assert.That(fixture.FaceRenderer.enabled, Is.False);
                Assert.That(restoredBlock.GetFloat("_Alpha"), Is.EqualTo(0.42f).Within(0.0001f));
                Assert.That(restoredFaceBlock.GetFloat("_Alpha"), Is.EqualTo(0.27f).Within(0.0001f));
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
                MaterialPropertyBlock faceBaselineBlock = new MaterialPropertyBlock();
                faceBaselineBlock.SetFloat("_Alpha", 0.23f);
                fixture.FaceRenderer.SetPropertyBlock(faceBaselineBlock);
                Invoke(fixture.Highlight, "StartHighlight");

                Invoke(fixture.Highlight, "OnDisable");

                MaterialPropertyBlock restoredBlock = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(restoredBlock);
                MaterialPropertyBlock restoredFaceBlock = new MaterialPropertyBlock();
                fixture.FaceRenderer.GetPropertyBlock(restoredFaceBlock);
                Assert.That(fixture.Renderer.enabled, Is.False);
                Assert.That(fixture.FaceRenderer.enabled, Is.False);
                Assert.That(restoredBlock.GetFloat("_Alpha"), Is.EqualTo(0.37f).Within(0.0001f));
                Assert.That(restoredFaceBlock.GetFloat("_Alpha"), Is.EqualTo(0.23f).Within(0.0001f));
                Assert.That(fixture.Renderer.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(GetProperty(fixture.Highlight, "IsHighlighted"), Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void StartHighlight_SynchronizesCurrentFrontFaceMesh()
        {
            HighlightTestFixture fixture = CreateFixture();
            Mesh updatedFrontFaceMesh = CreateTestMesh("UpdatedFrontFaceMesh");
            try
            {
                fixture.FrontFaceMeshFilter.sharedMesh = updatedFrontFaceMesh;

                Invoke(fixture.Highlight, "StartHighlight");

                Assert.That(fixture.FaceHighlightMeshFilter.sharedMesh, Is.SameAs(updatedFrontFaceMesh));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(updatedFrontFaceMesh);
                fixture.Dispose();
            }
        }

        [Test]
        public void StartHighlight_UsesSameRimColorAndPulseForShellAndFace()
        {
            HighlightTestFixture fixture = CreateFixture();
            try
            {
                Invoke(fixture.Highlight, "StartHighlight");

                MaterialPropertyBlock shellBlock = new MaterialPropertyBlock();
                fixture.Renderer.GetPropertyBlock(shellBlock);
                MaterialPropertyBlock faceBlock = new MaterialPropertyBlock();
                fixture.FaceRenderer.GetPropertyBlock(faceBlock);
                int rimColorId = Shader.PropertyToID("_RimColor");
                int pulseStrengthId = Shader.PropertyToID("_PulseStrength");
                int alphaId = Shader.PropertyToID("_Alpha");
                int surfaceIntensityId = Shader.PropertyToID("_SurfaceIntensity");
                int vertexExtrusionId = Shader.PropertyToID("_VertexExtrusion");

                Assert.That(faceBlock.GetColor(rimColorId), Is.EqualTo(shellBlock.GetColor(rimColorId)));
                Assert.That(faceBlock.GetFloat(pulseStrengthId), Is.EqualTo(shellBlock.GetFloat(pulseStrengthId)));
                Assert.That(shellBlock.GetFloat(alphaId), Is.EqualTo(0.18f).Within(0.0001f));
                Assert.That(faceBlock.GetFloat(alphaId), Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(shellBlock.GetFloat(surfaceIntensityId), Is.Zero);
                Assert.That(faceBlock.GetFloat(surfaceIntensityId), Is.EqualTo(1f).Within(0.0001f));
                Assert.That(faceBlock.GetFloat(vertexExtrusionId), Is.EqualTo(0.001f).Within(0.0001f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void ShellScale_UsesOnePointZeroOneToOnePointZeroFiveRange()
        {
            HighlightTestFixture fixture = CreateFixture();
            try
            {
                FieldInfo field = fixture.Highlight.GetType().GetField(
                    "shellScale",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                UnityEngine.RangeAttribute range = (UnityEngine.RangeAttribute)Attribute.GetCustomAttribute(
                    field,
                    typeof(UnityEngine.RangeAttribute));

                Assert.That(range.min, Is.EqualTo(1.01f));
                Assert.That(range.max, Is.EqualTo(1.05f));
                Assert.That((float)field.GetValue(fixture.Highlight), Is.EqualTo(1.03f));
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
            MeshRenderer faceRenderer = variant.transform.Find("Tile Prefab/ReactionHighlightFace")
                .GetComponent<MeshRenderer>();
            SerializedObject serializedTileView = new SerializedObject(tileView);
            SerializedProperty dimTargets = serializedTileView.FindProperty("dimTargetRenderers");

            Assert.That(dimTargets.arraySize, Is.EqualTo(3));
            for (int i = 0; i < dimTargets.arraySize; i++)
            {
                UnityEngine.Object target = dimTargets.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(target, Is.Not.SameAs(shellRenderer));
                Assert.That(target, Is.Not.SameAs(faceRenderer));
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
                MeshRenderer face = CreateChildRenderer(root.transform, "ReactionHighlightFace", originalMaterial);
                Component tileView = root.AddComponent(Type.GetType(TileViewTypeName, true));
                SetPrivateField(tileView, "dimTargetRenderers", new Renderer[] { tileBody, frontFace, backFace });
                SetPrivateField(tileView, "dimmedOverrideMaterial", dimmedMaterial);

                Invoke(tileView, "SetDimmed", true);

                Assert.That(tileBody.sharedMaterial, Is.SameAs(dimmedMaterial));
                Assert.That(frontFace.sharedMaterial, Is.SameAs(dimmedMaterial));
                Assert.That(backFace.sharedMaterial, Is.SameAs(dimmedMaterial));
                Assert.That(shell.sharedMaterial, Is.SameAs(originalMaterial));
                Assert.That(face.sharedMaterial, Is.SameAs(originalMaterial));
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
            MeshFilter frontFace = variant.transform.Find("Tile Prefab/FrontFace").GetComponent<MeshFilter>();
            MeshFilter shell = variant.transform.Find("Tile Prefab/ReactionHighlightShell").GetComponent<MeshFilter>();
            MeshFilter face = variant.transform.Find("Tile Prefab/ReactionHighlightFace").GetComponent<MeshFilter>();
            MeshRenderer shellRenderer = shell.GetComponent<MeshRenderer>();
            MeshRenderer faceRenderer = face.GetComponent<MeshRenderer>();

            Assert.That(shell.sharedMesh, Is.SameAs(tileBody.sharedMesh));
            Assert.That(face.sharedMesh, Is.SameAs(frontFace.sharedMesh));
            Assert.That(shellRenderer.enabled, Is.False);
            Assert.That(faceRenderer.enabled, Is.False);
            Assert.That(faceRenderer.sharedMaterial, Is.SameAs(shellRenderer.sharedMaterial));
            Assert.That(faceRenderer.transform.localScale, Is.EqualTo(Vector3.one));
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

        [Test]
        public void ResolveReactionHighlightDiscardId_UsesCallOptionsAndExactDiscardIdentity()
        {
            IList discards = CreateList(RequireType(DiscardRecordTypeName));
            discards.Add(CreateDiscard(101, "East", "5m", 3));
            discards.Add(CreateDiscard(102, "East", "5m", 7));

            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 7, "Pon")),
                Is.EqualTo(102));
            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 7, "Chi")),
                Is.EqualTo(102));
            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 7, "Daiminkan")),
                Is.EqualTo(102));
            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 7, "Ron", "Pon")),
                Is.EqualTo(102));
            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 8, "Pon")),
                Is.Null);
        }

        [Test]
        public void ResolveReactionHighlightDiscardId_ExcludesNonCallAndKakanRequests()
        {
            IList discards = CreateList(RequireType(DiscardRecordTypeName));
            discards.Add(CreateDiscard(101, "East", "5m", 7));

            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 7, "Ron")),
                Is.Null);
            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Discard", "East", "5m", 7)),
                Is.Null);
            Assert.That(
                ResolveReactionHighlightDiscardId(
                    discards,
                    CreateReactionRequest("Kakan", "East", "5m", 7, "Pon")),
                Is.Null);
        }

        [Test]
        public void RenderDiscardRiver_HighlightsOnlyTheMatchingUnclaimedDiscardId()
        {
            GameObject root = new GameObject("ReactionHighlightDiscardRiverTest");
            try
            {
                Component riverView = root.AddComponent(RequireType(DiscardRiverViewTypeName));
                SetPrivateField(
                    riverView,
                    "tilePrefab",
                    AssetDatabase.LoadAssetAtPath<GameObject>(RiverVariantPath)
                        .GetComponent(RequireType(TileViewTypeName)));
                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(21, "East", "5m", 2));
                discards.Add(CreateDiscard(22, "East", "5m", 6));

                Invoke(riverView, "RenderDiscardRiver", discards, null, Seat("East"), false, 0, 22);

                Component[] tiles = GetTileViews(root);
                Assert.That(tiles.Length, Is.EqualTo(2));
                Assert.That(IsReactionHighlighted(tiles[0]), Is.False);
                Assert.That(IsReactionHighlighted(tiles[1]), Is.True);

                Invoke(riverView, "ClearReactionHighlights");
                Assert.That(IsReactionHighlighted(tiles[0]), Is.False);
                Assert.That(IsReactionHighlighted(tiles[1]), Is.False);

                Invoke(
                    riverView,
                    "RenderDiscardRiver",
                    discards,
                    CreateClaims(22),
                    Seat("East"),
                    false,
                    0,
                    22);
                tiles = GetTileViews(root);
                Assert.That(tiles.Length, Is.EqualTo(1));
                Assert.That(IsReactionHighlighted(tiles[0]), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_HighlightsAReachDeclarationTileWithoutChangingItsRotation()
        {
            GameObject root = new GameObject("ReactionHighlightReachDiscardRiverTest");
            try
            {
                Component riverView = root.AddComponent(RequireType(DiscardRiverViewTypeName));
                SetPrivateField(
                    riverView,
                    "tilePrefab",
                    AssetDatabase.LoadAssetAtPath<GameObject>(RiverVariantPath)
                        .GetComponent(RequireType(TileViewTypeName)));
                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(31, "East", "5m", 4));

                Invoke(riverView, "RenderDiscardRiver", discards, null, Seat("East"), true, 4, 31);

                Component[] tiles = GetTileViews(root);
                Assert.That(tiles.Length, Is.EqualTo(1));
                Assert.That(IsReactionHighlighted(tiles[0]), Is.True);
                Assert.That(
                    Quaternion.Angle(tiles[0].transform.localRotation, Quaternion.Euler(0f, 0f, 90f)),
                    Is.LessThan(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UiManagerOnDisable_ClearsGeneratedDiscardReactionHighlights()
        {
            GameObject root = new GameObject("ReactionHighlightUiManagerDisableTest");
            try
            {
                Component uiManager = root.AddComponent(RequireType(UiManagerTypeName));
                Component presenter = CreateDiscardRiverPresenter(root.transform, out Component riverView);
                SetPrivateField(uiManager, "playerArea3DPresenter", presenter);
                SetPrivateField(
                    riverView,
                    "tilePrefab",
                    AssetDatabase.LoadAssetAtPath<GameObject>(RiverVariantPath)
                        .GetComponent(RequireType(TileViewTypeName)));
                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(41, "East", "5m", 4));
                Invoke(riverView, "RenderDiscardRiver", discards, null, Seat("East"), false, 0, 41);

                Component[] tiles = GetTileViews(root);
                Assert.That(tiles.Length, Is.EqualTo(1));
                Assert.That(IsReactionHighlighted(tiles[0]), Is.True);

                Invoke(uiManager, "OnDisable");

                Assert.That(IsReactionHighlighted(tiles[0]), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReactionWindowAnswerEvents_KeepHighlightUntilTheLocalCallRequestIsAnswered()
        {
            using (ReactionHighlightLifecycleFixture fixture = ReactionHighlightLifecycleFixture.Create(true))
            {
                fixture.RefreshForReactionWindowStarted();
                Assert.That(fixture.HighlightedTileCount, Is.EqualTo(1));

                fixture.SubmitResponse("South", "Pass");
                fixture.RefreshForReactionWindowAnswered();
                Assert.That(fixture.HighlightedTileCount, Is.EqualTo(1));

                fixture.SubmitResponse("East", "Pon");
                fixture.RefreshForReactionWindowAnswered();
                Assert.That(fixture.HighlightedTileCount, Is.Zero);

                fixture.RefreshForReactionWindowResolved();
                fixture.RefreshForReactionWindowClosed();
                Assert.That(fixture.HighlightedTileCount, Is.Zero);
            }
        }

        [Test]
        public void ReactionWindowStarted_DoesNotHighlightWhenOnlyAnotherSeatHasTheCallRequest()
        {
            using (ReactionHighlightLifecycleFixture fixture = ReactionHighlightLifecycleFixture.Create(false))
            {
                fixture.RefreshForReactionWindowStarted();

                Assert.That(fixture.HighlightedTileCount, Is.Zero);
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

            GameObject frontFace = new GameObject("FrontFace");
            frontFace.transform.SetParent(root.transform, false);
            MeshFilter frontFaceMeshFilter = frontFace.AddComponent<MeshFilter>();
            frontFaceMeshFilter.sharedMesh = CreateTestMesh("FrontFaceMesh");

            GameObject face = new GameObject("ReactionHighlightFace");
            face.transform.SetParent(root.transform, false);
            MeshFilter faceHighlightMeshFilter = face.AddComponent<MeshFilter>();
            faceHighlightMeshFilter.sharedMesh = CreateTestMesh("OriginalFaceHighlightMesh");
            MeshRenderer faceRenderer = face.AddComponent<MeshRenderer>();
            faceRenderer.sharedMaterial = CreateTestMaterial();
            faceRenderer.enabled = false;

            Component highlight = root.AddComponent(Type.GetType(HighlightTypeName, true));
            SetPrivateField(highlight, "highlightRenderer", renderer);
            SetPrivateField(highlight, "faceHighlightRenderer", faceRenderer);
            SetPrivateField(highlight, "shellTransform", shell.transform);
            SetPrivateField(highlight, "frontFaceMeshFilter", frontFaceMeshFilter);
            SetPrivateField(highlight, "faceHighlightMeshFilter", faceHighlightMeshFilter);

            return new HighlightTestFixture(
                root,
                renderer,
                faceRenderer,
                frontFaceMeshFilter,
                faceHighlightMeshFilter,
                highlight);
        }

        private static Material CreateTestMaterial()
        {
            Shader shader = Shader.Find(HighlightShaderName);
            Assert.That(shader, Is.Not.Null);
            return new Material(shader);
        }

        private static Mesh CreateTestMesh(string meshName)
        {
            Mesh mesh = new Mesh
            {
                name = meshName
            };
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2 };
            return mesh;
        }

        private static MeshRenderer CreateChildRenderer(Transform parent, string name, Material material)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static object ResolveReactionHighlightDiscardId(
            IList discards,
            object reactionRequest)
        {
            Type uiManagerType = RequireType(UiManagerTypeName);
            MethodInfo method = uiManagerType.GetMethod(
                "ResolveReactionHighlightDiscardId",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(IReadOnlyList<>).MakeGenericType(RequireType(DiscardRecordTypeName)), RequireType(ReactionDecisionRequestTypeName) },
                null);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new[] { (object)discards, reactionRequest });
        }

        private static object CreateReactionRequest(
            string sourceKind,
            string sourceSeat,
            string sourceTile,
            int sourceTurnIndex,
            params string[] optionKinds)
        {
            Type optionType = RequireType(ReactionDecisionOptionTypeName);
            IList options = CreateList(optionType);
            options.Add(CreateReactionOption("Pass"));
            for (int i = 0; i < optionKinds.Length; i++)
                options.Add(CreateReactionOption(optionKinds[i]));

            return Activator.CreateInstance(
                RequireType(ReactionDecisionRequestTypeName),
                1,
                Enum.Parse(RequireType(ReactionWindowSourceKindTypeName), sourceKind),
                Seat(sourceSeat),
                CreateTile(sourceTile),
                sourceTurnIndex,
                options);
        }

        private static object CreateReactionOption(string kind)
        {
            object answerKind = Enum.Parse(RequireType(ReactionWindowSeatAnswerKindTypeName), kind);
            if (kind != "Chi")
            {
                return Activator.CreateInstance(
                    RequireType(ReactionDecisionOptionTypeName),
                    answerKind,
                    null);
            }

            IList chiOptions = CreateList(RequireType(ReactionDecisionChiOptionTypeName));
            chiOptions.Add(Activator.CreateInstance(
                RequireType(ReactionDecisionChiOptionTypeName),
                1,
                CreateTiles("3m", "4m"),
                CreateTiles("3m", "4m", "5m")));
            return Activator.CreateInstance(
                RequireType(ReactionDecisionOptionTypeName),
                answerKind,
                chiOptions);
        }

        private static Component CreateDiscardRiverPresenter(
            Transform parent,
            out Component riverView)
        {
            Component presenter = new GameObject("Presenter")
                .AddComponent(RequireType(PlayerAreaPresenterTypeName));
            presenter.transform.SetParent(parent, false);
            Component controller = new GameObject("SelfController")
                .AddComponent(RequireType(PlayerUiControllerTypeName));
            controller.transform.SetParent(presenter.transform, false);
            riverView = new GameObject("DiscardRiver")
                .AddComponent(RequireType(DiscardRiverViewTypeName));
            riverView.transform.SetParent(controller.transform, false);
            SetPrivateField(controller, "discardRiverView", riverView);
            SetPrivateField(presenter, "selfBottomPlayerUiController", controller);
            return presenter;
        }

        private static Component CreateAllDiscardRiverPresenter(
            Transform parent,
            List<Component> riverViews)
        {
            Component presenter = new GameObject("Presenter")
                .AddComponent(RequireType(PlayerAreaPresenterTypeName));
            presenter.transform.SetParent(parent, false);
            string[] fields =
            {
                "selfBottomPlayerUiController",
                "nextLeftPlayerUiController",
                "acrossTopPlayerUiController",
                "previousRightPlayerUiController"
            };
            for (int i = 0; i < fields.Length; i++)
            {
                Component controller = new GameObject($"Controller{i}")
                    .AddComponent(RequireType(PlayerUiControllerTypeName));
                controller.transform.SetParent(presenter.transform, false);
                Component riverView = new GameObject($"DiscardRiver{i}")
                    .AddComponent(RequireType(DiscardRiverViewTypeName));
                riverView.transform.SetParent(controller.transform, false);
                SetPrivateField(controller, "discardRiverView", riverView);
                SetPrivateField(presenter, fields[i], controller);
                riverViews.Add(riverView);
            }

            return presenter;
        }

        private static bool IsReactionHighlighted(Component tileView)
        {
            Component reactionHighlight = tileView.GetComponent(RequireType(HighlightTypeName));
            Assert.That(reactionHighlight, Is.Not.Null);
            return (bool)GetProperty(reactionHighlight, "IsHighlighted");
        }

        private static Component[] GetTileViews(GameObject root)
        {
            return root.GetComponentsInChildren(RequireType(TileViewTypeName), true);
        }

        private static object CreateDiscard(int id, string seatName, string tileCode, int turnIndex)
        {
            return Activator.CreateInstance(
                RequireType(DiscardRecordTypeName),
                id,
                Seat(seatName),
                CreateTile(tileCode),
                turnIndex,
                Enum.Parse(RequireType(DiscardSourceTypeName), "Hand"),
                false);
        }

        private static IDictionary CreateClaims(int discardId)
        {
            Type claimType = RequireType(DiscardClaimTypeName);
            IDictionary claims = (IDictionary)Activator.CreateInstance(
                typeof(Dictionary<,>).MakeGenericType(typeof(int), claimType));
            claims.Add(discardId, Activator.CreateInstance(claimType));
            return claims;
        }

        private static IList CreateTiles(params string[] codes)
        {
            IList tiles = CreateList(RequireType(TileTypeName));
            for (int i = 0; i < codes.Length; i++)
                tiles.Add(CreateTile(codes[i]));

            return tiles;
        }

        private static IList CreateList(Type itemType)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        }

        private static object CreateTile(string code)
        {
            return Activator.CreateInstance(RequireType(TileTypeName), code);
        }

        private static object Seat(string name)
        {
            return Enum.Parse(RequireType(SeatIdTypeName), name);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName, true);
            Assert.That(type, Is.Not.Null, $"Missing type: {typeName}");
            return type;
        }

        private sealed class ReactionHighlightLifecycleFixture : IDisposable
        {
            private readonly MahjongGameFlowTestSession session;
            private readonly GameObject uiRoot;
            private readonly Component uiManager;
            private readonly List<Component> riverViews;
            private readonly Dictionary<string, object> providers;
            private readonly object reactionWindow;
            private bool disposed;

            private ReactionHighlightLifecycleFixture(
                MahjongGameFlowTestSession session,
                GameObject uiRoot,
                Component uiManager,
                List<Component> riverViews,
                Dictionary<string, object> providers,
                object reactionWindow)
            {
                this.session = session;
                this.uiRoot = uiRoot;
                this.uiManager = uiManager;
                this.riverViews = riverViews;
                this.providers = providers;
                this.reactionWindow = reactionWindow;
            }

            public int HighlightedTileCount
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < riverViews.Count; i++)
                    {
                        Component[] tileViews = GetTileViews(riverViews[i].gameObject);
                        for (int j = 0; j < tileViews.Length; j++)
                        {
                            if (IsReactionHighlighted(tileViews[j]))
                                count++;
                        }
                    }

                    return count;
                }
            }

            public static ReactionHighlightLifecycleFixture Create(bool includeLocalCall)
            {
                MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                    new MahjongGameFlowTestOptions
                    {
                        RootName = "ReactionHighlightLifecycleFlow",
                        AddEventNotifier = true,
                        LogWarnings = false,
                        ParticipantCount = 3,
                        InitialHandTileCount = 0,
                        AutoStart = false,
                        UseFixedRandomSeed = true,
                        FixedRandomSeed = 12345,
                        EnableAutoDraw = false,
                        RandomizeSelfSeat = false,
                        FixedSelfSeatName = "East"
                    });
                GameObject uiRoot = null;
                try
                {
                    Dictionary<string, object> providers = ConfigureAllLocalHumanPlayers(session);
                    object start = session.Reflection.Invoke(session.GameFlow, "TryStartNewRound");
                    Assert.That((bool)session.Reflection.GetProperty(start, "IsValid"), Is.True);

                    int turnIndex = (int)session.Reflection.GetProperty(session.CurrentState, "TurnIndex");
                    object sourceDiscard = session.Reflection.Invoke(
                        session.CurrentState,
                        "AddDiscard",
                        session.DataFactory.CreateDiscardRecord("West", "5m", turnIndex));
                    IList candidates = CreateList(RequireType(ReactionWindowCandidateTypeName));
                    if (includeLocalCall)
                        candidates.Add(CreatePonCandidate(session, "East"));
                    candidates.Add(CreatePonCandidate(session, "South"));
                    object reactionWindow = session.Reflection.Invoke(
                        session.CurrentState,
                        "BeginReactionWindow",
                        sourceDiscard,
                        candidates);
                    object[] beginRequestArguments = { reactionWindow, null };
                    Assert.That(
                        (bool)session.Reflection.Invoke(
                            session.GameFlow,
                            "TryBeginReactionSeatAnswerRequests",
                            beginRequestArguments),
                        Is.True,
                        beginRequestArguments[1] as string);

                    uiRoot = new GameObject("ReactionHighlightLifecycleUi");
                    Component uiManager = uiRoot.AddComponent(RequireType(UiManagerTypeName));
                    List<Component> riverViews = new List<Component>();
                    Component presenter = CreateAllDiscardRiverPresenter(uiRoot.transform, riverViews);
                    Component tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiverVariantPath)
                        .GetComponent(RequireType(TileViewTypeName));
                    for (int i = 0; i < riverViews.Count; i++)
                        SetPrivateField(riverViews[i], "tilePrefab", tilePrefab);

                    SetPrivateField(uiManager, "gameFlow", session.GameFlow);
                    SetPrivateField(uiManager, "playerArea3DPresenter", presenter);
                    return new ReactionHighlightLifecycleFixture(
                        session,
                        uiRoot,
                        uiManager,
                        riverViews,
                        providers,
                        reactionWindow);
                }
                catch
                {
                    if (uiRoot != null)
                        UnityEngine.Object.DestroyImmediate(uiRoot);
                    session.Dispose();
                    throw;
                }
            }

            public void RefreshForReactionWindowStarted()
            {
                Invoke(uiManager, "HandleReactionWindowChanged", reactionWindow);
            }

            public void RefreshForReactionWindowAnswered()
            {
                Invoke(uiManager, "HandleReactionWindowAnswered", (object)null);
            }

            public void RefreshForReactionWindowResolved()
            {
                Invoke(
                    uiManager,
                    "HandleReactionWindowResolved",
                    Activator.CreateInstance(
                        RequireType("MahjongPrototype.Domain.ReactionWindowResolution, Assembly-CSharp")));
            }

            public void RefreshForReactionWindowClosed()
            {
                Invoke(
                    uiManager,
                    "HandleReactionWindowClosed",
                    session.Reflection.GetProperty(reactionWindow, "WindowId"));
            }

            public void SubmitResponse(string seatName, string answerKind)
            {
                object seat = Seat(seatName);
                object playerId = session.Reflection.GetProperty(
                    session.Reflection.Invoke(session.CurrentState, "GetSeatSlot", seat),
                    "PlayerId");
                object[] pendingArguments = { playerId, null };
                Assert.That(
                    (bool)session.Reflection.Invoke(
                        session.GameFlow,
                        "TryGetPendingReactionDecisionRequest",
                        pendingArguments),
                    Is.True);
                object request = pendingArguments[1];
                object reaction = session.Reflection.GetProperty(request, "Reaction");
                object response = session.Reflection.CreateInstance(
                    RequireType(DecisionResponseTypeName),
                    session.Reflection.GetProperty(request, "RequestId"),
                    Enum.Parse(RequireType(DecisionKindTypeName), "Reaction"),
                    playerId,
                    session.Reflection.GetProperty(request, "ActorSeat"),
                    session.Reflection.GetProperty(request, "TurnIndex"),
                    true,
                    session.Reflection.CreateInstance(
                        RequireType(ReactionDecisionResponseTypeName),
                        session.Reflection.GetProperty(reaction, "WindowId"),
                        Enum.Parse(RequireType(ReactionWindowSeatAnswerKindTypeName), answerKind),
                        null));
                Assert.That(
                    (bool)session.Reflection.Invoke(
                        providers[playerId.ToString()],
                        "TrySubmitResponse",
                        response),
                    Is.True);
                session.Reflection.Invoke(
                    session.Reflection.GetProperty(session.GameFlow, "DecisionCoordinator"),
                    "Pump");
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                if (uiRoot != null)
                    UnityEngine.Object.DestroyImmediate(uiRoot);
                session.Dispose();
            }

            private static Dictionary<string, object> ConfigureAllLocalHumanPlayers(
                MahjongGameFlowTestSession session)
            {
                ReflectionTestAccess reflection = session.Reflection;
                IList participants = CreateList(RequireType(MatchParticipantTypeName));
                IList registrations = CreateList(RequireType(DecisionProviderRegistrationTypeName));
                Dictionary<string, object> providers = new Dictionary<string, object>();
                for (int i = 1; i <= 3; i++)
                {
                    string playerName = $"Player{i}";
                    object playerId = session.DataFactory.ParsePlayerId(playerName);
                    participants.Add(reflection.CreateInstance(
                        RequireType(MatchParticipantTypeName),
                        playerId,
                        Enum.Parse(RequireType(ParticipantKindTypeName), "Human")));
                    object provider = Activator.CreateInstance(RequireType(LocalUiDecisionProviderTypeName));
                    providers.Add(playerName, provider);
                    registrations.Add(reflection.CreateInstance(
                        RequireType(DecisionProviderRegistrationTypeName),
                        playerId,
                        Enum.Parse(RequireType(DecisionProviderRouteTypeName), "LocalUi"),
                        provider));
                }

                reflection.Invoke(
                    session.GameFlow,
                    "ConfigureMatch",
                    reflection.CreateInstance(RequireType(MatchRosterTypeName), participants),
                    reflection.CreateInstance(RequireType(DecisionProviderRegistryTypeName), registrations));
                return providers;
            }

            private static object CreatePonCandidate(
                MahjongGameFlowTestSession session,
                string seatName)
            {
                return session.Reflection.InvokeStatic(
                    RequireType(ReactionWindowCandidateTypeName),
                    "CreatePon",
                    Seat(seatName),
                    CreateTile("5m"));
            }
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

            public HighlightTestFixture(
                GameObject root,
                MeshRenderer renderer,
                MeshRenderer faceRenderer,
                MeshFilter frontFaceMeshFilter,
                MeshFilter faceHighlightMeshFilter,
                Component highlight)
            {
                this.root = root;
                Renderer = renderer;
                FaceRenderer = faceRenderer;
                FrontFaceMeshFilter = frontFaceMeshFilter;
                FaceHighlightMeshFilter = faceHighlightMeshFilter;
                Highlight = (Behaviour)highlight;
            }

            public MeshRenderer Renderer { get; }

            public MeshRenderer FaceRenderer { get; }

            public MeshFilter FrontFaceMeshFilter { get; }

            public MeshFilter FaceHighlightMeshFilter { get; }

            public Behaviour Highlight { get; }

            public void Dispose()
            {
                if (Renderer != null && Renderer.sharedMaterial != null)
                    UnityEngine.Object.DestroyImmediate(Renderer.sharedMaterial);

                if (FaceRenderer != null && FaceRenderer.sharedMaterial != null)
                    UnityEngine.Object.DestroyImmediate(FaceRenderer.sharedMaterial);

                if (FrontFaceMeshFilter != null && FrontFaceMeshFilter.sharedMesh != null)
                    UnityEngine.Object.DestroyImmediate(FrontFaceMeshFilter.sharedMesh);

                if (FaceHighlightMeshFilter != null && FaceHighlightMeshFilter.sharedMesh != null)
                    UnityEngine.Object.DestroyImmediate(FaceHighlightMeshFilter.sharedMesh);

                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
