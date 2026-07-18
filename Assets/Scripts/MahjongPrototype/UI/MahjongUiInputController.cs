using System;
using MahjongPrototype.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        private UnityAction reactionWinAction;
        private UnityAction reactionDeclineWinAction;
        private UnityAction reactionPonAction;
        private UnityAction reactionDeclinePonAction;
        private UnityAction winDecisionAction;
        private UnityAction declineWinDecisionAction;
        private UnityAction reachDecisionAction;
        private UnityAction declineReachDecisionAction;

        public event Action DrawRequested;
        public event Action<string> ForceDrawSkillRequested;
        public event Action<bool> AutoSortChanged;
        public event Action RetryRequested;
        public event Action WinRequested;
        public event Action DeclineWinRequested;
        public event Action PonRequested;
        public event Action DeclinePonRequested;
        public event Action<MeldCallKind, int> MeldCallRequested;
        public event Action DeclineMeldCallsRequested;
        public event Action<long, int, ReactionWindowSeatAnswerKind, int?>
            ReactionResponseRequested;
        public event Action<long, bool> WinDecisionResponseRequested;
        public event Action<long, bool> ReachDecisionResponseRequested;
        public event Action<long, bool, int> SelfKanDecisionResponseRequested;
        public event Action<SelfKanKind, int, int> SelfKanRequested;
        public event Action DeclineSelfKanRequested;
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
            ClearReactionResponseBindings();
            ClearWinDecisionResponseBindings();
            ClearReachDecisionResponseBindings();
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
            if (reactionWinAction != null || winDecisionAction != null)
                return;

            WinRequested?.Invoke();
        }

        private void HandleDeclineWinClicked()
        {
            if (reactionDeclineWinAction != null || declineWinDecisionAction != null)
                return;

            DeclineWinRequested?.Invoke();
        }

        private void HandlePonClicked()
        {
            if (reactionPonAction != null)
                return;

            PonRequested?.Invoke();
            RequestMeldCall(MeldCallKind.Pon, 0);
        }

        private void HandleDeclinePonClicked()
        {
            if (reactionDeclinePonAction != null)
                return;

            DeclinePonRequested?.Invoke();
            RequestDeclineMeldCalls();
        }

        public void RequestMeldCall(MeldCallKind kind, int chiOptionId)
        {
            MeldCallRequested?.Invoke(kind, chiOptionId);
        }

        public void RequestDeclineMeldCalls()
        {
            DeclineMeldCallsRequested?.Invoke();
        }

        /// <summary>
        /// Binds the static reaction buttons to one immutable request identity.
        /// The lambdas deliberately capture the supplied ids so a delayed UI
        /// callback from a closed window cannot be rewritten as a response to
        /// a newer window.
        /// </summary>
        public void SetReactionResponseBindings(
            long requestId,
            int windowId,
            bool showRon,
            bool showPon,
            bool showMeldPass)
        {
            ClearReactionResponseBindings();
            if (requestId <= 0 || windowId <= 0)
                return;

            if (showRon && winButton != null)
            {
                reactionWinAction = () => RequestReactionResponse(
                    requestId,
                    windowId,
                    ReactionWindowSeatAnswerKind.Ron,
                    null);
                winButton.onClick.AddListener(reactionWinAction);
            }

            if (showRon && declineWinButton != null)
            {
                reactionDeclineWinAction = () => RequestReactionResponse(
                    requestId,
                    windowId,
                    ReactionWindowSeatAnswerKind.Pass,
                    null);
                declineWinButton.onClick.AddListener(reactionDeclineWinAction);
            }
            else if (showMeldPass && declinePonButton != null)
            {
                reactionDeclinePonAction = () => RequestReactionResponse(
                    requestId,
                    windowId,
                    ReactionWindowSeatAnswerKind.Pass,
                    null);
                declinePonButton.onClick.AddListener(reactionDeclinePonAction);
            }

            if (showPon && ponButton != null)
            {
                reactionPonAction = () => RequestReactionResponse(
                    requestId,
                    windowId,
                    ReactionWindowSeatAnswerKind.Pon,
                    null);
                ponButton.onClick.AddListener(reactionPonAction);
            }
        }

        public void ClearReactionResponseBindings()
        {
            if (reactionWinAction != null && winButton != null)
                winButton.onClick.RemoveListener(reactionWinAction);
            if (reactionDeclineWinAction != null && declineWinButton != null)
                declineWinButton.onClick.RemoveListener(reactionDeclineWinAction);
            if (reactionPonAction != null && ponButton != null)
                ponButton.onClick.RemoveListener(reactionPonAction);
            if (reactionDeclinePonAction != null && declinePonButton != null)
                declinePonButton.onClick.RemoveListener(reactionDeclinePonAction);

            reactionWinAction = null;
            reactionDeclineWinAction = null;
            reactionPonAction = null;
            reactionDeclinePonAction = null;
        }

        /// <summary>
        /// Binds the self-draw win controls to an authority-issued decision.
        /// Capturing the request id prevents a delayed click from being
        /// interpreted as a choice for a later draw.
        /// </summary>
        public void SetWinDecisionResponseBindings(long requestId)
        {
            ClearWinDecisionResponseBindings();
            if (requestId <= 0)
                return;

            if (winButton != null)
            {
                winDecisionAction = () => RequestWinDecisionResponse(requestId, true);
                winButton.onClick.AddListener(winDecisionAction);
            }

            if (declineWinButton != null)
            {
                declineWinDecisionAction = () => RequestWinDecisionResponse(requestId, false);
                declineWinButton.onClick.AddListener(declineWinDecisionAction);
            }
        }

        public void ClearWinDecisionResponseBindings()
        {
            if (winDecisionAction != null && winButton != null)
                winButton.onClick.RemoveListener(winDecisionAction);
            if (declineWinDecisionAction != null && declineWinButton != null)
                declineWinButton.onClick.RemoveListener(declineWinDecisionAction);

            winDecisionAction = null;
            declineWinDecisionAction = null;
        }

        public void RequestWinDecisionResponse(long requestId, bool accepted)
        {
            WinDecisionResponseRequested?.Invoke(requestId, accepted);
        }

        public void RequestReactionResponse(
            long requestId,
            int windowId,
            ReactionWindowSeatAnswerKind kind,
            int? chiOptionId = null)
        {
            ReactionResponseRequested?.Invoke(
                requestId,
                windowId,
                kind,
                chiOptionId);
        }

        public void RequestSelfKan(
            SelfKanKind kind,
            int tileTypeIndex,
            int sourcePonMeldIndex)
        {
            SelfKanRequested?.Invoke(kind, tileTypeIndex, sourcePonMeldIndex);
        }

        public void RequestDeclineSelfKan()
        {
            DeclineSelfKanRequested?.Invoke();
        }

        public void RequestSelfKanDecisionResponse(
            long requestId,
            bool accepted,
            int optionId = -1)
        {
            SelfKanDecisionResponseRequested?.Invoke(requestId, accepted, optionId);
        }

        private void HandleReachClicked()
        {
            if (reachDecisionAction != null)
                return;

            ReachRequested?.Invoke();
        }

        private void HandleDeclineReachClicked()
        {
            if (declineReachDecisionAction != null)
                return;

            DeclineReachRequested?.Invoke();
        }

        private void HandleCancelReachClicked()
        {
            CancelReachRequested?.Invoke();
        }

        /// <summary>
        /// Binds reach acceptance and decline to the exact request created by
        /// the authority. The actual discard stays on the existing command
        /// path after an accepted response.
        /// </summary>
        public void SetReachDecisionResponseBindings(long requestId)
        {
            ClearReachDecisionResponseBindings();
            if (requestId <= 0)
                return;

            if (reachButton != null)
            {
                reachDecisionAction = () => RequestReachDecisionResponse(requestId, true);
                reachButton.onClick.AddListener(reachDecisionAction);
            }

            if (declineReachButton != null)
            {
                declineReachDecisionAction = () => RequestReachDecisionResponse(requestId, false);
                declineReachButton.onClick.AddListener(declineReachDecisionAction);
            }
        }

        public void ClearReachDecisionResponseBindings()
        {
            if (reachDecisionAction != null && reachButton != null)
                reachButton.onClick.RemoveListener(reachDecisionAction);
            if (declineReachDecisionAction != null && declineReachButton != null)
                declineReachButton.onClick.RemoveListener(declineReachDecisionAction);

            reachDecisionAction = null;
            declineReachDecisionAction = null;
        }

        public void RequestReachDecisionResponse(long requestId, bool accepted)
        {
            ReachDecisionResponseRequested?.Invoke(requestId, accepted);
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
            // PROTOTYPE: Kept for direct legacy callers. TargetTileInput is
            // intentionally excluded so temporary game-state refreshes never
            // interrupt its IME composition or selection.
            SetDrawButtonInteractable(interactable);
            SetForceDrawSkillButtonInteractable(interactable);
        }

        public void SetDrawButtonInteractable(bool interactable)
        {
            if (drawButton != null)
                drawButton.interactable = interactable;
        }

        public void SetForceDrawSkillButtonInteractable(bool interactable)
        {
            if (forceDrawSkillButton != null)
                forceDrawSkillButton.interactable = interactable;
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
