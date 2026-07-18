using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class KanService
    {
        private const int MaximumStructuralMeldCount = 4;
        private readonly WinChecker winChecker = new WinChecker();

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
            IReadOnlyList<SelfKanCandidate> selfKanCandidates =
                CollectSelfKanCandidates(gameState, seat);
            for (int i = 0; i < selfKanCandidates.Count; i++)
            {
                SelfKanCandidate candidate = selfKanCandidates[i];
                if (candidate.Kind == SelfKanKind.Ankan)
                    candidates.Add(candidate.Tile);
            }

            return candidates;
        }

        public IReadOnlyList<SelfKanCandidate> CollectSelfKanCandidates(
            MahjongGameState gameState,
            SeatId seat)
        {
            List<SelfKanCandidate> candidates = new List<SelfKanCandidate>();
            if (!CanConsiderSelfKan(gameState, seat, out PlayerSeat playerSeat))
                return candidates;

            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            Tile drawnTile = playerSeat.DrawnTile.Value;
            int[] typeCounts = CountLogicalTiles(handTiles, drawnTile);
            for (int typeIndex = 0; typeIndex < typeCounts.Length; typeIndex++)
            {
                if (typeCounts[typeIndex] != 4 ||
                    !TryFindTile(handTiles, drawnTile, typeIndex, out Tile tile,
                        out SelfKanTileLocation location))
                {
                    continue;
                }

                if (HasRoomForMeld(playerSeat) &&
                    (!playerSeat.IsReachDeclared ||
                     IsLegalReachAnkan(playerSeat, tile, drawnTile)))
                {
                    candidates.Add(new SelfKanCandidate(
                        SelfKanKind.Ankan,
                        seat,
                        tile,
                        location,
                        gameState.TurnIndex));
                }
            }

            if (playerSeat.IsReachDeclared)
                return candidates;

            for (int meldIndex = 0; meldIndex < playerSeat.Melds.Count; meldIndex++)
            {
                PlayerMeld meld = playerSeat.Melds[meldIndex];
                if (meld == null || meld.Type != PlayerMeldType.Pon ||
                    !meld.AcquiredTile.HasValue)
                {
                    continue;
                }

                Tile tile = meld.AcquiredTile.Value;
                if (drawnTile == tile)
                {
                    candidates.Add(new SelfKanCandidate(
                        SelfKanKind.Kakan,
                        seat,
                        tile,
                        SelfKanTileLocation.DrawnTile,
                        gameState.TurnIndex,
                        meldIndex,
                        meld));
                }
                else if (playerSeat.Hand.CountTilesByValue(tile) > 0)
                {
                    candidates.Add(new SelfKanCandidate(
                        SelfKanKind.Kakan,
                        seat,
                        tile,
                        SelfKanTileLocation.Hand,
                        gameState.TurnIndex,
                        meldIndex,
                        meld));
                }
            }

            return candidates;
        }

        public bool IsCurrentSelfKanCandidate(
            MahjongGameState gameState,
            SelfKanCandidate candidate)
        {
            if (candidate == null)
                return false;

            IReadOnlyList<SelfKanCandidate> candidates = CollectSelfKanCandidates(
                gameState,
                candidate.Seat);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Matches(candidate))
                    return true;
            }

            return false;
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

            SelfKanCandidate candidate = null;
            IReadOnlyList<SelfKanCandidate> candidates =
                CollectSelfKanCandidates(gameState, seat);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Kind == SelfKanKind.Ankan &&
                    candidates[i].Tile == tile)
                {
                    candidate = candidates[i];
                    break;
                }
            }
            if (candidate == null)
                return AnkanDeclarationResult.Rejected("AnkanStateChanged");

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

        private static bool CanConsiderSelfKan(
            MahjongGameState gameState,
            SeatId seat,
            out PlayerSeat playerSeat)
        {
            playerSeat = null;
            if (gameState == null || gameState.IsRoundEnded ||
                gameState.CurrentTurn != seat ||
                (gameState.TurnPhase != TurnPhase.WaitingForDiscard &&
                 gameState.TurnPhase != TurnPhase.SelfKanDecision) ||
                !gameState.Wall.CanDrawRinshan)
            {
                return false;
            }

            SeatSlot slot = gameState.GetSeatSlot(seat);
            // PROTOTYPE: Self-turn kan input is limited to LocalHuman in this phase.
            if (!slot.HasPlayer || slot.ParticipantType != ParticipantType.LocalHuman)
                return false;

            playerSeat = gameState.GetPlayerSeat(seat);
            if (gameState.IsSelfKanDecisionPending &&
                (gameState.CurrentSelfKanDecision == null ||
                 gameState.CurrentSelfKanDecision.Seat != seat ||
                 gameState.CurrentSelfKanDecision.TurnIndex != gameState.TurnIndex))
            {
                return false;
            }

            return playerSeat.HasDrawnTile;
        }

        private static bool HasRoomForMeld(PlayerSeat playerSeat)
        {
            return PlayerMeldRules.TryGetStructuralMeldCount(
                    playerSeat.Melds,
                    out int meldCount) &&
                meldCount < MaximumStructuralMeldCount;
        }

        private bool IsLegalReachAnkan(
            PlayerSeat playerSeat,
            Tile tile,
            Tile drawnTile)
        {
            if (drawnTile != tile || playerSeat.Hand.CountTilesByValue(tile) != 3 ||
                !HasRoomForMeld(playerSeat))
            {
                return false;
            }

            List<Tile> afterAnkanHand = new List<Tile>(playerSeat.Hand.GetTiles());
            if (!RemoveTiles(afterAnkanHand, tile, 3))
                return false;

            List<PlayerMeld> afterAnkanMelds = new List<PlayerMeld>(playerSeat.Melds)
            {
                PlayerMeld.CreateAnkan(new[] { tile, tile, tile, tile }, playerSeat.SeatId)
            };
            return HasSameWaitSet(
                playerSeat.Hand.GetTiles(),
                playerSeat.Melds,
                afterAnkanHand,
                afterAnkanMelds);
        }

        private bool HasSameWaitSet(
            IReadOnlyList<Tile> beforeHand,
            IReadOnlyList<PlayerMeld> beforeMelds,
            IReadOnlyList<Tile> afterHand,
            IReadOnlyList<PlayerMeld> afterMelds)
        {
            for (int typeIndex = 0; typeIndex < 34; typeIndex++)
            {
                Tile tile = FromTypeIndex(typeIndex);
                if (winChecker.CanWinWithTile(beforeHand, tile, beforeMelds) !=
                    winChecker.CanWinWithTile(afterHand, tile, afterMelds))
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] CountLogicalTiles(
            IReadOnlyList<Tile> handTiles,
            Tile drawnTile)
        {
            int[] typeCounts = new int[34];
            for (int i = 0; i < handTiles.Count; i++)
            {
                int typeIndex = handTiles[i].TypeIndex;
                if (typeIndex >= 0 && typeIndex < typeCounts.Length)
                    typeCounts[typeIndex]++;
            }

            typeCounts[drawnTile.TypeIndex]++;
            return typeCounts;
        }

        private static bool TryFindTile(
            IReadOnlyList<Tile> handTiles,
            Tile drawnTile,
            int typeIndex,
            out Tile tile,
            out SelfKanTileLocation location)
        {
            for (int i = 0; i < handTiles.Count; i++)
            {
                if (handTiles[i].TypeIndex == typeIndex)
                {
                    tile = handTiles[i];
                    location = drawnTile.TypeIndex == typeIndex
                        ? SelfKanTileLocation.DrawnTile
                        : SelfKanTileLocation.Hand;
                    return true;
                }
            }

            if (drawnTile.TypeIndex == typeIndex)
            {
                tile = drawnTile;
                location = SelfKanTileLocation.DrawnTile;
                return true;
            }

            tile = default;
            location = default;
            return false;
        }

        private static bool RemoveTiles(List<Tile> tiles, Tile tile, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int index = tiles.FindIndex(value => value == tile);
                if (index < 0)
                    return false;

                tiles.RemoveAt(index);
            }

            return true;
        }

        private static Tile FromTypeIndex(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= 34)
                return default;
            if (typeIndex < 9)
                return Tile.CreateNumber(TileSuit.Man, typeIndex + 1);
            if (typeIndex < 18)
                return Tile.CreateNumber(TileSuit.Pin, typeIndex - 8);
            if (typeIndex < 27)
                return Tile.CreateNumber(TileSuit.Sou, typeIndex - 17);

            return Tile.CreateHonor((HonorKind)(typeIndex - 26));
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
