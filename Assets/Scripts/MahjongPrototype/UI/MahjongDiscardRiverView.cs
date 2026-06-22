using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Discard River View")]
    public sealed class MahjongDiscardRiverView : MonoBehaviour
    {
        [Header("Discard River")]
        [SerializeField] private RectTransform eastDiscardRiverContainer;
        [SerializeField] private TileButtonView tileButtonPrefab;
        [SerializeField] private SeatId displayedSeat = SeatId.East;
        [SerializeField] private int columns = 6;
        [SerializeField] private float spacingX = 3f;
        [SerializeField] private float spacingY = 3f;

        private readonly List<TileButtonView> activeTileButtons = new List<TileButtonView>();
        private bool warnedMissingContainer;
        private bool warnedMissingTileButtonPrefab;

        public void Configure(RectTransform container, TileButtonView prefab)
        {
            eastDiscardRiverContainer = container;
            tileButtonPrefab = prefab;
        }

        public void ConfigureMissingReferences(RectTransform fallbackContainer, TileButtonView fallbackPrefab)
        {
            if (eastDiscardRiverContainer == null)
                eastDiscardRiverContainer = fallbackContainer;

            if (tileButtonPrefab == null)
                tileButtonPrefab = fallbackPrefab;
        }

        public void Rebuild(IReadOnlyList<DiscardRecord> discards)
        {
            Clear();

            if (eastDiscardRiverContainer == null)
            {
                WarnMissingOnce(ref warnedMissingContainer, "East discard river container is not assigned.");
                return;
            }

            DisableGridLayout(eastDiscardRiverContainer);

            if (discards == null)
                return;

            if (tileButtonPrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTileButtonPrefab, "TileButtonView prefab is not assigned.");
                return;
            }

            int riverIndex = 0;
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                if (record.ActorSeat != displayedSeat)
                    continue;

                TileButtonView view = Instantiate(tileButtonPrefab, eastDiscardRiverContainer);
                view.Initialize(riverIndex, record.Tile, null);
                view.SetInteractable(false);
                ApplyTileTransform(view, riverIndex, displayedSeat);
                activeTileButtons.Add(view);
                riverIndex++;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < activeTileButtons.Count; i++)
            {
                TileButtonView view = activeTileButtons[i];
                if (view != null)
                    DestroyView(view);
            }

            activeTileButtons.Clear();
        }

        private void ApplyTileTransform(TileButtonView view, int discardIndex, SeatId seat)
        {
            if (view == null)
                return;

            RectTransform tileRect = view.GetComponent<RectTransform>();
            if (tileRect == null)
                return;

            Vector2 tileSize = GetTileSize(tileRect);
            tileRect.anchorMin = new Vector2(0f, 1f);
            tileRect.anchorMax = new Vector2(0f, 1f);
            tileRect.pivot = new Vector2(0f, 1f);
            tileRect.anchoredPosition = CalculateTilePosition(discardIndex, tileSize);
            tileRect.localRotation = CalculateTileRotation(seat);
        }

        private static Vector2 GetTileSize(RectTransform tileRect)
        {
            Vector2 tileSize = tileRect.rect.size;
            if (tileSize.x <= 0f)
                tileSize.x = tileRect.sizeDelta.x;

            if (tileSize.y <= 0f)
                tileSize.y = tileRect.sizeDelta.y;

            if (tileSize.x <= 0f)
                tileSize.x = 1f;

            if (tileSize.y <= 0f)
                tileSize.y = 1f;

            return tileSize;
        }

        private Vector2 CalculateTilePosition(int discardIndex, Vector2 tileSize)
        {
            int safeColumns = Mathf.Max(1, columns);
            int column = discardIndex % safeColumns;
            int row = discardIndex / safeColumns;
            float x = column * (tileSize.x + spacingX);
            float y = -row * (tileSize.y + spacingY);
            return new Vector2(x, y);
        }

        private static Quaternion CalculateTileRotation(SeatId seat)
        {
            switch (seat)
            {
                case SeatId.South:
                    return Quaternion.Euler(0f, 0f, 90f);
                case SeatId.West:
                    return Quaternion.Euler(0f, 0f, 180f);
                case SeatId.North:
                    return Quaternion.Euler(0f, 0f, 270f);
                case SeatId.East:
                default:
                    return Quaternion.identity;
            }
        }

        private static void DisableGridLayout(RectTransform container)
        {
            GridLayoutGroup gridLayout = container.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
                gridLayout.enabled = false;
        }

        private static void DestroyView(TileButtonView view)
        {
            if (Application.isPlaying)
                Destroy(view.gameObject);
            else
                DestroyImmediate(view.gameObject);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongDiscardRiverView)}: {message}", this);
        }
    }
}
