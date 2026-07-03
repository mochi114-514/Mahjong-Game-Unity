using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Furiten Controller")]
    public sealed class MahjongFuritenController : MonoBehaviour
    {
        private const string DisplayText = "フリテン";

        [SerializeField] private TMP_Text furitenText;

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

            if (furitenText == null)
            {
                WarnMissingTextOnce();
                return;
            }

            if (furitenText.gameObject.activeSelf != visible)
                furitenText.gameObject.SetActive(visible);
        }

        public void Clear()
        {
            SetVisible(false);
        }

        private void CacheReferences()
        {
            if (furitenText == null)
                furitenText = GetComponent<TMP_Text>();
        }

        private void EnsureTextInitialized()
        {
            if (textInitialized || furitenText == null)
                return;

            furitenText.text = DisplayText;
            textInitialized = true;
        }

        private void WarnMissingTextOnce()
        {
            if (warnedMissingText)
                return;

            warnedMissingText = true;
            Debug.LogWarning(
                $"{nameof(MahjongFuritenController)}: Furiten Text is not assigned.",
                this);
        }
    }
}
