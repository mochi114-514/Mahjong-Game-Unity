using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class NineTerminalsAndHonorsEvaluator
    {
        private const int RequiredDistinctTileTypeCount = 9;
        private const int TileTypeCount = 34;

        public bool CanDeclare(
            PlayerSeat playerSeat,
            IReadOnlyList<DiscardRecord> discards,
            bool hasCallOccurred)
        {
            if (playerSeat == null || !playerSeat.HasDrawnTile || hasCallOccurred)
                return false;
            if (HasDiscarded(discards, playerSeat.SeatId))
                return false;

            bool[] countedTileTypes = new bool[TileTypeCount];
            int distinctTileTypeCount = 0;
            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            for (int i = 0; i < handTiles.Count; i++)
            {
                if (!TryCountTerminalOrHonor(
                        handTiles[i],
                        countedTileTypes,
                        ref distinctTileTypeCount))
                {
                    continue;
                }

                if (distinctTileTypeCount >= RequiredDistinctTileTypeCount)
                    return true;
            }

            TryCountTerminalOrHonor(
                playerSeat.DrawnTile.Value,
                countedTileTypes,
                ref distinctTileTypeCount);
            return distinctTileTypeCount >= RequiredDistinctTileTypeCount;
        }

        private static bool HasDiscarded(
            IReadOnlyList<DiscardRecord> discards,
            SeatId seat)
        {
            if (discards == null)
                return false;

            for (int i = 0; i < discards.Count; i++)
            {
                if (discards[i].ActorSeat == seat)
                    return true;
            }

            return false;
        }

        private static bool TryCountTerminalOrHonor(
            Tile tile,
            bool[] countedTileTypes,
            ref int distinctTileTypeCount)
        {
            if (!IsTerminalOrHonor(tile))
                return false;

            int typeIndex = tile.TypeIndex;
            if (typeIndex < 0 || typeIndex >= countedTileTypes.Length ||
                countedTileTypes[typeIndex])
            {
                return false;
            }

            countedTileTypes[typeIndex] = true;
            distinctTileTypeCount++;
            return true;
        }

        private static bool IsTerminalOrHonor(Tile tile)
        {
            return tile.IsHonorTile ||
                (tile.IsNumberTile && (tile.Rank == 1 || tile.Rank == 9));
        }
    }
}
