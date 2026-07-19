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

        [Header("Chi Options")]
        [SerializeField] private GameObject chiDecisionRoot;
        [SerializeField] private TMP_Text chiHeadingLabel;
        [SerializeField] private Transform chiOptionsContainer;
        [SerializeField] private MahjongChiOptionView chiOptionViewPrefab;
        [SerializeField] private MahjongTileSpriteCatalog chiTileSpriteCatalog;
        [SerializeField] private MahjongTileSpriteView chiTileViewPrefab;

        private readonly List<Button> dynamicMeldButtons = new List<Button>();
        private readonly List<MahjongChiOptionView> dynamicChiOptionViews =
            new List<MahjongChiOptionView>();
        private readonly HashSet<int> warnedMissingChiTileSpriteTypeIndexes =
            new HashSet<int>();
        private bool warnedMissingRoot;
        private bool warnedMissingPonButton;
        private bool warnedMissingInputController;
        private bool warnedMissingChiDecisionRoot;
        private bool warnedMissingChiHeadingLabel;
        private bool warnedMissingChiOptionsContainer;
        private bool warnedMissingChiOptionViewPrefab;
        private bool warnedInvalidChiOptionViewPrefab;
        private bool warnedMissingChiTileContainer;
        private bool warnedMissingChiTileSpriteCatalog;
        private bool warnedMissingChiTileViewPrefab;
        private bool warnedInvalidChiTileViewPrefab;
        private UnityAction selfKanDeclineAction;
        private bool hasReactionRequest;
        private long reactionRequestId;
        private int reactionWindowId;

        private void OnDisable()
        {
            ClearReactionMeldCallDecision();
        }

        /// <summary>
        /// Removes the provider-bound reaction controls. This is used when a
        /// reaction window closes or its UI host is disabled, so dynamically
        /// created chi/daiminkan buttons cannot outlive their request.
        /// </summary>
        public void ClearReactionMeldCallDecision()
        {
            ClearReactionRequest();
            ClearSelfKanDecisionResponseBinding();
            ClearDynamicMeldButtons();
            ClearDynamicChiOptions();
            ConfigureSelfKanDecline(false);
            if (ponDecisionRoot != null)
                ponDecisionRoot.SetActive(false);
        }

        public void SetPonDecision(bool visible, Tile? calledTile)
        {
            ClearReactionRequest();
            SetMeldCallDecision(visible, false, null, null, null, false, calledTile);
        }

        public void SetMeldCallDecision(
            bool showPon,
            IReadOnlyList<ChiOption> chiOptions,
            Tile? calledTile)
        {
            ClearReactionRequest();
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
            ClearReactionRequest();
            SetMeldCallDecision(
                showPon,
                showDaiminkan,
                chiOptions,
                ankanCandidates,
                null,
                false,
                calledTile);
        }

        /// <summary>
        /// Displays one immutable self-kan request. Unlike the compatibility
        /// path above, every button captures the request id and its option id;
        /// it never asks the game state for the currently available candidates.
        /// </summary>
        public void SetSelfKanDecision(
            long requestId,
            SelfKanDecisionRequest request)
        {
            ClearReactionRequest();
            ClearSelfKanDecisionResponseBinding();
            ClearDynamicMeldButtons();
            ClearDynamicChiOptions();
            ConfigureSelfKanDecline(false);

            if (ponDecisionRoot == null)
            {
                WarnMissingOnce(ref warnedMissingRoot, "PonDecisionRoot is not assigned.");
                return;
            }

            if (requestId <= 0 || request == null || request.Options.Count <= 0)
            {
                ponDecisionRoot.SetActive(false);
                return;
            }

            ponDecisionRoot.SetActive(true);
            SetStaticButtonVisibility(false, true, false);
            if (decisionLabel != null)
                decisionLabel.text = "カン";
            SetButtonLabel(declineButton, "スキップ");

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

            inputController.SetSelfKanDecisionResponseBinding(declineButton, requestId);
            bool showTargetTile = request.Options.Count > 1;
            for (int i = 0; i < request.Options.Count; i++)
            {
                CreateSelfKanDecisionButton(
                    requestId,
                    request.Options[i],
                    showTargetTile);
            }
        }

        /// <summary>
        /// Displays the meld portion of one immutable reaction request.
        /// The caller can keep the single pass action on the ron panel when
        /// ron and meld choices are offered together.
        /// </summary>
        public void SetReactionMeldCallDecision(
            bool showPon,
            bool showDaiminkan,
            IReadOnlyList<ChiOption> chiOptions,
            Tile? calledTile,
            bool showPass)
        {
            ClearReactionRequest();
            SetReactionMeldCallDecision(
                0,
                0,
                showPon,
                showDaiminkan,
                chiOptions,
                calledTile,
                showPass);
        }

        /// <summary>
        /// Displays a reaction request whose dynamic buttons retain the
        /// request/window identity that created them.
        /// </summary>
        public void SetReactionMeldCallDecision(
            long requestId,
            int windowId,
            bool showPon,
            bool showDaiminkan,
            IReadOnlyList<ChiOption> chiOptions,
            Tile? calledTile,
            bool showPass)
        {
            hasReactionRequest = requestId > 0 && windowId > 0;
            reactionRequestId = requestId;
            reactionWindowId = windowId;
            SetMeldCallDecisionCore(
                showPon,
                showDaiminkan,
                chiOptions,
                null,
                null,
                false,
                calledTile);

            if (declineButton == null)
                return;

            declineButton.gameObject.SetActive(showPass);
            if (showPass)
                SetButtonLabel(declineButton, "パス");
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
            ClearReactionRequest();
            SetMeldCallDecisionCore(
                showPon,
                showDaiminkan,
                chiOptions,
                ankanCandidates,
                selfKanCandidates,
                showSelfKanDecline,
                calledTile);
        }

        private void SetMeldCallDecisionCore(
            bool showPon,
            bool showDaiminkan,
            IReadOnlyList<ChiOption> chiOptions,
            IReadOnlyList<Tile> ankanCandidates,
            IReadOnlyList<SelfKanCandidate> selfKanCandidates,
            bool showSelfKanDecline,
            Tile? calledTile)
        {
            ClearSelfKanDecisionResponseBinding();
            ClearDynamicMeldButtons();
            ClearDynamicChiOptions();

            if (ponDecisionRoot == null)
            {
                WarnMissingOnce(ref warnedMissingRoot, "PonDecisionRoot is not assigned.");
                return;
            }

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

            bool needsDynamicMeldButton =
                showDaiminkan || showAnkan || showSelfKan;
            if (needsDynamicMeldButton && ponButton == null)
            {
                WarnMissingOnce(ref warnedMissingPonButton, "PonButton is not assigned.");
                needsDynamicMeldButton = false;
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

            if (showDaiminkan && needsDynamicMeldButton)
                CreateMeldButton("Daiminkan", "大明槓", MeldCallKind.Kan, 0);
            if (showChi)
                ShowChiOptions(chiOptions);
            if (showAnkan && needsDynamicMeldButton)
            {
                for (int i = 0; i < ankanCandidates.Count; i++)
                    CreateAnkanButton(ankanCandidates[i]);
            }
            if (showSelfKan && needsDynamicMeldButton)
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

        private void ShowChiOptions(IReadOnlyList<ChiOption> chiOptions)
        {
            if (chiDecisionRoot == null)
            {
                WarnMissingOnce(
                    ref warnedMissingChiDecisionRoot,
                    "ChiDecisionRoot is not assigned.");
                return;
            }

            chiDecisionRoot.SetActive(true);
            if (chiHeadingLabel != null &&
                IsSameOrChild(chiHeadingLabel.transform, chiDecisionRoot.transform))
                chiHeadingLabel.text = "チー";
            else
                WarnMissingOnce(
                    ref warnedMissingChiHeadingLabel,
                    "ChiHeadingLabel must be assigned under ChiDecisionRoot.");

            if (chiOptionsContainer == null ||
                !IsSameOrChild(chiOptionsContainer, chiDecisionRoot.transform))
            {
                WarnMissingOnce(
                    ref warnedMissingChiOptionsContainer,
                    "ChiOptionsContainer must be assigned under ChiDecisionRoot.");
                return;
            }

            if (chiOptionViewPrefab == null)
            {
                WarnMissingOnce(
                    ref warnedMissingChiOptionViewPrefab,
                    "ChiOptionViewPrefab is not assigned.");
                return;
            }

            for (int i = 0; i < chiOptions.Count; i++)
                CreateChiOptionView(chiOptions[i]);
        }

        private void CreateChiOptionView(ChiOption option)
        {
            if (option == null)
                return;

            MahjongChiOptionView optionView = Instantiate(
                chiOptionViewPrefab,
                chiOptionsContainer);
            optionView.name = $"ChiOption_{option.OptionId}";
            optionView.gameObject.SetActive(false);

            if (!optionView.HasClickableTileContainer ||
                !optionView.TrySetSelectionAction(
                    CreateMeldCallAction(MeldCallKind.Chi, option.OptionId)))
            {
                WarnMissingOnce(
                    ref warnedInvalidChiOptionViewPrefab,
                    "ChiOptionViewPrefab select Button must contain its tile container.");
                DestroyChiOptionView(optionView);
                return;
            }

            PopulateChiTileImages(optionView, option.MeldTiles);
            optionView.gameObject.SetActive(true);
            dynamicChiOptionViews.Add(optionView);
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

        private void CreateSelfKanDecisionButton(
            long requestId,
            SelfKanDecisionOption option,
            bool showTargetTile)
        {
            if (option == null)
                return;

            Button button = Instantiate(ponButton, ponButton.transform.parent);
            button.name = $"{option.Kind}_{option.OptionId}";
            if (declineButton != null)
                button.transform.SetSiblingIndex(declineButton.transform.GetSiblingIndex());
            // Remove persistent and runtime callbacks inherited from the
            // template before binding this immutable request/option pair.
            button.onClick = new Button.ButtonClickedEvent();
            int optionId = option.OptionId;
            button.onClick.AddListener(
                () => inputController.RequestSelfKanDecisionResponse(
                    requestId,
                    true,
                    optionId));
            button.gameObject.SetActive(true);
            SetButtonLabel(button, showTargetTile ? $"カン {option.Tile}" : "カン");
            dynamicMeldButtons.Add(button);
        }

        private Button CreateMeldButton(
            string buttonName,
            string label,
            MeldCallKind kind,
            int optionId)
        {
            Button button = Instantiate(ponButton, ponButton.transform.parent);
            button.name = buttonName;
            if (declineButton != null)
                button.transform.SetSiblingIndex(declineButton.transform.GetSiblingIndex());
            // Remove persistent and runtime callbacks inherited from the
            // template before binding the route selected for this button.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(CreateMeldCallAction(kind, optionId));
            button.gameObject.SetActive(true);
            SetButtonLabel(button, label);
            dynamicMeldButtons.Add(button);
            return button;
        }

        private void PopulateChiTileImages(
            MahjongChiOptionView optionView,
            IReadOnlyList<Tile> meldTiles)
        {
            if (optionView == null || meldTiles == null)
                return;

            if (!optionView.HasTileContainer)
            {
                WarnMissingOnce(
                    ref warnedMissingChiTileContainer,
                    "MahjongChiOptionView tile container must be assigned to a child Transform.");
                return;
            }

            if (chiTileSpriteCatalog == null)
            {
                WarnMissingOnce(
                    ref warnedMissingChiTileSpriteCatalog,
                    "Chi tile sprite catalog is not assigned.");
                return;
            }

            if (chiTileViewPrefab == null)
            {
                WarnMissingOnce(
                    ref warnedMissingChiTileViewPrefab,
                    "Chi tile view prefab is not assigned.");
                return;
            }

            if (!chiTileViewPrefab.HasTargetImage)
            {
                WarnMissingOnce(
                    ref warnedInvalidChiTileViewPrefab,
                    "Chi tile view prefab target Image is not assigned.");
                return;
            }

            optionView.ClearTiles();
            for (int i = 0; i < meldTiles.Count; i++)
            {
                Tile tile = meldTiles[i];
                if (!chiTileSpriteCatalog.TryGetSprite(tile, out Sprite sprite))
                {
                    WarnMissingChiTileSpriteOnce(tile);
                    continue;
                }

                if (!optionView.TryAddTile(chiTileViewPrefab, sprite))
                {
                    WarnMissingOnce(
                        ref warnedInvalidChiTileViewPrefab,
                        "Chi tile view prefab could not display its Sprite.");
                }
            }
        }

        private UnityAction CreateMeldCallAction(
            MeldCallKind kind,
            int optionId)
        {
            if (hasReactionRequest &&
                TryMapReactionAnswerKind(kind, out ReactionWindowSeatAnswerKind answerKind))
            {
                long requestId = reactionRequestId;
                int windowId = reactionWindowId;
                return () => inputController.RequestReactionResponse(
                    requestId,
                    windowId,
                    answerKind,
                    answerKind == ReactionWindowSeatAnswerKind.Chi
                        ? optionId
                        : (int?)null);
            }

            return () => inputController.RequestMeldCall(kind, optionId);
        }

        private void ClearDynamicMeldButtons()
        {
            for (int i = dynamicMeldButtons.Count - 1; i >= 0; i--)
            {
                Button button = dynamicMeldButtons[i];
                if (button == null)
                    continue;

                button.onClick = new Button.ButtonClickedEvent();
                button.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(button.gameObject);
                else
                    DestroyImmediate(button.gameObject);
            }

            dynamicMeldButtons.Clear();
        }

        private void ClearDynamicChiOptions()
        {
            for (int i = dynamicChiOptionViews.Count - 1; i >= 0; i--)
                DestroyChiOptionView(dynamicChiOptionViews[i]);

            dynamicChiOptionViews.Clear();
            if (chiDecisionRoot != null)
                chiDecisionRoot.SetActive(false);
        }

        private static void DestroyChiOptionView(MahjongChiOptionView optionView)
        {
            if (optionView == null)
                return;

            optionView.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(optionView.gameObject);
            else
                DestroyImmediate(optionView.gameObject);
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

        private void ClearSelfKanDecisionResponseBinding()
        {
            if (inputController != null)
                inputController.ClearSelfKanDecisionResponseBinding();
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
        }

        private void ClearReactionRequest()
        {
            hasReactionRequest = false;
            reactionRequestId = 0;
            reactionWindowId = 0;
        }

        private static bool TryMapReactionAnswerKind(
            MeldCallKind meldCallKind,
            out ReactionWindowSeatAnswerKind answerKind)
        {
            switch (meldCallKind)
            {
                case MeldCallKind.Pon:
                    answerKind = ReactionWindowSeatAnswerKind.Pon;
                    return true;
                case MeldCallKind.Chi:
                    answerKind = ReactionWindowSeatAnswerKind.Chi;
                    return true;
                case MeldCallKind.Kan:
                    answerKind = ReactionWindowSeatAnswerKind.Daiminkan;
                    return true;
                default:
                    answerKind = default;
                    return false;
            }
        }

        private static bool IsSameOrChild(Transform candidate, Transform parent)
        {
            return candidate != null &&
                   parent != null &&
                   (candidate == parent || candidate.IsChildOf(parent));
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongPonDecisionController)}: {message}", this);
        }

        private void WarnMissingChiTileSpriteOnce(Tile tile)
        {
            int typeIndex = tile.TypeIndex;
            if (!warnedMissingChiTileSpriteTypeIndexes.Add(typeIndex))
                return;

            Debug.LogWarning(
                $"{nameof(MahjongPonDecisionController)}: " +
                $"Chi tile sprite is not registered for {tile} (TypeIndex={typeIndex}).",
                this);
        }
    }
}
