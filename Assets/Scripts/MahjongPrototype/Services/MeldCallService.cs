using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class MeldCallService
    {
        private readonly PonService ponService;
        private readonly ChiService chiService;
        private readonly KanService kanService;

        public MeldCallService()
            : this(new PonService(), new ChiService(), new KanService())
        {
        }

        public MeldCallService(PonService ponService, ChiService chiService)
            : this(ponService, chiService, new KanService())
        {
        }

        public MeldCallService(
            PonService ponService,
            ChiService chiService,
            KanService kanService)
        {
            this.ponService = ponService ?? throw new ArgumentNullException(nameof(ponService));
            this.chiService = chiService ?? throw new ArgumentNullException(nameof(chiService));
            this.kanService = kanService ?? throw new ArgumentNullException(nameof(kanService));
        }

        public IReadOnlyList<ReactionWindowCandidate> CollectCandidates(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            AddCandidates(candidates, ponService.CollectCandidates(gameState, sourceDiscard));
            AddCandidates(candidates, kanService.CollectDaiminkanCandidates(gameState, sourceDiscard));
            AddCandidates(candidates, chiService.CollectCandidates(gameState, sourceDiscard));
            return candidates;
        }

        public IReadOnlyList<MeldCallKind> GetAvailableKinds(
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            List<MeldCallKind> kinds = new List<MeldCallKind>();
            if (reactionWindow == null || !reactionWindow.IsAcceptingAnswers ||
                reactionWindow.PendingRonCandidate != null)
                return kinds;

            bool hasAnyPendingPon = HasPendingCandidate(reactionWindow, ReactionKind.Pon);
            bool hasAnyPendingDaiminkan = HasPendingCandidate(
                reactionWindow,
                ReactionKind.Daiminkan);
            bool hasSeatPendingPon = HasPendingCandidate(
                reactionWindow,
                seat,
                ReactionKind.Pon);
            bool hasSeatPendingDaiminkan = HasPendingCandidate(
                reactionWindow,
                seat,
                ReactionKind.Daiminkan);
            if (hasSeatPendingPon)
                kinds.Add(MeldCallKind.Pon);
            if (hasSeatPendingDaiminkan)
                kinds.Add(MeldCallKind.Kan);

            if (HasPendingCandidate(reactionWindow, seat, ReactionKind.Chi) &&
                ((!hasAnyPendingPon && !hasAnyPendingDaiminkan) ||
                    hasSeatPendingPon || hasSeatPendingDaiminkan))
            {
                kinds.Add(MeldCallKind.Chi);
            }

            return kinds;
        }

        public MeldCallDeclarationResult TryDeclare(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            SeatId seat,
            MeldCallKind kind,
            int chiOptionId)
        {
            if (gameState == null || reactionWindow == null ||
                !gameState.IsReactionWindowPending ||
                gameState.CurrentReactionWindow != reactionWindow ||
                !reactionWindow.IsAcceptingAnswers)
            {
                return MeldCallDeclarationResult.Rejected("MeldCallWindowMissing");
            }

            if (!ContainsKind(GetAvailableKinds(reactionWindow, seat), kind))
                return MeldCallDeclarationResult.Rejected("MeldCallKindUnavailable");

            ReactionKind reactionKind;
            switch (kind)
            {
                case MeldCallKind.Pon:
                    reactionKind = ReactionKind.Pon;
                    break;
                case MeldCallKind.Chi:
                    reactionKind = ReactionKind.Chi;
                    break;
                case MeldCallKind.Kan:
                    reactionKind = ReactionKind.Daiminkan;
                    break;
                default:
                    return MeldCallDeclarationResult.Rejected("MeldCallKindUnsupported");
            }

            ReactionWindowCandidate candidate = FindPendingCandidate(
                reactionWindow,
                seat,
                reactionKind);
            if (candidate == null)
                return MeldCallDeclarationResult.Rejected("MeldCallCandidateMissing");

            PreparedMeldCall preparedCall;
            string reason;
            bool prepared;
            if (kind == MeldCallKind.Pon)
            {
                prepared = ponService.TryPrepareDeclaration(
                    gameState,
                    reactionWindow,
                    candidate,
                    out preparedCall,
                    out reason);
            }
            else if (kind == MeldCallKind.Chi)
            {
                prepared = chiService.TryPrepareDeclaration(
                    gameState,
                    reactionWindow,
                    candidate,
                    chiOptionId,
                    out preparedCall,
                    out reason);
            }
            else
            {
                prepared = kanService.TryPrepareDaiminkanDeclaration(
                    gameState,
                    reactionWindow,
                    candidate,
                    out preparedCall,
                    out reason);
            }
            if (!prepared)
                return MeldCallDeclarationResult.Rejected(reason);

            if (!gameState.TryCommitMeldCall(reactionWindow, preparedCall, out reason))
                return MeldCallDeclarationResult.Rejected(reason);

            return MeldCallDeclarationResult.Succeeded(kind, candidate, preparedCall.Meld);
        }

        public MeldCallDeclineResult TryDecline(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            if (gameState == null || reactionWindow == null ||
                !gameState.IsReactionWindowPending ||
                gameState.CurrentReactionWindow != reactionWindow ||
                !reactionWindow.IsAcceptingAnswers)
            {
                return MeldCallDeclineResult.Rejected("MeldCallWindowMissing");
            }

            IReadOnlyList<MeldCallKind> availableKinds = GetAvailableKinds(
                reactionWindow,
                seat);
            if (availableKinds.Count <= 0)
                return MeldCallDeclineResult.Rejected("MeldCallKindUnavailable");

            ReactionWindowCandidate declinedCandidate = null;
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate.Seat != seat || !candidate.IsPending ||
                    !TryGetMeldCallKind(candidate.Kind, out MeldCallKind kind) ||
                    !ContainsKind(availableKinds, kind))
                {
                    continue;
                }

                candidate.Decline();
                declinedCandidate ??= candidate;
            }

            return declinedCandidate != null
                ? MeldCallDeclineResult.Succeeded(declinedCandidate)
                : MeldCallDeclineResult.Rejected("MeldCallCandidateMissing");
        }

        private static void AddCandidates(
            List<ReactionWindowCandidate> destination,
            IReadOnlyList<ReactionWindowCandidate> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    destination.Add(source[i]);
            }
        }

        private static bool ContainsKind(IReadOnlyList<MeldCallKind> kinds, MeldCallKind kind)
        {
            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i] == kind)
                    return true;
            }

            return false;
        }

        private static bool HasPendingCandidate(
            ReactionWindow reactionWindow,
            ReactionKind kind)
        {
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate.Kind == kind && candidate.IsPending)
                    return true;
            }

            return false;
        }

        private static bool HasPendingCandidate(
            ReactionWindow reactionWindow,
            SeatId seat,
            ReactionKind kind)
        {
            return FindPendingCandidate(reactionWindow, seat, kind) != null;
        }

        private static ReactionWindowCandidate FindPendingCandidate(
            ReactionWindow reactionWindow,
            SeatId seat,
            ReactionKind kind)
        {
            for (int i = 0; i < reactionWindow.Candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = reactionWindow.Candidates[i];
                if (candidate.Seat == seat && candidate.Kind == kind && candidate.IsPending)
                    return candidate;
            }

            return null;
        }

        private static bool TryGetMeldCallKind(
            ReactionKind reactionKind,
            out MeldCallKind kind)
        {
            switch (reactionKind)
            {
                case ReactionKind.Pon:
                    kind = MeldCallKind.Pon;
                    return true;
                case ReactionKind.Chi:
                    kind = MeldCallKind.Chi;
                    return true;
                case ReactionKind.Daiminkan:
                    kind = MeldCallKind.Kan;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }
    }

    internal sealed class PreparedMeldCall
    {
        public PreparedMeldCall(
            ReactionWindowCandidate candidate,
            IReadOnlyList<Tile> handTiles,
            PlayerMeld meld)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (handTiles == null)
                throw new ArgumentNullException(nameof(handTiles));

            List<Tile> copiedHandTiles = new List<Tile>(handTiles.Count);
            for (int i = 0; i < handTiles.Count; i++)
                copiedHandTiles.Add(handTiles[i]);

            HandTiles = copiedHandTiles.AsReadOnly();
            Meld = meld ?? throw new ArgumentNullException(nameof(meld));
        }

        public ReactionWindowCandidate Candidate { get; }
        public IReadOnlyList<Tile> HandTiles { get; }
        public PlayerMeld Meld { get; }
    }

    public readonly struct MeldCallDeclarationResult
    {
        private MeldCallDeclarationResult(
            bool declared,
            MeldCallKind kind,
            ReactionWindowCandidate candidate,
            PlayerMeld meld,
            string reason)
        {
            Declared = declared;
            Kind = kind;
            Candidate = candidate;
            Meld = meld;
            Reason = reason ?? string.Empty;
        }

        public static MeldCallDeclarationResult Succeeded(
            MeldCallKind kind,
            ReactionWindowCandidate candidate,
            PlayerMeld meld)
        {
            return new MeldCallDeclarationResult(true, kind, candidate, meld, string.Empty);
        }

        public static MeldCallDeclarationResult Rejected(string reason)
        {
            return new MeldCallDeclarationResult(
                false,
                default,
                null,
                null,
                reason);
        }

        public bool Declared { get; }
        public MeldCallKind Kind { get; }
        public ReactionWindowCandidate Candidate { get; }
        public PlayerMeld Meld { get; }
        public string Reason { get; }
    }

    public readonly struct MeldCallDeclineResult
    {
        private MeldCallDeclineResult(
            bool declined,
            ReactionWindowCandidate candidate,
            string reason)
        {
            Declined = declined;
            Candidate = candidate;
            Reason = reason ?? string.Empty;
        }

        public static MeldCallDeclineResult Succeeded(ReactionWindowCandidate candidate)
        {
            return new MeldCallDeclineResult(true, candidate, string.Empty);
        }

        public static MeldCallDeclineResult Rejected(string reason)
        {
            return new MeldCallDeclineResult(false, null, reason);
        }

        public bool Declined { get; }
        public ReactionWindowCandidate Candidate { get; }
        public string Reason { get; }
    }
}
