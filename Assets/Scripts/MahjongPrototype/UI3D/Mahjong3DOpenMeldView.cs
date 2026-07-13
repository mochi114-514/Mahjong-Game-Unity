using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: renders called meld tiles in a simple line for each player area.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Open Meld View")]
    public sealed class Mahjong3DOpenMeldView : MonoBehaviour
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Mahjong3DTileView tilePrefab;
        [SerializeField] private float tileSpacing = 1.6f;
        [SerializeField] private float meldSpacing = 1f;

        private readonly List<Mahjong3DTileView> activeTiles = new List<Mahjong3DTileView>();
        private bool warnedMissingTilePrefab;

        public void RenderOpenMelds(IReadOnlyList<OpenMeld> openMelds)
        {
            Clear();

            if (openMelds == null)
                return;

            if (tilePrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTilePrefab, "Tile prefab is not assigned.");
                return;
            }

            Transform root = spawnRoot != null ? spawnRoot : transform;
            float x = 0f;
            int tileIndex = 0;
            for (int meldIndex = 0; meldIndex < openMelds.Count; meldIndex++)
            {
                OpenMeld openMeld = openMelds[meldIndex];
                if (openMeld == null)
                    continue;

                IReadOnlyList<Tile> tiles = openMeld.Tiles;
                for (int tileOffset = 0; tileOffset < tiles.Count; tileOffset++)
                {
                    Mahjong3DTileView tile = Instantiate(tilePrefab, root);
                    tile.transform.localPosition = new Vector3(x, 0f, 0f);
                    tile.transform.localRotation = Quaternion.identity;
                    tile.transform.localScale = Vector3.one;
                    tile.Initialize(tileIndex++, tiles[tileOffset], true, false);
                    activeTiles.Add(tile);
                    x += tileSpacing;
                }

                x += meldSpacing;
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

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(Mahjong3DOpenMeldView)}: {message}", this);
        }
    }
}
