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

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            SetVisible(false);
        }

        private void OnEnable()
        {
            CacheReferences();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            SetWinDecision(visible, null);
        }

        public void SetWinDecision(bool visible, WinType? winType)
        {
            CacheReferences();
            SetWinButtonLabel(winType);

            if (winDecisionRoot != null)
            {
                winDecisionRoot.SetActive(visible);
                return;
            }

            WarnMissingOnce(ref warnedMissingRoot, "WinDecisionRoot is not assigned.");
        }

        private void CacheReferences()
        {
            if (winButtonLabel == null && winDecisionRoot != null)
            {
                TMP_Text[] labels = winDecisionRoot.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    TMP_Text label = labels[i];
                    if (label != null &&
                        label.transform.parent != null &&
                        label.transform.parent.gameObject.name == "WinButton")
                    {
                        winButtonLabel = label;
                        break;
                    }
                }
            }

            WarnMissingReferences();
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

        private void WarnMissingReferences()
        {
            if (winDecisionRoot == null)
                WarnMissingOnce(ref warnedMissingRoot, "WinDecisionRoot is not assigned.");

            if (winButtonLabel == null)
                WarnMissingOnce(ref warnedMissingWinButtonLabel, "WinButtonLabel is not assigned.");
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
