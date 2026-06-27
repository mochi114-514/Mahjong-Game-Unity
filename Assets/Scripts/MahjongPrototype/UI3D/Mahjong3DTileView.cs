using System;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: 3D tile view for prefab instantiate verification only.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Tile View")]
    public sealed class Mahjong3DTileView : MonoBehaviour
    {
        public event Action<int> Clicked;

        public int HandIndex { get; private set; } = -1;

        public void Initialize(int handIndex)
        {
            HandIndex = handIndex;
        }

        public void NotifyClicked()
        {
            Clicked?.Invoke(HandIndex);
        }
    }
}
