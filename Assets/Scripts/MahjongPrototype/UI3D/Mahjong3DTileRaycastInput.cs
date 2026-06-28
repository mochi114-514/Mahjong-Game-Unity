using UnityEngine;
using UnityEngine.EventSystems;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: raycast bridge from screen pointer input to 3D tile views.
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

        private bool warnedMissingCamera;

        private void Awake()
        {
            if (raycastCamera == null)
                raycastCamera = Camera.main;
        }

        private void Update()
        {
            if (!TryGetPrimaryPointerDown(out Vector2 screenPosition, out int pointerId))
                return;

            if (ignorePointerOverUi && IsPointerOverUi(pointerId))
                return;

            TryNotifyTileClick(screenPosition);
        }

        private bool TryGetPrimaryPointerDown(out Vector2 screenPosition, out int pointerId)
        {
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }

            screenPosition = default;
            pointerId = -1;
            return false;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            return eventSystem.IsPointerOverGameObject(pointerId);
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
