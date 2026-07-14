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

        internal bool TryPrepareDeclaration(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            ReactionWindowCandidate candidate,
            out PreparedMeldCall preparedCall,
            out string reason)
        {
            preparedCall = null;
            reason = string.Empty;
            if (gameState == null || reactionWindow == null || candidate == null ||
                candidate.Kind != ReactionKind.Pon || candidate.PonDetail == null)
            {
                reason = "PonCandidateMissing";
                return false;
            }

            DiscardRecord sourceDiscard = reactionWindow.SourceDiscard;
            if (candidate.PonDetail.CalledTile != sourceDiscard.Tile ||
                !CanPon(gameState, candidate.Seat, sourceDiscard))
            {
                reason = "PonStateChanged";
                return false;
            }

            Tile[] handTiles =
            {
                sourceDiscard.Tile,
                sourceDiscard.Tile
            };
            PlayerMeld meld = PlayerMeld.CreatePon(
                new[] { sourceDiscard.Tile, sourceDiscard.Tile, sourceDiscard.Tile },
                candidate.Seat,
                sourceDiscard.ActorSeat,
                sourceDiscard.Tile,
                sourceDiscard.Id);
            preparedCall = new PreparedMeldCall(candidate, handTiles, meld);
            return true;
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

}
