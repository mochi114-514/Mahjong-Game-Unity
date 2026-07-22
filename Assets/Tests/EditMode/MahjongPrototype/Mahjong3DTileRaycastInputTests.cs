using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DTileRaycastInputTests
    {
        private const string MainScenePath = "Assets/Scenes/Mahjong Prototype.unity";
        private const string RaycastInputTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileRaycastInput, Assembly-CSharp";
        private const string TileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";

        [Test]
        public void PointerPriority_ProtectsUiAndTiles_ClearsOnlyOnTableSurface()
        {
            GameObject root = new GameObject("Mahjong3DTileRaycastInputTest");
            GameObject cameraObject = new GameObject("RaycastCamera");
            GameObject tableObject = new GameObject("TableInputSurface");
            GameObject tileObject = new GameObject("NonInteractableTile");
            GameObject canvasObject = new GameObject("ProtectedCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject protectedObject = new GameObject("ProtectedRect", typeof(RectTransform));
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                tableObject.layer = 7;
                tableObject.transform.position = Vector3.zero;
                BoxCollider tableCollider = tableObject.AddComponent<BoxCollider>();
                tableCollider.size = new Vector3(10f, 0.5f, 10f);

                tileObject.layer = 6;
                tileObject.transform.position = new Vector3(0f, 1f, 0f);
                tileObject.AddComponent<BoxCollider>();
                tileObject.AddComponent(RequireType(TileViewTypeName));

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                RectTransform protectedRect = protectedObject.GetComponent<RectTransform>();
                protectedRect.SetParent(canvasObject.transform, false);
                protectedRect.anchorMin = new Vector2(0.5f, 0.5f);
                protectedRect.anchorMax = new Vector2(0.5f, 0.5f);
                protectedRect.anchoredPosition = Vector2.zero;
                protectedRect.sizeDelta = new Vector2(10000f, 10000f);

                Component input = root.AddComponent(RequireType(RaycastInputTypeName));
                SetPrivateField(input, "raycastCamera", camera);
                SetPrivateField(input, "tileLayerMask", (LayerMask)(1 << 6));
                SetPrivateField(input, "tableInputLayerMask", (LayerMask)(1 << 7));
                SetPrivateField(input, "selectionClearProtectedUiRects", new[] { protectedRect });

                int tableClicks = 0;
                EventInfo tableEvent = input.GetType().GetEvent("TableInputSurfaceClicked");
                Assert.That(tableEvent, Is.Not.Null);
                Action handler = () => tableClicks++;
                tableEvent.AddEventHandler(input, handler);

                Physics.SyncTransforms();
                Vector2 tileScreenPoint = camera.WorldToScreenPoint(tileObject.transform.position);
                Invoke(input, "ProcessPointerClick", tileScreenPoint);
                Assert.That(tableClicks, Is.Zero, "protected UI");

                protectedObject.SetActive(false);
                Physics.SyncTransforms();
                Invoke(input, "ProcessPointerClick", tileScreenPoint);
                Assert.That(tableClicks, Is.Zero, "non-interactable tile");

                Vector2 tableScreenPoint = camera.WorldToScreenPoint(new Vector3(3f, 0f, 0f));
                Invoke(input, "ProcessPointerClick", tableScreenPoint);
                Assert.That(tableClicks, Is.EqualTo(1), "table surface");

                Vector2 backgroundScreenPoint = camera.WorldToScreenPoint(new Vector3(20f, 0f, 0f));
                Invoke(input, "ProcessPointerClick", backgroundScreenPoint);
                Assert.That(tableClicks, Is.EqualTo(1), "background");

                tableEvent.RemoveEventHandler(input, handler);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(protectedObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(tileObject);
                UnityEngine.Object.DestroyImmediate(tableObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MainScene_ConfiguresSimpleInvisibleTableSurfaceAndCandidateUiProtection()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            try
            {
                Component input = FindSceneComponent(scene, RequireType(RaycastInputTypeName));
                SerializedObject serializedInput = new SerializedObject(input);
                SerializedProperty protectedRects = serializedInput.FindProperty(
                    "selectionClearProtectedUiRects");
                GameObject table = FindSceneGameObject(scene, "TableInputSurface");

                Assert.That(table, Is.Not.Null);
                Assert.That(LayerMask.LayerToName(table.layer), Is.EqualTo("MahjongTableInput"));
                Assert.That(table.GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(table.GetComponent<Renderer>(), Is.Null);
                Assert.That(
                    serializedInput.FindProperty("tableInputLayerMask").intValue,
                    Is.EqualTo(1 << table.layer));
                Assert.That(serializedInput.FindProperty("ignorePointerOverUi").boolValue, Is.True);
                Assert.That(protectedRects.arraySize, Is.EqualTo(1));
                Assert.That(
                    protectedRects.GetArrayElementAtIndex(0).objectReferenceValue.name,
                    Is.EqualTo("WinningCandidateRoot"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Component FindSceneComponent(Scene scene, Type type)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Component component = root.GetComponentInChildren(type, true);
                if (component != null)
                    return component;
            }

            Assert.Fail($"Scene component not found: {type.FullName}");
            return null;
        }

        private static GameObject FindSceneGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == name)
                        return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName, true);
            Assert.That(type, Is.Not.Null);
            return type;
        }
    }
}
