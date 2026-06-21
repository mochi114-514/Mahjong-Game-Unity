using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

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
        [SerializeField] private TileButtonView tileButtonPrefab;

        private readonly List<TileButtonView> activeTileButtons = new List<TileButtonView>();
        private bool tilesInteractable = true;
        private bool warnedMissingHandContainer;
        private bool warnedMissingTileButtonPrefab;

        public event Action<int> TileClicked;

        public void Configure(RectTransform container, TileButtonView prefab)
        {
            handContainer = container;
            tileButtonPrefab = prefab;
        }

        public void ConfigureMissingReferences(RectTransform fallbackContainer, TileButtonView fallbackPrefab)
        {
            if (handContainer == null)
                handContainer = fallbackContainer;

            if (tileButtonPrefab == null)
                tileButtonPrefab = fallbackPrefab;
        }

        public void Rebuild(IReadOnlyList<Tile> handTiles)
        {
            Clear();

            if (handTiles == null)
                return;

            if (handContainer == null)
            {
                WarnMissingOnce(ref warnedMissingHandContainer, "Hand container is not assigned.");
                return;
            }

            if (tileButtonPrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTileButtonPrefab, "TileButtonView prefab is not assigned.");
                return;
            }

            for (int i = 0; i < handTiles.Count; i++)
            {
                TileButtonView view = Instantiate(tileButtonPrefab, handContainer);
                view.Initialize(i, handTiles[i], HandleTileClicked);
                view.SetInteractable(tilesInteractable);
                activeTileButtons.Add(view);
            }
        }

        public void SetTilesInteractable(bool interactable)
        {
            tilesInteractable = interactable;

            for (int i = 0; i < activeTileButtons.Count; i++)
            {
                TileButtonView view = activeTileButtons[i];
                if (view != null)
                    view.SetInteractable(interactable);
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
