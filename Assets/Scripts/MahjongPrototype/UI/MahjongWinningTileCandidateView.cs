using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Winning Tile Candidate View")]
    public sealed class MahjongWinningTileCandidateView : MonoBehaviour
    {
        [SerializeField] private MahjongTileSpriteView tileSpriteView;
        [SerializeField] private TMP_Text countText;

        public bool HasTileSpriteView => tileSpriteView != null;
        public bool HasCountText => countText != null;

        private void Awake()
        {
            DisableGraphicRaycasts();
        }

        public bool TrySet(Sprite sprite, int visibleRemainingCount)
        {
            DisableGraphicRaycasts();
            if (tileSpriteView == null || countText == null || sprite == null)
                return false;

            if (!tileSpriteView.TrySetSprite(sprite))
                return false;

            countText.text = $"{Mathf.Clamp(visibleRemainingCount, 0, 4)}枚";
            return true;
        }

        private void DisableGraphicRaycasts()
        {
            if (tileSpriteView != null &&
                tileSpriteView.TryGetComponent(out Image tileImage))
            {
                tileImage.raycastTarget = false;
            }

            if (countText != null)
                countText.raycastTarget = false;
        }
    }
}
