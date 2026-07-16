using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReactionWindowSeatAnswerDeclarationServiceTests
    {
        private const string DeclarationServiceTypeName =
            "MahjongPrototype.Services.ReactionWindowSeatAnswerDeclarationService, Assembly-CSharp";
        private const string ReactionKindTypeName =
            "MahjongPrototype.Domain.ReactionKind, Assembly-CSharp";
        private const string ReactionWindowCandidateTypeName =
            "MahjongPrototype.Domain.ReactionWindowCandidate, Assembly-CSharp";
        private const string ChiOptionTypeName =
            "MahjongPrototype.Domain.ChiOption, Assembly-CSharp";
        private const string WinCheckResultTypeName =
            "MahjongPrototype.Domain.WinCheckResult, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";
        private const string SeatAnswerTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswer, Assembly-CSharp";
        private const string SeatAnswerCollectionTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerCollection, Assembly-CSharp";
        private const string SeatAnswerResolverTypeName =
            "MahjongPrototype.Services.ReactionWindowSeatAnswerResolver, Assembly-CSharp";
        private const string SeatAnswerResolutionTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerResolution, Assembly-CSharp";
        private const string SelfKanCandidateTypeName =
            "MahjongPrototype.Domain.SelfKanCandidate, Assembly-CSharp";
        private const string SelfKanKindTypeName =
            "MahjongPrototype.Domain.SelfKanKind, Assembly-CSharp";
        private const string SelfKanTileLocationTypeName =
            "MahjongPrototype.Domain.SelfKanTileLocation, Assembly-CSharp";

        [Test]
        public void Prepare_RejectsAResolutionWhileAnyTargetSeatIsUnanswered()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", ponCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            object resolution = Resolve(fixture, answers);

            object preparation = Prepare(fixture, answers, resolution);

            AssertPreparationRejected(preparation, "ReactionAnswersPending");
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(2));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Pending"));
            Assert.That(fixture.Reflection.GetProperty(window, "State").ToString(),
                Is.EqualTo("AcceptingAnswers"));
        }

        [Test]
        public void Prepare_RejectsAResolutionWithADetachedCandidateWithoutMutation()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object windowCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", windowCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            object answer = CreateAnswer(fixture, window, "East", "Pon");
            RegisterAccepted(fixture, answers, answer);
            object detachedCandidate = CreatePonCandidate(fixture, "East", "5m");
            object forgedResolution = fixture.Reflection.InvokeStatic(
                fixture.SeatAnswerResolutionType,
                "DeclarationSelected",
                fixture.Reflection.GetProperty(window, "WindowId"),
                fixture.Reflection.GetProperty(window, "Source"),
                answer,
                detachedCandidate);

            object preparation = Prepare(fixture, answers, forgedResolution);

            AssertPreparationRejected(preparation, "ReactionCandidateMismatch");
            AssertMeldPreparationIsPure(
                fixture,
                window,
                windowCandidate,
                null,
                expectedHandCount: 2);
            Assert.That(CandidateState(fixture, detachedCandidate), Is.EqualTo("Pending"));
            Assert.That(FindRegisteredAnswer(fixture, answers, "East"), Is.SameAs(answer));
        }

        [Test]
        public void Pon_PrepareIsPureAndCommitAppliesOnlyTheSelectedSeatAnswerOnce()
        {
            Fixture fixture = CreateFixture("East", "South", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object passedRonCandidate = CreateRonCandidate(fixture, "South");
            object window = BeginDiscardWindow(
                fixture,
                "West",
                "5m",
                ponCandidate,
                passedRonCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            object ponAnswer = CreateAnswer(fixture, window, "East", "Pon");
            object passAnswer = CreateAnswer(fixture, window, "South", "Pass");
            RegisterAccepted(fixture, answers, ponAnswer);
            RegisterAccepted(fixture, answers, passAnswer);
            object resolution = Resolve(fixture, answers);
            object currentTurnBefore = fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn");
            int turnIndexBefore = (int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex");

            object preparation = Prepare(fixture, answers, resolution);
            object preparedDeclaration = AssertPrepared(preparation);

            Assert.That(fixture.Reflection.GetProperty(preparedDeclaration, "ReactionKind").ToString(),
                Is.EqualTo("Pon"));
            AssertMeldPreparationIsPure(
                fixture,
                window,
                ponCandidate,
                passedRonCandidate,
                expectedHandCount: 2);

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "PonDeclared", ponCandidate);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(MeldTypeAt(fixture, "East", 0), Is.EqualTo("Pon"));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(1));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.True);
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Declared"));
            Assert.That(CandidateState(fixture, passedRonCandidate), Is.EqualTo("Declined"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn"),
                Is.EqualTo(currentTurnBefore));
            Assert.That((int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex"),
                Is.EqualTo(turnIndexBefore));
            Assert.That(FindRegisteredAnswer(fixture, answers, "East"), Is.SameAs(ponAnswer));
            Assert.That(FindRegisteredAnswer(fixture, answers, "South"), Is.SameAs(passAnswer));

            object secondCommit = Commit(fixture, preparedDeclaration);
            AssertCommitRejected(secondCommit, "ReactionPreparationAlreadyCommitted");
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(1));
        }

        [Test]
        public void Chi_PrepareIsPureAndCommitAppliesTheSelectedOption()
        {
            Fixture fixture = CreateFixture("East", "South", "West");
            AddHandTiles(fixture, "East", "3m", "4m");
            object chiCandidate = CreateChiCandidate(fixture, "East", 3);
            object window = BeginDiscardWindow(fixture, "West", "5m", chiCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Chi", 3));
            object resolution = Resolve(fixture, answers);

            object preparation = Prepare(fixture, answers, resolution);
            object preparedDeclaration = AssertPrepared(preparation);

            Assert.That((int)fixture.Reflection.GetProperty(preparedDeclaration, "ChiOptionId"),
                Is.EqualTo(3));
            AssertMeldPreparationIsPure(
                fixture,
                window,
                chiCandidate,
                null,
                expectedHandCount: 2);

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "ChiDeclared", chiCandidate);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(MeldTypeAt(fixture, "East", 0), Is.EqualTo("Chi"));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(1));
            Assert.That(CandidateState(fixture, chiCandidate), Is.EqualTo("Declared"));
        }

        [Test]
        public void Daiminkan_PrepareIsPureAndCommitAppliesTheSelectedDeclaration()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m", "5m");
            object daiminkanCandidate = CreateDaiminkanCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", daiminkanCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Daiminkan"));
            object resolution = Resolve(fixture, answers);

            object preparation = Prepare(fixture, answers, resolution);
            object preparedDeclaration = AssertPrepared(preparation);

            AssertMeldPreparationIsPure(
                fixture,
                window,
                daiminkanCandidate,
                null,
                expectedHandCount: 3);

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "DaiminkanDeclared", daiminkanCandidate);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(MeldTypeAt(fixture, "East", 0), Is.EqualTo("Daiminkan"));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(1));
            Assert.That(CandidateState(fixture, daiminkanCandidate), Is.EqualTo("Declared"));
        }

        [Test]
        public void RonCommit_ReturnsRonResolutionWithoutEndingTheRoundOrAdvancingTheTurn()
        {
            Fixture fixture = CreateFixture("East", "South", "West");
            object ronCandidate = CreateRonCandidate(fixture, "East");
            object passedPonCandidate = CreatePonCandidate(fixture, "South", "5m");
            object window = BeginDiscardWindow(
                fixture,
                "West",
                "5m",
                ronCandidate,
                passedPonCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Ron"));
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "South", "Pass"));
            object resolution = Resolve(fixture, answers);
            object currentTurnBefore = fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn");
            int turnIndexBefore = (int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex");

            object preparation = Prepare(fixture, answers, resolution);
            object preparedDeclaration = AssertPrepared(preparation);
            Assert.That(fixture.Reflection.GetProperty(preparedDeclaration, "WinDeclarationEvaluation"),
                Is.SameAs(fixture.Reflection.GetProperty(ronCandidate, "WinDeclarationEvaluation")));
            Assert.That(CandidateState(fixture, ronCandidate), Is.EqualTo("Pending"));

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "RonDeclared", ronCandidate);
            Assert.That(CandidateState(fixture, ronCandidate), Is.EqualTo("Declared"));
            Assert.That(CandidateState(fixture, passedPonCandidate), Is.EqualTo("Declined"));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "IsRoundEnded"), Is.False);
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentRoundResult"), Is.Null);
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "TurnPhase").ToString(),
                Is.EqualTo("ReactionWindow"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn"),
                Is.EqualTo(currentTurnBefore));
            Assert.That((int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex"),
                Is.EqualTo(turnIndexBefore));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
        }

        [Test]
        public void AllPass_CommitReturnsNoReactionWithoutChangingPlayerState()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", ponCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Pass"));
            object resolution = Resolve(fixture, answers);
            object currentTurnBefore = fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn");
            int turnIndexBefore = (int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex");

            object preparation = Prepare(fixture, answers, resolution);
            object preparedDeclaration = AssertPrepared(preparation);
            AssertMeldPreparationIsPure(
                fixture,
                window,
                ponCandidate,
                null,
                expectedHandCount: 2);

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "NoReaction", null);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(2));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Declined"));
            Assert.That(fixture.Reflection.GetProperty(window, "State").ToString(),
                Is.EqualTo("AcceptingAnswers"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn"),
                Is.EqualTo(currentTurnBefore));
            Assert.That((int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex"),
                Is.EqualTo(turnIndexBefore));
        }

        [Test]
        public void Commit_RejectsPreparationWhoseHandPreconditionChangedWithoutPartialWrites()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", ponCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Pon"));
            object preparation = Prepare(fixture, answers, Resolve(fixture, answers));
            object preparedDeclaration = AssertPrepared(preparation);
            object hand = fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, "East"),
                "Hand");
            Assert.That((bool)fixture.Reflection.Invoke(hand, "TryRemoveAt", 0, null), Is.True);

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitRejected(commit, "ReactionPreparationStale");
            Assert.That((bool)fixture.Reflection.GetProperty(preparedDeclaration, "IsCommitted"), Is.False);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Pending"));
            Assert.That(fixture.Reflection.GetProperty(window, "State").ToString(),
                Is.EqualTo("AcceptingAnswers"));
        }

        [Test]
        public void Commit_RejectsPreparationWhoseDiscardClaimPreconditionChangedWithoutPartialWrites()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", ponCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Pon"));
            object preparation = Prepare(fixture, answers, Resolve(fixture, answers));
            object preparedDeclaration = AssertPrepared(preparation);
            object preparedMeld = fixture.Reflection.GetProperty(preparedDeclaration, "Meld");
            Assert.That((bool)fixture.Reflection.Invoke(
                fixture.GameState,
                "TryClaimDiscard",
                preparedMeld), Is.True);

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitRejected(commit, "ReactionPreparationStale");
            Assert.That((bool)fixture.Reflection.GetProperty(preparedDeclaration, "IsCommitted"), Is.False);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(2));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(1));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Pending"));
            Assert.That(fixture.Reflection.GetProperty(window, "State").ToString(),
                Is.EqualTo("AcceptingAnswers"));
        }

        [Test]
        public void Commit_RejectsPreparationWhoseCallerParticipantTypeChangedWithoutPartialWrites()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", ponCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Pon"));
            object preparation = Prepare(fixture, answers, Resolve(fixture, answers));
            object preparedDeclaration = AssertPrepared(preparation);
            fixture.DataFactory.SetParticipantType(fixture.GameState, "East", "Cpu");

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitRejected(commit, "ReactionPreparationStale");
            Assert.That((bool)fixture.Reflection.GetProperty(preparedDeclaration, "IsCommitted"), Is.False);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(2));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Pending"));
            Assert.That(fixture.Reflection.GetProperty(window, "State").ToString(),
                Is.EqualTo("AcceptingAnswers"));
        }

        [Test]
        public void Commit_RejectsAPreparationForAnOldWindowWithoutAdditionalWrites()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object ponCandidate = CreatePonCandidate(fixture, "East", "5m");
            object window = BeginDiscardWindow(fixture, "West", "5m", ponCandidate);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "East", "Pon"));
            object preparation = Prepare(fixture, answers, Resolve(fixture, answers));
            object preparedDeclaration = AssertPrepared(preparation);
            int windowId = (int)fixture.Reflection.GetProperty(window, "WindowId");

            Assert.That((bool)fixture.Reflection.Invoke(
                fixture.GameState,
                "CloseReactionWindow",
                windowId), Is.True);
            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitRejected(commit, "ReactionPreparationStale");
            Assert.That((bool)fixture.Reflection.GetProperty(preparedDeclaration, "IsCommitted"), Is.False);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(2));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
            Assert.That(CandidateState(fixture, ponCandidate), Is.EqualTo("Pending"));
        }

        [Test]
        public void KakanSource_RonCommitReturnsResolutionWithoutCommittingKakan()
        {
            Fixture fixture = CreateFixture("East", "South");
            object ronCandidate = CreateRonCandidate(fixture, "South");
            object pendingKakan;
            object window = BeginKakanWindow(fixture, ronCandidate, out pendingKakan);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "South", "Ron"));
            object preparation = Prepare(fixture, answers, Resolve(fixture, answers));
            object preparedDeclaration = AssertPrepared(preparation);
            object currentTurnBefore = fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn");

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "RonDeclared", ronCandidate);
            object committedResolution = fixture.Reflection.GetProperty(commit, "Resolution");
            Assert.That(fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(committedResolution, "Source"),
                "Kind").ToString(), Is.EqualTo("Kakan"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "PendingKakan"),
                Is.SameAs(pendingKakan));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentReactionWindow"),
                Is.SameAs(window));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "TurnPhase").ToString(),
                Is.EqualTo("ReactionWindow"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentTurn"),
                Is.EqualTo(currentTurnBefore));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "IsRoundEnded"), Is.False);
        }

        [Test]
        public void KakanSource_AllPassReturnsNoReactionWithoutCommittingKakan()
        {
            Fixture fixture = CreateFixture("East", "South");
            object ronCandidate = CreateRonCandidate(fixture, "South");
            object pendingKakan;
            object window = BeginKakanWindow(fixture, ronCandidate, out pendingKakan);
            object answers = CreateAnswerCollection(fixture, window);
            RegisterAccepted(fixture, answers, CreateAnswer(fixture, window, "South", "Pass"));
            object preparation = Prepare(fixture, answers, Resolve(fixture, answers));
            object preparedDeclaration = AssertPrepared(preparation);
            int eastMeldCountBefore = MeldCount(fixture, "East");
            object eastDrawnTileBefore = fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, "East"),
                "DrawnTile");

            object commit = Commit(fixture, preparedDeclaration);

            AssertCommitSucceeded(commit, "NoReaction", null);
            object committedResolution = fixture.Reflection.GetProperty(commit, "Resolution");
            Assert.That(fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(committedResolution, "Source"),
                "Kind").ToString(), Is.EqualTo("Kakan"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "PendingKakan"),
                Is.SameAs(pendingKakan));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "CurrentReactionWindow"),
                Is.SameAs(window));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(eastMeldCountBefore));
            Assert.That(fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, "East"),
                "DrawnTile"), Is.EqualTo(eastDrawnTileBefore));
            Assert.That(CandidateState(fixture, ronCandidate), Is.EqualTo("Declined"));
            Assert.That(fixture.Reflection.GetProperty(fixture.GameState, "TurnPhase").ToString(),
                Is.EqualTo("ReactionWindow"));
        }

        private static Fixture CreateFixture(params string[] seatNames)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new Fixture(
                reflection,
                new CollectionTestAccess(reflection),
                types,
                dataFactory,
                dataFactory.CreateGameState(seatNames),
                reflection.RequireType(DeclarationServiceTypeName),
                reflection.RequireType(ReactionKindTypeName),
                reflection.RequireType(ReactionWindowCandidateTypeName),
                reflection.RequireType(ChiOptionTypeName),
                reflection.RequireType(WinCheckResultTypeName),
                reflection.RequireType(WinDeclarationEvaluationResultTypeName),
                reflection.RequireType(SeatAnswerTypeName),
                reflection.RequireType(SeatAnswerCollectionTypeName),
                reflection.RequireType(SeatAnswerResolverTypeName),
                reflection.RequireType(SeatAnswerResolutionTypeName),
                reflection.RequireType(SelfKanCandidateTypeName),
                reflection.RequireType(SelfKanKindTypeName),
                reflection.RequireType(SelfKanTileLocationTypeName));
        }

        private static object BeginDiscardWindow(
            Fixture fixture,
            string sourceSeat,
            string tileCode,
            params object[] candidates)
        {
            object sourceDiscard = fixture.Reflection.Invoke(
                fixture.GameState,
                "AddDiscard",
                fixture.DataFactory.CreateDiscardRecord(sourceSeat, tileCode, 7));
            return fixture.Reflection.Invoke(
                fixture.GameState,
                "BeginReactionWindow",
                sourceDiscard,
                CreateCandidateList(fixture, candidates));
        }

        private static object BeginKakanWindow(
            Fixture fixture,
            object ronCandidate,
            out object pendingKakan)
        {
            fixture.DataFactory.SetCurrentTurn(fixture.GameState, "East");
            fixture.DataFactory.SetDrawnTile(fixture.GameState, "East", "5m");
            int turnIndex = (int)fixture.Reflection.GetProperty(fixture.GameState, "TurnIndex");
            pendingKakan = fixture.Reflection.CreateInstance(
                fixture.SelfKanCandidateType,
                Enum.Parse(fixture.SelfKanKindType, "Kakan"),
                fixture.DataFactory.ParseSeat("East"),
                fixture.DataFactory.CreateTile("5m"),
                Enum.Parse(fixture.SelfKanTileLocationType, "DrawnTile"),
                turnIndex,
                0,
                null);
            return fixture.Reflection.Invoke(
                fixture.GameState,
                "BeginKakanReactionWindow",
                pendingKakan,
                CreateCandidateList(fixture, ronCandidate));
        }

        private static object CreateAnswerCollection(Fixture fixture, object window)
        {
            return fixture.Reflection.CreateInstance(fixture.SeatAnswerCollectionType, window);
        }

        private static object Resolve(Fixture fixture, object answers)
        {
            object resolver = fixture.Reflection.CreateInstance(fixture.SeatAnswerResolverType);
            return fixture.Reflection.Invoke(
                resolver,
                "Resolve",
                answers,
                fixture.Reflection.GetProperty(fixture.GameState, "ActiveTurnSeats"));
        }

        private static object Prepare(Fixture fixture, object answers, object resolution)
        {
            return fixture.Reflection.Invoke(
                fixture.Reflection.CreateInstance(fixture.DeclarationServiceType),
                "Prepare",
                fixture.GameState,
                answers,
                resolution);
        }

        private static object Commit(Fixture fixture, object preparedDeclaration)
        {
            return fixture.Reflection.Invoke(
                fixture.Reflection.CreateInstance(fixture.DeclarationServiceType),
                "Commit",
                fixture.GameState,
                preparedDeclaration);
        }

        private static void RegisterAccepted(Fixture fixture, object answers, object answer)
        {
            object registration = fixture.Reflection.Invoke(answers, "TryRegister", answer);
            Assert.That((bool)fixture.Reflection.GetProperty(registration, "Accepted"), Is.True);
        }

        private static object CreateAnswer(
            Fixture fixture,
            object window,
            string seatName,
            string kindName,
            int? chiOptionId = null)
        {
            int windowId = (int)fixture.Reflection.GetProperty(window, "WindowId");
            object seat = fixture.DataFactory.ParseSeat(seatName);
            switch (kindName)
            {
                case "Pass":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Pass",
                        windowId,
                        seat);
                case "Ron":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Ron",
                        windowId,
                        seat);
                case "Pon":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Pon",
                        windowId,
                        seat);
                case "Chi":
                    if (!chiOptionId.HasValue)
                        throw new ArgumentException("A chi answer needs an option id.", nameof(chiOptionId));
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Chi",
                        windowId,
                        seat,
                        chiOptionId.Value);
                case "Daiminkan":
                    return fixture.Reflection.InvokeStatic(
                        fixture.SeatAnswerType,
                        "Daiminkan",
                        windowId,
                        seat);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kindName));
            }
        }

        private static object CreateRonCandidate(Fixture fixture, string seatName)
        {
            object evaluation = fixture.Reflection.InvokeStatic(
                fixture.WinDeclarationEvaluationResultType,
                "NotWinningShape",
                fixture.Reflection.GetStaticProperty(fixture.WinCheckResultType, "NotWin"));
            return fixture.Reflection.CreateInstance(
                fixture.ReactionWindowCandidateType,
                fixture.DataFactory.ParseSeat(seatName),
                Enum.Parse(fixture.ReactionKindType, "Ron"),
                evaluation);
        }

        private static object CreatePonCandidate(
            Fixture fixture,
            string seatName,
            string tileCode)
        {
            return fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowCandidateType,
                "CreatePon",
                fixture.DataFactory.ParseSeat(seatName),
                fixture.DataFactory.CreateTile(tileCode));
        }

        private static object CreateDaiminkanCandidate(
            Fixture fixture,
            string seatName,
            string tileCode)
        {
            return fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowCandidateType,
                "CreateDaiminkan",
                fixture.DataFactory.ParseSeat(seatName),
                fixture.DataFactory.CreateTile(tileCode));
        }

        private static object CreateChiCandidate(
            Fixture fixture,
            string seatName,
            int optionId)
        {
            object option = fixture.Reflection.CreateInstance(
                fixture.ChiOptionType,
                optionId,
                fixture.DataFactory.CreateTile("5m"),
                CreateTypedList(
                    fixture.Types.Tile,
                    fixture.DataFactory.CreateTile("3m"),
                    fixture.DataFactory.CreateTile("4m")),
                CreateTypedList(
                    fixture.Types.Tile,
                    fixture.DataFactory.CreateTile("3m"),
                    fixture.DataFactory.CreateTile("4m"),
                    fixture.DataFactory.CreateTile("5m")));
            return fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowCandidateType,
                "CreateChi",
                fixture.DataFactory.ParseSeat(seatName),
                fixture.DataFactory.CreateTile("5m"),
                CreateTypedList(fixture.ChiOptionType, option));
        }

        private static object AssertPrepared(object preparation)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(preparation, "Prepared"), Is.True);
            Assert.That((string)reflection.GetProperty(preparation, "Reason"), Is.Empty);
            object preparedDeclaration = reflection.GetProperty(preparation, "PreparedDeclaration");
            Assert.That(preparedDeclaration, Is.Not.Null);
            return preparedDeclaration;
        }

        private static void AssertPreparationRejected(object preparation, string expectedReason)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(preparation, "Prepared"), Is.False);
            Assert.That((string)reflection.GetProperty(preparation, "Reason"),
                Is.EqualTo(expectedReason));
            Assert.That(reflection.GetProperty(preparation, "PreparedDeclaration"), Is.Null);
        }

        private static void AssertCommitSucceeded(
            object commit,
            string expectedResolutionType,
            object expectedCandidate)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(commit, "Committed"), Is.True);
            Assert.That((string)reflection.GetProperty(commit, "Reason"), Is.Empty);
            object resolution = reflection.GetProperty(commit, "Resolution");
            Assert.That(reflection.GetProperty(resolution, "Type").ToString(),
                Is.EqualTo(expectedResolutionType));
            if (expectedCandidate == null)
                Assert.That(reflection.GetProperty(resolution, "Candidate"), Is.Null);
            else
                Assert.That(reflection.GetProperty(resolution, "Candidate"), Is.SameAs(expectedCandidate));
        }

        private static void AssertCommitRejected(object commit, string expectedReason)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(commit, "Committed"), Is.False);
            Assert.That((string)reflection.GetProperty(commit, "Reason"),
                Is.EqualTo(expectedReason));
        }

        private static void AssertMeldPreparationIsPure(
            Fixture fixture,
            object window,
            object selectedCandidate,
            object unselectedCandidate,
            int expectedHandCount)
        {
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(expectedHandCount));
            Assert.That(MeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(DiscardClaimCount(fixture), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
            Assert.That(CandidateState(fixture, selectedCandidate), Is.EqualTo("Pending"));
            if (unselectedCandidate != null)
                Assert.That(CandidateState(fixture, unselectedCandidate), Is.EqualTo("Pending"));
            Assert.That(fixture.Reflection.GetProperty(window, "State").ToString(),
                Is.EqualTo("AcceptingAnswers"));
        }

        private static void AddHandTiles(Fixture fixture, string seatName, params string[] tileCodes)
        {
            fixture.DataFactory.AddHandTiles(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                tileCodes);
        }

        private static int HandCount(Fixture fixture, string seatName)
        {
            return (int)fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(
                    fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                    "Hand"),
                "Count");
        }

        private static int MeldCount(Fixture fixture, string seatName)
        {
            return fixture.Collections.Count(fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                "Melds"));
        }

        private static string MeldTypeAt(Fixture fixture, string seatName, int index)
        {
            object melds = fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                "Melds");
            return fixture.Reflection.GetProperty(
                fixture.Collections.Item(melds, index),
                "Type").ToString();
        }

        private static int DiscardClaimCount(Fixture fixture)
        {
            return fixture.Collections.Count(
                fixture.Reflection.GetProperty(fixture.GameState, "DiscardClaims"));
        }

        private static string CandidateState(Fixture fixture, object candidate)
        {
            return fixture.Reflection.GetProperty(candidate, "ResponseState").ToString();
        }

        private static object FindRegisteredAnswer(
            Fixture fixture,
            object answers,
            string seatName)
        {
            object registeredAnswers = fixture.Reflection.GetProperty(answers, "RegisteredAnswers");
            for (int i = 0; i < fixture.Collections.Count(registeredAnswers); i++)
            {
                object answer = fixture.Collections.Item(registeredAnswers, i);
                if (fixture.Reflection.GetProperty(answer, "Seat").ToString() == seatName)
                    return answer;
            }

            return null;
        }

        private static IList CreateCandidateList(Fixture fixture, params object[] candidates)
        {
            return CreateTypedList(fixture.ReactionWindowCandidateType, candidates);
        }

        private static IList CreateTypedList(Type itemType, params object[] values)
        {
            IList list = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(itemType));
            for (int i = 0; i < values.Length; i++)
                list.Add(values[i]);

            return list;
        }

        private sealed class Fixture
        {
            public Fixture(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory,
                object gameState,
                Type declarationServiceType,
                Type reactionKindType,
                Type reactionWindowCandidateType,
                Type chiOptionType,
                Type winCheckResultType,
                Type winDeclarationEvaluationResultType,
                Type seatAnswerType,
                Type seatAnswerCollectionType,
                Type seatAnswerResolverType,
                Type seatAnswerResolutionType,
                Type selfKanCandidateType,
                Type selfKanKindType,
                Type selfKanTileLocationType)
            {
                Reflection = reflection;
                Collections = collections;
                Types = types;
                DataFactory = dataFactory;
                GameState = gameState;
                DeclarationServiceType = declarationServiceType;
                ReactionKindType = reactionKindType;
                ReactionWindowCandidateType = reactionWindowCandidateType;
                ChiOptionType = chiOptionType;
                WinCheckResultType = winCheckResultType;
                WinDeclarationEvaluationResultType = winDeclarationEvaluationResultType;
                SeatAnswerType = seatAnswerType;
                SeatAnswerCollectionType = seatAnswerCollectionType;
                SeatAnswerResolverType = seatAnswerResolverType;
                SeatAnswerResolutionType = seatAnswerResolutionType;
                SelfKanCandidateType = selfKanCandidateType;
                SelfKanKindType = selfKanKindType;
                SelfKanTileLocationType = selfKanTileLocationType;
            }

            public ReflectionTestAccess Reflection { get; }
            public CollectionTestAccess Collections { get; }
            public MahjongTestTypes Types { get; }
            public MahjongTestDataFactory DataFactory { get; }
            public object GameState { get; }
            public Type DeclarationServiceType { get; }
            public Type ReactionKindType { get; }
            public Type ReactionWindowCandidateType { get; }
            public Type ChiOptionType { get; }
            public Type WinCheckResultType { get; }
            public Type WinDeclarationEvaluationResultType { get; }
            public Type SeatAnswerType { get; }
            public Type SeatAnswerCollectionType { get; }
            public Type SeatAnswerResolverType { get; }
            public Type SeatAnswerResolutionType { get; }
            public Type SelfKanCandidateType { get; }
            public Type SelfKanKindType { get; }
            public Type SelfKanTileLocationType { get; }
        }
    }
}
