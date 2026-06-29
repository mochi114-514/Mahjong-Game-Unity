using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Reach Decision Controller")]
    public sealed class MahjongReachDecisionController : MonoBehaviour
    {
        [SerializeField] private GameObject reachDecisionRoot;

        private bool warnedMissingRoot;

        public void SetVisible(bool visible)
        {
            if (reachDecisionRoot != null)
            {
                reachDecisionRoot.SetActive(visible);
                return;
            }

            WarnMissingOnce(ref warnedMissingRoot, "ReachDecisionRoot is not assigned.");
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
