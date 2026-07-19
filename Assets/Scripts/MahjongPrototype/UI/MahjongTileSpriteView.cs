using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Tile Sprite View")]
    public sealed class MahjongTileSpriteView : MonoBehaviour
    {
        [SerializeField] private Image targetImage;

        public bool HasTargetImage => targetImage != null;

        public bool TrySetSprite(Sprite sprite)
        {
            if (targetImage == null || sprite == null)
                return false;

            targetImage.sprite = sprite;
            targetImage.enabled = true;
            return true;
        }
    }
}
