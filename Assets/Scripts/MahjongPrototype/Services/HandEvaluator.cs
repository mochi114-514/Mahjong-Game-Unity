using System.Collections.Generic;
using MahjongPrototype.Definitions;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class HandEvaluator
    {
        private readonly YakuDefinitionCatalog catalog;

        public HandEvaluator(YakuDefinitionCatalog catalog)
        {
            this.catalog = catalog;
        }

        public HandEvaluationResult Evaluate(HandEvaluationContext context)
        {
            if (context == null || catalog == null)
                return HandEvaluationResult.Empty;

            List<EvaluatedYaku> yakus = new List<EvaluatedYaku>();
            TryAddYaku(yakus, YakuKind.Reach, context.IsReachDeclared, context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.MenzenTsumo,
                context.WinType == WinType.Tsumo && context.IsClosed,
                context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.SevenPairs,
                context.Shape == WinningHandShape.SevenPairs,
                context.IsClosed);
            TryAddYaku(
                yakus,
                YakuKind.KokushiMusou,
                context.Shape == WinningHandShape.ThirteenOrphans,
                context.IsClosed);
            TryAddYaku(yakus, YakuKind.Tanyao, IsTanyao(context), context.IsClosed);

            if (yakus.Count <= 0)
                return HandEvaluationResult.Empty;

            return new HandEvaluationResult(yakus);
        }

        private void TryAddYaku(
            List<EvaluatedYaku> yakus,
            YakuKind kind,
            bool condition,
            bool isClosed)
        {
            if (!condition || !catalog.TryGet(kind, out YakuDefinition definition))
                return;

            HanValue han = ResolveHan(definition, isClosed);
            if (!definition.IsYakuman && han == HanValue.None)
                return;

            yakus.Add(new EvaluatedYaku(
                definition.Kind,
                definition.DisplayName,
                definition.IsYakuman ? HanValue.None : han,
                definition.IsYakuman));
        }

        private static HanValue ResolveHan(YakuDefinition definition, bool isClosed)
        {
            return isClosed ? definition.ClosedHan : definition.OpenHan;
        }

        private static bool IsTanyao(HandEvaluationContext context)
        {
            if (context.HandTiles == null || !IsSimpleNumberTile(context.WinningTile))
                return false;

            for (int i = 0; i < context.HandTiles.Count; i++)
            {
                if (!IsSimpleNumberTile(context.HandTiles[i]))
                    return false;
            }

            return true;
        }

        private static bool IsSimpleNumberTile(Tile tile)
        {
            return tile.IsNumberTile && tile.Rank >= 2 && tile.Rank <= 8;
        }
    }
}
