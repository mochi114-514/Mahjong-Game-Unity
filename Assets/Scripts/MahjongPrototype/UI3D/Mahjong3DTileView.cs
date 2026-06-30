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

        [Header("Dim Visual")]
        [SerializeField] private Transform dimTargetRoot;
        [SerializeField] private Renderer[] dimTargetRenderers;
        [SerializeField] private Color dimmedTint = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField]
        private string[] tintPropertyNames =
        {
            "_BaseColor",
            "_Color",
            "_MainColor",
            "_TintColor"
        };
        [SerializeField] private bool debugDimVisual;

        public event Action<int> Clicked;

        public int HandIndex { get; private set; } = -1;
        public Tile? Tile { get; private set; }
        public bool FaceUp { get; private set; } = true;
        public bool Interactable { get; private set; }
        public bool IsDimmed { get; private set; }

        private bool warnedMissingFrontFaceMeshFilter;
        private bool warnedMissingTileFaceCatalog;
        private bool warnedMissingDimTarget;
        private MaterialPropertyBlock visualPropertyBlock;
        private Renderer[] resolvedDimTargetRenderers;

        private void OnValidate()
        {
            resolvedDimTargetRenderers = null;
        }

        public void Initialize(int handIndex)
        {
            HandIndex = handIndex;
            Tile = null;
            FaceUp = true;
            Interactable = false;
            SetDimmed(false, true);
        }

        public void Initialize(int handIndex, Tile tile, bool faceUp, bool interactable)
        {
            HandIndex = handIndex;
            Tile = tile;
            FaceUp = faceUp;
            Interactable = faceUp && interactable;
            SetDimmed(false, true);

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
            SetDimmed(dimmed, false);
        }

        private void SetDimmed(bool dimmed, bool forceApply)
        {
            if (!forceApply && IsDimmed == dimmed)
                return;

            IsDimmed = dimmed;
            ApplyDimmedVisual();
        }

        private void ApplyDimmedVisual()
        {
            Renderer[] targets = ResolveDimTargetRenderers();
            LogDimVisualDebug(targets);

            if (IsDimmed && targets.Length == 0)
            {
                WarnMissingOnce(
                    ref warnedMissingDimTarget,
                    "Dim target root/renderers are not assigned or no Renderer was found under DimTargetRoot.");
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
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

        private Renderer[] ResolveDimTargetRenderers()
        {
            if (dimTargetRenderers != null && dimTargetRenderers.Length > 0)
                return dimTargetRenderers;

            if (resolvedDimTargetRenderers != null && resolvedDimTargetRenderers.Length > 0)
                return resolvedDimTargetRenderers;

            if (dimTargetRoot == null)
                return Array.Empty<Renderer>();

            resolvedDimTargetRenderers = dimTargetRoot.GetComponentsInChildren<Renderer>(true);
            return resolvedDimTargetRenderers;
        }

        private void LogDimVisualDebug(Renderer[] targets)
        {
            if (!debugDimVisual)
                return;

            string rootName = dimTargetRoot != null ? dimTargetRoot.name : "(null)";
            int count = targets != null ? targets.Length : 0;

            Debug.Log(
                $"{nameof(Mahjong3DTileView)}: IsDimmed={IsDimmed}, DimTargetRoot={rootName}, RendererCount={count}",
                this);

            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
                if (target == null)
                {
                    Debug.Log($"{nameof(Mahjong3DTileView)}: Renderer[{i}] is null.", this);
                    continue;
                }

                Material material = target.sharedMaterial;
                if (material == null)
                {
                    Debug.Log($"{nameof(Mahjong3DTileView)}: Renderer[{i}]={target.name}, Material=null", this);
                    continue;
                }

                string shaderName = material.shader != null ? material.shader.name : "(null)";
                Debug.Log(
                    $"{nameof(Mahjong3DTileView)}: Renderer[{i}]={target.name}, " +
                    $"Material={material.name}, Shader={shaderName}, " +
                    $"Has _BaseColor={material.HasProperty("_BaseColor")}, " +
                    $"Has _Color={material.HasProperty("_Color")}, " +
                    $"Has _MainColor={material.HasProperty("_MainColor")}, " +
                    $"Has _TintColor={material.HasProperty("_TintColor")}",
                    this);
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
