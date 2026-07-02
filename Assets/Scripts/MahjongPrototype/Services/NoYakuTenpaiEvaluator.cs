using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class NoYakuTenpaiEvaluator
    {
        private const int RequiredHandTileCount = 13;
        private const int TileTypeCount = 34;
        private const int FirstPinTypeIndex = 9;
        private const int FirstSouTypeIndex = 18;
        private const int FirstHonorTypeIndex = 27;

        private readonly WinDeclarationEvaluator winDeclarationEvaluator;

        public NoYakuTenpaiEvaluator(WinDeclarationEvaluator winDeclarationEvaluator)
        {
            this.winDeclarationEvaluator = winDeclarationEvaluator;
        }

        public NoYakuTenpaiEvaluationResult Evaluate(
            IReadOnlyList<Tile> handTiles,
            SeatId winnerSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed)
        {
            if (winDeclarationEvaluator == null)
                return NoYakuTenpaiEvaluationResult.NotEvaluated;

            if (!TryBuildTypeCounts(handTiles, out int[] typeCounts))
                return NoYakuTenpaiEvaluationResult.NotTenpai;

            bool hasWinningShapeWait = false;
            for (int typeIndex = 0; typeIndex < TileTypeCount; typeIndex++)
            {
                if (typeCounts[typeIndex] >= 4)
                    continue;

                Tile winningTile = CreateTileFromTypeIndex(typeIndex);
                WinDeclarationEvaluationResult result =
                    winDeclarationEvaluator.EvaluateWithTile(
                        new WinDeclarationEvaluationContext(
                            handTiles,
                            winningTile,
                            WinType.Ron,
                            winnerSeat,
                            null,
                            roundWind,
                            seatWind,
                            isReachDeclared,
                            isClosed));

                if (!result.IsWinningShape)
                    continue;

                hasWinningShapeWait = true;
                if (result.HasYaku)
                    return NoYakuTenpaiEvaluationResult.Tenpai(true);
            }

            return hasWinningShapeWait
                ? NoYakuTenpaiEvaluationResult.Tenpai(false)
                : NoYakuTenpaiEvaluationResult.NotTenpai;
        }

        private static bool TryBuildTypeCounts(IReadOnlyList<Tile> handTiles, out int[] typeCounts)
        {
            typeCounts = new int[TileTypeCount];

            if (handTiles == null || handTiles.Count != RequiredHandTileCount)
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

            return true;
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
