using System;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Tile Button View")]
    public sealed class TileButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private int handIndex = -1;
        private Action<int> clicked;
        private bool warnedMissingButton;
        private bool warnedMissingLabel;

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
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingButton, "Button is not assigned.");
            }
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        public void Initialize(int index, Tile tile, Action<int> onClicked)
        {
            CacheReferences();

            handIndex = index;
            clicked = onClicked;

            if (label != null)
            {
                // PROTOTYPE: 牌画像ではなくテキスト牌で表示する。
                label.text = tile.ToString();
            }
            else
            {
                WarnMissingOnce(ref warnedMissingLabel, "TMP_Text label is not assigned.");
            }
        }

        public void SetInteractable(bool interactable)
        {
            CacheReferences();

            if (button != null)
                button.interactable = interactable;
        }

        private void CacheReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (label == null)
                label = GetComponentInChildren<TMP_Text>(true);
        }

        private void HandleClicked()
        {
            if (button == null)
            {
                WarnMissingOnce(ref warnedMissingButton, "Button is not assigned.");
                return;
            }

            clicked?.Invoke(handIndex);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(TileButtonView)}: {message}", this);
        }
    }
}
