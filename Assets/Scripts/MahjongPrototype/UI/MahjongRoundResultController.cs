using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Round Result Controller")]
    public sealed class MahjongRoundResultController : MonoBehaviour
    {
        private enum ResultPresentationTier
        {
            Normal,
            ManganOrAbove,
            Yakuman
        }

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

        [Header("Input")]
        [SerializeField] private Button confirmButton;

        [Header("Yaku List")]
        [SerializeField] private Transform yakuListRoot;
        [SerializeField] private MahjongRoundResultYakuRowController yakuRowPrefab;

        [Header("Yaku Reveal")]
        [SerializeField, Min(0f)] private float yakuRevealDuration = 0.18f;

        [Header("Win Type Seal Reveal")]
        [SerializeField, Min(0f)] private float winTypeSealRevealDuration = 0.16f;

        [Header("Total Reveal")]
        [SerializeField, Min(0f)] private float totalRevealDelaySeconds = 0.16f;
        [SerializeField, Min(0f)] private float totalRevealDuration = 0.16f;

        [Header("Result Tier Presentation")]
        [SerializeField] private Color manganOrAboveTotalTextColor =
            new Color(1f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color yakumanTotalTextColor =
            new Color(1f, 0.94f, 0.72f, 1f);
        [SerializeField, Min(1f)] private float manganOrAboveTotalScalePeak = 1.1f;
        [SerializeField, Min(1f)] private float yakumanTotalScalePeak = 1.2f;
        [SerializeField, Min(0f)] private float resultTierEmphasisDuration = 0.18f;

        private readonly List<MahjongRoundResultYakuRowController> generatedYakuRows =
            new List<MahjongRoundResultYakuRowController>();

        private bool warnedMissingRoundResultRoot;
        private bool warnedMissingWinDetailsRoot;
        private bool warnedMissingSourceSeatRoot;
        private bool warnedMissingTexts;
        private bool warnedMissingYakuListRoot;
        private bool warnedMissingYakuRowPrefab;
        private Coroutine resultRevealRoutine;
        private RoundResult displayedResult;
        private bool shouldRevealWinTypeSeal;
        private ResultPresentationTier currentResultPresentationTier;
        private Color totalTextColor;
        private Color totalTextPresentationColor;
        private Vector3 totalTextScale;
        private bool totalPresentationCached;
        private Transform winTypePresentationRoot;
        private Vector3 winTypePresentationScale;
        private bool winTypePresentationCached;

        public void SetResult(RoundResult result)
        {
            if (result == null)
            {
                Clear();
                return;
            }

            if (ReferenceEquals(displayedResult, result))
                return;

            StopResultReveal();
            displayedResult = result;
            ClearGeneratedYakuRows();
            SetActiveOrWarn(
                roundResultRoot,
                true,
                ref warnedMissingRoundResultRoot,
                "RoundResultRoot is not assigned.");
            SetText(roundText, FormatWindProgress(result.WindProgress));
            SetText(confirmButtonLabel, result.IsFinalRound ? "ゲーム終了" : "次局へ進む");

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
            StopResultReveal();
            displayedResult = null;
            shouldRevealWinTypeSeal = false;
            ResetTotalPresentation();
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
            SetTotalRevealVisible(true);
            SetConfirmInteractable(true);
        }

        private void OnDisable()
        {
            StopResultReveal();
            displayedResult = null;
            shouldRevealWinTypeSeal = false;
        }

        private void SetWinResult(RoundResult result)
        {
            shouldRevealWinTypeSeal = result.WinType == WinType.Tsumo || result.WinType == WinType.Ron;
            SetResultPresentationTier(GetResultPresentationTier(result));
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
            BeginWinResultReveal();
        }

        private void SetExhaustiveDrawResult()
        {
            shouldRevealWinTypeSeal = false;
            ResetTotalPresentation();
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
            ResetWinTypePresentation();
            SetTotalRevealVisible(true);
            SetConfirmInteractable(true);
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

        private void BeginWinResultReveal()
        {
            if (!Application.isPlaying)
            {
                ResetWinTypePresentation();
                SetGeneratedRowsRevealVisible(true);
                SetTotalRevealVisible(true);
                SetConfirmInteractable(true);
                return;
            }

            SetGeneratedRowsRevealVisible(false);
            SetTotalRevealVisible(false);
            SetWinTypePresentationScale(1.3f);
            SetConfirmInteractable(false);
            resultRevealRoutine = StartCoroutine(RevealWinResult());
        }

        private IEnumerator RevealWinResult()
        {
            yield return RevealWinTypeSeal();

            for (int i = 0; i < generatedYakuRows.Count; i++)
            {
                MahjongRoundResultYakuRowController row = generatedYakuRows[i];
                if (row != null)
                    yield return row.PlayReveal(yakuRevealDuration);
            }

            yield return WaitForUnscaledSeconds(totalRevealDelaySeconds);
            yield return RevealTotal();
            yield return RevealResultTierEmphasis();
            resultRevealRoutine = null;
            SetConfirmInteractable(true);
        }

        private IEnumerator RevealWinTypeSeal()
        {
            if (!shouldRevealWinTypeSeal ||
                GetWinTypePresentationRoot() == null ||
                winTypeSealRevealDuration <= 0f)
            {
                ResetWinTypePresentation();
                yield break;
            }

            const float pressedScale = 0.92f;
            const float pressedProgress = 0.55f;
            for (float elapsed = 0f;
                elapsed < winTypeSealRevealDuration;
                elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / winTypeSealRevealDuration);
                float multiplier;
                if (progress < pressedProgress)
                {
                    float press = EaseOutCubic(progress / pressedProgress);
                    multiplier = Mathf.Lerp(1.3f, pressedScale, press);
                }
                else
                {
                    float settle = Mathf.SmoothStep(
                        0f,
                        1f,
                        (progress - pressedProgress) / (1f - pressedProgress));
                    multiplier = Mathf.Lerp(pressedScale, 1f, settle);
                }

                SetWinTypePresentationScale(multiplier);
                yield return null;
            }

            ResetWinTypePresentation();
        }

        private static IEnumerator WaitForUnscaledSeconds(float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                yield return null;
        }

        private IEnumerator RevealTotal()
        {
            CacheTotalPresentation();
            if (totalText == null || totalRevealDuration <= 0f)
            {
                SetTotalRevealVisible(true);
                yield break;
            }

            for (float elapsed = 0f; elapsed < totalRevealDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / totalRevealDuration);
                float eased = EaseOutCubic(t);
                SetTotalRevealAlpha(eased);
                totalText.transform.localScale = Vector3.Lerp(totalTextScale * 1.12f, totalTextScale, eased);
                yield return null;
            }

            SetTotalRevealVisible(true);
        }

        private IEnumerator RevealResultTierEmphasis()
        {
            if (currentResultPresentationTier == ResultPresentationTier.Normal ||
                totalText == null ||
                resultTierEmphasisDuration <= 0f)
            {
                SetTotalRevealVisible(true);
                yield break;
            }

            float peak = GetResultTierScalePeak(currentResultPresentationTier);
            for (float elapsed = 0f;
                elapsed < resultTierEmphasisDuration;
                elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / resultTierEmphasisDuration);
                totalText.transform.localScale = totalTextScale *
                    GetResultTierScaleMultiplier(currentResultPresentationTier, peak, progress);
                yield return null;
            }

            SetTotalRevealVisible(true);
        }

        private void StopResultReveal()
        {
            if (resultRevealRoutine != null)
            {
                StopCoroutine(resultRevealRoutine);
                resultRevealRoutine = null;
            }

            ResetWinTypePresentation();
            SetGeneratedRowsRevealVisible(true);
            ResetTotalPresentation();
            SetConfirmInteractable(true);
        }

        private void SetGeneratedRowsRevealVisible(bool visible)
        {
            for (int i = 0; i < generatedYakuRows.Count; i++)
            {
                MahjongRoundResultYakuRowController row = generatedYakuRows[i];
                if (row != null)
                    row.SetRevealVisible(visible);
            }
        }

        private void SetTotalRevealVisible(bool visible)
        {
            CacheTotalPresentation();
            SetTotalRevealAlpha(visible ? 1f : 0f);
            if (totalText != null)
                totalText.transform.localScale = totalTextScale;
        }

        private void CacheTotalPresentation()
        {
            if (totalPresentationCached || totalText == null)
                return;

            totalPresentationCached = true;
            totalTextColor = totalText.color;
            totalTextPresentationColor = totalTextColor;
            totalTextScale = totalText.transform.localScale;
        }

        private void SetTotalRevealAlpha(float alpha)
        {
            if (totalText == null)
                return;

            CacheTotalPresentation();
            Color color = totalTextPresentationColor;
            color.a *= alpha;
            totalText.color = color;
        }

        private void SetResultPresentationTier(ResultPresentationTier tier)
        {
            currentResultPresentationTier = tier;
            CacheTotalPresentation();
            totalTextPresentationColor = GetTotalTextColor(tier);
            SetTotalRevealVisible(true);
        }

        private void ResetTotalPresentation()
        {
            SetResultPresentationTier(ResultPresentationTier.Normal);
        }

        private Color GetTotalTextColor(ResultPresentationTier tier)
        {
            switch (tier)
            {
                case ResultPresentationTier.ManganOrAbove:
                    return manganOrAboveTotalTextColor;
                case ResultPresentationTier.Yakuman:
                    return yakumanTotalTextColor;
                default:
                    return totalTextColor;
            }
        }

        private static ResultPresentationTier GetResultPresentationTier(RoundResult result)
        {
            if (result != null && (result.HasYakuman || result.YakumanCount > 0))
                return ResultPresentationTier.Yakuman;

            if (result != null && result.TotalHan >= 5)
                return ResultPresentationTier.ManganOrAbove;

            return ResultPresentationTier.Normal;
        }

        private float GetResultTierScalePeak(ResultPresentationTier tier)
        {
            switch (tier)
            {
                case ResultPresentationTier.ManganOrAbove:
                    return Mathf.Max(1f, manganOrAboveTotalScalePeak);
                case ResultPresentationTier.Yakuman:
                    return Mathf.Max(1f, yakumanTotalScalePeak);
                default:
                    return 1f;
            }
        }

        private static float GetResultTierScaleMultiplier(
            ResultPresentationTier tier,
            float peak,
            float progress)
        {
            if (tier == ResultPresentationTier.ManganOrAbove)
            {
                const float peakProgress = 0.58f;
                if (progress < peakProgress)
                    return Mathf.Lerp(1f, peak, EaseOutCubic(progress / peakProgress));

                float settle = Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - peakProgress) / (1f - peakProgress));
                return Mathf.Lerp(peak, 1f, settle);
            }

            const float yakumanPeakProgress = 0.5f;
            const float yakumanSettleProgress = 0.78f;
            float yakumanSettleScale = Mathf.Lerp(1f, peak, 0.3f);
            if (progress < yakumanPeakProgress)
                return Mathf.Lerp(1f, peak, EaseOutCubic(progress / yakumanPeakProgress));

            if (progress < yakumanSettleProgress)
            {
                float settle = Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - yakumanPeakProgress) /
                    (yakumanSettleProgress - yakumanPeakProgress));
                return Mathf.Lerp(peak, yakumanSettleScale, settle);
            }

            float finish = Mathf.SmoothStep(
                0f,
                1f,
                (progress - yakumanSettleProgress) / (1f - yakumanSettleProgress));
            return Mathf.Lerp(yakumanSettleScale, 1f, finish);
        }

        private Transform GetWinTypePresentationRoot()
        {
            if (winTypePresentationRoot != null)
                return winTypePresentationRoot;

            if (winTypeText == null)
                return null;

            Transform candidate = winTypeText.transform.parent;
            if (candidate == null || candidate.Find("WinTypeSeal") == null)
                return null;

            winTypePresentationRoot = candidate;
            return winTypePresentationRoot;
        }

        private void ResetWinTypePresentation()
        {
            SetWinTypePresentationScale(1f);
        }

        private void SetWinTypePresentationScale(float multiplier)
        {
            Transform presentationRoot = GetWinTypePresentationRoot();
            if (presentationRoot == null)
                return;

            CacheWinTypePresentation(presentationRoot);
            presentationRoot.localScale = winTypePresentationScale * multiplier;
        }

        private void CacheWinTypePresentation(Transform presentationRoot)
        {
            if (winTypePresentationCached)
                return;

            winTypePresentationCached = true;
            winTypePresentationScale = presentationRoot.localScale;
        }

        private static float EaseOutCubic(float progress)
        {
            float inverse = 1f - Mathf.Clamp01(progress);
            return 1f - inverse * inverse * inverse;
        }

        private void SetConfirmInteractable(bool interactable)
        {
            if (confirmButton == null)
                confirmButton = FindConfirmButton();

            if (confirmButton != null)
                confirmButton.interactable = interactable;
        }

        private Button FindConfirmButton()
        {
            Transform root = roundResultRoot != null ? roundResultRoot.transform : transform;
            return root.GetComponentInChildren<Button>(true);
        }

        private void ClearGeneratedYakuRows()
        {
            for (int i = generatedYakuRows.Count - 1; i >= 0; i--)
            {
                MahjongRoundResultYakuRowController row = generatedYakuRows[i];
                if (row == null)
                    continue;

                row.gameObject.SetActive(false);
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
            return $"{FormatRoundWind(progress.RoundWind)}{FormatHandNumber(progress.HandNumber)}局";
        }

        private static string FormatHandNumber(int handNumber)
        {
            switch (handNumber)
            {
                case 1:
                    return "一";
                case 2:
                    return "二";
                case 3:
                    return "三";
                case 4:
                    return "四";
                default:
                    return handNumber.ToString();
            }
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
