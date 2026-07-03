using System;
using MahjongPrototype.Tests.TestSupport.Core;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class MahjongReachDecisionControllerTestDriver : IDisposable
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongReachDecisionController, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly Type controllerType;
        private readonly GameObject root;
        private GameObject controllerHost;
        private GameObject decisionRoot;
        private GameObject cancelRoot;
        private Component controller;
        private Button reachButton;
        private Button declineButton;
        private bool disposed;

        private MahjongReachDecisionControllerTestDriver(
            ReflectionTestAccess reflection,
            Type controllerType)
        {
            this.reflection = reflection;
            this.controllerType = controllerType;
            root = new GameObject("MahjongReachDecisionControllerTestDriver");
        }

        public static MahjongReachDecisionControllerTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            return new MahjongReachDecisionControllerTestDriver(
                reflection,
                reflection.RequireType(ControllerTypeName));
        }

        public void CreateDecisionRoot(string name, bool active)
        {
            decisionRoot = CreateChild(name, active);
            AssignRootIfControllerExists("reachDecisionRoot", decisionRoot);
        }

        public void CreateCancelRoot(string name, bool active)
        {
            cancelRoot = CreateChild(name, active);
            AssignRootIfControllerExists("reachCancelRoot", cancelRoot);
        }

        public void UseDecisionRootAsControllerHost()
        {
            EnsureDecisionRoot();
            controller = decisionRoot.AddComponent(controllerType);
            AssignRoots();
        }

        public void AddReachDecisionControls(bool interactable)
        {
            EnsureDecisionRoot();
            reachButton = CreateButton("ReachButton", decisionRoot, interactable);
            declineButton = CreateButton("DeclineReachButton", decisionRoot, interactable);
        }

        public void CreateControllerOnDecisionAreaNameWithoutAssignedRoot()
        {
            CreateDecisionRoot("ReachDecisionArea", true);
            controller = decisionRoot.AddComponent(controllerType);
        }

        public void ExpectWarning(string message)
        {
            LogAssert.Expect(LogType.Warning, message);
        }

        public void SetVisible(bool visible)
        {
            reflection.Invoke(Controller, "SetVisible", visible);
        }

        public void SetReachUiVisible(bool showDecision, bool showCancel)
        {
            reflection.Invoke(Controller, "SetReachUiVisible", showDecision, showCancel);
        }

        public bool DecisionRootActive => decisionRoot.activeSelf;
        public bool CancelRootActive => cancelRoot.activeSelf;
        public bool ReachDecisionControlInteractable => reachButton.interactable;
        public bool DeclineReachControlInteractable => declineButton.interactable;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        private Component Controller
        {
            get
            {
                EnsureController();
                return controller;
            }
        }

        private void EnsureController()
        {
            if (controller != null)
                return;

            controllerHost = CreateChild("ReachDecisionControllerHost", true);
            controller = controllerHost.AddComponent(controllerType);
            AssignRoots();
        }

        private void AssignRootIfControllerExists(string fieldName, GameObject value)
        {
            if (controller != null)
                reflection.SetPrivateField(controller, fieldName, value);
        }

        private void AssignRoots()
        {
            if (decisionRoot != null)
                reflection.SetPrivateField(controller, "reachDecisionRoot", decisionRoot);

            if (cancelRoot != null)
                reflection.SetPrivateField(controller, "reachCancelRoot", cancelRoot);
        }

        private GameObject CreateChild(string name, bool active)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(root.transform);
            child.SetActive(active);
            return child;
        }

        private static Button CreateButton(string name, GameObject parent, bool interactable)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent.transform);
            Button button = buttonObject.AddComponent<Button>();
            button.interactable = interactable;
            return button;
        }

        private void EnsureDecisionRoot()
        {
            if (decisionRoot == null)
                CreateDecisionRoot("ReachDecisionRoot", true);
        }
    }
}
