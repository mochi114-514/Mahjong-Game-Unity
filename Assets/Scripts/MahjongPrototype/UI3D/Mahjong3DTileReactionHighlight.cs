using UnityEngine;

namespace MahjongPrototype.UI3D
{
    /// <summary>
    /// Controls the presentation-only shell used to preview a callable discard.
    /// Selection of the target tile intentionally belongs to a higher-level UI flow.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong 3D Tile Reaction Highlight")]
    public sealed class Mahjong3DTileReactionHighlight : MonoBehaviour
    {
        private static readonly int RimColorPropertyId = Shader.PropertyToID("_RimColor");
        private static readonly int AlphaPropertyId = Shader.PropertyToID("_Alpha");
        private static readonly int FresnelPowerPropertyId = Shader.PropertyToID("_FresnelPower");
        private static readonly int FresnelIntensityPropertyId = Shader.PropertyToID("_FresnelIntensity");
        private static readonly int SurfaceIntensityPropertyId = Shader.PropertyToID("_SurfaceIntensity");
        private static readonly int PulseStrengthPropertyId = Shader.PropertyToID("_PulseStrength");
        private static readonly int VertexExtrusionPropertyId = Shader.PropertyToID("_VertexExtrusion");

        [Header("References")]
        [SerializeField] private MeshRenderer highlightRenderer;
        [SerializeField] private MeshRenderer faceHighlightRenderer;
        [SerializeField] private Transform shellTransform;
        [SerializeField] private MeshFilter frontFaceMeshFilter;
        [SerializeField] private MeshFilter faceHighlightMeshFilter;

        [Header("Shell Shape")]
        [SerializeField] private bool applyShellScale = true;
        [SerializeField, Range(1.01f, 1.05f)] private float shellScale = 1.03f;
        [SerializeField] private bool applyVertexExtrusion;
        [SerializeField, Range(0f, 0.01f)] private float vertexExtrusion;

        [Header("Fresnel")]
        [SerializeField, Range(0.5f, 8f)] private float fresnelPower = 0.75f;
        [SerializeField, Range(0f, 2f)] private float fresnelIntensity = 2f;
        [SerializeField] private Color rimColor = new Color(1f, 0.88f, 0.55f, 1f);
        [SerializeField, Range(0f, 1f)] private float alpha = 0.18f;

        [Header("Face Surface")]
        [SerializeField, Range(0f, 1f)] private float faceAlpha = 0.3f;
        [SerializeField, Range(0f, 2f)] private float faceSurfaceIntensity = 1f;
        [SerializeField, Range(0f, 0.01f)] private float faceVertexExtrusion = 0.001f;

        [Header("Pulse")]
        [SerializeField, Min(0.1f)] private float pulsePeriod = 2f;
        [SerializeField, Range(0f, 2f)] private float pulseMinStrength = 0.85f;
        [SerializeField, Range(0f, 2f)] private float pulseMaxStrength = 1f;

        private MaterialPropertyBlock shellWorkingPropertyBlock;
        private MaterialPropertyBlock faceWorkingPropertyBlock;
        private MaterialPropertyBlock originalShellPropertyBlock;
        private MaterialPropertyBlock originalFacePropertyBlock;
        private bool hasCapturedShellRendererState;
        private bool hasCapturedFaceRendererState;
        private bool originalShellRendererEnabled;
        private bool originalFaceRendererEnabled;
        private bool hasCapturedTransformState;
        private Vector3 originalLocalScale;

        public bool IsHighlighted { get; private set; }

        /// <summary>
        /// Starts the visual effect. Calling this repeatedly does not recapture its baseline state.
        /// </summary>
        public void StartHighlight()
        {
            if (IsHighlighted)
                return;

            if (highlightRenderer == null && faceHighlightRenderer == null)
            {
                Debug.LogWarning($"{nameof(Mahjong3DTileReactionHighlight)}: Highlight Renderers are not assigned.", this);
                return;
            }

            if (shellTransform == null)
                shellTransform = highlightRenderer != null ? highlightRenderer.transform : null;

            CaptureOriginalState();
            SyncFaceMesh();
            ApplyShellTransform();
            float pulseStrength = EvaluatePulseStrength();
            ApplyShellPropertyBlock(pulseStrength);
            ApplyFacePropertyBlock(pulseStrength);

            if (highlightRenderer != null)
                highlightRenderer.enabled = true;

            if (faceHighlightRenderer != null)
                faceHighlightRenderer.enabled = true;

            IsHighlighted = true;
        }

        /// <summary>
        /// Stops the visual effect and restores only state owned by this component.
        /// </summary>
        public void StopHighlight()
        {
            if (highlightRenderer != null && hasCapturedShellRendererState)
            {
                highlightRenderer.enabled = originalShellRendererEnabled;
                highlightRenderer.SetPropertyBlock(originalShellPropertyBlock);
            }

            if (faceHighlightRenderer != null && hasCapturedFaceRendererState)
            {
                faceHighlightRenderer.enabled = originalFaceRendererEnabled;
                faceHighlightRenderer.SetPropertyBlock(originalFacePropertyBlock);
            }

            if (shellTransform != null && hasCapturedTransformState)
                shellTransform.localScale = originalLocalScale;

            hasCapturedShellRendererState = false;
            hasCapturedFaceRendererState = false;
            hasCapturedTransformState = false;
            originalShellPropertyBlock = null;
            originalFacePropertyBlock = null;
            IsHighlighted = false;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
                StartHighlight();
            else
                StopHighlight();
        }

        private void Update()
        {
            if (!IsHighlighted)
                return;

            float pulseStrength = EvaluatePulseStrength();
            ApplyShellPropertyBlock(pulseStrength);
            ApplyFacePropertyBlock(pulseStrength);
        }

        private void OnDisable()
        {
            StopHighlight();
        }

        [ContextMenu("Preview Reaction Highlight On")]
        private void PreviewHighlightOn()
        {
            StartHighlight();
        }

        [ContextMenu("Preview Reaction Highlight Off")]
        private void PreviewHighlightOff()
        {
            StopHighlight();
        }

        private void CaptureOriginalState()
        {
            if (!hasCapturedShellRendererState && highlightRenderer != null)
            {
                originalShellRendererEnabled = highlightRenderer.enabled;
                originalShellPropertyBlock = new MaterialPropertyBlock();
                highlightRenderer.GetPropertyBlock(originalShellPropertyBlock);
                hasCapturedShellRendererState = true;
            }

            if (!hasCapturedFaceRendererState && faceHighlightRenderer != null)
            {
                originalFaceRendererEnabled = faceHighlightRenderer.enabled;
                originalFacePropertyBlock = new MaterialPropertyBlock();
                faceHighlightRenderer.GetPropertyBlock(originalFacePropertyBlock);
                hasCapturedFaceRendererState = true;
            }

            if (!hasCapturedTransformState && shellTransform != null)
            {
                originalLocalScale = shellTransform.localScale;
                hasCapturedTransformState = true;
            }
        }

        private void ApplyShellTransform()
        {
            if (shellTransform == null || !hasCapturedTransformState)
                return;

            shellTransform.localScale = applyShellScale
                ? originalLocalScale * shellScale
                : originalLocalScale;
        }

        private void SyncFaceMesh()
        {
            if (frontFaceMeshFilter == null || faceHighlightMeshFilter == null)
                return;

            faceHighlightMeshFilter.sharedMesh = frontFaceMeshFilter.sharedMesh;
        }

        private void ApplyShellPropertyBlock(float pulseStrength)
        {
            if (highlightRenderer == null)
                return;

            shellWorkingPropertyBlock ??= new MaterialPropertyBlock();
            highlightRenderer.GetPropertyBlock(shellWorkingPropertyBlock);
            shellWorkingPropertyBlock.SetColor(RimColorPropertyId, rimColor);
            shellWorkingPropertyBlock.SetFloat(AlphaPropertyId, alpha);
            shellWorkingPropertyBlock.SetFloat(FresnelPowerPropertyId, fresnelPower);
            shellWorkingPropertyBlock.SetFloat(FresnelIntensityPropertyId, fresnelIntensity);
            shellWorkingPropertyBlock.SetFloat(SurfaceIntensityPropertyId, 0f);
            shellWorkingPropertyBlock.SetFloat(PulseStrengthPropertyId, pulseStrength);
            shellWorkingPropertyBlock.SetFloat(
                VertexExtrusionPropertyId,
                applyVertexExtrusion ? vertexExtrusion : 0f);
            highlightRenderer.SetPropertyBlock(shellWorkingPropertyBlock);
        }

        private void ApplyFacePropertyBlock(float pulseStrength)
        {
            if (faceHighlightRenderer == null)
                return;

            faceWorkingPropertyBlock ??= new MaterialPropertyBlock();
            faceHighlightRenderer.GetPropertyBlock(faceWorkingPropertyBlock);
            faceWorkingPropertyBlock.SetColor(RimColorPropertyId, rimColor);
            faceWorkingPropertyBlock.SetFloat(AlphaPropertyId, faceAlpha);
            faceWorkingPropertyBlock.SetFloat(FresnelPowerPropertyId, fresnelPower);
            faceWorkingPropertyBlock.SetFloat(FresnelIntensityPropertyId, 0f);
            faceWorkingPropertyBlock.SetFloat(SurfaceIntensityPropertyId, faceSurfaceIntensity);
            faceWorkingPropertyBlock.SetFloat(PulseStrengthPropertyId, pulseStrength);
            faceWorkingPropertyBlock.SetFloat(VertexExtrusionPropertyId, faceVertexExtrusion);
            faceHighlightRenderer.SetPropertyBlock(faceWorkingPropertyBlock);
        }

        private float EvaluatePulseStrength()
        {
            float period = Mathf.Max(0.1f, pulsePeriod);
            float phase = (Time.unscaledTime / period) * (Mathf.PI * 2f);
            float normalizedWave = (Mathf.Sin(phase) + 1f) * 0.5f;
            return Mathf.Lerp(pulseMinStrength, pulseMaxStrength, normalizedWave);
        }
    }
}
