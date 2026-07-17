using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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
        private const string InputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string ChiOptionTypeName =
            "MahjongPrototype.Domain.ChiOption, Assembly-CSharp";
        private const string TmpTextTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        [Test]
        public void SetMeldCallDecision_ShowsPonAndEveryChiOption_AndRoutesTheSelectedOptionId()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("MeldCallDecisionTestRoot");
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
                    "拒否");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);

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

                reflection.Invoke(
                    controller,
                    "SetMeldCallDecision",
                    true,
                    options,
                    calledTile);
                Assert.That(ponButton.gameObject.activeSelf, Is.True);
                Button secondChiButton = FindButton(decisionRoot.transform, "ChiOption_4");
                Assert.That(secondChiButton, Is.Not.Null);
                Component label = secondChiButton.transform.Find("Text").GetComponent(tmpTextType);
                Assert.That(
                    reflection.GetProperty(label, "text"),
                    Is.EqualTo("チー 4m 5m 6m"));

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

                reflection.Invoke(controller, "SetMeldCallDecision", false, null, null);
                Assert.That(decisionRoot.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReactionMeldButtons_EmitTheRequestIdentityThatCreatedThem()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("ReactionMeldCallDecisionIdentityTestRoot");
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
                    "パス");
                reflection.SetPrivateField(controller, "ponDecisionRoot", decisionRoot);
                reflection.SetPrivateField(controller, "ponButton", ponButton);
                reflection.SetPrivateField(controller, "declineButton", declineButton);
                reflection.SetPrivateField(controller, "inputController", inputController);

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

                FindButton(decisionRoot.transform, "ChiOption_4").onClick.Invoke();
                Assert.That(actualRequestId, Is.EqualTo(901));
                Assert.That(actualWindowId, Is.EqualTo(71));
                Assert.That(actualKind, Is.EqualTo("Chi"));
                Assert.That(actualChiOptionId, Is.EqualTo(4));

                FindButton(decisionRoot.transform, "Daiminkan").onClick.Invoke();
                Assert.That(actualRequestId, Is.EqualTo(901));
                Assert.That(actualWindowId, Is.EqualTo(71));
                Assert.That(actualKind, Is.EqualTo("Daiminkan"));
                Assert.That(actualChiOptionId, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClearReactionMeldCallDecision_RemovesDynamicButtonsAndRequestIdentity()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            GameObject root = new GameObject("ReactionMeldCallDecisionCleanupTestRoot");
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
                Assert.That(FindButton(decisionRoot.transform, "ChiOption_4"), Is.Not.Null);
                Assert.That(FindButton(decisionRoot.transform, "Daiminkan"), Is.Not.Null);

                reflection.Invoke(controller, "ClearReactionMeldCallDecision");

                Assert.That(decisionRoot.activeSelf, Is.False);
                Assert.That(FindButton(decisionRoot.transform, "ChiOption_4"), Is.Null);
                Assert.That(FindButton(decisionRoot.transform, "Daiminkan"), Is.Null);
                Assert.That(
                    (bool)reflection.GetPrivateField(controller, "hasReactionRequest"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static IList CreateChiOptions(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            object calledTile)
        {
            Type chiOptionType = reflection.RequireType(ChiOptionTypeName);
            IList options = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(chiOptionType));
            options.Add(reflection.CreateInstance(
                chiOptionType,
                3,
                calledTile,
                dataFactory.CreateTileArrayFromText("3m 4m"),
                dataFactory.CreateTileArrayFromText("3m 4m 5m")));
            options.Add(reflection.CreateInstance(
                chiOptionType,
                4,
                calledTile,
                dataFactory.CreateTileArrayFromText("4m 6m"),
                dataFactory.CreateTileArrayFromText("4m 5m 6m")));
            return options;
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
        public void RefreshInteractionState_ReachDecision_KeepsControlAreaInteractableAndSelfTilesDisabled()
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
                Assert.That(driver.FirstSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.False);
            }
        }

        [Test]
        public void RefreshInteractionState_ReachDiscardSelection_KeepsForceDrawSkillAndCancelEnabledWithCandidates()
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
                Assert.That(driver.SecondSelfHandTileInteractable, Is.False);
                Assert.That(driver.SelfDrawnTileInteractable, Is.False);
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
