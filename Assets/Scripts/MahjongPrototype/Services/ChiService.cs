using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class ChiService
    {
        private readonly TurnOrderService turnOrderService;

        public ChiService()
            : this(new TurnOrderService())
        {
        }

        public ChiService(TurnOrderService turnOrderService)
        {
            this.turnOrderService = turnOrderService ??
                throw new ArgumentNullException(nameof(turnOrderService));
        }

        public IReadOnlyList<ReactionWindowCandidate> CollectCandidates(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard)
        {
            List<ReactionWindowCandidate> candidates =
                new List<ReactionWindowCandidate>();
            if (gameState == null || sourceDiscard.IsLastLiveWallDiscard ||
                !sourceDiscard.Tile.IsNumberTile)
            {
                return candidates;
            }

            IReadOnlyList<SeatId> activeSeats = gameState.ActiveTurnSeats;
            if (activeSeats == null || activeSeats.Count < 2 ||
                !ContainsSeat(activeSeats, sourceDiscard.ActorSeat))
            {
                return candidates;
            }

            SeatId eligibleSeat = turnOrderService.GetNextSeat(
                activeSeats,
                sourceDiscard.ActorSeat);
            if (eligibleSeat == sourceDiscard.ActorSeat ||
                !CanChi(gameState, eligibleSeat))
            {
                return candidates;
            }

            IReadOnlyList<ChiOption> options = CollectOptions(
                gameState.GetPlayerSeat(eligibleSeat),
                sourceDiscard.Tile);
            if (options.Count <= 0)
                return candidates;

            candidates.Add(ReactionWindowCandidate.CreateChi(
                eligibleSeat,
                sourceDiscard.Tile,
                options));
            return candidates;
        }

        internal bool TryPrepareDeclaration(
            MahjongGameState gameState,
            ReactionWindow reactionWindow,
            ReactionWindowCandidate candidate,
            int optionId,
            out PreparedMeldCall preparedCall,
            out string reason)
        {
            preparedCall = null;
            reason = string.Empty;
            if (gameState == null || reactionWindow == null || candidate == null ||
                candidate.Kind != ReactionKind.Chi || candidate.ChiDetail == null)
            {
                reason = "ChiCandidateMissing";
                return false;
            }

            DiscardRecord sourceDiscard = reactionWindow.SourceDiscard;
            if (candidate.ChiDetail.CalledTile != sourceDiscard.Tile)
            {
                reason = "ChiStateChanged";
                return false;
            }

            ReactionWindowCandidate currentCandidate = FindCurrentCandidate(
                gameState,
                sourceDiscard,
                candidate.Seat);
            if (currentCandidate == null || currentCandidate.ChiDetail == null)
            {
                reason = "ChiStateChanged";
                return false;
            }

            ChiOption option = FindOption(candidate.ChiDetail.Options, optionId);
            ChiOption currentOption = FindOption(currentCandidate.ChiDetail.Options, optionId);
            if (option == null || currentOption == null ||
                !HasSameTiles(option.HandTiles, currentOption.HandTiles) ||
                !HasSameTiles(option.MeldTiles, currentOption.MeldTiles))
            {
                reason = "ChiOptionMissing";
                return false;
            }

            PlayerMeld meld = PlayerMeld.CreateChi(
                option.MeldTiles,
                candidate.Seat,
                sourceDiscard.ActorSeat,
                sourceDiscard.Tile,
                sourceDiscard.Id);
            preparedCall = new PreparedMeldCall(candidate, option.HandTiles, meld);
            return true;
        }

        private static bool CanChi(MahjongGameState gameState, SeatId seat)
        {
            SeatSlot slot = gameState.GetSeatSlot(seat);
            if (!slot.HasPlayer || slot.ParticipantType != ParticipantType.LocalHuman)
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            return !playerSeat.IsReachDeclared && !playerSeat.HasDrawnTile;
        }

        private static IReadOnlyList<ChiOption> CollectOptions(
            PlayerSeat playerSeat,
            Tile calledTile)
        {
            List<ChiOption> options = new List<ChiOption>();
            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            for (int meldStartRank = calledTile.Rank - 2;
                 meldStartRank <= calledTile.Rank;
                 meldStartRank++)
            {
                if (meldStartRank < 1 || meldStartRank + 2 > 9)
                    continue;

                Tile[] meldTiles = new Tile[3];
                Tile[] requiredHandTiles = new Tile[2];
                int handTileIndex = 0;
                for (int rankOffset = 0; rankOffset < meldTiles.Length; rankOffset++)
                {
                    Tile tile = Tile.CreateNumber(
                        calledTile.Suit,
                        meldStartRank + rankOffset);
                    meldTiles[rankOffset] = tile;
                    if (tile != calledTile)
                        requiredHandTiles[handTileIndex++] = tile;
                }

                if (!ContainsTile(handTiles, requiredHandTiles[0]) ||
                    !ContainsTile(handTiles, requiredHandTiles[1]))
                {
                    continue;
                }

                options.Add(new ChiOption(
                    meldStartRank,
                    calledTile,
                    requiredHandTiles,
                    meldTiles));
            }

            return options;
        }

        private static bool ContainsSeat(IReadOnlyList<SeatId> seats, SeatId seat)
        {
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] == seat)
                    return true;
            }

            return false;
        }

        private static bool ContainsTile(IReadOnlyList<Tile> tiles, Tile targetTile)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == targetTile)
                    return true;
            }

            return false;
        }

        private ReactionWindowCandidate FindCurrentCandidate(
            MahjongGameState gameState,
            DiscardRecord sourceDiscard,
            SeatId seat)
        {
            IReadOnlyList<ReactionWindowCandidate> candidates =
                CollectCandidates(gameState, sourceDiscard);
            for (int i = 0; i < candidates.Count; i++)
            {
                ReactionWindowCandidate candidate = candidates[i];
                if (candidate.Seat == seat)
                    return candidate;
            }

            return null;
        }

        private static ChiOption FindOption(IReadOnlyList<ChiOption> options, int optionId)
        {
            if (options == null)
                return null;

            for (int i = 0; i < options.Count; i++)
            {
                ChiOption option = options[i];
                if (option != null && option.OptionId == optionId)
                    return option;
            }

            return null;
        }

        private static bool HasSameTiles(
            IReadOnlyList<Tile> firstTiles,
            IReadOnlyList<Tile> secondTiles)
        {
            if (firstTiles == null || secondTiles == null || firstTiles.Count != secondTiles.Count)
                return false;

            for (int i = 0; i < firstTiles.Count; i++)
            {
                if (firstTiles[i] != secondTiles[i])
                    return false;
            }

            return true;
        }
    }
}
