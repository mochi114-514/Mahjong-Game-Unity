using MahjongPrototype.Domain;
using UnityEngine;

namespace MahjongPrototype.UI3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI3D/Mahjong Table Center UI Controller")]
    public sealed class MahjongTableCenterUiController : MonoBehaviour
    {
        [Header("Presenters")]
        [SerializeField] private MahjongTableCenterTextPresenter textPresenter;

        private bool warnedMissingPresenterReferences;

        public void SetViewContext(MahjongViewContext context)
        {
            CacheReferences();
            if (textPresenter != null)
                textPresenter.SetViewContext(context);
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        public void Refresh(MahjongGameState state)
        {
            CacheReferences();

            if (state == null)
            {
                Clear();
                return;
            }

            if (textPresenter == null)
            {
                WarnMissingPresenterReferences();
                return;
            }

            textPresenter.Refresh(state);
        }

        public void Clear()
        {
            CacheReferences();

            if (textPresenter == null)
            {
                WarnMissingPresenterReferences();
                return;
            }

            textPresenter.Clear();
        }

        private void CacheReferences()
        {
            if (textPresenter == null)
                textPresenter = GetComponentInChildren<MahjongTableCenterTextPresenter>(true);
        }

        private void WarnMissingPresenterReferences()
        {
            if (textPresenter != null)
                return;

            WarnMissingOnce(
                ref warnedMissingPresenterReferences,
                "MahjongTableCenterTextPresenter is not assigned.");
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongTableCenterUiController)}: {message}", this);
        }
    }
}
