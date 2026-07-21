using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: spawns identical 3D tile prefabs for SelfBottom hand layout verification.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Hand View")]
    public sealed class Mahjong3DHandView : MonoBehaviour
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Mahjong3DTileView tilePrefab;
        [SerializeField] private int testTileCount = 14;
        [SerializeField] private float spacing = 0.45f;

        private readonly List<Mahjong3DTileView> activeTiles = new List<Mahjong3DTileView>();

        public event Action<int> TileClicked;
        public event Action<int, Tile> TileHoverEntered;
        public event Action<int, Tile> TileHoverExited;

        public void RenderHand(IReadOnlyList<Tile> handTiles, bool faceUp, bool interactable)
        {
            Clear();

            if (handTiles == null)
                return;

            if (tilePrefab == null)
            {
                Debug.LogWarning($"{nameof(Mahjong3DHandView)}: Tile prefab is not assigned.", this);
                return;
            }

            Transform root = GetSpawnRoot();

            for (int i = 0; i < handTiles.Count; i++)
            {
                Mahjong3DTileView tile = InstantiateTile(root, i);
                tile.Initialize(i, handTiles[i], faceUp, interactable);
                tile.Clicked += HandleTileClicked;
                tile.HoverEntered += HandleTileHoverEntered;
                tile.HoverExited += HandleTileHoverExited;
                activeTiles.Add(tile);
            }
        }

        public void SpawnTestTiles()
        {
            Clear();

            if (tilePrefab == null)
            {
                Debug.LogWarning($"{nameof(Mahjong3DHandView)}: Tile prefab is not assigned.", this);
                return;
            }

            if (testTileCount <= 0)
                return;

            Transform root = GetSpawnRoot();

            for (int i = 0; i < testTileCount; i++)
            {
                Mahjong3DTileView tile = InstantiateTile(root, i);
                tile.Initialize(i);
                tile.HoverEntered += HandleTileHoverEntered;
                tile.HoverExited += HandleTileHoverExited;
                activeTiles.Add(tile);
            }
        }

        public void SetTilesInteractable(bool interactable)
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Mahjong3DTileView tile = activeTiles[i];
                if (tile == null)
                    continue;

                tile.SetInteractable(interactable);
                tile.SetDimmed(false);
            }
        }

        public void SetTileInteractableByIndices(IReadOnlyCollection<int> handIndices)
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Mahjong3DTileView tile = activeTiles[i];
                if (tile == null)
                    continue;

                tile.SetInteractable(ContainsIndex(handIndices, i));
                tile.SetDimmed(false);
            }
        }

        public void SetReachCandidateInteractableByIndices(IReadOnlyCollection<int> handIndices)
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Mahjong3DTileView tile = activeTiles[i];
                if (tile == null)
                    continue;

                bool selectable = ContainsIndex(handIndices, i);
                tile.SetInteractable(true);
                tile.SetDimmed(!selectable);
            }
        }

        public void ClearDimmed()
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Mahjong3DTileView tile = activeTiles[i];
                if (tile != null)
                    tile.SetDimmed(false);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Mahjong3DTileView tile = activeTiles[i];
                if (tile != null)
                {
                    tile.NotifyHoverExited();
                    tile.Clicked -= HandleTileClicked;
                    tile.HoverEntered -= HandleTileHoverEntered;
                    tile.HoverExited -= HandleTileHoverExited;
                    tile.SetDimmed(false);
                    DestroyTile(tile);
                }
            }

            activeTiles.Clear();
        }

        public Vector3 GetTrailingTileWorldPosition(float gap)
        {
            Transform root = GetSpawnRoot();
            int lastTileIndex = activeTiles.Count - 1;
            float x = lastTileIndex >= 0 ? (lastTileIndex * spacing) + gap : 0f;
            return root.TransformPoint(new Vector3(x, 0f, 0f));
        }

        private void HandleTileClicked(int handIndex)
        {
            TileClicked?.Invoke(handIndex);
        }

        private void HandleTileHoverEntered(Mahjong3DTileView tileView)
        {
            if (tileView != null && tileView.Tile.HasValue)
                TileHoverEntered?.Invoke(tileView.HandIndex, tileView.Tile.Value);
        }

        private void HandleTileHoverExited(Mahjong3DTileView tileView)
        {
            if (tileView != null && tileView.Tile.HasValue)
                TileHoverExited?.Invoke(tileView.HandIndex, tileView.Tile.Value);
        }

        private static bool ContainsIndex(IReadOnlyCollection<int> indices, int index)
        {
            if (indices == null)
                return false;

            foreach (int candidateIndex in indices)
            {
                if (candidateIndex == index)
                    return true;
            }

            return false;
        }

        private Transform GetSpawnRoot()
        {
            return spawnRoot != null ? spawnRoot : transform;
        }

        private Mahjong3DTileView InstantiateTile(Transform root, int index)
        {
            Mahjong3DTileView tile = Instantiate(tilePrefab, root);
            tile.transform.localPosition = new Vector3(index * spacing, 0f, 0f);
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
            return tile;
        }

        private static void DestroyTile(Mahjong3DTileView tile)
        {
            if (Application.isPlaying)
                Destroy(tile.gameObject);
            else
                DestroyImmediate(tile.gameObject);
        }
    }
}
