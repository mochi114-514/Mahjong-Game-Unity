using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Reach Decision Controller")]
    public sealed class MahjongReachDecisionController : MonoBehaviour
    {
        [SerializeField] private GameObject reachDecisionRoot;

        private bool warnedMissingRoot;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
        }

        public void Configure(GameObject root)
        {
            reachDecisionRoot = root;
        }

        public void SetVisible(bool visible)
        {
            CacheReferences();
            if (reachDecisionRoot != null)
            {
                reachDecisionRoot.SetActive(visible);
                return;
            }

            WarnMissingOnce(ref warnedMissingRoot, "ReachDecisionRoot is not assigned.");
        }

        private void CacheReferences()
        {
            if (reachDecisionRoot != null)
                return;

            if (gameObject.name == "ReachDecisionArea")
            {
                reachDecisionRoot = gameObject;
                return;
            }

            Transform found = FindChildByName(transform.root, "ReachDecisionArea");
            if (found != null)
                reachDecisionRoot = found.gameObject;
        }

        private static Transform FindChildByName(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.gameObject.name == objectName)
                    return child;
            }

            return null;
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
