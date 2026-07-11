using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class ReactionWindowService
    {
        private readonly WinDecisionService winDecisionService;

        public ReactionWindowService(WinDecisionService winDecisionService)
        {
            this.winDecisionService = winDecisionService ??
                throw new ArgumentNullException(nameof(winDecisionService));
        }

        public ReactionWindowStartResult Begin(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            if (gameState == null)
                return ReactionWindowStartResult.None;

            WinDecisionEvaluation evaluation =
                winDecisionService.EvaluateRon(gameState, sourceDiscard);
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            if (evaluation.RonCandidate.HasValue)
            {
                RonWinCandidate ronCandidate = evaluation.RonCandidate.Value;
                candidates.Add(new ReactionWindowCandidate(
                    ronCandidate.Seat,
                    ReactionKind.Ron,
                    ronCandidate.EvaluationResult));
            }

            ReactionWindow reactionWindow =
                gameState.BeginReactionWindow(sourceDiscard, candidates);
            ReactionWindowResolution resolution = candidates.Count <= 0
                ? ReactionWindowResolution.NoReaction(sourceDiscard)
                : ReactionWindowResolution.None;
            return new ReactionWindowStartResult(
                reactionWindow,
                evaluation.Notifications,
                resolution);
        }

        public ReactionWindowAnswerResult DeclareRon(
            MahjongGameState gameState,
            SeatId seat,
            int windowId)
        {
            if (!TryGetPendingRonCandidate(
                    gameState,
                    seat,
                    windowId,
                    out ReactionWindow reactionWindow,
                    out ReactionWindowCandidate candidate,
                    out string reason))
            {
                return ReactionWindowAnswerResult.Rejected(reason);
            }

            candidate.Declare();
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                candidate,
                ReactionWindowResolution.RonDeclared(
                    reactionWindow.SourceDiscard,
                    candidate));
        }

        public ReactionWindowAnswerResult DeclineRon(
            MahjongGameState gameState,
            SeatId seat,
            int windowId)
        {
            if (!TryGetPendingRonCandidate(
                    gameState,
                    seat,
                    windowId,
                    out ReactionWindow reactionWindow,
                    out ReactionWindowCandidate candidate,
                    out string reason))
            {
                return ReactionWindowAnswerResult.Rejected(reason);
            }

            candidate.Decline();
            winDecisionService.MarkDeclinedRonFuriten(gameState, seat);
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                candidate,
                ReactionWindowResolution.NoReaction(reactionWindow.SourceDiscard));
        }

        private static bool TryGetPendingRonCandidate(
            MahjongGameState gameState,
            SeatId seat,
            int windowId,
            out ReactionWindow reactionWindow,
            out ReactionWindowCandidate candidate,
            out string reason)
        {
            reactionWindow = gameState != null
                ? gameState.CurrentReactionWindow
                : null;
            candidate = null;
            reason = string.Empty;

            if (gameState == null || !gameState.IsReactionWindowPending ||
                reactionWindow == null)
            {
                reason = "ReactionWindowMissing";
                return false;
            }

            if (reactionWindow.WindowId != windowId)
            {
                reason = "ReactionWindowStale";
                return false;
            }

            candidate = reactionWindow.PendingRonCandidate;
            if (candidate == null)
            {
                reason = "RonCandidateMissing";
                return false;
            }

            if (candidate.Seat != seat)
            {
                reason = "NotReactionCandidateSeat";
                return false;
            }

            return true;
        }
    }

    public readonly struct ReactionWindowStartResult
    {
        public static ReactionWindowStartResult None => new ReactionWindowStartResult(
            null,
            Array.Empty<WinCheckNotification>(),
            ReactionWindowResolution.None);

        public ReactionWindowStartResult(
            ReactionWindow reactionWindow,
            IReadOnlyList<WinCheckNotification> winCheckNotifications,
            ReactionWindowResolution resolution)
        {
            ReactionWindow = reactionWindow;
            WinCheckNotifications = winCheckNotifications ??
                Array.Empty<WinCheckNotification>();
            Resolution = resolution;
        }

        public ReactionWindow ReactionWindow { get; }
        public IReadOnlyList<WinCheckNotification> WinCheckNotifications { get; }
        public ReactionWindowResolution Resolution { get; }
    }
}
