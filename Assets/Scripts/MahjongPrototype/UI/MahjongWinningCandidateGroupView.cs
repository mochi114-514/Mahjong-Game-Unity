using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Winning Candidate Group View")]
    public sealed class MahjongWinningCandidateGroupView : MonoBehaviour
    {
        [SerializeField] private GameObject headingRoot;
        [SerializeField] private TMP_Text headingText;
        [SerializeField] private Transform candidateContainer;

        public bool HasHeadingText => headingText != null;
        public bool HasCandidateContainer =>
            candidateContainer != null && candidateContainer.IsChildOf(transform);
        public Transform CandidateContainer => candidateContainer;

        public void SetHeading(bool visible, string text)
        {
            if (headingText != null)
            {
                headingText.text = visible ? text ?? string.Empty : string.Empty;
                headingText.raycastTarget = false;
            }

            if (headingRoot != null)
                headingRoot.SetActive(visible);
        }
    }
}
