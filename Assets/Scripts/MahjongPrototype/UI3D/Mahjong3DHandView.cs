using System.Collections.Generic;
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

            Transform root = spawnRoot != null ? spawnRoot : transform;
            float startX = -((testTileCount - 1) * spacing) * 0.5f;

            for (int i = 0; i < testTileCount; i++)
            {
                Mahjong3DTileView tile = Instantiate(tilePrefab, root);
                tile.transform.localPosition = new Vector3(startX + (i * spacing), 0f, 0f);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = Vector3.one;
                tile.Initialize(i);
                activeTiles.Add(tile);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                Mahjong3DTileView tile = activeTiles[i];
                if (tile != null)
                    DestroyTile(tile);
            }

            activeTiles.Clear();
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
