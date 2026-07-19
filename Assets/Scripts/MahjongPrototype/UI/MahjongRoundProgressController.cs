using System;
using System.Collections;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Round Progress Controller")]
    public sealed class MahjongRoundProgressController : MonoBehaviour
    {
        [Header("Round Progress")]
        [SerializeField] private GameObject roundProgressRoot;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text myWindText;
        [SerializeField] private TMP_Text windText;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float characterRevealDuration = 0.14f;
        [SerializeField, Min(0f)] private float characterInterval = 0.12f;
        [SerializeField, Min(0f)] private float windRevealDelay = 0.18f;
        [SerializeField, Min(0f)] private float finalDisplayDuration = 0.8f;

        private Coroutine playRoutine;
        private WindProgress lastPlayedProgress;
        private WindProgress activeProgress;
        private bool hasPlayedProgress;
        private bool isPresentationActive;
        private bool warnedMissingRoundProgressRoot;
        private bool warnedMissingRoundText;
        private bool warnedMissingMyWindText;
        private bool warnedMissingWindText;
        private bool warnedInactiveController;

        public event Action<WindProgress> PresentationCompleted;

        private void Awake()
        {
            ResolveReferences();
            Clear();
        }

        private void OnDisable()
        {
            bool wasPresentationActive = isPresentationActive;
            WindProgress interruptedProgress = activeProgress;
            Clear();

            // The game flow waits for the presentation callback. Disabling only this
            // component must not leave the current round setup waiting indefinitely.
            if (wasPresentationActive)
                PresentationCompleted?.Invoke(interruptedProgress);
        }

        /// <summary>
        /// Plays the round presentation once for each distinct wind progress.
        /// </summary>
        public bool TryPlay(WindProgress progress, SeatId selfSeat)
        {
            if (!isActiveAndEnabled)
            {
                WarnMissingOnce(
                    ref warnedInactiveController,
                    "Round progress controller is inactive. Skipping the presentation.");
                return false;
            }

            ResolveReferences();
            if (!HasRequiredReferences())
                return false;

            if (hasPlayedProgress && lastPlayedProgress.Equals(progress))
                return false;

            hasPlayedProgress = true;
            lastPlayedProgress = progress;
            StopPlayRoutine();
            activeProgress = progress;
            isPresentationActive = true;

            if (!Application.isPlaying)
            {
                PresentImmediately(progress, selfSeat);
                return true;
            }

            playRoutine = StartCoroutine(PlayRoutine(progress, selfSeat));
            return true;
        }

        public void Clear()
        {
            StopPlayRoutine();
            SetActive(roundText, false);
            SetActive(myWindText, false);
            SetActive(windText, false);
            SetActive(roundProgressRoot, false);
            isPresentationActive = false;
        }

        public void ResetPlaybackHistory()
        {
            Clear();
            hasPlayedProgress = false;
        }

        private IEnumerator PlayRoutine(WindProgress progress, SeatId selfSeat)
        {
            string label = FormatWindProgress(progress);
            roundProgressRoot.SetActive(true);
            SetActive(roundText, true);
            SetActive(myWindText, false);
            SetActive(windText, false);

            for (int index = 1; index <= label.Length; index++)
            {
                roundText.text = label.Substring(0, index);
                yield return RevealText(roundText, characterRevealDuration);

                if (index < label.Length)
                    yield return new WaitForSecondsRealtime(characterInterval);
            }

            yield return new WaitForSecondsRealtime(windRevealDelay);
            windText.text = FormatSeat(selfSeat);
            SetActive(myWindText, true);
            SetActive(windText, true);
            yield return new WaitForSecondsRealtime(finalDisplayDuration);

            CompletePlayback(progress);
        }

        private IEnumerator RevealText(TMP_Text text, float duration)
        {
            if (text == null || duration <= 0f)
                yield break;

            Color color = text.color;
            Vector3 normalScale = text.transform.localScale;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                color.a = t;
                text.color = color;
                text.transform.localScale = Vector3.Lerp(normalScale * 1.12f, normalScale, t);
                yield return null;
            }

            color.a = 1f;
            text.color = color;
            text.transform.localScale = normalScale;
        }

        private void PresentImmediately(WindProgress progress, SeatId selfSeat)
        {
            roundProgressRoot.SetActive(true);
            roundText.text = FormatWindProgress(progress);
            windText.text = FormatSeat(selfSeat);
            SetActive(roundText, true);
            SetActive(myWindText, true);
            SetActive(windText, true);
            CompletePlayback(progress);
        }

        private void CompletePlayback(WindProgress progress)
        {
            SetActive(roundText, false);
            SetActive(myWindText, false);
            SetActive(windText, false);
            SetActive(roundProgressRoot, false);
            playRoutine = null;
            isPresentationActive = false;
            PresentationCompleted?.Invoke(progress);
        }

        private void StopPlayRoutine()
        {
            if (playRoutine == null)
                return;

            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = true;
            if (roundProgressRoot == null)
            {
                WarnMissingOnce(ref warnedMissingRoundProgressRoot, "RoundProgressRoot is not assigned.");
                hasReferences = false;
            }

            if (roundText == null)
            {
                WarnMissingOnce(ref warnedMissingRoundText, "RoundText is not assigned.");
                hasReferences = false;
            }

            if (myWindText == null)
            {
                WarnMissingOnce(ref warnedMissingMyWindText, "MyWindText is not assigned.");
                hasReferences = false;
            }

            if (windText == null)
            {
                WarnMissingOnce(ref warnedMissingWindText, "WindText is not assigned.");
                hasReferences = false;
            }

            return hasReferences;
        }

        private void ResolveReferences()
        {
            if (roundProgressRoot == null)
            {
                Transform[] transforms = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == "Round Progress")
                    {
                        roundProgressRoot = transforms[i].gameObject;
                        break;
                    }
                }
            }

            if (roundProgressRoot == null)
                return;

            TMP_Text[] texts = roundProgressRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (roundText == null && text.name == "Round Text")
                    roundText = text;
                else if (myWindText == null && text.name == "My Wind Text")
                    myWindText = text;
                else if (windText == null && text.name == "Wind Text")
                    windText = text;
            }
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
            return roundWind == RoundWind.South ? "南" : "東";
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

        private static void SetActive(TMP_Text text, bool active)
        {
            if (text != null)
                text.gameObject.SetActive(active);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongRoundProgressController)}: {message}", this);
        }
    }
}
