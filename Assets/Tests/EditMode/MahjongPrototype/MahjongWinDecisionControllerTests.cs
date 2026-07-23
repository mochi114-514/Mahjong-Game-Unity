using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongWinDecisionControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongWinDecisionController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string WinTypeName =
            "MahjongPrototype.Domain.WinType, Assembly-CSharp";

        [Test]
        public void SetWinDecision_WithAssignedReferences_ChangesLabelAndVisibilityWhenNamesDiffer()
        {
            GameObject controllerObject = new GameObject("WinDecisionControllerTest");
            controllerObject.SetActive(false);
            GameObject root = new GameObject("RenamedWinDecisionRoot");
            root.transform.SetParent(controllerObject.transform);
            GameObject labelObject = new GameObject("RenamedWinLabel");
            labelObject.transform.SetParent(root.transform);
            GameObject declineLabelObject = new GameObject("InspectorConfiguredDeclineLabel");
            declineLabelObject.transform.SetParent(root.transform);

            try
            {
                Component label = labelObject.AddComponent(
                    Type.GetType(TextMeshProUguiTypeName, true));
                Component declineLabel = declineLabelObject.AddComponent(
                    Type.GetType(TextMeshProUguiTypeName, true));
                SetProperty(declineLabel, "text", "スキップ");
                Component controller = controllerObject.AddComponent(
                    Type.GetType(ControllerTypeName, true));
                SetPrivateField(controller, "winDecisionRoot", root);
                SetPrivateField(controller, "winButtonLabel", label);
                controllerObject.SetActive(true);

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Tsumo"));
                Assert.That(GetProperty(label, "text"), Is.EqualTo("ツモ"));
                Assert.That(GetProperty(declineLabel, "text"), Is.EqualTo("スキップ"));
                Assert.That(root.activeSelf, Is.True);

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Ron"));
                Assert.That(GetProperty(label, "text"), Is.EqualTo("ロン"));
                Assert.That(GetProperty(declineLabel, "text"), Is.EqualTo("スキップ"));
                Assert.That(root.activeSelf, Is.True);

                Invoke(controller, "SetWinDecision", false, null);
                Assert.That(GetProperty(label, "text"), Is.EqualTo("和了"));
                Assert.That(GetProperty(declineLabel, "text"), Is.EqualTo("スキップ"));
                Assert.That(root.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void SetWinDecision_WithoutLabel_WarnsAndDoesNotAutoFindWinButtonChild()
        {
            GameObject controllerObject = new GameObject("WinDecisionMissingLabelTest");
            controllerObject.SetActive(false);
            GameObject root = new GameObject("WinDecisionRoot");
            root.transform.SetParent(controllerObject.transform);
            GameObject winButton = new GameObject("WinButton");
            winButton.transform.SetParent(root.transform);
            GameObject labelObject = new GameObject("Text (TMP)");
            labelObject.transform.SetParent(winButton.transform);

            try
            {
                Component label = labelObject.AddComponent(
                    Type.GetType(TextMeshProUguiTypeName, true));
                Component controller = controllerObject.AddComponent(
                    Type.GetType(ControllerTypeName, true));
                SetPrivateField(controller, "winDecisionRoot", root);
                controllerObject.SetActive(true);

                LogAssert.Expect(
                    LogType.Warning,
                    "MahjongWinDecisionController: WinButtonLabel is not assigned.");

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Tsumo"));

                Assert.That(GetProperty(label, "text"), Is.Not.EqualTo("ツモ"));
                Assert.That(root.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void AbortiveDrawDecision_UsesDrawLabel_ThenRestoresTsumoAndRonLabels()
        {
            GameObject controllerObject =
                new GameObject("AbortiveDrawDecisionControllerTest");
            controllerObject.SetActive(false);
            GameObject root = new GameObject("WinDecisionRoot");
            root.transform.SetParent(controllerObject.transform);
            GameObject labelObject = new GameObject("WinLabel");
            labelObject.transform.SetParent(root.transform);

            try
            {
                Component label = labelObject.AddComponent(
                    Type.GetType(TextMeshProUguiTypeName, true));
                Component controller = controllerObject.AddComponent(
                    Type.GetType(ControllerTypeName, true));
                SetPrivateField(controller, "winDecisionRoot", root);
                SetPrivateField(controller, "winButtonLabel", label);
                controllerObject.SetActive(true);

                Invoke(controller, "SetAbortiveDrawDecision", true);
                Assert.That(GetProperty(label, "text"), Is.EqualTo("流局"));
                Assert.That(root.activeSelf, Is.True);

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Tsumo"));
                Assert.That(GetProperty(label, "text"), Is.EqualTo("ツモ"));

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Ron"));
                Assert.That(GetProperty(label, "text"), Is.EqualTo("ロン"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerObject);
            }
        }

        [Test]
        public void SetVisibleTrue_DoesNotGetHiddenByEnable()
        {
            GameObject controllerObject = new GameObject("WinDecisionEnableVisibilityTest");
            controllerObject.SetActive(false);
            GameObject root = new GameObject("RenamedWinDecisionRoot");
            root.transform.SetParent(controllerObject.transform);
            GameObject labelObject = new GameObject("RenamedWinLabel");
            labelObject.transform.SetParent(root.transform);

            try
            {
                Component label = labelObject.AddComponent(
                    Type.GetType(TextMeshProUguiTypeName, true));
                Component controller = controllerObject.AddComponent(
                    Type.GetType(ControllerTypeName, true));
                SetPrivateField(controller, "winDecisionRoot", root);
                SetPrivateField(controller, "winButtonLabel", label);

                Invoke(controller, "SetVisible", true);
                controllerObject.SetActive(true);

                Assert.That(root.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerObject);
            }
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
    }
}
