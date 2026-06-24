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
        [Tooltip("Displays the seat whose turn is active.")]
        [SerializeField] private TMP_Text currentTurnText;
        [Tooltip("Displays the current turn index.")]
        [SerializeField] private TMP_Text turnIndexText;
        [Tooltip("Displays the remaining wall tile count.")]
        [SerializeField] private TMP_Text wallCountText;
        [Tooltip("Displays active skill effects.")]
        [SerializeField] private TMP_Text activeSkillText;

        private bool warnedMissingStatusText;

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

            SetText(currentTurnText, $"CurrentTurn: {state.CurrentTurn}");
            SetText(turnIndexText, $"Turn: {state.TurnIndex}");
            SetText(wallCountText, $"Wall: {state.Wall.Count}");
            SetText(activeSkillText, BuildActiveSkillText(state));
        }

        private void CacheReferences()
        {
            if (currentTurnText == null)
                currentTurnText = FindTextByName("CurrentTurnText");

            if (turnIndexText == null)
                turnIndexText = FindTextByName("TurnIndexText");

            if (wallCountText == null)
                wallCountText = FindTextByName("WallCountText");

            if (activeSkillText == null)
                activeSkillText = FindTextByName("ActiveSkillText");
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
            if (currentTurnText == null || turnIndexText == null || wallCountText == null || activeSkillText == null)
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
