using System.Collections.Generic;
using MahjongPrototype;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;

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
        [Tooltip("Controller for global status and skill text.")]
        [SerializeField] private MahjongUiDisplayController displayController;

        [Header("Player UI Controllers")]
        [SerializeField] private MahjongPlayerUiController selfBottomPlayerUiController;
        [SerializeField] private MahjongPlayerUiController nextLeftPlayerUiController;
        [SerializeField] private MahjongPlayerUiController acrossTopPlayerUiController;
        [SerializeField] private MahjongPlayerUiController previousRightPlayerUiController;

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
        private readonly HashSet<MahjongPlayerUiController> handEventSubscribedControllers =
            new HashSet<MahjongPlayerUiController>();
        private bool isDrawnTileControllerSubscribed;
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
            SubscribePlayerHandEvents();
            SubscribeDrawnTileEvents();
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
            SubscribePlayerHandEvents();
            SubscribeDrawnTileEvents();
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
            UnsubscribePlayerHandEvents();
            UnsubscribeDrawnTileEvents();
            UnsubscribeInputControllerEvents();
            UnsubscribeNotifications();
        }

        public void Refresh(MahjongGameState state)
        {
            if (state == null)
                return;

            RefreshDisplay(state);
            RefreshPlayerWinds(state);
            RefreshHand(state);
            RefreshDrawnTile(state);
            RefreshDiscardRiver(state);
            RefreshWinDecision(state);
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

            CachePlayerUiControllerReferences();

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
                    "MahjongEventNotifier is not assigned. Typed UI refresh events will not be received.");
                return;
            }

            eventNotifier.RoundStarted += HandleRoundStarted;
            eventNotifier.RoundSetupCompleted += HandleRoundSetupCompleted;
            eventNotifier.TurnStarted += HandleTurnStarted;
            eventNotifier.TileDrawn += HandleTileDrawn;
            eventNotifier.TileDiscarded += HandleTileDiscarded;
            eventNotifier.SkillActivated += HandleSkillActivated;
            eventNotifier.SkillEffectRegistered += HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved += HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired += HandleSkillEffectExpired;
            eventNotifier.WinChecked += HandleWinChecked;
            eventNotifier.WinDeclared += HandleWinDeclared;
            eventNotifier.WinDeclined += HandleWinDeclined;
            eventNotifier.HandAutoSorted += HandleHandAutoSorted;
            eventNotifier.RoundEnded += HandleRoundEnded;
        }

        private void UnsubscribeNotifications()
        {
            if (eventNotifier == null)
                return;

            eventNotifier.RoundStarted -= HandleRoundStarted;
            eventNotifier.RoundSetupCompleted -= HandleRoundSetupCompleted;
            eventNotifier.TurnStarted -= HandleTurnStarted;
            eventNotifier.TileDrawn -= HandleTileDrawn;
            eventNotifier.TileDiscarded -= HandleTileDiscarded;
            eventNotifier.SkillActivated -= HandleSkillActivated;
            eventNotifier.SkillEffectRegistered -= HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved -= HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired -= HandleSkillEffectExpired;
            eventNotifier.WinChecked -= HandleWinChecked;
            eventNotifier.WinDeclared -= HandleWinDeclared;
            eventNotifier.WinDeclined -= HandleWinDeclined;
            eventNotifier.HandAutoSorted -= HandleHandAutoSorted;
            eventNotifier.RoundEnded -= HandleRoundEnded;
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
                    "MahjongUiDisplayController is not assigned. Add it to the UI GameObject and assign the global status texts.");
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
        }

        private void HandleWinRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot declare win because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDeclareWin();
        }

        private void HandleDeclineWinRequested()
        {
            if (gameFlow == null)
            {
                WarnMissingOnce(ref warnedMissingFlow, "Cannot decline win because MahjongGameFlow is not assigned.");
                return;
            }

            gameFlow.RequestDeclineWin();
        }

        private void HandleRoundStarted(int _, int __)
        {
            RefreshFromFlow();
        }

        private void HandleRoundSetupCompleted()
        {
            RefreshFromFlow();
        }

        private void HandleTurnStarted(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleTileDrawn(DrawResult result)
        {
            if (!result.Success || result.Purpose == DrawPurpose.InitialDeal)
                return;

            RefreshHandForSeat(result.Seat);
            RefreshDrawnTileForSeat(result.Seat);
            RefreshGlobalStatus();
            RefreshInteractionUi();
        }

        private void HandleTileDiscarded(DiscardRecord record)
        {
            RefreshHandForSeat(record.ActorSeat);
            RefreshDrawnTileForSeat(record.ActorSeat);
            RefreshDiscardRiverForSeat(record.ActorSeat);
            RefreshGlobalStatus();
            RefreshInteractionUi();
        }

        private void HandleSkillActivated(SeatId _, ActiveSkillEffect __)
        {
            RefreshGlobalStatus();
        }

        private void HandleSkillEffectRegistered(ActiveSkillEffect _)
        {
            RefreshGlobalStatus();
        }

        private void HandleSkillEffectResolved(DrawResult _)
        {
            RefreshGlobalStatus();
        }

        private void HandleSkillEffectExpired(ActiveSkillEffect _, string __)
        {
            RefreshGlobalStatus();
        }

        private void HandleWinChecked(SeatId _, int __, bool ___)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclared(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleWinDeclined(SeatId _, int __)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void HandleHandAutoSorted(SeatId seat, int _)
        {
            RefreshHandForSeat(seat);
        }

        private void HandleRoundEnded(string _)
        {
            RefreshGlobalStatus();
            RefreshWinDecisionUi();
            RefreshInteractionUi();
        }

        private void RefreshDisplay(MahjongGameState state)
        {
            if (displayController == null)
                EnsureDisplayController();

            if (displayController != null)
                displayController.Refresh(state);
        }

        private void RefreshGlobalStatus()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshDisplay(state);
        }

        private void SubscribePlayerHandEvents()
        {
            CachePlayerUiControllerReferences();
            SubscribePlayerHandEvents(selfBottomPlayerUiController);
            SubscribePlayerHandEvents(nextLeftPlayerUiController);
            SubscribePlayerHandEvents(acrossTopPlayerUiController);
            SubscribePlayerHandEvents(previousRightPlayerUiController);
        }

        private void SubscribePlayerHandEvents(MahjongPlayerUiController controller)
        {
            if (controller == null || handEventSubscribedControllers.Contains(controller))
                return;

            controller.HandTileClicked += HandleHandTileClicked;
            handEventSubscribedControllers.Add(controller);
        }

        private void UnsubscribePlayerHandEvents()
        {
            foreach (MahjongPlayerUiController controller in handEventSubscribedControllers)
            {
                if (controller != null)
                    controller.HandTileClicked -= HandleHandTileClicked;
            }

            handEventSubscribedControllers.Clear();
        }

        private void SubscribeDrawnTileEvents()
        {
            MahjongPlayerUiController controller = GetPlayerUiController(ViewSlot.SelfBottom);
            if (controller == null || isDrawnTileControllerSubscribed)
                return;

            controller.DrawnTileClicked += HandleDrawnTileClicked;
            isDrawnTileControllerSubscribed = true;
        }

        private void UnsubscribeDrawnTileEvents()
        {
            if (!isDrawnTileControllerSubscribed)
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(ViewSlot.SelfBottom);
            if (controller != null)
                controller.DrawnTileClicked -= HandleDrawnTileClicked;

            isDrawnTileControllerSubscribed = false;
        }

        private void RefreshHand(MahjongGameState state)
        {
            SubscribePlayerHandEvents();

            bool canUseSelfInput = CanUseSelfGameplayInput(state);
            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId dataSeat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(dataSeat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, dataSeat);
                MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
                controller.RenderHand(
                    state.GetPlayerSeat(dataSeat).Hand.GetTiles(),
                    dataSeat,
                    isSelf,
                    isSelf && canUseSelfInput);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedPlayerHands(renderedViewSlots);
        }

        private void RefreshHandForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            if (seatSlot.IsEmpty)
                return;

            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
            controller.RenderHand(
                state.GetPlayerSeat(seat).Hand.GetTiles(),
                seat,
                isSelf,
                isSelf && CanUseSelfGameplayInput(state));
        }

        private void RefreshPlayerWinds(MahjongGameState state)
        {
            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId dataSeat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(dataSeat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, dataSeat);
                MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                controller.RenderWind(dataSeat);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedPlayerWinds(renderedViewSlots);
        }

        private void RefreshDrawnTile(MahjongGameState state)
        {
            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId seat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(seat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
                RefreshDrawnTileForSeat(state, seat);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedDrawnTiles(renderedViewSlots);
        }

        private void RefreshDrawnTileForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            RefreshDrawnTileForSeat(state, seat);
        }

        private void RefreshDrawnTileForSeat(MahjongGameState state, SeatId seat)
        {
            SeatSlot seatSlot = state.GetSeatSlot(seat);
            if (seatSlot.IsEmpty)
                return;

            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller == null)
                return;

            bool isSelf = seatSlot.PlayerId == state.SelfPlayerId;
            Tile? drawnTile = state.GetPlayerSeat(seat).DrawnTile;
            if (drawnTile.HasValue)
            {
                controller.RenderDrawnTile(
                    drawnTile,
                    isSelf,
                    isSelf && CanUseSelfGameplayInput(state));
            }
            else
            {
                controller.ClearDrawnTile();
            }
        }

        private void ClearUnrenderedDrawnTiles(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerDrawnTileIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerDrawnTileIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearDrawnTile();
        }

        private void RefreshDiscardRiver(MahjongGameState state)
        {
            HashSet<ViewSlot> renderedViewSlots = new HashSet<ViewSlot>();
            IReadOnlyList<SeatId> displaySeats = state.OccupiedSeats;
            for (int i = 0; i < displaySeats.Count; i++)
            {
                SeatId seat = displaySeats[i];
                SeatSlot seatSlot = state.GetSeatSlot(seat);
                if (seatSlot.IsEmpty)
                    continue;

                ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
                MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
                if (controller == null)
                    continue;

                controller.RenderDiscardRiver(state.Discards, seat);
                renderedViewSlots.Add(viewSlot);
            }

            ClearUnrenderedDiscardRivers(renderedViewSlots);
        }

        private void RefreshDiscardRiverForSeat(SeatId seat)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null)
                return;

            SeatSlot seatSlot = state.GetSeatSlot(seat);
            if (seatSlot.IsEmpty)
                return;

            ViewSlot viewSlot = SeatToViewSlotResolver.Resolve(state.SelfSeat, seat);
            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.RenderDiscardRiver(state.Discards, seat);
        }

        private void ClearUnrenderedDiscardRivers(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerDiscardRiverIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerDiscardRiverIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearDiscardRiver();
        }

        private void RefreshWinDecision(MahjongGameState state)
        {
            if (winDecisionController == null)
                EnsureWinDecisionController();

            if (winDecisionController != null)
            {
                bool showSelfWinDecision =
                    state != null &&
                    state.IsWinDecisionPending &&
                    state.WinDecisionSeat == state.SelfSeat;
                WinType? winType = showSelfWinDecision
                    ? state.WinDecisionType
                    : null;
                winDecisionController.SetWinDecision(showSelfWinDecision, winType);
            }
        }

        private void RefreshWinDecisionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshWinDecision(state);
        }

        private void RefreshInteractionState(MahjongGameState state)
        {
            bool canUseGameplayInput = CanUseSelfGameplayInput(state);

            if (inputController != null)
                inputController.SetGameplayInputInteractable(canUseGameplayInput);

            MahjongPlayerUiController selfController = GetPlayerUiController(
                SeatToViewSlotResolver.Resolve(state.SelfSeat, state.SelfSeat));
            if (selfController != null)
                selfController.SetHandInteractable(canUseGameplayInput);

            if (selfController != null)
                selfController.SetDrawnTileInteractable(canUseGameplayInput);
        }

        private void RefreshInteractionUi()
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state != null)
                RefreshInteractionState(state);
        }

        private bool CanUseSelfGameplayInput(MahjongGameState state)
        {
            return gameFlow != null &&
                state != null &&
                state.IsSelfTurn &&
                !state.IsInteractionLocked;
        }

        private void HandleHandTileClicked(SeatId dataSeat, int handIndex)
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

        private void CachePlayerUiControllerReferences()
        {
            if (selfBottomPlayerUiController == null)
                selfBottomPlayerUiController = FindPlayerUiController(ViewSlot.SelfBottom);

            if (nextLeftPlayerUiController == null)
                nextLeftPlayerUiController = FindPlayerUiController(ViewSlot.NextLeft);

            if (acrossTopPlayerUiController == null)
                acrossTopPlayerUiController = FindPlayerUiController(ViewSlot.AcrossTop);

            if (previousRightPlayerUiController == null)
                previousRightPlayerUiController = FindPlayerUiController(ViewSlot.PreviousRight);
        }

        private MahjongPlayerUiController GetPlayerUiController(ViewSlot viewSlot)
        {
            CachePlayerUiControllerReferences();
            switch (viewSlot)
            {
                case ViewSlot.SelfBottom:
                    return selfBottomPlayerUiController;
                case ViewSlot.NextLeft:
                    return nextLeftPlayerUiController;
                case ViewSlot.AcrossTop:
                    return acrossTopPlayerUiController;
                case ViewSlot.PreviousRight:
                    return previousRightPlayerUiController;
                default:
                    return null;
            }
        }

        private void ClearUnrenderedPlayerHands(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerHandIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerHandIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerHandIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerHandIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerHandIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearHand();
        }

        private void ClearUnrenderedPlayerWinds(HashSet<ViewSlot> renderedViewSlots)
        {
            ClearPlayerWindIfUnrendered(ViewSlot.SelfBottom, renderedViewSlots);
            ClearPlayerWindIfUnrendered(ViewSlot.NextLeft, renderedViewSlots);
            ClearPlayerWindIfUnrendered(ViewSlot.AcrossTop, renderedViewSlots);
            ClearPlayerWindIfUnrendered(ViewSlot.PreviousRight, renderedViewSlots);
        }

        private void ClearPlayerWindIfUnrendered(
            ViewSlot viewSlot,
            HashSet<ViewSlot> renderedViewSlots)
        {
            if (renderedViewSlots.Contains(viewSlot))
                return;

            MahjongPlayerUiController controller = GetPlayerUiController(viewSlot);
            if (controller != null)
                controller.ClearWind();
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
