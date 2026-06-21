using System;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Drawn Tile View")]
    public sealed class MahjongDrawnTileView : MonoBehaviour
    {
        [Header("Drawn Tile")]
        [SerializeField] private RectTransform drawnTileContainer;
        [SerializeField] private TileButtonView tileButtonPrefab;

        private TileButtonView activeTileButton;
        private bool warnedMissingDrawnTileContainer;
        private bool warnedMissingTileButtonPrefab;

        public event Action DrawnTileClicked;

        public void Configure(RectTransform container, TileButtonView prefab)
        {
            drawnTileContainer = container;
            tileButtonPrefab = prefab;
        }

        public void ConfigureMissingReferences(RectTransform fallbackContainer, TileButtonView fallbackPrefab)
        {
            if (drawnTileContainer == null)
                drawnTileContainer = fallbackContainer;

            if (tileButtonPrefab == null)
                tileButtonPrefab = fallbackPrefab;
        }

        public void Rebuild(Tile? drawnTile)
        {
            Clear();

            if (!drawnTile.HasValue)
                return;

            if (drawnTileContainer == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileContainer, "Drawn tile container is not assigned.");
                return;
            }

            if (tileButtonPrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTileButtonPrefab, "TileButtonView prefab is not assigned.");
                return;
            }

            activeTileButton = Instantiate(tileButtonPrefab, drawnTileContainer);
            activeTileButton.Initialize(0, drawnTile.Value, HandleDrawnTileClicked);
        }

        public void Clear()
        {
            if (activeTileButton != null)
                Destroy(activeTileButton.gameObject);

            activeTileButton = null;
        }

        private void HandleDrawnTileClicked(int _)
        {
            DrawnTileClicked?.Invoke();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongDrawnTileView)}: {message}", this);
        }
    }
}
