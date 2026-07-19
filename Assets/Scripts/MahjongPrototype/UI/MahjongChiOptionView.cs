using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Chi Option View")]
    public sealed class MahjongChiOptionView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Transform tileContainer;

        private readonly List<MahjongTileSpriteView> spawnedTileViews =
            new List<MahjongTileSpriteView>();
        private UnityAction selectionAction;

        public bool HasSelectButton => selectButton != null;
        public bool HasTileContainer =>
            tileContainer != null && tileContainer.IsChildOf(transform);
        public bool HasClickableTileContainer =>
            HasSelectButton &&
            tileContainer != null &&
            (tileContainer == selectButton.transform ||
             tileContainer.IsChildOf(selectButton.transform));
        public int SpawnedTileCount => spawnedTileViews.Count;

        private void OnDisable()
        {
            ClearSelectionAction();
            ClearTiles();
        }

        public bool TrySetSelectionAction(UnityAction action)
        {
            ClearSelectionAction();
            if (selectButton == null || action == null)
                return false;

            selectionAction = action;
            selectButton.onClick.AddListener(selectionAction);
            return true;
        }

        public bool TryAddTile(MahjongTileSpriteView tileViewPrefab, Sprite sprite)
        {
            if (tileContainer == null || tileViewPrefab == null || sprite == null)
                return false;

            MahjongTileSpriteView tileView = Instantiate(tileViewPrefab, tileContainer);
            if (!tileView.TrySetSprite(sprite))
            {
                DestroyTileView(tileView);
                return false;
            }

            tileView.gameObject.SetActive(true);
            spawnedTileViews.Add(tileView);
            return true;
        }

        public void ClearTiles()
        {
            for (int i = spawnedTileViews.Count - 1; i >= 0; i--)
                DestroyTileView(spawnedTileViews[i]);

            spawnedTileViews.Clear();
        }

        private void ClearSelectionAction()
        {
            if (selectButton != null && selectionAction != null)
                selectButton.onClick.RemoveListener(selectionAction);

            selectionAction = null;
        }

        private static void DestroyTileView(MahjongTileSpriteView tileView)
        {
            if (tileView == null)
                return;

            tileView.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(tileView.gameObject);
            else
                DestroyImmediate(tileView.gameObject);
        }
    }
}
