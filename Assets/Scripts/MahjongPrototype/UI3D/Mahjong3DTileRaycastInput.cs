using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: raycast bridge from Input System pointer input to 3D tile views.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Tile Raycast Input")]
    public sealed class Mahjong3DTileRaycastInput : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask tileLayerMask = ~0;
        [SerializeField] private float maxDistance = 500f;
        [SerializeField] private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        [Header("UI Blocking")]
        [SerializeField] private bool ignorePointerOverUi = true;

        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private PointerEventData pointerEventData;
        private EventSystem pointerEventSystem;
        private bool warnedMissingCamera;

        private void Awake()
        {
            if (raycastCamera == null)
                raycastCamera = Camera.main;
        }

        private void Update()
        {
            if (!TryGetPrimaryPointerDown(out Vector2 screenPosition))
                return;

            if (ignorePointerOverUi && IsPointerOverUi(screenPosition))
                return;

            TryNotifyTileClick(screenPosition);
        }

        private bool TryGetPrimaryPointerDown(out Vector2 screenPosition)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPosition = mouse.position.ReadValue();
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame)
                        continue;

                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }

            screenPosition = default;
            return false;
        }

        private bool IsPointerOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (pointerEventData == null || pointerEventSystem != eventSystem)
            {
                pointerEventData = new PointerEventData(eventSystem);
                pointerEventSystem = eventSystem;
            }

            pointerEventData.Reset();
            pointerEventData.position = screenPosition;

            uiRaycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, uiRaycastResults);
            return uiRaycastResults.Count > 0;
        }

        private void TryNotifyTileClick(Vector2 screenPosition)
        {
            Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
            if (cameraToUse == null)
            {
                WarnMissingCameraOnce();
                return;
            }

            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maxDistance,
                    tileLayerMask,
                    queryTriggerInteraction))
            {
                return;
            }

            Mahjong3DTileView tileView = hit.collider.GetComponentInParent<Mahjong3DTileView>();
            if (tileView == null)
                return;

            tileView.NotifyClicked();
        }

        private void WarnMissingCameraOnce()
        {
            if (warnedMissingCamera)
                return;

            warnedMissingCamera = true;
            Debug.LogWarning(
                $"{nameof(Mahjong3DTileRaycastInput)}: Raycast camera is not assigned and Camera.main was not found.",
                this);
        }
    }
}
