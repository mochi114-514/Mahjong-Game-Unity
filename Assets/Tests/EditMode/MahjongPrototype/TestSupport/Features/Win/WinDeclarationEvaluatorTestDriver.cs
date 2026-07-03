using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinDeclarationEvaluatorTestDriver : IDisposable
    {
        private readonly WinEvaluationTestSupport support;
        private bool disposed;

        private WinDeclarationEvaluatorTestDriver(WinEvaluationTestSupport support)
        {
            this.support = support;
        }

        public static WinDeclarationEvaluatorTestDriver Create()
        {
            return new WinDeclarationEvaluatorTestDriver(WinEvaluationTestSupport.Create());
        }

        public object CreateCatalog(params object[] definitions)
        {
            return support.CreateYakuCatalog(definitions);
        }

        public object CreateDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName,
            bool isYakuman = false)
        {
            return support.CreateYakuDefinition(
                yakuKindName,
                closedHanName,
                openHanName,
                isYakuman);
        }

        public bool CanWinWithTileShapeOnly(string handText, string winningTileCode)
        {
            object winChecker = support.CreateWinChecker();
            return (bool)support.Reflection.Invoke(
                winChecker,
                "CanWinWithTile",
                support.CreateTiles(handText),
                support.DataFactory.CreateTile(winningTileCode));
        }

        public object EvaluateWithTile(
            object catalog,
            string handText,
            string winningTileCode,
            string winTypeName,
            bool isReachDeclared = false)
        {
            object evaluator = support.CreateWinDeclarationEvaluator(catalog);
            object context = support.CreateWinDeclarationEvaluationContext(
                handText,
                winningTileCode,
                winTypeName,
                isReachDeclared);

            return support.Reflection.Invoke(evaluator, "EvaluateWithTile", context);
        }

        public bool IsWinningShape(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "IsWinningShape");
        }

        public bool HasYaku(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "HasYaku");
        }

        public bool CanDeclareWin(object result)
        {
            return (bool)support.Reflection.GetProperty(result, "CanDeclareWin");
        }

        public int TotalHan(object result)
        {
            return (int)support.Reflection.GetProperty(HandEvaluationResult(result), "TotalHan");
        }

        public bool HandEvaluationHasYaku(object result)
        {
            return (bool)support.Reflection.GetProperty(HandEvaluationResult(result), "HasYaku");
        }

        public bool HandEvaluationHasYakuman(object result)
        {
            return (bool)support.Reflection.GetProperty(HandEvaluationResult(result), "HasYakuman");
        }

        public bool ContainsYaku(object result, string yakuKindName)
        {
            return support.ContainsYaku(HandEvaluationResult(result), yakuKindName);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }

        private object HandEvaluationResult(object result)
        {
            return support.Reflection.GetProperty(result, "HandEvaluationResult");
        }
    }
}
