using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    /// <summary>
    /// Determines whether the latest discard can complete the four-kans abortive draw.
    /// The caller remains responsible for resolving reactions to that discard first.
    /// </summary>
    public sealed class FourKansEvaluator
    {
        private const int RequiredPlayerCount = 4;
        private const int RequiredKanCount = 4;

        public bool IsSatisfied(
            IReadOnlyList<SeatId> activeSeats,
            IReadOnlyList<PlayerSeat> playerSeats,
            IReadOnlyList<DiscardRecord> discards,
            DiscardRecord? resolvedDiscard,
            int remainingRinshanTileCount)
        {
            if (activeSeats == null ||
                activeSeats.Count != RequiredPlayerCount ||
                playerSeats == null ||
                playerSeats.Count != RequiredPlayerCount ||
                discards == null ||
                discards.Count <= 0 ||
                !resolvedDiscard.HasValue ||
                remainingRinshanTileCount != 0)
            {
                return false;
            }

            HashSet<SeatId> activeSeatSet = new HashSet<SeatId>(activeSeats);
            if (activeSeatSet.Count != RequiredPlayerCount)
                return false;

            Dictionary<SeatId, PlayerSeat> playerSeatsBySeat =
                new Dictionary<SeatId, PlayerSeat>();
            for (int i = 0; i < playerSeats.Count; i++)
            {
                PlayerSeat playerSeat = playerSeats[i];
                if (playerSeat == null ||
                    !activeSeatSet.Contains(playerSeat.SeatId) ||
                    playerSeatsBySeat.ContainsKey(playerSeat.SeatId))
                {
                    return false;
                }

                playerSeatsBySeat.Add(playerSeat.SeatId, playerSeat);
            }

            if (playerSeatsBySeat.Count != RequiredPlayerCount)
                return false;

            DiscardRecord discard = resolvedDiscard.Value;
            if (discard.Id <= 0 ||
                !discard.Tile.IsValid ||
                !IsSameDiscard(discard, discards[discards.Count - 1]) ||
                !playerSeatsBySeat.TryGetValue(discard.ActorSeat, out PlayerSeat actorSeat))
            {
                return false;
            }

            int kanCount = 0;
            int kanOwnerCount = 0;
            bool actorOwnsKan = false;
            foreach (SeatId activeSeat in activeSeatSet)
            {
                if (!playerSeatsBySeat.TryGetValue(activeSeat, out PlayerSeat playerSeat) ||
                    playerSeat.Melds == null)
                {
                    return false;
                }

                int playerKanCount = 0;
                for (int i = 0; i < playerSeat.Melds.Count; i++)
                {
                    PlayerMeld meld = playerSeat.Melds[i];
                    if (meld == null || meld.OwnerSeat != playerSeat.SeatId)
                        return false;
                    if (!meld.IsKan)
                        continue;

                    playerKanCount++;
                    kanCount++;
                }

                if (playerKanCount <= 0)
                    continue;

                kanOwnerCount++;
                if (playerSeat.SeatId == actorSeat.SeatId)
                    actorOwnsKan = true;
            }

            return kanCount == RequiredKanCount &&
                kanOwnerCount >= 2 &&
                actorOwnsKan;
        }

        private static bool IsSameDiscard(DiscardRecord left, DiscardRecord right)
        {
            return left.Id == right.Id &&
                left.ActorSeat == right.ActorSeat &&
                left.Tile == right.Tile &&
                left.TurnIndex == right.TurnIndex &&
                left.Source == right.Source &&
                left.IsLastLiveWallDiscard == right.IsLastLiveWallDiscard;
        }
    }
}
