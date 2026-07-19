using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongRoundResultYakuRowControllerTests
    {
        [Test]
        public void Bind_NormalYaku_SetsNameAndHanWhenObjectNamesDiffer()
        {
            using (RoundResultYakuRowHarness harness = RoundResultYakuRowHarness.Create())
            {
                object yaku = harness.CreateYaku("Tanyao", "断么九", "One", false);

                harness.Bind(yaku);

                Assert.That(harness.YakuNameText, Is.EqualTo("断么九"));
                Assert.That(harness.ValueText, Is.EqualTo("1翻"));
            }
        }

        [Test]
        public void Bind_Yakuman_SetsYakumanValue()
        {
            using (RoundResultYakuRowHarness harness = RoundResultYakuRowHarness.Create())
            {
                object yaku = harness.CreateYaku("Daisangen", "大三元", "None", true);

                harness.Bind(yaku);

                Assert.That(harness.YakuNameText, Is.EqualTo("大三元"));
                Assert.That(harness.ValueText, Is.EqualTo("役満"));
            }
        }

        [Test]
        public void Bind_MissingReferences_DoesNotThrowAndWarnsOnlyOncePerReference()
        {
            using (RoundResultYakuRowHarness harness =
                RoundResultYakuRowHarness.CreateWithoutTextReferences())
            {
                int warningCount = 0;
                Application.LogCallback callback = (condition, _, type) =>
                {
                    if (type == LogType.Warning &&
                        condition.StartsWith("MahjongRoundResultYakuRowController:"))
                    {
                        warningCount++;
                    }
                };
                Application.logMessageReceived += callback;

                try
                {
                    object yaku = harness.CreateYaku("Reach", "立直", "One", false);

                    Assert.DoesNotThrow(() => harness.Bind(yaku));
                    Assert.DoesNotThrow(() => harness.Bind(yaku));

                    Assert.That(warningCount, Is.EqualTo(2));
                }
                finally
                {
                    Application.logMessageReceived -= callback;
                }
            }
        }
    }

    public sealed class MahjongRoundResultControllerTests
    {
        [Test]
        public void SetResult_TsumoWin_DisplaysDetailsYakusTotalAndNextButton()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object result = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Tsumo",
                    null,
                    "C",
                    false,
                    harness.Data.CreateCandidate(
                        YakuSpec.Normal("Reach", "立直", "One"),
                        YakuSpec.Normal("Tanyao", "断么九", "One")));

                harness.SetResult(result);

                Assert.That(harness.RoundResultRootVisible, Is.True);
                Assert.That(harness.WinDetailsVisible, Is.True);
                Assert.That(harness.SourceSeatVisible, Is.False);
                Assert.That(harness.TitleText, Is.EqualTo("和了"));
                Assert.That(harness.RoundText, Is.EqualTo("東1局"));
                Assert.That(harness.WinnerText, Is.EqualTo("東"));
                Assert.That(harness.WinTypeText, Is.EqualTo("ツモ"));
                Assert.That(harness.WinningTileText, Is.EqualTo("C"));
                Assert.That(harness.YakuRowCount, Is.EqualTo(2));
                Assert.That(harness.YakuNameAt(0), Is.EqualTo("立直"));
                Assert.That(harness.YakuValueAt(0), Is.EqualTo("1翻"));
                Assert.That(harness.YakuNameAt(1), Is.EqualTo("断么九"));
                Assert.That(harness.TotalText, Is.EqualTo("2翻"));
                Assert.That(harness.ConfirmButtonLabel, Is.EqualTo("次局へ進む"));
            }
        }

        [Test]
        public void SetResult_RonWin_ShowsSourceSeat()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object result = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Ron",
                    "West",
                    "C",
                    false,
                    harness.Data.CreateCandidate(YakuSpec.Normal("Reach", "立直", "One")));

                harness.SetResult(result);

                Assert.That(harness.WinTypeText, Is.EqualTo("ロン"));
                Assert.That(harness.SourceSeatVisible, Is.True);
                Assert.That(harness.SourceSeatText, Is.EqualTo("西"));
            }
        }

        [TestCase(1, "役満")]
        [TestCase(2, "役満×2")]
        public void SetResult_YakumanTotals_UseYakumanCount(int yakumanCount, string expected)
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                List<YakuSpec> yakus = new List<YakuSpec>
                {
                    YakuSpec.Yakuman("Daisangen", "大三元")
                };
                if (yakumanCount > 1)
                    yakus.Add(YakuSpec.Yakuman("Tsuuiisou", "字一色"));

                object result = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Tsumo",
                    null,
                    "C",
                    false,
                    harness.Data.CreateCandidate(yakus.ToArray()));

                harness.SetResult(result);

                Assert.That(harness.TotalText, Is.EqualTo(expected));
            }
        }

        [Test]
        public void SetResult_ExhaustiveDraw_HidesWinDetailsAndClearsYakus()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object result = harness.Data.CreateExhaustiveDraw("East", 2, false);

                harness.SetResult(result);

                Assert.That(harness.RoundResultRootVisible, Is.True);
                Assert.That(harness.WinDetailsVisible, Is.False);
                Assert.That(harness.SourceSeatVisible, Is.False);
                Assert.That(harness.TitleText, Is.EqualTo("流局"));
                Assert.That(harness.RoundText, Is.EqualTo("東2局"));
                Assert.That(harness.YakuRowCount, Is.EqualTo(0));
                Assert.That(harness.TotalText, Is.EqualTo(string.Empty));
                Assert.That(harness.ConfirmButtonLabel, Is.EqualTo("次局へ進む"));
            }
        }

        [Test]
        public void SetResult_FinalRound_UsesGameEndButtonLabel()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object result = harness.Data.CreateExhaustiveDraw("South", 4, true);

                harness.SetResult(result);

                Assert.That(harness.RoundText, Is.EqualTo("南4局"));
                Assert.That(harness.ConfirmButtonLabel, Is.EqualTo("ゲーム終了"));
            }
        }

        [Test]
        public void SetResult_RepeatedCalls_DoNotDuplicateGeneratedYakuRows()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object first = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Tsumo",
                    null,
                    "C",
                    false,
                    harness.Data.CreateCandidate(
                        YakuSpec.Normal("Reach", "立直", "One"),
                        YakuSpec.Normal("Tanyao", "断么九", "One")));
                object second = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Tsumo",
                    null,
                    "C",
                    false,
                    harness.Data.CreateCandidate(YakuSpec.Normal("Pinfu", "平和", "One")));

                harness.SetResult(first);
                harness.SetResult(second);

                Assert.That(harness.YakuRowCount, Is.EqualTo(1));
                Assert.That(harness.YakuNameAt(0), Is.EqualTo("平和"));
            }
        }

        [Test]
        public void ClearAndSetResultNull_HidePanelAndRemoveGeneratedRows()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object result = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Tsumo",
                    null,
                    "C",
                    false,
                    harness.Data.CreateCandidate(YakuSpec.Normal("Reach", "立直", "One")));

                harness.SetResult(result);
                harness.Clear();

                Assert.That(harness.RoundResultRootVisible, Is.False);
                Assert.That(harness.YakuRowCount, Is.EqualTo(0));

                harness.SetResult(result);
                harness.SetResultNull();

                Assert.That(harness.RoundResultRootVisible, Is.False);
                Assert.That(harness.YakuRowCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void SetResult_IncompleteWinData_DoesNotThrow()
        {
            using (RoundResultControllerTestHarness harness =
                RoundResultControllerTestHarness.Create())
            {
                object result = harness.Data.CreateWin(
                    "East",
                    1,
                    "East",
                    "Ron",
                    null,
                    null,
                    false,
                    null);

                Assert.DoesNotThrow(() => harness.SetResult(result));
                Assert.That(harness.YakuRowCount, Is.EqualTo(0));
                Assert.That(harness.WinningTileText, Is.EqualTo(string.Empty));
            }
        }
    }

    public sealed class MahjongRoundResultUiConnectionTests
    {
        [Test]
        public void RoundResultConfirmButton_NormalRound_AdvancesToNextRoundThroughGameFlow()
        {
            using (RoundResultUiConnectionDriver driver = RoundResultUiConnectionDriver.Create())
            {
                driver.PrepareNormalExhaustiveDrawResult();
                driver.EnableRoundResultButtonRouting();

                driver.ClickRoundResultConfirm();

                Assert.That(driver.IsRoundResultPending, Is.False);
                Assert.That(driver.IsGameEnded, Is.False);
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(2));
            }
        }

        [Test]
        public void RoundResultConfirmButton_FinalRound_MovesToGameEndedThroughGameFlow()
        {
            using (RoundResultUiConnectionDriver driver = RoundResultUiConnectionDriver.Create())
            {
                driver.PrepareFinalExhaustiveDrawResult();
                driver.EnableRoundResultButtonRouting();

                driver.ClickRoundResultConfirm();

                Assert.That(driver.IsRoundResultPending, Is.False);
                Assert.That(driver.IsGameEnded, Is.True);
                Assert.That(driver.WindProgressRoundWindName, Is.EqualTo("South"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(4));
            }
        }

        [Test]
        public void UiManager_RoundResultReadyShowsAndConfirmedOrGameEndedClears()
        {
            using (RoundResultUiConnectionDriver driver = RoundResultUiConnectionDriver.Create())
            {
                driver.EnableUiManagerNotifications();
                object result = driver.CreateExhaustiveDrawResult("East", 1, false);

                driver.NotifyRoundResultReady(result);

                Assert.That(driver.RoundResultRootVisible, Is.True);
                Assert.That(driver.ResultTitleText, Is.EqualTo("流局"));

                driver.NotifyRoundResultConfirmed(result);

                Assert.That(driver.RoundResultRootVisible, Is.False);

                driver.NotifyRoundResultReady(result);
                driver.NotifyGameEnded(result);

                Assert.That(driver.RoundResultRootVisible, Is.False);
            }
        }

        [Test]
        public void UiManager_RefreshRestoresCurrentRoundResultPendingState()
        {
            using (RoundResultUiConnectionDriver driver = RoundResultUiConnectionDriver.Create())
            {
                driver.BeginStateRoundResult("East", 2, false);

                driver.RefreshCurrentState();

                Assert.That(driver.RoundResultRootVisible, Is.True);
                Assert.That(driver.ResultTitleText, Is.EqualTo("流局"));
                Assert.That(driver.ResultRoundText, Is.EqualTo("東2局"));
            }
        }

        [Test]
        public void UiManager_AfterUnsubscribe_DoesNotUpdateRoundResultUi()
        {
            using (RoundResultUiConnectionDriver driver = RoundResultUiConnectionDriver.Create())
            {
                driver.EnableUiManagerNotifications();
                driver.DisableUiManagerNotifications();
                object result = driver.CreateExhaustiveDrawResult("East", 1, false);

                driver.NotifyRoundResultReady(result);

                Assert.That(driver.RoundResultRootVisible, Is.False);
            }
        }
    }

    public sealed class MahjongRoundResultPrefabAndSceneConnectionTests
    {
        private const string ResultPanelPath = "Assets/Prefab/Result/Result Panel.prefab";
        private const string YakuRowPrefabPath = "Assets/Prefab/Result/Yaku Line Prefab.prefab";
        private const string MainScenePath = "Assets/Scenes/Mahjong Prototype.unity";
        private const string ResultControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundResultController, Assembly-CSharp";
        private const string YakuRowControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundResultYakuRowController, Assembly-CSharp";
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string RoundProgressControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundProgressController, Assembly-CSharp";
        private const string InputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";

        [Test]
        public void ResultPanelPrefab_HasControllerAndInspectorReferences()
        {
            GameObject prefab = LoadPrefab(ResultPanelPath);
            Component controller = RequireComponent(prefab, ResultControllerTypeName);
            SerializedObject serialized = new SerializedObject(controller);

            AssertObjectReference(serialized, "roundResultRoot", prefab);
            AssertObjectReferenceName(serialized, "winDetailsRoot", "displayArea");
            AssertObjectReferenceName(serialized, "sourceSeatRoot", "SourceSeatRoot");
            AssertObjectReferenceName(serialized, "titleText", "TitleText");
            AssertObjectReferenceTrimmedName(serialized, "roundText", "RoundText");
            AssertObjectReferenceName(serialized, "winnerText", "WinnerText");
            AssertObjectReferenceName(serialized, "winTypeText", "WinTypeText");
            AssertObjectReferenceName(serialized, "sourceSeatText", "SourceSeatText");
            AssertObjectReferenceName(serialized, "winningTileText", "WinningTileText");
            AssertObjectReferenceName(serialized, "totalText", "TotalText");
            AssertObjectReferenceName(serialized, "confirmButtonLabel", "Text (TMP)");
            AssertObjectReferenceName(serialized, "confirmButton", "Button");

            Object yakuListRoot = ObjectReference(serialized, "yakuListRoot");
            Assert.That(yakuListRoot, Is.TypeOf<RectTransform>());
            RectTransform yakuListTransform = (RectTransform)yakuListRoot;
            Assert.That(yakuListTransform.name, Is.EqualTo("Content"));
            Assert.That(yakuListTransform.parent.name, Is.EqualTo("displayArea"));
            Assert.That(yakuListTransform.GetComponent<GridLayoutGroup>(), Is.Not.Null);

            Object yakuRowPrefab = ObjectReference(serialized, "yakuRowPrefab");
            Assert.That(AssetDatabase.GetAssetPath(yakuRowPrefab), Is.EqualTo(YakuRowPrefabPath));
        }

        [Test]
        public void YakuLinePrefab_HasControllerReferencesAndLayoutElement()
        {
            GameObject prefab = LoadPrefab(YakuRowPrefabPath);
            Component controller = RequireComponent(prefab, YakuRowControllerTypeName);
            SerializedObject serialized = new SerializedObject(controller);

            AssertObjectReferenceName(serialized, "yakuNameText", "Yaku Text");
            AssertObjectReferenceName(serialized, "valueText", "YakuValueText");
            Assert.That(prefab.GetComponent<LayoutElement>(), Is.Not.Null);
        }

        [Test]
        public void MainScene_UsesExistingResultPanelInstanceAndConnectsUiReferences()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject[] resultPanels = FindSceneGameObjects(scene, "Result Panel");
                Assert.That(resultPanels, Has.Length.EqualTo(1));

                GameObject resultPanel = resultPanels[0];
                Assert.That(resultPanel.activeSelf, Is.False);
                Component resultController = RequireComponent(resultPanel, ResultControllerTypeName);

                Component uiManager = FindSceneComponent(scene, UiManagerTypeName);
                SerializedObject uiSerialized = new SerializedObject(uiManager);
                AssertObjectReference(uiSerialized, "roundResultController", resultController);

                Component roundProgressController =
                    FindSceneComponent(scene, RoundProgressControllerTypeName);
                AssertObjectReference(
                    uiSerialized,
                    "roundProgressController",
                    roundProgressController);
                Assert.That(FindSceneGameObjects(scene, "Round Progress"), Has.Length.EqualTo(1));

                Component inputController = FindSceneComponent(scene, InputControllerTypeName);
                SerializedObject inputSerialized = new SerializedObject(inputController);
                Object confirmButtonReference =
                    ObjectReference(inputSerialized, "roundResultConfirmButton");
                Assert.That(confirmButtonReference, Is.TypeOf<Button>());

                Button confirmButton = (Button)confirmButtonReference;
                Assert.That(confirmButton.gameObject.name, Is.EqualTo("Button"));
                Assert.That(confirmButton.transform.IsChildOf(resultPanel.transform), Is.True);
                Assert.That(confirmButton.onClick.GetPersistentEventCount(), Is.EqualTo(0));
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Prefab not found at {path}.");
            return prefab;
        }

        private static Component RequireComponent(GameObject gameObject, string typeName)
        {
            Type type = RequireType(typeName);
            Component component = gameObject.GetComponent(type);
            Assert.That(component, Is.Not.Null, $"{gameObject.name} is missing {typeName}.");
            return component;
        }

        private static Component FindSceneComponent(Scene scene, string typeName)
        {
            Type type = RequireType(typeName);
            Component component = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(gameObject => gameObject.scene == scene)
                .Select(gameObject => gameObject.GetComponent(type))
                .FirstOrDefault(found => found != null);
            Assert.That(component, Is.Not.Null, $"Scene is missing {typeName}.");
            return component;
        }

        private static GameObject[] FindSceneGameObjects(Scene scene, string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(gameObject => gameObject.scene == scene && gameObject.name == name)
                .ToArray();
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Type not found: {typeName}.");
            return type;
        }

        private static void AssertObjectReference(
            SerializedObject serialized,
            string propertyName,
            Object expected)
        {
            Assert.That(ObjectReference(serialized, propertyName), Is.SameAs(expected));
        }

        private static void AssertObjectReferenceName(
            SerializedObject serialized,
            string propertyName,
            string expectedName)
        {
            Object reference = ObjectReference(serialized, propertyName);
            Assert.That(reference.name, Is.EqualTo(expectedName));
        }

        private static void AssertObjectReferenceTrimmedName(
            SerializedObject serialized,
            string propertyName,
            string expectedName)
        {
            Object reference = ObjectReference(serialized, propertyName);
            Assert.That(reference.name.Trim(), Is.EqualTo(expectedName));
        }

        private static Object ObjectReference(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Serialized property not found: {propertyName}.");
            Assert.That(property.objectReferenceValue, Is.Not.Null, $"{propertyName} is not assigned.");
            return property.objectReferenceValue;
        }
    }

    internal readonly struct YakuSpec
    {
        private YakuSpec(string kindName, string displayName, string hanName, bool isYakuman)
        {
            KindName = kindName;
            DisplayName = displayName;
            HanName = hanName;
            IsYakuman = isYakuman;
        }

        public string KindName { get; }
        public string DisplayName { get; }
        public string HanName { get; }
        public bool IsYakuman { get; }

        public static YakuSpec Normal(string kindName, string displayName, string hanName)
        {
            return new YakuSpec(kindName, displayName, hanName, false);
        }

        public static YakuSpec Yakuman(string kindName, string displayName)
        {
            return new YakuSpec(kindName, displayName, "None", true);
        }
    }

    internal sealed class RoundResultUiTestData
    {
        private const string RoundResultTypeName =
            "MahjongPrototype.Domain.RoundResult, Assembly-CSharp";
        private const string HandEvaluationCandidateTypeName =
            "MahjongPrototype.Domain.HandEvaluationCandidate, Assembly-CSharp";
        private const string HandEvaluationCandidateResultTypeName =
            "MahjongPrototype.Domain.HandEvaluationCandidateResult, Assembly-CSharp";
        private const string SevenPairsAnalysisTypeName =
            "MahjongPrototype.Domain.SevenPairsAnalysis, Assembly-CSharp";
        private const string EvaluatedYakuTypeName =
            "MahjongPrototype.Domain.EvaluatedYaku, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory dataFactory;

        public RoundResultUiTestData(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory)
        {
            this.reflection = reflection;
            this.types = types;
            this.dataFactory = dataFactory;
        }

        private Type RoundResultType => reflection.RequireType(RoundResultTypeName);
        private Type CandidateResultType =>
            reflection.RequireType(HandEvaluationCandidateResultTypeName);
        private Type EvaluatedYakuType => reflection.RequireType(EvaluatedYakuTypeName);

        public object CreateWin(
            string roundWindName,
            int handNumber,
            string winnerSeatName,
            string winTypeName,
            string sourceSeatName,
            string winningTileCode,
            bool isFinalRound,
            object selectedCandidate)
        {
            object sourceSeat = sourceSeatName == null
                ? null
                : dataFactory.ParseSeat(sourceSeatName);
            object winningTile = winningTileCode == null
                ? null
                : dataFactory.CreateTile(winningTileCode);

            return reflection.InvokeStatic(
                RoundResultType,
                "CreateWin",
                dataFactory.CreateWindProgress(roundWindName, handNumber),
                3,
                dataFactory.ParseSeat(winnerSeatName),
                dataFactory.ParseWinType(winTypeName),
                sourceSeat,
                winningTile,
                selectedCandidate,
                isFinalRound);
        }

        public object CreateExhaustiveDraw(
            string roundWindName,
            int handNumber,
            bool isFinalRound)
        {
            return reflection.InvokeStatic(
                RoundResultType,
                "CreateExhaustiveDraw",
                dataFactory.CreateWindProgress(roundWindName, handNumber),
                12,
                isFinalRound);
        }

        public object CreateCandidate(params YakuSpec[] yakuSpecs)
        {
            return reflection.CreateInstance(
                CandidateResultType,
                CreateSevenPairsCandidate(),
                CreateEvaluatedYakuList(yakuSpecs));
        }

        public object CreateYaku(
            string kindName,
            string displayName,
            string hanName,
            bool isYakuman)
        {
            return reflection.CreateInstance(
                EvaluatedYakuType,
                Enum.Parse(types.YakuKind, kindName),
                displayName,
                Enum.Parse(types.HanValue, hanName),
                isYakuman);
        }

        private object CreateSevenPairsCandidate()
        {
            object analysis = reflection.InvokeStatic(
                reflection.RequireType(SevenPairsAnalysisTypeName),
                "Win",
                dataFactory.CreateTileArray("1m", "2m", "3p", "4p", "5s", "E", "C"));
            return reflection.InvokeStatic(
                reflection.RequireType(HandEvaluationCandidateTypeName),
                "SevenPairs",
                analysis);
        }

        private object CreateEvaluatedYakuList(params YakuSpec[] yakuSpecs)
        {
            Type listType = typeof(List<>).MakeGenericType(EvaluatedYakuType);
            IList list = (IList)Activator.CreateInstance(listType);
            for (int i = 0; i < yakuSpecs.Length; i++)
            {
                YakuSpec spec = yakuSpecs[i];
                list.Add(CreateYaku(spec.KindName, spec.DisplayName, spec.HanName, spec.IsYakuman));
            }

            return list;
        }
    }

    internal sealed class RoundResultYakuRowHarness : IDisposable
    {
        private const string RowControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundResultYakuRowController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private readonly ReflectionTestAccess reflection;
        private readonly GameObject root;
        private readonly Component controller;
        private readonly Component yakuNameText;
        private readonly Component valueText;
        private readonly RoundResultUiTestData data;

        private RoundResultYakuRowHarness(
            ReflectionTestAccess reflection,
            GameObject root,
            Component controller,
            Component yakuNameText,
            Component valueText,
            RoundResultUiTestData data)
        {
            this.reflection = reflection;
            this.root = root;
            this.controller = controller;
            this.yakuNameText = yakuNameText;
            this.valueText = valueText;
            this.data = data;
        }

        public string YakuNameText => Text(yakuNameText);
        public string ValueText => Text(valueText);

        public static RoundResultYakuRowHarness Create()
        {
            return Create(assignTexts: true);
        }

        public static RoundResultYakuRowHarness CreateWithoutTextReferences()
        {
            return Create(assignTexts: false);
        }

        public object CreateYaku(
            string kindName,
            string displayName,
            string hanName,
            bool isYakuman)
        {
            return data.CreateYaku(kindName, displayName, hanName, isYakuman);
        }

        public void Bind(object yaku)
        {
            reflection.Invoke(controller, "Bind", yaku);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static RoundResultYakuRowHarness Create(bool assignTexts)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            RoundResultUiTestData data =
                new RoundResultUiTestData(reflection, types, dataFactory);
            GameObject root = new GameObject("RoundResultYakuRowHarness");
            root.SetActive(false);
            Component controller = root.AddComponent(reflection.RequireType(RowControllerTypeName));
            Component yakuNameText = CreateText(reflection, root.transform, "RenamedYakuName");
            Component valueText = CreateText(reflection, root.transform, "RenamedYakuValue");

            if (assignTexts)
            {
                reflection.SetPrivateField(controller, "yakuNameText", yakuNameText);
                reflection.SetPrivateField(controller, "valueText", valueText);
            }

            return new RoundResultYakuRowHarness(
                reflection,
                root,
                controller,
                yakuNameText,
                valueText,
                data);
        }

        private string Text(Component text)
        {
            return (string)reflection.GetProperty(text, "text");
        }

        private static Component CreateText(
            ReflectionTestAccess reflection,
            Transform parent,
            string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent(reflection.RequireType(TextMeshProUguiTypeName));
        }
    }

    internal sealed class RoundResultControllerTestHarness : IDisposable
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundResultController, Assembly-CSharp";
        private const string RowControllerTypeName =
            "MahjongPrototype.UI.MahjongRoundResultYakuRowController, Assembly-CSharp";
        private const string RoundResultTypeName =
            "MahjongPrototype.Domain.RoundResult, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private readonly ReflectionTestAccess reflection;
        private readonly GameObject root;
        private readonly Component controller;
        private readonly GameObject roundResultRoot;
        private readonly GameObject winDetailsRoot;
        private readonly GameObject sourceSeatRoot;
        private readonly Transform yakuListRoot;
        private readonly TextRefs texts;
        private bool disposed;

        private RoundResultControllerTestHarness(
            ReflectionTestAccess reflection,
            GameObject root,
            Component controller,
            GameObject roundResultRoot,
            GameObject winDetailsRoot,
            GameObject sourceSeatRoot,
            Transform yakuListRoot,
            TextRefs texts,
            RoundResultUiTestData data)
        {
            this.reflection = reflection;
            this.root = root;
            this.controller = controller;
            this.roundResultRoot = roundResultRoot;
            this.winDetailsRoot = winDetailsRoot;
            this.sourceSeatRoot = sourceSeatRoot;
            this.yakuListRoot = yakuListRoot;
            this.texts = texts;
            Data = data;
        }

        public RoundResultUiTestData Data { get; }
        public bool RoundResultRootVisible => roundResultRoot.activeSelf;
        public bool WinDetailsVisible => winDetailsRoot.activeSelf;
        public bool SourceSeatVisible => sourceSeatRoot.activeSelf;
        public int YakuRowCount => yakuListRoot.childCount;
        public string TitleText => Text(texts.TitleText);
        public string RoundText => Text(texts.RoundText);
        public string WinnerText => Text(texts.WinnerText);
        public string WinTypeText => Text(texts.WinTypeText);
        public string SourceSeatText => Text(texts.SourceSeatText);
        public string WinningTileText => Text(texts.WinningTileText);
        public string TotalText => Text(texts.TotalText);
        public string ConfirmButtonLabel => Text(texts.ConfirmButtonLabel);
        public Component Controller => controller;

        public static RoundResultControllerTestHarness Create()
        {
            return Create(new ReflectionTestAccess(), null);
        }

        public static RoundResultControllerTestHarness Create(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            RoundResultUiTestData data =
                new RoundResultUiTestData(reflection, types, dataFactory);

            GameObject root = new GameObject("RoundResultControllerHarness");
            root.SetActive(false);
            if (parent != null)
                root.transform.SetParent(parent);

            Component controller = root.AddComponent(reflection.RequireType(ControllerTypeName));
            GameObject roundResultRoot = CreateChild(root.transform, "RenamedRoundResultPanel");
            GameObject winDetailsRoot = CreateChild(roundResultRoot.transform, "RenamedWinDetails");
            GameObject sourceSeatRoot = CreateChild(winDetailsRoot.transform, "RenamedSourceSeat");
            Transform yakuListRoot =
                CreateChild(roundResultRoot.transform, "RenamedYakuList").transform;
            TextRefs texts = TextRefs.Create(reflection, roundResultRoot.transform);
            Component rowPrefab = CreateYakuRowPrefab(reflection, root.transform);

            reflection.SetPrivateField(controller, "roundResultRoot", roundResultRoot);
            reflection.SetPrivateField(controller, "winDetailsRoot", winDetailsRoot);
            reflection.SetPrivateField(controller, "sourceSeatRoot", sourceSeatRoot);
            reflection.SetPrivateField(controller, "titleText", texts.TitleText);
            reflection.SetPrivateField(controller, "roundText", texts.RoundText);
            reflection.SetPrivateField(controller, "winnerText", texts.WinnerText);
            reflection.SetPrivateField(controller, "winTypeText", texts.WinTypeText);
            reflection.SetPrivateField(controller, "sourceSeatText", texts.SourceSeatText);
            reflection.SetPrivateField(controller, "winningTileText", texts.WinningTileText);
            reflection.SetPrivateField(controller, "totalText", texts.TotalText);
            reflection.SetPrivateField(controller, "confirmButtonLabel", texts.ConfirmButtonLabel);
            reflection.SetPrivateField(controller, "yakuListRoot", yakuListRoot);
            reflection.SetPrivateField(controller, "yakuRowPrefab", rowPrefab);

            return new RoundResultControllerTestHarness(
                reflection,
                root,
                controller,
                roundResultRoot,
                winDetailsRoot,
                sourceSeatRoot,
                yakuListRoot,
                texts,
                data);
        }

        public void SetResult(object result)
        {
            reflection.Invoke(controller, "SetResult", result);
        }

        public void SetResultNull()
        {
            reflection.InvokeWithSignature(
                controller,
                "SetResult",
                new[] { reflection.RequireType(RoundResultTypeName) },
                new object[] { null });
        }

        public void Clear()
        {
            reflection.Invoke(controller, "Clear");
        }

        public string YakuNameAt(int index)
        {
            return RowText(index, "yakuNameText");
        }

        public string YakuValueAt(int index)
        {
            return RowText(index, "valueText");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            UnityEngine.Object.DestroyImmediate(root);
        }

        private string RowText(int index, string fieldName)
        {
            Component row = yakuListRoot.GetChild(index)
                .GetComponent(reflection.RequireType(RowControllerTypeName));
            Component text = (Component)reflection.GetPrivateField(row, fieldName);
            return Text(text);
        }

        private string Text(Component text)
        {
            return (string)reflection.GetProperty(text, "text");
        }

        private static Component CreateYakuRowPrefab(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject rowObject = CreateChild(parent, "RenamedYakuRowPrefab");
            Component row = rowObject.AddComponent(reflection.RequireType(RowControllerTypeName));
            Component nameText = CreateText(reflection, rowObject.transform, "RenamedRowName");
            Component valueText = CreateText(reflection, rowObject.transform, "RenamedRowValue");
            reflection.SetPrivateField(row, "yakuNameText", nameText);
            reflection.SetPrivateField(row, "valueText", valueText);
            return row;
        }

        private static Component CreateText(
            ReflectionTestAccess reflection,
            Transform parent,
            string name)
        {
            return CreateChild(parent, name)
                .AddComponent(reflection.RequireType(TextMeshProUguiTypeName));
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject;
        }

        private sealed class TextRefs
        {
            public Component TitleText;
            public Component RoundText;
            public Component WinnerText;
            public Component WinTypeText;
            public Component SourceSeatText;
            public Component WinningTileText;
            public Component TotalText;
            public Component ConfirmButtonLabel;

            public static TextRefs Create(
                ReflectionTestAccess reflection,
                Transform parent)
            {
                return new TextRefs
                {
                    TitleText = CreateText(reflection, parent, "RenamedTitle"),
                    RoundText = CreateText(reflection, parent, "RenamedRound"),
                    WinnerText = CreateText(reflection, parent, "RenamedWinner"),
                    WinTypeText = CreateText(reflection, parent, "RenamedWinType"),
                    SourceSeatText = CreateText(reflection, parent, "RenamedSourceSeatText"),
                    WinningTileText = CreateText(reflection, parent, "RenamedWinningTile"),
                    TotalText = CreateText(reflection, parent, "RenamedTotal"),
                    ConfirmButtonLabel = CreateText(reflection, parent, "RenamedConfirmLabel")
                };
            }
        }
    }

    internal sealed class RoundResultUiConnectionDriver : IDisposable
    {
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string InputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string CommandRouterTypeName =
            "MahjongPrototype.UI.MahjongUiCommandRouter, Assembly-CSharp";
        private const string DisplayControllerTypeName =
            "MahjongPrototype.UI.MahjongUiDisplayController, Assembly-CSharp";
        private const string WinDecisionControllerTypeName =
            "MahjongPrototype.UI.MahjongWinDecisionController, Assembly-CSharp";
        private const string ReachDecisionControllerTypeName =
            "MahjongPrototype.UI.MahjongReachDecisionController, Assembly-CSharp";
        private const string LogPreviewControllerTypeName =
            "MahjongPrototype.UI.MahjongLogPreviewController, Assembly-CSharp";
        private const string ZeroHanTenpaiControllerTypeName =
            "MahjongPrototype.UI.MahjongZeroHanTenpaiController, Assembly-CSharp";
        private const string FuritenControllerTypeName =
            "MahjongPrototype.UI.MahjongFuritenController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        private readonly MahjongGameFlowTestSession session;
        private readonly Component uiManager;
        private readonly Component inputController;
        private readonly Component commandRouter;
        private readonly Button roundResultConfirmButton;
        private readonly RoundResultControllerTestHarness roundResultHarness;
        private bool inputRoutingEnabled;
        private bool uiManagerEnabled;
        private bool disposed;

        private RoundResultUiConnectionDriver(
            MahjongGameFlowTestSession session,
            Component uiManager,
            Component inputController,
            Component commandRouter,
            Button roundResultConfirmButton,
            RoundResultControllerTestHarness roundResultHarness)
        {
            this.session = session;
            this.uiManager = uiManager;
            this.inputController = inputController;
            this.commandRouter = commandRouter;
            this.roundResultConfirmButton = roundResultConfirmButton;
            this.roundResultHarness = roundResultHarness;
        }

        public bool IsRoundResultPending => session.Query.IsRoundResultPending;
        public bool IsGameEnded => session.Query.IsGameEnded;
        public string WindProgressRoundWindName => session.Query.WindProgressRoundWindName;
        public int WindProgressHandNumber => session.Query.WindProgressHandNumber;
        public bool RoundResultRootVisible => roundResultHarness.RoundResultRootVisible;
        public string ResultTitleText => roundResultHarness.TitleText;
        public string ResultRoundText => roundResultHarness.RoundText;

        public static RoundResultUiConnectionDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "RoundResultUiConnectionHarness",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = 1,
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
                return CreateUi(session);
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        public object CreateExhaustiveDrawResult(
            string roundWindName,
            int handNumber,
            bool isFinalRound)
        {
            return roundResultHarness.Data.CreateExhaustiveDraw(
                roundWindName,
                handNumber,
                isFinalRound);
        }

        public void BeginStateRoundResult(
            string roundWindName,
            int handNumber,
            bool isFinalRound)
        {
            session.Commands.StartNewRound();
            object result = CreateExhaustiveDrawResult(roundWindName, handNumber, isFinalRound);
            session.Reflection.Invoke(session.CurrentState, "BeginRoundResult", result);
        }

        public void PrepareNormalExhaustiveDrawResult()
        {
            session.Commands.StartNewRound();
            ClearWall();
            session.Commands.RequestDraw();
            Assert.That(IsRoundResultPending, Is.True);
        }

        public void PrepareFinalExhaustiveDrawResult()
        {
            object windProgress = session.DataFactory.CreateWindProgress("South", 4);
            session.Reflection.InvokeWithSignature(
                session.GameFlow,
                "StartRound",
                new[] { session.Types.WindProgress, typeof(bool), session.Types.SeatId },
                windProgress,
                false,
                session.DataFactory.ParseSeat("South"));
            ClearWall();
            session.Commands.RequestDraw();
            Assert.That(IsRoundResultPending, Is.True);
        }

        public void EnableRoundResultButtonRouting()
        {
            if (inputRoutingEnabled)
                return;

            session.Reflection.Invoke(inputController, "OnEnable");
            session.Reflection.Invoke(commandRouter, "OnEnable");
            inputRoutingEnabled = true;
        }

        public void ClickRoundResultConfirm()
        {
            roundResultConfirmButton.onClick.Invoke();
        }

        public void EnableUiManagerNotifications()
        {
            if (uiManagerEnabled)
                return;

            session.Reflection.Invoke(uiManager, "OnEnable");
            uiManagerEnabled = true;
        }

        public void DisableUiManagerNotifications()
        {
            if (!uiManagerEnabled)
                return;

            session.Reflection.Invoke(uiManager, "OnDisable");
            uiManagerEnabled = false;
        }

        public void RefreshCurrentState()
        {
            session.Reflection.InvokeWithSignature(
                uiManager,
                "Refresh",
                new[] { session.Types.MahjongGameState },
                session.CurrentState);
        }

        public void NotifyRoundResultReady(object result)
        {
            session.Reflection.Invoke(session.EventNotifier, "NotifyRoundResultReady", result);
        }

        public void NotifyRoundResultConfirmed(object result)
        {
            session.Reflection.Invoke(session.EventNotifier, "NotifyRoundResultConfirmed", result);
        }

        public void NotifyGameEnded(object result)
        {
            session.Reflection.Invoke(session.EventNotifier, "NotifyGameEnded", result);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (uiManagerEnabled)
                DisableUiManagerNotifications();
            if (inputRoutingEnabled)
            {
                session.Reflection.Invoke(commandRouter, "OnDisable");
                session.Reflection.Invoke(inputController, "OnDisable");
            }

            roundResultHarness.Dispose();
            session.Dispose();
        }

        private static RoundResultUiConnectionDriver CreateUi(MahjongGameFlowTestSession session)
        {
            ReflectionTestAccess reflection = session.Reflection;
            Transform rootTransform = ((Component)session.GameFlow).transform;
            GameObject uiRoot = new GameObject("RoundResultUiRoot");
            uiRoot.SetActive(false);
            uiRoot.transform.SetParent(rootTransform);

            Component uiManager = uiRoot.AddComponent(reflection.RequireType(UiManagerTypeName));
            Component inputController = CreateInputController(
                reflection,
                uiRoot.transform,
                out Button roundResultConfirmButton);
            Component commandRouter = CreateCommandRouter(
                reflection,
                uiRoot.transform,
                session.GameFlow,
                inputController);
            RoundResultControllerTestHarness roundResultHarness =
                RoundResultControllerTestHarness.Create(reflection, uiRoot.transform);

            reflection.SetPrivateField(uiManager, "gameFlow", session.GameFlow);
            reflection.SetPrivateField(uiManager, "eventNotifier", session.EventNotifier);
            reflection.SetPrivateField(uiManager, "inputController", inputController);
            reflection.SetPrivateField(uiManager, "commandRouter", commandRouter);
            reflection.SetPrivateField(uiManager, "roundResultController", roundResultHarness.Controller);
            AssignSupportControllers(reflection, uiRoot.transform, uiManager);

            return new RoundResultUiConnectionDriver(
                session,
                uiManager,
                inputController,
                commandRouter,
                roundResultConfirmButton,
                roundResultHarness);
        }

        private static void AssignSupportControllers(
            ReflectionTestAccess reflection,
            Transform parent,
            Component uiManager)
        {
            reflection.SetPrivateField(
                uiManager,
                "displayController",
                CreateDisplayController(reflection, parent));
            reflection.SetPrivateField(
                uiManager,
                "winDecisionController",
                CreateWinDecisionController(reflection, parent));
            reflection.SetPrivateField(
                uiManager,
                "reachDecisionController",
                CreateReachDecisionController(reflection, parent));
            reflection.SetPrivateField(
                uiManager,
                "logPreviewController",
                CreateLogPreviewController(reflection, parent));
            reflection.SetPrivateField(
                uiManager,
                "zeroHanTenpaiController",
                CreateSingleTextController(reflection, parent, ZeroHanTenpaiControllerTypeName, "zeroHanTenpaiText"));
            reflection.SetPrivateField(
                uiManager,
                "furitenController",
                CreateSingleTextController(reflection, parent, FuritenControllerTypeName, "furitenText"));
        }

        private static Component CreateDisplayController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject displayObject = CreateChild(parent, "DisplaySupport");
            Component controller = displayObject.AddComponent(reflection.RequireType(DisplayControllerTypeName));
            reflection.SetPrivateField(controller, "currentTurnText", CreateText(reflection, displayObject.transform, "CurrentTurn"));
            reflection.SetPrivateField(controller, "turnIndexText", CreateText(reflection, displayObject.transform, "TurnIndex"));
            reflection.SetPrivateField(controller, "wallCountText", CreateText(reflection, displayObject.transform, "WallCount"));
            reflection.SetPrivateField(controller, "activeSkillText", CreateText(reflection, displayObject.transform, "ActiveSkill"));
            return controller;
        }

        private static Component CreateInputController(
            ReflectionTestAccess reflection,
            Transform parent,
            out Button roundResultConfirmButton)
        {
            GameObject inputObject = CreateChild(parent, "InputSupport");
            Component controller = inputObject.AddComponent(reflection.RequireType(InputControllerTypeName));
            reflection.SetPrivateField(controller, "drawButton", CreateButton(inputObject.transform, "Draw"));
            reflection.SetPrivateField(controller, "forceDrawSkillButton", CreateButton(inputObject.transform, "ForceDrawSkill"));
            reflection.SetPrivateField(controller, "autoSortToggle", CreateToggle(inputObject.transform, "AutoSort"));
            reflection.SetPrivateField(controller, "retryButton", CreateButton(inputObject.transform, "Retry"));
            reflection.SetPrivateField(controller, "winButton", CreateButton(inputObject.transform, "Win"));
            reflection.SetPrivateField(controller, "declineWinButton", CreateButton(inputObject.transform, "DeclineWin"));
            reflection.SetPrivateField(controller, "reachButton", CreateButton(inputObject.transform, "Reach"));
            reflection.SetPrivateField(controller, "declineReachButton", CreateButton(inputObject.transform, "DeclineReach"));
            reflection.SetPrivateField(controller, "cancelReachButton", CreateButton(inputObject.transform, "CancelReach"));
            roundResultConfirmButton = CreateButton(inputObject.transform, "RoundResultConfirm");
            reflection.SetPrivateField(controller, "roundResultConfirmButton", roundResultConfirmButton);
            return controller;
        }

        private static Component CreateCommandRouter(
            ReflectionTestAccess reflection,
            Transform parent,
            object gameFlow,
            Component inputController)
        {
            GameObject commandObject = CreateChild(parent, "CommandRouterSupport");
            Component controller = commandObject.AddComponent(reflection.RequireType(CommandRouterTypeName));
            reflection.SetPrivateField(controller, "gameFlow", gameFlow);
            reflection.SetPrivateField(controller, "inputController", inputController);
            return controller;
        }

        private static Component CreateWinDecisionController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject controllerObject = CreateChild(parent, "WinDecisionSupport");
            GameObject root = CreateChild(controllerObject.transform, "Root");
            Component label = CreateText(reflection, root.transform, "Label");
            Component controller = controllerObject.AddComponent(reflection.RequireType(WinDecisionControllerTypeName));
            reflection.SetPrivateField(controller, "winDecisionRoot", root);
            reflection.SetPrivateField(controller, "winButtonLabel", label);
            return controller;
        }

        private static Component CreateReachDecisionController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject controllerObject = CreateChild(parent, "ReachDecisionSupport");
            GameObject decisionRoot = CreateChild(controllerObject.transform, "DecisionRoot");
            GameObject cancelRoot = CreateChild(controllerObject.transform, "CancelRoot");
            Component controller = controllerObject.AddComponent(reflection.RequireType(ReachDecisionControllerTypeName));
            reflection.SetPrivateField(controller, "reachDecisionRoot", decisionRoot);
            reflection.SetPrivateField(controller, "reachCancelRoot", cancelRoot);
            return controller;
        }

        private static Component CreateLogPreviewController(
            ReflectionTestAccess reflection,
            Transform parent)
        {
            GameObject controllerObject = CreateChild(parent, "LogPreviewSupport");
            Component controller = controllerObject.AddComponent(reflection.RequireType(LogPreviewControllerTypeName));
            reflection.SetPrivateField(controller, "recentLogText", CreateText(reflection, controllerObject.transform, "RecentLog"));
            return controller;
        }

        private static Component CreateSingleTextController(
            ReflectionTestAccess reflection,
            Transform parent,
            string controllerTypeName,
            string textFieldName)
        {
            GameObject controllerObject = CreateChild(parent, controllerTypeName);
            Component text = CreateText(reflection, controllerObject.transform, "Text");
            Component controller = controllerObject.AddComponent(reflection.RequireType(controllerTypeName));
            reflection.SetPrivateField(controller, textFieldName, text);
            return controller;
        }

        private void ClearWall()
        {
            object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
            IList tiles = (IList)session.Reflection.GetPrivateField(wall, "tiles");
            tiles.Clear();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            return CreateChild(parent, name).AddComponent<Button>();
        }

        private static Toggle CreateToggle(Transform parent, string name)
        {
            return CreateChild(parent, name).AddComponent<Toggle>();
        }

        private static Component CreateText(
            ReflectionTestAccess reflection,
            Transform parent,
            string name)
        {
            return CreateChild(parent, name)
                .AddComponent(reflection.RequireType(TextMeshProUguiTypeName));
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject;
        }
    }
}
