using TMPro;
using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Pon Decision Controller")]
    public sealed class MahjongPonDecisionController : MonoBehaviour
    {
        [SerializeField] private GameObject ponDecisionRoot;
        [SerializeField] private TMP_Text decisionLabel;

        private bool warnedMissingRoot;

        public void SetPonDecision(bool visible, Tile? calledTile)
        {
            if (ponDecisionRoot == null)
            {
                WarnMissingOnce(ref warnedMissingRoot, "PonDecisionRoot is not assigned.");
                return;
            }

            ponDecisionRoot.SetActive(visible);
            if (decisionLabel != null)
                decisionLabel.text = visible && calledTile.HasValue
                    ? $"ポン {calledTile.Value}"
                    : string.Empty;
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongPonDecisionController)}: {message}", this);
        }
    }
}
