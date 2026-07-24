using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class YakumanMultiplierDataContractTests
    {
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        public void YakuDefinition_MultiplierDerivesIsYakuman(
            int yakumanMultiplier,
            bool expectedIsYakuman)
        {
            Driver driver = Driver.Create();
            object definition = driver.CreateDefinition(
                yakumanMultiplier == 0 ? "Tanyao" : "Daisangen",
                yakumanMultiplier == 0 ? "One" : "None",
                yakumanMultiplier == 0 ? "One" : "None",
                yakumanMultiplier);

            Assert.That(
                driver.IntProperty(definition, "YakumanMultiplier"),
                Is.EqualTo(yakumanMultiplier));
            Assert.That(
                driver.BoolProperty(definition, "IsYakuman"),
                Is.EqualTo(expectedIsYakuman));
        }

        [Test]
        public void YakuDefinition_NegativeMultiplierIsRejected()
        {
            Driver driver = Driver.Create();

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => driver.CreateDefinition("Daisangen", "None", "None", -1));

            Assert.That(exception.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void YakuDefinition_LegacyBoolConstructorMapsToMultiplier(
            bool isYakuman,
            int expectedMultiplier)
        {
            Driver driver = Driver.Create();
            object definition = driver.CreateDefinitionWithLegacyBool(
                isYakuman ? "Daisangen" : "Tanyao",
                isYakuman ? "None" : "One",
                isYakuman ? "None" : "One",
                isYakuman);

            Assert.That(
                driver.IntProperty(definition, "YakumanMultiplier"),
                Is.EqualTo(expectedMultiplier));
        }

        [TestCase(0, "Three", false)]
        [TestCase(1, "None", true)]
        [TestCase(2, "None", true)]
        public void EvaluatedYaku_MultiplierDerivesIsYakumanAndNormalizesHan(
            int yakumanMultiplier,
            string expectedHanName,
            bool expectedIsYakuman)
        {
            Driver driver = Driver.Create();
            object yaku = driver.CreateEvaluatedYaku(
                yakumanMultiplier == 0 ? "Ryanpeikou" : "Daisangen",
                "Three",
                yakumanMultiplier);

            Assert.That(
                driver.IntProperty(yaku, "YakumanMultiplier"),
                Is.EqualTo(yakumanMultiplier));
            Assert.That(driver.Property(yaku, "Han").ToString(), Is.EqualTo(expectedHanName));
            Assert.That(
                driver.BoolProperty(yaku, "IsYakuman"),
                Is.EqualTo(expectedIsYakuman));
        }

        [Test]
        public void EvaluatedYaku_NegativeMultiplierIsRejected()
        {
            Driver driver = Driver.Create();

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                () => driver.CreateEvaluatedYaku("Daisangen", "None", -1));

            Assert.That(exception.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void EvaluatedYaku_LegacyBoolConstructorMapsToMultiplier(
            bool isYakuman,
            int expectedMultiplier)
        {
            Driver driver = Driver.Create();
            object yaku = driver.CreateEvaluatedYakuWithLegacyBool(
                isYakuman ? "Daisangen" : "Tanyao",
                isYakuman ? "None" : "One",
                isYakuman);

            Assert.That(
                driver.IntProperty(yaku, "YakumanMultiplier"),
                Is.EqualTo(expectedMultiplier));
        }

        [Test]
        public void CandidateResult_NormalYakuSumHanOnly()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku("Tanyao", "One", 0),
                driver.CreateEvaluatedYaku("Ryanpeikou", "Three", 0));

            AssertCandidateTotals(driver, candidate, totalHan: 4, totalYakumanMultiplier: 0);
        }

        [Test]
        public void CandidateResult_TwoSingleYakumanSumToTwo()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku("Daisangen", "None", 1),
                driver.CreateEvaluatedYaku("Tsuuiisou", "None", 1));

            AssertCandidateTotals(driver, candidate, totalHan: 0, totalYakumanMultiplier: 2);
        }

        [Test]
        public void CandidateResult_ThreeSingleYakumanSumToThree()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku("Daisangen", "None", 1),
                driver.CreateEvaluatedYaku("Tsuuiisou", "None", 1),
                driver.CreateEvaluatedYaku("Suuankou", "None", 1));

            AssertCandidateTotals(driver, candidate, totalHan: 0, totalYakumanMultiplier: 3);
        }

        [Test]
        public void CandidateResult_SingleDoubleYakumanTotalsTwo()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku(
                    "KokushiMusouThirteenWait",
                    "None",
                    2));

            AssertCandidateTotals(driver, candidate, totalHan: 0, totalYakumanMultiplier: 2);
        }

        [Test]
        public void CandidateResult_SingleAndDoubleYakumanSumToThree()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku("Daisangen", "None", 1),
                driver.CreateEvaluatedYaku("SuuankouTanki", "None", 2));

            AssertCandidateTotals(driver, candidate, totalHan: 0, totalYakumanMultiplier: 3);
        }

        [Test]
        public void CandidateResult_TwoDoubleYakumanSumToFour()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku("Daisuushii", "None", 2),
                driver.CreateEvaluatedYaku("SuuankouTanki", "None", 2));

            AssertCandidateTotals(driver, candidate, totalHan: 0, totalYakumanMultiplier: 4);
        }

        [Test]
        public void CandidateResult_YakumanForcesTotalHanToZero()
        {
            Driver driver = Driver.Create();
            object candidate = driver.CreateCandidateResult(
                driver.CreateEvaluatedYaku("Chinitsu", "Six", 0),
                driver.CreateEvaluatedYaku("Daisangen", "None", 1));

            AssertCandidateTotals(driver, candidate, totalHan: 0, totalYakumanMultiplier: 1);
        }

        [Test]
        public void LegacyHandEvaluationResult_UsesSameYakumanInvariant()
        {
            Driver driver = Driver.Create();
            object result = driver.CreateLegacyHandEvaluationResult(
                driver.CreateEvaluatedYaku("Chinitsu", "Six", 0),
                driver.CreateEvaluatedYaku("SuuankouTanki", "None", 2));

            Assert.That(driver.IntProperty(result, "TotalHan"), Is.EqualTo(0));
            Assert.That(
                driver.IntProperty(result, "TotalYakumanMultiplier"),
                Is.EqualTo(2));
            Assert.That(driver.BoolProperty(result, "HasYakuman"), Is.True);
            Assert.That(driver.BoolProperty(result, "HasYaku"), Is.True);
        }

        [Test]
        public void CandidateBasedHandEvaluationResult_DoesNotSumAcrossCandidates()
        {
            Driver driver = Driver.Create();
            object result = driver.CreateCandidateBasedHandEvaluationResult(
                driver.CreateCandidateResult(
                    driver.CreateEvaluatedYaku("Daisangen", "None", 1)),
                driver.CreateCandidateResult(
                    driver.CreateEvaluatedYaku("SuuankouTanki", "None", 2)));

            Assert.That(driver.IntProperty(result, "TotalHan"), Is.EqualTo(0));
            Assert.That(
                driver.IntProperty(result, "TotalYakumanMultiplier"),
                Is.EqualTo(0));
            Assert.That(driver.BoolProperty(result, "HasYakuman"), Is.True);
            Assert.That(driver.BoolProperty(result, "HasYaku"), Is.True);
        }

        private static void AssertCandidateTotals(
            Driver driver,
            object candidate,
            int totalHan,
            int totalYakumanMultiplier)
        {
            Assert.That(driver.IntProperty(candidate, "TotalHan"), Is.EqualTo(totalHan));
            Assert.That(
                driver.IntProperty(candidate, "TotalYakumanMultiplier"),
                Is.EqualTo(totalYakumanMultiplier));
            Assert.That(
                driver.BoolProperty(candidate, "HasYakuman"),
                Is.EqualTo(totalYakumanMultiplier > 0));
            Assert.That(
                driver.BoolProperty(candidate, "HasYaku"),
                Is.EqualTo(totalHan > 0 || totalYakumanMultiplier > 0));
        }

        private sealed class Driver
        {
            private const string EvaluatedYakuTypeName =
                "MahjongPrototype.Domain.EvaluatedYaku, Assembly-CSharp";
            private const string HandEvaluationCandidateTypeName =
                "MahjongPrototype.Domain.HandEvaluationCandidate, Assembly-CSharp";
            private const string HandEvaluationCandidateResultTypeName =
                "MahjongPrototype.Domain.HandEvaluationCandidateResult, Assembly-CSharp";
            private const string HandEvaluationResultTypeName =
                "MahjongPrototype.Domain.HandEvaluationResult, Assembly-CSharp";
            private const string SevenPairsAnalysisTypeName =
                "MahjongPrototype.Domain.SevenPairsAnalysis, Assembly-CSharp";

            private readonly ReflectionTestAccess reflection;
            private readonly MahjongTestTypes types;
            private readonly MahjongTestDataFactory dataFactory;

            private Driver(
                ReflectionTestAccess reflection,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory)
            {
                this.reflection = reflection;
                this.types = types;
                this.dataFactory = dataFactory;
            }

            private Type EvaluatedYakuType =>
                reflection.RequireType(EvaluatedYakuTypeName);
            private Type CandidateResultType =>
                reflection.RequireType(HandEvaluationCandidateResultTypeName);
            private Type HandEvaluationResultType =>
                reflection.RequireType(HandEvaluationResultTypeName);

            public static Driver Create()
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                return new Driver(
                    reflection,
                    types,
                    new MahjongTestDataFactory(reflection, types));
            }

            public object CreateDefinition(
                string kindName,
                string closedHanName,
                string openHanName,
                int yakumanMultiplier)
            {
                return reflection.CreateInstance(
                    types.YakuDefinition,
                    Enum.Parse(types.YakuKind, kindName),
                    kindName,
                    Enum.Parse(types.HanValue, closedHanName),
                    Enum.Parse(types.HanValue, openHanName),
                    yakumanMultiplier,
                    true);
            }

            public object CreateDefinitionWithLegacyBool(
                string kindName,
                string closedHanName,
                string openHanName,
                bool isYakuman)
            {
                return reflection.CreateInstance(
                    types.YakuDefinition,
                    Enum.Parse(types.YakuKind, kindName),
                    kindName,
                    Enum.Parse(types.HanValue, closedHanName),
                    Enum.Parse(types.HanValue, openHanName),
                    isYakuman,
                    true);
            }

            public object CreateEvaluatedYaku(
                string kindName,
                string hanName,
                int yakumanMultiplier)
            {
                return reflection.CreateInstance(
                    EvaluatedYakuType,
                    Enum.Parse(types.YakuKind, kindName),
                    kindName,
                    Enum.Parse(types.HanValue, hanName),
                    yakumanMultiplier);
            }

            public object CreateEvaluatedYakuWithLegacyBool(
                string kindName,
                string hanName,
                bool isYakuman)
            {
                return reflection.CreateInstance(
                    EvaluatedYakuType,
                    Enum.Parse(types.YakuKind, kindName),
                    kindName,
                    Enum.Parse(types.HanValue, hanName),
                    isYakuman);
            }

            public object CreateCandidateResult(params object[] yakus)
            {
                return reflection.CreateInstance(
                    CandidateResultType,
                    CreateSevenPairsCandidate(),
                    CreateList(EvaluatedYakuType, yakus));
            }

            public object CreateLegacyHandEvaluationResult(params object[] yakus)
            {
                return reflection.CreateInstance(
                    HandEvaluationResultType,
                    CreateList(EvaluatedYakuType, yakus));
            }

            public object CreateCandidateBasedHandEvaluationResult(
                params object[] candidates)
            {
                return reflection.CreateInstance(
                    HandEvaluationResultType,
                    CreateList(EvaluatedYakuType),
                    CreateList(CandidateResultType, candidates));
            }

            public object Property(object target, string propertyName)
            {
                return reflection.GetProperty(target, propertyName);
            }

            public int IntProperty(object target, string propertyName)
            {
                return (int)Property(target, propertyName);
            }

            public bool BoolProperty(object target, string propertyName)
            {
                return (bool)Property(target, propertyName);
            }

            private object CreateSevenPairsCandidate()
            {
                object analysis = reflection.InvokeStatic(
                    reflection.RequireType(SevenPairsAnalysisTypeName),
                    "Win",
                    dataFactory.CreateTileArray(
                        "1m",
                        "2m",
                        "3p",
                        "4p",
                        "5s",
                        "E",
                        "C"));
                return reflection.InvokeStatic(
                    reflection.RequireType(HandEvaluationCandidateTypeName),
                    "SevenPairs",
                    analysis);
            }

            private static object CreateList(Type elementType, params object[] values)
            {
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = (IList)Activator.CreateInstance(listType);
                for (int i = 0; values != null && i < values.Length; i++)
                    list.Add(values[i]);

                return list;
            }
        }
    }
}
