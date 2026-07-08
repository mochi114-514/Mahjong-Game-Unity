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

        public void Bind(EvaluatedYaku yaku)
        {
            SetTextOrWarn(
                yakuNameText,
                yaku.DisplayName ?? string.Empty,
                ref warnedMissingYakuNameText,
                "YakuNameText is not assigned.");
            SetTextOrWarn(
                valueText,
                yaku.IsYakuman ? "役満" : $"{(int)yaku.Han}翻",
                ref warnedMissingValueText,
                "ValueText is not assigned.");
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
