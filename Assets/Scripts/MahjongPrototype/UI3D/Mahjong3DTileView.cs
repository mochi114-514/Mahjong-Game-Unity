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
        [Header("3D Face Mesh")]
        [SerializeField] private MeshFilter frontFaceMeshFilter;
        [SerializeField] private Mahjong3DTileFaceCatalog tileFaceCatalog;

        public event Action<int> Clicked;

        public int HandIndex { get; private set; } = -1;
        public Tile? Tile { get; private set; }
        public bool FaceUp { get; private set; } = true;
        public bool Interactable { get; private set; }

        private bool warnedMissingFrontFaceMeshFilter;
        private bool warnedMissingTileFaceCatalog;

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

            ApplyFrontFaceMesh(tile);
        }

        public void NotifyClicked()
        {
            if (!Interactable)
                return;

            Clicked?.Invoke(HandIndex);
        }

        public void SetInteractable(bool interactable)
        {
            Interactable = FaceUp && interactable;
        }

        private void ApplyFrontFaceMesh(Tile tile)
        {
            if (frontFaceMeshFilter == null)
            {
                WarnMissingOnce(ref warnedMissingFrontFaceMeshFilter, "Front face MeshFilter is not assigned.");
                return;
            }

            if (tileFaceCatalog == null)
            {
                WarnMissingOnce(ref warnedMissingTileFaceCatalog, "Tile face catalog is not assigned.");
                return;
            }

            if (!tileFaceCatalog.TryGetFrontFaceMesh(tile, out Mesh mesh))
                return;

            frontFaceMeshFilter.sharedMesh = mesh;
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(Mahjong3DTileView)}: {message}", this);
        }
    }
}
