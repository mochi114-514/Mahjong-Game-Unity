using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class MeldCallService
    {
        private readonly PonService ponService;
        private readonly ChiService chiService;

        public MeldCallService()
            : this(new PonService(), new ChiService())
        {
        }

        public MeldCallService(PonService ponService, ChiService chiService)
        {
            this.ponService = ponService ?? throw new ArgumentNullException(nameof(ponService));
            this.chiService = chiService ?? throw new ArgumentNullException(nameof(chiService));
        }

        public IReadOnlyList<ReactionWindowCandidate> CollectCandidates(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            AddCandidates(candidates, ponService.CollectCandidates(gameState, sourceDiscard));
            AddCandidates(candidates, chiService.CollectCandidates(gameState, sourceDiscard));
            return candidates;
        }

        public IReadOnlyList<MeldCallKind> GetAvailableKinds(
            ReactionWindow reactionWindow,
            SeatId seat)
        {
            List<MeldCallKind> kinds = new List<MeldCallKind>();
            if (reactionWindow == null || reactionWindow.PendingRonCandidate != null)
                return kinds;

            bool hasAnyPendingPon = HasPendingCandidate(reactionWindow, ReactionKind.Pon);
            bool hasSeatPendingPon = HasPendingCandidate(
                reactionWindow,
                seat,
                ReactionKind.Pon);
            if (hasSeatPendingPon)
                kinds.Add(MeldCallKind.Pon);

            if (HasPendingCandidate(reactionWindow, seat, ReactionKind.Chi) &&
                (!hasAnyPendingPon || hasSeatPendingPon))
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
                gameState.CurrentReactionWindow != reactionWindow)
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
            else
            {
                prepared = chiService.TryPrepareDeclaration(
                    gameState,
                    reactionWindow,
                    candidate,
                    chiOptionId,
                    out preparedCall,
                    out reason);
            }
            if (!prepared)
                return MeldCallDeclarationResult.Rejected(reason);

            if (!TryCommitPreparedCall(gameState, preparedCall, out reason))
                return MeldCallDeclarationResult.Rejected(reason);

            candidate.Declare();
            reactionWindow.CloseMeldCallsExcept(candidate);
            return MeldCallDeclarationResult.Succeeded(kind, candidate, preparedCall.OpenMeld);
        }

        internal static bool TryCommitPreparedCall(
            MahjongGameState gameState,
            PreparedMeldCall preparedCall,
            out string reason)
        {
            reason = string.Empty;
            if (gameState == null || preparedCall == null || preparedCall.Candidate == null ||
                preparedCall.OpenMeld == null || preparedCall.HandTiles == null)
            {
                reason = "MeldCallCandidateMissing";
                return false;
            }

            OpenMeld openMeld = preparedCall.OpenMeld;
            if (!gameState.CanClaimDiscard(
                    openMeld.SourceDiscardId,
                    openMeld.CallerSeat,
                    openMeld.SourceSeat,
                    openMeld.CalledTile))
            {
                reason = "MeldCallStateChanged";
                return false;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(preparedCall.Candidate.Seat);
            if (!playerSeat.Hand.TryRemoveTilesByValue(preparedCall.HandTiles))
            {
                reason = "MeldCallTilesMissing";
                return false;
            }

            playerSeat.AddOpenMeld(openMeld);
            if (!gameState.TryClaimDiscard(openMeld))
            {
                throw new InvalidOperationException(
                    "A validated meld discard claim could not be recorded.");
            }

            gameState.MarkCallOccurred();
            return true;
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
    }

    internal sealed class PreparedMeldCall
    {
        public PreparedMeldCall(
            ReactionWindowCandidate candidate,
            IReadOnlyList<Tile> handTiles,
            OpenMeld openMeld)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (handTiles == null)
                throw new ArgumentNullException(nameof(handTiles));

            List<Tile> copiedHandTiles = new List<Tile>(handTiles.Count);
            for (int i = 0; i < handTiles.Count; i++)
                copiedHandTiles.Add(handTiles[i]);

            HandTiles = copiedHandTiles.AsReadOnly();
            OpenMeld = openMeld ?? throw new ArgumentNullException(nameof(openMeld));
        }

        public ReactionWindowCandidate Candidate { get; }
        public IReadOnlyList<Tile> HandTiles { get; }
        public OpenMeld OpenMeld { get; }
    }

    public readonly struct MeldCallDeclarationResult
    {
        private MeldCallDeclarationResult(
            bool declared,
            MeldCallKind kind,
            ReactionWindowCandidate candidate,
            OpenMeld openMeld,
            string reason)
        {
            Declared = declared;
            Kind = kind;
            Candidate = candidate;
            OpenMeld = openMeld;
            Reason = reason ?? string.Empty;
        }

        public static MeldCallDeclarationResult Succeeded(
            MeldCallKind kind,
            ReactionWindowCandidate candidate,
            OpenMeld openMeld)
        {
            return new MeldCallDeclarationResult(true, kind, candidate, openMeld, string.Empty);
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
        public OpenMeld OpenMeld { get; }
        public string Reason { get; }
    }
}
