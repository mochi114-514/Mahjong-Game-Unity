using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Reach Decision Controller")]
    public sealed class MahjongReachDecisionController : MonoBehaviour
    {
        [SerializeField] private GameObject reachDecisionRoot;
        [SerializeField] private GameObject reachCancelRoot;

        private bool warnedMissingRoot;
        private bool warnedMissingCancelRoot;

        public void SetVisible(bool visible)
        {
            SetReachUiVisible(visible, false);
        }

        public void SetReachUiVisible(bool showDecision, bool showCancel)
        {
            if (reachDecisionRoot != null)
            {
                reachDecisionRoot.SetActive(showDecision);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingRoot, "ReachDecisionRoot is not assigned.");
            }

            if (reachCancelRoot != null)
            {
                reachCancelRoot.SetActive(showCancel);
            }
            else if (showCancel)
            {
                WarnMissingOnce(ref warnedMissingCancelRoot, "ReachCancelRoot is not assigned.");
            }
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongReachDecisionController)}: {message}", this);
        }
    }
}
