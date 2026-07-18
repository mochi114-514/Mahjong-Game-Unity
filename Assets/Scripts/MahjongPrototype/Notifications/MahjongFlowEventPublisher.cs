using System;
using MahjongPrototype.Domain;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;

namespace MahjongPrototype.Notifications
{
    public sealed class MahjongFlowEventPublisher
    {
        private readonly Func<MahjongEventNotifier> notifierProvider;
        private readonly Action<string> warnMissingNotifier;
        private bool warnedMissingNotifier;

        public MahjongFlowEventPublisher(MahjongEventNotifier eventNotifier)
            : this(() => eventNotifier, null)
        {
        }

        public MahjongFlowEventPublisher(
            Func<MahjongEventNotifier> notifierProvider,
            Action<string> warnMissingNotifier)
        {
            this.notifierProvider = notifierProvider ?? throw new ArgumentNullException(nameof(notifierProvider));
            this.warnMissingNotifier = warnMissingNotifier;
        }

        public void NotifyRunStarted()
        {
            MahjongEventNotifier notifier = notifierProvider();
            if (notifier == null)
            {
                WarnMissingNotifierOnce();
                return;
            }

            notifier.NotifyRunStarted();
        }

        public void NotifyRoundStarted(int turnIndex, int wallCount)
        {
            notifierProvider()?.NotifyRoundStarted(turnIndex, wallCount);
        }

        public void NotifyRoundResultReady(RoundResult result)
        {
            notifierProvider()?.NotifyRoundResultReady(result);
        }

        public void NotifyRoundResultConfirmed(RoundResult result)
        {
            notifierProvider()?.NotifyRoundResultConfirmed(result);
        }

        public void NotifyGameEnded(RoundResult result)
        {
            notifierProvider()?.NotifyGameEnded(result);
        }

        public void NotifyRoundSetupCompleted()
        {
            notifierProvider()?.NotifyRoundSetupCompleted();
        }

        public void NotifyTurnStarted(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyTurnStarted(seat, turnIndex);
        }

        public void NotifyTileDrawn(DrawResult result)
        {
            notifierProvider()?.NotifyTileDrawn(result);
        }

        public void NotifyTileDiscarded(DiscardRecord record)
        {
            notifierProvider()?.NotifyTileDiscarded(record);
        }

        public void NotifyReactionWindowStarted(ReactionWindow reactionWindow)
        {
            notifierProvider()?.NotifyReactionWindowStarted(reactionWindow);
        }

        public void NotifyReactionWindowAnswered(ReactionWindowAnswerResult result)
        {
            notifierProvider()?.NotifyReactionWindowAnswered(result);
        }

        public void NotifyReactionWindowResolved(ReactionWindowResolution resolution)
        {
            notifierProvider()?.NotifyReactionWindowResolved(resolution);
        }

        public void NotifyReactionWindowClosed(int windowId)
        {
            notifierProvider()?.NotifyReactionWindowClosed(windowId);
        }

        public void NotifyMeldDeclared(PlayerMeld meld)
        {
            notifierProvider()?.NotifyMeldDeclared(meld);
        }

        public void NotifySelfKanDecisionStarted(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifySelfKanDecisionStarted(seat, turnIndex);
        }

        public void NotifySkillActivated(SeatId actorSeat, ActiveSkillEffect effect)
        {
            notifierProvider()?.NotifySkillActivated(actorSeat, effect);
        }

        public void NotifySkillActivatedDetailed(
            SeatId actorSeat,
            ActiveSkillEffect effect,
            bool beforeDraw)
        {
            notifierProvider()?.NotifySkillActivatedDetailed(actorSeat, effect, beforeDraw);
        }

        public void NotifySkillEffectRegistered(ActiveSkillEffect effect)
        {
            notifierProvider()?.NotifySkillEffectRegistered(effect);
        }

        public void NotifySkillEffectResolved(DrawResult result)
        {
            notifierProvider()?.NotifySkillEffectResolved(result);
        }

        public void NotifySkillEffectExpired(ActiveSkillEffect effect, string reason)
        {
            notifierProvider()?.NotifySkillEffectExpired(effect, reason);
        }

        public void NotifyWinChecked(SeatId seat, int turnIndex, bool isWin)
        {
            notifierProvider()?.NotifyWinChecked(seat, turnIndex, isWin);
        }

        public void NotifyWinCheckedDetailed(
            SeatId seat,
            WinType winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            bool isWin)
        {
            notifierProvider()?.NotifyWinCheckedDetailed(
                seat,
                winType,
                winningTile,
                sourceSeat,
                turnIndex,
                isWin);
        }

        public void NotifyWinDeclared(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyWinDeclared(seat, turnIndex);
        }

        public void NotifyWinDeclaredDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            notifierProvider()?.NotifyWinDeclaredDetailed(seat, winType, turnIndex);
        }

        public void NotifyWinDeclaredEvaluated(
            SeatId seat,
            WinType? winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            WinDeclarationEvaluationResult evaluationResult)
        {
            notifierProvider()?.NotifyWinDeclaredEvaluated(
                seat,
                winType,
                winningTile,
                sourceSeat,
                turnIndex,
                evaluationResult);
        }

        public void NotifyWinDeclined(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyWinDeclined(seat, turnIndex);
        }

        public void NotifyWinDeclinedDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            notifierProvider()?.NotifyWinDeclinedDetailed(seat, winType, turnIndex);
        }

        public void NotifyReachDecisionStarted(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyReachDecisionStarted(seat, turnIndex);
        }

        public void NotifyReachDiscardSelectionStarted(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyReachDiscardSelectionStarted(seat, turnIndex);
        }

        public void NotifyReachDiscardSelectionCanceled(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyReachDiscardSelectionCanceled(seat, turnIndex);
        }

        public void NotifyReachDeclared(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyReachDeclared(seat, turnIndex);
        }

        public void NotifyReachDeclined(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyReachDeclined(seat, turnIndex);
        }

        public void NotifyHandAutoSorted(SeatId seat, int turnIndex)
        {
            notifierProvider()?.NotifyHandAutoSorted(seat, turnIndex);
        }

        public void NotifyHandAutoSortedDetailed(SeatId seat, int turnIndex, string reason)
        {
            notifierProvider()?.NotifyHandAutoSortedDetailed(seat, turnIndex, reason);
        }

        public void NotifySeatSlotsAssigned()
        {
            notifierProvider()?.NotifySeatSlotsAssigned();
        }

        public void NotifyTurnDebug(
            string eventName,
            string message,
            SeatId? seat = null,
            Tile? tile = null,
            int? turnIndex = null)
        {
            notifierProvider()?.NotifyTurnDebug(eventName, message, seat, tile, turnIndex);
        }

        public void NotifyRoundEnded(string reason)
        {
            notifierProvider()?.NotifyRoundEnded(reason);
        }

        public void NotifySkillReserved(PendingSkillReservation reservation)
        {
            notifierProvider()?.NotifySkillReserved(reservation);
        }

        public void NotifySkillReservationConsumed(PendingSkillReservation reservation)
        {
            notifierProvider()?.NotifySkillReservationConsumed(reservation);
        }

        public void NotifySkillReservationRejected(
            SeatId ownerSeat,
            SkillEffectKind skillEffectKind,
            Tile targetTile,
            string reason)
        {
            notifierProvider()?.NotifySkillReservationRejected(
                ownerSeat,
                skillEffectKind,
                targetTile,
                reason);
        }

        public void NotifyAutoSortChanged(bool enabled)
        {
            notifierProvider()?.NotifyAutoSortChanged(enabled);
        }

        private void WarnMissingNotifierOnce()
        {
            if (warnedMissingNotifier)
                return;

            warnedMissingNotifier = true;
            warnMissingNotifier?.Invoke("MahjongEventNotifier is not assigned.");
        }
    }
}
