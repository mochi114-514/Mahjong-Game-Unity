using System.Collections.Generic;
using System.Text;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong UI Display Controller")]
    public sealed class MahjongUiDisplayController : MonoBehaviour
    {
        [Header("Status Text")]
        [Tooltip("現在のSeat表示用TMP Textです。")]
        [SerializeField] private TMP_Text currentSeatText;
        [Tooltip("現在のターン番号表示用TMP Textです。")]
        [SerializeField] private TMP_Text turnIndexText;
        [Tooltip("山の残り枚数表示用TMP Textです。")]
        [SerializeField] private TMP_Text wallCountText;
        [Tooltip("現在のActiveSkillEffect表示用TMP Textです。")]
        [SerializeField] private TMP_Text activeSkillText;
        [Tooltip("捨て牌一覧表示用TMP Textです。")]
        [SerializeField] private TMP_Text discardText;

        private bool warnedMissingStatusText;
        private bool warnedMissingDiscardText;

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
            if (state == null)
                return;

            CacheReferences();
            WarnMissingStaticReferences();

            SetText(currentSeatText, $"Seat: {state.CurrentSeat}");
            SetText(turnIndexText, $"Turn: {state.TurnIndex}");
            SetText(wallCountText, $"Wall: {state.Wall.Count}");
            SetText(activeSkillText, BuildActiveSkillText(state));
            RefreshDiscards(state.Discards);
        }

        private void CacheReferences()
        {
            if (currentSeatText == null)
                currentSeatText = FindTextByName("CurrentSeatText");

            if (turnIndexText == null)
                turnIndexText = FindTextByName("TurnIndexText");

            if (wallCountText == null)
                wallCountText = FindTextByName("WallCountText");

            if (activeSkillText == null)
                activeSkillText = FindTextByName("ActiveSkillText");

            if (discardText == null)
                discardText = FindTextByName("Discard Text");
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

        private TMP_Text FindTextByName(string objectName)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.gameObject.name == objectName)
                    return text;
            }

            return null;
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
            Debug.LogWarning($"{nameof(MahjongUiDisplayController)}: {message}", this);
        }
    }
}
