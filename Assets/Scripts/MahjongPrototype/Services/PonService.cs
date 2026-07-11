using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class PonService
    {
        private const int RequiredMatchingTileCount = 2;

        public IReadOnlyList<ReactionWindowCandidate> CollectCandidates(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            if (gameState == null || sourceDiscard.IsLastLiveWallDiscard)
                return candidates;

            IReadOnlyList<SeatId> activeSeats = gameState.ActiveTurnSeats;
            for (int i = 0; i < activeSeats.Count; i++)
            {
                SeatId seat = activeSeats[i];
                if (!CanPon(gameState, seat, sourceDiscard))
                    continue;

                candidates.Add(ReactionWindowCandidate.CreatePon(seat, sourceDiscard.Tile));
            }

            return candidates;
        }

        public PonDeclarationResult TryDeclare(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            ReactionWindowCandidate candidate)
        {
            if (gameState == null || reactionWindow == null || candidate == null ||
                candidate.Kind != ReactionKind.Pon || candidate.PonDetail == null)
            {
                return PonDeclarationResult.Rejected("PonCandidateMissing");
            }

            DiscardRecord sourceDiscard = reactionWindow.SourceDiscard;
            if (candidate.PonDetail.CalledTile != sourceDiscard.Tile ||
                !CanPon(gameState, candidate.Seat, sourceDiscard) ||
                !gameState.CanClaimDiscard(
                    sourceDiscard.Id,
                    candidate.Seat,
                    sourceDiscard.ActorSeat,
                    sourceDiscard.Tile))
            {
                return PonDeclarationResult.Rejected("PonStateChanged");
            }

            OpenMeld openMeld = new OpenMeld(
                OpenMeldType.Pon,
                new[] { sourceDiscard.Tile, sourceDiscard.Tile, sourceDiscard.Tile },
                candidate.Seat,
                sourceDiscard.ActorSeat,
                sourceDiscard.Tile,
                sourceDiscard.Id);
            PlayerSeat playerSeat = gameState.GetPlayerSeat(candidate.Seat);
            if (!playerSeat.Hand.TryRemoveTilesByValue(
                    sourceDiscard.Tile,
                    RequiredMatchingTileCount))
            {
                return PonDeclarationResult.Rejected("PonTilesMissing");
            }

            playerSeat.AddOpenMeld(openMeld);
            if (!gameState.TryClaimDiscard(openMeld))
            {
                throw new InvalidOperationException(
                    "A validated pon discard claim could not be recorded.");
            }

            return PonDeclarationResult.Succeeded(openMeld);
        }

        private static bool CanPon(
            MahjongGameState gameState,
            SeatId seat,
            DiscardRecord sourceDiscard)
        {
            if (gameState == null || seat == sourceDiscard.ActorSeat ||
                sourceDiscard.IsLastLiveWallDiscard || !sourceDiscard.Tile.IsValid)
            {
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            if (!slot.HasPlayer || slot.ParticipantType != ParticipantType.LocalHuman)
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.IsReachDeclared || playerSeat.HasDrawnTile)
                return false;

            int matchingTiles = 0;
            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            for (int i = 0; i < handTiles.Count; i++)
            {
                if (handTiles[i] == sourceDiscard.Tile)
                    matchingTiles++;
            }

            return matchingTiles >= RequiredMatchingTileCount;
        }
    }

    public readonly struct PonDeclarationResult
    {
        private PonDeclarationResult(bool declared, OpenMeld openMeld, string reason)
        {
            Declared = declared;
            OpenMeld = openMeld;
            Reason = reason ?? string.Empty;
        }

        public static PonDeclarationResult Succeeded(OpenMeld openMeld)
        {
            return new PonDeclarationResult(true, openMeld, string.Empty);
        }

        public static PonDeclarationResult Rejected(string reason)
        {
            return new PonDeclarationResult(false, null, reason);
        }

        public bool Declared { get; }
        public OpenMeld OpenMeld { get; }
        public string Reason { get; }
    }
}
