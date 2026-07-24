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
            : this(winDecisionService, new PonService(), new ChiService(), new KanService())
        {
        }

        public ReactionWindowService(
            WinDecisionService winDecisionService,
            PonService ponService)
            : this(winDecisionService, ponService, new ChiService(), new KanService())
        {
        }

        public ReactionWindowService(
            WinDecisionService winDecisionService,
            PonService ponService,
            ChiService chiService)
            : this(winDecisionService, ponService, chiService, new KanService())
        {
        }

        public ReactionWindowService(
            WinDecisionService winDecisionService,
            PonService ponService,
            ChiService chiService,
            KanService kanService)
        {
            this.winDecisionService = winDecisionService ??
                throw new ArgumentNullException(nameof(winDecisionService));
            this.ponService = ponService ?? throw new ArgumentNullException(nameof(ponService));
            meldCallService = new MeldCallService(
                this.ponService,
                chiService ?? throw new ArgumentNullException(nameof(chiService)),
                kanService ?? throw new ArgumentNullException(nameof(kanService)));
        }

        public ReactionWindowStartResult Begin(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            return BeginInternal(gameState, sourceDiscard, false);
        }

        public ReactionWindowStartResult BeginRonOnly(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            return BeginInternal(gameState, sourceDiscard, true);
        }

        private ReactionWindowStartResult BeginInternal(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard,
            bool ronOnly)
        {
            if (gameState == null)
                return ReactionWindowStartResult.None;

            WinDecisionEvaluation evaluation =
                winDecisionService.EvaluateRon(gameState, sourceDiscard);
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            for (int i = 0; i < evaluation.RonCandidates.Count; i++)
            {
                RonWinCandidate ronCandidate = evaluation.RonCandidates[i];
                candidates.Add(new ReactionWindowCandidate(
                    ronCandidate.Seat,
                    ReactionKind.Ron,
                    ronCandidate.EvaluationResult));
            }

            if (!ronOnly)
            {
                IReadOnlyList<ReactionWindowCandidate> meldCallCandidates =
                    meldCallService.CollectCandidates(gameState, sourceDiscard);
                for (int i = 0; i < meldCallCandidates.Count; i++)
                    candidates.Add(meldCallCandidates[i]);
            }

            ReactionWindow reactionWindow =
                gameState.BeginReactionWindow(sourceDiscard, candidates);
            ReactionWindowResolution resolution = candidates.Count <= 0
                ? ReactionWindowResolution.NoReaction(reactionWindow.WindowId, sourceDiscard)
                : ReactionWindowResolution.None;
            BeginResolutionIfNeeded(reactionWindow, resolution);
            return new ReactionWindowStartResult(
                reactionWindow,
                evaluation.Notifications,
                resolution);
        }

        public ReactionWindowStartResult BeginChankan(
            MahjongGameState gameState,
            SelfKanCandidate pendingKakan)
        {
            if (gameState == null || pendingKakan == null ||
                pendingKakan.Kind != SelfKanKind.Kakan)
            {
                return ReactionWindowStartResult.None;
            }

            WinDecisionEvaluation evaluation = winDecisionService.EvaluateRon(
                gameState,
                pendingKakan.Tile,
                pendingKakan.Seat,
                pendingKakan.TurnIndex,
                true);
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            for (int i = 0; i < evaluation.RonCandidates.Count; i++)
            {
                RonWinCandidate ronCandidate = evaluation.RonCandidates[i];
                candidates.Add(new ReactionWindowCandidate(
                    ronCandidate.Seat,
                    ReactionKind.Ron,
                    ronCandidate.EvaluationResult));
            }

            ReactionWindow reactionWindow = gameState.BeginKakanReactionWindow(
                pendingKakan,
                candidates);
            ReactionWindowResolution resolution = candidates.Count <= 0
                ? ReactionWindowResolution.NoReaction(
                    reactionWindow.WindowId,
                    reactionWindow.Source)
                : ReactionWindowResolution.None;
            BeginResolutionIfNeeded(reactionWindow, resolution);
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
            ReactionWindowResolution resolution = ReactionWindowResolution.RonDeclared(
                reactionWindow.WindowId,
                reactionWindow.Source,
                candidate);
            BeginResolutionIfNeeded(reactionWindow, resolution);
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                candidate,
                resolution);
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
            ReactionWindowResolution resolution = ResolveIfNoPendingCandidates(reactionWindow);
            BeginResolutionIfNeeded(reactionWindow, resolution);
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                candidate,
                resolution);
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

        public ReactionWindowAnswerResult DeclareDaiminkan(
            MahjongGameState gameState,
            SeatId seat,
            int windowId)
        {
            return DeclareCall(
                gameState,
                seat,
                windowId,
                MeldCallKind.Kan,
                0);
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
            if (!reactionWindow.IsAcceptingAnswers)
                return ReactionWindowAnswerResult.Rejected("ReactionWindowResolving");

            MeldCallDeclarationResult result = meldCallService.TryDeclare(
                gameState,
                reactionWindow,
                seat,
                kind,
                chiOptionId);
            if (!result.Declared)
                return ReactionWindowAnswerResult.Rejected(result.Reason);

            ReactionWindowResolution resolution;
            switch (kind)
            {
                case MeldCallKind.Pon:
                    resolution = ReactionWindowResolution.PonDeclared(
                        reactionWindow.WindowId,
                        reactionWindow.SourceDiscard,
                        result.Candidate,
                        result.Meld);
                    break;
                case MeldCallKind.Chi:
                    resolution = ReactionWindowResolution.ChiDeclared(
                        reactionWindow.WindowId,
                        reactionWindow.SourceDiscard,
                        result.Candidate,
                        result.Meld);
                    break;
                case MeldCallKind.Kan:
                    resolution = ReactionWindowResolution.DaiminkanDeclared(
                        reactionWindow.WindowId,
                        reactionWindow.SourceDiscard,
                        result.Candidate,
                        result.Meld);
                    break;
                default:
                    return ReactionWindowAnswerResult.Rejected("MeldCallKindUnsupported");
            }
            BeginResolutionIfNeeded(reactionWindow, resolution);
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
            ReactionWindowResolution resolution = ResolveIfNoPendingCandidates(reactionWindow);
            BeginResolutionIfNeeded(reactionWindow, resolution);
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                candidate,
                resolution);
        }

        public ReactionWindowAnswerResult DeclineMeldCalls(
            MahjongGameState gameState,
            SeatId seat,
            int windowId)
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
            if (!reactionWindow.IsAcceptingAnswers)
                return ReactionWindowAnswerResult.Rejected("ReactionWindowResolving");

            MeldCallDeclineResult result = meldCallService.TryDecline(
                gameState,
                reactionWindow,
                seat);
            if (!result.Declined)
                return ReactionWindowAnswerResult.Rejected(result.Reason);

            ReactionWindowResolution resolution = ResolveIfNoPendingCandidates(reactionWindow);
            BeginResolutionIfNeeded(reactionWindow, resolution);
            return ReactionWindowAnswerResult.AcceptedAnswer(
                reactionWindow.WindowId,
                result.Candidate,
                resolution);
        }

        private static void BeginResolutionIfNeeded(
            ReactionWindow reactionWindow,
            ReactionWindowResolution resolution)
        {
            if (resolution.IsResolved)
                reactionWindow.TryBeginResolution();
        }

        private static ReactionWindowResolution ResolveIfNoPendingCandidates(
            ReactionWindow reactionWindow)
        {
            return reactionWindow.PendingCandidate == null
                ? ReactionWindowResolution.NoReaction(
                    reactionWindow.WindowId,
                    reactionWindow.Source)
                : ReactionWindowResolution.Pending(
                    reactionWindow.WindowId,
                    reactionWindow.Source);
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

            if (!reactionWindow.IsAcceptingAnswers)
            {
                reason = "ReactionWindowResolving";
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
