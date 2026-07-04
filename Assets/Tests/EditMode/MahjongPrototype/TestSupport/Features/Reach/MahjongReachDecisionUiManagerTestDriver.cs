using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class MahjongReachDecisionUiManagerTestDriver : IDisposable
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongReachDecisionController, Assembly-CSharp";
        private const string UiManagerTypeName =
            "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly UnityObjectTestOwner owner;
        private readonly Type controllerType;
        private readonly Type uiManagerType;
        private readonly GameObject root;
        private readonly GameObject uiObject;
        private readonly Component uiManager;
        private ReachGameFlowTestSupport flowSupport;
        private GameObject decisionArea;
        private Component decisionController;
        private bool disposed;

        private MahjongReachDecisionUiManagerTestDriver(
            ReflectionTestAccess reflection,
            UnityObjectTestOwner owner,
            Type controllerType,
            Type uiManagerType)
        {
            this.reflection = reflection;
            this.owner = owner;
            this.controllerType = controllerType;
            this.uiManagerType = uiManagerType;

            root = owner.Own(new GameObject("MahjongReachDecisionUiManagerTestDriver"));
            uiObject = new GameObject("MahjongUiManager");
            uiObject.transform.SetParent(root.transform);
            uiObject.SetActive(false);
            uiManager = uiObject.AddComponent(this.uiManagerType);
        }

        public static MahjongReachDecisionUiManagerTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            return new MahjongReachDecisionUiManagerTestDriver(
                reflection,
                new UnityObjectTestOwner(),
                reflection.RequireType(ControllerTypeName),
                reflection.RequireType(UiManagerTypeName));
        }

        public void PrepareReachableGameState()
        {
            flowSupport = ReachGameFlowTestSupport.Create("ReachDecisionUiManagerGameFlowTest");
            flowSupport.DrawReachableHand();
            reflection.SetPrivateField(uiManager, "gameFlow", flowSupport.GameFlow);
        }

        public void CreateDecisionArea(string name, bool active)
        {
            decisionArea = new GameObject(name);
            decisionArea.transform.SetParent(uiObject.transform);
            decisionArea.SetActive(active);
        }

        public void AddDecisionControllerToArea()
        {
            EnsureDecisionArea();
            decisionController = decisionArea.AddComponent(controllerType);
            reflection.SetPrivateField(decisionController, "reachDecisionRoot", decisionArea);
        }

        public void AssignControllerToUiManager()
        {
            reflection.SetPrivateField(uiManager, "reachDecisionController", decisionController);
        }

        public void RefreshReachDecision()
        {
            reflection.Invoke(uiManager, "RefreshReachDecision", flowSupport.CurrentState);
        }

        public void EnsureReachDecisionController()
        {
            reflection.Invoke(uiManager, "EnsureReachDecisionController");
        }

        public void ExpectWarning(string message)
        {
            LogAssert.Expect(LogType.Warning, message);
        }

        public bool IsReachDecisionPending =>
            (bool)reflection.GetProperty(flowSupport.CurrentState, "IsReachDecisionPending");

        public bool DecisionAreaActive => decisionArea.activeSelf;

        public bool DecisionAreaHasController =>
            decisionArea.GetComponent(controllerType) != null;

        public bool UiManagerControllerReferenceIsNull =>
            reflection.GetPrivateField(uiManager, "reachDecisionController") == null;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flowSupport?.Dispose();
            owner.Dispose();
        }

        private void EnsureDecisionArea()
        {
            if (decisionArea == null)
                CreateDecisionArea("ReachDecisionArea", true);
        }
    }
}
