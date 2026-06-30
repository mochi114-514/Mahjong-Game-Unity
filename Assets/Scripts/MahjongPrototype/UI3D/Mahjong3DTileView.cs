using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    // PROTOTYPE: 3D tile view for prefab instantiate verification only.
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Tile View")]
    public sealed class Mahjong3DTileView : MonoBehaviour
    {
        private enum DimVisualMode
        {
            MaterialPropertyBlockTint = 0,
            OverrideMaterial = 1
        }

        [Header("3D Face Mesh")]
        [SerializeField] private MeshFilter frontFaceMeshFilter;
        [SerializeField] private Mahjong3DTileFaceCatalog tileFaceCatalog;

        [Header("Dim Visual")]
        [SerializeField] private Transform dimTargetRoot;
        [SerializeField] private Renderer[] dimTargetRenderers;
        [SerializeField] private DimVisualMode dimVisualMode = DimVisualMode.OverrideMaterial;
        [SerializeField] private Material dimmedOverrideMaterial;
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
        private bool warnedMissingDimmedOverrideMaterial;
        private MaterialPropertyBlock visualPropertyBlock;
        private Renderer[] resolvedDimTargetRenderers;
        private readonly Dictionary<Renderer, Material[]> originalSharedMaterialsByRenderer =
            new Dictionary<Renderer, Material[]>();

        private void OnValidate()
        {
            resolvedDimTargetRenderers = null;

            if (Application.isPlaying && IsDimmed)
                ApplyDimmedVisual();
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

            if (!IsDimmed)
            {
                ClearPropertyBlockDim(targets);
                ClearOverrideMaterialDim();
                return;
            }

            if (targets.Length == 0)
                return;

            switch (dimVisualMode)
            {
                case DimVisualMode.OverrideMaterial:
                    ClearPropertyBlockDim(targets);
                    ApplyOverrideMaterialDim(targets);
                    break;
                case DimVisualMode.MaterialPropertyBlockTint:
                    ClearOverrideMaterialDim();
                    ApplyPropertyBlockDim(targets);
                    break;
            }
        }

        private void ApplyOverrideMaterialDim(Renderer[] targets)
        {
            if (dimmedOverrideMaterial == null)
            {
                WarnMissingOnce(
                    ref warnedMissingDimmedOverrideMaterial,
                    "Dimmed override material is not assigned.");
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
                if (target == null)
                    continue;

                if (!originalSharedMaterialsByRenderer.ContainsKey(target))
                    originalSharedMaterialsByRenderer[target] = target.sharedMaterials;

                Material[] currentMaterials = target.sharedMaterials;
                int materialCount = currentMaterials != null && currentMaterials.Length > 0
                    ? currentMaterials.Length
                    : 1;
                Material[] overrideMaterials = new Material[materialCount];
                for (int materialIndex = 0; materialIndex < overrideMaterials.Length; materialIndex++)
                    overrideMaterials[materialIndex] = dimmedOverrideMaterial;

                target.sharedMaterials = overrideMaterials;
            }
        }

        private void ClearOverrideMaterialDim()
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in originalSharedMaterialsByRenderer)
            {
                if (pair.Key != null)
                    pair.Key.sharedMaterials = pair.Value;
            }

            originalSharedMaterialsByRenderer.Clear();
        }

        private void ApplyPropertyBlockDim(Renderer[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
                if (target == null)
                    continue;

                if (visualPropertyBlock == null)
                    visualPropertyBlock = new MaterialPropertyBlock();

                target.GetPropertyBlock(visualPropertyBlock);
                ApplyTintProperties(visualPropertyBlock);
                target.SetPropertyBlock(visualPropertyBlock);
            }
        }

        private static void ClearPropertyBlockDim(Renderer[] targets)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
                if (target != null)
                    target.SetPropertyBlock(null);
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

        [ContextMenu("Debug Force Dim On")]
        private void DebugForceDimOn()
        {
            SetDimmed(true, true);
        }

        [ContextMenu("Debug Force Dim Off")]
        private void DebugForceDimOff()
        {
            SetDimmed(false, true);
        }

        [ContextMenu("Debug Force Apply Dim Visual")]
        private void DebugForceApplyDimVisual()
        {
            ApplyDimmedVisual();
        }

        [ContextMenu("Debug Force Material Color Red")]
        private void DebugForceMaterialColorRed()
        {
            Renderer[] targets = ResolveDimTargetRenderers();
            for (int i = 0; i < targets.Length; i++)
            {
                Renderer target = targets[i];
                if (target == null)
                    continue;

                Material[] materials = target.materials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                        continue;

                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", Color.red);
                    if (material.HasProperty("_Color"))
                        material.SetColor("_Color", Color.red);
                    if (material.HasProperty("_MainColor"))
                        material.SetColor("_MainColor", Color.red);
                    if (material.HasProperty("_TintColor"))
                        material.SetColor("_TintColor", Color.red);
                }
            }
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
