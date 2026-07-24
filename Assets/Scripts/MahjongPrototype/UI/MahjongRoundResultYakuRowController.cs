using System.Collections;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Round Result Yaku Row Controller")]
    public sealed class MahjongRoundResultYakuRowController : MonoBehaviour
    {
        [SerializeField] private TMP_Text yakuNameText;
        [SerializeField] private TMP_Text valueText;

        private bool warnedMissingYakuNameText;
        private bool warnedMissingValueText;
        private Color yakuNameColor;
        private Color valueColor;
        private Vector3 normalScale;
        private bool presentationCached;

        public void Bind(EvaluatedYaku yaku)
        {
            SetTextOrWarn(
                yakuNameText,
                yaku.DisplayName ?? string.Empty,
                ref warnedMissingYakuNameText,
                "YakuNameText is not assigned.");
            SetTextOrWarn(
                valueText,
                yaku.YakumanMultiplier > 0
                    ? YakumanMultiplierFormatter.Format(yaku.YakumanMultiplier)
                    : $"{(int)yaku.Han}翻",
                ref warnedMissingValueText,
                "ValueText is not assigned.");
        }

        public void SetRevealVisible(bool visible)
        {
            CachePresentation();
            SetTextAlpha(yakuNameText, yakuNameColor, visible ? 1f : 0f);
            SetTextAlpha(valueText, valueColor, visible ? 1f : 0f);
            transform.localScale = normalScale;
        }

        public IEnumerator PlayReveal(float duration)
        {
            CachePresentation();
            if (duration <= 0f)
            {
                SetRevealVisible(true);
                yield break;
            }

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = EaseOutCubic(t);
                SetTextAlpha(yakuNameText, yakuNameColor, alpha);
                SetTextAlpha(valueText, valueColor, alpha);
                transform.localScale = normalScale * GetRevealScaleMultiplier(t);
                yield return null;
            }

            SetRevealVisible(true);
        }

        private static float GetRevealScaleMultiplier(float progress)
        {
            const float startScale = 0.9f;
            const float overshootScale = 1.055f;
            const float overshootProgress = 0.72f;

            if (progress < overshootProgress)
            {
                float approach = EaseOutCubic(progress / overshootProgress);
                return Mathf.Lerp(startScale, overshootScale, approach);
            }

            float settle = Mathf.SmoothStep(
                0f,
                1f,
                (progress - overshootProgress) / (1f - overshootProgress));
            return Mathf.Lerp(overshootScale, 1f, settle);
        }

        private static float EaseOutCubic(float progress)
        {
            float inverse = 1f - Mathf.Clamp01(progress);
            return 1f - inverse * inverse * inverse;
        }

        private void CachePresentation()
        {
            if (presentationCached)
                return;

            presentationCached = true;
            normalScale = transform.localScale;
            if (yakuNameText != null)
                yakuNameColor = yakuNameText.color;
            if (valueText != null)
                valueColor = valueText.color;
        }

        private static void SetTextAlpha(TMP_Text text, Color baseColor, float alpha)
        {
            if (text == null)
                return;

            baseColor.a *= alpha;
            text.color = baseColor;
        }

        private void SetTextOrWarn(
            TMP_Text text,
            string value,
            ref bool warned,
            string warning)
        {
            if (text != null)
            {
                text.text = value;
                return;
            }

            WarnMissingOnce(ref warned, warning);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongRoundResultYakuRowController)}: {message}", this);
        }
    }
}
