using System.Collections.Generic;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Meld Call Decision Controller")]
    public sealed class MahjongPonDecisionController : MonoBehaviour
    {
        [SerializeField] private GameObject ponDecisionRoot;
        [SerializeField] private TMP_Text decisionLabel;
        [SerializeField] private Button ponButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private MahjongUiInputController inputController;

        private readonly List<Button> chiOptionButtons = new List<Button>();
        private bool warnedMissingRoot;
        private bool warnedMissingPonButton;
        private bool warnedMissingInputController;

        public void SetPonDecision(bool visible, Tile? calledTile)
        {
            SetMeldCallDecision(visible, null, calledTile);
        }

        public void SetMeldCallDecision(
            bool showPon,
            IReadOnlyList<ChiOption> chiOptions,
            Tile? calledTile)
        {
            if (ponDecisionRoot == null)
            {
                WarnMissingOnce(ref warnedMissingRoot, "PonDecisionRoot is not assigned.");
                return;
            }

            ClearChiOptionButtons();
            bool showChi = chiOptions != null && chiOptions.Count > 0;
            bool visible = showPon || showChi;
            ponDecisionRoot.SetActive(visible);
            SetStaticButtonVisibility(showPon, visible);
            if (decisionLabel != null)
            {
                decisionLabel.text = visible && calledTile.HasValue
                    ? $"鳴き {calledTile.Value}"
                    : string.Empty;
            }

            if (!showChi)
                return;

            if (ponButton == null)
            {
                WarnMissingOnce(ref warnedMissingPonButton, "PonButton is not assigned.");
                return;
            }

            if (inputController == null)
                inputController = GetComponent<MahjongUiInputController>();
            if (inputController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingInputController,
                    "MahjongUiInputController is not assigned.");
                return;
            }

            for (int i = 0; i < chiOptions.Count; i++)
                CreateChiOptionButton(chiOptions[i]);
        }

        private void SetStaticButtonVisibility(bool showPon, bool visible)
        {
            if (ponButton != null)
                ponButton.gameObject.SetActive(showPon);
            else if (showPon)
                WarnMissingOnce(ref warnedMissingPonButton, "PonButton is not assigned.");

            if (declineButton != null)
                declineButton.gameObject.SetActive(visible);
        }

        private void CreateChiOptionButton(ChiOption option)
        {
            if (option == null)
                return;

            Button button = Instantiate(ponButton, ponButton.transform.parent);
            int optionId = option.OptionId;
            button.name = $"ChiOption_{optionId}";
            if (declineButton != null)
                button.transform.SetSiblingIndex(declineButton.transform.GetSiblingIndex());
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => inputController.RequestMeldCall(MeldCallKind.Chi, optionId));
            button.gameObject.SetActive(true);
            SetButtonLabel(button, $"チー {FormatTiles(option.MeldTiles)}");
            chiOptionButtons.Add(button);
        }

        private void ClearChiOptionButtons()
        {
            for (int i = chiOptionButtons.Count - 1; i >= 0; i--)
            {
                Button button = chiOptionButtons[i];
                if (button == null)
                    continue;

                button.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(button.gameObject);
                else
                    DestroyImmediate(button.gameObject);
            }

            chiOptionButtons.Clear();
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
        }

        private static string FormatTiles(IReadOnlyList<Tile> tiles)
        {
            if (tiles == null || tiles.Count <= 0)
                return string.Empty;

            string[] labels = new string[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
                labels[i] = tiles[i].ToString();

            return string.Join(" ", labels);
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
