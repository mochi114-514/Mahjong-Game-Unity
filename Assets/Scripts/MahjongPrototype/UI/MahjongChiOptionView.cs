using System.Collections.Generic;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Chi Option View")]
    public sealed class MahjongChiOptionView : MonoBehaviour
    {
        [SerializeField] private Transform tileContainer;

        private readonly List<MahjongTileSpriteView> spawnedTileViews =
            new List<MahjongTileSpriteView>();

        public bool HasTileContainer =>
            tileContainer != null && tileContainer.IsChildOf(transform);
        public int SpawnedTileCount => spawnedTileViews.Count;

        private void OnDisable()
        {
            ClearTiles();
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
