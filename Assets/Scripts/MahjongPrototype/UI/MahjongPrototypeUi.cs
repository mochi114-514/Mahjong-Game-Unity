using System.Collections.Generic;
using System.Text;
using MahjongPrototype;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using UnityEngine;
using TMPro;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Prototype UI")]
    public sealed class MahjongPrototypeUi : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("ゲーム進行の指示役です。MahjongPrototypeRoot の MahjongGameFlow を割り当てます。")]
        [SerializeField] private MahjongGameFlow gameFlow;
        [Tooltip("決定済みイベントの通知役です。MahjongPrototypeRoot の MahjongEventNotifier を割り当てます。")]
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Status Text")]
        [Tooltip("現在のSeat表示用TMP Textです。")]
        [SerializeField] private TMP_Text currentSeatText;
        [Tooltip("現在のターン番号表示用TMP Textです。")]
        [SerializeField] private TMP_Text turnIndexText;
        [Tooltip("山の残り枚数表示用TMP Textです。")]
        [SerializeField] private TMP_Text wallCountText;
        [Tooltip("現在のActiveSkillEffect表示用TMP Textです。")]
        [SerializeField] private TMP_Text activeSkillText;

        [Header("Tile Areas")]
        [Tooltip("手牌ボタン表示のViewです。未設定ならPlay時に同じGameObjectへ追加します。")]
        [SerializeField] private MahjongHandView handView;
        [Tooltip("手牌ボタンを生成する親RectTransformです。Canvas/HandArea を割り当てます。")]
        [SerializeField] private RectTransform handContainer;
        [Tooltip("手牌1枚分のTileButtonViewテンプレートまたはPrefabです。")]
        [SerializeField] private TileButtonView tileButtonPrefab;
        [Tooltip("捨て牌一覧表示用TMP Textです。")]
        [SerializeField] private TMP_Text discardText;

        [Header("Input")]
        [Tooltip("Draw/SkillDraw/Retry の入力受付Controllerです。")]
        [SerializeField] private MahjongUiInputController inputController;

        [Header("Log Preview")]
        [Tooltip("画面ログ表示のControllerです。RecentLogText と表示行数はこのController側で設定します。")]
        [SerializeField] private MahjongLogPreviewController logPreviewController;

        private bool warnedMissingFlow;
        private bool warnedMissingInputController;
        private bool warnedMissingLogPreviewController;
        private bool warnedMissingStatusText;
        private bool warnedMissingDiscardText;
        private bool isHandViewSubscribed;
        private bool isInputControllerSubscribed;

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
            EnsureHandView();
            SubscribeHandViewEvents();
            EnsureInputController();
            SubscribeInputControllerEvents();
            EnsureLogPreviewController();
            SubscribeNotifications();
        }

        private void Start()
        {
            CacheReferences();
            EnsureHandView();
            SubscribeHandViewEvents();
            EnsureInputController();
            SubscribeInputControllerEvents();
            EnsureLogPreviewController();
            WarnMissingStaticReferences();
            RefreshFromFlow();
            RefreshLogPreview();
        }

        private void OnDisable()
        {
            UnsubscribeHandViewEvents();
            UnsubscribeInputControllerEvents();
            UnsubscribeNotifications();
        }

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
                return;

            SetText(currentSeatText, $"Seat: {state.CurrentSeat}");
            SetText(turnIndexText, $"Turn: {state.TurnIndex}");
            SetText(wallCountText, $"Wall: {state.Wall.Count}");
            SetText(activeSkillText, BuildActiveSkillText(state));

            RefreshHand(state);
            RefreshDiscards(state.Discards);
            RefreshLogPreview();
        }

        public void RefreshFromFlow()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "MahjongGameFlow is not assigned.");
                return;
            }

            Refresh(gameFlow.CurrentState);
        }

        private void CacheReferences()
        {
            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (eventNotifier == null && gameFlow != null)
                eventNotifier = gameFlow.EventNotifier;

            if (handView == null)
                handView = GetComponentInChildren<MahjongHandView>(true);

            if (inputController == null)
                inputController = GetComponentInChildren<MahjongUiInputController>(true);

            if (logPreviewController == null)
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);
        }

        private void SubscribeNotifications()
        {
            if (eventNotifier == null)
                return;

            eventNotifier.AnyEventNotified += RefreshFromFlow;
        }

        private void UnsubscribeNotifications()
        {
            if (eventNotifier == null)
                return;

            eventNotifier.AnyEventNotified -= RefreshFromFlow;
        }

        private void EnsureInputController()
        {
            if (inputController == null)
            {
                inputController = GetComponentInChildren<MahjongUiInputController>(true);
            }

            if (inputController != null)
                return;

            inputController = gameObject.AddComponent<MahjongUiInputController>();
            if (inputController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingInputController,
                    "MahjongUiInputController is not assigned. Add it to the UI GameObject and assign the Draw/Skill/Retry controls.");
            }
        }

        private void SubscribeInputControllerEvents()
        {
            if (inputController == null || isInputControllerSubscribed)
                return;

            inputController.DrawRequested += HandleDrawRequested;
            inputController.ForceDrawSkillRequested += HandleForceDrawSkillRequested;
            inputController.RetryRequested += HandleRetryRequested;
            isInputControllerSubscribed = true;
        }

        private void UnsubscribeInputControllerEvents()
        {
            if (inputController == null || !isInputControllerSubscribed)
                return;

            inputController.DrawRequested -= HandleDrawRequested;
            inputController.ForceDrawSkillRequested -= HandleForceDrawSkillRequested;
            inputController.RetryRequested -= HandleRetryRequested;
            isInputControllerSubscribed = false;
        }

        private void HandleDrawRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot draw because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDraw();
        }

        private void HandleForceDrawSkillRequested(string targetTileText)
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot activate skill because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestForceDrawSkill(targetTileText);
        }

        private void HandleRetryRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot retry because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RetryPrototype();
        }

        private void EnsureHandView()
        {
            if (handView == null)
            {
                handView = GetComponentInChildren<MahjongHandView>(true);
            }

            if (handView == null)
            {
                handView = gameObject.AddComponent<MahjongHandView>();
                handView.Configure(handContainer, tileButtonPrefab);
                return;
            }

            handView.ConfigureMissingReferences(handContainer, tileButtonPrefab);
        }

        private void SubscribeHandViewEvents()
        {
            if (handView == null || isHandViewSubscribed)
                return;

            handView.TileClicked += HandleTileClicked;
            isHandViewSubscribed = true;
        }

        private void UnsubscribeHandViewEvents()
        {
            if (handView == null || !isHandViewSubscribed)
                return;

            handView.TileClicked -= HandleTileClicked;
            isHandViewSubscribed = false;
        }

        private void RefreshHand(MahjongGameState state)
        {
            if (handView == null)
                EnsureHandView();

            if (handView != null)
                handView.Rebuild(state.GetPlayerSeat(state.CurrentSeat).Hand.GetTiles());
        }

        private void HandleTileClicked(int handIndex)
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot discard because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDiscard(handIndex);
        }

        private void RefreshDiscards(IReadOnlyList<DiscardRecord> discards)
        {
            if (discardText == null)
            {
                WarnMissingOnce(ref warnedMissingDiscardText, "DiscardText is not assigned.");
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                builder.Append(record.ActorSeat)
                    .Append(" ")
                    .Append(record.Tile)
                    .Append(" (T")
                    .Append(record.TurnIndex)
                    .Append(')');

                if (i + 1 < discards.Count)
                    builder.AppendLine();
            }

            discardText.text = builder.ToString();
        }

        private void EnsureLogPreviewController()
        {
            if (logPreviewController == null)
            {
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);
            }

            if (logPreviewController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingLogPreviewController,
                "MahjongLogPreviewController is not assigned. Add it to the UI GameObject and assign RecentLogText there.");
        }

        private void RefreshLogPreview()
        {
            if (logPreviewController == null)
                EnsureLogPreviewController();

            if (logPreviewController != null)
                logPreviewController.Refresh();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private void WarnMissingStaticReferences()
        {
            if (currentSeatText == null || turnIndexText == null || wallCountText == null || activeSkillText == null)
                WarnMissingOnce(ref warnedMissingStatusText, "One or more status TMP_Text references are not assigned.");
        }

        private static string BuildActiveSkillText(MahjongGameState state)
        {
            if (state.ActiveSkillEffects.Count <= 0)
                return "Skill: none";

            StringBuilder builder = new StringBuilder("Skill: ");
            for (int i = 0; i < state.ActiveSkillEffects.Count; i++)
            {
                builder.Append(state.ActiveSkillEffects[i].ToLogText());
                if (i + 1 < state.ActiveSkillEffects.Count)
                    builder.Append(", ");
            }

            return builder.ToString();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongPrototypeUi)}: {message}", this);
        }
    }
}
