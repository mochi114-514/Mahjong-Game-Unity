using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Win Decision Controller")]
    public sealed class MahjongWinDecisionController : MonoBehaviour
    {
        // PROTOTYPE: Minimal win decision UI until formal round result flow is introduced.
        [Header("Win Decision")]
        [SerializeField] private GameObject winDecisionRoot;
        [SerializeField] private TMP_Text winButtonLabel;

        private bool warnedMissingRoot;
        private bool warnedMissingWinButtonLabel;

        public void SetVisible(bool visible)
        {
            SetWinDecision(visible, null);
        }

        public void SetWinDecision(bool visible, WinType? winType)
        {
            SetWinButtonLabel(winType);
            SetRootVisible(visible);
        }

        public void SetAbortiveDrawDecision(bool visible)
        {
            if (winButtonLabel != null)
                winButtonLabel.text = "流局";
            else
            {
                WarnMissingOnce(
                    ref warnedMissingWinButtonLabel,
                    "WinButtonLabel is not assigned.");
            }

            SetRootVisible(visible);
        }

        private void SetRootVisible(bool visible)
        {
            if (winDecisionRoot != null)
            {
                winDecisionRoot.SetActive(visible);
                return;
            }

            WarnMissingOnce(ref warnedMissingRoot, "WinDecisionRoot is not assigned.");
        }

        private void SetWinButtonLabel(WinType? winType)
        {
            if (winButtonLabel == null)
            {
                WarnMissingOnce(
                    ref warnedMissingWinButtonLabel,
                    "WinButtonLabel is not assigned.");
                return;
            }

            switch (winType)
            {
                case WinType.Tsumo:
                    winButtonLabel.text = "ツモ";
                    break;
                case WinType.Ron:
                    winButtonLabel.text = "ロン";
                    break;
                default:
                    winButtonLabel.text = "和了";
                    break;
            }
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongWinDecisionController)}: {message}", this);
        }
    }
}
