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
        [Tooltip("プロトタイプ状態を初期化するButtonです。")]
        [SerializeField] private Button retryButton;
        [Header("Win Decision")]
        [SerializeField] private Button winButton;
        [SerializeField] private Button declineWinButton;
        [Tooltip("指定牌ツモの対象を入力するTMP_InputFieldです。1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C を受け付けます。")]
        [SerializeField] private TMP_InputField targetTileInput;

        private bool isSubscribed;
        private bool warnedMissingTargetInput;
        private bool warnedMissingDrawButton;
        private bool warnedMissingSkillButton;
        private bool warnedMissingRetryButton;
        private bool warnedMissingWinButton;
        private bool warnedMissingDeclineWinButton;

        public event Action DrawRequested;
        public event Action<string> ForceDrawSkillRequested;
        public event Action RetryRequested;
        public event Action WinRequested;
        public event Action DeclineWinRequested;

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
            CacheReferences();
            RegisterButtonListeners();
        }

        private void OnDisable()
        {
            UnregisterButtonListeners();
        }

        private void CacheReferences()
        {
            if (drawButton == null)
                drawButton = FindButtonByName("DrawButton");

            if (forceDrawSkillButton == null)
                forceDrawSkillButton = FindButtonByName("ForceDrawSkillButton");

            if (retryButton == null)
                retryButton = FindButtonByName("RetryButton");

            if (targetTileInput == null)
                targetTileInput = FindComponentByName<TMP_InputField>("TargetTileInput");
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

            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);

            if (winButton != null)
                winButton.onClick.RemoveListener(HandleWinClicked);

            if (declineWinButton != null)
                declineWinButton.onClick.RemoveListener(HandleDeclineWinClicked);

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

        private Button FindButtonByName(string objectName)
        {
            return FindComponentByName<Button>(objectName);
        }

        private T FindComponentByName<T>(string objectName) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null && component.gameObject.name == objectName)
                    return component;
            }

            return null;
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
