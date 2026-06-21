using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Win Decision Controller")]
    public sealed class MahjongWinDecisionController : MonoBehaviour
    {
        // PROTOTYPE: Minimal self-draw win decision UI until formal round result flow is introduced.
        [Header("Win Decision")]
        [SerializeField] private GameObject winDecisionRoot;

        private bool warnedMissingRoot;

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
            if (winDecisionRoot != null)
            {
                winDecisionRoot.SetActive(visible);
                return;
            }

            WarnMissingOnce(ref warnedMissingRoot, "WinDecisionRoot is not assigned.");
        }

        private void CacheReferences()
        {
            WarnMissingReferences();
        }

        private void WarnMissingReferences()
        {
            if (winDecisionRoot == null)
                WarnMissingOnce(ref warnedMissingRoot, "WinDecisionRoot is not assigned.");
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
