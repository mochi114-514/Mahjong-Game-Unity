using System;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Features.Furiten
{
    internal sealed class FuritenUiTestDriver : IDisposable
    {
        private const string DrawResultTypeName =
            "MahjongPrototype.Services.DrawResult, Assembly-CSharp";
        private const string DrawPurposeTypeName =
            "MahjongPrototype.Services.DrawPurpose, Assembly-CSharp";
        private const string DrawSourceTypeName =
            "MahjongPrototype.Services.DrawSource, Assembly-CSharp";
        private const string ActiveSkillEffectTypeName =
            "MahjongPrototype.Skills.ActiveSkillEffect, Assembly-CSharp";
        private const string MahjongPrototypeUiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string MahjongUiDisplayControllerTypeName =
            "MahjongPrototype.UI.MahjongUiDisplayController, Assembly-CSharp";
        private const string MahjongUiInputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string MahjongUiCommandRouterTypeName =
            "MahjongPrototype.UI.MahjongUiCommandRouter, Assembly-CSharp";
        private const string MahjongWinDecisionControllerTypeName =
            "MahjongPrototype.UI.MahjongWinDecisionController, Assembly-CSharp";
        private const string MahjongReachDecisionControllerTypeName =
            "MahjongPrototype.UI.MahjongReachDecisionController, Assembly-CSharp";
        private const string MahjongLogPreviewControllerTypeName =
            "MahjongPrototype.UI.MahjongLogPreviewController, Assembly-CSharp";
        private const string MahjongZeroHanTenpaiControllerTypeName =
            "MahjongPrototype.UI.MahjongZeroHanTenpaiController, Assembly-CSharp";
        private const string MahjongFuritenControllerTypeName =
            "MahjongPrototype.UI.MahjongFuritenController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string ToggleTypeName =
            "UnityEngine.UI.Toggle, UnityEngine.UI";

        private readonly MahjongGameFlowTestSession session;
        private readonly Component uiManager;
        private readonly Component furitenController;
        private readonly GameObject zeroHanTextObject;
        private readonly GameObject furitenTextObject;
        private readonly Component furitenText;
        private bool disposed;

        private FuritenUiTestDriver(
            MahjongGameFlowTestSession session,
            Component uiManager,
            Component furitenController,
            GameObject zeroHanTextObject,
            GameObject furitenTextObject,
            Component furitenText)
        {
            this.session = session;
            this.uiManager = uiManager;
            this.furitenController = furitenController;
            this.zeroHanTextObject = zeroHanTextObject;
            this.furitenTextObject = furitenTextObject;
            this.furitenText = furitenText;
        }

        public bool FuritenTextVisible => furitenTextObject.activeSelf;
        public bool ZeroHanTextVisible => zeroHanTextObject.activeSelf;
        public string FuritenText => (string)session.Reflection.GetProperty(furitenText, "text");

        public static FuritenUiTestDriver Create(int participantCount)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "FuritenUiHarnessRoot",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = participantCount,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = 0f,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East"
                },
                reflection,
                collections,
                types,
                dataFactory);

            try
            {
                object catalog = dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinition("Tanyao", "One", "One"),
                    dataFactory.CreateYakuDefinition("Reach", "One", "None"));
                session.RegisterOwnedScriptableObject(catalog);
                reflection.SetPrivateField(session.GameFlow, "yakuDefinitionCatalog", catalog);

                return CreateUi(session);
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        public void StartRound()
        {
            session.Reflection.Invoke(session.GameFlow, "StartNewRound");
        }

        public void AddHandTiles(string seatName, params string[] tileCodes)
        {
            session.DataFactory.AddHandTiles(
                session.DataFactory.GetPlayerSeat(session.CurrentState, seatName),
                tileCodes);
        }

        public object AddDiscard(string seatName, string tileCode, int turnIndex)
        {
            return session.DataFactory.AddDiscard(
                session.CurrentState,
                seatName,
                tileCode,
                turnIndex);
        }

        public void AddSelfFuritenHand(string[] tileCodes)
        {
            AddHandTiles("East", tileCodes);
            AddDiscard("East", "5m", 0);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            session.DataFactory.SetDrawnTile(session.CurrentState, seatName, tileCode);
        }

        public void RefreshFuritenUi()
        {
            session.Reflection.Invoke(uiManager, "RefreshFuritenUi");
        }

        public void RefreshFromFlow()
        {
            session.Reflection.Invoke(uiManager, "RefreshFromFlow");
        }

        public void RefreshCurrentState()
        {
            session.Reflection.InvokeWithSignature(
                uiManager,
                "Refresh",
                new[] { session.Types.MahjongGameState },
                session.CurrentState);
        }

        public void RefreshNullState()
        {
            session.Reflection.InvokeWithSignature(
                uiManager,
                "Refresh",
                new[] { session.Types.MahjongGameState },
                new object[] { null });
        }

        public void HandleTileDrawn(string seatName, string tileCode, string purposeName)
        {
            session.Reflection.Invoke(
                uiManager,
                "HandleTileDrawn",
                CreateDrawResult(seatName, tileCode, purposeName));
        }

        public void HandleTileDiscarded(object discardRecord)
        {
            session.Reflection.Invoke(uiManager, "HandleTileDiscarded", discardRecord);
        }

        public void HandleRoundStarted(int handNumber, int wallCount)
        {
            session.Reflection.Invoke(uiManager, "HandleRoundStarted", handNumber, wallCount);
        }

        public void HandleRoundEnded(string result)
        {
            session.Reflection.Invoke(uiManager, "HandleRoundEnded", result);
        }

        public void SetFuritenVisible(bool visible)
        {
            session.Reflection.Invoke(furitenController, "SetVisible", visible);
        }

        public string SnapshotState()
        {
            return string.Join(
                "|",
                session.Reflection.GetProperty(session.CurrentState, "CurrentTurn"),
                session.Reflection.GetProperty(session.CurrentState, "TurnIndex"),
                session.DataFactory.HandDisplayString(session.CurrentState, "East"),
                SnapshotDiscards());
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private static FuritenUiTestDriver CreateUi(MahjongGameFlowTestSession session)
        {
            ReflectionTestAccess reflection = session.Reflection;
            Transform rootTransform = ((Component)session.GameFlow).transform;
            GameObject uiObject = new GameObject("MahjongUiManager");
            uiObject.SetActive(false);
            uiObject.transform.SetParent(rootTransform);

            GameObject tenpai = new GameObject("Tenpai");
            tenpai.transform.SetParent(uiObject.transform);
            GameObject zeroHanObject = new GameObject("RenamedZeroHan");
            zeroHanObject.transform.SetParent(tenpai.transform);
            GameObject furitenObject = new GameObject("RenamedFuriten");
            furitenObject.transform.SetParent(tenpai.transform);

            Component zeroHanText = zeroHanObject.AddComponent(
                reflection.RequireType(TextMeshProUguiTypeName));
            Component furitenText = furitenObject.AddComponent(
                reflection.RequireType(TextMeshProUguiTypeName));
            Component zeroHanController = zeroHanObject.AddComponent(
                reflection.RequireType(MahjongZeroHanTenpaiControllerTypeName));
            Component furitenController = furitenObject.AddComponent(
                reflection.RequireType(MahjongFuritenControllerTypeName));
            reflection.SetPrivateField(zeroHanController, "zeroHanTenpaiText", zeroHanText);
            reflection.SetPrivateField(furitenController, "furitenText", furitenText);
            zeroHanObject.SetActive(false);
            furitenObject.SetActive(false);

            Component uiManager = uiObject.AddComponent(
                reflection.RequireType(MahjongPrototypeUiManagerTypeName));
            reflection.SetPrivateField(uiManager, "gameFlow", session.GameFlow);
            reflection.SetPrivateField(uiManager, "eventNotifier", session.EventNotifier);
            AssignUiManagerSupportControllers(reflection, session.GameFlow, uiObject.transform, uiManager);
            reflection.SetPrivateField(uiManager, "zeroHanTenpaiController", zeroHanController);
            reflection.SetPrivateField(uiManager, "furitenController", furitenController);

            return new FuritenUiTestDriver(
                session,
                uiManager,
                furitenController,
                zeroHanObject,
                furitenObject,
                furitenText);
        }

        private static void AssignUiManagerSupportControllers(
            ReflectionTestAccess reflection,
            object gameFlow,
            Transform uiRoot,
            Component uiManager)
        {
            GameObject supportRoot = new GameObject("UiManagerSupport");
            supportRoot.SetActive(false);
            supportRoot.transform.SetParent(uiRoot);

            Component displayController = CreateDisplayController(reflection, supportRoot.transform);
            Component inputController = CreateInputController(reflection, supportRoot.transform);
            Component commandRouter = CreateCommandRouter(
                reflection,
                supportRoot.transform,
                gameFlow,
                inputController);
            Component winDecisionController = CreateWinDecisionController(reflection, supportRoot.transform);
            Component reachDecisionController = CreateReachDecisionController(reflection, supportRoot.transform);
            Component logPreviewController = CreateLogPreviewController(reflection, supportRoot.transform);

            reflection.SetPrivateField(uiManager, "displayController", displayController);
            reflection.SetPrivateField(uiManager, "inputController", inputController);
            reflection.SetPrivateField(uiManager, "commandRouter", commandRouter);
            reflection.SetPrivateField(uiManager, "winDecisionController", winDecisionController);
            reflection.SetPrivateField(uiManager, "reachDecisionController", reachDecisionController);
            reflection.SetPrivateField(uiManager, "logPreviewController", logPreviewController);
        }

        private static Component CreateDisplayController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject displayObject = CreateChild(parent, "UiDisplaySupport");
            Component controller = displayObject.AddComponent(
                reflection.RequireType(MahjongUiDisplayControllerTypeName));

            reflection.SetPrivateField(
                controller,
                "currentTurnText",
                CreateText(reflection, displayObject.transform, "CurrentTurnText"));
            reflection.SetPrivateField(
                controller,
                "turnIndexText",
                CreateText(reflection, displayObject.transform, "TurnIndexText"));
            reflection.SetPrivateField(
                controller,
                "wallCountText",
                CreateText(reflection, displayObject.transform, "WallCountText"));
            reflection.SetPrivateField(
                controller,
                "activeSkillText",
                CreateText(reflection, displayObject.transform, "ActiveSkillText"));

            return controller;
        }

        private static Component CreateInputController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject inputObject = CreateChild(parent, "UiInputSupport");
            Component autoSortToggle = CreateComponent(
                reflection,
                inputObject.transform,
                "AutoSortToggle",
                ToggleTypeName);
            Component controller = inputObject.AddComponent(
                reflection.RequireType(MahjongUiInputControllerTypeName));
            reflection.SetPrivateField(controller, "autoSortToggle", autoSortToggle);
            return controller;
        }

        private static Component CreateCommandRouter(
            ReflectionTestAccess reflection,
            Transform parent,
            object gameFlow,
            Component inputController)
        {
            GameObject commandObject = CreateChild(parent, "UiCommandRouterSupport");
            Component controller = commandObject.AddComponent(
                reflection.RequireType(MahjongUiCommandRouterTypeName));
            reflection.SetPrivateField(controller, "gameFlow", gameFlow);
            reflection.SetPrivateField(controller, "inputController", inputController);
            return controller;
        }

        private static Component CreateWinDecisionController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject controllerObject = CreateChild(parent, "WinDecisionSupport");
            GameObject root = CreateChild(controllerObject.transform, "WinDecisionRoot");
            Component label = CreateText(reflection, root.transform, "WinButtonLabel");
            Component controller = controllerObject.AddComponent(
                reflection.RequireType(MahjongWinDecisionControllerTypeName));
            reflection.SetPrivateField(controller, "winDecisionRoot", root);
            reflection.SetPrivateField(controller, "winButtonLabel", label);
            return controller;
        }

        private static Component CreateReachDecisionController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject controllerObject = CreateChild(parent, "ReachDecisionSupport");
            GameObject decisionRoot = CreateChild(controllerObject.transform, "ReachDecisionRoot");
            GameObject cancelRoot = CreateChild(controllerObject.transform, "ReachCancelRoot");
            Component controller = controllerObject.AddComponent(
                reflection.RequireType(MahjongReachDecisionControllerTypeName));
            reflection.SetPrivateField(controller, "reachDecisionRoot", decisionRoot);
            reflection.SetPrivateField(controller, "reachCancelRoot", cancelRoot);
            return controller;
        }

        private static Component CreateLogPreviewController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject controllerObject = CreateChild(parent, "LogPreviewSupport");
            Component text = CreateText(reflection, controllerObject.transform, "RecentLogText");
            Component controller = controllerObject.AddComponent(
                reflection.RequireType(MahjongLogPreviewControllerTypeName));
            reflection.SetPrivateField(controller, "recentLogText", text);
            return controller;
        }

        private static Component CreateText(
            ReflectionTestAccess reflection,
            Transform parent,
            string name)
        {
            return CreateComponent(reflection, parent, name, TextMeshProUguiTypeName);
        }

        private static Component CreateComponent(
            ReflectionTestAccess reflection,
            Transform parent,
            string name,
            string typeName)
        {
            return CreateChild(parent, name).AddComponent(reflection.RequireType(typeName));
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        private object CreateDrawResult(string seatName, string tileCode, string purposeName)
        {
            Type drawResultType = session.Reflection.RequireType(DrawResultTypeName);
            Type drawPurposeType = session.Reflection.RequireType(DrawPurposeTypeName);
            Type drawSourceType = session.Reflection.RequireType(DrawSourceTypeName);
            ConstructorInfo constructor = drawResultType.GetConstructor(new[]
            {
                typeof(bool),
                session.Types.SeatId,
                session.Types.Tile,
                drawPurposeType,
                drawSourceType,
                typeof(int),
                session.Reflection.RequireType(ActiveSkillEffectTypeName),
                typeof(bool),
                typeof(bool),
                typeof(string)
            });

            if (constructor == null)
                throw new MissingMethodException(drawResultType.FullName, ".ctor");

            return constructor.Invoke(new[]
            {
                true,
                session.DataFactory.ParseSeat(seatName),
                session.DataFactory.CreateTile(tileCode),
                Enum.Parse(drawPurposeType, purposeName),
                Enum.Parse(drawSourceType, "Normal"),
                70,
                null,
                false,
                false,
                string.Empty
            });
        }

        private string SnapshotDiscards()
        {
            object discards = session.Reflection.GetProperty(session.CurrentState, "Discards");
            int count = session.Collections.Count(discards);
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
                snapshot += "|" + session.Collections.Item(discards, i);

            return snapshot;
        }
    }
}
