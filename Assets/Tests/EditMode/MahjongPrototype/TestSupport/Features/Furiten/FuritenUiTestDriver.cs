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
        private const string MahjongZeroHanTenpaiControllerTypeName =
            "MahjongPrototype.UI.MahjongZeroHanTenpaiController, Assembly-CSharp";
        private const string MahjongFuritenControllerTypeName =
            "MahjongPrototype.UI.MahjongFuritenController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private readonly MahjongGameFlowTestSession session;
        private readonly GameObject uiObject;
        private readonly Component uiManager;
        private readonly Component furitenController;
        private readonly GameObject zeroHanTextObject;
        private readonly GameObject furitenTextObject;
        private readonly Component furitenText;
        private bool disposed;

        private FuritenUiTestDriver(
            MahjongGameFlowTestSession session,
            GameObject uiObject,
            Component uiManager,
            Component furitenController,
            GameObject zeroHanTextObject,
            GameObject furitenTextObject,
            Component furitenText)
        {
            this.session = session;
            this.uiObject = uiObject;
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

        public void SetUiActive(bool active)
        {
            uiObject.SetActive(active);
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
            reflection.SetPrivateField(uiManager, "zeroHanTenpaiController", zeroHanController);
            reflection.SetPrivateField(uiManager, "furitenController", furitenController);

            return new FuritenUiTestDriver(
                session,
                uiObject,
                uiManager,
                furitenController,
                zeroHanObject,
                furitenObject,
                furitenText);
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
