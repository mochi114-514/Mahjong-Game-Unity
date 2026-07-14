using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class KanService
    {
        private const int MaximumStructuralMeldCount = 4;

        public IReadOnlyList<ReactionWindowCandidate> CollectDaiminkanCandidates(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            if (gameState == null || sourceDiscard.IsLastLiveWallDiscard ||
                !gameState.Wall.CanDrawRinshan)
            {
                return candidates;
            }

            IReadOnlyList<SeatId> activeSeats = gameState.ActiveTurnSeats;
            for (int i = 0; i < activeSeats.Count; i++)
            {
                SeatId seat = activeSeats[i];
                if (CanDaiminkan(gameState, seat, sourceDiscard))
                {
                    candidates.Add(ReactionWindowCandidate.CreateDaiminkan(
                        seat,
                        sourceDiscard.Tile));
                }
            }

            return candidates;
        }

        public IReadOnlyList<Tile> CollectAnkanCandidates(
            MahjongGameState gameState,
            SeatId seat)
        {
            List<Tile> candidates = new List<Tile>();
            if (!CanConsiderAnkan(gameState, seat, out PlayerSeat playerSeat))
                return candidates;

            int[] typeCounts = new int[34];
            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            for (int i = 0; i < handTiles.Count; i++)
            {
                int typeIndex = handTiles[i].TypeIndex;
                if (typeIndex >= 0 && typeIndex < typeCounts.Length)
                    typeCounts[typeIndex]++;
            }

            Tile drawnTile = playerSeat.DrawnTile.Value;
            typeCounts[drawnTile.TypeIndex]++;
            for (int typeIndex = 0; typeIndex < typeCounts.Length; typeIndex++)
            {
                if (typeCounts[typeIndex] == 4 &&
                    TryFindTile(handTiles, drawnTile, typeIndex, out Tile candidate))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        internal bool TryPrepareDaiminkanDeclaration(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            ReactionWindowCandidate candidate,
            out PreparedMeldCall preparedCall,
            out string reason)
        {
            preparedCall = null;
            reason = string.Empty;
            if (gameState == null || reactionWindow == null || candidate == null ||
                candidate.Kind != ReactionKind.Daiminkan ||
                candidate.DaiminkanDetail == null)
            {
                reason = "DaiminkanCandidateMissing";
                return false;
            }

            DiscardRecord sourceDiscard = reactionWindow.SourceDiscard;
            if (candidate.DaiminkanDetail.CalledTile != sourceDiscard.Tile ||
                !CanDaiminkan(gameState, candidate.Seat, sourceDiscard))
            {
                reason = "DaiminkanStateChanged";
                return false;
            }

            Tile[] handTiles =
            {
                sourceDiscard.Tile,
                sourceDiscard.Tile,
                sourceDiscard.Tile
            };
            PlayerMeld meld = PlayerMeld.CreateDaiminkan(
                new[]
                {
                    sourceDiscard.Tile,
                    sourceDiscard.Tile,
                    sourceDiscard.Tile,
                    sourceDiscard.Tile
                },
                candidate.Seat,
                sourceDiscard.ActorSeat,
                sourceDiscard.Tile,
                sourceDiscard.Id);
            preparedCall = new PreparedMeldCall(candidate, handTiles, meld);
            return true;
        }

        public AnkanDeclarationResult TryDeclareAnkan(
            MahjongGameState gameState,
            SeatId seat,
            Tile tile)
        {
            if (gameState == null)
                return AnkanDeclarationResult.Rejected("GameStateMissing");

            if (!gameState.TryCommitAnkan(seat, tile, out PlayerMeld meld, out string reason))
                return AnkanDeclarationResult.Rejected(reason);

            return AnkanDeclarationResult.Succeeded(meld);
        }

        private static bool CanDaiminkan(
            MahjongGameState gameState,
            SeatId seat,
            DiscardRecord sourceDiscard)
        {
            if (seat == sourceDiscard.ActorSeat || sourceDiscard.IsLastLiveWallDiscard ||
                !sourceDiscard.Tile.IsValid || !gameState.Wall.CanDrawRinshan ||
                !gameState.CanClaimDiscard(
                    sourceDiscard.Id,
                    seat,
                    sourceDiscard.ActorSeat,
                    sourceDiscard.Tile))
            {
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            // PROTOTYPE: Kan response collection is limited to the current LocalHuman path.
            if (!slot.HasPlayer || slot.ParticipantType != ParticipantType.LocalHuman)
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.IsReachDeclared || playerSeat.HasDrawnTile ||
                !HasRoomForMeld(playerSeat))
            {
                return false;
            }

            return playerSeat.Hand.CountTilesByValue(sourceDiscard.Tile) == 3;
        }

        private static bool CanConsiderAnkan(
            MahjongGameState gameState,
            SeatId seat,
            out PlayerSeat playerSeat)
        {
            playerSeat = null;
            if (gameState == null || gameState.IsRoundEnded ||
                gameState.CurrentTurn != seat ||
                gameState.TurnPhase != TurnPhase.WaitingForDiscard ||
                !gameState.Wall.CanDrawRinshan)
            {
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            // PROTOTYPE: Self-turn kan input is limited to LocalHuman in this phase.
            if (!slot.HasPlayer || slot.ParticipantType != ParticipantType.LocalHuman)
                return false;

            playerSeat = gameState.GetPlayerSeat(seat);
            return !playerSeat.IsReachDeclared && playerSeat.HasDrawnTile &&
                HasRoomForMeld(playerSeat);
        }

        private static bool HasRoomForMeld(PlayerSeat playerSeat)
        {
            return PlayerMeldRules.TryGetStructuralMeldCount(
                    playerSeat.Melds,
                    out int meldCount) &&
                meldCount < MaximumStructuralMeldCount;
        }

        private static bool TryFindTile(
            IReadOnlyList<Tile> handTiles,
            Tile drawnTile,
            int typeIndex,
            out Tile tile)
        {
            for (int i = 0; i < handTiles.Count; i++)
            {
                if (handTiles[i].TypeIndex == typeIndex)
                {
                    tile = handTiles[i];
                    return true;
                }
            }

            if (drawnTile.TypeIndex == typeIndex)
            {
                tile = drawnTile;
                return true;
            }

            tile = default;
            return false;
        }

    }

    public readonly struct AnkanDeclarationResult
    {
        private AnkanDeclarationResult(bool declared, PlayerMeld meld, string reason)
        {
            Declared = declared;
            Meld = meld;
            Reason = reason ?? string.Empty;
        }

        public static AnkanDeclarationResult Succeeded(PlayerMeld meld)
        {
            return new AnkanDeclarationResult(true, meld, string.Empty);
        }

        public static AnkanDeclarationResult Rejected(string reason)
        {
            return new AnkanDeclarationResult(false, null, reason);
        }

        public bool Declared { get; }
        public PlayerMeld Meld { get; }
        public string Reason { get; }
    }
}
