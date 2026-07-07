using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Features.Win;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinningCandidateSelectorTests
    {
        private const string AmbiguousMultiCandidateHand =
            "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m";

        [Test]
        public void Select_NullEvaluationResult_ReturnsNull()
        {
            using (Driver driver = Driver.Create())
            {
                Assert.That(driver.Select(null), Is.Null);
            }
        }

        [Test]
        public void Select_EmptyHandEvaluationResult_ReturnsNull()
        {
            using (Driver driver = Driver.Create())
            {
                Assert.That(driver.Select(driver.EmptyHandEvaluationResult), Is.Null);
            }
        }

        [Test]
        public void Select_NoCandidateHasYaku_ReturnsNull()
        {
            using (Driver driver = Driver.Create())
            {
                object noYakuCandidate = driver.CreateCandidate();
                object evaluation = driver.CreateEvaluation(noYakuCandidate);

                Assert.That(driver.Select(evaluation), Is.Null);
            }
        }

        [Test]
        public void Select_SingleYakuCandidate_ReturnsThatCandidate()
        {
            using (Driver driver = Driver.Create())
            {
                object candidate = driver.CreateCandidate(YakuSpec.Normal("Tanyao", "One"));
                object evaluation = driver.CreateEvaluation(candidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(candidate));
            }
        }

        [Test]
        public void Select_NormalCandidates_ReturnsHighestTotalHan()
        {
            using (Driver driver = Driver.Create())
            {
                object lowCandidate = driver.CreateCandidate(YakuSpec.Normal("Tanyao", "One"));
                object highCandidate = driver.CreateCandidate(
                    YakuSpec.Normal("Chinitsu", "Six"),
                    YakuSpec.Normal("Ryanpeikou", "Three"));
                object evaluation = driver.CreateEvaluation(lowCandidate, highCandidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(highCandidate));
            }
        }

        [Test]
        public void Select_NormalCandidatesWithEqualTotalHan_ReturnsEarlierCandidate()
        {
            using (Driver driver = Driver.Create())
            {
                object firstCandidate = driver.CreateCandidate(YakuSpec.Normal("Tanyao", "One"));
                object secondCandidate = driver.CreateCandidate(YakuSpec.Normal("Pinfu", "One"));
                object evaluation = driver.CreateEvaluation(firstCandidate, secondCandidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(firstCandidate));
            }
        }

        [Test]
        public void Select_YakuLessCandidateBeforeYakuCandidate_DoesNotSelectYakuLessCandidate()
        {
            using (Driver driver = Driver.Create())
            {
                object noYakuCandidate = driver.CreateCandidate();
                object yakuCandidate = driver.CreateCandidate(YakuSpec.Normal("Tanyao", "One"));
                object evaluation = driver.CreateEvaluation(noYakuCandidate, yakuCandidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(yakuCandidate));
            }
        }

        [Test]
        public void Select_YakumanCandidate_BeatsHigherNormalCandidate()
        {
            using (Driver driver = Driver.Create())
            {
                object normalCandidate = driver.CreateCandidate(
                    YakuSpec.Normal("Chinitsu", "Six"),
                    YakuSpec.Normal("Ryanpeikou", "Three"));
                object yakumanCandidate = driver.CreateCandidate(YakuSpec.Yakuman("Daisangen"));
                object evaluation = driver.CreateEvaluation(normalCandidate, yakumanCandidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(yakumanCandidate));
            }
        }

        [Test]
        public void Select_MultipleYakumanCandidate_BeatsSingleYakumanCandidate()
        {
            using (Driver driver = Driver.Create())
            {
                object singleYakumanCandidate = driver.CreateCandidate(YakuSpec.Yakuman("Daisangen"));
                object multipleYakumanCandidate = driver.CreateCandidate(
                    YakuSpec.Yakuman("Shousuushii"),
                    YakuSpec.Yakuman("Tsuuiisou"));
                object evaluation = driver.CreateEvaluation(singleYakumanCandidate, multipleYakumanCandidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(multipleYakumanCandidate));
            }
        }

        [Test]
        public void Select_YakumanCandidatesWithEqualYakumanCount_ReturnsEarlierCandidate()
        {
            using (Driver driver = Driver.Create())
            {
                object firstCandidate = driver.CreateCandidate(YakuSpec.Yakuman("Daisangen"));
                object secondCandidate = driver.CreateCandidate(YakuSpec.Yakuman("Shousuushii"));
                object evaluation = driver.CreateEvaluation(firstCandidate, secondCandidate);

                Assert.That(driver.Select(evaluation), Is.SameAs(firstCandidate));
            }
        }

        [Test]
        public void Select_YakumanComparison_IgnoresTotalHan()
        {
            using (Driver driver = Driver.Create())
            {
                object firstCandidate = driver.CreateCandidate(YakuSpec.Yakuman("Daisangen"));
                object secondCandidate = driver.CreateCandidate(
                    YakuSpec.Yakuman("Shousuushii"),
                    YakuSpec.Normal("Chinitsu", "Six"));
                object evaluation = driver.CreateEvaluation(firstCandidate, secondCandidate);

                Assert.That(driver.TotalHan(secondCandidate), Is.GreaterThan(driver.TotalHan(firstCandidate)));
                Assert.That(driver.Select(evaluation), Is.SameAs(firstCandidate));
            }
        }

        [Test]
        public void Select_ReturnsOriginalCandidateInstance()
        {
            using (Driver driver = Driver.Create())
            {
                object selectedSource = driver.CreateCandidate(YakuSpec.Normal("Pinfu", "One"));
                object evaluation = driver.CreateEvaluation(selectedSource);
                object selected = driver.Select(evaluation);

                Assert.That(ReferenceEquals(selected, selectedSource), Is.True);
            }
        }

        [Test]
        public void Select_DoesNotMutateCandidatesOrTopLevelEvaluation()
        {
            using (Driver driver = Driver.Create())
            {
                object firstCandidate = driver.CreateCandidate(YakuSpec.Normal("Tanyao", "One"));
                object secondCandidate = driver.CreateCandidate(
                    YakuSpec.Normal("Pinfu", "One"),
                    YakuSpec.Normal("Iipeikou", "One"));
                object evaluation = driver.CreateEvaluation(firstCandidate, secondCandidate);
                CandidateListSnapshot before = driver.CaptureCandidateListSnapshot(evaluation);
                int topLevelYakuCountBefore = driver.TopLevelYakuCount(evaluation);
                int topLevelTotalHanBefore = driver.TopLevelTotalHan(evaluation);

                object selected = driver.Select(evaluation);

                CandidateListSnapshot after = driver.CaptureCandidateListSnapshot(evaluation);
                Assert.That(after, Is.EqualTo(before));
                Assert.That(driver.TopLevelYakuCount(evaluation), Is.EqualTo(topLevelYakuCountBefore));
                Assert.That(driver.TopLevelTotalHan(evaluation), Is.EqualTo(topLevelTotalHanBefore));
                Assert.That(selected, Is.SameAs(secondCandidate));
                Assert.That(driver.TotalHan(selected), Is.EqualTo(2));
                Assert.That(driver.CandidateYakuCount(selected), Is.EqualTo(2));
            }
        }

        [Test]
        public void Select_ActualEvaluatorResult_ReturnsExistingCandidateAndKeepsCandidatesUnchanged()
        {
            using (WinDeclarationEvaluatorTestDriver evaluatorDriver =
                WinDeclarationEvaluatorTestDriver.Create())
            using (Driver selectorDriver = Driver.Create())
            {
                object result = evaluatorDriver.EvaluateWithTile(
                    evaluatorDriver.CreateCatalog(
                        evaluatorDriver.CreateDefinition("MenzenTsumo", "One", "None")),
                    AmbiguousMultiCandidateHand,
                    "7m",
                    "Tsumo");
                object handEvaluation = selectorDriver.HandEvaluationResult(result);
                CandidateListSnapshot before =
                    selectorDriver.CaptureCandidateListSnapshot(handEvaluation);

                object selected = selectorDriver.Select(handEvaluation);

                CandidateListSnapshot after =
                    selectorDriver.CaptureCandidateListSnapshot(handEvaluation);
                Assert.That(selected, Is.Not.Null);
                Assert.That(selectorDriver.ContainsCandidate(handEvaluation, selected), Is.True);
                Assert.That(after, Is.EqualTo(before));
                Assert.That(selectorDriver.TopLevelYakuCount(handEvaluation), Is.EqualTo(0));
                Assert.That(selectorDriver.TopLevelTotalHan(handEvaluation), Is.EqualTo(0));
                Assert.That(selectorDriver.CandidateYakuSignature(selected), Does.Contain("MenzenTsumo"));
            }
        }

        private readonly struct YakuSpec
        {
            private YakuSpec(string kindName, string hanName, bool isYakuman)
            {
                KindName = kindName;
                HanName = hanName;
                IsYakuman = isYakuman;
            }

            public string KindName { get; }
            public string HanName { get; }
            public bool IsYakuman { get; }

            public static YakuSpec Normal(string kindName, string hanName)
            {
                return new YakuSpec(kindName, hanName, false);
            }

            public static YakuSpec Yakuman(string kindName)
            {
                return new YakuSpec(kindName, "None", true);
            }
        }

        private readonly struct CandidateListSnapshot : IEquatable<CandidateListSnapshot>
        {
            private readonly string snapshot;

            public CandidateListSnapshot(string snapshot)
            {
                this.snapshot = snapshot ?? string.Empty;
            }

            public bool Equals(CandidateListSnapshot other)
            {
                return snapshot == other.snapshot;
            }

            public override bool Equals(object obj)
            {
                return obj is CandidateListSnapshot other && Equals(other);
            }

            public override int GetHashCode()
            {
                return snapshot.GetHashCode();
            }

            public override string ToString()
            {
                return snapshot;
            }
        }

        private sealed class Driver : IDisposable
        {
            private const string SelectorTypeName =
                "MahjongPrototype.Services.WinningCandidateSelector, Assembly-CSharp";
            private const string HandEvaluationResultTypeName =
                "MahjongPrototype.Domain.HandEvaluationResult, Assembly-CSharp";
            private const string HandEvaluationCandidateTypeName =
                "MahjongPrototype.Domain.HandEvaluationCandidate, Assembly-CSharp";
            private const string HandEvaluationCandidateResultTypeName =
                "MahjongPrototype.Domain.HandEvaluationCandidateResult, Assembly-CSharp";
            private const string SevenPairsAnalysisTypeName =
                "MahjongPrototype.Domain.SevenPairsAnalysis, Assembly-CSharp";
            private const string EvaluatedYakuTypeName =
                "MahjongPrototype.Domain.EvaluatedYaku, Assembly-CSharp";

            private readonly WinFeatureTestSupport support;
            private readonly object selector;
            private bool disposed;

            private Driver(WinFeatureTestSupport support)
            {
                this.support = support;
                selector = Reflection.CreateInstance(Reflection.RequireType(SelectorTypeName));
            }

            private ReflectionTestAccess Reflection => support.Reflection;
            private CollectionTestAccess Collections => support.Collections;
            private MahjongTestTypes Types => support.Types;
            private MahjongTestDataFactory DataFactory => support.DataFactory;

            private Type HandEvaluationResultType =>
                Reflection.RequireType(HandEvaluationResultTypeName);
            private Type CandidateResultType =>
                Reflection.RequireType(HandEvaluationCandidateResultTypeName);
            private Type EvaluatedYakuType =>
                Reflection.RequireType(EvaluatedYakuTypeName);

            public object EmptyHandEvaluationResult =>
                Reflection.GetStaticProperty(HandEvaluationResultType, "Empty");

            public static Driver Create()
            {
                return new Driver(WinFeatureTestSupport.Create());
            }

            public object Select(object evaluationResult)
            {
                return Reflection.Invoke(selector, "Select", evaluationResult);
            }

            public object CreateEvaluation(params object[] candidates)
            {
                return Reflection.CreateInstance(
                    HandEvaluationResultType,
                    CreateEvaluatedYakuList(),
                    CreateCandidateResultList(candidates));
            }

            public object CreateCandidate(params YakuSpec[] yakuSpecs)
            {
                return Reflection.CreateInstance(
                    CandidateResultType,
                    CreateSevenPairsCandidate(),
                    CreateEvaluatedYakuList(yakuSpecs));
            }

            public object HandEvaluationResult(object winDeclarationEvaluationResult)
            {
                return Reflection.GetProperty(winDeclarationEvaluationResult, "HandEvaluationResult");
            }

            public bool ContainsCandidate(object evaluationResult, object candidate)
            {
                object candidates = CandidateResults(evaluationResult);
                int count = Collections.Count(candidates);
                for (int i = 0; i < count; i++)
                {
                    if (ReferenceEquals(Collections.Item(candidates, i), candidate))
                        return true;
                }

                return false;
            }

            public CandidateListSnapshot CaptureCandidateListSnapshot(object evaluationResult)
            {
                object candidates = CandidateResults(evaluationResult);
                int count = Collections.Count(candidates);
                StringBuilder builder = new StringBuilder();
                builder.Append("count=");
                builder.Append(count);
                builder.Append(';');

                for (int i = 0; i < count; i++)
                {
                    object candidate = Collections.Item(candidates, i);
                    builder.Append(i);
                    builder.Append('=');
                    builder.Append(candidate == null ? 0 : RuntimeHelpers.GetHashCode(candidate));
                    builder.Append('|');
                    builder.Append(TotalHan(candidate));
                    builder.Append('|');
                    builder.Append(CandidateYakuSignature(candidate));
                    builder.Append(';');
                }

                return new CandidateListSnapshot(builder.ToString());
            }

            public int TopLevelYakuCount(object evaluationResult)
            {
                return Collections.Count(Reflection.GetProperty(evaluationResult, "Yakus"));
            }

            public int TopLevelTotalHan(object evaluationResult)
            {
                return (int)Reflection.GetProperty(evaluationResult, "TotalHan");
            }

            public int TotalHan(object candidateResult)
            {
                return (int)Reflection.GetProperty(candidateResult, "TotalHan");
            }

            public int CandidateYakuCount(object candidateResult)
            {
                return Collections.Count(Reflection.GetProperty(candidateResult, "Yakus"));
            }

            public string CandidateYakuSignature(object candidateResult)
            {
                object yakus = Reflection.GetProperty(candidateResult, "Yakus");
                int count = Collections.Count(yakus);
                StringBuilder builder = new StringBuilder();
                builder.Append("yakus=");
                builder.Append(count);
                builder.Append('[');

                for (int i = 0; i < count; i++)
                {
                    object yaku = Collections.Item(yakus, i);
                    if (i > 0)
                        builder.Append(',');

                    builder.Append(Reflection.GetProperty(yaku, "Kind"));
                    builder.Append(':');
                    builder.Append(Reflection.GetProperty(yaku, "Han"));
                    builder.Append(':');
                    builder.Append(Reflection.GetProperty(yaku, "IsYakuman"));
                }

                builder.Append(']');
                return builder.ToString();
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                support.Dispose();
            }

            private object CandidateResults(object evaluationResult)
            {
                return Reflection.GetProperty(evaluationResult, "CandidateResults");
            }

            private object CreateSevenPairsCandidate()
            {
                object analysis = Reflection.InvokeStatic(
                    Reflection.RequireType(SevenPairsAnalysisTypeName),
                    "Win",
                    DataFactory.CreateTileArray("1m", "2m", "3p", "4p", "5s", "E", "C"));
                return Reflection.InvokeStatic(
                    Reflection.RequireType(HandEvaluationCandidateTypeName),
                    "SevenPairs",
                    analysis);
            }

            private object CreateCandidateResultList(params object[] candidates)
            {
                Type listType = typeof(List<>).MakeGenericType(CandidateResultType);
                IList list = (IList)Activator.CreateInstance(listType);
                for (int i = 0; i < candidates.Length; i++)
                    list.Add(candidates[i]);

                return list;
            }

            private object CreateEvaluatedYakuList(params YakuSpec[] yakuSpecs)
            {
                Type listType = typeof(List<>).MakeGenericType(EvaluatedYakuType);
                IList list = (IList)Activator.CreateInstance(listType);
                for (int i = 0; i < yakuSpecs.Length; i++)
                    list.Add(CreateEvaluatedYaku(yakuSpecs[i]));

                return list;
            }

            private object CreateEvaluatedYaku(YakuSpec yakuSpec)
            {
                return Reflection.CreateInstance(
                    EvaluatedYakuType,
                    Enum.Parse(Types.YakuKind, yakuSpec.KindName),
                    yakuSpec.KindName,
                    Enum.Parse(Types.HanValue, yakuSpec.HanName),
                    yakuSpec.IsYakuman);
            }
        }
    }
}
