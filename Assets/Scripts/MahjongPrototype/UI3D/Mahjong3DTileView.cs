using System;
using MahjongPrototype.Domain;
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
        public Tile? Tile { get; private set; }
        public bool FaceUp { get; private set; } = true;
        public bool Interactable { get; private set; }

        public void Initialize(int handIndex)
        {
            HandIndex = handIndex;
            Tile = null;
            FaceUp = true;
            Interactable = false;
        }

        public void Initialize(int handIndex, Tile tile, bool faceUp, bool interactable)
        {
            HandIndex = handIndex;
            Tile = tile;
            FaceUp = faceUp;
            Interactable = faceUp && interactable;
        }

        public void NotifyClicked()
        {
            Clicked?.Invoke(HandIndex);
        }
    }
}
