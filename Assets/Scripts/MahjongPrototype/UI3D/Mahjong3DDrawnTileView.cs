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

        private Mahjong3DTileView activeTile;
        private bool faceUp = true;
        private bool tileInteractable = true;
        private bool warnedMissingTilePrefab;

        public void Render(Tile? drawnTile, bool faceUp, bool interactable)
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

            Transform root = spawnRoot != null ? spawnRoot : transform;
            activeTile = Instantiate(tilePrefab, root);
            activeTile.transform.localPosition = Vector3.zero;
            activeTile.transform.localRotation = Quaternion.identity;
            activeTile.transform.localScale = Vector3.one;
            activeTile.Initialize(0, drawnTile.Value, faceUp, tileInteractable);
        }

        public void Rebuild(Tile? drawnTile)
        {
            Render(drawnTile, true, tileInteractable);
        }

        public void Clear()
        {
            if (activeTile != null)
                DestroyTile(activeTile);

            activeTile = null;
        }

        public void SetTileInteractable(bool interactable)
        {
            tileInteractable = faceUp && interactable;

            if (activeTile != null)
                activeTile.SetInteractable(tileInteractable);
        }

        private static void DestroyTile(Mahjong3DTileView tile)
        {
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
