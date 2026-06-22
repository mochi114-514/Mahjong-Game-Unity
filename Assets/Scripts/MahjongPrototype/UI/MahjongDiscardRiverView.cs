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
        private const int RiverColumns = 6;

        private static readonly Vector2 TileCellSize = new Vector2(60f, 65f);
        private static readonly Vector2 TileSpacing = new Vector2(3f, 3f);

        [Header("Discard River")]
        [SerializeField] private RectTransform eastDiscardRiverContainer;
        [SerializeField] private TileButtonView tileButtonPrefab;
        [SerializeField] private SeatId displayedSeat = SeatId.East;

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

            if (discards == null)
                return;

            if (eastDiscardRiverContainer == null)
            {
                WarnMissingOnce(ref warnedMissingContainer, "East discard river container is not assigned.");
                return;
            }

            if (tileButtonPrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTileButtonPrefab, "TileButtonView prefab is not assigned.");
                return;
            }

            EnsureGridLayout(eastDiscardRiverContainer);

            int riverIndex = 0;
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                if (record.ActorSeat != displayedSeat)
                    continue;

                TileButtonView view = Instantiate(tileButtonPrefab, eastDiscardRiverContainer);
                view.Initialize(riverIndex, record.Tile, null);
                view.SetInteractable(false);
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

        private static void EnsureGridLayout(RectTransform container)
        {
            GridLayoutGroup gridLayout = container.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
                gridLayout = container.gameObject.AddComponent<GridLayoutGroup>();

            gridLayout.cellSize = TileCellSize;
            gridLayout.spacing = TileSpacing;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = RiverColumns;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
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
