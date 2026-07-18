using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReactionWindowMultiSeatResponseTests
    {
        private const string ReactionWindowTypeName =
            "MahjongPrototype.Domain.ReactionWindow, Assembly-CSharp";
        private const string ReactionWindowSourceTypeName =
            "MahjongPrototype.Domain.ReactionWindowSource, Assembly-CSharp";
        private const string ReactionWindowCandidateTypeName =
            "MahjongPrototype.Domain.ReactionWindowCandidate, Assembly-CSharp";
        private const string ReactionKindTypeName =
            "MahjongPrototype.Domain.ReactionKind, Assembly-CSharp";
        private const string ChiOptionTypeName =
            "MahjongPrototype.Domain.ChiOption, Assembly-CSharp";
        private const string WinCheckResultTypeName =
            "MahjongPrototype.Domain.WinCheckResult, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";
        private const string SeatAnswerTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswer, Assembly-CSharp";
        private const string SeatAnswerKindTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerKind, Assembly-CSharp";
        private const string SeatAnswerCollectionTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerCollection, Assembly-CSharp";
        private const string SeatAnswerResolverTypeName =
            "MahjongPrototype.Services.ReactionWindowSeatAnswerResolver, Assembly-CSharp";

        [Test]
        public void SeatAnswers_DeduplicateTargetSeats_AndLetEachSeatAnswerIndependently()
        {
            Fixture fixture = CreateFixture();
            object southPon = CreateCandidate(fixture, "Pon", "South");
            object southChi = CreateCandidate(fixture, "Chi", "South");
            object westRon = CreateCandidate(fixture, "Ron", "West");
            object northDaiminkan = CreateCandidate(fixture, "Daiminkan", "North");
            object window = CreateDiscardWindow(
                fixture,
                101,
                "East",
                southPon,
                southChi,
                westRon,
                northDaiminkan);
            object answers = CreateAnswerCollection(fixture, window);

            AssertSeats(fixture, fixture.Reflection.GetProperty(answers, "TargetSeats"),
                "South", "West", "North");
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "HasUnansweredSeats"), Is.True);
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "AreAllSeatsAnswered"), Is.False);

            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 101, "South", "Pass")));
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 101, "West", "Ron")));
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 101, "North", "Daiminkan")));

            Assert.That(RegisteredAnswerCount(fixture, answers), Is.EqualTo(3));
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "HasUnansweredSeats"), Is.False);
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "AreAllSeatsAnswered"), Is.True);
            Assert.That(FindRegisteredAnswer(fixture, answers, "South"), Is.Not.Null);
            Assert.That(FindRegisteredAnswer(fixture, answers, "West"), Is.Not.Null);
            Assert.That(FindRegisteredAnswer(fixture, answers, "North"), Is.Not.Null);
        }

        [TestCase("Pass")]
        [TestCase("Ron")]
        [TestCase("Pon")]
        [TestCase("Chi")]
        [TestCase("Daiminkan")]
        public void TryRegister_AcceptsPassAndEveryCandidateDeclaration(string answerKind)
        {
            Fixture fixture = CreateFixture();
            object candidate = CreateCandidate(
                fixture,
                answerKind == "Pass" ? "Pon" : answerKind,
                "South");
            object window = CreateDiscardWindow(fixture, 102, "East", candidate);
            object answers = CreateAnswerCollection(fixture, window);
            object answer = CreateAnswer(
                fixture,
                102,
                "South",
                answerKind,
                answerKind == "Chi" ? 3 : (int?)null);

            object result = Register(fixture, answers, answer);

            AssertAccepted(result);
            Assert.That((int)fixture.Reflection.GetProperty(result, "WindowId"), Is.EqualTo(102));
            Assert.That(fixture.Reflection.GetProperty(result, "Answer"), Is.SameAs(answer));
            Assert.That(RegisteredAnswerCount(fixture, answers), Is.EqualTo(1));
            Assert.That(FindRegisteredAnswer(fixture, answers, "South"), Is.SameAs(answer));
        }

        [Test]
        public void TryRegister_RejectsInvalidAnswers_WithoutChangingRegisteredAnswers()
        {
            Fixture fixture = CreateFixture();
            object window = CreateDiscardWindow(
                fixture,
                103,
                "East",
                CreateCandidate(fixture, "Pon", "South"),
                CreateCandidate(fixture, "Chi", "South"),
                CreateCandidate(fixture, "Ron", "West"));
            object answers = CreateAnswerCollection(fixture, window);

            AssertRejectedWithoutRegistration(
                fixture,
                answers,
                CreateAnswer(fixture, 103, "North", "Pass"),
                "NotReactionCandidateSeat");
            AssertRejectedWithoutRegistration(
                fixture,
                answers,
                CreateAnswer(fixture, 103, "South", "Daiminkan"),
                "ReactionKindUnavailable");
            AssertRejectedWithoutRegistration(
                fixture,
                answers,
                CreateRawAnswer(fixture, 103, "South", 99),
                "ReactionKindUnsupported");
            AssertRejectedWithoutRegistration(
                fixture,
                answers,
                CreateAnswer(fixture, 103, "South", "Chi", 4),
                "ChiOptionMissing");
            AssertRejectedWithoutRegistration(
                fixture,
                answers,
                CreateAnswer(fixture, 999, "South", "Pon"),
                "ReactionWindowMismatch");

            object firstAnswer = CreateAnswer(fixture, 103, "South", "Pon");
            AssertAccepted(Register(fixture, answers, firstAnswer));
            object duplicateResult = Register(
                fixture,
                answers,
                CreateAnswer(fixture, 103, "South", "Chi", 3));

            AssertRejected(duplicateResult, "ReactionSeatAlreadyAnswered");
            Assert.That(RegisteredAnswerCount(fixture, answers), Is.EqualTo(1));
            Assert.That(FindRegisteredAnswer(fixture, answers, "South"), Is.SameAs(firstAnswer));
            Assert.That(
                fixture.Reflection.GetProperty(
                    FindRegisteredAnswer(fixture, answers, "South"),
                    "Kind").ToString(),
                Is.EqualTo("Pon"));
        }

        [Test]
        public void AllSeatsAnswered_BecomesTrueOnlyAfterEveryTargetSeatResponds()
        {
            Fixture fixture = CreateFixture();
            object window = CreateDiscardWindow(
                fixture,
                104,
                "East",
                CreateCandidate(fixture, "Pon", "South"),
                CreateCandidate(fixture, "Ron", "West"));
            object answers = CreateAnswerCollection(fixture, window);

            Assert.That((bool)fixture.Reflection.GetProperty(answers, "HasUnansweredSeats"), Is.True);
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "AreAllSeatsAnswered"), Is.False);

            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 104, "South", "Pass")));
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "HasUnansweredSeats"), Is.True);
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "AreAllSeatsAnswered"), Is.False);

            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 104, "West", "Ron")));
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "HasUnansweredSeats"), Is.False);
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "AreAllSeatsAnswered"), Is.True);
        }

        [Test]
        public void Resolver_ReturnsPendingUntilEveryTargetSeatHasAnswered()
        {
            Fixture fixture = CreateFixture();
            object window = CreateDiscardWindow(
                fixture,
                201,
                "East",
                CreateCandidate(fixture, "Ron", "South"),
                CreateCandidate(fixture, "Pon", "West"));
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 201, "South", "Ron")));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            Assert.That(fixture.Reflection.GetProperty(resolution, "Type").ToString(),
                Is.EqualTo("PendingAnswers"));
            Assert.That((int)fixture.Reflection.GetProperty(resolution, "WindowId"), Is.EqualTo(201));
            Assert.That(fixture.Reflection.GetProperty(resolution, "Candidate"), Is.Null);
        }

        [Test]
        public void Resolver_ReturnsNoReactionWhenEverySeatPasses()
        {
            Fixture fixture = CreateFixture();
            object window = CreateDiscardWindow(
                fixture,
                202,
                "East",
                CreateCandidate(fixture, "Ron", "South"),
                CreateCandidate(fixture, "Pon", "West"));
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 202, "South", "Pass")));
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 202, "West", "Pass")));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            Assert.That(fixture.Reflection.GetProperty(resolution, "Type").ToString(),
                Is.EqualTo("NoReaction"));
            Assert.That(fixture.Reflection.GetProperty(resolution, "SelectedSeat"), Is.Null);
            Assert.That(fixture.Reflection.GetProperty(resolution, "SelectedKind"), Is.Null);
            Assert.That(fixture.Reflection.GetProperty(resolution, "Candidate"), Is.Null);
        }

        [Test]
        public void Resolver_PrioritizesRonOverMeldCalls()
        {
            Fixture fixture = CreateFixture();
            object southPon = CreateCandidate(fixture, "Pon", "South");
            object westRon = CreateCandidate(fixture, "Ron", "West");
            object northChi = CreateCandidate(fixture, "Chi", "North");
            object window = CreateDiscardWindow(
                fixture,
                203,
                "East",
                southPon,
                westRon,
                northChi);
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 203, "South", "Pon")));
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 203, "North", "Chi", 3)));
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 203, "West", "Ron")));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(fixture, resolution, 203, "West", "Ron", westRon, null);
        }

        [TestCase("Pon")]
        [TestCase("Daiminkan")]
        public void Resolver_PrioritizesPonAndDaiminkanOverChi(string higherPriorityKind)
        {
            Fixture fixture = CreateFixture();
            object highPriorityCandidate = CreateCandidate(fixture, higherPriorityKind, "North");
            object chiCandidate = CreateCandidate(fixture, "Chi", "South");
            object window = CreateDiscardWindow(
                fixture,
                204,
                "East",
                chiCandidate,
                highPriorityCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(
                fixture,
                answers,
                CreateAnswer(fixture, 204, "South", "Chi", 3)));
            AssertAccepted(Register(
                fixture,
                answers,
                CreateAnswer(fixture, 204, "North", higherPriorityKind)));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(
                fixture,
                resolution,
                204,
                "North",
                higherPriorityKind,
                highPriorityCandidate,
                null);
        }

        [Test]
        public void Resolver_UsesNearestSeatFromSourceForEqualPriorityCalls()
        {
            Fixture fixture = CreateFixture();
            object southPon = CreateCandidate(fixture, "Pon", "South");
            object eastDaiminkan = CreateCandidate(fixture, "Daiminkan", "East");
            object window = CreateDiscardWindow(
                fixture,
                205,
                "West",
                southPon,
                eastDaiminkan);
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 205, "South", "Pon")));
            AssertAccepted(Register(
                fixture,
                answers,
                CreateAnswer(fixture, 205, "East", "Daiminkan")));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(fixture, resolution, 205, "East", "Daiminkan", eastDaiminkan, null);
        }

        [Test]
        public void Resolver_UsesNearestRonOnlyAsTemporaryMultipleRonRule()
        {
            Fixture fixture = CreateFixture();
            object southRon = CreateCandidate(fixture, "Ron", "South");
            object westRon = CreateCandidate(fixture, "Ron", "West");
            object window = CreateDiscardWindow(fixture, 206, "East", westRon, southRon);
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 206, "West", "Ron")));
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 206, "South", "Ron")));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(fixture, resolution, 206, "South", "Ron", southRon, null);
            Assert.That(RegisteredAnswerCount(fixture, answers), Is.EqualTo(2));
            Assert.That(FindRegisteredAnswer(fixture, answers, "West"), Is.Not.Null);
        }

        [Test]
        public void Resolver_ResultDoesNotDependOnCandidateOrAnswerRegistrationOrder()
        {
            Fixture fixture = CreateFixture();
            object firstSouthPon = CreateCandidate(fixture, "Pon", "South");
            object firstNorthDaiminkan = CreateCandidate(fixture, "Daiminkan", "North");
            object firstWindow = CreateDiscardWindow(
                fixture,
                207,
                "East",
                firstNorthDaiminkan,
                firstSouthPon);
            object firstAnswers = CreateAnswerCollection(fixture, firstWindow);
            AssertAccepted(Register(
                fixture,
                firstAnswers,
                CreateAnswer(fixture, 207, "North", "Daiminkan")));
            AssertAccepted(Register(fixture, firstAnswers, CreateAnswer(fixture, 207, "South", "Pon")));

            object secondSouthPon = CreateCandidate(fixture, "Pon", "South");
            object secondNorthDaiminkan = CreateCandidate(fixture, "Daiminkan", "North");
            object secondWindow = CreateDiscardWindow(
                fixture,
                208,
                "East",
                secondSouthPon,
                secondNorthDaiminkan);
            object secondAnswers = CreateAnswerCollection(fixture, secondWindow);
            AssertAccepted(Register(fixture, secondAnswers, CreateAnswer(fixture, 208, "South", "Pon")));
            AssertAccepted(Register(
                fixture,
                secondAnswers,
                CreateAnswer(fixture, 208, "North", "Daiminkan")));

            object firstResolution = Resolve(fixture, firstAnswers, "East", "South", "West", "North");
            object secondResolution = Resolve(fixture, secondAnswers, "East", "South", "West", "North");

            AssertSelected(fixture, firstResolution, 207, "South", "Pon", firstSouthPon, null);
            AssertSelected(fixture, secondResolution, 208, "South", "Pon", secondSouthPon, null);
        }

        [Test]
        public void Resolver_MapsChiOptionToItsCandidateWithoutCandidateOrderDependency()
        {
            Fixture fixture = CreateFixture();
            object firstChiOptionThree = CreateCandidate(fixture, "Chi", "South", 3);
            object firstChiOptionFour = CreateCandidate(fixture, "Chi", "South", 4);
            object firstWindow = CreateDiscardWindow(
                fixture,
                209,
                "East",
                firstChiOptionThree,
                firstChiOptionFour);
            object firstAnswers = CreateAnswerCollection(fixture, firstWindow);
            AssertAccepted(Register(
                fixture,
                firstAnswers,
                CreateAnswer(fixture, 209, "South", "Chi", 4)));

            object secondChiOptionThree = CreateCandidate(fixture, "Chi", "South", 3);
            object secondChiOptionFour = CreateCandidate(fixture, "Chi", "South", 4);
            object secondWindow = CreateDiscardWindow(
                fixture,
                210,
                "East",
                secondChiOptionFour,
                secondChiOptionThree);
            object secondAnswers = CreateAnswerCollection(fixture, secondWindow);
            AssertAccepted(Register(
                fixture,
                secondAnswers,
                CreateAnswer(fixture, 210, "South", "Chi", 4)));

            object firstResolution = Resolve(fixture, firstAnswers, "East", "South", "West", "North");
            object secondResolution = Resolve(fixture, secondAnswers, "East", "South", "West", "North");

            AssertSelected(
                fixture,
                firstResolution,
                209,
                "South",
                "Chi",
                firstChiOptionFour,
                4);
            AssertSelected(
                fixture,
                secondResolution,
                210,
                "South",
                "Chi",
                secondChiOptionFour,
                4);
        }

        [Test]
        public void Resolver_SelectedChiExposesWindowSeatKindCandidateAndOptionId()
        {
            Fixture fixture = CreateFixture();
            object chiCandidate = CreateCandidate(fixture, "Chi", "South");
            object window = CreateDiscardWindow(fixture, 211, "East", chiCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            object chiAnswer = CreateAnswer(fixture, 211, "South", "Chi", 3);
            AssertAccepted(Register(fixture, answers, chiAnswer));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(fixture, resolution, 211, "South", "Chi", chiCandidate, 3);
            Assert.That(fixture.Reflection.GetProperty(resolution, "Answer"), Is.SameAs(chiAnswer));
        }

        [Test]
        public void Resolver_DoesNotMutateWindowCandidatesOrRegisteredAnswers()
        {
            Fixture fixture = CreateFixture();
            object southPon = CreateCandidate(fixture, "Pon", "South");
            object westChi = CreateCandidate(fixture, "Chi", "West");
            object window = CreateDiscardWindow(fixture, 212, "East", southPon, westChi);
            object answers = CreateAnswerCollection(fixture, window);
            object southAnswer = CreateAnswer(fixture, 212, "South", "Pon");
            object westAnswer = CreateAnswer(fixture, 212, "West", "Chi", 3);
            AssertAccepted(Register(fixture, answers, southAnswer));
            AssertAccepted(Register(fixture, answers, westAnswer));

            object windowStateBefore = fixture.Reflection.GetProperty(window, "State");
            int turnIndexBefore = (int)fixture.Reflection.GetProperty(window, "TurnIndex");
            object sourceBefore = fixture.Reflection.GetProperty(window, "Source");
            object candidatesBefore = fixture.Reflection.GetProperty(window, "Candidates");
            object[] candidateReferencesBefore = CopyCollection(fixture, candidatesBefore);
            string[] candidateStatesBefore = CandidateResponseStates(fixture, candidateReferencesBefore);
            object[] registeredAnswersBefore = CopyCollection(
                fixture,
                fixture.Reflection.GetProperty(answers, "RegisteredAnswers"));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(fixture, resolution, 212, "South", "Pon", southPon, null);
            Assert.That(fixture.Reflection.GetProperty(window, "State"), Is.EqualTo(windowStateBefore));
            Assert.That((int)fixture.Reflection.GetProperty(window, "TurnIndex"), Is.EqualTo(turnIndexBefore));
            Assert.That(fixture.Reflection.GetProperty(window, "Source"), Is.EqualTo(sourceBefore));
            AssertSameCollection(
                fixture,
                candidateReferencesBefore,
                fixture.Reflection.GetProperty(window, "Candidates"));
            Assert.That(
                CandidateResponseStates(
                    fixture,
                    CopyCollection(fixture, fixture.Reflection.GetProperty(window, "Candidates"))),
                Is.EqualTo(candidateStatesBefore));
            AssertSameCollection(
                fixture,
                registeredAnswersBefore,
                fixture.Reflection.GetProperty(answers, "RegisteredAnswers"));
            Assert.That((bool)fixture.Reflection.GetProperty(answers, "AreAllSeatsAnswered"), Is.True);
            Assert.That(southAnswer, Is.SameAs(FindRegisteredAnswer(fixture, answers, "South")));
            Assert.That(westAnswer, Is.SameAs(FindRegisteredAnswer(fixture, answers, "West")));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resolver_HandlesDiscardAndKakanReactionSources(bool isKakan)
        {
            Fixture fixture = CreateFixture();
            object ronCandidate = CreateCandidate(fixture, "Ron", "South");
            object window = isKakan
                ? CreateKakanWindow(fixture, 213, "East", ronCandidate)
                : CreateDiscardWindow(fixture, 213, "East", ronCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            AssertAccepted(Register(fixture, answers, CreateAnswer(fixture, 213, "South", "Ron")));

            object resolution = Resolve(fixture, answers, "East", "South", "West", "North");

            AssertSelected(fixture, resolution, 213, "South", "Ron", ronCandidate, null);
            object source = fixture.Reflection.GetProperty(resolution, "Source");
            Assert.That(fixture.Reflection.GetProperty(source, "Kind").ToString(),
                Is.EqualTo(isKakan ? "Kakan" : "Discard"));
            Assert.That(fixture.Reflection.GetProperty(source, "ActorSeat").ToString(), Is.EqualTo("East"));
        }

        private static Fixture CreateFixture()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new Fixture(
                reflection,
                new CollectionTestAccess(reflection),
                types,
                dataFactory,
                reflection.RequireType(ReactionWindowTypeName),
                reflection.RequireType(ReactionWindowSourceTypeName),
                reflection.RequireType(ReactionWindowCandidateTypeName),
                reflection.RequireType(ReactionKindTypeName),
                reflection.RequireType(ChiOptionTypeName),
                reflection.RequireType(WinCheckResultTypeName),
                reflection.RequireType(WinDeclarationEvaluationResultTypeName),
                reflection.RequireType(SeatAnswerTypeName),
                reflection.RequireType(SeatAnswerKindTypeName),
                reflection.RequireType(SeatAnswerCollectionTypeName),
                reflection.RequireType(SeatAnswerResolverTypeName));
        }

        private static object CreateDiscardWindow(
            Fixture fixture,
            int windowId,
            string sourceSeat,
            params object[] candidates)
        {
            return fixture.Reflection.CreateInstance(
                fixture.ReactionWindowType,
                windowId,
                fixture.DataFactory.CreateDiscardRecord(sourceSeat, "5m", 7),
                7,
                CreateCandidateList(fixture, candidates));
        }

        private static object CreateKakanWindow(
            Fixture fixture,
            int windowId,
            string sourceSeat,
            params object[] candidates)
        {
            object source = fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowSourceType,
                "FromKakan",
                fixture.DataFactory.ParseSeat(sourceSeat),
                fixture.DataFactory.CreateTile("5m"),
                7);
            return fixture.Reflection.CreateInstance(
                fixture.ReactionWindowType,
                windowId,
                source,
                7,
                CreateCandidateList(fixture, candidates));
        }

        private static object CreateAnswerCollection(Fixture fixture, object window)
        {
            return fixture.Reflection.CreateInstance(fixture.SeatAnswerCollectionType, window);
        }

        private static object CreateCandidate(
            Fixture fixture,
            string kind,
            string seat,
            int chiOptionId = 3)
        {
            switch (kind)
            {
                case "Ron":
                    return CreateRonCandidate(fixture, seat);
                case "Pon":
                    return fixture.Reflection.InvokeStatic(
                        fixture.ReactionWindowCandidateType,
                        "CreatePon",
                        fixture.DataFactory.ParseSeat(seat),
                        fixture.DataFactory.CreateTile("5m"));
                case "Chi":
                    return CreateChiCandidate(fixture, seat, chiOptionId);
                case "Daiminkan":
                    return fixture.Reflection.InvokeStatic(
                        fixture.ReactionWindowCandidateType,
                        "CreateDaiminkan",
                        fixture.DataFactory.ParseSeat(seat),
                        fixture.DataFactory.CreateTile("5m"));
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static object CreateRonCandidate(Fixture fixture, string seat)
        {
            object notWin = fixture.Reflection.GetStaticProperty(
                fixture.WinCheckResultType,
                "NotWin");
            object evaluation = fixture.Reflection.InvokeStatic(
                fixture.WinDeclarationEvaluationResultType,
                "NotWinningShape",
                notWin);
            return fixture.Reflection.CreateInstance(
                fixture.ReactionWindowCandidateType,
                fixture.DataFactory.ParseSeat(seat),
                Enum.Parse(fixture.ReactionKindType, "Ron"),
                evaluation);
        }

        private static object CreateChiCandidate(Fixture fixture, string seat, int optionId)
        {
            string firstHandTile;
            string secondHandTile;
            string firstMeldTile;
            string secondMeldTile;
            string thirdMeldTile;
            switch (optionId)
            {
                case 3:
                    firstHandTile = "3m";
                    secondHandTile = "4m";
                    firstMeldTile = "3m";
                    secondMeldTile = "4m";
                    thirdMeldTile = "5m";
                    break;
                case 4:
                    firstHandTile = "4m";
                    secondHandTile = "6m";
                    firstMeldTile = "4m";
                    secondMeldTile = "5m";
                    thirdMeldTile = "6m";
                    break;
                case 5:
                    firstHandTile = "6m";
                    secondHandTile = "7m";
                    firstMeldTile = "5m";
                    secondMeldTile = "6m";
                    thirdMeldTile = "7m";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(optionId));
            }

            object option = fixture.Reflection.CreateInstance(
                fixture.ChiOptionType,
                optionId,
                fixture.DataFactory.CreateTile("5m"),
                CreateTypedList(
                    fixture.Types.Tile,
                    fixture.DataFactory.CreateTile(firstHandTile),
                    fixture.DataFactory.CreateTile(secondHandTile)),
                CreateTypedList(
                    fixture.Types.Tile,
                    fixture.DataFactory.CreateTile(firstMeldTile),
                    fixture.DataFactory.CreateTile(secondMeldTile),
                    fixture.DataFactory.CreateTile(thirdMeldTile)));
            return fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowCandidateType,
                "CreateChi",
                fixture.DataFactory.ParseSeat(seat),
                fixture.DataFactory.CreateTile("5m"),
                CreateTypedList(fixture.ChiOptionType, option));
        }

        private static object CreateAnswer(
            Fixture fixture,
            int windowId,
            string seat,
            string kind,
            int? chiOptionId = null)
        {
            object parsedSeat = fixture.DataFactory.ParseSeat(seat);
            switch (kind)
            {
                case "Pass":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Pass",
                        windowId,
                        parsedSeat);
                case "Ron":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Ron",
                        windowId,
                        parsedSeat);
                case "Pon":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Pon",
                        windowId,
                        parsedSeat);
                case "Chi":
                    return chiOptionId.HasValue
                        ? fixture.Reflection.InvokeStatic(
                            fixture.SeatAnswerType,
                            "Chi",
                            windowId,
                            parsedSeat,
                            chiOptionId.Value)
                        : fixture.Reflection.CreateInstance(
                            fixture.SeatAnswerType,
                            windowId,
                            parsedSeat,
                            Enum.Parse(fixture.SeatAnswerKindType, "Chi"));
                case "Daiminkan":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Daiminkan",
                        windowId,
                        parsedSeat);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static object CreateRawAnswer(
            Fixture fixture,
            int windowId,
            string seat,
            int answerKindValue)
        {
            return fixture.Reflection.CreateInstance(
                fixture.SeatAnswerType,
                windowId,
                fixture.DataFactory.ParseSeat(seat),
                Enum.ToObject(fixture.SeatAnswerKindType, answerKindValue));
        }

        private static object Register(Fixture fixture, object answers, object answer)
        {
            return fixture.Reflection.Invoke(answers, "TryRegister", answer);
        }

        private static object Resolve(
            Fixture fixture,
            object answers,
            params string[] activeSeats)
        {
            object resolver = fixture.Reflection.CreateInstance(fixture.SeatAnswerResolverType);
            return fixture.Reflection.Invoke(
                resolver,
                "Resolve",
                answers,
                CreateSeatList(fixture, activeSeats));
        }

        private static void AssertAccepted(object result)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(result, "Accepted"), Is.True);
            Assert.That((string)reflection.GetProperty(result, "Reason"), Is.Empty);
        }

        private static void AssertRejectedWithoutRegistration(
            Fixture fixture,
            object answers,
            object answer,
            string reason)
        {
            int answerCountBefore = RegisteredAnswerCount(fixture, answers);

            AssertRejected(Register(fixture, answers, answer), reason);

            Assert.That(RegisteredAnswerCount(fixture, answers), Is.EqualTo(answerCountBefore));
        }

        private static void AssertRejected(object result, string reason)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(result, "Accepted"), Is.False);
            Assert.That((string)reflection.GetProperty(result, "Reason"), Is.EqualTo(reason));
            Assert.That(reflection.GetProperty(result, "Answer"), Is.Null);
        }

        private static void AssertSelected(
            Fixture fixture,
            object resolution,
            int windowId,
            string seat,
            string kind,
            object candidate,
            int? chiOptionId)
        {
            Assert.That(fixture.Reflection.GetProperty(resolution, "Type").ToString(),
                Is.EqualTo("DeclarationSelected"));
            Assert.That((int)fixture.Reflection.GetProperty(resolution, "WindowId"), Is.EqualTo(windowId));
            Assert.That(fixture.Reflection.GetProperty(resolution, "SelectedSeat").ToString(),
                Is.EqualTo(seat));
            Assert.That(fixture.Reflection.GetProperty(resolution, "SelectedKind").ToString(),
                Is.EqualTo(kind));
            Assert.That(fixture.Reflection.GetProperty(resolution, "Candidate"), Is.SameAs(candidate));
            if (chiOptionId.HasValue)
            {
                Assert.That(
                    (int)fixture.Reflection.GetProperty(resolution, "ChiOptionId"),
                    Is.EqualTo(chiOptionId.Value));
            }
            else
            {
                Assert.That(fixture.Reflection.GetProperty(resolution, "ChiOptionId"), Is.Null);
            }
        }

        private static void AssertSeats(Fixture fixture, object seats, params string[] expectedSeats)
        {
            Assert.That(fixture.Collections.Count(seats), Is.EqualTo(expectedSeats.Length));
            for (int i = 0; i < expectedSeats.Length; i++)
            {
                Assert.That(fixture.Collections.Item(seats, i).ToString(), Is.EqualTo(expectedSeats[i]));
            }
        }

        private static int RegisteredAnswerCount(Fixture fixture, object answers)
        {
            return fixture.Collections.Count(fixture.Reflection.GetProperty(answers, "RegisteredAnswers"));
        }

        private static object FindRegisteredAnswer(Fixture fixture, object answers, string seat)
        {
            object registeredAnswers = fixture.Reflection.GetProperty(answers, "RegisteredAnswers");
            for (int i = 0; i < fixture.Collections.Count(registeredAnswers); i++)
            {
                object answer = fixture.Collections.Item(registeredAnswers, i);
                if (fixture.Reflection.GetProperty(answer, "Seat").ToString() == seat)
                    return answer;
            }

            return null;
        }

        private static IList CreateCandidateList(Fixture fixture, params object[] candidates)
        {
            return CreateTypedList(fixture.ReactionWindowCandidateType, candidates);
        }

        private static IList CreateSeatList(Fixture fixture, params string[] seats)
        {
            object[] parsedSeats = new object[seats.Length];
            for (int i = 0; i < seats.Length; i++)
                parsedSeats[i] = fixture.DataFactory.ParseSeat(seats[i]);

            return CreateTypedList(fixture.Types.SeatId, parsedSeats);
        }

        private static IList CreateTypedList(Type itemType, params object[] items)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
            for (int i = 0; i < items.Length; i++)
                list.Add(items[i]);

            return list;
        }

        private static object[] CopyCollection(Fixture fixture, object collection)
        {
            int count = fixture.Collections.Count(collection);
            object[] copiedItems = new object[count];
            for (int i = 0; i < count; i++)
                copiedItems[i] = fixture.Collections.Item(collection, i);

            return copiedItems;
        }

        private static string[] CandidateResponseStates(Fixture fixture, object[] candidates)
        {
            string[] states = new string[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
            {
                states[i] = fixture.Reflection.GetProperty(candidates[i], "ResponseState").ToString();
            }

            return states;
        }

        private static void AssertSameCollection(
            Fixture fixture,
            object[] expectedItems,
            object actualCollection)
        {
            Assert.That(fixture.Collections.Count(actualCollection), Is.EqualTo(expectedItems.Length));
            for (int i = 0; i < expectedItems.Length; i++)
            {
                Assert.That(fixture.Collections.Item(actualCollection, i), Is.SameAs(expectedItems[i]));
            }
        }

        private sealed class Fixture
        {
            public Fixture(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory,
                Type reactionWindowType,
                Type reactionWindowSourceType,
                Type reactionWindowCandidateType,
                Type reactionKindType,
                Type chiOptionType,
                Type winCheckResultType,
                Type winDeclarationEvaluationResultType,
                Type seatAnswerType,
                Type seatAnswerKindType,
                Type seatAnswerCollectionType,
                Type seatAnswerResolverType)
            {
                Reflection = reflection;
                Collections = collections;
                Types = types;
                DataFactory = dataFactory;
                ReactionWindowType = reactionWindowType;
                ReactionWindowSourceType = reactionWindowSourceType;
                ReactionWindowCandidateType = reactionWindowCandidateType;
                ReactionKindType = reactionKindType;
                ChiOptionType = chiOptionType;
                WinCheckResultType = winCheckResultType;
                WinDeclarationEvaluationResultType = winDeclarationEvaluationResultType;
                SeatAnswerType = seatAnswerType;
                SeatAnswerKindType = seatAnswerKindType;
                SeatAnswerCollectionType = seatAnswerCollectionType;
                SeatAnswerResolverType = seatAnswerResolverType;
            }

            public ReflectionTestAccess Reflection { get; }
            public CollectionTestAccess Collections { get; }
            public MahjongTestTypes Types { get; }
            public MahjongTestDataFactory DataFactory { get; }
            public Type ReactionWindowType { get; }
            public Type ReactionWindowSourceType { get; }
            public Type ReactionWindowCandidateType { get; }
            public Type ReactionKindType { get; }
            public Type ChiOptionType { get; }
            public Type WinCheckResultType { get; }
            public Type WinDeclarationEvaluationResultType { get; }
            public Type SeatAnswerType { get; }
            public Type SeatAnswerKindType { get; }
            public Type SeatAnswerCollectionType { get; }
            public Type SeatAnswerResolverType { get; }
        }
    }
}
