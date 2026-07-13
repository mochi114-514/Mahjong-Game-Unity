using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: renders called meld tiles in a simple line for each player area.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Open Meld View")]
    public sealed class Mahjong3DOpenMeldView : MonoBehaviour
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private Mahjong3DTileView tilePrefab;

        [Header("Tile Spacing")]
        [FormerlySerializedAs("tileSpacing")]
        [SerializeField] private float verticalTileSpacing = 1.6f;
        [SerializeField] private float horizontalTileSpacing = 1.2f;

        [Header("Meld Spacing")]
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

            List<List<MeldTileLayout>> meldLayouts = BuildMeldLayouts(openMelds);
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
                        0f,
                        0f);
                    tile.transform.localRotation = meldTile.IsCalledTile
                        ? Quaternion.Euler(0f, 0f, 90f)
                        : Quaternion.identity;
                    tile.transform.localScale = Vector3.one;
                    tile.Initialize(tileIndex++, meldTile.Tile, true, false);
                    activeTiles.Add(tile);
                }

                if (meldIndex + 1 < meldLayouts.Count)
                {
                    float leftEdgeX = tilePositions[0] - (GetTileSpacing(meldTiles[0]) * 0.5f);
                    List<MeldTileLayout> nextMeldTiles = meldLayouts[meldIndex + 1];
                    float nextRightmostHalfWidth =
                        GetTileSpacing(nextMeldTiles[nextMeldTiles.Count - 1]) * 0.5f;
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

        private List<List<MeldTileLayout>> BuildMeldLayouts(IReadOnlyList<OpenMeld> openMelds)
        {
            List<List<MeldTileLayout>> layouts = new List<List<MeldTileLayout>>();
            for (int meldIndex = 0; meldIndex < openMelds.Count; meldIndex++)
            {
                OpenMeld openMeld = openMelds[meldIndex];
                if (openMeld == null)
                    continue;

                layouts.Add(BuildMeldTileLayout(openMeld));
            }

            return layouts;
        }

        private static List<MeldTileLayout> BuildMeldTileLayout(OpenMeld openMeld)
        {
            IReadOnlyList<Tile> tiles = openMeld.Tiles;
            List<MeldTileLayout> layout = new List<MeldTileLayout>(tiles.Count);
            if (openMeld.Type == OpenMeldType.Chi)
            {
                int calledTileIndex = FindCalledTileIndex(tiles, openMeld.CalledTile);
                if (calledTileIndex >= 0)
                    layout.Add(new MeldTileLayout(tiles[calledTileIndex], true));

                for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
                {
                    if (tileIndex != calledTileIndex)
                        layout.Add(new MeldTileLayout(tiles[tileIndex], false));
                }

                return layout;
            }

            int ponCalledTileIndex = ResolvePonCalledTileIndex(openMeld);
            for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
            {
                layout.Add(new MeldTileLayout(tiles[tileIndex], tileIndex == ponCalledTileIndex));
            }

            return layout;
        }

        private float[] CalculateTilePositions(
            IReadOnlyList<MeldTileLayout> meldTiles,
            float rightmostTileX)
        {
            float[] positions = new float[meldTiles.Count];
            int lastIndex = meldTiles.Count - 1;
            positions[lastIndex] = rightmostTileX;
            for (int tileIndex = lastIndex - 1; tileIndex >= 0; tileIndex--)
            {
                float spacing = (GetTileSpacing(meldTiles[tileIndex]) +
                                 GetTileSpacing(meldTiles[tileIndex + 1])) *
                                0.5f;
                positions[tileIndex] = positions[tileIndex + 1] - spacing;
            }

            return positions;
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

        private static int ResolvePonCalledTileIndex(OpenMeld openMeld)
        {
            int relativeSourceSeat =
                ((int)openMeld.SourceSeat - (int)openMeld.CallerSeat + 4) % 4;
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
            public MeldTileLayout(Tile tile, bool isCalledTile)
            {
                Tile = tile;
                IsCalledTile = isCalledTile;
            }

            public Tile Tile { get; }
            public bool IsCalledTile { get; }
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
