using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong UI Input Controller")]
    public sealed class MahjongUiInputController : MonoBehaviour
    {
        [Header("Controls")]
        [Tooltip("現在のSeatでツモを要求するButtonです。")]
        [SerializeField] private Button drawButton;
        [Tooltip("TargetTileInputの牌を次ツモで狙う必殺技Buttonです。")]
        [SerializeField] private Button forceDrawSkillButton;
        [Tooltip("Enables automatic TypeIndex sorting when the hand changes.")]
        [SerializeField] private Toggle autoSortToggle;
        [Tooltip("プロトタイプ状態を初期化するButtonです。")]
        [SerializeField] private Button retryButton;
        [Header("Win Decision")]
        [SerializeField] private Button winButton;
        [SerializeField] private Button declineWinButton;
        [Header("Pon Decision")]
        [SerializeField] private Button ponButton;
        [SerializeField] private Button declinePonButton;
        [Header("Reach Decision")]
        [SerializeField] private Button reachButton;
        [SerializeField] private Button declineReachButton;
        [SerializeField] private Button cancelReachButton;
        [Header("Round Result")]
        [SerializeField] private Button roundResultConfirmButton;
        [Tooltip("指定牌ツモの対象を入力するTMP_InputFieldです。1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C を受け付けます。")]
        [SerializeField] private TMP_InputField targetTileInput;

        private bool isSubscribed;
        private bool warnedMissingTargetInput;
        private bool warnedMissingDrawButton;
        private bool warnedMissingSkillButton;
        private bool warnedMissingAutoSortToggle;
        private bool warnedMissingRetryButton;
        private bool warnedMissingWinButton;
        private bool warnedMissingDeclineWinButton;
        private bool warnedMissingPonButton;
        private bool warnedMissingDeclinePonButton;
        private bool warnedMissingReachButton;
        private bool warnedMissingDeclineReachButton;
        private bool warnedMissingCancelReachButton;
        private bool warnedMissingRoundResultConfirmButton;

        public event Action DrawRequested;
        public event Action<string> ForceDrawSkillRequested;
        public event Action<bool> AutoSortChanged;
        public event Action RetryRequested;
        public event Action WinRequested;
        public event Action DeclineWinRequested;
        public event Action PonRequested;
        public event Action DeclinePonRequested;
        public event Action ReachRequested;
        public event Action DeclineReachRequested;
        public event Action CancelReachRequested;
        public event Action RoundResultConfirmRequested;

        private void OnEnable()
        {
            RegisterButtonListeners();
        }

        private void OnDisable()
        {
            UnregisterButtonListeners();
        }

        private void RegisterButtonListeners()
        {
            if (isSubscribed)
                return;

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

            if (autoSortToggle != null)
            {
                autoSortToggle.onValueChanged.AddListener(HandleAutoSortChanged);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingAutoSortToggle, "AutoSortToggle is not assigned.");
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetryClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingRetryButton, "RetryButton is not assigned.");
            }

            if (winButton != null)
            {
                winButton.onClick.AddListener(HandleWinClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingWinButton, "WinButton is not assigned.");
            }

            if (declineWinButton != null)
            {
                declineWinButton.onClick.AddListener(HandleDeclineWinClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingDeclineWinButton, "DeclineWinButton is not assigned.");
            }

            if (ponButton != null)
            {
                ponButton.onClick.AddListener(HandlePonClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingPonButton, "PonButton is not assigned.");
            }

            if (declinePonButton != null)
            {
                declinePonButton.onClick.AddListener(HandleDeclinePonClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingDeclinePonButton, "DeclinePonButton is not assigned.");
            }

            if (reachButton != null)
            {
                reachButton.onClick.AddListener(HandleReachClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingReachButton, "ReachButton is not assigned.");
            }

            if (declineReachButton != null)
            {
                declineReachButton.onClick.AddListener(HandleDeclineReachClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingDeclineReachButton, "DeclineReachButton is not assigned.");
            }

            if (cancelReachButton != null)
            {
                cancelReachButton.onClick.AddListener(HandleCancelReachClicked);
            }
            else
            {
                WarnMissingOnce(ref warnedMissingCancelReachButton, "CancelReachButton is not assigned.");
            }

            if (roundResultConfirmButton != null)
            {
                roundResultConfirmButton.onClick.AddListener(HandleRoundResultConfirmClicked);
            }
            else
            {
                WarnMissingOnce(
                    ref warnedMissingRoundResultConfirmButton,
                    "RoundResultConfirmButton is not assigned.");
            }

            isSubscribed = true;
        }

        private void UnregisterButtonListeners()
        {
            if (!isSubscribed)
                return;

            if (drawButton != null)
                drawButton.onClick.RemoveListener(HandleDrawClicked);

            if (forceDrawSkillButton != null)
                forceDrawSkillButton.onClick.RemoveListener(HandleForceDrawSkillClicked);

            if (autoSortToggle != null)
                autoSortToggle.onValueChanged.RemoveListener(HandleAutoSortChanged);

            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);

            if (winButton != null)
                winButton.onClick.RemoveListener(HandleWinClicked);

            if (declineWinButton != null)
                declineWinButton.onClick.RemoveListener(HandleDeclineWinClicked);

            if (ponButton != null)
                ponButton.onClick.RemoveListener(HandlePonClicked);

            if (declinePonButton != null)
                declinePonButton.onClick.RemoveListener(HandleDeclinePonClicked);

            if (reachButton != null)
                reachButton.onClick.RemoveListener(HandleReachClicked);

            if (declineReachButton != null)
                declineReachButton.onClick.RemoveListener(HandleDeclineReachClicked);

            if (cancelReachButton != null)
                cancelReachButton.onClick.RemoveListener(HandleCancelReachClicked);

            if (roundResultConfirmButton != null)
                roundResultConfirmButton.onClick.RemoveListener(HandleRoundResultConfirmClicked);

            isSubscribed = false;
        }

        private void HandleDrawClicked()
        {
            DrawRequested?.Invoke();
        }

        private void HandleForceDrawSkillClicked()
        {
            if (targetTileInput == null)
            {
                WarnMissingOnce(ref warnedMissingTargetInput, "TargetTileInput is not assigned.");
                return;
            }

            ForceDrawSkillRequested?.Invoke(targetTileInput.text);
        }

        private void HandleAutoSortChanged(bool enabled)
        {
            AutoSortChanged?.Invoke(enabled);
        }

        private void HandleRetryClicked()
        {
            RetryRequested?.Invoke();
        }

        private void HandleWinClicked()
        {
            WinRequested?.Invoke();
        }

        private void HandleDeclineWinClicked()
        {
            DeclineWinRequested?.Invoke();
        }

        private void HandlePonClicked()
        {
            PonRequested?.Invoke();
        }

        private void HandleDeclinePonClicked()
        {
            DeclinePonRequested?.Invoke();
        }

        private void HandleReachClicked()
        {
            ReachRequested?.Invoke();
        }

        private void HandleDeclineReachClicked()
        {
            DeclineReachRequested?.Invoke();
        }

        private void HandleCancelReachClicked()
        {
            CancelReachRequested?.Invoke();
        }

        private void HandleRoundResultConfirmClicked()
        {
            RoundResultConfirmRequested?.Invoke();
        }

        public void SetAutoSortWithoutNotify(bool enabled)
        {
            if (autoSortToggle == null)
            {
                WarnMissingOnce(ref warnedMissingAutoSortToggle, "AutoSortToggle is not assigned.");
                return;
            }

            autoSortToggle.SetIsOnWithoutNotify(enabled);
        }

        public void SetGameplayInputInteractable(bool interactable)
        {
            if (drawButton != null)
                drawButton.interactable = interactable;

            if (forceDrawSkillButton != null)
                forceDrawSkillButton.interactable = interactable;

            if (targetTileInput != null)
                targetTileInput.interactable = interactable;
        }

        public void SetAutoSortInteractable(bool interactable)
        {
            if (autoSortToggle != null)
                autoSortToggle.interactable = interactable;
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongUiInputController)}: {message}", this);
        }
    }
}
