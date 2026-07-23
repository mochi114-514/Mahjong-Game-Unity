using MahjongPrototype.Domain;
using MahjongPrototype.UI3D;
using UnityEngine;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong UI Command Router")]
    public sealed class MahjongUiCommandRouter : MonoBehaviour
    {
        [Header("Command Targets")]
        [Tooltip("Game flow controller that receives UI commands.")]
        [SerializeField] private MahjongGameFlow gameFlow;
        [Tooltip("Control area input event source.")]
        [SerializeField] private MahjongUiInputController inputController;
        [Tooltip("Retained scene reference; tile clicks are selected by MahjongPrototypeUiManager before routing.")]
        [SerializeField] private Mahjong3DPlayerAreaPresenter playerArea3DPresenter;

        private MahjongUiInputController subscribedInputController;
        private bool warnedMissingFlow;
        private bool warnedMissingInputController;

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
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void CacheReferences()
        {
            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (inputController == null)
                inputController = GetComponentInChildren<MahjongUiInputController>(true);

            if (playerArea3DPresenter == null)
                playerArea3DPresenter = GetComponentInChildren<Mahjong3DPlayerAreaPresenter>(true);

        }

        public void RefreshSubscriptions()
        {
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            CacheReferences();
            SubscribeInputControllerEvents();
        }

        private void UnsubscribeEvents()
        {
            UnsubscribeInputControllerEvents();
        }

        private void SubscribeInputControllerEvents()
        {
            if (inputController == null)
            {
                WarnMissingOnce(
                    ref warnedMissingInputController,
                    "MahjongUiInputController is not assigned. UI control commands will not be routed.");
                return;
            }

            if (subscribedInputController == inputController)
                return;

            UnsubscribeInputControllerEvents();
            inputController.DrawRequested += HandleDrawRequested;
            inputController.ForceDrawSkillRequested += HandleForceDrawSkillRequested;
            inputController.AutoSortChanged += HandleAutoSortChanged;
            inputController.RetryRequested += HandleRetryRequested;
            inputController.WinRequested += HandleWinRequested;
            inputController.DeclineWinRequested += HandleDeclineWinRequested;
            inputController.WinDecisionResponseRequested += HandleWinDecisionResponseRequested;
            inputController.AbortiveDrawDecisionResponseRequested +=
                HandleAbortiveDrawDecisionResponseRequested;
            inputController.MeldCallRequested += HandleMeldCallRequested;
            inputController.DeclineMeldCallsRequested += HandleDeclineMeldCallsRequested;
            inputController.ReactionResponseRequested += HandleReactionResponseRequested;
            inputController.SelfKanRequested += HandleSelfKanRequested;
            inputController.DeclineSelfKanRequested += HandleDeclineSelfKanRequested;
            inputController.SelfKanDecisionResponseRequested += HandleSelfKanDecisionResponseRequested;
            inputController.ReachRequested += HandleReachRequested;
            inputController.DeclineReachRequested += HandleDeclineReachRequested;
            inputController.ReachDecisionResponseRequested += HandleReachDecisionResponseRequested;
            inputController.CancelReachRequested += HandleCancelReachRequested;
            inputController.RoundResultConfirmRequested += HandleRoundResultConfirmRequested;
            subscribedInputController = inputController;
        }

        private void UnsubscribeInputControllerEvents()
        {
            if (subscribedInputController == null)
                return;

            subscribedInputController.DrawRequested -= HandleDrawRequested;
            subscribedInputController.ForceDrawSkillRequested -= HandleForceDrawSkillRequested;
            subscribedInputController.AutoSortChanged -= HandleAutoSortChanged;
            subscribedInputController.RetryRequested -= HandleRetryRequested;
            subscribedInputController.WinRequested -= HandleWinRequested;
            subscribedInputController.DeclineWinRequested -= HandleDeclineWinRequested;
            subscribedInputController.WinDecisionResponseRequested -= HandleWinDecisionResponseRequested;
            subscribedInputController.AbortiveDrawDecisionResponseRequested -=
                HandleAbortiveDrawDecisionResponseRequested;
            subscribedInputController.MeldCallRequested -= HandleMeldCallRequested;
            subscribedInputController.DeclineMeldCallsRequested -= HandleDeclineMeldCallsRequested;
            subscribedInputController.ReactionResponseRequested -= HandleReactionResponseRequested;
            subscribedInputController.SelfKanRequested -= HandleSelfKanRequested;
            subscribedInputController.DeclineSelfKanRequested -= HandleDeclineSelfKanRequested;
            subscribedInputController.SelfKanDecisionResponseRequested -= HandleSelfKanDecisionResponseRequested;
            subscribedInputController.ReachRequested -= HandleReachRequested;
            subscribedInputController.DeclineReachRequested -= HandleDeclineReachRequested;
            subscribedInputController.ReachDecisionResponseRequested -= HandleReachDecisionResponseRequested;
            subscribedInputController.CancelReachRequested -= HandleCancelReachRequested;
            subscribedInputController.RoundResultConfirmRequested -= HandleRoundResultConfirmRequested;
            subscribedInputController = null;
        }

        private void HandleDrawRequested()
        {
            if (!TryGetGameFlow("Cannot draw because MahjongGameFlow is not assigned."))
                return;

            TryExecuteLocalTurnCommand(MahjongAuthorityCommandKind.Draw);
        }

        private void HandleForceDrawSkillRequested(string targetTileText)
        {
            if (!TryGetGameFlow("Cannot activate skill because MahjongGameFlow is not assigned."))
                return;

            TryExecuteLocalTurnCommand(
                MahjongAuthorityCommandKind.ForceDrawSkill,
                textPayload: targetTileText);
        }

        private void HandleAutoSortChanged(bool enabled)
        {
            if (!TryGetGameFlow("Cannot change auto sort because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestSetAutoSortEnabled(enabled);
        }

        private void HandleRetryRequested()
        {
            if (!TryGetGameFlow("Cannot retry because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RetryPrototype();
        }

        private void HandleWinRequested()
        {
            if (!TryGetGameFlow("Cannot declare win because MahjongGameFlow is not assigned."))
                return;

            TrySubmitCurrentDecisionResponse(DecisionKind.WinDeclaration, true);
        }

        private void HandleDeclineWinRequested()
        {
            if (!TryGetGameFlow("Cannot decline win because MahjongGameFlow is not assigned."))
                return;

            TrySubmitCurrentDecisionResponse(DecisionKind.WinDeclaration, false);
        }

        private void HandleWinDecisionResponseRequested(long requestId, bool accepted)
        {
            if (!TryGetGameFlow("Cannot submit win decision because MahjongGameFlow is not assigned."))
                return;

            TrySubmitBoundDecisionResponse(
                requestId,
                DecisionKind.WinDeclaration,
                accepted);
        }

        private void HandleAbortiveDrawDecisionResponseRequested(
            long requestId,
            bool accepted)
        {
            if (!TryGetGameFlow(
                    "Cannot submit abortive draw decision because MahjongGameFlow is not assigned."))
            {
                return;
            }

            TrySubmitBoundDecisionResponse(
                requestId,
                DecisionKind.AbortiveDraw,
                accepted);
        }

        private void HandleMeldCallRequested(MeldCallKind kind, int optionId)
        {
            if (!TryGetGameFlow("Cannot respond to a meld call because MahjongGameFlow is not assigned."))
                return;

            if (kind == MeldCallKind.Kan)
                TrySubmitLegacySelfKanResponse(
                    SelfKanKind.Ankan,
                    optionId,
                    -1);
        }

        private void HandleDeclineMeldCallsRequested()
        {
            if (!TryGetGameFlow("Cannot decline meld calls because MahjongGameFlow is not assigned."))
                return;

            MahjongGameState state = gameFlow.CurrentState;
            // A reaction pass is emitted only by an identity-bound request
            // callback; it must not fall through to the legacy direct path.
            if (state != null && state.IsReactionWindowPending)
                return;
        }

        private void HandleReactionResponseRequested(
            long requestId,
            int windowId,
            ReactionWindowSeatAnswerKind kind,
            int? chiOptionId)
        {
            if (!TryGetGameFlow("Cannot submit reaction response because MahjongGameFlow is not assigned."))
                return;

            TrySubmitBoundReactionResponse(
                requestId,
                windowId,
                kind,
                chiOptionId);
        }

        private void HandleSelfKanRequested(
            SelfKanKind kind,
            int tileTypeIndex,
            int sourcePonMeldIndex)
        {
            if (!TryGetGameFlow("Cannot declare a self kan because MahjongGameFlow is not assigned."))
                return;

            TrySubmitLegacySelfKanResponse(kind, tileTypeIndex, sourcePonMeldIndex);
        }

        private void HandleDeclineSelfKanRequested()
        {
            if (!TryGetGameFlow("Cannot decline self kan because MahjongGameFlow is not assigned."))
                return;

            TrySubmitCurrentDecisionResponse(DecisionKind.SelfKan, false);
        }

        private void HandleSelfKanDecisionResponseRequested(
            long requestId,
            bool accepted,
            int optionId)
        {
            if (!TryGetGameFlow("Cannot submit self kan decision because MahjongGameFlow is not assigned."))
                return;

            TrySubmitBoundDecisionResponse(
                requestId,
                DecisionKind.SelfKan,
                accepted,
                accepted ? new SelfKanDecisionResponse(optionId) : null);
        }

        private void HandleReachRequested()
        {
            if (!TryGetGameFlow("Cannot declare reach because MahjongGameFlow is not assigned."))
                return;

            TrySubmitCurrentDecisionResponse(DecisionKind.Reach, true);
        }

        private void HandleDeclineReachRequested()
        {
            if (!TryGetGameFlow("Cannot decline reach because MahjongGameFlow is not assigned."))
                return;

            TrySubmitCurrentDecisionResponse(DecisionKind.Reach, false);
        }

        private void HandleReachDecisionResponseRequested(long requestId, bool accepted)
        {
            if (!TryGetGameFlow("Cannot submit reach decision because MahjongGameFlow is not assigned."))
                return;

            TrySubmitBoundDecisionResponse(requestId, DecisionKind.Reach, accepted);
        }

        private void HandleCancelReachRequested()
        {
            if (!TryGetGameFlow("Cannot cancel reach discard selection because MahjongGameFlow is not assigned."))
                return;

            TryExecuteLocalTurnCommand(
                MahjongAuthorityCommandKind.CancelReachDiscardSelection);
        }

        private void HandleRoundResultConfirmRequested()
        {
            if (!TryGetGameFlow("Cannot advance round result because MahjongGameFlow is not assigned."))
                return;

            gameFlow.RequestAdvanceFromRoundResult();
        }

        public bool TryDiscardHandFromTileSelection(SeatId dataSeat, int handIndex)
        {
            if (!TryGetGameFlow("Cannot discard because MahjongGameFlow is not assigned."))
                return false;

            MahjongGameState state = gameFlow.CurrentState;
            if (state == null || !TryGetLocalActor(state, out _, out SeatId actorSeat) ||
                dataSeat != actorSeat)
            {
                return false;
            }

            return TryExecuteLocalTurnCommand(
                MahjongAuthorityCommandKind.DiscardHandFromTileClick,
                handIndex);
        }

        public bool TryDiscardDrawnTileFromTileSelection()
        {
            if (!TryGetGameFlow("Cannot discard drawn tile because MahjongGameFlow is not assigned."))
                return false;

            return TryExecuteLocalTurnCommand(
                MahjongAuthorityCommandKind.DiscardDrawnTileFromTileClick);
        }

        private bool TryExecuteLocalTurnCommand(
            MahjongAuthorityCommandKind kind,
            int handIndex = -1,
            string textPayload = null)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || !TryGetLocalActor(state, out PlayerId playerId, out SeatId seat))
                return false;

            return gameFlow.TryExecuteCommand(new MahjongAuthorityCommand(
                kind,
                playerId,
                seat,
                state.TurnIndex,
                handIndex,
                textPayload)).Accepted;
        }

        private bool TrySubmitCurrentDecisionResponse(
            DecisionKind kind,
            bool accepted)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || !TryGetLocalActor(state, out PlayerId playerId, out _) ||
                !gameFlow.TryGetPendingDecisionRequest(playerId, kind, out DecisionRequest request))
            {
                return false;
            }

            return TrySubmitBoundDecisionResponse(request.RequestId, kind, accepted);
        }

        private bool TrySubmitLegacySelfKanResponse(
            SelfKanKind kind,
            int tileTypeIndex,
            int sourcePonMeldIndex)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || !TryGetLocalActor(state, out PlayerId playerId, out _) ||
                !gameFlow.TryGetPendingDecisionRequest(
                    playerId,
                    DecisionKind.SelfKan,
                    out DecisionRequest request) ||
                request.SelfKan == null)
            {
                return false;
            }

            for (int i = 0; i < request.SelfKan.Options.Count; i++)
            {
                SelfKanDecisionOption option = request.SelfKan.Options[i];
                if (option.Kind == kind && option.Tile.TypeIndex == tileTypeIndex &&
                    option.SourcePonMeldIndex == sourcePonMeldIndex)
                {
                    return TrySubmitBoundDecisionResponse(
                        request.RequestId,
                        DecisionKind.SelfKan,
                        true,
                        new SelfKanDecisionResponse(option.OptionId));
                }
            }

            return false;
        }

        private bool TrySubmitBoundDecisionResponse(
            long requestId,
            DecisionKind kind,
            bool accepted,
            SelfKanDecisionResponse selfKan = null)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || !TryGetLocalActor(state, out PlayerId playerId, out SeatId seat) ||
                !gameFlow.TryGetPendingDecisionRequest(playerId, kind, out DecisionRequest request) ||
                request.Kind != kind || request.RequestId != requestId ||
                request.PlayerId != playerId || request.ActorSeat != seat ||
                request.TurnIndex != state.TurnIndex ||
                !gameFlow.TryGetLocalUiDecisionProvider(
                    playerId,
                    out LocalUiDecisionProvider provider))
            {
                return false;
            }

            DecisionResponse response = selfKan == null
                ? new DecisionResponse(
                    request.RequestId,
                    kind,
                    playerId,
                    seat,
                    request.TurnIndex,
                    accepted)
                : new DecisionResponse(
                    request.RequestId,
                    kind,
                    playerId,
                    seat,
                    request.TurnIndex,
                    accepted,
                    selfKan);
            return provider.TrySubmitResponse(response);
        }

        private bool TrySubmitBoundReactionResponse(
            long requestId,
            int windowId,
            ReactionWindowSeatAnswerKind kind,
            int? chiOptionId)
        {
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            if (state == null || !TryGetLocalActor(state, out PlayerId playerId, out SeatId seat) ||
                !gameFlow.TryGetPendingReactionDecisionRequest(playerId, out DecisionRequest request))
            {
                return false;
            }

            if (request.Kind != DecisionKind.Reaction || request.Reaction == null ||
                request.PlayerId != playerId || request.ActorSeat != seat ||
                request.RequestId != requestId || request.Reaction.WindowId != windowId ||
                !request.Reaction.Allows(kind) ||
                !gameFlow.TryGetLocalUiDecisionProvider(
                    playerId,
                    out LocalUiDecisionProvider provider))
            {
                return false;
            }

            DecisionResponse response = new DecisionResponse(
                request.RequestId,
                DecisionKind.Reaction,
                playerId,
                seat,
                request.TurnIndex,
                true,
                new ReactionDecisionResponse(
                    request.Reaction.WindowId,
                    kind,
                    chiOptionId));
            return provider.TrySubmitResponse(response);
        }

        private bool TryGetLocalActor(
            MahjongGameState state,
            out PlayerId playerId,
            out SeatId seat)
        {
            playerId = default;
            seat = default;
            if (gameFlow == null || state == null || gameFlow.ViewContext == null ||
                !gameFlow.ViewContext.TryGetSelfSeat(state, out seat))
            {
                return false;
            }

            playerId = gameFlow.ViewContext.LocalPlayerId;
            return true;
        }

        private bool TryGetGameFlow(string warning)
        {
            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (gameFlow != null)
                return true;

            WarnMissingOnce(ref warnedMissingFlow, warning);
            return false;
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongUiCommandRouter)}: {message}", this);
        }
    }
}
