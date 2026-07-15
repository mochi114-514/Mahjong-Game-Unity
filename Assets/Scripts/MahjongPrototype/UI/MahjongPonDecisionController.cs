using System.Collections.Generic;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

        private readonly List<Button> dynamicMeldButtons = new List<Button>();
        private bool warnedMissingRoot;
        private bool warnedMissingPonButton;
        private bool warnedMissingInputController;
        private UnityAction selfKanDeclineAction;

        public void SetPonDecision(bool visible, Tile? calledTile)
        {
            SetMeldCallDecision(visible, false, null, null, null, false, calledTile);
        }

        public void SetMeldCallDecision(
            bool showPon,
            IReadOnlyList<ChiOption> chiOptions,
            Tile? calledTile)
        {
            SetMeldCallDecision(showPon, false, chiOptions, null, null, false, calledTile);
        }

        // Compatibility overload retained for the existing pon/chi/ankan UI path.
        public void SetMeldCallDecision(
            bool showPon,
            bool showDaiminkan,
            IReadOnlyList<ChiOption> chiOptions,
            IReadOnlyList<Tile> ankanCandidates,
            Tile? calledTile)
        {
            SetMeldCallDecision(
                showPon,
                showDaiminkan,
                chiOptions,
                ankanCandidates,
                null,
                false,
                calledTile);
        }

        public void SetMeldCallDecision(
            bool showPon,
            bool showDaiminkan,
            IReadOnlyList<ChiOption> chiOptions,
            IReadOnlyList<Tile> ankanCandidates,
            IReadOnlyList<SelfKanCandidate> selfKanCandidates,
            bool showSelfKanDecline,
            Tile? calledTile)
        {
            if (ponDecisionRoot == null)
            {
                WarnMissingOnce(ref warnedMissingRoot, "PonDecisionRoot is not assigned.");
                return;
            }

            ClearDynamicMeldButtons();
            bool showChi = chiOptions != null && chiOptions.Count > 0;
            bool showAnkan = ankanCandidates != null && ankanCandidates.Count > 0;
            bool showSelfKan = selfKanCandidates != null && selfKanCandidates.Count > 0;
            bool showReactionDecision = showPon || showDaiminkan || showChi;
            bool visible = showReactionDecision || showAnkan || showSelfKan;
            ponDecisionRoot.SetActive(visible);
            SetStaticButtonVisibility(showPon, showReactionDecision || showSelfKanDecline, showSelfKanDecline);
            if (decisionLabel != null)
            {
                decisionLabel.text = visible && calledTile.HasValue
                    ? $"鳴き {calledTile.Value}"
                    : showAnkan
                        ? "暗槓"
                        : string.Empty;
            }

            if (!showDaiminkan && !showChi && !showAnkan && !showSelfKan)
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

            ConfigureSelfKanDecline(showSelfKanDecline);

            if (showDaiminkan)
                CreateMeldButton("Daiminkan", "大明槓", MeldCallKind.Kan, 0);
            if (showChi)
            {
                for (int i = 0; i < chiOptions.Count; i++)
                    CreateChiOptionButton(chiOptions[i]);
            }
            if (showAnkan)
            {
                for (int i = 0; i < ankanCandidates.Count; i++)
                    CreateAnkanButton(ankanCandidates[i]);
            }
            if (showSelfKan)
            {
                for (int i = 0; i < selfKanCandidates.Count; i++)
                    CreateSelfKanButton(selfKanCandidates[i]);
            }
        }

        private void SetStaticButtonVisibility(
            bool showPon,
            bool showDecline,
            bool showSelfKanDecline)
        {
            if (ponButton != null)
                ponButton.gameObject.SetActive(showPon);
            else if (showPon)
                WarnMissingOnce(ref warnedMissingPonButton, "PonButton is not assigned.");

            if (declineButton != null)
            {
                declineButton.gameObject.SetActive(showDecline);
                ConfigureSelfKanDecline(showSelfKanDecline);
                if (showDecline)
                    SetButtonLabel(declineButton, showSelfKanDecline ? "カンしない" : "拒否");
            }
        }

        private void CreateChiOptionButton(ChiOption option)
        {
            if (option == null)
                return;

            CreateMeldButton(
                $"ChiOption_{option.OptionId}",
                $"チー {FormatTiles(option.MeldTiles)}",
                MeldCallKind.Chi,
                option.OptionId);
        }

        private void CreateAnkanButton(Tile tile)
        {
            if (!tile.IsValid)
                return;

            CreateMeldButton(
                $"Ankan_{tile.TypeIndex}",
                $"暗槓 {tile}",
                MeldCallKind.Kan,
                tile.TypeIndex);
        }

        private void CreateSelfKanButton(SelfKanCandidate candidate)
        {
            if (candidate == null)
                return;

            Button button = Instantiate(ponButton, ponButton.transform.parent);
            button.name = $"{candidate.Kind}_{candidate.Tile.TypeIndex}_{candidate.SourcePonMeldIndex}";
            if (declineButton != null)
                button.transform.SetSiblingIndex(declineButton.transform.GetSiblingIndex());
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => inputController.RequestSelfKan(
                candidate.Kind,
                candidate.Tile.TypeIndex,
                candidate.SourcePonMeldIndex));
            button.gameObject.SetActive(true);
            string action = candidate.Kind == SelfKanKind.Ankan ? "暗槓" : "加槓";
            SetButtonLabel(button, $"{action} {candidate.Tile}");
            dynamicMeldButtons.Add(button);
        }

        private void CreateMeldButton(
            string buttonName,
            string label,
            MeldCallKind kind,
            int optionId)
        {
            Button button = Instantiate(ponButton, ponButton.transform.parent);
            button.name = buttonName;
            if (declineButton != null)
                button.transform.SetSiblingIndex(declineButton.transform.GetSiblingIndex());
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => inputController.RequestMeldCall(kind, optionId));
            button.gameObject.SetActive(true);
            SetButtonLabel(button, label);
            dynamicMeldButtons.Add(button);
        }

        private void ClearDynamicMeldButtons()
        {
            for (int i = dynamicMeldButtons.Count - 1; i >= 0; i--)
            {
                Button button = dynamicMeldButtons[i];
                if (button == null)
                    continue;

                button.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(button.gameObject);
                else
                    DestroyImmediate(button.gameObject);
            }

            dynamicMeldButtons.Clear();
        }

        private void ConfigureSelfKanDecline(bool enabled)
        {
            if (declineButton == null)
                return;

            if (selfKanDeclineAction != null)
            {
                declineButton.onClick.RemoveListener(selfKanDeclineAction);
                selfKanDeclineAction = null;
            }

            if (!enabled || inputController == null)
                return;

            selfKanDeclineAction = () => inputController.RequestDeclineSelfKan();
            declineButton.onClick.AddListener(selfKanDeclineAction);
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
