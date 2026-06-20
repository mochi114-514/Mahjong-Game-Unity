using System.Collections.Generic;
using System.Text;
using MahjongPrototype;
using MahjongPrototype.Domain;
using MahjongPrototype.Logging;
using MahjongPrototype.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("手牌ボタンを生成する親RectTransformです。Canvas/HandArea を割り当てます。")]
        [SerializeField] private RectTransform handContainer;
        [Tooltip("手牌1枚分のTileButtonViewテンプレートまたはPrefabです。")]
        [SerializeField] private TileButtonView tileButtonPrefab;
        [Tooltip("捨て牌一覧表示用TMP Textです。")]
        [SerializeField] private TMP_Text discardText;

        [Header("Controls")]
        [Tooltip("現在のSeatでツモを要求するButtonです。")]
        [SerializeField] private Button drawButton;
        [Tooltip("TargetTileInputの牌を次ツモで狙う必殺技Buttonです。")]
        [SerializeField] private Button forceDrawSkillButton;
        [Tooltip("プロトタイプ状態を初期化するButtonです。")]
        [SerializeField] private Button retryButton;
        [Tooltip("指定牌ツモの対象を入力するTMP_InputFieldです。1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C を受け付けます。")]
        [SerializeField] private TMP_InputField targetTileInput;

        [Header("Log Preview")]
        [Tooltip("画面表示用の短い人間向けログを表示するTMP Textです。JSONL全文はファイルにのみ保存します。")]
        [SerializeField] private TMP_Text recentLogText;
        [Tooltip("画面に表示する最新ログ行数です。")]
        [SerializeField, Min(1)] private int maxVisibleLogLines = 5;

        private readonly List<TileButtonView> activeTileButtons = new List<TileButtonView>();
        private bool warnedMissingFlow;
        private bool warnedMissingHandContainer;
        private bool warnedMissingTileButtonPrefab;
        private bool warnedMissingTargetInput;
        private bool warnedMissingDrawButton;
        private bool warnedMissingSkillButton;
        private bool warnedMissingRetryButton;
        private bool warnedMissingStatusText;
        private bool warnedMissingDiscardText;
        private bool warnedMissingRecentLogText;

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
            RegisterButtonListeners();
            SubscribeNotifications();
            DevLog.DisplayLineWritten += HandleLogLineWritten;
        }

        private void Start()
        {
            CacheReferences();
            WarnMissingStaticReferences();
            RefreshFromFlow();
            RefreshRecentLogText();
        }

        private void OnDisable()
        {
            UnregisterButtonListeners();
            UnsubscribeNotifications();
            DevLog.DisplayLineWritten -= HandleLogLineWritten;
        }

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
                return;

            SetText(currentSeatText, $"Seat: {state.CurrentSeat}");
            SetText(turnIndexText, $"Turn: {state.TurnIndex}");
            SetText(wallCountText, $"Wall: {state.Wall.Count}");
            SetText(activeSkillText, BuildActiveSkillText(state));

            RebuildHand(state.GetPlayerSeat(state.CurrentSeat).Hand.GetTiles());
            RefreshDiscards(state.Discards);
            RefreshRecentLogText();
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
        }

        private void RegisterButtonListeners()
        {
            if (drawButton != null)
            {
                drawButton.onClick.AddListener(HandleDrawClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingDrawButton, "DrawButton is not assigned.");
            }

            if (forceDrawSkillButton != null)
            {
                forceDrawSkillButton.onClick.AddListener(HandleForceDrawSkillClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingSkillButton, "ForceDrawSkillButton is not assigned.");
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetryClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingRetryButton, "RetryButton is not assigned.");
            }
        }

        private void UnregisterButtonListeners()
        {
            if (drawButton != null)
                drawButton.onClick.RemoveListener(HandleDrawClicked);

            if (forceDrawSkillButton != null)
                forceDrawSkillButton.onClick.RemoveListener(HandleForceDrawSkillClicked);

            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);
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

        private void HandleDrawClicked()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot draw because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDraw();
        }

        private void HandleForceDrawSkillClicked()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot activate skill because MahjongGameFlow is not assigned.");
                return;
            }

            if (targetTileInput == null)
            {
                WarnMissingOnce(ref warnedMissingTargetInput, "TargetTileInput is not assigned.");
                return;
            }

            gameFlow.RequestForceDrawSkill(targetTileInput.text);
        }

        private void HandleRetryClicked()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot retry because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RetryPrototype();
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

        private void RebuildHand(IReadOnlyList<Tile> handTiles)
        {
            ClearHandButtons();

            if (handContainer == null)
            {
                WarnMissingOnce(ref warnedMissingHandContainer, "Hand container is not assigned.");
                return;
            }

            if (tileButtonPrefab == null)
            {
                WarnMissingOnce(ref warnedMissingTileButtonPrefab, "TileButtonView prefab is not assigned.");
                return;
            }

            for (int i = 0; i < handTiles.Count; i++)
            {
                TileButtonView view = Instantiate(tileButtonPrefab, handContainer);
                view.Initialize(i, handTiles[i], HandleTileClicked);
                activeTileButtons.Add(view);
            }
        }

        private void ClearHandButtons()
        {
            for (int i = 0; i < activeTileButtons.Count; i++)
            {
                TileButtonView view = activeTileButtons[i];
                if (view != null)
                    Destroy(view.gameObject);
            }

            activeTileButtons.Clear();
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

        private void RefreshRecentLogText()
        {
            if (recentLogText == null)
            {
                WarnMissingOnce(ref warnedMissingRecentLogText, "RecentLogText is not assigned.");
                return;
            }

            IReadOnlyList<string> lines = DevLog.RecentDisplayLines;
            int start = Mathf.Max(0, lines.Count - maxVisibleLogLines);
            StringBuilder builder = new StringBuilder();
            for (int i = start; i < lines.Count; i++)
            {
                builder.AppendLine(lines[i]);
            }

            recentLogText.text = builder.ToString();
        }

        private void HandleLogLineWritten(string line)
        {
            RefreshRecentLogText();
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
