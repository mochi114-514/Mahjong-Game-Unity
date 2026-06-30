using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongUiInputControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string TmpInputFieldTypeName =
            "TMPro.TMP_InputField, Unity.TextMeshPro";

        [Test]
        public void AssignedControls_InvokeEventsEvenWhenObjectNamesDiffer()
        {
            GameObject root = new GameObject("InputControllerHost");
            root.SetActive(false);
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Controls controls = CreateControls(root.transform);
                AssignControls(controller, controls);

                int drawCount = 0;
                string skillTarget = null;
                bool? autoSortValue = null;
                int retryCount = 0;
                int winCount = 0;
                int declineWinCount = 0;
                int reachCount = 0;
                int declineReachCount = 0;
                int cancelReachCount = 0;
                AddEventHandler(controller, "DrawRequested", new Action(() => drawCount++));
                AddEventHandler(controller, "ForceDrawSkillRequested", new Action<string>(value => skillTarget = value));
                AddEventHandler(controller, "AutoSortChanged", new Action<bool>(value => autoSortValue = value));
                AddEventHandler(controller, "RetryRequested", new Action(() => retryCount++));
                AddEventHandler(controller, "WinRequested", new Action(() => winCount++));
                AddEventHandler(controller, "DeclineWinRequested", new Action(() => declineWinCount++));
                AddEventHandler(controller, "ReachRequested", new Action(() => reachCount++));
                AddEventHandler(controller, "DeclineReachRequested", new Action(() => declineReachCount++));
                AddEventHandler(controller, "CancelReachRequested", new Action(() => cancelReachCount++));

                SetProperty(controls.TargetTileInput, "text", "5m");
                root.SetActive(true);
                controls.DrawButton.onClick.Invoke();
                controls.ForceDrawSkillButton.onClick.Invoke();
                controls.AutoSortToggle.onValueChanged.Invoke(true);
                controls.RetryButton.onClick.Invoke();
                controls.WinButton.onClick.Invoke();
                controls.DeclineWinButton.onClick.Invoke();
                controls.ReachButton.onClick.Invoke();
                controls.DeclineReachButton.onClick.Invoke();
                controls.CancelReachButton.onClick.Invoke();

                Assert.That(drawCount, Is.EqualTo(1));
                Assert.That(skillTarget, Is.EqualTo("5m"));
                Assert.That(autoSortValue, Is.True);
                Assert.That(retryCount, Is.EqualTo(1));
                Assert.That(winCount, Is.EqualTo(1));
                Assert.That(declineWinCount, Is.EqualTo(1));
                Assert.That(reachCount, Is.EqualTo(1));
                Assert.That(declineReachCount, Is.EqualTo(1));
                Assert.That(cancelReachCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MissingDrawButton_WarnsAndDoesNotAutoFindChildNamedDrawButton()
        {
            GameObject root = new GameObject("InputControllerNoDrawTest");
            root.SetActive(false);
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Controls controls = CreateControls(root.transform);
                Button childNamedDrawButton = CreateButton(root.transform, "DrawButton");
                AssignControls(controller, controls);
                SetPrivateField(controller, "drawButton", null);
                int drawCount = 0;
                AddEventHandler(controller, "DrawRequested", new Action(() => drawCount++));

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: DrawButton is not assigned.");

                root.SetActive(true);
                childNamedDrawButton.onClick.Invoke();

                Assert.That(drawCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MissingReachButton_Warns()
        {
            GameObject root = new GameObject("InputControllerNoReachTest");
            root.SetActive(false);
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Controls controls = CreateControls(root.transform);
                AssignControls(controller, controls);
                SetPrivateField(controller, "reachButton", null);

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: ReachButton is not assigned.");

                root.SetActive(true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MissingAutoSortToggle_Warns()
        {
            GameObject root = new GameObject("InputControllerNoAutoSortTest");
            root.SetActive(false);
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Controls controls = CreateControls(root.transform);
                AssignControls(controller, controls);
                SetPrivateField(controller, "autoSortToggle", null);

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: AutoSortToggle is not assigned.");

                root.SetActive(true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetGameplayInputInteractable_ControlsOnlyGameplayInputs()
        {
            GameObject root = new GameObject("InputControllerInteractableTest");
            root.SetActive(false);
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Controls controls = CreateControls(root.transform);
                AssignControls(controller, controls);
                controls.RetryButton.interactable = true;
                controls.CancelReachButton.interactable = true;

                Invoke(controller, "SetGameplayInputInteractable", false);

                Assert.That(controls.DrawButton.interactable, Is.False);
                Assert.That(controls.ForceDrawSkillButton.interactable, Is.False);
                Assert.That(GetProperty(controls.TargetTileInput, "interactable"), Is.False);
                Assert.That(controls.RetryButton.interactable, Is.True);
                Assert.That(controls.CancelReachButton.interactable, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetAutoSortWithoutNotify_UpdatesToggleWithoutEvent()
        {
            GameObject root = new GameObject("InputControllerAutoSortTest");
            root.SetActive(false);
            try
            {
                Component controller = root.AddComponent(Type.GetType(ControllerTypeName, true));
                Controls controls = CreateControls(root.transform);
                AssignControls(controller, controls);
                int autoSortEventCount = 0;
                AddEventHandler(controller, "AutoSortChanged", new Action<bool>(_ => autoSortEventCount++));

                Invoke(controller, "SetAutoSortWithoutNotify", true);

                Assert.That(controls.AutoSortToggle.isOn, Is.True);
                Assert.That(autoSortEventCount, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Controls CreateControls(Transform parent)
        {
            return new Controls
            {
                DrawButton = CreateButton(parent, "RenamedDraw"),
                ForceDrawSkillButton = CreateButton(parent, "RenamedSkill"),
                AutoSortToggle = CreateToggle(parent, "RenamedAutoSort"),
                RetryButton = CreateButton(parent, "RenamedRetry"),
                WinButton = CreateButton(parent, "RenamedWin"),
                DeclineWinButton = CreateButton(parent, "RenamedDeclineWin"),
                ReachButton = CreateButton(parent, "RenamedReach"),
                DeclineReachButton = CreateButton(parent, "RenamedDeclineReach"),
                CancelReachButton = CreateButton(parent, "RenamedCancelReach"),
                TargetTileInput = CreateInput(parent, "RenamedTargetTile")
            };
        }

        private static Button CreateButton(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<Button>();
        }

        private static Toggle CreateToggle(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<Toggle>();
        }

        private static Component CreateInput(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent(Type.GetType(TmpInputFieldTypeName, true));
        }

        private static void AssignControls(Component controller, Controls controls)
        {
            SetPrivateField(controller, "drawButton", controls.DrawButton);
            SetPrivateField(controller, "forceDrawSkillButton", controls.ForceDrawSkillButton);
            SetPrivateField(controller, "autoSortToggle", controls.AutoSortToggle);
            SetPrivateField(controller, "retryButton", controls.RetryButton);
            SetPrivateField(controller, "winButton", controls.WinButton);
            SetPrivateField(controller, "declineWinButton", controls.DeclineWinButton);
            SetPrivateField(controller, "reachButton", controls.ReachButton);
            SetPrivateField(controller, "declineReachButton", controls.DeclineReachButton);
            SetPrivateField(controller, "cancelReachButton", controls.CancelReachButton);
            SetPrivateField(controller, "targetTileInput", controls.TargetTileInput);
        }

        private static void AddEventHandler(object target, string eventName, Delegate handler)
        {
            EventInfo eventInfo = target.GetType().GetEvent(eventName);
            Assert.That(eventInfo, Is.Not.Null);
            eventInfo.AddEventHandler(target, handler);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            property.SetValue(target, value);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class Controls
        {
            public Button DrawButton;
            public Button ForceDrawSkillButton;
            public Toggle AutoSortToggle;
            public Button RetryButton;
            public Button WinButton;
            public Button DeclineWinButton;
            public Button ReachButton;
            public Button DeclineReachButton;
            public Button CancelReachButton;
            public Component TargetTileInput;
        }
    }
}
