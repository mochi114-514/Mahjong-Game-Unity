using MahjongPrototype;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Prototype UI Manager")]
    public sealed class MahjongPrototypeUiManager : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("ゲーム進行の指示役です。MahjongPrototypeRoot の MahjongGameFlow を割り当てます。")]
        [SerializeField] private MahjongGameFlow gameFlow;
        [Tooltip("決定済みイベントの通知役です。MahjongPrototypeRoot の MahjongEventNotifier を割り当てます。")]
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Display")]
        [Tooltip("Status/ActiveSkill/Discard の表示更新Controllerです。")]
        [SerializeField] private MahjongUiDisplayController displayController;

        [Header("Tile Areas")]
        [Tooltip("手牌ボタン表示のViewです。未設定ならPlay時に同じGameObjectへ追加します。")]
        [SerializeField] private MahjongHandView handView;
        [Tooltip("手牌ボタンを生成する親RectTransformです。Canvas/HandArea を割り当てます。")]
        [SerializeField] private RectTransform handContainer;
        [Tooltip("手牌1枚分のTileButtonViewテンプレートまたはPrefabです。")]
        [SerializeField] private TileButtonView tileButtonPrefab;

        [Header("Input")]
        [Tooltip("Draw/SkillDraw/Retry の入力受付Controllerです。")]
        [SerializeField] private MahjongUiInputController inputController;

        [Header("Win Decision")]
        [SerializeField] private MahjongWinDecisionController winDecisionController;

        [Header("Log Preview")]
        [Tooltip("画面ログ表示のControllerです。RecentLogText と表示行数はこのController側で設定します。")]
        [SerializeField] private MahjongLogPreviewController logPreviewController;

        private bool warnedMissingFlow;
        private bool warnedMissingEventNotifier;
        private bool warnedMissingDisplayController;
        private bool warnedMissingInputController;
        private bool warnedMissingWinDecisionController;
        private bool warnedMissingLogPreviewController;
        private bool isHandViewSubscribed;
        private bool isInputControllerSubscribed;

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
            EnsureDisplayController();
            EnsureHandView();
            SubscribeHandViewEvents();
            EnsureInputController();
            SubscribeInputControllerEvents();
            EnsureWinDecisionController();
            EnsureLogPreviewController();
            SubscribeNotifications();
            RefreshFromFlow();
        }

        private void Start()
        {
            CacheReferences();
            EnsureDisplayController();
            EnsureHandView();
            SubscribeHandViewEvents();
            EnsureInputController();
            SubscribeInputControllerEvents();
            EnsureWinDecisionController();
            EnsureLogPreviewController();
            RefreshFromFlow();
            RefreshLogPreview();
        }

        private void OnDisable()
        {
            UnsubscribeHandViewEvents();
            UnsubscribeInputControllerEvents();
            UnsubscribeNotifications();
        }

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
                return;

            RefreshDisplay(state);
            RefreshHand(state);
            RefreshWinDecision();
            RefreshLogPreview();
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

            if (displayController == null)
                displayController = GetComponentInChildren<MahjongUiDisplayController>(true);

            if (handView == null)
                handView = GetComponentInChildren<MahjongHandView>(true);

            if (inputController == null)
                inputController = GetComponentInChildren<MahjongUiInputController>(true);

            if (logPreviewController == null)
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);
        }

        private void SubscribeNotifications()
        {
            if (eventNotifier == null)
            {
                WarnMissingOnce(
                    ref warnedMissingEventNotifier,
                    "MahjongEventNotifier is not assigned. UI refresh depends on AnyEventNotified.");
                return;
            }

            eventNotifier.AnyEventNotified += RefreshFromFlow;
        }

        private void UnsubscribeNotifications()
        {
            if (eventNotifier == null)
                return;

            eventNotifier.AnyEventNotified -= RefreshFromFlow;
        }

        private void EnsureDisplayController()
        {
            if (displayController == null)
            {
                displayController = GetComponentInChildren<MahjongUiDisplayController>(true);
            }

            if (displayController != null)
                return;

            displayController = gameObject.AddComponent<MahjongUiDisplayController>();
            if (displayController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingDisplayController,
                    "MahjongUiDisplayController is not assigned. Add it to the UI GameObject and assign the status/discard texts.");
            }
        }

        private void EnsureInputController()
        {
            if (inputController == null)
            {
                inputController = GetComponentInChildren<MahjongUiInputController>(true);
            }

            if (inputController != null)
                return;

            inputController = gameObject.AddComponent<MahjongUiInputController>();
            if (inputController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingInputController,
                    "MahjongUiInputController is not assigned. Add it to the UI GameObject and assign the Draw/Skill/Retry controls.");
            }
        }

        private void SubscribeInputControllerEvents()
        {
            if (inputController == null || isInputControllerSubscribed)
                return;

            inputController.DrawRequested += HandleDrawRequested;
            inputController.ForceDrawSkillRequested += HandleForceDrawSkillRequested;
            inputController.RetryRequested += HandleRetryRequested;
            inputController.WinRequested += HandleWinRequested;
            inputController.DeclineWinRequested += HandleDeclineWinRequested;
            isInputControllerSubscribed = true;
        }

        private void UnsubscribeInputControllerEvents()
        {
            if (inputController == null || !isInputControllerSubscribed)
                return;

            inputController.DrawRequested -= HandleDrawRequested;
            inputController.ForceDrawSkillRequested -= HandleForceDrawSkillRequested;
            inputController.RetryRequested -= HandleRetryRequested;
            inputController.WinRequested -= HandleWinRequested;
            inputController.DeclineWinRequested -= HandleDeclineWinRequested;
            isInputControllerSubscribed = false;
        }

        private void EnsureWinDecisionController()
        {
            if (winDecisionController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingWinDecisionController,
                "MahjongWinDecisionController is not assigned. Add it to the UI GameObject and assign WinDecisionArea and its buttons.");
        }

        private void HandleDrawRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot draw because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDraw();
        }

        private void HandleForceDrawSkillRequested(string targetTileText)
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot activate skill because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestForceDrawSkill(targetTileText);
        }

        private void HandleRetryRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot retry because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RetryPrototype();
            RefreshWinDecision();
        }

        private void HandleWinRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot declare win because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDeclareWin();
            RefreshWinDecision();
        }

        private void HandleDeclineWinRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot decline win because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDeclineWin();
            RefreshWinDecision();
        }

        private void RefreshDisplay(MahjongGameState state)
        {
            if (displayController == null)
                EnsureDisplayController();

            if (displayController != null)
                displayController.Refresh(state);
        }

        private void EnsureHandView()
        {
            if (handView == null)
            {
                handView = GetComponentInChildren<MahjongHandView>(true);
            }

            if (handView == null)
            {
                handView = gameObject.AddComponent<MahjongHandView>();
                handView.Configure(handContainer, tileButtonPrefab);
                return;
            }

            handView.ConfigureMissingReferences(handContainer, tileButtonPrefab);
        }

        private void SubscribeHandViewEvents()
        {
            if (handView == null || isHandViewSubscribed)
                return;

            handView.TileClicked += HandleTileClicked;
            isHandViewSubscribed = true;
        }

        private void UnsubscribeHandViewEvents()
        {
            if (handView == null || !isHandViewSubscribed)
                return;

            handView.TileClicked -= HandleTileClicked;
            isHandViewSubscribed = false;
        }

        private void RefreshHand(MahjongGameState state)
        {
            if (handView == null)
                EnsureHandView();

            if (handView != null)
                handView.Rebuild(state.GetPlayerSeat(state.CurrentSeat).Hand.GetTiles());
        }

        private void RefreshWinDecision()
        {
            if (winDecisionController == null)
                EnsureWinDecisionController();

            if (winDecisionController != null)
                winDecisionController.SetVisible(gameFlow != null && gameFlow.IsWinDecisionPending);
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

        private void EnsureLogPreviewController()
        {
            if (logPreviewController == null)
            {
                logPreviewController = GetComponentInChildren<MahjongLogPreviewController>(true);
            }

            if (logPreviewController != null)
                return;

            WarnMissingOnce(
                ref warnedMissingLogPreviewController,
                "MahjongLogPreviewController is not assigned. Add it to the UI GameObject and assign RecentLogText there.");
        }

        private void RefreshLogPreview()
        {
            if (logPreviewController == null)
                EnsureLogPreviewController();

            if (logPreviewController != null)
                logPreviewController.Refresh();
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongPrototypeUiManager)}: {message}", this);
        }
    }
}
