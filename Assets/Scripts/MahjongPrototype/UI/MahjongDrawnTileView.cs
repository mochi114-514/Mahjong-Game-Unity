using System;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Drawn Tile View")]
    public sealed class MahjongDrawnTileView : MonoBehaviour
    {
        [Header("Drawn Tile")]
        [SerializeField] private RectTransform drawnTileContainer;
        [FormerlySerializedAs("tileButtonPrefab")]
        [SerializeField] private TileButtonView faceUpTileButtonPrefab;
        [SerializeField] private TileButtonView faceDownTileButtonPrefab;

        private TileButtonView activeTileButton;
        private bool faceUp = true;
        private bool tileInteractable = true;
        private bool warnedMissingDrawnTileContainer;
        private bool warnedMissingFaceUpTileButtonPrefab;
        private bool warnedMissingFaceDownTileButtonPrefab;

        public event Action DrawnTileClicked;

        public void Configure(RectTransform container, TileButtonView prefab)
        {
            drawnTileContainer = container;
            faceUpTileButtonPrefab = prefab;
        }

        public void Configure(
            RectTransform container,
            TileButtonView faceUpPrefab,
            TileButtonView faceDownPrefab)
        {
            drawnTileContainer = container;
            faceUpTileButtonPrefab = faceUpPrefab;
            faceDownTileButtonPrefab = faceDownPrefab;
        }

        public void ConfigureMissingReferences(RectTransform fallbackContainer, TileButtonView fallbackPrefab)
        {
            if (drawnTileContainer == null)
                drawnTileContainer = fallbackContainer;

            if (faceUpTileButtonPrefab == null)
                faceUpTileButtonPrefab = fallbackPrefab;
        }

        public void Rebuild(Tile? drawnTile)
        {
            Render(drawnTile, true, tileInteractable);
        }

        public void Render(Tile? drawnTile, bool faceUp, bool interactable)
        {
            this.faceUp = faceUp;
            tileInteractable = faceUp && interactable;
            Clear();

            if (!drawnTile.HasValue)
                return;

            if (drawnTileContainer == null)
            {
                WarnMissingOnce(ref warnedMissingDrawnTileContainer, "Drawn tile container is not assigned.");
                return;
            }

            TileButtonView prefab = GetTileButtonPrefab(faceUp);
            if (prefab == null)
                return;

            activeTileButton = Instantiate(prefab, drawnTileContainer);
            if (faceUp)
                activeTileButton.Initialize(0, drawnTile.Value, HandleDrawnTileClicked);
            else
                activeTileButton.InitializeFaceDown(0, null);

            activeTileButton.SetInteractable(tileInteractable);
        }

        public void SetTileInteractable(bool interactable)
        {
            tileInteractable = faceUp && interactable;

            if (activeTileButton != null)
                activeTileButton.SetInteractable(tileInteractable);
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

        private TileButtonView GetTileButtonPrefab(bool renderFaceUp)
        {
            if (renderFaceUp)
            {
                if (faceUpTileButtonPrefab == null)
                {
                    WarnMissingOnce(
                        ref warnedMissingFaceUpTileButtonPrefab,
                        "Face-up TileButtonView prefab is not assigned.");
                }

                return faceUpTileButtonPrefab;
            }

            if (faceDownTileButtonPrefab != null)
                return faceDownTileButtonPrefab;

            WarnMissingOnce(
                ref warnedMissingFaceDownTileButtonPrefab,
                "Face-down TileButtonView prefab is not assigned. Falling back to face-up prefab with hidden label.");
            return faceUpTileButtonPrefab;
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
