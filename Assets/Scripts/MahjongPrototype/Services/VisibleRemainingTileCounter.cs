using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    /// <summary>
    /// Counts tile copies that remain unseen from one local player's perspective.
    /// Opponent concealed hands and every wall snapshot are intentionally excluded.
    /// </summary>
    public sealed class VisibleRemainingTileCounter
    {
        private const int TileTypeCount = 34;
        private const int CopiesPerTileType = 4;

        public int CountVisibleRemaining(
            MahjongGameState gameState,
            SeatId localSeat,
            Tile tile)
        {
            if (gameState == null || !tile.IsValid || !IsOccupiedSeat(gameState, localSeat))
                return 0;

            int[] remainingCounts = BuildVisibleRemainingCounts(gameState, localSeat);
            int typeIndex = tile.TypeIndex;
            return typeIndex >= 0 && typeIndex < remainingCounts.Length
                ? remainingCounts[typeIndex]
                : 0;
        }

        internal int[] BuildVisibleRemainingCounts(
            MahjongGameState gameState,
            SeatId localSeat)
        {
            int[] visibleCounts = new int[TileTypeCount];
            if (gameState == null || !IsOccupiedSeat(gameState, localSeat))
                return visibleCounts;

            PlayerSeat localPlayer = gameState.GetPlayerSeat(localSeat);
            AddTiles(localPlayer.Hand.GetTiles(), visibleCounts);
            if (localPlayer.DrawnTile.HasValue)
                AddTile(localPlayer.DrawnTile.Value, visibleCounts);

            Dictionary<int, Tile> discardedTilesById = new Dictionary<int, Tile>();
            IReadOnlyList<DiscardRecord> discards = gameState.Discards;
            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord discard = discards[i];
                AddTile(discard.Tile, visibleCounts);
                if (discard.Id > 0 && !discardedTilesById.ContainsKey(discard.Id))
                    discardedTilesById.Add(discard.Id, discard.Tile);
            }

            HashSet<int> deduplicatedSourceDiscardIds = new HashSet<int>();
            IReadOnlyList<SeatId> occupiedSeats = gameState.OccupiedSeats;
            for (int i = 0; i < occupiedSeats.Count; i++)
            {
                IReadOnlyList<PlayerMeld> melds = gameState.GetPlayerSeat(occupiedSeats[i]).Melds;
                for (int meldIndex = 0; meldIndex < melds.Count; meldIndex++)
                {
                    PlayerMeld meld = melds[meldIndex];
                    int skippedPhysicalTileIndex = FindDuplicateSourceTileIndex(
                        meld,
                        discardedTilesById,
                        deduplicatedSourceDiscardIds);

                    for (int tileIndex = 0; tileIndex < meld.PhysicalTiles.Count; tileIndex++)
                    {
                        if (tileIndex != skippedPhysicalTileIndex)
                            AddTile(meld.PhysicalTiles[tileIndex], visibleCounts);
                    }
                }
            }

            int[] remainingCounts = new int[TileTypeCount];
            for (int typeIndex = 0; typeIndex < TileTypeCount; typeIndex++)
            {
                remainingCounts[typeIndex] = Math.Max(
                    0,
                    Math.Min(CopiesPerTileType, CopiesPerTileType - visibleCounts[typeIndex]));
            }

            return remainingCounts;
        }

        private static int FindDuplicateSourceTileIndex(
            PlayerMeld meld,
            IReadOnlyDictionary<int, Tile> discardedTilesById,
            ISet<int> deduplicatedSourceDiscardIds)
        {
            if (meld == null || !meld.HasDiscardSource)
                return -1;

            int sourceDiscardId = meld.SourceDiscardId.Value;
            if (deduplicatedSourceDiscardIds.Contains(sourceDiscardId) ||
                !discardedTilesById.TryGetValue(sourceDiscardId, out Tile sourceTile))
            {
                return -1;
            }

            for (int i = 0; i < meld.PhysicalTiles.Count; i++)
            {
                if (meld.PhysicalTiles[i] != sourceTile)
                    continue;

                deduplicatedSourceDiscardIds.Add(sourceDiscardId);
                return i;
            }

            return -1;
        }

        private static bool IsOccupiedSeat(MahjongGameState gameState, SeatId seat)
        {
            IReadOnlyList<SeatId> occupiedSeats = gameState.OccupiedSeats;
            for (int i = 0; i < occupiedSeats.Count; i++)
            {
                if (occupiedSeats[i] == seat)
                    return true;
            }

            return false;
        }

        private static void AddTiles(IReadOnlyList<Tile> tiles, int[] counts)
        {
            if (tiles == null)
                return;

            for (int i = 0; i < tiles.Count; i++)
                AddTile(tiles[i], counts);
        }

        private static void AddTile(Tile tile, int[] counts)
        {
            int typeIndex = tile.TypeIndex;
            if (tile.IsValid && typeIndex >= 0 && typeIndex < counts.Length)
                counts[typeIndex]++;
        }
    }
}
