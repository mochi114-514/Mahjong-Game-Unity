using System.Collections.Generic;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Round Result Controller")]
    public sealed class MahjongRoundResultController : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private GameObject roundResultRoot;
        [SerializeField] private GameObject winDetailsRoot;
        [SerializeField] private GameObject sourceSeatRoot;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private TMP_Text winTypeText;
        [SerializeField] private TMP_Text sourceSeatText;
        [SerializeField] private TMP_Text winningTileText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text confirmButtonLabel;

        [Header("Yaku List")]
        [SerializeField] private Transform yakuListRoot;
        [SerializeField] private MahjongRoundResultYakuRowController yakuRowPrefab;

        private readonly List<MahjongRoundResultYakuRowController> generatedYakuRows =
            new List<MahjongRoundResultYakuRowController>();

        private bool warnedMissingRoundResultRoot;
        private bool warnedMissingWinDetailsRoot;
        private bool warnedMissingSourceSeatRoot;
        private bool warnedMissingTexts;
        private bool warnedMissingYakuListRoot;
        private bool warnedMissingYakuRowPrefab;

        public void SetResult(RoundResult result)
        {
            if (result == null)
            {
                Clear();
                return;
            }

            ClearGeneratedYakuRows();
            SetActiveOrWarn(
                roundResultRoot,
                true,
                ref warnedMissingRoundResultRoot,
                "RoundResultRoot is not assigned.");
            SetText(roundText, FormatWindProgress(result.WindProgress));
            SetText(confirmButtonLabel, result.IsFinalRound ? "ゲーム終了" : "次局へ");

            switch (result.Type)
            {
                case RoundResultType.Win:
                    SetWinResult(result);
                    break;
                case RoundResultType.ExhaustiveDraw:
                    SetExhaustiveDrawResult();
                    break;
                default:
                    SetExhaustiveDrawResult();
                    break;
            }

            WarnMissingTextReferencesOnce();
        }

        public void Clear()
        {
            ClearGeneratedYakuRows();
            SetActive(roundResultRoot, false);
            SetActive(winDetailsRoot, false);
            SetActive(sourceSeatRoot, false);
            SetText(titleText, string.Empty);
            SetText(roundText, string.Empty);
            SetText(winnerText, string.Empty);
            SetText(winTypeText, string.Empty);
            SetText(sourceSeatText, string.Empty);
            SetText(winningTileText, string.Empty);
            SetText(totalText, string.Empty);
            SetText(confirmButtonLabel, string.Empty);
        }

        private void SetWinResult(RoundResult result)
        {
            SetText(titleText, "和了");
            SetActiveOrWarn(
                winDetailsRoot,
                true,
                ref warnedMissingWinDetailsRoot,
                "WinDetailsRoot is not assigned.");
            SetText(winnerText, result.WinnerSeat.HasValue
                ? FormatSeat(result.WinnerSeat.Value)
                : string.Empty);
            SetText(winTypeText, FormatWinType(result.WinType));
            SetText(winningTileText, result.WinningTile.HasValue
                ? result.WinningTile.Value.ToString()
                : string.Empty);

            bool showSourceSeat = result.WinType == WinType.Ron;
            SetActiveOrWarn(
                sourceSeatRoot,
                showSourceSeat,
                ref warnedMissingSourceSeatRoot,
                "SourceSeatRoot is not assigned.");
            SetText(sourceSeatText, showSourceSeat && result.SourceSeat.HasValue
                ? FormatSeat(result.SourceSeat.Value)
                : string.Empty);

            PopulateYakuRows(result.Yakus);
            SetText(totalText, FormatTotal(result));
        }

        private void SetExhaustiveDrawResult()
        {
            SetText(titleText, "流局");
            SetActiveOrWarn(
                winDetailsRoot,
                false,
                ref warnedMissingWinDetailsRoot,
                "WinDetailsRoot is not assigned.");
            SetActive(sourceSeatRoot, false);
            SetText(winnerText, string.Empty);
            SetText(winTypeText, string.Empty);
            SetText(sourceSeatText, string.Empty);
            SetText(winningTileText, string.Empty);
            SetText(totalText, string.Empty);
        }

        private void PopulateYakuRows(IReadOnlyList<EvaluatedYaku> yakus)
        {
            if (yakus == null || yakus.Count == 0)
                return;

            if (yakuListRoot == null)
            {
                WarnMissingOnce(ref warnedMissingYakuListRoot, "YakuListRoot is not assigned.");
                return;
            }

            if (yakuRowPrefab == null)
            {
                WarnMissingOnce(ref warnedMissingYakuRowPrefab, "YakuRowPrefab is not assigned.");
                return;
            }

            for (int i = 0; i < yakus.Count; i++)
            {
                MahjongRoundResultYakuRowController row =
                    Instantiate(yakuRowPrefab, yakuListRoot);
                generatedYakuRows.Add(row);
                row.Bind(yakus[i]);
            }
        }

        private void ClearGeneratedYakuRows()
        {
            for (int i = generatedYakuRows.Count - 1; i >= 0; i--)
            {
                MahjongRoundResultYakuRowController row = generatedYakuRows[i];
                if (row == null)
                    continue;

                DestroyRow(row.gameObject);
            }

            generatedYakuRows.Clear();
        }

        private static void DestroyRow(GameObject rowObject)
        {
            if (rowObject == null)
                return;

            if (Application.isPlaying)
                Destroy(rowObject);
            else
                DestroyImmediate(rowObject);
        }

        private static string FormatTotal(RoundResult result)
        {
            if (result.HasYakuman)
            {
                int yakumanCount = result.YakumanCount;
                if (yakumanCount <= 1)
                    return "役満";

                return $"役満×{yakumanCount}";
            }

            return $"{result.TotalHan}翻";
        }

        private static string FormatWindProgress(WindProgress progress)
        {
            return $"{FormatRoundWind(progress.RoundWind)}{progress.HandNumber}局";
        }

        private static string FormatRoundWind(RoundWind roundWind)
        {
            switch (roundWind)
            {
                case RoundWind.East:
                    return "東";
                case RoundWind.South:
                    return "南";
                default:
                    return string.Empty;
            }
        }

        private static string FormatSeat(SeatId seat)
        {
            switch (seat)
            {
                case SeatId.East:
                    return "東";
                case SeatId.South:
                    return "南";
                case SeatId.West:
                    return "西";
                case SeatId.North:
                    return "北";
                default:
                    return seat.ToString();
            }
        }

        private static string FormatWinType(WinType? winType)
        {
            switch (winType)
            {
                case WinType.Tsumo:
                    return "ツモ";
                case WinType.Ron:
                    return "ロン";
                default:
                    return string.Empty;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private void SetActiveOrWarn(
            GameObject target,
            bool active,
            ref bool warned,
            string warning)
        {
            if (target != null)
            {
                target.SetActive(active);
                return;
            }

            WarnMissingOnce(ref warned, warning);
        }

        private void WarnMissingTextReferencesOnce()
        {
            if (titleText != null &&
                roundText != null &&
                winnerText != null &&
                winTypeText != null &&
                sourceSeatText != null &&
                winningTileText != null &&
                totalText != null &&
                confirmButtonLabel != null)
            {
                return;
            }

            WarnMissingOnce(ref warnedMissingTexts, "One or more round result TMP_Text references are not assigned.");
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongRoundResultController)}: {message}", this);
        }
    }
}
