using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Unity
{
    internal sealed class UnityObjectTestOwner : IDisposable
    {
        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();
        private bool disposed;

        public T Own<T>(T value)
            where T : UnityEngine.Object
        {
            Register(value);
            return value;
        }

        public void Register(object value)
        {
            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (disposed || unityObject == null || ownedObjects.Contains(unityObject))
                return;

            ownedObjects.Add(unityObject);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            for (int i = ownedObjects.Count - 1; i >= 0; i--)
            {
                if (ownedObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
            }

            ownedObjects.Clear();
        }
    }

    internal static class TmpInputFieldTestFactory
    {
        private const string TmpInputFieldTypeName =
            "TMPro.TMP_InputField, Unity.TextMeshPro";
        private const string TmpTextTypeName =
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro";

        public static Component Create(
            ReflectionTestAccess reflection,
            Transform parent,
            string name)
        {
            GameObject inputObject = new GameObject(name, typeof(RectTransform));
            inputObject.transform.SetParent(parent, false);
            Component input = inputObject.AddComponent(
                reflection.RequireType(TmpInputFieldTypeName));

            GameObject viewportObject = new GameObject(
                "TextViewport",
                typeof(RectTransform));
            viewportObject.transform.SetParent(inputObject.transform, false);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(viewportObject.transform, false);
            Component text = textObject.AddComponent(reflection.RequireType(TmpTextTypeName));

            reflection.SetProperty(
                input,
                "textViewport",
                viewportObject.GetComponent<RectTransform>());
            reflection.SetProperty(input, "textComponent", text);
            return input;
        }
    }
}
