using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Hand View")]
    public sealed class MahjongHandView : MonoBehaviour
    {
        [Header("Hand Tiles")]
        [Tooltip("手牌ボタンを生成する親RectTransformです。Canvas/HandArea を割り当てます。")]
        [SerializeField] private RectTransform handContainer;
        [Tooltip("手牌1枚分のTileButtonViewテンプレートまたはPrefabです。")]
        [FormerlySerializedAs("tileButtonPrefab")]
        [SerializeField] private TileButtonView faceUpTileButtonPrefab;
        [SerializeField] private TileButtonView faceDownTileButtonPrefab;
        [SerializeField] private ViewSlot viewSlot = ViewSlot.SelfBottom;

        private readonly List<TileButtonView> activeTileButtons = new List<TileButtonView>();
        private SeatId dataSeat = SeatId.East;
        private bool faceUp = true;
        private bool tilesInteractable = true;
        private bool warnedMissingHandContainer;
        private bool warnedMissingFaceUpTileButtonPrefab;
        private bool warnedMissingFaceDownTileButtonPrefab;

        public event Action<int> TileClicked;
        public SeatId DataSeat => dataSeat;
        public ViewSlot ViewSlot => viewSlot;
        public bool FaceUp => faceUp;

        public void Configure(RectTransform container, TileButtonView prefab)
        {
            handContainer = container;
            faceUpTileButtonPrefab = prefab;
        }

        public void ConfigureMissingReferences(RectTransform fallbackContainer, TileButtonView fallbackPrefab)
        {
            if (handContainer == null)
                handContainer = fallbackContainer;

            if (faceUpTileButtonPrefab == null)
                faceUpTileButtonPrefab = fallbackPrefab;
        }

        public void Rebuild(IReadOnlyList<Tile> handTiles)
        {
            Render(handTiles, dataSeat, viewSlot, true, tilesInteractable);
        }

        public void Render(
            IReadOnlyList<Tile> handTiles,
            SeatId dataSeat,
            ViewSlot viewSlot,
            bool faceUp,
            bool interactable)
        {
            this.dataSeat = dataSeat;
            this.viewSlot = viewSlot;
            this.faceUp = faceUp;
            tilesInteractable = faceUp && interactable;
            RebuildInternal(handTiles);
        }

        private void RebuildInternal(IReadOnlyList<Tile> handTiles)
        {
            Clear();

            if (handTiles == null)
                return;

            if (handContainer == null)
            {
                WarnMissingOnce(ref warnedMissingHandContainer, "Hand container is not assigned.");
                return;
            }

            TileButtonView prefab = GetTileButtonPrefab(faceUp);
            if (prefab == null)
                return;

            for (int i = 0; i < handTiles.Count; i++)
            {
                TileButtonView view = Instantiate(prefab, handContainer);
                if (faceUp)
                    view.Initialize(i, handTiles[i], HandleTileClicked);
                else
                    view.InitializeFaceDown(i, null);

                view.SetInteractable(tilesInteractable);
                activeTileButtons.Add(view);
            }
        }

        public void SetTilesInteractable(bool interactable)
        {
            tilesInteractable = faceUp && interactable;

            for (int i = 0; i < activeTileButtons.Count; i++)
            {
                TileButtonView view = activeTileButtons[i];
                if (view != null)
                    view.SetInteractable(tilesInteractable);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < activeTileButtons.Count; i++)
            {
                TileButtonView view = activeTileButtons[i];
                if (view != null)
                    Destroy(view.gameObject);
            }

            activeTileButtons.Clear();
        }

        private TileButtonView GetTileButtonPrefab(bool faceUp)
        {
            if (faceUp)
            {
                if (faceUpTileButtonPrefab == null)
                    WarnMissingOnce(ref warnedMissingFaceUpTileButtonPrefab, "Face-up TileButtonView prefab is not assigned.");

                return faceUpTileButtonPrefab;
            }

            if (faceDownTileButtonPrefab != null)
                return faceDownTileButtonPrefab;

            WarnMissingOnce(
                ref warnedMissingFaceDownTileButtonPrefab,
                "Face-down TileButtonView prefab is not assigned. Falling back to face-up prefab with hidden label.");
            return faceUpTileButtonPrefab;
        }

        private void HandleTileClicked(int handIndex)
        {
            TileClicked?.Invoke(handIndex);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongHandView)}: {message}", this);
        }
    }
}
