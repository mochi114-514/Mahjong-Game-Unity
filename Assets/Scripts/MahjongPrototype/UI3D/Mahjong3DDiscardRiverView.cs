using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: simple 3D discard river view that rebuilds tiles from discard records.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Discard River View")]
    public sealed class Mahjong3DDiscardRiverView : MonoBehaviour
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Mahjong3DTileView tilePrefab;
        [SerializeField] private int columns = 6;
        [SerializeField] private float spacingX = 0.45f;

        [FormerlySerializedAs("spacingZ")]
        [SerializeField] private float spacingY = 0.6f;

        private readonly List<Mahjong3DTileView> activeTiles = new List<Mahjong3DTileView>();
        private bool warnedMissingTilePrefab;

        public void RenderDiscardRiver(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            Clear();

            if (discards == null)
                return;

            if (tilePrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTilePrefab, "Tile prefab is not assigned.");
                return;
            }

            Transform root = spawnRoot != null ? spawnRoot : transform;
            int safeColumns = Mathf.Max(1, columns);
            int riverIndex = 0;
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                if (record.ActorSeat != dataSeat)
                    continue;

                Mahjong3DTileView tile = Instantiate(tilePrefab, root);
                int column = riverIndex % safeColumns;
                int row = riverIndex / safeColumns;
                tile.transform.localPosition = new Vector3(column * spacingX, row * spacingY, 0f);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = Vector3.one;
                tile.Initialize(riverIndex, record.Tile, true, false);
                activeTiles.Add(tile);
                riverIndex++;
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
            Debug.LogWarning($"{nameof(Mahjong3DDiscardRiverView)}: {message}", this);
        }
    }
}
