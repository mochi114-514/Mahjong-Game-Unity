using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class ReactionWindowService
    {
        private readonly WinDecisionService winDecisionService;
        private readonly PonService ponService;
        private readonly MeldCallService meldCallService;

        public ReactionWindowService(WinDecisionService winDecisionService)
            : this(winDecisionService, new PonService(), new ChiService())
        {
        }

        public ReactionWindowService(
            WinDecisionService winDecisionService,
            PonService ponService)
            : this(winDecisionService, ponService, new ChiService())
        {
        }

        public ReactionWindowService(
            WinDecisionService winDecisionService,
            PonService ponService,
            ChiService chiService)
        {
            this.winDecisionService = winDecisionService ??
                throw new ArgumentNullException(nameof(winDecisionService));
            this.ponService = ponService ?? throw new ArgumentNullException(nameof(ponService));
            meldCallService = new MeldCallService(
                this.ponService,
                chiService ?? throw new ArgumentNullException(nameof(chiService)));
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

            IReadOnlyList<ReactionWindowCandidate> ponCandidates =
                ponService.CollectCandidates(gameState, sourceDiscard);
            for (int i = 0; i < ponCandidates.Count; i++)
                candidates.Add(ponCandidates[i]);

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
            if (!TryGetPendingCandidate(
                    gameState,
                    seat,
                    windowId,
                    ReactionKind.Ron,
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
            if (!TryGetPendingCandidate(
                    gameState,
                    seat,
                    windowId,
                    ReactionKind.Ron,
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
                ResolveIfNoPendingCandidates(reactionWindow));
        }

        public ReactionWindowAnswerResult DeclarePon(
            MahjongGameState gameState,
            SeatId seat,
            int windowId)
        {
            return DeclareCall(
                gameState,
                seat,
                windowId,
                MeldCallKind.Pon,
                0);
        }

        public ReactionWindowAnswerResult DeclareChi(
            MahjongGameState gameState,
            SeatId seat,
            int windowId,
            int optionId)
        {
            return DeclareCall(
                gameState,
                seat,
                windowId,
                MeldCallKind.Chi,
                optionId);
        }

        public ReactionWindowAnswerResult DeclareCall(
            MahjongGameState gameState,
            SeatId seat,
            int windowId,
            MeldCallKind kind,
            int chiOptionId)
        {
            ReactionWindow reactionWindow = gameState != null
                ? gameState.CurrentReactionWindow
                : null;
            if (gameState == null || !gameState.IsReactionWindowPending ||
                reactionWindow == null)
            {
                return ReactionWindowAnswerResult.Rejected("ReactionWindowMissing");
            }

            if (reactionWindow.WindowId != windowId)
                return ReactionWindowAnswerResult.Rejected("ReactionWindowStale");

            MeldCallDeclarationResult result = meldCallService.TryDeclare(
                gameState,
                reactionWindow,
                seat,
                kind,
                chiOptionId);
            if (!result.Declared)
                return ReactionWindowAnswerResult.Rejected(result.Reason);

            ReactionWindowResolution resolution = kind == MeldCallKind.Pon
                ? ReactionWindowResolution.PonDeclared(
                    reactionWindow.SourceDiscard,
                    result.Candidate,
                    result.OpenMeld)
                : ReactionWindowResolution.ChiDeclared(
                    reactionWindow.SourceDiscard,
                    result.Candidate,
                    result.OpenMeld);
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                result.Candidate,
                resolution);
        }

        public ReactionWindowAnswerResult DeclinePon(
            MahjongGameState gameState,
            SeatId seat,
            int windowId)
        {
            if (!TryGetPendingCandidate(
                    gameState,
                    seat,
                    windowId,
                    ReactionKind.Pon,
                    out ReactionWindow reactionWindow,
                    out ReactionWindowCandidate candidate,
                    out string reason))
            {
                return ReactionWindowAnswerResult.Rejected(reason);
            }

            candidate.Decline();
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                candidate,
                ResolveIfNoPendingCandidates(reactionWindow));
        }

        private static ReactionWindowResolution ResolveIfNoPendingCandidates(
            ReactionWindow reactionWindow)
        {
            return reactionWindow.PendingCandidate == null
                ? ReactionWindowResolution.NoReaction(reactionWindow.SourceDiscard)
                : ReactionWindowResolution.Pending(reactionWindow.SourceDiscard);
        }

        private static bool TryGetPendingCandidate(
            MahjongGameState gameState,
            SeatId seat,
            int windowId,
            ReactionKind expectedKind,
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

            candidate = reactionWindow.PendingCandidate;
            if (candidate == null)
            {
                reason = "ReactionCandidateMissing";
                return false;
            }

            if (candidate.Kind != expectedKind)
            {
                reason = "ReactionKindMismatch";
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
