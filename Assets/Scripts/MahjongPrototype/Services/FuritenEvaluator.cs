using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class FuritenEvaluator
    {
        private const int BaseHandTileCount = 13;
        private const int TileTypeCount = 34;
        private const int FirstPinTypeIndex = 9;
        private const int FirstSouTypeIndex = 18;
        private const int FirstHonorTypeIndex = 27;

        private readonly WinChecker winChecker;

        public FuritenEvaluator()
            : this(new WinChecker())
        {
        }

        public FuritenEvaluator(WinChecker winChecker)
        {
            this.winChecker = winChecker ?? new WinChecker();
        }

        public FuritenEvaluationResultSet EvaluateAll(MahjongGameState gameState)
        {
            if (gameState == null)
                return FuritenEvaluationResultSet.Empty;

            List<FuritenSeatEvaluationResult> results =
                new List<FuritenSeatEvaluationResult>();
            IReadOnlyList<SeatSlot> seatSlots = gameState.SeatSlots;

            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (!slot.HasPlayer)
                    continue;

                SeatId seat = slot.Wind;
                PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
                results.Add(EvaluateSeat(gameState, seat, playerSeat));
            }

            return new FuritenEvaluationResultSet(results);
        }

        private FuritenSeatEvaluationResult EvaluateSeat(
            MahjongGameState gameState,
            SeatId seat,
            PlayerSeat playerSeat)
        {
            if (playerSeat == null || playerSeat.HasDrawnTile)
                return FuritenSeatEvaluationResult.NotEvaluated(seat);

            IReadOnlyList<Tile> handTiles = playerSeat.Hand.GetTiles();
            if (!TryBuildTypeCounts(handTiles, playerSeat.OpenMelds, out int[] typeCounts))
                return FuritenSeatEvaluationResult.NotEvaluated(seat);

            bool[] selfDiscardedTypeIndices = BuildSelfDiscardedTypeIndices(
                gameState.Discards,
                seat);
            bool isTenpai = false;
            bool isDiscardFuriten = false;

            for (int typeIndex = 0; typeIndex < TileTypeCount; typeIndex++)
            {
                if (typeCounts[typeIndex] >= 4)
                    continue;

                Tile winningTile = CreateTileFromTypeIndex(typeIndex);
                if (!winChecker.CanWinWithTile(
                        handTiles,
                        winningTile,
                        playerSeat.OpenMelds))
                    continue;

                isTenpai = true;
                if (selfDiscardedTypeIndices[typeIndex])
                    isDiscardFuriten = true;
            }

            return FuritenSeatEvaluationResult.Evaluated(
                seat,
                isTenpai,
                isDiscardFuriten,
                playerSeat.IsTemporaryFuriten,
                playerSeat.IsReachPassFuriten);
        }

        private static bool TryBuildTypeCounts(
            IReadOnlyList<Tile> handTiles,
            IReadOnlyList<OpenMeld> openMelds,
            out int[] typeCounts)
        {
            typeCounts = new int[TileTypeCount];

            int openMeldCount = openMelds != null ? openMelds.Count : 0;
            if (handTiles == null || handTiles.Count != BaseHandTileCount - openMeldCount * 3)
                return false;

            for (int i = 0; i < handTiles.Count; i++)
            {
                Tile tile = handTiles[i];
                int typeIndex = tile.TypeIndex;
                if (!tile.IsValid || typeIndex < 0 || typeIndex >= TileTypeCount)
                    return false;

                typeCounts[typeIndex]++;
                if (typeCounts[typeIndex] > 4)
                    return false;
            }

            if (openMelds != null)
            {
                for (int i = 0; i < openMelds.Count; i++)
                {
                    OpenMeld openMeld = openMelds[i];
                    if (openMeld == null)
                        return false;

                    for (int j = 0; j < openMeld.Tiles.Count; j++)
                    {
                        Tile tile = openMeld.Tiles[j];
                        int typeIndex = tile.TypeIndex;
                        if (!tile.IsValid || typeIndex < 0 || typeIndex >= TileTypeCount)
                            return false;

                        typeCounts[typeIndex]++;
                        if (typeCounts[typeIndex] > 4)
                            return false;
                    }
                }
            }

            return true;
        }

        private static bool[] BuildSelfDiscardedTypeIndices(
            IReadOnlyList<DiscardRecord> discards,
            SeatId seat)
        {
            bool[] selfDiscardedTypeIndices = new bool[TileTypeCount];
            if (discards == null)
                return selfDiscardedTypeIndices;

            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord discard = discards[i];
                if (discard.ActorSeat != seat)
                    continue;

                int typeIndex = discard.Tile.TypeIndex;
                if (!discard.Tile.IsValid || typeIndex < 0 || typeIndex >= TileTypeCount)
                    continue;

                selfDiscardedTypeIndices[typeIndex] = true;
            }

            return selfDiscardedTypeIndices;
        }

        private static Tile CreateTileFromTypeIndex(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= TileTypeCount)
                return default;

            if (typeIndex < FirstPinTypeIndex)
                return Tile.CreateNumber(TileSuit.Man, typeIndex + 1);

            if (typeIndex < FirstSouTypeIndex)
                return Tile.CreateNumber(TileSuit.Pin, typeIndex - FirstPinTypeIndex + 1);

            if (typeIndex < FirstHonorTypeIndex)
                return Tile.CreateNumber(TileSuit.Sou, typeIndex - FirstSouTypeIndex + 1);

            switch (typeIndex)
            {
                case 27:
                    return Tile.CreateHonor(HonorKind.East);
                case 28:
                    return Tile.CreateHonor(HonorKind.South);
                case 29:
                    return Tile.CreateHonor(HonorKind.West);
                case 30:
                    return Tile.CreateHonor(HonorKind.North);
                case 31:
                    return Tile.CreateHonor(HonorKind.White);
                case 32:
                    return Tile.CreateHonor(HonorKind.Green);
                case 33:
                    return Tile.CreateHonor(HonorKind.Red);
                default:
                    return default;
            }
        }
    }
}
