using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class RoundResultTests
    {
        [Test]
        public void CreateWin_KeepsRoundAndWinFields()
        {
            Driver driver = Driver.Create();
            object selectedCandidate = driver.CreateCandidate(
                YakuSpec.Normal("Tanyao", "One"));

            object result = driver.CreateWin(
                "East",
                2,
                9,
                "West",
                "Ron",
                "South",
                "C",
                selectedCandidate);

            Assert.That(driver.TypeName(result), Is.EqualTo("Win"));
            Assert.That(driver.RoundWindName(result), Is.EqualTo("East"));
            Assert.That(driver.HandNumber(result), Is.EqualTo(2));
            Assert.That(driver.TurnIndex(result), Is.EqualTo(9));
            Assert.That(driver.WinnerSeatName(result), Is.EqualTo("West"));
            Assert.That(driver.WinTypeName(result), Is.EqualTo("Ron"));
            Assert.That(driver.SourceSeatName(result), Is.EqualTo("South"));
            Assert.That(driver.WinningTileCode(result), Is.EqualTo("C"));
            Assert.That(driver.SelectedCandidate(result), Is.SameAs(selectedCandidate));
        }

        [Test]
        public void CreateWin_ExposesSelectedCandidateYakuSummary()
        {
            Driver driver = Driver.Create();
            object selectedCandidate = driver.CreateCandidate(
                YakuSpec.Normal("Chinitsu", "Six"),
                YakuSpec.Yakuman("Daisangen"),
                YakuSpec.Yakuman("Tsuuiisou"));

            object result = driver.CreateWin(
                "East",
                1,
                3,
                "East",
                "Tsumo",
                null,
                "C",
                selectedCandidate);

            Assert.That(driver.YakuCount(result), Is.EqualTo(3));
            Assert.That(driver.TotalHan(result), Is.EqualTo(6));
            Assert.That(driver.HasYakuman(result), Is.True);
            Assert.That(driver.YakumanCount(result), Is.EqualTo(2));
        }

        [Test]
        public void CreateWin_KeepsSelectorResultInstance()
        {
            Driver driver = Driver.Create();
            object lowCandidate = driver.CreateCandidate(
                YakuSpec.Normal("Tanyao", "One"));
            object highCandidate = driver.CreateCandidate(
                YakuSpec.Normal("Chinitsu", "Six"));
            object evaluation = driver.CreateEvaluation(lowCandidate, highCandidate);
            object selected = driver.Select(evaluation);

            object result = driver.CreateWin(
                "East",
                1,
                3,
                "East",
                "Tsumo",
                null,
                "C",
                selected);

            Assert.That(selected, Is.SameAs(highCandidate));
            Assert.That(driver.SelectedCandidate(result), Is.SameAs(highCandidate));
        }

        [Test]
        public void CreateExhaustiveDraw_HasNoWinFieldsOrSelectedCandidate()
        {
            Driver driver = Driver.Create();

            object result = driver.CreateExhaustiveDraw("East", 4, 12, false);

            Assert.That(driver.TypeName(result), Is.EqualTo("ExhaustiveDraw"));
            Assert.That(driver.RoundWindName(result), Is.EqualTo("East"));
            Assert.That(driver.HandNumber(result), Is.EqualTo(4));
            Assert.That(driver.TurnIndex(result), Is.EqualTo(12));
            Assert.That(driver.WinnerSeatName(result), Is.Null);
            Assert.That(driver.WinTypeName(result), Is.Null);
            Assert.That(driver.SourceSeatName(result), Is.Null);
            Assert.That(driver.WinningTileCode(result), Is.Null);
            Assert.That(driver.SelectedCandidate(result), Is.Null);
        }

        [Test]
        public void CreateExhaustiveDraw_ExposesEmptyYakuSummary()
        {
            Driver driver = Driver.Create();

            object result = driver.CreateExhaustiveDraw("South", 4, 20, true);

            Assert.That(driver.YakuCount(result), Is.EqualTo(0));
            Assert.That(driver.TotalHan(result), Is.EqualTo(0));
            Assert.That(driver.HasYakuman(result), Is.False);
            Assert.That(driver.YakumanCount(result), Is.EqualTo(0));
            Assert.That(driver.IsFinalRound(result), Is.True);
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

        private sealed class Driver
        {
            private const string SelectorTypeName =
                "MahjongPrototype.Services.WinningCandidateSelector, Assembly-CSharp";
            private const string RoundResultTypeName =
                "MahjongPrototype.Domain.RoundResult, Assembly-CSharp";
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

            private readonly ReflectionTestAccess reflection;
            private readonly CollectionTestAccess collections;
            private readonly MahjongTestTypes types;
            private readonly MahjongTestDataFactory dataFactory;
            private readonly object selector;

            private Driver(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory)
            {
                this.reflection = reflection;
                this.collections = collections;
                this.types = types;
                this.dataFactory = dataFactory;
                selector = reflection.CreateInstance(reflection.RequireType(SelectorTypeName));
            }

            private Type RoundResultType => reflection.RequireType(RoundResultTypeName);
            private Type HandEvaluationResultType => reflection.RequireType(HandEvaluationResultTypeName);
            private Type CandidateResultType =>
                reflection.RequireType(HandEvaluationCandidateResultTypeName);
            private Type EvaluatedYakuType => reflection.RequireType(EvaluatedYakuTypeName);

            public static Driver Create()
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory =
                    new MahjongTestDataFactory(reflection, types);
                return new Driver(reflection, collections, types, dataFactory);
            }

            public object CreateWin(
                string roundWindName,
                int handNumber,
                int turnIndex,
                string winnerSeatName,
                string winTypeName,
                string sourceSeatName,
                string winningTileCode,
                object selectedCandidate)
            {
                object sourceSeat = sourceSeatName == null
                    ? null
                    : dataFactory.ParseSeat(sourceSeatName);
                object winningTile = winningTileCode == null
                    ? null
                    : dataFactory.CreateTile(winningTileCode);

                return reflection.InvokeStatic(
                    RoundResultType,
                    "CreateWin",
                    dataFactory.CreateWindProgress(roundWindName, handNumber),
                    turnIndex,
                    dataFactory.ParseSeat(winnerSeatName),
                    dataFactory.ParseWinType(winTypeName),
                    sourceSeat,
                    winningTile,
                    selectedCandidate,
                    roundWindName == "South" && handNumber == 4);
            }

            public object CreateExhaustiveDraw(
                string roundWindName,
                int handNumber,
                int turnIndex,
                bool isFinalRound)
            {
                return reflection.InvokeStatic(
                    RoundResultType,
                    "CreateExhaustiveDraw",
                    dataFactory.CreateWindProgress(roundWindName, handNumber),
                    turnIndex,
                    isFinalRound);
            }

            public object CreateEvaluation(params object[] candidates)
            {
                return reflection.CreateInstance(
                    HandEvaluationResultType,
                    CreateEvaluatedYakuList(),
                    CreateCandidateResultList(candidates));
            }

            public object CreateCandidate(params YakuSpec[] yakuSpecs)
            {
                return reflection.CreateInstance(
                    CandidateResultType,
                    CreateSevenPairsCandidate(),
                    CreateEvaluatedYakuList(yakuSpecs));
            }

            public object Select(object evaluation)
            {
                return reflection.Invoke(selector, "Select", evaluation);
            }

            public string TypeName(object result) => Property(result, "Type").ToString();
            public string RoundWindName(object result) =>
                Property(Property(result, "WindProgress"), "RoundWind").ToString();
            public int HandNumber(object result) =>
                (int)Property(Property(result, "WindProgress"), "HandNumber");
            public int TurnIndex(object result) => (int)Property(result, "TurnIndex");
            public bool IsFinalRound(object result) => (bool)Property(result, "IsFinalRound");
            public string WinnerSeatName(object result) => NullableProperty(result, "WinnerSeat");
            public string WinTypeName(object result) => NullableProperty(result, "WinType");
            public string SourceSeatName(object result) => NullableProperty(result, "SourceSeat");
            public string WinningTileCode(object result) => NullableProperty(result, "WinningTile");
            public object SelectedCandidate(object result) => Property(result, "SelectedCandidate");
            public int YakuCount(object result) => collections.Count(Property(result, "Yakus"));
            public int TotalHan(object result) => (int)Property(result, "TotalHan");
            public bool HasYakuman(object result) => (bool)Property(result, "HasYakuman");
            public int YakumanCount(object result) => (int)Property(result, "YakumanCount");

            private object CreateSevenPairsCandidate()
            {
                object analysis = reflection.InvokeStatic(
                    reflection.RequireType(SevenPairsAnalysisTypeName),
                    "Win",
                    dataFactory.CreateTileArray("1m", "2m", "3p", "4p", "5s", "E", "C"));
                return reflection.InvokeStatic(
                    reflection.RequireType(HandEvaluationCandidateTypeName),
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
                return reflection.CreateInstance(
                    EvaluatedYakuType,
                    Enum.Parse(types.YakuKind, yakuSpec.KindName),
                    yakuSpec.KindName,
                    Enum.Parse(types.HanValue, yakuSpec.HanName),
                    yakuSpec.IsYakuman);
            }

            private object Property(object target, string propertyName)
            {
                return reflection.GetProperty(target, propertyName);
            }

            private string NullableProperty(object target, string propertyName)
            {
                object value = Property(target, propertyName);
                return value == null ? null : value.ToString();
            }
        }
    }
}
