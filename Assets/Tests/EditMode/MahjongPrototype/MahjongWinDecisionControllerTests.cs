using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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
        public void SetWinDecision_ChangesWinButtonLabelAndClearsItWhenHidden()
        {
            GameObject controllerObject = new GameObject("WinDecisionControllerTest");
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
                SetPrivateField(controller, "winButtonLabel", label);
                controllerObject.SetActive(true);

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Tsumo"));
                Assert.That(GetProperty(label, "text"), Is.EqualTo("ツモ"));
                Assert.That(root.activeSelf, Is.True);

                Invoke(
                    controller,
                    "SetWinDecision",
                    true,
                    Enum.Parse(Type.GetType(WinTypeName, true), "Ron"));
                Assert.That(GetProperty(label, "text"), Is.EqualTo("ロン"));
                Assert.That(root.activeSelf, Is.True);

                Invoke(controller, "SetWinDecision", false, null);
                Assert.That(GetProperty(label, "text"), Is.EqualTo("和了"));
                Assert.That(root.activeSelf, Is.False);
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
