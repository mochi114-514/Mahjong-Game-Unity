using System.Collections.Generic;
using System.Text;
using MahjongPrototype.Logging;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Log Preview Controller")]
    public sealed class MahjongLogPreviewController : MonoBehaviour
    {
        [Header("Log Preview")]
        [Tooltip("画面表示用の短い人間向けログを表示するTMP Textです。JSONL全文はファイルにのみ保存します。")]
        [SerializeField] private TMP_Text recentLogText;
        [Tooltip("画面に表示する最新ログ行数です。")]
        [SerializeField, Min(1)] private int maxVisibleLogLines = 5;

        private bool isSubscribed;
        private bool warnedMissingRecentLogText;

        private void OnEnable()
        {
            SubscribeLogEvents();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeLogEvents();
        }

        public void Configure(TMP_Text logText, int visibleLineCount)
        {
            recentLogText = logText;
            maxVisibleLogLines = Mathf.Max(1, visibleLineCount);
            Refresh();
        }

        public void ConfigureMissingReferences(TMP_Text fallbackLogText, int fallbackVisibleLineCount)
        {
            if (recentLogText != null)
                return;

            recentLogText = fallbackLogText;
            maxVisibleLogLines = Mathf.Max(1, fallbackVisibleLineCount);
            Refresh();
        }

        public void Refresh()
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

        private void SubscribeLogEvents()
        {
            if (isSubscribed)
                return;

            DevLog.DisplayLineWritten += HandleLogLineWritten;
            isSubscribed = true;
        }

        private void UnsubscribeLogEvents()
        {
            if (!isSubscribed)
                return;

            DevLog.DisplayLineWritten -= HandleLogLineWritten;
            isSubscribed = false;
        }

        private void HandleLogLineWritten(string line)
        {
            Refresh();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongLogPreviewController)}: {message}", this);
        }
    }
}
