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
        private static readonly int PulseStrengthPropertyId = Shader.PropertyToID("_PulseStrength");
        private static readonly int VertexExtrusionPropertyId = Shader.PropertyToID("_VertexExtrusion");

        [Header("References")]
        [SerializeField] private MeshRenderer highlightRenderer;
        [SerializeField] private Transform shellTransform;

        [Header("Shell Shape")]
        [SerializeField] private bool applyShellScale = true;
        [SerializeField, Range(1.01f, 1.03f)] private float shellScale = 1.02f;
        [SerializeField] private bool applyVertexExtrusion;
        [SerializeField, Range(0f, 0.01f)] private float vertexExtrusion;

        [Header("Fresnel")]
        [SerializeField, Range(0.5f, 8f)] private float fresnelPower = 4f;
        [SerializeField, Range(0f, 2f)] private float fresnelIntensity = 1f;
        [SerializeField] private Color rimColor = new Color(1f, 0.88f, 0.55f, 1f);
        [SerializeField, Range(0f, 1f)] private float alpha = 0.18f;

        [Header("Pulse")]
        [SerializeField, Min(0.1f)] private float pulsePeriod = 2f;
        [SerializeField, Range(0f, 2f)] private float pulseMinStrength = 0.85f;
        [SerializeField, Range(0f, 2f)] private float pulseMaxStrength = 1f;

        private MaterialPropertyBlock workingPropertyBlock;
        private MaterialPropertyBlock originalPropertyBlock;
        private bool hasCapturedRendererState;
        private bool originalRendererEnabled;
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

            if (highlightRenderer == null)
            {
                Debug.LogWarning($"{nameof(Mahjong3DTileReactionHighlight)}: Highlight Renderer is not assigned.", this);
                return;
            }

            if (shellTransform == null)
                shellTransform = highlightRenderer.transform;

            CaptureOriginalState();
            ApplyShellTransform();
            ApplyPropertyBlock(EvaluatePulseStrength());
            highlightRenderer.enabled = true;
            IsHighlighted = true;
        }

        /// <summary>
        /// Stops the visual effect and restores only state owned by this component.
        /// </summary>
        public void StopHighlight()
        {
            if (highlightRenderer != null && hasCapturedRendererState)
            {
                highlightRenderer.enabled = originalRendererEnabled;
                highlightRenderer.SetPropertyBlock(originalPropertyBlock);
            }

            if (shellTransform != null && hasCapturedTransformState)
                shellTransform.localScale = originalLocalScale;

            hasCapturedRendererState = false;
            hasCapturedTransformState = false;
            originalPropertyBlock = null;
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
            if (!IsHighlighted || highlightRenderer == null)
                return;

            ApplyPropertyBlock(EvaluatePulseStrength());
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
            if (!hasCapturedRendererState)
            {
                originalRendererEnabled = highlightRenderer.enabled;
                originalPropertyBlock = new MaterialPropertyBlock();
                highlightRenderer.GetPropertyBlock(originalPropertyBlock);
                hasCapturedRendererState = true;
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

        private void ApplyPropertyBlock(float pulseStrength)
        {
            workingPropertyBlock ??= new MaterialPropertyBlock();
            highlightRenderer.GetPropertyBlock(workingPropertyBlock);
            workingPropertyBlock.SetColor(RimColorPropertyId, rimColor);
            workingPropertyBlock.SetFloat(AlphaPropertyId, alpha);
            workingPropertyBlock.SetFloat(FresnelPowerPropertyId, fresnelPower);
            workingPropertyBlock.SetFloat(FresnelIntensityPropertyId, fresnelIntensity);
            workingPropertyBlock.SetFloat(PulseStrengthPropertyId, pulseStrength);
            workingPropertyBlock.SetFloat(
                VertexExtrusionPropertyId,
                applyVertexExtrusion ? vertexExtrusion : 0f);
            highlightRenderer.SetPropertyBlock(workingPropertyBlock);
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
