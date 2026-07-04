using TMPro;
using MahjongPrototype.Diagnostics;
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
            FuritenUiLifecycleTrace.LogSetVisible(
                "FuritenController.Awake.start",
                this,
                furitenText != null ? furitenText.gameObject : null,
                false);
            CacheReferences();
            EnsureTextInitialized();
            FuritenUiLifecycleTrace.LogSetVisible(
                "FuritenController.Awake.end",
                this,
                furitenText != null ? furitenText.gameObject : null,
                furitenText != null && furitenText.gameObject.activeSelf);
        }

        public void SetVisible(bool visible)
        {
            FuritenUiLifecycleTrace.LogSetVisible(
                "FuritenController.SetVisible.start",
                this,
                furitenText != null ? furitenText.gameObject : null,
                visible);
            CacheReferences();
            EnsureTextInitialized();

            if (furitenText == null)
            {
                WarnMissingTextOnce();
                return;
            }

            if (furitenText.gameObject.activeSelf != visible)
                furitenText.gameObject.SetActive(visible);
            FuritenUiLifecycleTrace.LogSetVisible(
                "FuritenController.SetVisible.end",
                this,
                furitenText.gameObject,
                visible);
        }

        public void Clear()
        {
            FuritenUiLifecycleTrace.LogSetVisible(
                "FuritenController.Clear.start",
                this,
                furitenText != null ? furitenText.gameObject : null,
                false);
            SetVisible(false);
            FuritenUiLifecycleTrace.LogSetVisible(
                "FuritenController.Clear.end",
                this,
                furitenText != null ? furitenText.gameObject : null,
                false);
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
