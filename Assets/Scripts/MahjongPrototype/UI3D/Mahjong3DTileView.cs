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

        [Header("Visual State")]
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private Color dimmedTint = new Color(0.32f, 0.32f, 0.32f, 1f);
        [SerializeField] private string[] tintPropertyNames = { "_BaseColor", "_Color" };

        public event Action<int> Clicked;

        public int HandIndex { get; private set; } = -1;
        public Tile? Tile { get; private set; }
        public bool FaceUp { get; private set; } = true;
        public bool Interactable { get; private set; }
        public bool IsDimmed { get; private set; }

        private bool warnedMissingFrontFaceMeshFilter;
        private bool warnedMissingTileFaceCatalog;
        private MaterialPropertyBlock visualPropertyBlock;

        public void Initialize(int handIndex)
        {
            HandIndex = handIndex;
            Tile = null;
            FaceUp = true;
            Interactable = false;
            SetDimmed(false);
        }

        public void Initialize(int handIndex, Tile tile, bool faceUp, bool interactable)
        {
            HandIndex = handIndex;
            Tile = tile;
            FaceUp = faceUp;
            Interactable = faceUp && interactable;
            SetDimmed(false);

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

        public void SetDimmed(bool dimmed)
        {
            if (IsDimmed == dimmed)
                return;

            IsDimmed = dimmed;
            ApplyDimmedVisual();
        }

        private void ApplyDimmedVisual()
        {
            CacheVisualRenderers();

            if (visualRenderers == null)
                return;

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Renderer target = visualRenderers[i];
                if (target == null)
                    continue;

                if (!IsDimmed)
                {
                    target.SetPropertyBlock(null);
                    continue;
                }

                if (visualPropertyBlock == null)
                    visualPropertyBlock = new MaterialPropertyBlock();

                target.GetPropertyBlock(visualPropertyBlock);
                ApplyTintProperties(visualPropertyBlock);
                target.SetPropertyBlock(visualPropertyBlock);
            }
        }

        private void ApplyTintProperties(MaterialPropertyBlock propertyBlock)
        {
            if (propertyBlock == null || tintPropertyNames == null)
                return;

            for (int i = 0; i < tintPropertyNames.Length; i++)
            {
                string propertyName = tintPropertyNames[i];
                if (string.IsNullOrWhiteSpace(propertyName))
                    continue;

                propertyBlock.SetColor(propertyName, dimmedTint);
            }
        }

        private void CacheVisualRenderers()
        {
            if (visualRenderers != null && visualRenderers.Length > 0)
                return;

            visualRenderers = GetComponentsInChildren<Renderer>(true);
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
