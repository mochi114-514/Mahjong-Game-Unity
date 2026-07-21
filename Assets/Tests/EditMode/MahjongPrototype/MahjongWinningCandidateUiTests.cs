using System;
using System.Linq;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongWinningCandidatePrefabAndSceneTests
    {
        private const string MainScenePath = "Assets/Scenes/Mahjong Prototype.unity";
        private const string GroupPrefabPath =
            "Assets/Prefab/Mahjong Winning Candidate Group.prefab";
        private const string CandidatePrefabPath =
            "Assets/Prefab/Mahjong Winning Tile Candidate.prefab";
        private const string CatalogPath =
            "Assets/Scripts/MahjongPrototype/ScriptableObjects/MahjongTileSpriteCatalog.asset";
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongWinningCandidateController, Assembly-CSharp";
        private const string GroupViewTypeName =
            "MahjongPrototype.UI.MahjongWinningCandidateGroupView, Assembly-CSharp";
        private const string CandidateViewTypeName =
            "MahjongPrototype.UI.MahjongWinningTileCandidateView, Assembly-CSharp";
        private const string TileSpriteViewTypeName =
            "MahjongPrototype.UI.MahjongTileSpriteView, Assembly-CSharp";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

        [Test]
        public void CandidatePrefab_HasSpriteAndNonRaycastCountWithoutButtonOrBackground()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            GameObject prefab = LoadPrefab(CandidatePrefabPath);
            Component view = RequireComponent(prefab, reflection.RequireType(CandidateViewTypeName));
            Component tileView = RequireComponent(
                prefab,
                reflection.RequireType(TileSpriteViewTypeName));
            Component countText = prefab.GetComponentsInChildren(
                    reflection.RequireType(TmpTextTypeName),
                    true)
                .Single(text => text.name == "CountText");
            Image tileImage = tileView.GetComponent<Image>();

            Assert.That(reflection.GetPrivateField(view, "tileSpriteView"), Is.SameAs(tileView));
            Assert.That(reflection.GetPrivateField(view, "countText"), Is.SameAs(countText));
            Assert.That(tileImage, Is.Not.Null);
            Assert.That(tileImage.raycastTarget, Is.False);
            Assert.That(reflection.GetProperty(countText, "raycastTarget"), Is.False);
            Assert.That(prefab.GetComponent<Image>(), Is.Null);
            Assert.That(prefab.GetComponentInChildren<Button>(true), Is.Null);
        }

        [Test]
        public void GroupPrefab_HasOptionalHeadingAndEightColumnWrappingContainer()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            GameObject prefab = LoadPrefab(GroupPrefabPath);
            Component groupView = RequireComponent(prefab, reflection.RequireType(GroupViewTypeName));
            GridLayoutGroup grid = prefab.GetComponentInChildren<GridLayoutGroup>(true);
            Component heading = prefab.GetComponentsInChildren(
                    reflection.RequireType(TmpTextTypeName),
                    true)
                .Single(text => text.name == "DiscardHeading");

            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(8));
            Assert.That(reflection.GetProperty(heading, "raycastTarget"), Is.False);
            Assert.That(reflection.GetPrivateField(groupView, "headingText"), Is.SameAs(heading));
            Assert.That(reflection.GetPrivateField(groupView, "candidateContainer"),
                Is.SameAs(grid.transform));
            Assert.That(prefab.GetComponentInChildren<Image>(true), Is.Null);
            Assert.That(prefab.GetComponentInChildren<Button>(true), Is.Null);
        }

        [Test]
        public void MainScene_ConnectsOneNonRaycastPanelControllerCatalogAndPrefabs()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

            try
            {
                Component uiManager = FindSceneComponent(
                    scene,
                    reflection.RequireType(UiManagerTypeName));
                Component controller = FindSceneComponent(
                    scene,
                    reflection.RequireType(ControllerTypeName));
                SerializedObject uiSerialized = new SerializedObject(uiManager);
                SerializedObject controllerSerialized = new SerializedObject(controller);
                GameObject panelRoot = (GameObject)controllerSerialized
                    .FindProperty("root").objectReferenceValue;

                Assert.That(panelRoot, Is.Not.Null);
                Assert.That(panelRoot.name, Is.EqualTo("WinningCandidateRoot"));
                Assert.That(panelRoot.activeSelf, Is.False);
                Assert.That(panelRoot.GetComponent<Image>().raycastTarget, Is.False);
                Assert.That(panelRoot.GetComponent<Outline>(), Is.Not.Null);
                Assert.That(panelRoot.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(panelRoot.GetComponentsInChildren<Graphic>(true),
                    Has.All.Matches<Graphic>(graphic => !graphic.raycastTarget));
                Assert.That(
                    uiSerialized.FindProperty("winningCandidateController").objectReferenceValue,
                    Is.SameAs(controller));
                Assert.That(
                    AssetDatabase.GetAssetPath(controllerSerialized
                        .FindProperty("groupViewPrefab").objectReferenceValue),
                    Is.EqualTo(GroupPrefabPath));
                Assert.That(
                    AssetDatabase.GetAssetPath(controllerSerialized
                        .FindProperty("candidateViewPrefab").objectReferenceValue),
                    Is.EqualTo(CandidatePrefabPath));
                Assert.That(
                    AssetDatabase.GetAssetPath(controllerSerialized
                        .FindProperty("tileSpriteCatalog").objectReferenceValue),
                    Is.EqualTo(CatalogPath));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Prefab not found: {path}");
            return prefab;
        }

        private static Component RequireComponent(GameObject root, Type type)
        {
            Component component = root.GetComponentInChildren(type, true);
            Assert.That(component, Is.Not.Null, $"Component not found: {type.FullName}");
            return component;
        }

        private static Component FindSceneComponent(Scene scene, Type type)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Component component = root.GetComponentInChildren(type, true);
                if (component != null)
                    return component;
            }

            Assert.Fail($"Scene component not found: {type.FullName}");
            return null;
        }
    }

    public sealed class MahjongWinningCandidateControllerTests
    {
        private const string MainScenePath = "Assets/Scenes/Mahjong Prototype.unity";
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongWinningCandidateController, Assembly-CSharp";
        private const string GroupViewTypeName =
            "MahjongPrototype.UI.MahjongWinningCandidateGroupView, Assembly-CSharp";
        private const string CandidateViewTypeName =
            "MahjongPrototype.UI.MahjongWinningTileCandidateView, Assembly-CSharp";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";

        [Test]
        public void SetCandidates_UsesOneHeadinglessGroupAndKeepsZeroCountCandidate()
        {
            WinningTileCandidateEvaluatorTestDriver evaluator =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object state = evaluator.CreateGameState();
            evaluator.AddHand(
                state,
                "East",
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C");
            evaluator.AddDiscard(state, "South", "C", 1);
            evaluator.AddDiscard(state, "West", "C", 2);
            evaluator.AddDiscard(state, "North", "C", 3);
            object candidates = evaluator.EvaluateCurrent(state);

            WithSceneController((reflection, controller, root) =>
            {
                reflection.Invoke(controller, "SetCandidates", candidates);

                Assert.That(root.activeSelf, Is.True);
                Assert.That(reflection.GetProperty(controller, "SpawnedGroupCount"),
                    Is.EqualTo(1));
                Component group = root.GetComponentInChildren(
                    reflection.RequireType(GroupViewTypeName),
                    true);
                GameObject headingRoot = (GameObject)reflection.GetPrivateField(
                    group,
                    "headingRoot");
                Component candidate = root.GetComponentInChildren(
                    reflection.RequireType(CandidateViewTypeName),
                    true);
                Component countText = (Component)reflection.GetPrivateField(
                    candidate,
                    "countText");

                Assert.That(headingRoot.activeSelf, Is.False);
                Assert.That(reflection.GetProperty(countText, "text").ToString(),
                    Does.StartWith("0"));
            });
        }

        [Test]
        public void SetGroups_SingleGroupOmitsHeadingAndKeepsZeroCountCandidate()
        {
            WinningTileCandidateEvaluatorTestDriver evaluator =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object state = evaluator.CreateGameState();
            evaluator.AddHand(
                state,
                "East",
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C");
            evaluator.SetDrawnTile(state, "East", "1m");
            evaluator.AddDiscard(state, "South", "C", 1);
            evaluator.AddDiscard(state, "West", "C", 2);
            evaluator.AddDiscard(state, "North", "C", 3);
            object discards = evaluator.CreateReachCandidateList(
                evaluator.CreateReachCandidate("Hand", 0, "1m"),
                evaluator.CreateReachCandidate("DrawnTile", -1, "1m"));
            object groups = evaluator.GroupReachCandidates(state, discards);

            WithSceneController((reflection, controller, root) =>
            {
                reflection.Invoke(controller, "SetGroups", groups);

                Assert.That(root.activeSelf, Is.True);
                Assert.That(reflection.GetProperty(controller, "SpawnedGroupCount"),
                    Is.EqualTo(1));
                Component group = root.GetComponentInChildren(
                    reflection.RequireType(GroupViewTypeName),
                    true);
                GameObject headingRoot = (GameObject)reflection.GetPrivateField(
                    group,
                    "headingRoot");
                Component candidate = root.GetComponentInChildren(
                    reflection.RequireType(CandidateViewTypeName),
                    true);
                Component countText = (Component)reflection.GetPrivateField(
                    candidate,
                    "countText");

                Assert.That(headingRoot.activeSelf, Is.False);
                Assert.That(reflection.GetProperty(countText, "text"), Is.EqualTo("0枚"));
                Assert.That(reflection.GetProperty(countText, "raycastTarget"), Is.False);
            });
        }

        [Test]
        public void SetGroups_RyanmenDisplaysBothCandidatesInTypeOrder()
        {
            WinningTileCandidateEvaluatorTestDriver evaluator =
                WinningTileCandidateEvaluatorTestDriver.Create();
            object state = evaluator.CreateGameState();
            evaluator.AddHand(
                state,
                "East",
                "1p 2p 3p 1s 2s 3s E E E P P 4m 5m");
            evaluator.SetDrawnTile(state, "East", "1p");
            object discards = evaluator.CreateReachCandidateList(
                evaluator.CreateReachCandidate("Hand", 0, "1p"),
                evaluator.CreateReachCandidate("DrawnTile", -1, "1p"));
            object groups = evaluator.GroupReachCandidates(state, discards);

            WithSceneController((reflection, controller, root) =>
            {
                reflection.Invoke(controller, "SetGroups", groups);

                Component[] candidates = root.GetComponentsInChildren(
                    reflection.RequireType(CandidateViewTypeName),
                    true);
                Assert.That(candidates, Has.Length.EqualTo(2));
                Assert.That(candidates[0].transform.GetSiblingIndex(),
                    Is.LessThan(candidates[1].transform.GetSiblingIndex()));
                Assert.That(candidates.Select(candidate =>
                        reflection.GetProperty(
                            reflection.GetPrivateField(candidate, "countText"),
                            "text").ToString()),
                    Is.All.EndsWith("枚"));
            });
        }

        [Test]
        public void SetGroups_DifferentWaitsUseHeadingsInsideOneRootAndClearRemovesAll()
        {
            WinningTileCandidateEvaluatorTestDriver evaluator =
                WinningTileCandidateEvaluatorTestDriver.Create();
            const string hand =
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m";
            object state = evaluator.CreateGameState();
            evaluator.AddHand(state, "East", hand);
            evaluator.SetDrawnTile(state, "East", "6m");
            object reach = evaluator.CheckReach(state, hand, "6m");
            object groups = evaluator.GroupReachCandidates(
                state,
                evaluator.ReachCandidates(reach));

            WithSceneController((reflection, controller, root) =>
            {
                reflection.Invoke(controller, "SetGroups", groups);

                Component[] groupViews = root.GetComponentsInChildren(
                    reflection.RequireType(GroupViewTypeName),
                    true);
                Assert.That(groupViews.Length, Is.GreaterThan(1));
                foreach (Component group in groupViews)
                {
                    GameObject headingRoot = (GameObject)reflection.GetPrivateField(
                        group,
                        "headingRoot");
                    Component heading = (Component)reflection.GetPrivateField(
                        group,
                        "headingText");
                    Assert.That(headingRoot.activeSelf, Is.True);
                    Assert.That(
                        reflection.GetProperty(heading, "text").ToString(),
                        Does.EndWith("切り"));
                }

                Assert.That(root.GetComponentsInChildren<Outline>(true), Has.Length.EqualTo(1));
                reflection.Invoke(controller, "Clear");
                Assert.That(root.activeSelf, Is.False);
                Assert.That(reflection.GetProperty(controller, "SpawnedGroupCount"),
                    Is.EqualTo(0));
            });
        }

        private static void WithSceneController(
            Action<ReflectionTestAccess, Component, GameObject> assertion)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

            try
            {
                Component controller = null;
                Type controllerType = reflection.RequireType(ControllerTypeName);
                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                {
                    controller = sceneRoot.GetComponentInChildren(controllerType, true);
                    if (controller != null)
                        break;
                }

                Assert.That(controller, Is.Not.Null);
                GameObject root = (GameObject)reflection.GetPrivateField(controller, "root");
                assertion(reflection, controller, root);
                reflection.Invoke(controller, "Clear");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
