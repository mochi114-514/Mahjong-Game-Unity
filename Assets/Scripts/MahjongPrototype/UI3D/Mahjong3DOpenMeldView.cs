using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: renders fixed meld tiles in a simple line for each player area.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Open Meld View")]
    public sealed class Mahjong3DOpenMeldView : MonoBehaviour
    {
        // Based on the 0.8-scale open meld tile prefab dimensions.
        private const float HorizontalTileBottomAlignmentOffsetY = -0.24f;

        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Mahjong3DTileView tilePrefab;

        [Header("Face-down Tile Position")]
        // Matches the face-depth difference of the current 0.8-scale open meld tile prefab.
        [SerializeField] private float faceDownTileLocalZOffset = -0.307f;

        [Header("Kakan Added Tile Position")]
        // Matches the horizontal tile height of the current 0.8-scale open meld tile prefab.
        [SerializeField] private float kakanAddedTileLocalYOffset = 1.546f;

        [Header("Tile Spacing")]
        [FormerlySerializedAs("tileSpacing")]
        [SerializeField] private float verticalTileSpacing = 1.6f;
        [SerializeField] private float horizontalTileSpacing = 1.2f;

        [Header("Meld Spacing")]
        [SerializeField] private float meldSpacing = 1f;

        private readonly List<Mahjong3DTileView> activeTiles = new List<Mahjong3DTileView>();
        private bool warnedMissingTilePrefab;

        public void RenderOpenMelds(IReadOnlyList<PlayerMeld> melds)
        {
            Clear();

            if (melds == null)
                return;

            if (tilePrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTilePrefab, "Tile prefab is not assigned.");
                return;
            }

            List<List<MeldTileLayout>> meldLayouts = BuildMeldLayouts(melds);
            Transform root = spawnRoot != null ? spawnRoot : transform;
            float rightEdgeX = 0f;
            int tileIndex = 0;
            for (int meldIndex = 0; meldIndex < meldLayouts.Count; meldIndex++)
            {
                List<MeldTileLayout> meldTiles = meldLayouts[meldIndex];
                float[] tilePositions = CalculateTilePositions(meldTiles, rightEdgeX);
                for (int tileOffset = 0; tileOffset < meldTiles.Count; tileOffset++)
                {
                    MeldTileLayout meldTile = meldTiles[tileOffset];
                    Mahjong3DTileView tile = Instantiate(tilePrefab, root);
                    tile.transform.localPosition = new Vector3(
                        tilePositions[tileOffset],
                        GetTileLocalY(meldTile),
                        GetTileLocalZ(meldTile));
                    tile.transform.localRotation = GetTileRotation(meldTile);
                    tile.transform.localScale = Vector3.one;
                    tile.Initialize(tileIndex++, meldTile.Tile, meldTile.IsFaceUp, false);
                    activeTiles.Add(tile);
                }

                if (meldIndex + 1 < meldLayouts.Count)
                {
                    float leftEdgeX = tilePositions[0] - (GetTileSpacing(meldTiles[0]) * 0.5f);
                    List<MeldTileLayout> nextMeldTiles = meldLayouts[meldIndex + 1];
                    float nextRightmostHalfWidth =
                        GetTileSpacing(nextMeldTiles[GetRightmostTileIndex(nextMeldTiles)]) * 0.5f;
                    rightEdgeX = leftEdgeX - meldSpacing - nextRightmostHalfWidth;
                }
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

        private List<List<MeldTileLayout>> BuildMeldLayouts(IReadOnlyList<PlayerMeld> melds)
        {
            List<List<MeldTileLayout>> layouts = new List<List<MeldTileLayout>>();
            for (int meldIndex = 0; meldIndex < melds.Count; meldIndex++)
            {
                PlayerMeld meld = melds[meldIndex];
                if (meld == null ||
                    (meld.Type != PlayerMeldType.Chi &&
                        meld.Type != PlayerMeldType.Pon &&
                        meld.Type != PlayerMeldType.Daiminkan &&
                        meld.Type != PlayerMeldType.Ankan &&
                        meld.Type != PlayerMeldType.Kakan))
                    continue;

                layouts.Add(BuildMeldTileLayout(meld));
            }

            return layouts;
        }

        private static List<MeldTileLayout> BuildMeldTileLayout(PlayerMeld meld)
        {
            IReadOnlyList<Tile> tiles = meld.PhysicalTiles;
            List<MeldTileLayout> layout = new List<MeldTileLayout>(tiles.Count);
            if (meld.Type == PlayerMeldType.Chi)
            {
                int calledTileIndex = FindCalledTileIndex(tiles, meld.AcquiredTile.Value);
                if (calledTileIndex >= 0)
                    layout.Add(new MeldTileLayout(tiles[calledTileIndex], true, true));

                for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
                {
                    if (tileIndex != calledTileIndex)
                        layout.Add(new MeldTileLayout(tiles[tileIndex], false, true));
                }

                return layout;
            }

            int ponCalledTileIndex = meld.HasDiscardSource
                ? ResolvePonCalledTileIndex(meld)
                : -1;
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                bool isKakanAddedTile =
                    meld.Type == PlayerMeldType.Kakan && tileIndex == tiles.Count - 1;
                bool isFaceUp = meld.Type != PlayerMeldType.Ankan ||
                                (tileIndex != 0 && tileIndex != tiles.Count - 1);
                layout.Add(new MeldTileLayout(
                    tiles[tileIndex],
                    tileIndex == ponCalledTileIndex || isKakanAddedTile,
                    isFaceUp,
                    isKakanAddedTile,
                    isKakanAddedTile ? ponCalledTileIndex : -1));
            }

            return layout;
        }

        private float[] CalculateTilePositions(
            IReadOnlyList<MeldTileLayout> meldTiles,
            float rightmostTileX)
        {
            float[] positions = new float[meldTiles.Count];
            int rightmostTileIndex = GetRightmostTileIndex(meldTiles);
            positions[rightmostTileIndex] = rightmostTileX;
            for (int tileIndex = rightmostTileIndex - 1; tileIndex >= 0; tileIndex--)
            {
                float spacing = (GetTileSpacing(meldTiles[tileIndex]) +
                                 GetTileSpacing(meldTiles[tileIndex + 1])) *
                                0.5f;
                positions[tileIndex] = positions[tileIndex + 1] - spacing;
            }

            for (int tileIndex = rightmostTileIndex + 1; tileIndex < meldTiles.Count; tileIndex++)
            {
                MeldTileLayout meldTile = meldTiles[tileIndex];
                if (meldTile.IsKakanAddedTile)
                    positions[tileIndex] = positions[meldTile.OverlappedTileIndex];
            }

            return positions;
        }

        private float GetTileLocalY(MeldTileLayout meldTile)
        {
            float localY = meldTile.IsCalledTile ? HorizontalTileBottomAlignmentOffsetY : 0f;
            return meldTile.IsKakanAddedTile ? localY + kakanAddedTileLocalYOffset : localY;
        }

        private float GetTileLocalZ(MeldTileLayout meldTile)
        {
            if (!meldTile.IsFaceUp)
                return faceDownTileLocalZOffset;

            return 0f;
        }

        private static int GetRightmostTileIndex(IReadOnlyList<MeldTileLayout> meldTiles)
        {
            for (int tileIndex = meldTiles.Count - 1; tileIndex >= 0; tileIndex--)
            {
                if (!meldTiles[tileIndex].IsKakanAddedTile)
                    return tileIndex;
            }

            return meldTiles.Count - 1;
        }

        private static Quaternion GetTileRotation(MeldTileLayout meldTile)
        {
            Quaternion rotation = meldTile.IsCalledTile
                ? Quaternion.Euler(0f, 0f, 90f)
                : Quaternion.identity;
            return meldTile.IsFaceUp
                ? rotation
                : rotation * Quaternion.Euler(0f, 180f, 0f);
        }

        private float GetTileSpacing(MeldTileLayout meldTile)
        {
            float spacing = meldTile.IsCalledTile ? horizontalTileSpacing : verticalTileSpacing;
            return Mathf.Max(0.001f, spacing);
        }

        private static int FindCalledTileIndex(IReadOnlyList<Tile> tiles, Tile calledTile)
        {
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                if (tiles[tileIndex] == calledTile)
                    return tileIndex;
            }

            return -1;
        }

        private static int ResolvePonCalledTileIndex(PlayerMeld meld)
        {
            int relativeSourceSeat =
                ((int)meld.SourceSeat.Value - (int)meld.OwnerSeat + 4) % 4;
            switch (relativeSourceSeat)
            {
                case 3:
                    return 0;
                case 2:
                    return 1;
                case 1:
                    return 2;
                default:
                    return 1;
            }
        }

        private readonly struct MeldTileLayout
        {
            public MeldTileLayout(
                Tile tile,
                bool isCalledTile,
                bool isFaceUp,
                bool isKakanAddedTile = false,
                int overlappedTileIndex = -1)
            {
                Tile = tile;
                IsCalledTile = isCalledTile;
                IsFaceUp = isFaceUp;
                IsKakanAddedTile = isKakanAddedTile;
                OverlappedTileIndex = overlappedTileIndex;
            }

            public Tile Tile { get; }
            public bool IsCalledTile { get; }
            public bool IsFaceUp { get; }
            public bool IsKakanAddedTile { get; }
            public int OverlappedTileIndex { get; }
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
