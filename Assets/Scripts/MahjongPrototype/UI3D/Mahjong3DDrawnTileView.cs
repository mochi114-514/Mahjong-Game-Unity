using System;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: 3D companion for a single drawn tile view.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Drawn Tile View")]
    public sealed class Mahjong3DDrawnTileView : MonoBehaviour
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Mahjong3DTileView tilePrefab;
        [SerializeField] private float handGap = 1f;

        private Mahjong3DTileView activeTile;
        private bool faceUp = true;
        private bool tileInteractable = true;
        private bool warnedMissingTilePrefab;

        public event Action DrawnTileClicked;
        public event Action<Tile> DrawnTileHoverEntered;
        public event Action<Tile> DrawnTileHoverExited;
        public float HandGap => handGap;

        public void Render(Tile? drawnTile, bool faceUp, bool interactable)
        {
            RenderInternal(drawnTile, faceUp, interactable, false, Vector3.zero);
        }

        public void RenderAtWorldPosition(
            Tile? drawnTile,
            bool faceUp,
            bool interactable,
            Vector3 worldPosition)
        {
            RenderInternal(drawnTile, faceUp, interactable, true, worldPosition);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            if (activeTile == null)
                return;

            Transform root = GetSpawnRoot();
            activeTile.transform.localPosition = root.InverseTransformPoint(worldPosition);
        }

        private void RenderInternal(
            Tile? drawnTile,
            bool faceUp,
            bool interactable,
            bool useWorldPosition,
            Vector3 worldPosition)
        {
            this.faceUp = faceUp;
            tileInteractable = faceUp && interactable;
            Clear();

            if (!drawnTile.HasValue)
                return;

            if (tilePrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTilePrefab, "Tile prefab is not assigned.");
                return;
            }

            Transform root = GetSpawnRoot();
            activeTile = Instantiate(tilePrefab, root);
            activeTile.transform.localPosition = useWorldPosition
                ? root.InverseTransformPoint(worldPosition)
                : Vector3.zero;
            activeTile.transform.localRotation = Quaternion.identity;
            activeTile.transform.localScale = Vector3.one;
            activeTile.Initialize(0, drawnTile.Value, faceUp, tileInteractable);
            activeTile.Clicked += HandleTileClicked;
            activeTile.HoverEntered += HandleTileHoverEntered;
            activeTile.HoverExited += HandleTileHoverExited;
        }

        public void Rebuild(Tile? drawnTile)
        {
            Render(drawnTile, true, tileInteractable);
        }

        public void Clear()
        {
            if (activeTile != null)
            {
                activeTile.NotifyHoverExited();
                activeTile.Clicked -= HandleTileClicked;
                activeTile.HoverEntered -= HandleTileHoverEntered;
                activeTile.HoverExited -= HandleTileHoverExited;
                activeTile.SetDimmed(false);
                DestroyTile(activeTile);
            }

            activeTile = null;
        }

        public void SetTileInteractable(bool interactable)
        {
            tileInteractable = faceUp && interactable;

            if (activeTile != null)
                activeTile.SetInteractable(tileInteractable);

            SetDimmed(false);
        }

        public void SetReachCandidateInteractable(bool selectable)
        {
            tileInteractable = faceUp;

            if (activeTile == null)
                return;

            activeTile.SetInteractable(tileInteractable);
            activeTile.SetDimmed(!selectable);
        }

        public void SetDimmed(bool dimmed)
        {
            if (activeTile != null)
                activeTile.SetDimmed(dimmed);
        }

        private void HandleTileClicked(int _)
        {
            DrawnTileClicked?.Invoke();
        }

        private void HandleTileHoverEntered(Mahjong3DTileView tileView)
        {
            if (tileView != null && tileView.Tile.HasValue)
                DrawnTileHoverEntered?.Invoke(tileView.Tile.Value);
        }

        private void HandleTileHoverExited(Mahjong3DTileView tileView)
        {
            if (tileView != null && tileView.Tile.HasValue)
                DrawnTileHoverExited?.Invoke(tileView.Tile.Value);
        }

        private Transform GetSpawnRoot()
        {
            return spawnRoot != null ? spawnRoot : transform;
        }

        private static void DestroyTile(Mahjong3DTileView tile)
        {
            tile.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(tile.gameObject);
            else
                DestroyImmediate(tile.gameObject);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(Mahjong3DDrawnTileView)}: {message}", this);
        }
    }
}
