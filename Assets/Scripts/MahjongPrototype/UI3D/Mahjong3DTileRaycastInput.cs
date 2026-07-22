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
        [SerializeField] private LayerMask tableInputLayerMask;
        [SerializeField] private float maxDistance = 500f;
        [SerializeField] private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        [Header("UI Blocking")]
        [SerializeField] private bool ignorePointerOverUi = true;
        [SerializeField] private RectTransform[] selectionClearProtectedUiRects;

        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private PointerEventData pointerEventData;
        private EventSystem pointerEventSystem;
        private bool warnedMissingCamera;
        private bool syncTransformsBeforeHoverRefresh;
        private Mahjong3DTileView hoveredTileView;

        public event System.Action HoverReevaluated;
        public event System.Action TableInputSurfaceClicked;

        private void Awake()
        {
            if (raycastCamera == null)
                raycastCamera = Camera.main;
        }

        private void Update()
        {
            if (!TryGetPrimaryPointerDown(out Vector2 screenPosition))
                return;

            ProcessPointerClick(screenPosition);
        }

        private void ProcessPointerClick(Vector2 screenPosition)
        {
            if (IsPointerProtectedByUi(screenPosition))
                return;

            if (TryNotifyTileClick(screenPosition))
                return;

            if (!IsTableInputSurfaceHit(screenPosition))
                return;

            SetHoveredTile(null);
            HoverReevaluated?.Invoke();
            TableInputSurfaceClicked?.Invoke();
        }

        private void LateUpdate()
        {
            RefreshHover();
        }

        private void OnDisable()
        {
            SetHoveredTile(null);
            HoverReevaluated?.Invoke();
        }

        public void RefreshHover()
        {
            if (syncTransformsBeforeHoverRefresh)
            {
                // A hand redraw may have replaced the collider this frame.
                syncTransformsBeforeHoverRefresh = false;
                Physics.SyncTransforms();
            }

            UpdateMouseHover();
            HoverReevaluated?.Invoke();
        }

        public void RequestHoverRefresh()
        {
            syncTransformsBeforeHoverRefresh = true;
        }

        private void UpdateMouseHover()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                SetHoveredTile(null);
                return;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            if (IsPointerProtectedByUi(screenPosition))
            {
                SetHoveredTile(null);
                return;
            }

            Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
            if (cameraToUse == null)
            {
                SetHoveredTile(null);
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
                SetHoveredTile(null);
                return;
            }

            SetHoveredTile(hit.collider.GetComponentInParent<Mahjong3DTileView>());
        }

        private void SetHoveredTile(Mahjong3DTileView nextTileView)
        {
            if (hoveredTileView == nextTileView)
                return;

            Mahjong3DTileView previousTileView = hoveredTileView;
            hoveredTileView = nextTileView;
            if (previousTileView != null)
                previousTileView.NotifyHoverExited();
            if (hoveredTileView != null)
                hoveredTileView.NotifyHoverEntered();
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

        private bool IsPointerProtectedByUi(Vector2 screenPosition)
        {
            if (ignorePointerOverUi && IsPointerOverUi(screenPosition))
                return true;

            if (selectionClearProtectedUiRects == null)
                return false;

            for (int i = 0; i < selectionClearProtectedUiRects.Length; i++)
            {
                RectTransform protectedRect = selectionClearProtectedUiRects[i];
                if (protectedRect == null || !protectedRect.gameObject.activeInHierarchy)
                    continue;

                Canvas canvas = protectedRect.GetComponentInParent<Canvas>();
                Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        protectedRect,
                        screenPosition,
                        eventCamera))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryNotifyTileClick(Vector2 screenPosition)
        {
            Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
            if (cameraToUse == null)
            {
                WarnMissingCameraOnce();
                return false;
            }

            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    maxDistance,
                    tileLayerMask,
                    queryTriggerInteraction))
            {
                return false;
            }

            Mahjong3DTileView tileView = hit.collider.GetComponentInParent<Mahjong3DTileView>();
            if (tileView != null)
                tileView.NotifyClicked();

            // Any tile-layer hit, including a non-interactable opponent, river,
            // or meld tile, protects the current self selection from table clearing.
            return true;
        }

        private bool IsTableInputSurfaceHit(Vector2 screenPosition)
        {
            Camera cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
            if (cameraToUse == null)
            {
                WarnMissingCameraOnce();
                return false;
            }

            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            return Physics.Raycast(
                ray,
                maxDistance,
                tableInputLayerMask,
                queryTriggerInteraction);
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
