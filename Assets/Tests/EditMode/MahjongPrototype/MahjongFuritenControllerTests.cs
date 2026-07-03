using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongFuritenControllerTests
    {
        private const string ControllerTypeName =
            "MahjongPrototype.UI.MahjongFuritenController, Assembly-CSharp";
        private const string TextMeshProUguiTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        [Test]
        public void SetVisibleTrue_ActivatesTextAndInitializesLabel()
        {
            GameObject parent = new GameObject("TenpaiParent");
            GameObject textObject = new GameObject("RenamedFuritenIndicator");
            textObject.transform.SetParent(parent.transform);
            textObject.SetActive(false);

            try
            {
                Component text = textObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
                Component controller = textObject.AddComponent(Type.GetType(ControllerTypeName, true));
                SetPrivateField(controller, "furitenText", text);

                Invoke(controller, "SetVisible", true);

                Assert.That(parent.activeSelf, Is.True);
                Assert.That(textObject.activeSelf, Is.True);
                Assert.That(GetProperty(text, "text"), Is.EqualTo("フリテン"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SetVisibleFalseAndClear_HideOnlyAssignedTextObject()
        {
            GameObject parent = new GameObject("TenpaiParent");
            GameObject textObject = new GameObject("RenamedFuritenIndicator");
            GameObject neighborObject = new GameObject("RenamedZeroHanIndicator");
            textObject.transform.SetParent(parent.transform);
            neighborObject.transform.SetParent(parent.transform);
            neighborObject.SetActive(true);

            try
            {
                Component text = textObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
                Component neighborText = neighborObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
                SetProperty(neighborText, "text", "neighbor");
                Component controller = textObject.AddComponent(Type.GetType(ControllerTypeName, true));
                SetPrivateField(controller, "furitenText", text);

                Invoke(controller, "SetVisible", true);
                Invoke(controller, "SetVisible", false);

                Assert.That(parent.activeSelf, Is.True);
                Assert.That(textObject.activeSelf, Is.False);
                Assert.That(neighborObject.activeSelf, Is.True);
                Assert.That(GetProperty(neighborText, "text"), Is.EqualTo("neighbor"));

                Invoke(controller, "SetVisible", true);
                Invoke(controller, "Clear");

                Assert.That(textObject.activeSelf, Is.False);
                Assert.That(neighborObject.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SetVisible_UsesSameGameObjectTmpWhenReferenceIsNotAssigned()
        {
            GameObject textObject = new GameObject("NameIndependentFuritenText");
            textObject.SetActive(false);

            try
            {
                Component text = textObject.AddComponent(Type.GetType(TextMeshProUguiTypeName, true));
                Component controller = textObject.AddComponent(Type.GetType(ControllerTypeName, true));

                Invoke(controller, "SetVisible", true);

                Assert.That(textObject.activeSelf, Is.True);
                Assert.That(GetProperty(text, "text"), Is.EqualTo("フリテン"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(textObject);
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
