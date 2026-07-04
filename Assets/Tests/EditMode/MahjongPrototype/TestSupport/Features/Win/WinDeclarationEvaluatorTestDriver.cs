using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinDeclarationEvaluatorTestDriver : IDisposable
    {
        private const string HandEvaluationContextTypeName =
            "MahjongPrototype.Domain.HandEvaluationContext, Assembly-CSharp";
        private const string HandEvaluationResultTypeName =
            "MahjongPrototype.Domain.HandEvaluationResult, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";
        private const string WinningHandShapeTypeName =
            "MahjongPrototype.Domain.WinningHandShape, Assembly-CSharp";

        private readonly WinFeatureTestSupport support;
        private bool disposed;

        private WinDeclarationEvaluatorTestDriver(WinFeatureTestSupport support)
        {
            this.support = support;
        }

        public static WinDeclarationEvaluatorTestDriver Create()
        {
            return new WinDeclarationEvaluatorTestDriver(WinFeatureTestSupport.Create());
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

        public object WinningHandAnalysis(object result)
        {
            return support.Reflection.GetProperty(result, "WinningHandAnalysis");
        }

        public bool AnalysisCanWin(object result)
        {
            return (bool)support.Reflection.GetProperty(WinningHandAnalysis(result), "CanWin");
        }

        public int AnalysisStandardDecompositionCount(object result)
        {
            return support.Collections.Count(
                support.Reflection.GetProperty(
                    WinningHandAnalysis(result),
                    "StandardDecompositions"));
        }

        public int AnalysisStandardWinningInterpretationCount(object result)
        {
            return support.Collections.Count(
                support.Reflection.GetProperty(
                    WinningHandAnalysis(result),
                    "StandardWinningInterpretations"));
        }

        public bool AnalysisHasWaitType(object result, string waitTypeName)
        {
            object interpretations = support.Reflection.GetProperty(
                WinningHandAnalysis(result),
                "StandardWinningInterpretations");
            int count = support.Collections.Count(interpretations);

            for (int i = 0; i < count; i++)
            {
                object interpretation = support.Collections.Item(interpretations, i);
                if (support.Reflection.GetProperty(interpretation, "WaitType").ToString() == waitTypeName)
                    return true;
            }

            return false;
        }

        public bool AnalysisSevenPairsIsWin(object result)
        {
            object analysis = support.Reflection.GetProperty(
                WinningHandAnalysis(result),
                "SevenPairsAnalysis");
            return (bool)support.Reflection.GetProperty(analysis, "IsWin");
        }

        public bool AnalysisThirteenOrphansIsWin(object result)
        {
            object analysis = support.Reflection.GetProperty(
                WinningHandAnalysis(result),
                "ThirteenOrphansAnalysis");
            return (bool)support.Reflection.GetProperty(analysis, "IsWin");
        }

        public object CreateLegacyHandEvaluationContext()
        {
            return support.Reflection.CreateInstance(
                support.Reflection.RequireType(HandEvaluationContextTypeName),
                support.CreateTiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C"),
                support.DataFactory.CreateTile("C"),
                support.DataFactory.ParseWinType("Ron"),
                ParseWinningHandShape("Standard"),
                support.DataFactory.ParseSeat("East"),
                null,
                support.DataFactory.ParseRoundWind("East"),
                support.DataFactory.ParseSeat("East"),
                false,
                true);
        }

        public object CreateLegacyWinDeclarationEvaluationResult()
        {
            object winChecker = support.CreateWinChecker();
            object winCheckResult = support.Reflection.Invoke(
                winChecker,
                "CheckCompletedHand",
                support.CreateTiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C C"));
            object handEvaluationResult = support.Reflection.GetStaticProperty(
                support.Reflection.RequireType(HandEvaluationResultTypeName),
                "Empty");

            return support.Reflection.CreateInstance(
                support.Reflection.RequireType(WinDeclarationEvaluationResultTypeName),
                winCheckResult,
                handEvaluationResult);
        }

        public object CreateWinDeclarationEvaluatorWithEmptyCatalog()
        {
            return support.CreateWinDeclarationEvaluator(support.CreateYakuCatalog());
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

        private object ParseWinningHandShape(string shapeName)
        {
            return Enum.Parse(
                support.Reflection.RequireType(WinningHandShapeTypeName),
                shapeName);
        }
    }
}
