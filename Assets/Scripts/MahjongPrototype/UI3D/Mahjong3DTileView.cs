using System;
using System.Collections;
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

        [Header("Pointer / Selection Transform Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 hoverPositionOffset = new Vector3(0f, 0.12f, 0f);
        [SerializeField] private Vector3 selectedPositionOffset = new Vector3(0f, 0.38f, 0f);
        [SerializeField] private float selectedRotationAngle = -4f;
        [SerializeField, Min(0f)] private float hoverTransitionDuration = 0.08f;
        [SerializeField, Min(0f)] private float selectionTransitionDuration = 0.1f;
        [SerializeField, Min(0f)] private float selectionOvershootAmount = 0.08f;
        [SerializeField, Min(0f)] private float selectionAnimationDuration = 0.18f;
        [SerializeField, Min(0f)] private float deselectionTransitionDuration = 0.1f;

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
        public event Action<Mahjong3DTileView> HoverEntered;
        public event Action<Mahjong3DTileView> HoverExited;

        public int HandIndex { get; private set; } = -1;
        public Tile? Tile { get; private set; }
        public bool FaceUp { get; private set; } = true;
        public bool Interactable { get; private set; }
        public bool IsDimmed { get; private set; }
        public bool IsHovered { get; private set; }
        public bool IsSelected { get; private set; }

        private bool warnedMissingFrontFaceMeshFilter;
        private bool warnedMissingTileFaceCatalog;
        private bool warnedMissingDimTarget;
        private bool warnedMissingDimmedOverrideMaterial;
        private MaterialPropertyBlock visualPropertyBlock;
        private Renderer[] resolvedDimTargetRenderers;
        private readonly Dictionary<Renderer, Material[]> originalSharedMaterialsByRenderer =
            new Dictionary<Renderer, Material[]>();
        private Coroutine transformVisualRoutine;
        private Transform cachedVisualRoot;
        private Vector3 baseVisualLocalPosition;
        private Quaternion baseVisualLocalRotation;
        private bool visualBaselineCached;

        private void Awake()
        {
            EnsureVisualBaseline();
        }

        private void OnDisable()
        {
            NotifyHoverExited();
            IsSelected = false;
            StopTransformVisualRoutine();
            ResetTransformVisualImmediate();
        }

        private void OnDestroy()
        {
            StopTransformVisualRoutine();
        }

        private void OnValidate()
        {
            resolvedDimTargetRenderers = null;
            if (cachedVisualRoot != visualRoot)
            {
                cachedVisualRoot = null;
                visualBaselineCached = false;
            }

            if (Application.isPlaying && IsDimmed)
                ApplyDimmedVisual();
        }

        public void Initialize(int handIndex)
        {
            NotifyHoverExited();
            ResetSelectionVisualState();
            HandIndex = handIndex;
            Tile = null;
            FaceUp = true;
            Interactable = false;
            SetDimmed(false, true);
        }

        public void Initialize(int handIndex, Tile tile, bool faceUp, bool interactable)
        {
            NotifyHoverExited();
            ResetSelectionVisualState();
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

        public void NotifyHoverEntered()
        {
            if (IsHovered)
                return;

            IsHovered = true;
            if (!IsSelected)
                AnimateToCurrentState(hoverTransitionDuration);
            HoverEntered?.Invoke(this);
        }

        public void NotifyHoverExited()
        {
            if (!IsHovered)
                return;

            IsHovered = false;
            if (!IsSelected)
                AnimateToCurrentState(hoverTransitionDuration);
            HoverExited?.Invoke(this);
        }

        public void SetSelected(bool selected)
        {
            if (IsSelected == selected)
                return;

            IsSelected = selected;
            if (selected)
                AnimateSelectionWithSingleOvershoot();
            else
                AnimateToCurrentState(deselectionTransitionDuration);
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

        private void ResetSelectionVisualState()
        {
            IsSelected = false;
            StopTransformVisualRoutine();
            ResetTransformVisualImmediate();
        }

        private void AnimateToCurrentState(float duration)
        {
            if (!TryGetCurrentTargetPose(out Vector3 targetPosition, out Quaternion targetRotation))
                return;

            StartTransformVisualTransition(targetPosition, targetRotation, duration);
        }

        private void AnimateSelectionWithSingleOvershoot()
        {
            if (!TryGetSelectedTargetPose(out Vector3 targetPosition, out Quaternion targetRotation))
                return;

            StopTransformVisualRoutine();
            if (!CanAnimate(selectionAnimationDuration))
            {
                ApplyTransformVisualPose(targetPosition, targetRotation);
                return;
            }

            float approachDuration = Mathf.Min(
                Mathf.Max(0f, selectionTransitionDuration),
                selectionAnimationDuration);
            float settleDuration = selectionAnimationDuration - approachDuration;
            if (selectionOvershootAmount <= 0f || approachDuration <= 0f || settleDuration <= 0f)
            {
                transformVisualRoutine = StartCoroutine(AnimateToPose(
                    visualRoot.localPosition,
                    visualRoot.localRotation,
                    targetPosition,
                    targetRotation,
                    selectionAnimationDuration));
                return;
            }

            Vector3 overshootPosition = targetPosition + (Vector3.up * selectionOvershootAmount);
            transformVisualRoutine = StartCoroutine(AnimateSelectionPose(
                visualRoot.localPosition,
                visualRoot.localRotation,
                overshootPosition,
                targetPosition,
                targetRotation,
                approachDuration,
                settleDuration));
        }

        private void StartTransformVisualTransition(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            StopTransformVisualRoutine();
            if (!CanAnimate(duration))
            {
                ApplyTransformVisualPose(targetPosition, targetRotation);
                return;
            }

            transformVisualRoutine = StartCoroutine(AnimateToPose(
                visualRoot.localPosition,
                visualRoot.localRotation,
                targetPosition,
                targetRotation,
                duration));
        }

        private IEnumerator AnimateToPose(
            Vector3 startPosition,
            Quaternion startRotation,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = EaseOutCubic(elapsed / duration);
                ApplyTransformVisualPose(
                    Vector3.LerpUnclamped(startPosition, targetPosition, progress),
                    Quaternion.SlerpUnclamped(startRotation, targetRotation, progress));
                yield return null;
            }

            ApplyTransformVisualPose(targetPosition, targetRotation);
            transformVisualRoutine = null;
        }

        private IEnumerator AnimateSelectionPose(
            Vector3 startPosition,
            Quaternion startRotation,
            Vector3 overshootPosition,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float approachDuration,
            float settleDuration)
        {
            for (float elapsed = 0f; elapsed < approachDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = EaseOutCubic(elapsed / approachDuration);
                ApplyTransformVisualPose(
                    Vector3.LerpUnclamped(startPosition, overshootPosition, progress),
                    Quaternion.SlerpUnclamped(startRotation, targetRotation, progress));
                yield return null;
            }

            ApplyTransformVisualPose(overshootPosition, targetRotation);
            for (float elapsed = 0f; elapsed < settleDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / settleDuration);
                ApplyTransformVisualPose(
                    Vector3.LerpUnclamped(overshootPosition, targetPosition, progress),
                    targetRotation);
                yield return null;
            }

            ApplyTransformVisualPose(targetPosition, targetRotation);
            transformVisualRoutine = null;
        }

        private bool TryGetCurrentTargetPose(
            out Vector3 targetPosition,
            out Quaternion targetRotation)
        {
            targetPosition = default;
            targetRotation = default;
            if (!EnsureVisualBaseline())
                return false;

            if (IsSelected)
            {
                targetPosition = baseVisualLocalPosition + selectedPositionOffset;
                targetRotation = baseVisualLocalRotation * Quaternion.Euler(selectedRotationAngle, 0f, 0f);
            }
            else if (IsHovered)
            {
                targetPosition = baseVisualLocalPosition + hoverPositionOffset;
                targetRotation = baseVisualLocalRotation;
            }
            else
            {
                targetPosition = baseVisualLocalPosition;
                targetRotation = baseVisualLocalRotation;
            }

            return true;
        }

        private bool TryGetSelectedTargetPose(
            out Vector3 targetPosition,
            out Quaternion targetRotation)
        {
            targetPosition = default;
            targetRotation = default;
            if (!EnsureVisualBaseline())
                return false;

            targetPosition = baseVisualLocalPosition + selectedPositionOffset;
            targetRotation = baseVisualLocalRotation * Quaternion.Euler(selectedRotationAngle, 0f, 0f);
            return true;
        }

        private bool EnsureVisualBaseline()
        {
            if (visualRoot == null)
                return false;

            if (visualBaselineCached && cachedVisualRoot == visualRoot)
                return true;

            cachedVisualRoot = visualRoot;
            baseVisualLocalPosition = visualRoot.localPosition;
            baseVisualLocalRotation = visualRoot.localRotation;
            visualBaselineCached = true;
            return true;
        }

        private bool CanAnimate(float duration)
        {
            return duration > 0f && Application.isPlaying && isActiveAndEnabled &&
                EnsureVisualBaseline();
        }

        private void StopTransformVisualRoutine()
        {
            if (transformVisualRoutine == null)
                return;

            StopCoroutine(transformVisualRoutine);
            transformVisualRoutine = null;
        }

        private void ResetTransformVisualImmediate()
        {
            if (!EnsureVisualBaseline())
                return;

            ApplyTransformVisualPose(baseVisualLocalPosition, baseVisualLocalRotation);
        }

        private void ApplyTransformVisualPose(Vector3 localPosition, Quaternion localRotation)
        {
            if (visualRoot == null)
                return;

            visualRoot.localPosition = localPosition;
            visualRoot.localRotation = localRotation;
        }

        private static float EaseOutCubic(float progress)
        {
            float inverse = 1f - Mathf.Clamp01(progress);
            return 1f - (inverse * inverse * inverse);
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
