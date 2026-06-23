using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    // PROTOTYPE: only the self/bottom river slot is rendered until full 4-seat river UI exists.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Discard River View")]
    public sealed class MahjongDiscardRiverView : MonoBehaviour
    {
        [Header("Discard River")]
        [FormerlySerializedAs("eastDiscardRiverContainer")]
        [SerializeField] private RectTransform selfBottomDiscardRiverContainer;
        [SerializeField] private TileButtonView tileButtonPrefab;
        [SerializeField] private ViewSlot viewSlot = ViewSlot.SelfBottom;
        [SerializeField] private int columns = 6;
        [SerializeField] private float spacingX = 3f;
        [SerializeField] private float spacingY = 3f;

        private readonly List<TileButtonView> activeTileButtons = new List<TileButtonView>();
        private SeatId dataSeat = SeatId.East;
        private bool warnedMissingContainer;
        private bool warnedMissingTileButtonPrefab;

        public void Configure(RectTransform container, TileButtonView prefab)
        {
            selfBottomDiscardRiverContainer = container;
            tileButtonPrefab = prefab;
        }

        public void ConfigureMissingReferences(RectTransform fallbackContainer, TileButtonView fallbackPrefab)
        {
            if (selfBottomDiscardRiverContainer == null)
                selfBottomDiscardRiverContainer = fallbackContainer;

            if (tileButtonPrefab == null)
                tileButtonPrefab = fallbackPrefab;
        }

        public void Rebuild(IReadOnlyList<DiscardRecord> discards)
        {
            Rebuild(discards, dataSeat, viewSlot);
        }

        public void Rebuild(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat)
        {
            Rebuild(discards, dataSeat, ViewSlot.SelfBottom);
        }

        public void Rebuild(IReadOnlyList<DiscardRecord> discards, SeatId dataSeat, ViewSlot viewSlot)
        {
            this.dataSeat = dataSeat;
            this.viewSlot = viewSlot;
            RebuildInternal(discards);
        }

        private void RebuildInternal(IReadOnlyList<DiscardRecord> discards)
        {
            Clear();

            RectTransform container = GetContainerForViewSlot(viewSlot);
            if (container == null)
            {
                WarnMissingOnce(ref warnedMissingContainer, "Self bottom discard river container is not assigned.");
                return;
            }

            DisableGridLayout(container);

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
                if (record.ActorSeat != dataSeat)
                    continue;

                TileButtonView view = Instantiate(tileButtonPrefab, container);
                view.Initialize(riverIndex, record.Tile, null);
                view.SetInteractable(false);
                ApplyTileTransform(view, riverIndex, viewSlot);
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

        private RectTransform GetContainerForViewSlot(ViewSlot slot)
        {
            switch (slot)
            {
                case ViewSlot.SelfBottom:
                default:
                    return selfBottomDiscardRiverContainer;
            }
        }

        private void ApplyTileTransform(TileButtonView view, int discardIndex, ViewSlot slot)
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
            tileRect.localRotation = CalculateTileRotation(slot);
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

        private static Quaternion CalculateTileRotation(ViewSlot slot)
        {
            switch (slot)
            {
                case ViewSlot.SelfBottom:
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
