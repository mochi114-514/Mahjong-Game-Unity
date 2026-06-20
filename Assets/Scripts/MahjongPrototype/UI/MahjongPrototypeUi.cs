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
        [SerializeField] private MahjongGameFlow gameFlow;
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Status Text")]
        [SerializeField] private TMP_Text currentSeatText;
        [SerializeField] private TMP_Text turnIndexText;
        [SerializeField] private TMP_Text wallCountText;
        [SerializeField] private TMP_Text activeSkillText;

        [Header("Tile Areas")]
        [SerializeField] private Transform handContainer;
        [SerializeField] private TileButtonView tileButtonPrefab;
        [SerializeField] private TMP_Text discardText;

        [Header("Controls")]
        [SerializeField] private Button drawButton;
        [SerializeField] private Button forceDrawSkillButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private TMP_InputField targetTileInput;

        [Header("Log Preview")]
        [SerializeField] private TMP_Text recentLogText;
        [SerializeField, Min(1)] private int maxVisibleLogLines = 8;

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
            DevLog.LineWritten += HandleLogLineWritten;
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
            DevLog.LineWritten -= HandleLogLineWritten;
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

            IReadOnlyList<string> lines = DevLog.RecentLines;
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
