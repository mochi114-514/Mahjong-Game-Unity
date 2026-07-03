using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenUiTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string DiscardRecordTypeName =
            "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string DrawResultTypeName =
            "MahjongPrototype.Services.DrawResult, Assembly-CSharp";
        private const string DrawPurposeTypeName =
            "MahjongPrototype.Services.DrawPurpose, Assembly-CSharp";
        private const string DrawSourceTypeName =
            "MahjongPrototype.Services.DrawSource, Assembly-CSharp";
        private const string HanValueTypeName = "MahjongPrototype.Domain.HanValue, Assembly-CSharp";
        private const string YakuKindTypeName = "MahjongPrototype.Domain.YakuKind, Assembly-CSharp";
        private const string YakuDefinitionTypeName =
            "MahjongPrototype.Definitions.YakuDefinition, Assembly-CSharp";
        private const string YakuDefinitionCatalogTypeName =
            "MahjongPrototype.Definitions.YakuDefinitionCatalog, Assembly-CSharp";
        private const string MahjongGameStateTypeName =
            "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string MahjongGameFlowTypeName =
            "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string MahjongEventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";
        private const string MahjongPrototypeUiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string MahjongZeroHanTenpaiControllerTypeName =
            "MahjongPrototype.UI.MahjongZeroHanTenpaiController, Assembly-CSharp";
        private const string MahjongFuritenControllerTypeName =
            "MahjongPrototype.UI.MahjongFuritenController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private static readonly string[] SimpleFiveManWait =
        {
            "2m", "3m", "4m",
            "2p", "3p", "4p",
            "2s", "3s", "4s",
            "6s", "7s", "8s",
            "5m"
        };

        private static readonly string[] NoYakuSingleWait =
        {
            "1m", "2m", "3m",
            "4m", "5m", "6m",
            "7p", "8p", "9p",
            "1s", "2s", "3s",
            "P"
        };

        [Test]
        public void RefreshFuritenUi_SelfNotFuriten_HidesText()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddHandTiles(GetPlayerSeat(state, "East"), SimpleFiveManWait);

                Invoke(harness.UiManager, "RefreshFuritenUi");

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void RefreshFuritenUi_SelfDiscardFuriten_ShowsText()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddSelfFuritenHand(state);

                Invoke(harness.UiManager, "RefreshFuritenUi");

                Assert.That(harness.FuritenTextObject.activeSelf, Is.True);
                Assert.That(GetProperty(harness.FuritenText, "text"), Is.EqualTo("フリテン"));
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void RefreshFuritenUi_CpuSeatOnlyFuriten_DoesNotShowSelfUi()
        {
            UiHarness harness = CreateHarness(2);
            try
            {
                object state = StartRound(harness);
                AddHandTiles(GetPlayerSeat(state, "West"), SimpleFiveManWait);
                AddDiscard(state, "West", "5m", 0);

                Invoke(harness.UiManager, "RefreshFuritenUi");

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void RefreshFuritenUi_OtherSeatDiscardOnly_DoesNotShow()
        {
            UiHarness harness = CreateHarness(2);
            try
            {
                object state = StartRound(harness);
                AddHandTiles(GetPlayerSeat(state, "East"), SimpleFiveManWait);
                AddDiscard(state, "West", "5m", 0);

                Invoke(harness.UiManager, "RefreshFuritenUi");

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void HandleTileDrawn_SelfDraw_ClearsFuritenText()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddSelfFuritenHand(state);
                Invoke(harness.UiManager, "RefreshFuritenUi");
                Invoke(GetPlayerSeat(state, "East"), "SetDrawnTile", CreateTile("9m"));

                Invoke(
                    harness.UiManager,
                    "HandleTileDrawn",
                    CreateDrawResult("East", "9m", "TurnDraw"));

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void HandleTileDiscarded_SelfDiscard_ReevaluatesAndShowsFuritenText()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddHandTiles(GetPlayerSeat(state, "East"), SimpleFiveManWait);
                object record = AddDiscard(state, "East", "5m", 1);

                Invoke(harness.UiManager, "HandleTileDiscarded", record);

                Assert.That(harness.FuritenTextObject.activeSelf, Is.True);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void RoundStartedAndRoundEnded_ClearFuritenText()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddSelfFuritenHand(state);
                Invoke(harness.UiManager, "RefreshFuritenUi");

                Invoke(harness.UiManager, "HandleRoundStarted", 1, 70);

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);

                Invoke(harness.UiManager, "RefreshFuritenUi");
                Invoke(harness.UiManager, "HandleRoundEnded", "Win");

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void Refresh_NullStateAndNotEvaluatedState_HideFuritenText()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                Invoke(harness.FuritenController, "SetVisible", true);

                InvokeWithSignature(
                    harness.UiManager,
                    "Refresh",
                    new[] { Type.GetType(MahjongGameStateTypeName, true) },
                    new object[] { null });

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);

                object state = StartRound(harness);
                AddHandTiles(
                    GetPlayerSeat(state, "East"),
                    "2m", "3m", "4m",
                    "2p", "3p", "4p",
                    "2s", "3s", "4s",
                    "6s", "7s", "8s");
                AddDiscard(state, "East", "5m", 0);

                Invoke(harness.UiManager, "RefreshFuritenUi");

                Assert.That(harness.FuritenTextObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator OnEnable_SynchronizesCurrentFuritenState()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddSelfFuritenHand(state);
                harness.UiObject.SetActive(false);

                harness.UiObject.SetActive(true);
                yield return null;

                Assert.That(harness.FuritenTextObject.activeSelf, Is.True);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void Refresh_CanShowZeroHanTenpaiAndFuritenTogether()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddHandTiles(GetPlayerSeat(state, "East"), NoYakuSingleWait);
                AddDiscard(state, "East", "P", 0);

                InvokeWithSignature(
                    harness.UiManager,
                    "Refresh",
                    new[] { Type.GetType(MahjongGameStateTypeName, true) },
                    state);

                Assert.That(harness.ZeroHanTextObject.activeSelf, Is.True);
                Assert.That(harness.FuritenTextObject.activeSelf, Is.True);
            }
            finally
            {
                harness.Destroy();
            }
        }

        [Test]
        public void RefreshFuritenUi_DoesNotChangeGameStateHandOrDiscards()
        {
            UiHarness harness = CreateHarness(1);
            try
            {
                object state = StartRound(harness);
                AddSelfFuritenHand(state);
                string before = SnapshotState(state);

                Invoke(harness.UiManager, "RefreshFuritenUi");

                Assert.That(SnapshotState(state), Is.EqualTo(before));
            }
            finally
            {
                harness.Destroy();
            }
        }

        private static UiHarness CreateHarness(int participantCount)
        {
            GameObject root = new GameObject("FuritenUiHarnessRoot");
            GameObject uiObject = new GameObject("MahjongUiManager");
            uiObject.SetActive(false);
            uiObject.transform.SetParent(root.transform);

            object eventNotifier = root.AddComponent(Type.GetType(MahjongEventNotifierTypeName, true));
            object gameFlow = root.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "participantCount", participantCount);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            SetPrivateField(gameFlow, "autoDiscardDrawnTileDelaySeconds", 0f);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            SetPrivateField(
                gameFlow,
                "yakuDefinitionCatalog",
                CreateYakuCatalog(
                    CreateYakuDefinition("Tanyao", "One", "One"),
                    CreateYakuDefinition("Reach", "One", "None")));

            GameObject tenpai = new GameObject("Tenpai");
            tenpai.transform.SetParent(uiObject.transform);
            GameObject zeroHanObject = new GameObject("RenamedZeroHan");
            zeroHanObject.transform.SetParent(tenpai.transform);
            GameObject furitenObject = new GameObject("RenamedFuriten");
            furitenObject.transform.SetParent(tenpai.transform);

            Component zeroHanText = zeroHanObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
            Component furitenText = furitenObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
            Component zeroHanController =
                zeroHanObject.AddComponent(Type.GetType(MahjongZeroHanTenpaiControllerTypeName, true));
            Component furitenController =
                furitenObject.AddComponent(Type.GetType(MahjongFuritenControllerTypeName, true));
            SetPrivateField(zeroHanController, "zeroHanTenpaiText", zeroHanText);
            SetPrivateField(furitenController, "furitenText", furitenText);
            zeroHanObject.SetActive(false);
            furitenObject.SetActive(false);

            Component uiManager = uiObject.AddComponent(Type.GetType(MahjongPrototypeUiManagerTypeName, true));
            SetPrivateField(uiManager, "gameFlow", gameFlow);
            SetPrivateField(uiManager, "eventNotifier", eventNotifier);
            SetPrivateField(uiManager, "zeroHanTenpaiController", zeroHanController);
            SetPrivateField(uiManager, "furitenController", furitenController);

            return new UiHarness(
                root,
                uiObject,
                uiManager,
                zeroHanController,
                furitenController,
                zeroHanObject,
                furitenObject,
                zeroHanText,
                furitenText,
                gameFlow);
        }

        private static object StartRound(UiHarness harness)
        {
            Invoke(harness.GameFlow, "StartNewRound");
            return GetProperty(harness.GameFlow, "CurrentState");
        }

        private static void AddSelfFuritenHand(object gameState)
        {
            AddHandTiles(GetPlayerSeat(gameState, "East"), SimpleFiveManWait);
            AddDiscard(gameState, "East", "5m", 0);
        }

        private static void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                Invoke(hand, "Add", CreateTile(tileCodes[i]));
        }

        private static object AddDiscard(
            object gameState,
            string seatName,
            string tileCode,
            int turnIndex)
        {
            object record = Activator.CreateInstance(
                Type.GetType(DiscardRecordTypeName, true),
                ParseSeat(seatName),
                CreateTile(tileCode),
                turnIndex);
            Invoke(gameState, "AddDiscard", record);
            return record;
        }

        private static object GetPlayerSeat(object gameState, string seatName)
        {
            return Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
        }

        private static object CreateDrawResult(string seatName, string tileCode, string purposeName)
        {
            Type drawResultType = Type.GetType(DrawResultTypeName, true);
            Type drawPurposeType = Type.GetType(DrawPurposeTypeName, true);
            Type drawSourceType = Type.GetType(DrawSourceTypeName, true);
            ConstructorInfo constructor = drawResultType.GetConstructor(new[]
            {
                typeof(bool),
                Type.GetType(SeatIdTypeName, true),
                Type.GetType(TileTypeName, true),
                drawPurposeType,
                drawSourceType,
                typeof(int),
                Type.GetType("MahjongPrototype.Skills.ActiveSkillEffect, Assembly-CSharp", true),
                typeof(bool),
                typeof(bool),
                typeof(string)
            });
            Assert.That(constructor, Is.Not.Null);

            return constructor.Invoke(new[]
            {
                true,
                ParseSeat(seatName),
                CreateTile(tileCode),
                Enum.Parse(drawPurposeType, purposeName),
                Enum.Parse(drawSourceType, "Normal"),
                70,
                null,
                false,
                false,
                string.Empty
            });
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object CreateYakuCatalog(params object[] definitions)
        {
            Type catalogType = Type.GetType(YakuDefinitionCatalogTypeName, true);
            object catalog = ScriptableObject.CreateInstance(catalogType);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(
                Type.GetType(YakuDefinitionTypeName, true));
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < definitions.Length; i++)
                list.Add(definitions[i]);

            SetPrivateField(catalog, "definitions", list);
            return catalog;
        }

        private static object CreateYakuDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName)
        {
            Type definitionType = Type.GetType(YakuDefinitionTypeName, true);
            Type yakuKindType = Type.GetType(YakuKindTypeName, true);
            Type hanValueType = Type.GetType(HanValueTypeName, true);
            ConstructorInfo constructor = definitionType.GetConstructor(new[]
            {
                yakuKindType,
                typeof(string),
                hanValueType,
                hanValueType,
                typeof(bool),
                typeof(bool)
            });
            Assert.That(constructor, Is.Not.Null);

            return constructor.Invoke(new[]
            {
                Enum.Parse(yakuKindType, yakuKindName),
                yakuKindName,
                Enum.Parse(hanValueType, closedHanName),
                Enum.Parse(hanValueType, openHanName),
                false,
                true
            });
        }

        private static string SnapshotState(object gameState)
        {
            return string.Join(
                "|",
                GetProperty(gameState, "CurrentTurn"),
                GetProperty(gameState, "TurnIndex"),
                GetHandDisplayString(gameState, "East"),
                SnapshotDiscards(gameState));
        }

        private static string GetHandDisplayString(object gameState, string seatName)
        {
            object hand = GetProperty(GetPlayerSeat(gameState, seatName), "Hand");
            return (string)Invoke(hand, "ToDisplayString");
        }

        private static string SnapshotDiscards(object gameState)
        {
            object discards = GetProperty(gameState, "Discards");
            int count = (int)GetProperty(discards, "Count");
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
                snapshot += "|" + GetListItem(discards, i);

            return snapshot;
        }

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static object InvokeWithSignature(
            object target,
            string methodName,
            Type[] parameterTypes,
            params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                parameterTypes,
                null);
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

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private sealed class UiHarness
        {
            public UiHarness(
                GameObject root,
                GameObject uiObject,
                object uiManager,
                object zeroHanController,
                object furitenController,
                GameObject zeroHanTextObject,
                GameObject furitenTextObject,
                object zeroHanText,
                object furitenText,
                object gameFlow)
            {
                Root = root;
                UiObject = uiObject;
                UiManager = uiManager;
                ZeroHanController = zeroHanController;
                FuritenController = furitenController;
                ZeroHanTextObject = zeroHanTextObject;
                FuritenTextObject = furitenTextObject;
                ZeroHanText = zeroHanText;
                FuritenText = furitenText;
                GameFlow = gameFlow;
            }

            public GameObject Root { get; }
            public GameObject UiObject { get; }
            public object UiManager { get; }
            public object ZeroHanController { get; }
            public object FuritenController { get; }
            public GameObject ZeroHanTextObject { get; }
            public GameObject FuritenTextObject { get; }
            public object ZeroHanText { get; }
            public object FuritenText { get; }
            public object GameFlow { get; }

            public void Destroy()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
    }
}
