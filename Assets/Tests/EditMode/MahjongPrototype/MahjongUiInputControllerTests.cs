using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Features.UiInput;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using MahjongPrototype.Tests.TestSupport.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        public void SetGameplayInputInteractable_ControlsOnlyGameplayButtons()
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
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.RetryInteractable, Is.True);
                Assert.That(driver.CancelReachInteractable, Is.True);
                Assert.That(driver.RoundResultConfirmInteractable, Is.True);
            }
        }

        [Test]
        public void SetGameplayInputInteractable_PreservesTargetTileTextAndSelection()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerTargetTilePreservationTest"))
            {
                driver.TargetTileText = "5m";
                driver.SetTargetTileSelection(1, 2);

                Assert.That(driver.TargetTileText, Is.EqualTo("5m"));
                Assert.That(driver.TargetTileSelectionAnchorPosition, Is.EqualTo(1));
                Assert.That(driver.TargetTileSelectionFocusPosition, Is.EqualTo(2));

                driver.SetGameplayInputInteractable(false);

                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.TargetTileText, Is.EqualTo("5m"));
                Assert.That(driver.TargetTileSelectionAnchorPosition, Is.EqualTo(1));
                Assert.That(driver.TargetTileSelectionFocusPosition, Is.EqualTo(2));
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

        [Test]
        public void ReactionResponseBindings_CaptureTheOriginalRequestIdentity_ForRonAndPass()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("ReactionInputBindingTest"))
            {
                long actualRequestId = 0;
                int actualWindowId = 0;
                string actualKind = null;
                int? actualChiOptionId = -1;
                EventInfo eventInfo = driver.Controller.GetType().GetEvent(
                    "ReactionResponseRequested");
                Assert.That(eventInfo, Is.Not.Null);
                eventInfo.AddEventHandler(
                    driver.Controller,
                    CreateReactionInputHandler(
                        eventInfo.EventHandlerType,
                        (requestId, windowId, kind, chiOptionId) =>
                        {
                            actualRequestId = requestId;
                            actualWindowId = windowId;
                            actualKind = kind;
                            actualChiOptionId = chiOptionId;
                        }));

                driver.SubscribeAllRequestEvents();
                driver.Reflection.Invoke(
                    driver.Controller,
                    "SetReactionResponseBindings",
                    801L,
                    41,
                    true,
                    false,
                    false);
                driver.EnableController();

                driver.ClickWin();
                Assert.That(actualRequestId, Is.EqualTo(801));
                Assert.That(actualWindowId, Is.EqualTo(41));
                Assert.That(actualKind, Is.EqualTo("Ron"));
                Assert.That(actualChiOptionId, Is.Null);
                Assert.That(driver.WinCount, Is.EqualTo(0));

                driver.ClickDeclineWin();
                Assert.That(actualRequestId, Is.EqualTo(801));
                Assert.That(actualWindowId, Is.EqualTo(41));
                Assert.That(actualKind, Is.EqualTo("Pass"));
                Assert.That(actualChiOptionId, Is.Null);
                Assert.That(driver.DeclineWinCount, Is.EqualTo(0));
            }
        }

        private static Delegate CreateReactionInputHandler(
            Type handlerType,
            Action<long, int, string, int?> recorder)
        {
            ParameterInfo[] parameters = handlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression requestId = Expression.Parameter(
                parameters[0].ParameterType,
                "requestId");
            ParameterExpression windowId = Expression.Parameter(
                parameters[1].ParameterType,
                "windowId");
            ParameterExpression kind = Expression.Parameter(
                parameters[2].ParameterType,
                "kind");
            ParameterExpression chiOptionId = Expression.Parameter(
                parameters[3].ParameterType,
                "chiOptionId");
            MethodInfo record = typeof(MahjongUiInputControllerTests).GetMethod(
                nameof(RecordReactionInput),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodCallExpression body = Expression.Call(
                record,
                Expression.Constant(recorder),
                requestId,
                windowId,
                Expression.Convert(kind, typeof(object)),
                chiOptionId);
            return Expression.Lambda(
                handlerType,
                body,
                requestId,
                windowId,
                kind,
                chiOptionId).Compile();
        }

        private static void RecordReactionInput(
            Action<long, int, string, int?> recorder,
            long requestId,
            int windowId,
            object kind,
            int? chiOptionId)
        {
            recorder(requestId, windowId, kind.ToString(), chiOptionId);
        }
    }

    public sealed class MahjongMeldCallDecisionControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongPonDecisionController, Assembly-CSharp";
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
        private const string InputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string EventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";
        private const string ChiOptionTypeName =
            "MahjongPrototype.Domain.ChiOption, Assembly-CSharp";
        private const string ChiOptionViewTypeName =
            "MahjongPrototype.UI.MahjongChiOptionView, Assembly-CSharp";
        private const string SelfKanCandidateTypeName =
            "MahjongPrototype.Domain.SelfKanCandidate, Assembly-CSharp";
        private const string SelfKanKindTypeName =
            "MahjongPrototype.Domain.SelfKanKind, Assembly-CSharp";
        private const string SelfKanTileLocationTypeName =
            "MahjongPrototype.Domain.SelfKanTileLocation, Assembly-CSharp";
        private const string SelfKanDecisionRequestTypeName =
            "MahjongPrototype.Domain.SelfKanDecisionRequest, Assembly-CSharp";
        private const string TileSpriteCatalogTypeName =
            "MahjongPrototype.UI.MahjongTileSpriteCatalog, Assembly-CSharp";
        private const string TileSpriteViewTypeName =
            "MahjongPrototype.UI.MahjongTileSpriteView, Assembly-CSharp";
        private const string TmpTextTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        [Test]
        public void SetMeldCallDecision_ShowsPonAndEveryChiOption_AndRoutesTheSelectedOptionId()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("MeldCallDecisionTestRoot");
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "MeldCallDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);
                ConfigureChiTileImages(
                    reflection,
                    dataFactory,
                    controller,
                    ponButton,
                    owner,
                    "3m",
                    "4m",
                    "5m",
                    "6m");

                string requestedKind = null;
                int requestedOptionId = 0;
                EventInfo eventInfo = inputController.GetType().GetEvent("MeldCallRequested");
                Assert.That(eventInfo, Is.Not.Null);
                Delegate handler = CreateMeldCallHandler(
                    eventInfo.EventHandlerType,
                    (kind, optionId) =>
                    {
                        requestedKind = kind;
                        requestedOptionId = optionId;
                    });
                eventInfo.AddEventHandler(inputController, handler);

                object calledTile = dataFactory.CreateTile("5m");
                IList options = CreateChiOptions(reflection, dataFactory, calledTile);
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);

                Assert.That(decisionRoot.activeSelf, Is.True);
                Assert.That(ponButton.gameObject.activeSelf, Is.False);
                Assert.That(declineButton.gameObject.activeSelf, Is.True);
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, declineButton),
                    Is.EqualTo("スキップ"));

                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    true,
                    options,
                    calledTile);
                Assert.That(ponButton.gameObject.activeSelf, Is.True);
                Button firstChiButton = FindChiOptionButton(
                    decisionRoot.transform,
                    "ChiOption_3");
                Button secondChiButton = FindChiOptionButton(
                    decisionRoot.transform,
                    "ChiOption_4");
                Assert.That(firstChiButton, Is.Not.Null);
                Assert.That(secondChiButton, Is.Not.Null);
                Transform chiDecisionRoot = decisionRoot.transform.Find("ChiDecisionRoot");
                Assert.That(chiDecisionRoot, Is.Not.Null);
                Assert.That(chiDecisionRoot.gameObject.activeSelf, Is.True);
                Component headingLabel = chiDecisionRoot.Find("ChiHeading")
                    .GetComponent(tmpTextType);
                Assert.That(
                    reflection.GetProperty(headingLabel, "text"),
                    Is.EqualTo("チー"));
                Assert.That(
                    CountText(
                        reflection,
                        decisionRoot.transform,
                        tmpTextType,
                        "チー"),
                    Is.EqualTo(1));
                Assert.That(
                    firstChiButton.GetComponentsInChildren(tmpTextType, true).Length,
                    Is.Zero);
                Assert.That(
                    secondChiButton.GetComponentsInChildren(tmpTextType, true).Length,
                    Is.Zero);
                Assert.That(firstChiButton.transform.parent.name, Is.EqualTo("ChiOptions"));
                Assert.That(secondChiButton.transform.parent.name, Is.EqualTo("ChiOptions"));
                AssertChiTileSprites(firstChiButton, "3m", "4m", "5m");
                AssertChiTileSprites(secondChiButton, "4m", "5m", "6m");

                secondChiButton.onClick.Invoke();

                Assert.That(requestedKind, Is.EqualTo("Chi"));
                Assert.That(requestedOptionId, Is.EqualTo(4));

                IList ankanCandidates = (IList)Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(calledTile.GetType()));
                ankanCandidates.Add(dataFactory.CreateTile("P"));
                ankanCandidates.Add(dataFactory.CreateTile("C"));
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    true,
                    null,
                    ankanCandidates,
                    calledTile);

                Button daiminkanButton = FindButton(decisionRoot.transform, "Daiminkan");
                Assert.That(daiminkanButton, Is.Not.Null);
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, daiminkanButton),
                    Is.EqualTo("カン"));
                Assert.That(
                    decisionRoot.transform.Find("ChiDecisionRoot").gameObject.activeSelf,
                    Is.False);
                daiminkanButton.onClick.Invoke();
                Assert.That(requestedKind, Is.EqualTo("Kan"));
                Assert.That(requestedOptionId, Is.EqualTo(0));

                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    false,
                    null,
                    ankanCandidates,
                    null);
                Assert.That(declineButton.gameObject.activeSelf, Is.False);
                Button redDragonAnkanButton = FindButton(decisionRoot.transform, "Ankan_33");
                Assert.That(redDragonAnkanButton, Is.Not.Null);
                redDragonAnkanButton.onClick.Invoke();
                Assert.That(requestedKind, Is.EqualTo("Kan"));
                Assert.That(requestedOptionId, Is.EqualTo(33));

                // When ron shares a request with meld calls, the win panel
                // owns Pass. The meld panel keeps the meld choices but must
                // not expose a second pass button.
                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    true,
                    false,
                    options,
                    calledTile,
                    false);
                Assert.That(decisionRoot.activeSelf, Is.True);
                Assert.That(ponButton.gameObject.activeSelf, Is.True);
                Assert.That(declineButton.gameObject.activeSelf, Is.False);

                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    true,
                    false,
                    options,
                    calledTile,
                    true);
                Assert.That(declineButton.gameObject.activeSelf, Is.True);
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, declineButton),
                    Is.EqualTo("スキップ"));

                reflection.Invoke(controller, "SetMeldCallDecision", false, null, null);
                Assert.That(decisionRoot.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                owner.Dispose();
            }
        }

        [TestCase(1)]
        [TestCase(3)]
        public void DedicatedChiOptionList_CreatesOneClickableViewPerOption_AndClearsOnRefresh(
            int optionCount)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("DedicatedChiOptionListTestRoot");
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "MeldCallDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);
                string requestedKind = null;
                int requestedOptionId = -1;
                EventInfo eventInfo = inputController.GetType().GetEvent("MeldCallRequested");
                Assert.That(eventInfo, Is.Not.Null);
                eventInfo.AddEventHandler(
                    inputController,
                    CreateMeldCallHandler(
                        eventInfo.EventHandlerType,
                        (kind, optionId) =>
                        {
                            requestedKind = kind;
                            requestedOptionId = optionId;
                        }));
                ConfigureChiTileImages(
                    reflection,
                    dataFactory,
                    controller,
                    ponButton,
                    owner,
                    "3m",
                    "4m",
                    "5m",
                    "6m",
                    "7m");

                object calledTile = dataFactory.CreateTile("5m");
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    CreateChiOptions(
                        reflection,
                        dataFactory,
                        calledTile,
                        optionCount),
                    calledTile);

                Transform chiDecisionRoot = decisionRoot.transform.Find("ChiDecisionRoot");
                Transform optionsContainer = chiDecisionRoot.Find("ChiOptions");
                Assert.That(chiDecisionRoot.gameObject.activeSelf, Is.True);
                Assert.That(optionsContainer.childCount, Is.EqualTo(optionCount));
                for (int i = 0; i < optionCount; i++)
                {
                    Transform option = optionsContainer.Find($"ChiOption_{i + 3}");
                    Assert.That(option, Is.Not.Null);
                    Assert.That(option.GetComponent<Button>(), Is.Not.Null);
                    Assert.That(
                        option.GetComponentsInChildren(
                            reflection.RequireType(TileSpriteViewTypeName),
                            true).Length,
                        Is.EqualTo(3));
                    Assert.That(
                        option.GetComponentsInChildren(tmpTextType, true).Length,
                        Is.Zero);
                    option.GetComponent<Button>().onClick.Invoke();
                    Assert.That(requestedKind, Is.EqualTo("Chi"));
                    Assert.That(requestedOptionId, Is.EqualTo(i + 3));
                }

                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    null,
                    null);

                Assert.That(chiDecisionRoot.gameObject.activeSelf, Is.False);
                Assert.That(optionsContainer.childCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                owner.Dispose();
            }
        }

        [Test]
        public void SelfKanDecisionButtons_UseOnlyRequestBoundEvents_AndClearStaleChoices()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("SelfKanDecisionInputTestRoot");
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "SelfKanDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");

                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);

                // Reproduce the static legacy listener that the production input
                // controller registers on this shared decline button.
                declineButton.onClick.AddListener(
                    () => reflection.Invoke(inputController, "HandleDeclinePonClicked"));

                int legacyEventCount = 0;
                foreach (string eventName in new[]
                {
                    "MeldCallRequested",
                    "DeclineMeldCallsRequested",
                    "SelfKanRequested",
                    "DeclineSelfKanRequested"
                })
                {
                    EventInfo legacyEvent = inputController.GetType().GetEvent(eventName);
                    Assert.That(legacyEvent, Is.Not.Null);
                    legacyEvent.AddEventHandler(
                        inputController,
                        CreateIgnoringHandler(
                            legacyEvent.EventHandlerType,
                            () => legacyEventCount++));
                }

                List<long> requestIds = new List<long>();
                List<bool> acceptedValues = new List<bool>();
                List<int> optionIds = new List<int>();
                inputController.GetType()
                    .GetEvent("SelfKanDecisionResponseRequested")
                    .AddEventHandler(
                        inputController,
                        new Action<long, bool, int>((requestId, accepted, optionId) =>
                        {
                            requestIds.Add(requestId);
                            acceptedValues.Add(accepted);
                            optionIds.Add(optionId);
                        }));

                reflection.Invoke(
                    controller,
                    "SetSelfKanDecision",
                    501L,
                    CreateSelfKanDecisionRequest(
                        reflection,
                        dataFactory,
                        includeKakan: true));

                Button ankanButton = FindButton(decisionRoot.transform, "Ankan_0");
                Button kakanButton = FindButton(decisionRoot.transform, "Kakan_1");
                Assert.That(ankanButton, Is.Not.Null);
                Assert.That(kakanButton, Is.Not.Null);
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, ankanButton),
                    Is.EqualTo("カン P"));
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, kakanButton),
                    Is.EqualTo("カン C"));
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, declineButton),
                    Is.EqualTo("スキップ"));

                ankanButton.onClick.Invoke();
                kakanButton.onClick.Invoke();
                declineButton.onClick.Invoke();

                Assert.That(requestIds, Is.EqualTo(new[] { 501L, 501L, 501L }));
                Assert.That(acceptedValues, Is.EqualTo(new[] { true, true, false }));
                Assert.That(optionIds, Is.EqualTo(new[] { 0, 1, -1 }));
                Assert.That(legacyEventCount, Is.Zero);

                reflection.Invoke(
                    controller,
                    "SetSelfKanDecision",
                    502L,
                    CreateSelfKanDecisionRequest(
                        reflection,
                        dataFactory,
                        includeKakan: false));

                Assert.That(ankanButton == null, Is.True);
                Assert.That(kakanButton == null, Is.True);
                Button currentButton = FindButton(decisionRoot.transform, "Ankan_0");
                Assert.That(currentButton, Is.Not.Null);
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, currentButton),
                    Is.EqualTo("カン"));
                currentButton.onClick.Invoke();
                Assert.That(requestIds.Last(), Is.EqualTo(502L));
                Assert.That(optionIds.Last(), Is.Zero);

                reflection.Invoke(controller, "ClearReactionMeldCallDecision");
                Assert.That(currentButton == null, Is.True);
                Assert.That(decisionRoot.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SelfKanDecisionDeclinedNotification_ClosesTheDecisionUi()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("SelfKanDeclinedUiRefreshTestRoot");
            root.SetActive(false);
            try
            {
                Component notifier = root.AddComponent(
                    reflection.RequireType(EventNotifierTypeName));
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Component uiManager = root.AddComponent(
                    reflection.RequireType(UiManagerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "SelfKanDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);
                reflection.SetPrivateField(uiManager, "eventNotifier", notifier);
                reflection.SetPrivateField(uiManager, "ponDecisionController", controller);
                reflection.Invoke(uiManager, "SubscribeNotifications");

                reflection.Invoke(
                    controller,
                    "SetSelfKanDecision",
                    601L,
                    CreateSelfKanDecisionRequest(
                        reflection,
                        dataFactory,
                        includeKakan: false));
                Button staleButton = FindButton(decisionRoot.transform, "Ankan_0");
                Assert.That(decisionRoot.activeSelf, Is.True);
                Assert.That(staleButton, Is.Not.Null);

                reflection.Invoke(
                    notifier,
                    "NotifySelfKanDecisionDeclined",
                    dataFactory.ParseSeat("East"),
                    7);

                Assert.That(decisionRoot.activeSelf, Is.False);
                Assert.That(staleButton == null, Is.True);
                reflection.Invoke(uiManager, "UnsubscribeNotifications");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProductionScene_ChiOptionPanel_GrowsWithoutOverlap_AndUsesExistingInputPath()
        {
            const string scenePath = "Assets/Scenes/Mahjong Prototype.unity";
            const string optionPrefabPath = "Assets/Prefab/Mahjong Chi Option.prefab";
            const string tilePrefabPath = "Assets/Prefab/Chi Prefab.prefab";
            const string catalogPath =
                "Assets/Scripts/MahjongPrototype/ScriptableObjects/MahjongTileSpriteCatalog.asset";

            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            Component controller = null;
            try
            {
                Type controllerType = reflection.RequireType(ControllerTypeName);
                controller = Resources.FindObjectsOfTypeAll(controllerType)
                    .Cast<Component>()
                    .Single(candidate => candidate.gameObject.scene == scene);
                Component inputController = (Component)reflection.GetPrivateField(
                    controller,
                    "inputController");
                Component decisionLabel = (Component)reflection.GetPrivateField(
                    controller,
                    "decisionLabel");
                Button ponButton = (Button)reflection.GetPrivateField(
                    controller,
                    "ponButton");
                Button declineButton = (Button)reflection.GetPrivateField(
                    controller,
                    "declineButton");
                reflection.Invoke(inputController, "OnDisable");
                reflection.Invoke(inputController, "OnEnable");
                GameObject decisionRoot = (GameObject)reflection.GetPrivateField(
                    controller,
                    "ponDecisionRoot");
                GameObject chiDecisionRoot = (GameObject)reflection.GetPrivateField(
                    controller,
                    "chiDecisionRoot");
                Transform optionsContainer = (Transform)reflection.GetPrivateField(
                    controller,
                    "chiOptionsContainer");
                Component heading = (Component)reflection.GetPrivateField(
                    controller,
                    "chiHeadingLabel");
                Component optionViewPrefab = (Component)reflection.GetPrivateField(
                    controller,
                    "chiOptionViewPrefab");
                ScriptableObject catalog = (ScriptableObject)reflection.GetPrivateField(
                    controller,
                    "chiTileSpriteCatalog");
                Component tileViewPrefab = (Component)reflection.GetPrivateField(
                    controller,
                    "chiTileViewPrefab");

                Assert.That(decisionRoot, Is.Not.Null);
                Assert.That(
                    decisionLabel,
                    Is.Null,
                    "The optional heading must not reuse either button's text component.");
                Assert.That(
                    reflection.GetPrivateField(inputController, "ponButton"),
                    Is.SameAs(ponButton));
                Assert.That(
                    reflection.GetPrivateField(inputController, "declinePonButton"),
                    Is.SameAs(declineButton));
                Assert.That(ponButton.onClick.GetPersistentEventCount(), Is.Zero);
                Assert.That(declineButton.onClick.GetPersistentEventCount(), Is.Zero);
                Assert.That(chiDecisionRoot, Is.Not.Null);
                Assert.That(optionsContainer, Is.Not.Null);
                Assert.That(heading, Is.Not.Null);
                Assert.That(optionViewPrefab, Is.Not.Null);
                Assert.That(catalog, Is.Not.Null);
                Assert.That(tileViewPrefab, Is.Not.Null);
                Assert.That(
                    AssetDatabase.GetAssetPath(optionViewPrefab),
                    Is.EqualTo(optionPrefabPath));
                Assert.That(
                    AssetDatabase.GetAssetPath(tileViewPrefab),
                    Is.EqualTo(tilePrefabPath));
                Assert.That(
                    AssetDatabase.GetAssetPath(catalog),
                    Is.EqualTo(catalogPath));
                Assert.That(
                    optionViewPrefab.GetComponent<Button>(),
                    Is.Not.Null);
                Assert.That(
                    optionViewPrefab.transform.Find("Tiles"),
                    Is.Not.Null);
                Assert.That(
                    optionViewPrefab.GetComponentsInChildren(
                        reflection.RequireType(TmpTextTypeName),
                        true).Length,
                    Is.Zero);
                Assert.That(
                    chiDecisionRoot.GetComponent<VerticalLayoutGroup>(),
                    Is.Not.Null);
                Assert.That(
                    chiDecisionRoot.GetComponent<ContentSizeFitter>(),
                    Is.Not.Null);
                Assert.That(
                    optionsContainer.GetComponent<HorizontalLayoutGroup>(),
                    Is.Not.Null);
                Assert.That(
                    decisionRoot.GetComponent<ContentSizeFitter>(),
                    Is.Not.Null);
                Color panelColor = chiDecisionRoot.GetComponent<Image>().color;
                Assert.That(panelColor.b, Is.GreaterThan(panelColor.r));
                Assert.That(panelColor.a, Is.GreaterThan(0.8f));

                string requestedKind = null;
                int requestedOptionId = -1;
                EventInfo meldCallEvent = inputController.GetType().GetEvent(
                    "MeldCallRequested");
                meldCallEvent.AddEventHandler(
                    inputController,
                    CreateMeldCallHandler(
                        meldCallEvent.EventHandlerType,
                        (kind, optionId) =>
                        {
                            requestedKind = kind;
                            requestedOptionId = optionId;
                        }));
                int skipCount = 0;
                inputController.GetType()
                    .GetEvent("DeclineMeldCallsRequested")
                    .AddEventHandler(inputController, new Action(() => skipCount++));

                object calledTile = dataFactory.CreateTile("5m");
                RectTransform chiRect = (RectTransform)chiDecisionRoot.transform;
                RectTransform decisionRect = (RectTransform)decisionRoot.transform;
                RectTransform canvasRect = (RectTransform)decisionRect.parent;
                Assert.That(canvasRect, Is.SameAs(controller.transform));
                Assert.That(decisionRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(decisionRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(decisionRect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(decisionRect.anchoredPosition.x, Is.LessThanOrEqualTo(0f));
                Assert.That(decisionRect.anchoredPosition.y, Is.GreaterThanOrEqualTo(0f));
                EventSystem eventSystem = Resources.FindObjectsOfTypeAll<EventSystem>()
                    .Single(candidate => candidate.gameObject.scene == scene);
                float previousChiWidth = 0f;
                float previousLeftEdge = float.PositiveInfinity;
                float fixedSkipRightEdge = 0f;
                for (int optionCount = 1; optionCount <= 3; optionCount++)
                {
                    reflection.Invoke(
                        controller,
                        "SetReactionMeldCallDecision",
                        false,
                        false,
                        CreateChiOptions(
                            reflection,
                            dataFactory,
                            calledTile,
                            optionCount),
                        calledTile,
                        true);

                    Assert.That(chiDecisionRoot.activeSelf, Is.True);
                    Assert.That(optionsContainer.childCount, Is.EqualTo(optionCount));
                    Assert.That(
                        reflection.GetProperty(heading, "text"),
                        Is.EqualTo("チー"));
                    Assert.That(
                        CountText(
                            reflection,
                            chiDecisionRoot.transform,
                            reflection.RequireType(TmpTextTypeName),
                            "チー"),
                        Is.EqualTo(1));
                    Assert.That(
                        ButtonLabel(reflection, reflection.RequireType(TmpTextTypeName), declineButton),
                        Is.EqualTo("スキップ"));
                    Assert.That(chiRect.rect.width, Is.GreaterThan(previousChiWidth));
                    previousChiWidth = chiRect.rect.width;

                    Bounds decisionBounds = CalculateRectBounds(canvasRect, decisionRect);
                    float skipRightEdge = AssertRightAnchoredDecisionLayout(
                        canvasRect,
                        decisionRect,
                        declineButton);
                    if (optionCount == 1)
                        fixedSkipRightEdge = skipRightEdge;
                    else
                        Assert.That(skipRightEdge, Is.EqualTo(fixedSkipRightEdge).Within(0.05f));
                    Assert.That(decisionBounds.min.x, Is.LessThan(previousLeftEdge));
                    previousLeftEdge = decisionBounds.min.x;

                    AssertProductionChiLayout(
                        reflection,
                        decisionRect,
                        chiRect,
                        optionsContainer,
                        declineButton,
                        optionCount);

                    for (int i = 0; i < optionsContainer.childCount; i++)
                    {
                        Transform option = optionsContainer.GetChild(i);
                        Assert.That(
                            option.GetComponentsInChildren(
                                reflection.RequireType(TileSpriteViewTypeName),
                                true).Length,
                            Is.EqualTo(3));
                        Component firstTile = option.GetComponentsInChildren(
                            reflection.RequireType(TileSpriteViewTypeName),
                            true)[0];
                        PointerEventData pointer = new PointerEventData(eventSystem)
                        {
                            button = PointerEventData.InputButton.Left
                        };
                        Assert.That(
                            ExecuteEvents.ExecuteHierarchy(
                                firstTile.gameObject,
                                pointer,
                                ExecuteEvents.pointerClickHandler),
                            Is.Not.Null);
                        Assert.That(requestedKind, Is.EqualTo("Chi"));
                        Assert.That(requestedOptionId, Is.EqualTo(i + 3));
                    }

                    AssertProductionChiTileSprites(
                        reflection,
                        dataFactory,
                        catalog,
                        optionsContainer.GetChild(0),
                        "3m",
                        "4m",
                        "5m");
                    declineButton.onClick.Invoke();
                    Assert.That(skipCount, Is.EqualTo(optionCount));

                    reflection.Invoke(controller, "ClearReactionMeldCallDecision");
                    Assert.That(chiDecisionRoot.activeSelf, Is.False);
                    Assert.That(optionsContainer.childCount, Is.Zero);
                    Assert.That(decisionRoot.activeSelf, Is.False);
                }

                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    true,
                    false,
                    null,
                    calledTile,
                    true);
                Assert.That(chiDecisionRoot.activeSelf, Is.False);
                Assert.That(optionsContainer.childCount, Is.Zero);
                Assert.That(decisionRect.rect.width, Is.InRange(180f, 200f));
                Assert.That(
                    AssertRightAnchoredDecisionLayout(
                        canvasRect,
                        decisionRect,
                        declineButton),
                    Is.EqualTo(fixedSkipRightEdge).Within(0.05f));
                Assert.That(
                    CalculateRectBounds(decisionRect, (RectTransform)ponButton.transform).max.x,
                    Is.LessThan(CalculateRectBounds(
                        decisionRect,
                        (RectTransform)declineButton.transform).min.x));

                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    true,
                    false,
                    CreateChiOptions(reflection, dataFactory, calledTile, 3),
                    calledTile,
                    true);
                Assert.That(
                    AssertRightAnchoredDecisionLayout(
                        canvasRect,
                        decisionRect,
                        declineButton),
                    Is.EqualTo(fixedSkipRightEdge).Within(0.05f));
                Assert.That(
                    CalculateRectBounds(decisionRect, (RectTransform)ponButton.transform).max.x,
                    Is.LessThanOrEqualTo(CalculateRectBounds(
                        decisionRect,
                        chiRect).min.x + 0.05f));
                AssertProductionChiLayout(
                    reflection,
                    decisionRect,
                    chiRect,
                    optionsContainer,
                    declineButton,
                    3);

                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    false,
                    true,
                    null,
                    calledTile,
                    true);
                Button daiminkanButton = FindButton(decisionRoot.transform, "Daiminkan");
                Assert.That(daiminkanButton, Is.Not.Null);
                Assert.That(
                    ButtonLabel(
                        reflection,
                        reflection.RequireType(TmpTextTypeName),
                        daiminkanButton),
                    Is.EqualTo("カン"));
                Assert.That(
                    ButtonLabel(
                        reflection,
                        reflection.RequireType(TmpTextTypeName),
                        declineButton),
                    Is.EqualTo("スキップ"));
                Assert.That(
                    AssertRightAnchoredDecisionLayout(
                        canvasRect,
                        decisionRect,
                        declineButton),
                    Is.EqualTo(fixedSkipRightEdge).Within(0.05f));
                Assert.That(
                    CalculateRectBounds(
                        decisionRect,
                        (RectTransform)daiminkanButton.transform).max.x,
                    Is.LessThan(CalculateRectBounds(
                        decisionRect,
                        (RectTransform)declineButton.transform).min.x));

                reflection.Invoke(
                    controller,
                    "SetSelfKanDecision",
                    801L,
                    CreateSelfKanDecisionRequest(
                        reflection,
                        dataFactory,
                        includeKakan: true));
                Assert.That(
                    ButtonLabel(
                        reflection,
                        reflection.RequireType(TmpTextTypeName),
                        declineButton),
                    Is.EqualTo("スキップ"));
                Assert.That(
                    AssertRightAnchoredDecisionLayout(
                        canvasRect,
                        decisionRect,
                        declineButton),
                    Is.EqualTo(fixedSkipRightEdge).Within(0.05f));
                reflection.Invoke(controller, "ClearReactionMeldCallDecision");
                Assert.That(decisionRoot.activeSelf, Is.False);
            }
            finally
            {
                if (controller != null)
                    reflection.Invoke(controller, "ClearReactionMeldCallDecision");
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ProductionScene_DecisionButtons_UseTheChiVisualSystem_AndDynamicKanButtonsInheritIt()
        {
            const string scenePath = "Assets/Scenes/Mahjong Prototype.unity";
            const string optionPrefabPath = "Assets/Prefab/Mahjong Chi Option.prefab";

            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            Component meldController = null;
            try
            {
                GameObject chiOptionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(optionPrefabPath);
                Assert.That(chiOptionPrefab, Is.Not.Null);
                AssertDecisionButtonVisualStyle(
                    chiOptionPrefab.GetComponent<Button>(),
                    requiresReadableLabel: false);

                string[] decisionButtonNames =
                {
                    "PonButton",
                    "DeclinePonButton",
                    "WinButton",
                    "DeclineWinButton",
                    "ReachButton",
                    "DeclineReachButton",
                    "CancelButton"
                };
                for (int i = 0; i < decisionButtonNames.Length; i++)
                {
                    AssertDecisionButtonVisualStyle(
                        FindButtonInScene(scene, decisionButtonNames[i]));
                }

                Type controllerType = reflection.RequireType(ControllerTypeName);
                meldController = Resources.FindObjectsOfTypeAll(controllerType)
                    .Cast<Component>()
                    .Single(candidate => candidate.gameObject.scene == scene);
                GameObject decisionRoot = (GameObject)reflection.GetPrivateField(
                    meldController,
                    "ponDecisionRoot");
                object calledTile = dataFactory.CreateTile("5m");

                reflection.Invoke(
                    meldController,
                    "SetReactionMeldCallDecision",
                    false,
                    true,
                    null,
                    calledTile,
                    true);
                AssertDecisionButtonVisualStyle(
                    FindButton(decisionRoot.transform, "Daiminkan"));

                reflection.Invoke(
                    meldController,
                    "SetSelfKanDecision",
                    911L,
                    CreateSelfKanDecisionRequest(
                        reflection,
                        dataFactory,
                        includeKakan: true));
                AssertDecisionButtonVisualStyle(
                    FindButton(decisionRoot.transform, "Ankan_0"));
                AssertDecisionButtonVisualStyle(
                    FindButton(decisionRoot.transform, "Kakan_1"));
            }
            finally
            {
                if (meldController != null)
                    reflection.Invoke(meldController, "ClearReactionMeldCallDecision");
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ReactionMeldButtons_EmitTheRequestIdentityThatCreatedThem()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("ReactionMeldCallDecisionIdentityTestRoot");
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "MeldCallDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);
                ConfigureChiTileImages(
                    reflection,
                    dataFactory,
                    controller,
                    ponButton,
                    owner,
                    "3m",
                    "4m",
                    "5m",
                    "6m");

                long actualRequestId = 0;
                int actualWindowId = 0;
                string actualKind = null;
                int? actualChiOptionId = null;
                EventInfo eventInfo = inputController.GetType().GetEvent(
                    "ReactionResponseRequested");
                Assert.That(eventInfo, Is.Not.Null);
                eventInfo.AddEventHandler(
                    inputController,
                    CreateReactionResponseHandler(
                        eventInfo.EventHandlerType,
                        (requestId, windowId, kind, chiOptionId) =>
                        {
                            actualRequestId = requestId;
                            actualWindowId = windowId;
                            actualKind = kind;
                            actualChiOptionId = chiOptionId;
                        }));

                object calledTile = dataFactory.CreateTile("5m");
                IList options = CreateChiOptions(reflection, dataFactory, calledTile);
                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    901L,
                    71,
                    false,
                    true,
                    options,
                    calledTile,
                    true);

                Button daiminkanButton = FindButton(decisionRoot.transform, "Daiminkan");
                Assert.That(daiminkanButton, Is.Not.Null);
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, daiminkanButton),
                    Is.EqualTo("カン"));
                Assert.That(
                    ButtonLabel(reflection, tmpTextType, declineButton),
                    Is.EqualTo("スキップ"));

                FindChiOptionButton(
                    decisionRoot.transform,
                    "ChiOption_4").onClick.Invoke();
                Assert.That(actualRequestId, Is.EqualTo(901));
                Assert.That(actualWindowId, Is.EqualTo(71));
                Assert.That(actualKind, Is.EqualTo("Chi"));
                Assert.That(actualChiOptionId, Is.EqualTo(4));

                daiminkanButton.onClick.Invoke();
                Assert.That(actualRequestId, Is.EqualTo(901));
                Assert.That(actualWindowId, Is.EqualTo(71));
                Assert.That(actualKind, Is.EqualTo("Daiminkan"));
                Assert.That(actualChiOptionId, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                owner.Dispose();
            }
        }

        [Test]
        public void ClearReactionMeldCallDecision_RemovesDynamicButtonsAndRequestIdentity()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("ReactionMeldCallDecisionCleanupTestRoot");
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "MeldCallDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "繝昴Φ");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "繝代せ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);
                ConfigureChiTileImages(
                    reflection,
                    dataFactory,
                    controller,
                    ponButton,
                    owner,
                    "3m",
                    "4m",
                    "5m",
                    "6m");

                object calledTile = dataFactory.CreateTile("5m");
                reflection.Invoke(
                    controller,
                    "SetReactionMeldCallDecision",
                    901L,
                    71,
                    false,
                    true,
                    CreateChiOptions(reflection, dataFactory, calledTile),
                    calledTile,
                    true);
                Assert.That(
                    FindChiOptionButton(decisionRoot.transform, "ChiOption_4"),
                    Is.Not.Null);
                Assert.That(FindButton(decisionRoot.transform, "Daiminkan"), Is.Not.Null);
                Assert.That(
                    decisionRoot.GetComponentsInChildren(
                        reflection.RequireType(TileSpriteViewTypeName),
                        true).Length,
                    Is.EqualTo(6));

                reflection.Invoke(controller, "ClearReactionMeldCallDecision");

                Assert.That(decisionRoot.activeSelf, Is.False);
                Assert.That(
                    FindChiOptionButton(decisionRoot.transform, "ChiOption_4"),
                    Is.Null);
                Assert.That(FindButton(decisionRoot.transform, "Daiminkan"), Is.Null);
                Assert.That(
                    decisionRoot.GetComponentsInChildren(
                        reflection.RequireType(TileSpriteViewTypeName),
                        true).Length,
                    Is.Zero);
                Assert.That(
                    (bool)reflection.GetPrivateField(controller, "hasReactionRequest"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                owner.Dispose();
            }
        }

        [Test]
        public void MissingChiUiConfiguration_WarnsOnceAndKeepsConfiguredCandidateUsable()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("MissingChiImageConfigurationTestRoot");
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "MeldCallDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);

                string requestedKind = null;
                int requestedOptionId = 0;
                EventInfo eventInfo = inputController.GetType().GetEvent("MeldCallRequested");
                Assert.That(eventInfo, Is.Not.Null);
                eventInfo.AddEventHandler(
                    inputController,
                    CreateMeldCallHandler(
                        eventInfo.EventHandlerType,
                        (kind, optionId) =>
                        {
                            requestedKind = kind;
                            requestedOptionId = optionId;
                        }));

                object calledTile = dataFactory.CreateTile("5m");
                IList options = CreateChiOptions(reflection, dataFactory, calledTile);
                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongPonDecisionController: ChiDecisionRoot is not assigned.");
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);

                ConfigureChiUiStructure(
                    reflection,
                    controller,
                    decisionRoot.transform,
                    owner);
                object optionViewPrefab = reflection.GetPrivateField(
                    controller,
                    "chiOptionViewPrefab");
                reflection.SetPrivateField(controller, "chiOptionViewPrefab", null);
                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongPonDecisionController: ChiOptionViewPrefab is not assigned.");
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);
                reflection.SetPrivateField(
                    controller,
                    "chiOptionViewPrefab",
                    optionViewPrefab);

                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongPonDecisionController: Chi tile sprite catalog is not assigned.");
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);

                // A refreshed candidate list does not repeat the same warning.
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);

                ScriptableObject emptyCatalog = ScriptableObject.CreateInstance(
                    reflection.RequireType(TileSpriteCatalogTypeName));
                owner.Register(emptyCatalog);
                reflection.SetPrivateField(controller, "chiTileSpriteCatalog", emptyCatalog);
                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongPonDecisionController: Chi tile view prefab is not assigned.");
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);

                Button secondChiButton = FindChiOptionButton(
                    decisionRoot.transform,
                    "ChiOption_4");
                Assert.That(secondChiButton, Is.Not.Null);
                secondChiButton.onClick.Invoke();
                Assert.That(requestedKind, Is.EqualTo("Chi"));
                Assert.That(requestedOptionId, Is.EqualTo(4));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                owner.Dispose();
            }
        }

        [Test]
        public void UnregisteredChiTileSprite_WarnsOnceAndSkipsOnlyThatImage()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("UnregisteredChiTileSpriteTestRoot");
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            root.SetActive(false);
            try
            {
                Component inputController = root.AddComponent(
                    reflection.RequireType(InputControllerTypeName));
                Component controller = root.AddComponent(
                    reflection.RequireType(ControllerTypeName));
                Type tmpTextType = reflection.RequireType(TmpTextTypeName);
                GameObject decisionRoot = new GameObject(
                    "MeldCallDecisionRoot",
                    typeof(RectTransform));
                decisionRoot.transform.SetParent(root.transform);
                Button ponButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "PonButton",
                    "ポン");
                Button declineButton = CreateButton(
                    reflection,
                    tmpTextType,
                    decisionRoot.transform,
                    "DeclineButton",
                    "スキップ");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);
                ConfigureChiTileImages(
                    reflection,
                    dataFactory,
                    controller,
                    ponButton,
                    owner,
                    "3m",
                    "4m",
                    "5m");

                object calledTile = dataFactory.CreateTile("5m");
                IList options = CreateChiOptions(reflection, dataFactory, calledTile);
                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongPonDecisionController: " +
                    "Chi tile sprite is not registered for 6m (TypeIndex=5).");
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);

                Button secondChiButton = FindChiOptionButton(
                    decisionRoot.transform,
                    "ChiOption_4");
                Assert.That(secondChiButton, Is.Not.Null);
                AssertChiTileSprites(secondChiButton, "4m", "5m");

                // Refreshing the same options does not repeat the TypeIndex warning.
                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    false,
                    options,
                    calledTile);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                owner.Dispose();
            }
        }

        [Test]
        public void TileSpriteCatalog_MapsTilesByTypeIndex()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            UnityObjectTestOwner owner = new UnityObjectTestOwner();
            try
            {
                Type catalogType = reflection.RequireType(TileSpriteCatalogTypeName);
                ScriptableObject catalog = ScriptableObject.CreateInstance(catalogType);
                owner.Register(catalog);
                Sprite sprite = CreateTestSprite(owner, "5m");
                SetCatalogEntries(
                    reflection,
                    dataFactory,
                    catalog,
                    new[] { "5m" },
                    new[] { sprite });

                MethodInfo tryGetSprite = catalogType.GetMethod("TryGetSprite");
                Assert.That(tryGetSprite, Is.Not.Null);
                object[] registeredArguments =
                {
                    dataFactory.CreateTile("5m"),
                    null
                };
                object[] missingArguments =
                {
                    dataFactory.CreateTile("6m"),
                    null
                };

                Assert.That((bool)tryGetSprite.Invoke(catalog, registeredArguments), Is.True);
                Assert.That(registeredArguments[1], Is.SameAs(sprite));
                Assert.That((bool)tryGetSprite.Invoke(catalog, missingArguments), Is.False);
                Assert.That(missingArguments[1], Is.Null);
            }
            finally
            {
                owner.Dispose();
            }
        }

        private static IList CreateChiOptions(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            object calledTile)
        {
            return CreateChiOptions(reflection, dataFactory, calledTile, 2);
        }

        private static object CreateSelfKanDecisionRequest(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            bool includeKakan)
        {
            Type candidateType = reflection.RequireType(SelfKanCandidateTypeName);
            IList candidates = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(candidateType));
            candidates.Add(reflection.CreateInstance(
                candidateType,
                Enum.Parse(reflection.RequireType(SelfKanKindTypeName), "Ankan"),
                dataFactory.ParseSeat("East"),
                dataFactory.CreateTile("P"),
                Enum.Parse(
                    reflection.RequireType(SelfKanTileLocationTypeName),
                    "DrawnTile"),
                7,
                -1,
                null));
            if (includeKakan)
            {
                candidates.Add(reflection.CreateInstance(
                    candidateType,
                    Enum.Parse(reflection.RequireType(SelfKanKindTypeName), "Kakan"),
                    dataFactory.ParseSeat("East"),
                    dataFactory.CreateTile("C"),
                    Enum.Parse(
                        reflection.RequireType(SelfKanTileLocationTypeName),
                        "Hand"),
                    7,
                    0,
                    null));
            }

            return reflection.CreateInstance(
                reflection.RequireType(SelfKanDecisionRequestTypeName),
                candidates);
        }

        private static IList CreateChiOptions(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            object calledTile,
            int optionCount)
        {
            Assert.That(optionCount, Is.InRange(1, 3));
            Type chiOptionType = reflection.RequireType(ChiOptionTypeName);
            IList options = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(chiOptionType));
            options.Add(reflection.CreateInstance(
                chiOptionType,
                3,
                calledTile,
                dataFactory.CreateTileArrayFromText("3m 4m"),
                dataFactory.CreateTileArrayFromText("3m 4m 5m")));
            if (optionCount == 1)
                return options;

            options.Add(reflection.CreateInstance(
                chiOptionType,
                4,
                calledTile,
                dataFactory.CreateTileArrayFromText("4m 6m"),
                dataFactory.CreateTileArrayFromText("4m 5m 6m")));
            if (optionCount == 2)
                return options;

            options.Add(reflection.CreateInstance(
                chiOptionType,
                5,
                calledTile,
                dataFactory.CreateTileArrayFromText("6m 7m"),
                dataFactory.CreateTileArrayFromText("5m 6m 7m")));
            return options;
        }

        private static void AssertProductionChiLayout(
            ReflectionTestAccess reflection,
            RectTransform decisionRect,
            RectTransform chiRect,
            Transform optionsContainer,
            Button declineButton,
            int expectedOptionCount)
        {
            const float tolerance = 0.05f;
            Bounds chiBounds = CalculateRectBounds(decisionRect, chiRect);
            Bounds declineBounds = CalculateRectBounds(
                decisionRect,
                (RectTransform)declineButton.transform);
            AssertBoundsInside(decisionRect.rect, chiBounds, tolerance);
            AssertBoundsInside(decisionRect.rect, declineBounds, tolerance);
            Assert.That(
                declineBounds.min.x,
                Is.GreaterThanOrEqualTo(chiBounds.max.x - tolerance),
                "Pass must follow the final chi panel width without overlap.");

            Bounds optionsBounds = CalculateRectBounds(
                chiRect,
                (RectTransform)optionsContainer);
            AssertBoundsInside(chiRect.rect, optionsBounds, tolerance);

            Bounds previousOptionBounds = default;
            for (int i = 0; i < expectedOptionCount; i++)
            {
                RectTransform optionRect = (RectTransform)optionsContainer.GetChild(i);
                Bounds optionBounds = CalculateRectBounds(
                    (RectTransform)optionsContainer,
                    optionRect);
                AssertBoundsInside(
                    ((RectTransform)optionsContainer).rect,
                    optionBounds,
                    tolerance);
                if (i > 0)
                {
                    Assert.That(
                        optionBounds.min.x,
                        Is.GreaterThanOrEqualTo(previousOptionBounds.max.x - tolerance),
                        "Chi candidates must not overlap.");
                }

                Component[] tileViews = optionRect.GetComponentsInChildren(
                    reflection.RequireType(TileSpriteViewTypeName),
                    true).Cast<Component>().ToArray();
                for (int tileIndex = 0; tileIndex < tileViews.Length; tileIndex++)
                {
                    Bounds tileBounds = CalculateRectBounds(
                        optionRect,
                        (RectTransform)tileViews[tileIndex].transform);
                    AssertBoundsInside(optionRect.rect, tileBounds, tolerance);
                }

                previousOptionBounds = optionBounds;
            }
        }

        private static float AssertRightAnchoredDecisionLayout(
            RectTransform canvasRect,
            RectTransform decisionRect,
            Button declineButton)
        {
            const float tolerance = 0.05f;
            Bounds decisionBounds = CalculateRectBounds(canvasRect, decisionRect);
            Bounds declineBounds = CalculateRectBounds(
                canvasRect,
                (RectTransform)declineButton.transform);
            AssertBoundsInside(canvasRect.rect, decisionBounds, tolerance);
            AssertBoundsInside(canvasRect.rect, declineBounds, tolerance);
            Assert.That(
                declineBounds.max.x,
                Is.EqualTo(decisionBounds.max.x).Within(tolerance),
                "The Inspector-configured skip button must remain at the decision UI's fixed right edge.");
            return declineBounds.max.x;
        }

        private static Bounds CalculateRectBounds(
            RectTransform relativeTo,
            RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 first = relativeTo.InverseTransformPoint(corners[0]);
            Bounds bounds = new Bounds(first, Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(relativeTo.InverseTransformPoint(corners[i]));

            return bounds;
        }

        private static void AssertBoundsInside(
            Rect container,
            Bounds content,
            float tolerance)
        {
            Assert.That(content.min.x, Is.GreaterThanOrEqualTo(container.xMin - tolerance));
            Assert.That(content.max.x, Is.LessThanOrEqualTo(container.xMax + tolerance));
            Assert.That(content.min.y, Is.GreaterThanOrEqualTo(container.yMin - tolerance));
            Assert.That(content.max.y, Is.LessThanOrEqualTo(container.yMax + tolerance));
        }

        private static void AssertProductionChiTileSprites(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            ScriptableObject catalog,
            Transform option,
            params string[] tileCodes)
        {
            Type tileViewType = reflection.RequireType(TileSpriteViewTypeName);
            Component[] tileViews = option.GetComponentsInChildren(tileViewType, true)
                .Cast<Component>()
                .ToArray();
            Assert.That(tileViews.Length, Is.EqualTo(tileCodes.Length));

            MethodInfo tryGetSprite = catalog.GetType().GetMethod("TryGetSprite");
            Assert.That(tryGetSprite, Is.Not.Null);
            for (int i = 0; i < tileCodes.Length; i++)
            {
                object[] arguments =
                {
                    dataFactory.CreateTile(tileCodes[i]),
                    null
                };
                Assert.That((bool)tryGetSprite.Invoke(catalog, arguments), Is.True);
                Image image = tileViews[i].GetComponent<Image>();
                Assert.That(image, Is.Not.Null);
                Assert.That(image.sprite, Is.SameAs(arguments[1]));
            }
        }

        private static Button FindButtonInScene(Scene scene, string buttonName)
        {
            return Resources.FindObjectsOfTypeAll<Button>()
                .SingleOrDefault(button =>
                    button.gameObject.scene == scene &&
                    button.name == buttonName);
        }

        private static void AssertDecisionButtonVisualStyle(
            Button button,
            bool requiresReadableLabel = true)
        {
            Assert.That(button, Is.Not.Null);
            Image background = button.targetGraphic as Image;
            Graphic label = button.GetComponentsInChildren<Graphic>(true)
                .FirstOrDefault(graphic => graphic != background);
            Outline outline = button.GetComponent<Outline>();
            Assert.That(background, Is.Not.Null);
            Assert.That(outline, Is.Not.Null);

            Assert.That(
                background.color.b,
                Is.GreaterThan(background.color.r),
                "Decision buttons need the chi-style cool, dark background.");
            Assert.That(background.color.grayscale, Is.LessThan(0.5f));
            Assert.That(background.color.a, Is.GreaterThan(0.8f));
            Assert.That(outline.effectColor.grayscale, Is.GreaterThan(background.color.grayscale));
            if (requiresReadableLabel)
            {
                Assert.That(label, Is.Not.Null);
                Assert.That(label.color.r, Is.GreaterThan(0.9f));
                Assert.That(label.color.g, Is.GreaterThan(0.9f));
                Assert.That(label.color.b, Is.GreaterThan(0.9f));
            }

            ColorBlock colors = button.colors;
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
            Assert.That(colors.highlightedColor, Is.Not.EqualTo(colors.normalColor));
            Assert.That(colors.pressedColor, Is.Not.EqualTo(colors.highlightedColor));
            Assert.That(colors.selectedColor, Is.Not.EqualTo(colors.normalColor));
            Assert.That(colors.disabledColor.a, Is.LessThan(colors.normalColor.a));
            Assert.That(colors.fadeDuration, Is.GreaterThan(0f));
        }

        private static void ConfigureChiTileImages(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            Component controller,
            Button ponButton,
            UnityObjectTestOwner owner,
            params string[] tileCodes)
        {
            ConfigureChiUiStructure(
                reflection,
                controller,
                ponButton.transform.parent,
                owner);

            Type tileSpriteViewType = reflection.RequireType(TileSpriteViewTypeName);
            GameObject tilePrefabObject = owner.Own(new GameObject(
                "ChiTileViewPrefab",
                typeof(RectTransform),
                typeof(Image)));
            tilePrefabObject.SetActive(false);
            Component tileViewPrefab = tilePrefabObject.AddComponent(tileSpriteViewType);
            reflection.SetPrivateField(
                tileViewPrefab,
                "targetImage",
                tilePrefabObject.GetComponent<Image>());

            ScriptableObject catalog = ScriptableObject.CreateInstance(
                reflection.RequireType(TileSpriteCatalogTypeName));
            owner.Register(catalog);
            Sprite[] sprites = new Sprite[tileCodes.Length];
            for (int i = 0; i < tileCodes.Length; i++)
                sprites[i] = CreateTestSprite(owner, tileCodes[i]);

            SetCatalogEntries(
                reflection,
                dataFactory,
                catalog,
                tileCodes,
                sprites);
            reflection.SetPrivateField(controller, "chiTileSpriteCatalog", catalog);
            reflection.SetPrivateField(controller, "chiTileViewPrefab", tileViewPrefab);
        }

        private static void ConfigureChiUiStructure(
            ReflectionTestAccess reflection,
            Component controller,
            Transform decisionRoot,
            UnityObjectTestOwner owner)
        {
            Type tmpTextType = reflection.RequireType(TmpTextTypeName);
            GameObject chiDecisionRoot = new GameObject(
                "ChiDecisionRoot",
                typeof(RectTransform));
            chiDecisionRoot.transform.SetParent(decisionRoot, false);
            GameObject headingObject = new GameObject(
                "ChiHeading",
                typeof(RectTransform),
                tmpTextType);
            headingObject.transform.SetParent(chiDecisionRoot.transform, false);
            Component headingLabel = headingObject.GetComponent(tmpTextType);
            GameObject optionsContainer = new GameObject(
                "ChiOptions",
                typeof(RectTransform));
            optionsContainer.transform.SetParent(chiDecisionRoot.transform, false);

            GameObject optionPrefabObject = owner.Own(new GameObject(
                "ChiOptionViewPrefab",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)));
            optionPrefabObject.SetActive(false);
            Component optionView = optionPrefabObject.AddComponent(
                reflection.RequireType(ChiOptionViewTypeName));
            GameObject tileContainer = new GameObject(
                "ChiTileImages",
                typeof(RectTransform));
            tileContainer.transform.SetParent(optionPrefabObject.transform, false);
            reflection.SetPrivateField(
                optionView,
                "selectButton",
                optionPrefabObject.GetComponent<Button>());
            reflection.SetPrivateField(optionView, "tileContainer", tileContainer.transform);

            reflection.SetPrivateField(controller, "chiDecisionRoot", chiDecisionRoot);
            reflection.SetPrivateField(controller, "chiHeadingLabel", headingLabel);
            reflection.SetPrivateField(
                controller,
                "chiOptionsContainer",
                optionsContainer.transform);
            reflection.SetPrivateField(controller, "chiOptionViewPrefab", optionView);
            chiDecisionRoot.SetActive(false);
        }

        private static void SetCatalogEntries(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            ScriptableObject catalog,
            IReadOnlyList<string> tileCodes,
            IReadOnlyList<Sprite> sprites)
        {
            Type entryType = catalog.GetType().GetNestedType(
                "Entry",
                BindingFlags.Public);
            Assert.That(entryType, Is.Not.Null);
            IList entries = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(entryType));
            for (int i = 0; i < tileCodes.Count; i++)
            {
                object entry = Activator.CreateInstance(entryType);
                object tile = dataFactory.CreateTile(tileCodes[i]);
                reflection.SetPrivateField(
                    entry,
                    "typeIndex",
                    (int)reflection.GetProperty(tile, "TypeIndex"));
                reflection.SetPrivateField(entry, "sprite", sprites[i]);
                entries.Add(entry);
            }

            reflection.SetPrivateField(catalog, "entries", entries);
        }

        private static Sprite CreateTestSprite(
            UnityObjectTestOwner owner,
            string name)
        {
            Texture2D texture = owner.Own(new Texture2D(1, 1));
            Sprite sprite = owner.Own(Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f)));
            sprite.name = name;
            return sprite;
        }

        private static void AssertChiTileSprites(
            Button chiButton,
            params string[] expectedSpriteNames)
        {
            Transform tileContainer = chiButton.transform.Find("ChiTileImages");
            Assert.That(tileContainer, Is.Not.Null);
            Assert.That(tileContainer.childCount, Is.EqualTo(expectedSpriteNames.Length));
            for (int i = 0; i < expectedSpriteNames.Length; i++)
            {
                Image image = tileContainer.GetChild(i).GetComponent<Image>();
                Assert.That(image, Is.Not.Null);
                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.sprite.name, Is.EqualTo(expectedSpriteNames[i]));
            }
        }

        private static Delegate CreateMeldCallHandler(
            Type handlerType,
            Action<string, int> recorder)
        {
            ParameterInfo[] parameters = handlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression kind = Expression.Parameter(parameters[0].ParameterType, "kind");
            ParameterExpression optionId = Expression.Parameter(parameters[1].ParameterType, "optionId");
            MethodInfo record = typeof(MahjongMeldCallDecisionControllerTests).GetMethod(
                nameof(RecordMeldCall),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodCallExpression body = Expression.Call(
                record,
                Expression.Constant(recorder),
                Expression.Convert(kind, typeof(object)),
                optionId);
            return Expression.Lambda(handlerType, body, kind, optionId).Compile();
        }

        private static Delegate CreateIgnoringHandler(
            Type handlerType,
            Action recorder)
        {
            ParameterInfo[] parameters = handlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression[] ignoredParameters = new ParameterExpression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ignoredParameters[i] = Expression.Parameter(
                    parameters[i].ParameterType,
                    parameters[i].Name);
            }

            MethodCallExpression body = Expression.Call(
                Expression.Constant(recorder),
                typeof(Action).GetMethod("Invoke"));
            return Expression.Lambda(handlerType, body, ignoredParameters).Compile();
        }

        private static void RecordMeldCall(Action<string, int> recorder, object kind, int optionId)
        {
            recorder(kind.ToString(), optionId);
        }

        private static Delegate CreateReactionResponseHandler(
            Type handlerType,
            Action<long, int, string, int?> recorder)
        {
            ParameterInfo[] parameters = handlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression requestId = Expression.Parameter(
                parameters[0].ParameterType,
                "requestId");
            ParameterExpression windowId = Expression.Parameter(
                parameters[1].ParameterType,
                "windowId");
            ParameterExpression kind = Expression.Parameter(
                parameters[2].ParameterType,
                "kind");
            ParameterExpression chiOptionId = Expression.Parameter(
                parameters[3].ParameterType,
                "chiOptionId");
            MethodInfo record = typeof(MahjongMeldCallDecisionControllerTests).GetMethod(
                nameof(RecordReactionResponse),
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodCallExpression body = Expression.Call(
                record,
                Expression.Constant(recorder),
                requestId,
                windowId,
                Expression.Convert(kind, typeof(object)),
                chiOptionId);
            return Expression.Lambda(
                handlerType,
                body,
                requestId,
                windowId,
                kind,
                chiOptionId).Compile();
        }

        private static void RecordReactionResponse(
            Action<long, int, string, int?> recorder,
            long requestId,
            int windowId,
            object kind,
            int? chiOptionId)
        {
            recorder(requestId, windowId, kind.ToString(), chiOptionId);
        }

        private static Button CreateButton(
            ReflectionTestAccess reflection,
            Type tmpTextType,
            Transform parent,
            string name,
            string label)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent);
            GameObject textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                tmpTextType);
            textObject.transform.SetParent(buttonObject.transform);
            reflection.SetProperty(textObject.GetComponent(tmpTextType), "text", label);
            return buttonObject.GetComponent<Button>();
        }

        private static Button FindButton(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static string ButtonLabel(
            ReflectionTestAccess reflection,
            Type tmpTextType,
            Button button)
        {
            Component label = button.GetComponentInChildren(tmpTextType, true);
            return label != null ? (string)reflection.GetProperty(label, "text") : null;
        }

        private static Button FindChiOptionButton(Transform decisionRoot, string name)
        {
            Transform child = decisionRoot.Find($"ChiDecisionRoot/ChiOptions/{name}");
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static int CountText(
            ReflectionTestAccess reflection,
            Transform root,
            Type tmpTextType,
            string expected)
        {
            Component[] texts = root.GetComponentsInChildren(tmpTextType, true);
            int count = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                if ((string)reflection.GetProperty(texts[i], "text") == expected)
                    count++;
            }

            return count;
        }
    }

    public sealed class MahjongPrototypeUiManagerInteractionTests
    {
        [Test]
        public void RefreshInteractionState_OtherTurn_DisablesDrawButAllowsSkillReservationAndSelfTilesDisabled()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalOtherTurn();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.False);
            }
        }

        [Test]
        public void RefreshInteractionState_SelfTurnWithDrawnTile_DisablesDrawButKeepsSkillAndSelfTilesInteractable()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
                Assert.That(driver.SelfDrawnTileInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_ReachDecision_KeepsControlAreaAndSelfTilesInteractable()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.BeginReachDecision();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.False);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
                Assert.That(driver.SelfDrawnTileInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_ReachDiscardSelection_KeepsAllTilesClickableAndCandidatesUndimmed()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.BeginReachDiscardSelection();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.AutoSortInteractable, Is.False);
                Assert.That(driver.CancelReachInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
                Assert.That(driver.SecondSelfHandTileInteractable, Is.True);
                Assert.That(driver.SelfDrawnTileInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileDimmed, Is.False);
                Assert.That(driver.SecondSelfHandTileDimmed, Is.True);
                Assert.That(driver.SelfDrawnTileDimmed, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_WinDecision_LeavesOnlyTileDiscardInputAvailable()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.BeginWinDecision();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.AutoSortInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
                Assert.That(driver.SelfDrawnTileInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_LockedStatesDisableButtonsButKeepTargetTileInputEditable()
        {
            AssertControlAreaLocked(driver => driver.BeginWinDecision());
            AssertControlAreaLocked(driver => driver.MarkRoundEnded());
        }

        [Test]
        public void ReachDiscardSelection_ForceDrawSkillKeepsCandidatesAndDisablesButtonImmediately()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.BeginReachDiscardSelection();
                driver.TargetTileText = "5m";
                driver.EnableUiNotifications();
                driver.EnableCommandRouting();
                driver.RefreshInteraction();
                int candidateCountBefore = driver.ReachDiscardCandidateCount;

                Assert.That(driver.CancelReachInteractable, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);

                driver.ClickForceDrawSkill();

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(1));
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(candidateCountBefore));
                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.False);
                Assert.That(driver.AutoSortInteractable, Is.False);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.CancelReachInteractable, Is.True);
                Assert.That(driver.FirstSelfHandTileInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_ReachWaitingForDraw_EnablesDrawAndForceDrawSkillSeparately()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.DeclareReachWaitingForDraw();

                driver.RefreshInteraction();

                Assert.That(driver.DrawInteractable, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.True);
                Assert.That(driver.TargetTileInputInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_DeclaredReachWithDrawnTile_LocksHandButAllowsTsumogiri()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.DeclareReachWithDrawnTile();

                driver.RefreshInteraction();

                Assert.That(driver.FirstSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.True);
            }
        }

        [Test]
        public void RefreshInteractionState_PreservesTargetTileTextAndSelectionAcrossStateTransitions()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalSelfTurn();
                driver.TargetTileText = "5m";
                driver.SetTargetTileSelection(1, 2);

                Assert.That(driver.TargetTileText, Is.EqualTo("5m"));
                Assert.That(driver.TargetTileSelectionAnchorPosition, Is.EqualTo(1));
                Assert.That(driver.TargetTileSelectionFocusPosition, Is.EqualTo(2));

                driver.RefreshInteraction();
                driver.BeginReachDiscardSelection();
                driver.RefreshInteraction();
                driver.BeginWinDecision();
                driver.RefreshInteraction();

                Assert.That(driver.TargetTileInputInteractable, Is.True);
                Assert.That(driver.TargetTileText, Is.EqualTo("5m"));
                Assert.That(driver.TargetTileSelectionAnchorPosition, Is.EqualTo(1));
                Assert.That(driver.TargetTileSelectionFocusPosition, Is.EqualTo(2));
            }
        }

        [Test]
        public void OtherTurn_ForceDrawSkillCommand_ReservesSkillForViewContextLocalSeat()
        {
            using (Driver driver = Driver.Create())
            {
                driver.PrepareNormalOtherTurn();
                driver.TargetTileText = "5m";
                driver.EnableUiNotifications();
                driver.EnableCommandRouting();
                driver.RefreshInteraction();

                driver.ClickForceDrawSkill();

                Assert.That(driver.HasForceDrawReservationForSelf, Is.True);
                Assert.That(driver.ForceDrawSkillInteractable, Is.False);
            }
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
                Assert.That(driver.DrawInteractable, Is.False);
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
                Assert.That(driver.TargetTileInputInteractable, Is.True);
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
            private const string OpenMeldViewTypeName =
                "MahjongPrototype.UI3D.Mahjong3DOpenMeldView, Assembly-CSharp";
            private const string TileViewTypeName =
                "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";
            private const string TileFaceCatalogTypeName =
                "MahjongPrototype.UI3D.Mahjong3DTileFaceCatalog, Assembly-CSharp";
            private const string ReachDiscardCandidateTypeName =
                "MahjongPrototype.Services.ReachDiscardCandidate, Assembly-CSharp";
            private const string DiscardSourceTypeName =
                "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";
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
            private bool uiNotificationsEnabled;
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
            public bool IsReachDiscardSelectionPending =>
                (bool)reflection.GetProperty(State, "IsReachDiscardSelectionPending");
            public int ReachDiscardCandidateCount => collections.Count(
                reflection.GetProperty(State, "ReachDiscardCandidates"));
            public int ActiveSkillEffectCount => session.Query.ActiveSkillEffectCount;
            public bool CancelReachInteractable =>
                ((Button)reflection.GetPrivateField(inputController, "cancelReachButton")).interactable;
            public string TargetTileText
            {
                get => (string)reflection.GetProperty(targetTileInput, "text");
                set => reflection.SetProperty(targetTileInput, "text", value);
            }
            public int TargetTileSelectionAnchorPosition =>
                (int)reflection.GetProperty(
                    targetTileInput,
                    "selectionStringAnchorPosition");
            public int TargetTileSelectionFocusPosition =>
                (int)reflection.GetProperty(
                    targetTileInput,
                    "selectionStringFocusPosition");
            public bool HasForceDrawReservationForSelf
            {
                get
                {
                    object reservationService = reflection.GetPrivateField(
                        session.GameFlow,
                        "skillReservationService");
                    return (bool)reflection.Invoke(
                        reservationService,
                        "HasReservation",
                        session.DataFactory.ParseSeat("East"));
                }
            }
            public bool FirstSelfHandTileInteractable => TileInteractable(FirstSelfHandTile);
            public bool SecondSelfHandTileInteractable => TileInteractable(SelfHandTileAt(1));
            public bool SelfDrawnTileInteractable => TileInteractable(SelfDrawnTile);
            public bool FirstSelfHandTileDimmed => TileDimmed(FirstSelfHandTile);
            public bool SecondSelfHandTileDimmed => TileDimmed(SelfHandTileAt(1));
            public bool SelfDrawnTileDimmed => TileDimmed(SelfDrawnTile);

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
                    AddEventNotifier = true,
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

            public void DeclareReachWithDrawnTile()
            {
                reflection.Invoke(
                    session.Query.GetPlayerSeat("East"),
                    "DeclareReach",
                    session.Query.TurnIndex);
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

            public void EnableUiNotifications()
            {
                if (uiNotificationsEnabled)
                    return;

                reflection.SetPrivateField(uiManager, "eventNotifier", session.EventNotifier);
                reflection.Invoke(uiManager, "SubscribeNotifications");
                uiNotificationsEnabled = true;
            }

            public void ClickDraw()
            {
                drawButton.onClick.Invoke();
            }

            public void ClickForceDrawSkill()
            {
                forceDrawSkillButton.onClick.Invoke();
            }

            public void SetTargetTileSelection(int anchorPosition, int focusPosition)
            {
                reflection.SetProperty(
                    targetTileInput,
                    "selectionStringAnchorPosition",
                    anchorPosition);
                reflection.SetProperty(
                    targetTileInput,
                    "selectionStringFocusPosition",
                    focusPosition);
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

                if (uiNotificationsEnabled)
                    reflection.Invoke(uiManager, "UnsubscribeNotifications");

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
                targetTileInput = CreateInput(reflection, inputObject.transform, "TargetTile");

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
                Component openMeldView =
                    CreateChild(selfController.transform, "OpenMeldView")
                    .AddComponent(reflection.RequireType(OpenMeldViewTypeName));

                reflection.SetPrivateField(handView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(drawnTileView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(discardRiverView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(openMeldView, "tilePrefab", tilePrefab);
                reflection.SetPrivateField(selfController, "handView", handView);
                reflection.SetPrivateField(selfController, "drawnTileView", drawnTileView);
                reflection.SetPrivateField(selfController, "discardRiverView", discardRiverView);
                reflection.SetPrivateField(selfController, "openMeldView", openMeldView);
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

            private bool TileDimmed(object tile)
            {
                return (bool)reflection.GetProperty(tile, "IsDimmed");
            }

            private static Button CreateButton(Transform parent, string name)
            {
                return CreateChild(parent, name).AddComponent<Button>();
            }

            private static Toggle CreateToggle(Transform parent, string name)
            {
                return CreateChild(parent, name).AddComponent<Toggle>();
            }

            private static Component CreateInput(
                ReflectionTestAccess reflection,
                Transform parent,
                string name)
            {
                return TmpInputFieldTestFactory.Create(reflection, parent, name);
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
