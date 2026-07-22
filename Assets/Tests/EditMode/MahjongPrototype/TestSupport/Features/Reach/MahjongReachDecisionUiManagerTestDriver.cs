using System;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class MahjongReachDecisionUiManagerTestDriver : IDisposable
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongReachDecisionController, Assembly-CSharp";
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string WinningCandidateControllerTypeName =
            "MahjongPrototype.UI.MahjongWinningCandidateController, Assembly-CSharp";
        private const string WinningGroupViewTypeName =
            "MahjongPrototype.UI.MahjongWinningCandidateGroupView, Assembly-CSharp";
        private const string WinningTileViewTypeName =
            "MahjongPrototype.UI.MahjongWinningTileCandidateView, Assembly-CSharp";
        private const string TileHoverInfoTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileHoverInfo, Assembly-CSharp";
        private const string DrawResultTypeName =
            "MahjongPrototype.Services.DrawResult, Assembly-CSharp";
        private const string DrawPurposeTypeName =
            "MahjongPrototype.Services.DrawPurpose, Assembly-CSharp";
        private const string DrawSourceTypeName =
            "MahjongPrototype.Services.DrawSource, Assembly-CSharp";
        private const string ActiveSkillEffectTypeName =
            "MahjongPrototype.Skills.ActiveSkillEffect, Assembly-CSharp";
        private const string GroupPrefabPath =
            "Assets/Prefab/Mahjong Winning Candidate Group.prefab";
        private const string CandidatePrefabPath =
            "Assets/Prefab/Mahjong Winning Tile Candidate.prefab";
        private const string CatalogPath =
            "Assets/Scripts/MahjongPrototype/ScriptableObjects/MahjongTileSpriteCatalog.asset";

        private readonly ReflectionTestAccess reflection;
        private readonly UnityObjectTestOwner owner;
        private readonly Type controllerType;
        private readonly Type uiManagerType;
        private readonly Type winningCandidateControllerType;
        private readonly GameObject root;
        private readonly GameObject uiObject;
        private readonly Component uiManager;
        private ReachGameFlowTestSupport flowSupport;
        private GameObject decisionArea;
        private Component decisionController;
        private GameObject winningCandidateRoot;
        private Component winningCandidateController;
        private object currentHoverInfo;
        private object savedReachCandidate;
        private bool disposed;

        private MahjongReachDecisionUiManagerTestDriver(
            ReflectionTestAccess reflection,
            UnityObjectTestOwner owner,
            Type controllerType,
            Type uiManagerType,
            Type winningCandidateControllerType)
        {
            this.reflection = reflection;
            this.owner = owner;
            this.controllerType = controllerType;
            this.uiManagerType = uiManagerType;
            this.winningCandidateControllerType = winningCandidateControllerType;

            root = owner.Own(new GameObject("MahjongReachDecisionUiManagerTestDriver"));
            uiObject = new GameObject("MahjongUiManager");
            uiObject.transform.SetParent(root.transform);
            uiObject.SetActive(false);
            uiManager = uiObject.AddComponent(this.uiManagerType);
        }

        public static MahjongReachDecisionUiManagerTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            return new MahjongReachDecisionUiManagerTestDriver(
                reflection,
                new UnityObjectTestOwner(),
                reflection.RequireType(ControllerTypeName),
                reflection.RequireType(UiManagerTypeName),
                reflection.RequireType(WinningCandidateControllerTypeName));
        }

        public void PrepareReachableGameState()
        {
            flowSupport = ReachGameFlowTestSupport.Create("ReachDecisionUiManagerGameFlowTest");
            flowSupport.DrawReachableHand();
            reflection.SetPrivateField(uiManager, "gameFlow", flowSupport.GameFlow);
        }

        public void CreateDecisionArea(string name, bool active)
        {
            decisionArea = new GameObject(name);
            decisionArea.transform.SetParent(uiObject.transform);
            decisionArea.SetActive(active);
        }

        public void AddDecisionControllerToArea()
        {
            EnsureDecisionArea();
            decisionController = decisionArea.AddComponent(controllerType);
            reflection.SetPrivateField(decisionController, "reachDecisionRoot", decisionArea);
        }

        public void AssignControllerToUiManager()
        {
            reflection.SetPrivateField(uiManager, "reachDecisionController", decisionController);
        }

        public void CreateAndAssignWinningCandidateController()
        {
            winningCandidateRoot = new GameObject("WinningCandidateRoot");
            winningCandidateRoot.transform.SetParent(uiObject.transform);
            GameObject groups = new GameObject("Groups");
            groups.transform.SetParent(winningCandidateRoot.transform);

            winningCandidateController =
                uiObject.AddComponent(winningCandidateControllerType);
            GameObject groupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroupPrefabPath);
            GameObject candidatePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePrefabPath);
            Assert.That(groupPrefab, Is.Not.Null);
            Assert.That(candidatePrefab, Is.Not.Null);

            reflection.SetPrivateField(
                winningCandidateController,
                "root",
                winningCandidateRoot);
            reflection.SetPrivateField(
                winningCandidateController,
                "groupsContainer",
                groups.transform);
            reflection.SetPrivateField(
                winningCandidateController,
                "groupViewPrefab",
                groupPrefab.GetComponent(reflection.RequireType(WinningGroupViewTypeName)));
            reflection.SetPrivateField(
                winningCandidateController,
                "candidateViewPrefab",
                candidatePrefab.GetComponent(reflection.RequireType(WinningTileViewTypeName)));
            reflection.SetPrivateField(
                winningCandidateController,
                "tileSpriteCatalog",
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath));
            reflection.SetPrivateField(
                uiManager,
                "winningCandidateController",
                winningCandidateController);
            winningCandidateRoot.SetActive(false);
        }

        public void RefreshReachDecision()
        {
            reflection.Invoke(uiManager, "RefreshReachDecision", flowSupport.CurrentState);
        }

        public void RefreshReachDecisionWithNullState()
        {
            reflection.Invoke(
                uiManager,
                "RefreshReachDecision",
                new object[] { null });
        }

        public void InvokeUiManagerOnDisable()
        {
            reflection.Invoke(uiManager, "OnDisable");
        }

        public void AcceptReach()
        {
            flowSupport.RequestDeclareReach();
        }

        public void DeclineReach()
        {
            flowSupport.RequestDeclineReach();
        }

        public void HoverFirstReachCandidate(string seatName = "East")
        {
            HoverReachCandidate(0, seatName);
        }

        public void HoverReachCandidate(int index, string seatName = "East")
        {
            savedReachCandidate = GetReachCandidate(index);
            HoverCandidate(savedReachCandidate, seatName);
        }

        public void HoverFirstReachCandidateFromSource(string sourceName)
        {
            object candidates = reflection.GetProperty(
                flowSupport.CurrentState,
                "ReachDiscardCandidates");
            int count = flowSupport.Collections.Count(candidates);
            for (int i = 0; i < count; i++)
            {
                object candidate = flowSupport.Collections.Item(candidates, i);
                if (reflection.GetProperty(candidate, "Source").ToString() != sourceName)
                    continue;

                savedReachCandidate = candidate;
                HoverCandidate(savedReachCandidate, "East");
                return;
            }

            Assert.Fail($"A reach candidate from {sourceName} was not found.");
        }

        public void HoverSavedReachCandidate(string seatName = "East")
        {
            Assert.That(savedReachCandidate, Is.Not.Null);
            HoverCandidate(savedReachCandidate, seatName);
        }

        public void SelectFirstReachCandidate()
        {
            savedReachCandidate = GetReachCandidate(0);
            SelectCandidate(savedReachCandidate);
        }

        public void SelectReachCandidate(int index)
        {
            savedReachCandidate = GetReachCandidate(index);
            SelectCandidate(savedReachCandidate);
        }

        public void SelectSavedReachCandidate()
        {
            Assert.That(savedReachCandidate, Is.Not.Null);
            SelectCandidate(savedReachCandidate);
        }

        public void SelectReachNonCandidate()
        {
            object candidates = reflection.GetProperty(
                flowSupport.CurrentState,
                "ReachDiscardCandidates");
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            object hand = reflection.GetProperty(player, "Hand");
            object handTiles = reflection.Invoke(hand, "GetTiles");

            for (int handIndex = 0;
                 handIndex < flowSupport.Collections.Count(handTiles);
                 handIndex++)
            {
                object tile = flowSupport.Collections.Item(handTiles, handIndex);
                if (ContainsReachCandidate(candidates, "Hand", handIndex, tile))
                    continue;

                reflection.Invoke(
                    uiManager,
                    "HandleHandTileClicked",
                    selfSeat,
                    handIndex,
                    tile);
                return;
            }

            Assert.Fail("A non-reach hand tile candidate was not found.");
        }

        public void SelectHandTile(int handIndex)
        {
            object candidate = savedReachCandidate ?? GetReachCandidate(0);
            object sourceSample = reflection.GetProperty(candidate, "Source");
            object handSource = Enum.Parse(sourceSample.GetType(), "Hand");
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            object hand = reflection.GetProperty(player, "Hand");
            object tile = flowSupport.Collections.Item(
                reflection.Invoke(hand, "GetTiles"),
                handIndex);
            reflection.Invoke(
                uiManager,
                "HandleHandTileClicked",
                selfSeat,
                handIndex,
                tile);
        }

        public void ClearSelectionFromTable()
        {
            reflection.Invoke(uiManager, "HandleTableInputSurfaceClicked");
        }

        public void HoverReachNonCandidate()
        {
            object candidates = reflection.GetProperty(
                flowSupport.CurrentState,
                "ReachDiscardCandidates");
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            object hand = reflection.GetProperty(player, "Hand");
            object handTiles = reflection.Invoke(hand, "GetTiles");

            for (int handIndex = 0;
                 handIndex < flowSupport.Collections.Count(handTiles);
                 handIndex++)
            {
                object tile = flowSupport.Collections.Item(handTiles, handIndex);
                if (ContainsReachCandidate(candidates, "Hand", handIndex, tile))
                    continue;

                object sourceSample = reflection.GetProperty(GetReachCandidate(0), "Source");
                object handSource = Enum.Parse(sourceSample.GetType(), "Hand");
                currentHoverInfo = CreateHoverInfo(
                    selfSeat,
                    handSource,
                    handIndex,
                    tile);
                reflection.Invoke(uiManager, "HandleTileHoverEntered", currentHoverInfo);
                return;
            }

            Assert.Fail("A non-reach hand tile candidate was not found.");
        }

        public void ExitCurrentHover()
        {
            BeginCurrentHoverExit();
            CompleteHoverReevaluation();
        }

        public void BeginCurrentHoverExit()
        {
            Assert.That(currentHoverInfo, Is.Not.Null);
            reflection.Invoke(uiManager, "HandleTileHoverExited", currentHoverInfo);
            currentHoverInfo = null;
        }

        public void CompleteHoverReevaluation()
        {
            reflection.Invoke(uiManager, "HandleTileHoverReevaluated");
        }

        public void RefreshReachDecisionUi()
        {
            reflection.Invoke(uiManager, "RefreshReachDecisionUi");
        }

        public void NotifyWinChecked()
        {
            reflection.Invoke(
                uiManager,
                "HandleWinChecked",
                flowSupport.DataFactory.ParseSeat("East"),
                0,
                false);
        }

        public void NotifyTurnStarted()
        {
            reflection.Invoke(
                uiManager,
                "HandleTurnStarted",
                flowSupport.DataFactory.ParseSeat("East"),
                0);
        }

        public void NotifyTileDrawn(string tileCode)
        {
            reflection.Invoke(uiManager, "HandleTileDrawn", CreateDrawResult(tileCode));
        }

        public void NotifyHandAutoSorted()
        {
            reflection.Invoke(
                uiManager,
                "HandleHandAutoSorted",
                flowSupport.DataFactory.ParseSeat("East"),
                0);
        }

        public void ClearDrawnTileDirectly()
        {
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            reflection.Invoke(player, "ClearDrawnTile");
        }

        public void HoverFirstHandTile()
        {
            HoverHandTile(0);
        }

        public void HoverHandTile(int handIndex)
        {
            object candidate = savedReachCandidate ?? GetReachCandidate(0);
            object sourceSample = reflection.GetProperty(candidate, "Source");
            object handSource = Enum.Parse(sourceSample.GetType(), "Hand");
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            object hand = reflection.GetProperty(player, "Hand");
            object tile = flowSupport.Collections.Item(
                reflection.Invoke(hand, "GetTiles"),
                handIndex);
            currentHoverInfo = CreateHoverInfo(selfSeat, handSource, handIndex, tile);
            reflection.Invoke(uiManager, "HandleTileHoverEntered", currentHoverInfo);
        }

        public void DeclareReachAndDiscardSavedCandidate()
        {
            Assert.That(savedReachCandidate, Is.Not.Null);
            flowSupport.RequestDeclareReach();
            string source = reflection.GetProperty(savedReachCandidate, "Source").ToString();
            if (source == "DrawnTile")
            {
                flowSupport.RequestDiscardDrawnTile();
                return;
            }

            flowSupport.RequestDiscard(
                (int)reflection.GetProperty(savedReachCandidate, "HandIndex"));
        }

        public void SetDrawnTileDirectly(string tileCode)
        {
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            if ((bool)reflection.GetProperty(player, "HasDrawnTile"))
                return;

            reflection.Invoke(
                player,
                "SetDrawnTile",
                flowSupport.DataFactory.CreateTile(tileCode));
        }

        public void HoverCurrentDrawnTile()
        {
            Assert.That(savedReachCandidate, Is.Not.Null);
            object selfSeat = flowSupport.DataFactory.ParseSeat("East");
            object player = reflection.Invoke(
                flowSupport.CurrentState,
                "GetPlayerSeat",
                selfSeat);
            object drawnTile = reflection.GetProperty(player, "DrawnTile");
            Assert.That(drawnTile, Is.Not.Null);
            object sourceSample = reflection.GetProperty(savedReachCandidate, "Source");
            object drawnSource = Enum.Parse(sourceSample.GetType(), "DrawnTile");
            currentHoverInfo = CreateHoverInfo(selfSeat, drawnSource, -1, drawnTile);
            reflection.Invoke(uiManager, "HandleTileHoverEntered", currentHoverInfo);
        }

        public void EnsureReachDecisionController()
        {
            reflection.Invoke(uiManager, "EnsureReachDecisionController");
        }

        public void ExpectWarning(string message)
        {
            LogAssert.Expect(LogType.Warning, message);
        }

        public bool IsReachDecisionPending =>
            (bool)reflection.GetProperty(flowSupport.CurrentState, "IsReachDecisionPending");

        public int ReachCandidateCount => flowSupport.Collections.Count(
            reflection.GetProperty(flowSupport.CurrentState, "ReachDiscardCandidates"));

        public bool IsSelfReachDeclared => flowSupport.IsReachDeclared("East");

        public bool DecisionAreaActive => decisionArea.activeSelf;

        public bool WinningCandidateRootActive =>
            winningCandidateRoot != null && winningCandidateRoot.activeSelf;

        public int SpawnedWinningGroupCount => winningCandidateController == null
            ? 0
            : (int)reflection.GetProperty(winningCandidateController, "SpawnedGroupCount");

        public int SpawnedWinningCandidateCount => winningCandidateRoot == null
            ? 0
            : winningCandidateRoot.GetComponentsInChildren(
                reflection.RequireType(WinningTileViewTypeName),
                true).Length;

        public bool HasHoveredSelfTile =>
            reflection.GetPrivateField(uiManager, "hoveredSelfTile") != null;

        public bool HasSelectedSelfTile =>
            reflection.GetPrivateField(uiManager, "selectedSelfTile") != null;

        public int SelectedHandIndex => (int)reflection.GetProperty(
            reflection.GetPrivateField(uiManager, "selectedSelfTile"),
            "HandIndex");

        public string SelectedTileIdentity
        {
            get
            {
                object selection = reflection.GetPrivateField(uiManager, "selectedSelfTile");
                return reflection.GetProperty(selection, "Source") + ":" +
                    reflection.GetProperty(selection, "HandIndex") + ":" +
                    reflection.GetProperty(selection, "Tile");
            }
        }

        public string HoveredTileIdentity
        {
            get
            {
                object hover = reflection.GetPrivateField(uiManager, "hoveredSelfTile");
                return reflection.GetProperty(hover, "Source") + ":" +
                    reflection.GetProperty(hover, "HandIndex") + ":" +
                    reflection.GetProperty(hover, "Tile");
            }
        }

        public string WinningCandidateSignature
        {
            get
            {
                if (winningCandidateController == null)
                    return string.Empty;

                object groups = reflection.GetPrivateField(
                    winningCandidateController,
                    "displayedGroups");
                if (flowSupport.Collections.Count(groups) <= 0)
                    return string.Empty;

                object group = flowSupport.Collections.Item(groups, 0);
                object candidates = reflection.GetProperty(group, "Candidates");
                return BuildDisplayedCandidateSignature(candidates);
            }
        }

        public string EvaluateAfterDiscardSignatureForReachCandidate(int index)
        {
            object evaluator = reflection.GetPrivateField(
                uiManager,
                "winningTileCandidateEvaluator");
            object candidates = reflection.Invoke(
                evaluator,
                "EvaluateAfterDiscard",
                flowSupport.CurrentState,
                flowSupport.DataFactory.ParseSeat("East"),
                GetReachCandidate(index));
            return BuildEvaluatorCandidateSignature(candidates);
        }

        public bool IsHoverReevaluationPending =>
            (bool)reflection.GetPrivateField(uiManager, "tileHoverReevaluationPending");

        public bool DecisionAreaHasController =>
            decisionArea.GetComponent(controllerType) != null;

        public bool UiManagerControllerReferenceIsNull =>
            reflection.GetPrivateField(uiManager, "reachDecisionController") == null;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flowSupport?.Dispose();
            owner.Dispose();
        }

        private void EnsureDecisionArea()
        {
            if (decisionArea == null)
                CreateDecisionArea("ReachDecisionArea", true);
        }

        private object GetReachCandidate(int index)
        {
            object candidates = reflection.GetProperty(
                flowSupport.CurrentState,
                "ReachDiscardCandidates");
            Assert.That(flowSupport.Collections.Count(candidates), Is.GreaterThan(index));
            return flowSupport.Collections.Item(candidates, index);
        }

        private string BuildDisplayedCandidateSignature(object candidates)
        {
            string[] states = new string[flowSupport.Collections.Count(candidates)];
            for (int i = 0; i < states.Length; i++)
            {
                object candidate = flowSupport.Collections.Item(candidates, i);
                states[i] = reflection.GetProperty(candidate, "TypeIndex") + ":" +
                    reflection.GetProperty(candidate, "VisibleRemainingCount");
            }

            return string.Join(",", states);
        }

        private string BuildEvaluatorCandidateSignature(object candidates)
        {
            string[] states = new string[flowSupport.Collections.Count(candidates)];
            for (int i = 0; i < states.Length; i++)
            {
                object candidate = flowSupport.Collections.Item(candidates, i);
                object tile = reflection.GetProperty(candidate, "Tile");
                states[i] = reflection.GetProperty(tile, "TypeIndex") + ":" +
                    reflection.GetProperty(candidate, "VisibleRemainingCount");
            }

            return string.Join(",", states);
        }

        private void HoverCandidate(object candidate, string seatName)
        {
            currentHoverInfo = CreateHoverInfo(
                flowSupport.DataFactory.ParseSeat(seatName),
                reflection.GetProperty(candidate, "Source"),
                (int)reflection.GetProperty(candidate, "HandIndex"),
                reflection.GetProperty(candidate, "Tile"));
            reflection.Invoke(uiManager, "HandleTileHoverEntered", currentHoverInfo);
        }

        private void SelectCandidate(object candidate)
        {
            object source = reflection.GetProperty(candidate, "Source");
            object seat = flowSupport.DataFactory.ParseSeat("East");
            int handIndex = (int)reflection.GetProperty(candidate, "HandIndex");
            object tile = reflection.GetProperty(candidate, "Tile");
            if (source.ToString() == "Hand")
            {
                reflection.Invoke(
                    uiManager,
                    "HandleHandTileClicked",
                    seat,
                    handIndex,
                    tile);
                return;
            }

            reflection.Invoke(uiManager, "HandleDrawnTileClicked", seat, tile);
        }

        private object CreateHoverInfo(
            object seat,
            object source,
            int handIndex,
            object tile)
        {
            return reflection.CreateInstance(
                reflection.RequireType(TileHoverInfoTypeName),
                seat,
                source,
                handIndex,
                tile);
        }

        private object CreateDrawResult(string tileCode)
        {
            Type drawResultType = reflection.RequireType(DrawResultTypeName);
            Type drawPurposeType = reflection.RequireType(DrawPurposeTypeName);
            Type drawSourceType = reflection.RequireType(DrawSourceTypeName);
            ConstructorInfo constructor = drawResultType.GetConstructor(new[]
            {
                typeof(bool),
                flowSupport.Types.SeatId,
                flowSupport.Types.Tile,
                drawPurposeType,
                drawSourceType,
                typeof(int),
                reflection.RequireType(ActiveSkillEffectTypeName),
                typeof(bool),
                typeof(bool),
                typeof(string)
            });
            Assert.That(constructor, Is.Not.Null);

            return constructor.Invoke(new[]
            {
                (object)true,
                flowSupport.DataFactory.ParseSeat("East"),
                flowSupport.DataFactory.CreateTile(tileCode),
                Enum.Parse(drawPurposeType, "TurnDraw"),
                Enum.Parse(drawSourceType, "Normal"),
                70,
                null,
                false,
                false,
                string.Empty
            });
        }

        private bool ContainsReachCandidate(
            object candidates,
            string sourceName,
            int handIndex,
            object tile)
        {
            int count = flowSupport.Collections.Count(candidates);
            for (int i = 0; i < count; i++)
            {
                object candidate = flowSupport.Collections.Item(candidates, i);
                if (reflection.GetProperty(candidate, "Source").ToString() == sourceName &&
                    (int)reflection.GetProperty(candidate, "HandIndex") == handIndex &&
                    reflection.GetProperty(candidate, "Tile").Equals(tile))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
