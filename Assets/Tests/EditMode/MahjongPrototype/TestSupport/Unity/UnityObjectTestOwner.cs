using System;
using System.Collections.Generic;
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
}
