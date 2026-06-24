using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Seat Wind View")]
    public sealed class MahjongSeatWindView : MonoBehaviour
    {
        [Header("Seat Wind")]
        [SerializeField] private TMP_Text windText;

        private bool warnedMissingWindText;

        public void Render(SeatId seatId)
        {
            if (!TryGetWindText(out TMP_Text target))
                return;

            target.text = seatId.ToString();
        }

        public void Clear()
        {
            if (!TryGetWindText(out TMP_Text target))
                return;

            target.text = string.Empty;
        }

        private bool TryGetWindText(out TMP_Text target)
        {
            target = windText;
            if (target != null)
                return true;

            if (!warnedMissingWindText)
            {
                Debug.LogWarning(
                    $"{nameof(MahjongSeatWindView)}: Wind Text is not assigned.",
                    this);
                warnedMissingWindText = true;
            }

            return false;
        }
    }
}
