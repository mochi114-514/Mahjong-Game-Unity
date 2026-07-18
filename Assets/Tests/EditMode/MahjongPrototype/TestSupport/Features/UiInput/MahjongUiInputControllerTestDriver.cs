using System;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.Tests.TestSupport.Features.UiInput
{
    internal sealed class MahjongUiInputControllerTestDriver : IDisposable
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private readonly ReflectionTestAccess reflection;
        private readonly UnityObjectTestOwner owner;
        private readonly GameObject root;
        private readonly Component controller;
        private readonly Controls controls;
        private Button unassignedDrawButton;
        private bool enabled;
        private bool disposed;

        private MahjongUiInputControllerTestDriver(
            ReflectionTestAccess reflection,
            UnityObjectTestOwner owner,
            GameObject root,
            Component controller,
            Controls controls)
        {
            this.reflection = reflection;
            this.owner = owner;
            this.root = root;
            this.controller = controller;
            this.controls = controls;
        }

        public int DrawCount { get; private set; }
        public string SkillTarget { get; private set; }
        public bool? AutoSortValue { get; private set; }
        public int RetryCount { get; private set; }
        public int WinCount { get; private set; }
        public int DeclineWinCount { get; private set; }
        public int ReachCount { get; private set; }
        public int DeclineReachCount { get; private set; }
        public int CancelReachCount { get; private set; }
        public int RoundResultConfirmCount { get; private set; }
        public int AutoSortEventCount { get; private set; }
        public ReflectionTestAccess Reflection => reflection;
        public Component Controller => controller;

        public bool DrawInteractable => controls.DrawButton.interactable;
        public bool ForceDrawSkillInteractable => controls.ForceDrawSkillButton.interactable;
        public bool TargetTileInputInteractable =>
            (bool)reflection.GetProperty(controls.TargetTileInput, "interactable");
        public bool RetryInteractable
        {
            get => controls.RetryButton.interactable;
            set => controls.RetryButton.interactable = value;
        }

        public bool CancelReachInteractable
        {
            get => controls.CancelReachButton.interactable;
            set => controls.CancelReachButton.interactable = value;
        }

        public bool AutoSortIsOn => controls.AutoSortToggle.isOn;
        public bool AutoSortInteractable => controls.AutoSortToggle.interactable;
        public bool RoundResultConfirmInteractable
        {
            get => controls.RoundResultConfirmButton.interactable;
            set => controls.RoundResultConfirmButton.interactable = value;
        }

        public string TargetTileText
        {
            get => (string)reflection.GetProperty(controls.TargetTileInput, "text");
            set => reflection.SetProperty(controls.TargetTileInput, "text", value);
        }

        public int TargetTileSelectionAnchorPosition =>
            (int)reflection.GetProperty(
                controls.TargetTileInput,
                "selectionStringAnchorPosition");

        public int TargetTileSelectionFocusPosition =>
            (int)reflection.GetProperty(
                controls.TargetTileInput,
                "selectionStringFocusPosition");

        public static MahjongUiInputControllerTestDriver Create(string rootName)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            UnityObjectTestOwner owner = new UnityObjectTestOwner();

            try
            {
                GameObject root = owner.Own(new GameObject(rootName));
                root.SetActive(false);
                Component controller = root.AddComponent(reflection.RequireType(ControllerTypeName));
                Controls controls = Controls.Create(reflection, root.transform);
                AssignControls(reflection, controller, controls);
                return new MahjongUiInputControllerTestDriver(
                    reflection,
                    owner,
                    root,
                    controller,
                    controls);
            }
            catch
            {
                owner.Dispose();
                throw;
            }
        }

        public void SubscribeAllRequestEvents()
        {
            AddEventHandler("DrawRequested", new Action(() => DrawCount++));
            AddEventHandler("ForceDrawSkillRequested", new Action<string>(value => SkillTarget = value));
            AddEventHandler("AutoSortChanged", new Action<bool>(value => AutoSortValue = value));
            AddEventHandler("RetryRequested", new Action(() => RetryCount++));
            AddEventHandler("WinRequested", new Action(() => WinCount++));
            AddEventHandler("DeclineWinRequested", new Action(() => DeclineWinCount++));
            AddEventHandler("ReachRequested", new Action(() => ReachCount++));
            AddEventHandler("DeclineReachRequested", new Action(() => DeclineReachCount++));
            AddEventHandler("CancelReachRequested", new Action(() => CancelReachCount++));
            AddEventHandler("RoundResultConfirmRequested", new Action(() => RoundResultConfirmCount++));
        }

        public void SubscribeDrawRequested()
        {
            AddEventHandler("DrawRequested", new Action(() => DrawCount++));
        }

        public void SubscribeAutoSortChangedCount()
        {
            AddEventHandler("AutoSortChanged", new Action<bool>(_ => AutoSortEventCount++));
        }

        public void SubscribeRoundResultConfirmRequested()
        {
            AddEventHandler("RoundResultConfirmRequested", new Action(() => RoundResultConfirmCount++));
        }

        public void EnableController()
        {
            if (enabled)
                return;

            reflection.Invoke(controller, "OnEnable");
            enabled = true;
        }

        public void DisableController()
        {
            if (!enabled)
                return;

            reflection.Invoke(controller, "OnDisable");
            enabled = false;
        }

        public void CreateUnassignedDrawButtonChild()
        {
            unassignedDrawButton = CreateButton(root.transform, "DrawButton");
        }

        public void ClearDrawButton()
        {
            reflection.SetPrivateField(controller, "drawButton", null);
        }

        public void ClearReachButton()
        {
            reflection.SetPrivateField(controller, "reachButton", null);
        }

        public void ClearAutoSortToggle()
        {
            reflection.SetPrivateField(controller, "autoSortToggle", null);
        }

        public void ClearRoundResultConfirmButton()
        {
            reflection.SetPrivateField(controller, "roundResultConfirmButton", null);
        }

        public void ClickDraw()
        {
            controls.DrawButton.onClick.Invoke();
        }

        public void ClickForceDrawSkill()
        {
            controls.ForceDrawSkillButton.onClick.Invoke();
        }

        public void ToggleAutoSort(bool value)
        {
            controls.AutoSortToggle.onValueChanged.Invoke(value);
        }

        public void ClickRetry()
        {
            controls.RetryButton.onClick.Invoke();
        }

        public void ClickWin()
        {
            controls.WinButton.onClick.Invoke();
        }

        public void ClickDeclineWin()
        {
            controls.DeclineWinButton.onClick.Invoke();
        }

        public void ClickReach()
        {
            controls.ReachButton.onClick.Invoke();
        }

        public void ClickDeclineReach()
        {
            controls.DeclineReachButton.onClick.Invoke();
        }

        public void ClickCancelReach()
        {
            controls.CancelReachButton.onClick.Invoke();
        }

        public void ClickRoundResultConfirm()
        {
            controls.RoundResultConfirmButton.onClick.Invoke();
        }

        public void ClickUnassignedDrawButton()
        {
            unassignedDrawButton.onClick.Invoke();
        }

        public void SetGameplayInputInteractable(bool interactable)
        {
            reflection.Invoke(controller, "SetGameplayInputInteractable", interactable);
        }

        public void SetTargetTileSelection(int anchorPosition, int focusPosition)
        {
            reflection.SetProperty(
                controls.TargetTileInput,
                "selectionStringAnchorPosition",
                anchorPosition);
            reflection.SetProperty(
                controls.TargetTileInput,
                "selectionStringFocusPosition",
                focusPosition);
        }

        public void SetAutoSortInteractable(bool interactable)
        {
            reflection.Invoke(controller, "SetAutoSortInteractable", interactable);
        }

        public void SetAutoSortWithoutNotify(bool value)
        {
            reflection.Invoke(controller, "SetAutoSortWithoutNotify", value);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            DisableController();
            owner.Dispose();
        }

        private static void AssignControls(
            ReflectionTestAccess reflection,
            Component controller,
            Controls controls)
        {
            reflection.SetPrivateField(controller, "drawButton", controls.DrawButton);
            reflection.SetPrivateField(controller, "forceDrawSkillButton", controls.ForceDrawSkillButton);
            reflection.SetPrivateField(controller, "autoSortToggle", controls.AutoSortToggle);
            reflection.SetPrivateField(controller, "retryButton", controls.RetryButton);
            reflection.SetPrivateField(controller, "winButton", controls.WinButton);
            reflection.SetPrivateField(controller, "declineWinButton", controls.DeclineWinButton);
            reflection.SetPrivateField(controller, "reachButton", controls.ReachButton);
            reflection.SetPrivateField(controller, "declineReachButton", controls.DeclineReachButton);
            reflection.SetPrivateField(controller, "cancelReachButton", controls.CancelReachButton);
            reflection.SetPrivateField(controller, "roundResultConfirmButton", controls.RoundResultConfirmButton);
            reflection.SetPrivateField(controller, "targetTileInput", controls.TargetTileInput);
        }

        private void AddEventHandler(string eventName, Delegate handler)
        {
            EventInfo eventInfo = controller.GetType().GetEvent(eventName);
            if (eventInfo == null)
                throw new MissingMemberException(controller.GetType().FullName, eventName);

            eventInfo.AddEventHandler(controller, handler);
        }

        private static Button CreateButton(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<Button>();
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
            public Button RoundResultConfirmButton;
            public Component TargetTileInput;

            public static Controls Create(ReflectionTestAccess reflection, Transform parent)
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
                    RoundResultConfirmButton = CreateButton(parent, "RenamedRoundResultConfirm"),
                    TargetTileInput = CreateInput(reflection, parent, "RenamedTargetTile")
                };
            }

            private static Toggle CreateToggle(Transform parent, string name)
            {
                GameObject gameObject = new GameObject(name);
                gameObject.transform.SetParent(parent);
                return gameObject.AddComponent<Toggle>();
            }

            private static Component CreateInput(
                ReflectionTestAccess reflection,
                Transform parent,
                string name)
            {
                return TmpInputFieldTestFactory.Create(reflection, parent, name);
            }
        }
    }
}
