using MahjongPrototype;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Prototype UI Manager")]
    public sealed class MahjongPrototypeUiManager : MonoBehaviour
    {
        [Header("Flow")]
        [Tooltip("Game flow controller for the prototype.")]
        [SerializeField] private MahjongGameFlow gameFlow;
        [Tooltip("Event notifier used to refresh UI after game events.")]
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Display")]
        [Tooltip("Controller for status, skill, and discard text.")]
        [SerializeField] private MahjongUiDisplayController displayController;

        [Header("Player UI Controllers")]
        [SerializeField] private MahjongPlayerUiController selfBottomPlayerUiController;

        [Header("Tile Areas")]
        [Tooltip("View for hand tiles.")]
        [SerializeField] private MahjongHandView handView;
        [SerializeField] private MahjongHandBoardView handBoardView;
        [SerializeField] private MahjongDrawnTileView drawnTileView;
        [SerializeField] private MahjongDiscardRiverView discardRiverView;
        [Tooltip("Container for hand tile buttons.")]
        [SerializeField] private RectTransform handContainer;
        [SerializeField] private RectTransform drawnTileContainer;
        [FormerlySerializedAs("eastDiscardRiverContainer")]
        [SerializeField] private RectTransform selfBottomDiscardRiverContainer;
        [Tooltip("Prefab used to render a single tile button.")]
        [SerializeField] private TileButtonView tileButtonPrefab;

        [Header("Input")]
        [Tooltip("Controller for draw, skill, retry, and win decision input.")]
        [SerializeField] private MahjongUiInputController inputController;

        [Header("Win Decision")]
        [SerializeField] private MahjongWinDecisionController winDecisionController;

        [Header("Log Preview")]
        [Tooltip("Controller for the on-screen recent log preview.")]
        [SerializeField] private MahjongLogPreviewController logPreviewController;

        private bool warnedMissingFlow;
        private bool warnedMissingEventNotifier;
        private bool warnedMissingDisplayController;
        private bool warnedMissingInputController;
        private bool warnedMissingWinDecisionController;
        private bool warnedMissingLogPreviewController;
        private bool warnedMissingDiscardArea;
        private bool isHandBoardViewSubscribed;
        private bool isSelfBottomPlayerUiControllerSubscribed;
        private bool isDrawnTileViewSubscribed;
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
            EnsureHandBoardView();
            SubscribeHandBoardViewEvents();
            EnsureDrawnTileView();
            ConfigureSelfBottomPlayerUiControllerViews();
            SubscribeDrawnTileViewEvents();
            if (selfBottomPlayerUiController == null)
                EnsureDiscardRiverView();
            EnsureInputController();
            SubscribeInputControllerEvents();
            SyncAutoSortToggleFromFlow();
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
            EnsureHandBoardView();
            SubscribeHandBoardViewEvents();
            EnsureDrawnTileView();
            ConfigureSelfBottomPlayerUiControllerViews();
            SubscribeDrawnTileViewEvents();
            if (selfBottomPlayerUiController == null)
                EnsureDiscardRiverView();
            EnsureInputController();
            SubscribeInputControllerEvents();
            SyncAutoSortToggleFromFlow();
            EnsureWinDecisionController();
            EnsureLogPreviewController();
            RefreshFromFlow();
            RefreshLogPreview();
        }

        private void OnDisable()
        {
            UnsubscribeHandBoardViewEvents();
            UnsubscribeDrawnTileViewEvents();
            UnsubscribeInputControllerEvents();
            UnsubscribeNotifications();
        }

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
                return;

            RefreshDisplay(state);
            RefreshHand(state);
            RefreshDrawnTile(state);
            RefreshDiscardRiver(state);
            RefreshWinDecision();
            RefreshInteractionState(state);
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

            if (selfBottomPlayerUiController == null)
                selfBottomPlayerUiController = FindPlayerUiController(ViewSlot.SelfBottom);

            if (handView == null)
                handView = GetComponentInChildren<MahjongHandView>(true);

            if (handBoardView == null)
                handBoardView = GetComponentInChildren<MahjongHandBoardView>(true);

            if (drawnTileView == null)
                drawnTileView = GetComponentInChildren<MahjongDrawnTileView>(true);

            if (discardRiverView == null)
                discardRiverView = GetComponentInChildren<MahjongDiscardRiverView>(true);

            ConfigureSelfBottomPlayerUiControllerViews();

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
            inputController.AutoSortChanged += HandleAutoSortChanged;
            inputController.RetryRequested += HandleRetryRequested;
            inputController.WinRequested += HandleWinRequested;
            inputController.DeclineWinRequested += HandleDeclineWinRequested;
            isInputControllerSubscribed = true;
        }

        private void SyncAutoSortToggleFromFlow()
        {
            if (inputController == null || gameFlow == null)
                return;

            inputController.SetAutoSortWithoutNotify(gameFlow.IsAutoSortEnabled);
        }

        private void UnsubscribeInputControllerEvents()
        {
            if (inputController == null || !isInputControllerSubscribed)
                return;

            inputController.DrawRequested -= HandleDrawRequested;
            inputController.ForceDrawSkillRequested -= HandleForceDrawSkillRequested;
            inputController.AutoSortChanged -= HandleAutoSortChanged;
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

        private void HandleAutoSortChanged(bool enabled)
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot change auto sort because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestSetAutoSortEnabled(enabled);
        }

        private void HandleRetryRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot retry because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RetryPrototype();
            RefreshFromFlow();
        }

        private void HandleWinRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot declare win because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDeclareWin();
            RefreshFromFlow();
        }

        private void HandleDeclineWinRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot decline win because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDeclineWin();
            RefreshFromFlow();
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

        private void EnsureHandBoardView()
        {
            if (handBoardView == null)
            {
                handBoardView = GetComponentInChildren<MahjongHandBoardView>(true);
            }

            if (handBoardView == null)
                handBoardView = gameObject.AddComponent<MahjongHandBoardView>();

            handBoardView.ConfigureMissingReferences(handView);
        }

        private void EnsureDrawnTileView()
        {
            if (drawnTileView == null)
            {
                drawnTileView = GetComponentInChildren<MahjongDrawnTileView>(true);
            }

            if (drawnTileView == null)
            {
                drawnTileView = gameObject.AddComponent<MahjongDrawnTileView>();
                drawnTileView.Configure(drawnTileContainer, tileButtonPrefab);
                return;
            }

            drawnTileView.ConfigureMissingReferences(drawnTileContainer, tileButtonPrefab);
        }

        private void EnsureDiscardRiverView()
        {
            if (discardRiverView == null)
            {
                discardRiverView = GetComponentInChildren<MahjongDiscardRiverView>(true);
            }

            RectTransform container = GetOrCreateSelfBottomDiscardRiverContainer();
            if (discardRiverView == null)
            {
                discardRiverView = gameObject.AddComponent<MahjongDiscardRiverView>();
                discardRiverView.Configure(container, tileButtonPrefab);
                return;
            }

            discardRiverView.ConfigureMissingReferences(container, tileButtonPrefab);
        }

        private void ConfigureSelfBottomPlayerUiControllerViews()
        {
            if (selfBottomPlayerUiController == null)
                selfBottomPlayerUiController = FindPlayerUiController(ViewSlot.SelfBottom);

            if (selfBottomPlayerUiController == null)
                return;

            selfBottomPlayerUiController.ConfigureMissingViews(handView, discardRiverView, drawnTileView);
        }

        private void SubscribeHandBoardViewEvents()
        {
            if (handBoardView == null || isHandBoardViewSubscribed)
                return;

            handBoardView.TileClicked += HandleHandBoardTileClicked;
            isHandBoardViewSubscribed = true;
        }

        private void UnsubscribeHandBoardViewEvents()
        {
            if (handBoardView == null || !isHandBoardViewSubscribed)
                return;

            handBoardView.TileClicked -= HandleHandBoardTileClicked;
            isHandBoardViewSubscribed = false;
        }

        private void SubscribeDrawnTileViewEvents()
        {
            ConfigureSelfBottomPlayerUiControllerViews();
            if (selfBottomPlayerUiController != null)
            {
                if (isSelfBottomPlayerUiControllerSubscribed)
                    return;

                UnsubscribeFallbackDrawnTileViewEvents();
                selfBottomPlayerUiController.DrawnTileClicked += HandleDrawnTileClicked;
                isSelfBottomPlayerUiControllerSubscribed = true;
                return;
            }

            if (drawnTileView == null || isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked += HandleDrawnTileClicked;
            isDrawnTileViewSubscribed = true;
        }

        private void UnsubscribeDrawnTileViewEvents()
        {
            if (selfBottomPlayerUiController != null && isSelfBottomPlayerUiControllerSubscribed)
            {
                selfBottomPlayerUiController.DrawnTileClicked -= HandleDrawnTileClicked;
                isSelfBottomPlayerUiControllerSubscribed = false;
            }

            if (drawnTileView == null || !isDrawnTileViewSubscribed)
                return;

            UnsubscribeFallbackDrawnTileViewEvents();
        }

        private void UnsubscribeFallbackDrawnTileViewEvents()
        {
            if (drawnTileView == null || !isDrawnTileViewSubscribed)
                return;

            drawnTileView.DrawnTileClicked -= HandleDrawnTileClicked;
            isDrawnTileViewSubscribed = false;
        }

        private void RefreshHand(MahjongGameState state)
        {
            if (handView == null)
                EnsureHandView();

            if (handBoardView == null)
                EnsureHandBoardView();

            if (handBoardView != null)
                handBoardView.Render(state, state.OccupiedSeats, CanUseSelfGameplayInput(state));
        }

        private void RefreshDrawnTile(MahjongGameState state)
        {
            ConfigureSelfBottomPlayerUiControllerViews();
            if (selfBottomPlayerUiController != null)
            {
                selfBottomPlayerUiController.RenderDrawnTile(state, state.SelfSeat);
                return;
            }

            if (drawnTileView == null)
                EnsureDrawnTileView();

            if (drawnTileView != null)
                drawnTileView.Rebuild(state.GetPlayerSeat(state.SelfSeat).DrawnTile);
        }

        private void RefreshDiscardRiver(MahjongGameState state)
        {
            if (selfBottomPlayerUiController == null)
                selfBottomPlayerUiController = FindPlayerUiController(ViewSlot.SelfBottom);

            ConfigureSelfBottomPlayerUiControllerViews();
            if (selfBottomPlayerUiController != null)
            {
                selfBottomPlayerUiController.RenderDiscardRiver(state, state.SelfSeat);
                return;
            }

            if (discardRiverView == null)
                EnsureDiscardRiverView();

            if (discardRiverView != null)
                discardRiverView.Rebuild(state.Discards, state.SelfSeat);
        }

        private void RefreshWinDecision()
        {
            if (winDecisionController == null)
                EnsureWinDecisionController();

            if (winDecisionController != null)
                winDecisionController.SetVisible(gameFlow != null && gameFlow.IsWinDecisionPending);
        }

        private void RefreshInteractionState(MahjongGameState state)
        {
            bool canUseGameplayInput = CanUseSelfGameplayInput(state);

            if (inputController != null)
                inputController.SetGameplayInputInteractable(canUseGameplayInput);

            if (handBoardView != null)
                handBoardView.SetSelfInteractable(state, canUseGameplayInput);

            ConfigureSelfBottomPlayerUiControllerViews();
            if (selfBottomPlayerUiController != null)
                selfBottomPlayerUiController.SetDrawnTileInteractable(canUseGameplayInput);
            else if (drawnTileView != null)
                drawnTileView.SetTileInteractable(canUseGameplayInput);
        }

        private bool CanUseSelfGameplayInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                state.IsSelfTurn &&
                !gameFlow.IsInteractionLocked;
        }

        private void HandleHandBoardTileClicked(SeatId dataSeat, int handIndex)
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot discard because MahjongGameFlow is not assigned.");
                return;
            }

            MahjongGameState state = gameFlow.CurrentState;
            if (state == null || dataSeat != state.SelfSeat)
                return;

            gameFlow.RequestDiscard(handIndex);
        }

        private void HandleDrawnTileClicked()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot discard drawn tile because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDiscardDrawnTile();
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

        private RectTransform GetOrCreateSelfBottomDiscardRiverContainer()
        {
            if (selfBottomDiscardRiverContainer != null)
                return selfBottomDiscardRiverContainer;

            selfBottomDiscardRiverContainer = FindRectTransformByName("SelfBottomDiscardRiverContainer");
            if (selfBottomDiscardRiverContainer != null)
                return selfBottomDiscardRiverContainer;

            selfBottomDiscardRiverContainer = FindRectTransformByName("EastDiscardRiverContainer");
            if (selfBottomDiscardRiverContainer != null)
                return selfBottomDiscardRiverContainer;

            RectTransform discardArea = FindRectTransformByName("DiscardArea");
            if (discardArea == null)
            {
                WarnMissingOnce(
                    ref warnedMissingDiscardArea,
                    "DiscardArea is not found. Add SelfBottomDiscardRiverContainer under DiscardArea for discard tiles.");
                return null;
            }

            GameObject containerObject = new GameObject("SelfBottomDiscardRiverContainer", typeof(RectTransform));
            selfBottomDiscardRiverContainer = containerObject.GetComponent<RectTransform>();
            selfBottomDiscardRiverContainer.SetParent(discardArea, false);
            selfBottomDiscardRiverContainer.anchorMin = new Vector2(0.5f, 0.5f);
            selfBottomDiscardRiverContainer.anchorMax = new Vector2(0.5f, 0.5f);
            selfBottomDiscardRiverContainer.pivot = new Vector2(0.5f, 0.5f);
            selfBottomDiscardRiverContainer.anchoredPosition = new Vector2(0f, -85f);
            selfBottomDiscardRiverContainer.sizeDelta = new Vector2(380f, 140f);
            return selfBottomDiscardRiverContainer;
        }

        private RectTransform FindRectTransformByName(string objectName)
        {
            RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];
                if (rectTransform != null && rectTransform.gameObject.name == objectName)
                    return rectTransform;
            }

            return null;
        }

        private MahjongPlayerUiController FindPlayerUiController(ViewSlot targetViewSlot)
        {
            MahjongPlayerUiController[] controllers = GetComponentsInChildren<MahjongPlayerUiController>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                MahjongPlayerUiController controller = controllers[i];
                if (controller != null && controller.ViewSlot == targetViewSlot)
                    return controller;
            }

            return null;
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
