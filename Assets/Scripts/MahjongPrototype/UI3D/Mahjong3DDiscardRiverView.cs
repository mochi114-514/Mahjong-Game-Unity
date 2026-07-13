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

        [Header("Tile Spacing")]
        [FormerlySerializedAs("spacingX")]
        [SerializeField] private float verticalTileSpacing = 0.45f;
        [SerializeField] private float horizontalTileSpacing = 2.6f;

        [FormerlySerializedAs("spacingZ")]
        [SerializeField] private float spacingY = 0.6f;

        private readonly List<Mahjong3DTileView> activeTiles = new List<Mahjong3DTileView>();
        private bool warnedMissingTilePrefab;

        public void RenderDiscardRiver(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            RenderDiscardRiver(discards, null, dataSeat, false, 0);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat)
        {
            RenderDiscardRiver(discards, discardClaims, dataSeat, false, 0);
        }

        public void RenderDiscardRiver(
            IReadOnlyList<DiscardRecord> discards,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims,
            SeatId dataSeat,
            bool isReachDeclared,
            int reachDeclaredTurnIndex)
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
            float previousTileSpacing = 0f;
            float previousTileX = 0f;
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                if (record.ActorSeat != dataSeat || IsClaimed(record, discardClaims))
                    continue;

                int column = riverIndex % safeColumns;
                int row = riverIndex / safeColumns;
                bool isReachDeclarationTile = isReachDeclared &&
                                              record.TurnIndex == reachDeclaredTurnIndex;
                float tileSpacing = GetTileSpacing(isReachDeclarationTile);
                float tileX = column == 0
                    ? 0f
                    : previousTileX + ((previousTileSpacing + tileSpacing) * 0.5f);

                Mahjong3DTileView tile = Instantiate(tilePrefab, root);
                tile.transform.localPosition = new Vector3(tileX, row * spacingY, 0f);
                tile.transform.localRotation = isReachDeclarationTile
                    ? Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.identity;
                tile.transform.localScale = Vector3.one;
                tile.Initialize(riverIndex, record.Tile, true, false);
                activeTiles.Add(tile);
                previousTileSpacing = tileSpacing;
                previousTileX = tileX;
                riverIndex++;
            }
        }

        private float GetTileSpacing(bool isHorizontal)
        {
            float spacing = isHorizontal ? horizontalTileSpacing : verticalTileSpacing;
            return Mathf.Max(0.001f, spacing);
        }

        private static bool IsClaimed(
            DiscardRecord record,
            IReadOnlyDictionary<int, DiscardClaim> discardClaims)
        {
            return discardClaims != null && discardClaims.ContainsKey(record.Id);
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
