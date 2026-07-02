using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Zero Han Tenpai Controller")]
    public sealed class MahjongZeroHanTenpaiController : MonoBehaviour
    {
        private const string DisplayText = "テンパイ（役なし）";

        [SerializeField] private TMP_Text zeroHanTenpaiText;

        private bool warnedMissingText;
        private bool textInitialized;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            EnsureTextInitialized();
        }

        public void SetVisible(bool visible)
        {
            CacheReferences();
            EnsureTextInitialized();

            if (zeroHanTenpaiText == null)
            {
                WarnMissingTextOnce();
                return;
            }

            if (zeroHanTenpaiText.gameObject.activeSelf != visible)
                zeroHanTenpaiText.gameObject.SetActive(visible);
        }

        public void Clear()
        {
            SetVisible(false);
        }

        private void CacheReferences()
        {
            if (zeroHanTenpaiText == null)
                zeroHanTenpaiText = GetComponent<TMP_Text>();
        }

        private void EnsureTextInitialized()
        {
            if (textInitialized || zeroHanTenpaiText == null)
                return;

            zeroHanTenpaiText.text = DisplayText;
            textInitialized = true;
        }

        private void WarnMissingTextOnce()
        {
            if (warnedMissingText)
                return;

            warnedMissingText = true;
            Debug.LogWarning(
                $"{nameof(MahjongZeroHanTenpaiController)}: Zero Han Tenpai Text is not assigned.",
                this);
        }
    }
}
