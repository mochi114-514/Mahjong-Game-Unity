using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Features.UiInput;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using MahjongPrototype.Tests.TestSupport.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongUiInputControllerTests
    {
        [Test]
        public void AssignedControls_InvokeEventsEvenWhenObjectNamesDiffer()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerHost"))
            {
                driver.SubscribeAllRequestEvents();

                driver.TargetTileText = "5m";
                driver.EnableController();
                driver.ClickDraw();
                driver.ClickForceDrawSkill();
                driver.ToggleAutoSort(true);
                driver.ClickRetry();
                driver.ClickWin();
                driver.ClickDeclineWin();
                driver.ClickReach();
                driver.ClickDeclineReach();
                driver.ClickCancelReach();
                driver.ClickRoundResultConfirm();

                Assert.That(driver.DrawCount, Is.EqualTo(1));
                Assert.That(driver.SkillTarget, Is.EqualTo("5m"));
                Assert.That(driver.AutoSortValue, Is.True);
                Assert.That(driver.RetryCount, Is.EqualTo(1));
                Assert.That(driver.WinCount, Is.EqualTo(1));
                Assert.That(driver.DeclineWinCount, Is.EqualTo(1));
                Assert.That(driver.ReachCount, Is.EqualTo(1));
                Assert.That(driver.DeclineReachCount, Is.EqualTo(1));
                Assert.That(driver.CancelReachCount, Is.EqualTo(1));
                Assert.That(driver.RoundResultConfirmCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void MissingDrawButton_WarnsAndDoesNotAutoFindChildNamedDrawButton()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoDrawTest"))
            {
                driver.CreateUnassignedDrawButtonChild();
                driver.ClearDrawButton();
                driver.SubscribeDrawRequested();

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: DrawButton is not assigned.");

                driver.EnableController();
                driver.ClickUnassignedDrawButton();

                Assert.That(driver.DrawCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void MissingReachButton_Warns()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoReachTest"))
            {
                driver.ClearReachButton();

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: ReachButton is not assigned.");

                driver.EnableController();
            }
        }

        [Test]
        public void MissingAutoSortToggle_Warns()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoAutoSortTest"))
            {
                driver.ClearAutoSortToggle();

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: AutoSortToggle is not assigned.");

                driver.EnableController();
            }
        }

        [Test]
        public void SetGameplayInputInteractable_ControlsOnlyGameplayInputs()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerInteractableTest"))
            {
                driver.RetryInteractable = true;
                driver.CancelReachInteractable = true;
                driver.RoundResultConfirmInteractable = true;

                driver.SetGameplayInputInteractable(false);

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.False);
                Assert.That(driver.TargetTileInputInteractable, Is.False);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.RetryInteractable, Is.True);
                Assert.That(driver.CancelReachInteractable, Is.True);
                Assert.That(driver.RoundResultConfirmInteractable, Is.True);
            }
        }

        [Test]
        public void SetAutoSortInteractable_ControlsOnlyAutoSortToggle()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerAutoSortInteractableTest"))
            {
                driver.SetAutoSortInteractable(false);

                Assert.That(driver.AutoSortInteractable, Is.False);
                Assert.That(driver.DrawInteractable, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
            }
        }

        [Test]
        public void RoundResultConfirmButton_EnableDisable_DoesNotRegisterMultipleHandlers()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerRoundResultConfirmTest"))
            {
                driver.SubscribeRoundResultConfirmRequested();

                driver.EnableController();
                driver.DisableController();
                driver.EnableController();
                driver.ClickRoundResultConfirm();

                Assert.That(driver.RoundResultConfirmCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void MissingRoundResultConfirmButton_Warns()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoRoundResultConfirmTest"))
            {
                driver.ClearRoundResultConfirmButton();

                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongUiInputController: RoundResultConfirmButton is not assigned.");

                driver.EnableController();
            }
        }

        [Test]
        public void SetAutoSortWithoutNotify_UpdatesToggleWithoutEvent()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerAutoSortTest"))
            {
                driver.SubscribeAutoSortChangedCount();

                driver.SetAutoSortWithoutNotify(true);

                Assert.That(driver.AutoSortIsOn, Is.True);
                Assert.That(driver.AutoSortEventCount, Is.EqualTo(0));
            }
        }
    }

    public sealed class MahjongPrototypeUiManagerInteractionTests
    {
        [Test]
        public void RefreshInteractionState_OtherTurn_KeepsControlAreaInteractableAndSelfTilesDisabled()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalOtherTurn();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.False);
            }
        }

        [Test]
        public void RefreshInteractionState_SelfTurn_KeepsControlAreaAndSelfTilesInteractable()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
                Assert.That(driver.SelfDrawnTileInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_ReachDecision_KeepsControlAreaInteractableAndSelfTilesDisabled()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.BeginReachDecision();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.False);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.False);
            }
        }

        [Test]
        public void RefreshInteractionState_ReachDiscardSelection_LocksControlAreaAndEnablesOnlyCandidates()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.BeginReachDiscardSelection();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.False);
                Assert.That(driver.TargetTileInputInteractable, Is.False);
                Assert.That(driver.AutoSortInteractable, Is.False);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
                Assert.That(driver.SecondSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.False);
            }
        }

        [Test]
        public void RefreshInteractionState_LockedStatesKeepControlAreaNotInteractable()
        {
            AssertControlAreaLocked(driver => driver.BeginWinDecision());
            AssertControlAreaLocked(driver => driver.BeginReachDiscardSelection());
            AssertControlAreaLocked(driver => driver.MarkRoundEnded());
            AssertControlAreaLocked(driver => driver.DeclareReachWaitingForDraw());
        }

        [Test]
        public void OtherTurn_DrawButtonCommand_DoesNotChangeGameState()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalOtherTurn();
                driver.EnableCommandRouting();
                driver.RefreshInteraction();
                Snapshot before = driver.CaptureSnapshot();

                driver.ClickDraw();

                Snapshot after = driver.CaptureSnapshot();
                Assert.That(driver.DrawInteractable, Is.True);
                Assert.That(after.CurrentTurnName, Is.EqualTo(before.CurrentTurnName));
                Assert.That(after.TurnIndex, Is.EqualTo(before.TurnIndex));
                Assert.That(after.SelfHandCount, Is.EqualTo(before.SelfHandCount));
                Assert.That(after.SelfDrawnTileCode, Is.EqualTo(before.SelfDrawnTileCode));
                Assert.That(after.WallCount, Is.EqualTo(before.WallCount));
                Assert.That(after.DiscardCount, Is.EqualTo(before.DiscardCount));
            }
        }

        private static void AssertControlAreaLocked(Action<Driver> configure)
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                configure(driver);

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.False);
                Assert.That(driver.TargetTileInputInteractable, Is.False);
            }
        }

        private readonly struct Snapshot
        {
            public Snapshot(
                string currentTurnName,
                int turnIndex,
                int selfHandCount,
                string selfDrawnTileCode,
                int wallCount,
                int discardCount)
            {
                CurrentTurnName = currentTurnName;
                TurnIndex = turnIndex;
                SelfHandCount = selfHandCount;
                SelfDrawnTileCode = selfDrawnTileCode;
                WallCount = wallCount;
                DiscardCount = discardCount;
            }

            public string CurrentTurnName { get; }
            public int TurnIndex { get; }
            public int SelfHandCount { get; }
            public string SelfDrawnTileCode { get; }
            public int WallCount { get; }
            public int DiscardCount { get; }
        }

        private sealed class Driver : IDisposable
        {
            private const string UiManagerTypeName =
                "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
            private const string InputControllerTypeName =
                "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
            private const string CommandRouterTypeName =
                "MahjongPrototype.UI.MahjongUiCommandRouter, Assembly-CSharp";
            private const string PlayerAreaPresenterTypeName =
                "MahjongPrototype.UI3D.Mahjong3DPlayerAreaPresenter, Assembly-CSharp";
            private const string PlayerUiControllerTypeName =
                "MahjongPrototype.UI3D.Mahjong3DPlayerUiController, Assembly-CSharp";
            private const string HandViewTypeName =
                "MahjongPrototype.UI3D.Mahjong3DHandView, Assembly-CSharp";
            private const string DrawnTileViewTypeName =
                "MahjongPrototype.UI3D.Mahjong3DDrawnTileView, Assembly-CSharp";
            private const string DiscardRiverViewTypeName =
                "MahjongPrototype.UI3D.Mahjong3DDiscardRiverView, Assembly-CSharp";
            private const string TileViewTypeName =
                "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";
            private const string TileFaceCatalogTypeName =
                "MahjongPrototype.UI3D.Mahjong3DTileFaceCatalog, Assembly-CSharp";
            private const string ReachDiscardCandidateTypeName =
                "MahjongPrototype.Services.ReachDiscardCandidate, Assembly-CSharp";
            private const string DiscardSourceTypeName =
                "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";
            private const string TmpInputFieldTypeName =
                "TMPro.TMP_InputField, Unity.TextMeshPro";

            private readonly UnityObjectTestOwner owner;
            private readonly ReflectionTestAccess reflection;
            private readonly CollectionTestAccess collections;
            private readonly MahjongGameFlowTestSession session;
            private readonly Component uiManager;
            private readonly Component inputController;
            private readonly Component commandRouter;
            private readonly Component handView;
            private readonly Component drawnTileView;
            private readonly Button drawButton;
            private readonly Button forceDrawSkillButton;
            private readonly Toggle autoSortToggle;
            private readonly Component targetTileInput;
            private bool commandRoutingEnabled;
            private bool disposed;

            private Driver(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                UnityObjectTestOwner owner,
                MahjongGameFlowTestSession session,
                Component uiManager,
                Component inputController,
                Component commandRouter,
                Component handView,
                Component drawnTileView,
                Button drawButton,
                Button forceDrawSkillButton,
                Toggle autoSortToggle,
                Component targetTileInput)
            {
                this.reflection = reflection;
                this.collections = collections;
                this.owner = owner;
                this.session = session;
                this.uiManager = uiManager;
                this.inputController = inputController;
                this.commandRouter = commandRouter;
                this.handView = handView;
                this.drawnTileView = drawnTileView;
                this.drawButton = drawButton;
                this.forceDrawSkillButton = forceDrawSkillButton;
                this.autoSortToggle = autoSortToggle;
                this.targetTileInput = targetTileInput;
            }

            public bool DrawInteractable => drawButton.interactable;
            public bool ForceDrawSkillInteractable => forceDrawSkillButton.interactable;
            public bool AutoSortInteractable => autoSortToggle.interactable;
            public bool TargetTileInputInteractable =>
                (bool)reflection.GetProperty(targetTileInput, "interactable");
            public bool FirstSelfHandTileInteractable => TileInteractable(FirstSelfHandTile);
            public bool SecondSelfHandTileInteractable => TileInteractable(SelfHandTileAt(1));
            public bool SelfDrawnTileInteractable => TileInteractable(SelfDrawnTile);

            public static Driver Create()
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
                object catalog = MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "MahjongPrototypeUiManagerInteractionTest",
                    AddEventNotifier = false,
                    LogWarnings = false,
                    ParticipantCount = 2,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East",
                    YakuDefinitionCatalog = catalog
                };
                MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                    options,
                    reflection,
                    collections,
                    types,
                    dataFactory);
                session.RegisterOwnedScriptableObject(catalog);

                try
                {
                    Driver driver = CreateUi(reflection, collections, session);
                    return driver;
                }
                catch
                {
                    session.Dispose();
                    throw;
                }
            }

            public void PrepareNormalSelfTurn()
            {
                PrepareRoundWithSelfTiles();
                session.DataFactory.SetCurrentTurn(State, "East");
            }

            public void PrepareNormalOtherTurn()
            {
                PrepareRoundWithSelfTiles();
                session.DataFactory.SetCurrentTurn(State, "West");
            }

            public void BeginWinDecision()
            {
                reflection.Invoke(
                    State,
                    "BeginWinDecision",
                    session.DataFactory.ParseSeat("East"),
                    session.Query.TurnIndex);
            }

            public void BeginReachDecision()
            {
                Type candidateType = reflection.RequireType(ReachDiscardCandidateTypeName);
                Type listType = typeof(List<>).MakeGenericType(candidateType);
                System.Collections.IList candidates =
                    (System.Collections.IList)Activator.CreateInstance(listType);
                candidates.Add(reflection.CreateInstance(
                    candidateType,
                    Enum.Parse(reflection.RequireType(DiscardSourceTypeName), "Hand"),
                    0,
                    session.DataFactory.CreateTile("1m")));

                reflection.Invoke(
                    State,
                    "BeginReachDecision",
                    session.DataFactory.ParseSeat("East"),
                    candidates,
                    session.Query.TurnIndex);
            }

            public void BeginReachDiscardSelection()
            {
                BeginReachDecision();
                reflection.Invoke(
                    State,
                    "BeginReachDiscardSelection",
                    session.DataFactory.ParseSeat("East"));
            }

            public void MarkRoundEnded()
            {
                reflection.SetProperty(State, "IsRoundEnded", true);
            }

            public void DeclareReachWaitingForDraw()
            {
                object selfSeat = session.Query.GetPlayerSeat("East");
                reflection.Invoke(selfSeat, "ClearDrawnTile");
                reflection.Invoke(selfSeat, "DeclareReach", session.Query.TurnIndex);
                session.DataFactory.SetCurrentTurn(State, "East");
            }

            public void RefreshInteraction()
            {
                reflection.Invoke(uiManager, "RefreshPlayerArea3D", State);
                reflection.Invoke(uiManager, "RefreshInteractionState", State);
            }

            public void EnableCommandRouting()
            {
                if (commandRoutingEnabled)
                    return;

                reflection.Invoke(inputController, "OnEnable");
                reflection.Invoke(commandRouter, "OnEnable");
                commandRoutingEnabled = true;
            }

            public void ClickDraw()
            {
                drawButton.onClick.Invoke();
            }

            public Snapshot CaptureSnapshot()
            {
                return new Snapshot(
                    session.Query.CurrentTurnName,
                    session.Query.TurnIndex,
                    session.Query.HandCountForPlayerId("Player1"),
                    session.Query.DrawnTileCodeOrNullForPlayerId("Player1"),
                    session.Query.WallCount,
                    session.Query.DiscardCount);
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                if (commandRoutingEnabled)
                {
                    reflection.Invoke(commandRouter, "OnDisable");
                    reflection.Invoke(inputController, "OnDisable");
                }

                session.Dispose();
                owner.Dispose();
            }

            private object State => session.CurrentState;

            private object FirstSelfHandTile
            {
                get { return SelfHandTileAt(0); }
            }

            private object SelfDrawnTile
            {
                get
                {
                    object activeTile = reflection.GetPrivateField(drawnTileView, "activeTile");
                    Assert.That(activeTile, Is.Not.Null);
                    return activeTile;
                }
            }

            private object SelfHandTileAt(int index)
            {
                object activeTiles = reflection.GetPrivateField(handView, "activeTiles");
                Assert.That(collections.Count(activeTiles), Is.GreaterThan(index));
                return collections.Item(activeTiles, index);
            }

            private static Driver CreateUi(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongGameFlowTestSession session)
            {
                UnityObjectTestOwner owner = new UnityObjectTestOwner();
                try
                {
                    GameObject uiRoot = owner.Own(new GameObject("InteractionUiRoot"));
                    uiRoot.SetActive(false);

                    Component uiManager =
                        uiRoot.AddComponent(reflection.RequireType(UiManagerTypeName));

                    Component inputController =
                        CreateInputController(
                            reflection,
                            uiRoot.transform,
                            out Button drawButton,
                            out Button skillButton,
                            out Toggle autoSortToggle,
                            out Component targetInput);

                    Component playerAreaPresenter =
                        CreatePlayerAreaPresenter(
                            reflection,
                            uiRoot.transform,
                            owner,
                            out Component handView,
                            out Component drawnTileView);

                    Component commandRouter =
                        CreateCommandRouter(
                            reflection,
                            uiRoot.transform,
                            session.GameFlow,
                            inputController,
                            playerAreaPresenter);

                    reflection.SetPrivateField(uiManager, "gameFlow", session.GameFlow);
                    reflection.SetPrivateField(uiManager, "inputController", inputController);
                    reflection.SetPrivateField(uiManager, "commandRouter", commandRouter);
                    reflection.SetPrivateField(uiManager, "playerArea3DPresenter", playerAreaPresenter);

                    Driver driver = new Driver(
                        reflection,
                        collections,
                        owner,
                        session,
                        uiManager,
                        inputController,
                        commandRouter,
                        handView,
                        drawnTileView,
                        drawButton,
                        skillButton,
                        autoSortToggle,
                        targetInput);
                    return driver;
                }
                catch
                {
                    owner.Dispose();
                    throw;
                }
            }

            private static Component CreateInputController(
                ReflectionTestAccess reflection,
                Transform parent,
                out Button drawButton,
                out Button forceDrawSkillButton,
                out Toggle autoSortToggle,
                out Component targetTileInput)
            {
                GameObject inputObject = CreateChild(parent, "InputController");
                Component controller =
                    inputObject.AddComponent(reflection.RequireType(InputControllerTypeName));

                drawButton = CreateButton(inputObject.transform, "Draw");
                forceDrawSkillButton = CreateButton(inputObject.transform, "ForceDrawSkill");
                autoSortToggle = CreateToggle(inputObject.transform, "AutoSort");
                targetTileInput = CreateInput(inputObject.transform, "TargetTile");

                reflection.SetPrivateField(controller, "drawButton", drawButton);
                reflection.SetPrivateField(controller, "forceDrawSkillButton", forceDrawSkillButton);
                reflection.SetPrivateField(controller, "targetTileInput", targetTileInput);
                reflection.SetPrivateField(controller, "autoSortToggle", autoSortToggle);
                reflection.SetPrivateField(controller, "retryButton", CreateButton(inputObject.transform, "Retry"));
                reflection.SetPrivateField(controller, "winButton", CreateButton(inputObject.transform, "Win"));
                reflection.SetPrivateField(controller, "declineWinButton", CreateButton(inputObject.transform, "DeclineWin"));
                reflection.SetPrivateField(controller, "reachButton", CreateButton(inputObject.transform, "Reach"));
                reflection.SetPrivateField(controller, "declineReachButton", CreateButton(inputObject.transform, "DeclineReach"));
                reflection.SetPrivateField(controller, "cancelReachButton", CreateButton(inputObject.transform, "CancelReach"));
                return controller;
            }

            private static Component CreateCommandRouter(
                ReflectionTestAccess reflection,
                Transform parent,
                object gameFlow,
                Component inputController,
                Component playerAreaPresenter)
            {
                Component commandRouter =
                    CreateChild(parent, "CommandRouter")
                    .AddComponent(reflection.RequireType(CommandRouterTypeName));
                reflection.SetPrivateField(commandRouter, "gameFlow", gameFlow);
                reflection.SetPrivateField(commandRouter, "inputController", inputController);
                reflection.SetPrivateField(commandRouter, "playerArea3DPresenter", playerAreaPresenter);
                return commandRouter;
            }

            private static Component CreatePlayerAreaPresenter(
                ReflectionTestAccess reflection,
                Transform parent,
                UnityObjectTestOwner owner,
                out Component handView,
                out Component drawnTileView)
            {
                GameObject presenterObject = CreateChild(parent, "PlayerAreaPresenter");
                Component presenter =
                    presenterObject.AddComponent(reflection.RequireType(PlayerAreaPresenterTypeName));
                Component selfController =
                    CreateChild(presenterObject.transform, "SelfPlayerController")
                    .AddComponent(reflection.RequireType(PlayerUiControllerTypeName));
                Component tilePrefab = CreateTilePrefab(reflection, owner);

                handView = CreateChild(selfController.transform, "HandView")
                    .AddComponent(reflection.RequireType(HandViewTypeName));
                drawnTileView = CreateChild(selfController.transform, "DrawnTileView")
                    .AddComponent(reflection.RequireType(DrawnTileViewTypeName));
                Component discardRiverView =
                    CreateChild(selfController.transform, "DiscardRiverView")
                    .AddComponent(reflection.RequireType(DiscardRiverViewTypeName));

                reflection.SetPrivateField(handView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(drawnTileView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(discardRiverView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(selfController, "handView", handView);
                reflection.SetPrivateField(selfController, "drawnTileView", drawnTileView);
                reflection.SetPrivateField(selfController, "discardRiverView", discardRiverView);
                reflection.SetPrivateField(presenter, "selfBottomPlayerUiController", selfController);
                return presenter;
            }

            private static Component CreateTilePrefab(
                ReflectionTestAccess reflection,
                UnityObjectTestOwner owner)
            {
                GameObject prefabObject = owner.Own(new GameObject("TilePrefab"));
                Component tileView =
                    prefabObject.AddComponent(reflection.RequireType(TileViewTypeName));
                MeshFilter meshFilter = CreateChild(prefabObject.transform, "FrontFace").AddComponent<MeshFilter>();
                ScriptableObject catalog =
                    ScriptableObject.CreateInstance(reflection.RequireType(TileFaceCatalogTypeName));
                owner.Register(catalog);
                reflection.SetPrivateField(tileView, "frontFaceMeshFilter", meshFilter);
                reflection.SetPrivateField(tileView, "tileFaceCatalog", catalog);
                return tileView;
            }

            private void PrepareRoundWithSelfTiles()
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTilesFromText(
                    session.Query.GetPlayerSeat("East"),
                    "1m 2m");
                session.DataFactory.SetDrawnTile(State, "East", "3m");
            }

            private bool TileInteractable(object tile)
            {
                return (bool)reflection.GetProperty(tile, "Interactable");
            }

            private static Button CreateButton(Transform parent, string name)
            {
                return CreateChild(parent, name).AddComponent<Button>();
            }

            private static Toggle CreateToggle(Transform parent, string name)
            {
                return CreateChild(parent, name).AddComponent<Toggle>();
            }

            private static Component CreateInput(Transform parent, string name)
            {
                return CreateChild(parent, name)
                    .AddComponent(Type.GetType(TmpInputFieldTypeName, true));
            }

            private static GameObject CreateChild(Transform parent, string name)
            {
                GameObject gameObject = new GameObject(name);
                gameObject.transform.SetParent(parent);
                return gameObject;
            }
        }
    }
}
