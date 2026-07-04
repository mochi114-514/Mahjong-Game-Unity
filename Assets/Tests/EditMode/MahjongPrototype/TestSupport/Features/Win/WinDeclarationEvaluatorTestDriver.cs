using System;
using System.Collections.Generic;
using System.Reflection;

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
        private const string EvaluatedYakuTypeName =
            "MahjongPrototype.Domain.EvaluatedYaku, Assembly-CSharp";

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

        public object CandidateResultsCollection(object result)
        {
            return support.Reflection.GetProperty(HandEvaluationResult(result), "CandidateResults");
        }

        public int CandidateResultCount(object result)
        {
            return support.Collections.Count(CandidateResultsCollection(result));
        }

        public object CandidateResultAt(object result, int index)
        {
            return support.Collections.Item(CandidateResultsCollection(result), index);
        }

        public int CountCandidatesOfType(object result, string typeName)
        {
            object candidateResults = CandidateResultsCollection(result);
            int count = support.Collections.Count(candidateResults);
            int matchingCount = 0;

            for (int i = 0; i < count; i++)
            {
                if (CandidateTypeName(support.Collections.Item(candidateResults, i)) == typeName)
                    matchingCount++;
            }

            return matchingCount;
        }

        public string CandidateTypeName(object candidateResult)
        {
            return support.Reflection.GetProperty(Candidate(candidateResult), "Type").ToString();
        }

        public bool CandidateHasStandardInterpretation(object candidateResult)
        {
            return support.Reflection.GetProperty(
                Candidate(candidateResult),
                "StandardInterpretation") != null;
        }

        public bool CandidateSevenPairsIsWin(object candidateResult)
        {
            object analysis = support.Reflection.GetProperty(
                Candidate(candidateResult),
                "SevenPairsAnalysis");
            return analysis != null && (bool)support.Reflection.GetProperty(analysis, "IsWin");
        }

        public bool CandidateThirteenOrphansIsWin(object candidateResult)
        {
            object analysis = support.Reflection.GetProperty(
                Candidate(candidateResult),
                "ThirteenOrphansAnalysis");
            return analysis != null && (bool)support.Reflection.GetProperty(analysis, "IsWin");
        }

        public bool CandidateHasYaku(object candidateResult)
        {
            return (bool)support.Reflection.GetProperty(candidateResult, "HasYaku");
        }

        public bool CandidateHasYakuman(object candidateResult)
        {
            return (bool)support.Reflection.GetProperty(candidateResult, "HasYakuman");
        }

        public int CandidateTotalHan(object candidateResult)
        {
            return (int)support.Reflection.GetProperty(candidateResult, "TotalHan");
        }

        public object CandidateYakus(object candidateResult)
        {
            return support.Reflection.GetProperty(candidateResult, "Yakus");
        }

        public int CandidateYakuCount(object candidateResult)
        {
            return support.Collections.Count(CandidateYakus(candidateResult));
        }

        public object CandidateYakuAt(object candidateResult, int index)
        {
            return support.Collections.Item(CandidateYakus(candidateResult), index);
        }

        public bool CandidateContainsYaku(object candidateResult, string yakuKindName)
        {
            object yakus = CandidateYakus(candidateResult);
            int count = support.Collections.Count(yakus);

            for (int i = 0; i < count; i++)
            {
                object yaku = support.Collections.Item(yakus, i);
                if (support.Reflection.GetProperty(yaku, "Kind").ToString() == yakuKindName)
                    return true;
            }

            return false;
        }

        public bool CandidatePropertyCanWrite(object candidateResult, string propertyName)
        {
            object candidate = Candidate(candidateResult);
            PropertyInfo property = candidate.GetType().GetProperty(propertyName);
            return property != null && property.CanWrite;
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

        public object CreateLegacyHandEvaluationResult()
        {
            Type evaluatedYakuType = support.Reflection.RequireType(EvaluatedYakuTypeName);
            Type listType = typeof(List<>).MakeGenericType(evaluatedYakuType);
            object yakus = Activator.CreateInstance(listType);

            return support.Reflection.CreateInstance(
                support.Reflection.RequireType(HandEvaluationResultTypeName),
                yakus);
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

        private object Candidate(object candidateResult)
        {
            return support.Reflection.GetProperty(candidateResult, "Candidate");
        }

        private static object HandEvaluationResult(object result)
        {
            if (result == null)
                return null;

            PropertyInfo property = result.GetType().GetProperty(
                "HandEvaluationResult",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property == null ? result : property.GetValue(result);
        }

        private object ParseWinningHandShape(string shapeName)
        {
            return Enum.Parse(
                support.Reflection.RequireType(WinningHandShapeTypeName),
                shapeName);
        }
    }
}
