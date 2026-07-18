using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ChiServiceTests
    {
        private const string ChiServiceTypeName =
            "MahjongPrototype.Services.ChiService, Assembly-CSharp";
        private const string ReactionKindTypeName =
            "MahjongPrototype.Domain.ReactionKind, Assembly-CSharp";
        private const string ReactionWindowTypeName =
            "MahjongPrototype.Domain.ReactionWindow, Assembly-CSharp";
        private const string ReactionWindowCandidateTypeName =
            "MahjongPrototype.Domain.ReactionWindowCandidate, Assembly-CSharp";
        private const string ChiOptionTypeName =
            "MahjongPrototype.Domain.ChiOption, Assembly-CSharp";
        private const string ChiDetailTypeName =
            "MahjongPrototype.Domain.ChiReactionWindowCandidateDetail, Assembly-CSharp";
        private const string WinCheckResultTypeName =
            "MahjongPrototype.Domain.WinCheckResult, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";
        private const string DiscardSourceTypeName =
            "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";

        [Test]
        public void CollectCandidates_UsesOnlyTheNextActiveSeat_AndKeepsAllChiOptionsTogether()
        {
            Fixture fixture = CreateFixture("East", "South", "West", "North");
            SetAllParticipantsToLocalHuman(fixture, "East", "South", "West", "North");
            AddHandTiles(fixture, "East", "3m", "4m", "6m", "7m");
            AddHandTiles(fixture, "South", "3m", "4m", "6m", "7m");
            AddHandTiles(fixture, "North", "3m", "3m", "4m", "6m", "7m");

            object candidates = CollectCandidates(fixture, "West", "5m");

            Assert.That(fixture.Collections.Count(candidates), Is.EqualTo(1));
            object candidate = fixture.Collections.Item(candidates, 0);
            Assert.That(fixture.Reflection.GetProperty(candidate, "Seat").ToString(), Is.EqualTo("North"));
            Assert.That(fixture.Reflection.GetProperty(candidate, "Kind").ToString(), Is.EqualTo("Chi"));

            object chiDetail = fixture.Reflection.GetProperty(candidate, "ChiDetail");
            Assert.That(chiDetail, Is.Not.Null);
            Assert.That(TileCode(fixture, fixture.Reflection.GetProperty(chiDetail, "CalledTile")), Is.EqualTo("5m"));

            object options = fixture.Reflection.GetProperty(chiDetail, "Options");
            Assert.That(fixture.Collections.Count(options), Is.EqualTo(3));
            AssertOption(fixture, options, 0, 3, "3m 4m", "3m 4m 5m");
            AssertOption(fixture, options, 1, 4, "4m 6m", "4m 5m 6m");
            AssertOption(fixture, options, 2, 5, "6m 7m", "5m 6m 7m");
        }

        [Test]
        public void CollectCandidates_DoesNotUseTilesHeldByNonEligibleSeats()
        {
            Fixture fixture = CreateFixture("East", "South", "West", "North");
            AddHandTiles(fixture, "East", "3m", "4m");
            fixture.DataFactory.SetParticipantType(
                fixture.GameState,
                "East",
                "LocalHuman");
            fixture.DataFactory.SetParticipantType(
                fixture.GameState,
                "North",
                "LocalHuman");

            object candidates = CollectCandidates(fixture, "West", "5m");

            Assert.That(fixture.Collections.Count(candidates), Is.EqualTo(0));
        }

        [Test]
        public void CollectCandidates_WrapsFromNorthToEastUsingActiveTurnOrder()
        {
            Fixture fixture = CreateFixture("East", "South", "West", "North");
            AddHandTiles(fixture, "East", "2m", "3m");

            object candidates = CollectCandidates(fixture, "North", "1m");

            Assert.That(fixture.Collections.Count(candidates), Is.EqualTo(1));
            object candidate = fixture.Collections.Item(candidates, 0);
            Assert.That(fixture.Reflection.GetProperty(candidate, "Seat").ToString(), Is.EqualTo("East"));
            object options = fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(candidate, "ChiDetail"),
                "Options");
            AssertOption(fixture, options, 0, 1, "2m 3m", "1m 2m 3m");
        }

        [TestCase("1m", "2m 3m", 1, "1m 2m 3m")]
        [TestCase("9m", "7m 8m", 7, "7m 8m 9m")]
        public void CollectCandidates_EnumeratesOnlyInRangeSequences(
            string calledTile,
            string handTiles,
            int optionId,
            string meldTiles)
        {
            Fixture fixture = CreateEligibleFixture("North");
            AddHandTiles(fixture, "North", SplitTiles(handTiles));

            object candidates = CollectCandidates(fixture, "West", calledTile);

            Assert.That(fixture.Collections.Count(candidates), Is.EqualTo(1));
            object options = fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(fixture.Collections.Item(candidates, 0), "ChiDetail"),
                "Options");
            Assert.That(fixture.Collections.Count(options), Is.EqualTo(1));
            string expectedHandTiles = calledTile == "1m" ? "2m 3m" : "7m 8m";
            AssertOption(fixture, options, 0, optionId, expectedHandTiles, meldTiles);
        }

        [TestCase("E", "3m 4m")]
        [TestCase("5m", "3p 4p")]
        [TestCase("5m", "3m")]
        public void CollectCandidates_RejectsInvalidOrIncompleteChiShapes(
            string calledTile,
            string handTiles)
        {
            Fixture fixture = CreateEligibleFixture("North");
            AddHandTiles(fixture, "North", SplitTiles(handTiles));

            object candidates = CollectCandidates(fixture, "West", calledTile);

            Assert.That(fixture.Collections.Count(candidates), Is.EqualTo(0));
        }

        [Test]
        public void CollectCandidates_RejectsLastLiveWallDiscardAndIneligiblePlayerStates()
        {
            Fixture fixture = CreateEligibleFixture("North");
            AddHandTiles(fixture, "North", "3m", "4m");

            object lastLiveWallDiscard = CreateDiscard(
                fixture,
                "West",
                "5m",
                isLastLiveWallDiscard: true);
            Assert.That(
                fixture.Collections.Count(CollectCandidates(fixture, lastLiveWallDiscard)),
                Is.EqualTo(0));

            fixture.Reflection.Invoke(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, "North"),
                "DeclareReach",
                1);
            Assert.That(fixture.Collections.Count(CollectCandidates(fixture, "West", "5m")), Is.EqualTo(0));

            Fixture drawnTileFixture = CreateEligibleFixture("North");
            AddHandTiles(drawnTileFixture, "North", "3m", "4m");
            drawnTileFixture.DataFactory.SetDrawnTile(drawnTileFixture.GameState, "North", "1p");
            Assert.That(
                drawnTileFixture.Collections.Count(CollectCandidates(drawnTileFixture, "West", "5m")),
                Is.EqualTo(0));

            Fixture cpuFixture = CreateFixture("East", "South", "West", "North");
            AddHandTiles(cpuFixture, "North", "3m", "4m");
            Assert.That(
                cpuFixture.Collections.Count(CollectCandidates(cpuFixture, "West", "5m")),
                Is.EqualTo(0));
        }

        [Test]
        public void CollectCandidates_RequiresAtLeastTwoActiveSeats()
        {
            Fixture fixture = CreateFixture("East");
            AddHandTiles(fixture, "East", "3m", "4m");

            object candidates = CollectCandidates(fixture, "East", "5m");

            Assert.That(fixture.Collections.Count(candidates), Is.EqualTo(0));
        }

        [Test]
        public void ChiOptionAndDetail_ValidateAndDefensivelyCopyInputCollections()
        {
            Fixture fixture = CreateFixture("East");
            IList handTiles = CreateTileList(fixture, "3m", "4m");
            IList meldTiles = CreateTileList(fixture, "3m", "4m", "5m");
            object option = CreateChiOption(fixture, 3, "5m", handTiles, meldTiles);

            handTiles[0] = fixture.DataFactory.CreateTile("1m");
            meldTiles[0] = fixture.DataFactory.CreateTile("1m");
            Assert.That(TileCodes(fixture, fixture.Reflection.GetProperty(option, "HandTiles")), Is.EqualTo("3m 4m"));
            Assert.That(TileCodes(fixture, fixture.Reflection.GetProperty(option, "MeldTiles")), Is.EqualTo("3m 4m 5m"));

            IList options = CreateOptionList(fixture, option);
            object detail = CreateChiDetail(fixture, "5m", options);
            options.Clear();
            Assert.That(
                fixture.Collections.Count(fixture.Reflection.GetProperty(detail, "Options")),
                Is.EqualTo(1));

            Assert.Throws<TargetInvocationException>(() => CreateChiOption(
                fixture,
                3,
                "5m",
                CreateTileList(fixture, "3m", "4m"),
                CreateTileList(fixture, "3m", "4m", "6m")));
            Assert.Throws<TargetInvocationException>(() =>
                CreateChiDetail(fixture, "5m", CreateOptionList(fixture)));
            Assert.Throws<TargetInvocationException>(() => fixture.Reflection.CreateInstance(
                fixture.ChiDetailType,
                fixture.DataFactory.CreateTile("5m"),
                null));
            Assert.Throws<TargetInvocationException>(() =>
                CreateChiDetail(fixture, "5m", CreateOptionList(fixture, option, option)));

            object ponKind = Enum.Parse(fixture.ReactionKindType, "Pon");
            Assert.Throws<TargetInvocationException>(() => fixture.Reflection.CreateInstance(
                fixture.ReactionWindowCandidateType,
                fixture.DataFactory.ParseSeat("East"),
                ponKind,
                detail));
        }

        [Test]
        public void ReactionWindow_PrioritizesRonThenPonThenChi()
        {
            Fixture fixture = CreateFixture("East");
            object chiCandidate = CreateChiCandidate(fixture, "East", "5m", 3, "3m", "4m", "3m", "4m", "5m");
            object ponCandidate = fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowCandidateType,
                "CreatePon",
                fixture.DataFactory.ParseSeat("East"),
                fixture.DataFactory.CreateTile("5m"));
            object ronCandidate = CreateRonCandidate(fixture, "East");
            IList candidates = CreateCandidateList(fixture, chiCandidate, ponCandidate, ronCandidate);
            object reactionWindow = fixture.Reflection.CreateInstance(
                fixture.ReactionWindowType,
                1,
                CreateDiscard(fixture, "West", "5m", false),
                7,
                candidates);

            Assert.That(fixture.Reflection.GetProperty(reactionWindow, "PendingCandidate"), Is.SameAs(ronCandidate));
            Assert.That(fixture.Reflection.GetProperty(reactionWindow, "PendingChiCandidate"), Is.Null);

            fixture.Reflection.Invoke(ronCandidate, "Decline");
            Assert.That(fixture.Reflection.GetProperty(reactionWindow, "PendingCandidate"), Is.SameAs(ponCandidate));
            Assert.That(fixture.Reflection.GetProperty(reactionWindow, "PendingChiCandidate"), Is.Null);

            fixture.Reflection.Invoke(ponCandidate, "Decline");
            Assert.That(fixture.Reflection.GetProperty(reactionWindow, "PendingCandidate"), Is.SameAs(chiCandidate));
            Assert.That(fixture.Reflection.GetProperty(reactionWindow, "PendingChiCandidate"), Is.SameAs(chiCandidate));
        }

        private static Fixture CreateEligibleFixture(string eligibleSeat)
        {
            Fixture fixture = CreateFixture("East", "South", "West", "North");
            fixture.DataFactory.SetParticipantType(
                fixture.GameState,
                eligibleSeat,
                "LocalHuman");
            return fixture;
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
                reflection.RequireType(ChiServiceTypeName),
                reflection.RequireType(ReactionKindTypeName),
                reflection.RequireType(ReactionWindowTypeName),
                reflection.RequireType(ReactionWindowCandidateTypeName),
                reflection.RequireType(ChiOptionTypeName),
                reflection.RequireType(ChiDetailTypeName),
                reflection.RequireType(WinCheckResultTypeName),
                reflection.RequireType(WinDeclarationEvaluationResultTypeName),
                reflection.RequireType(DiscardSourceTypeName));
        }

        private static void SetAllParticipantsToLocalHuman(Fixture fixture, params string[] seatNames)
        {
            for (int i = 0; i < seatNames.Length; i++)
            {
                fixture.DataFactory.SetParticipantType(
                    fixture.GameState,
                    seatNames[i],
                    "LocalHuman");
            }
        }

        private static void AddHandTiles(Fixture fixture, string seatName, params string[] tileCodes)
        {
            fixture.DataFactory.AddHandTiles(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                tileCodes);
        }

        private static object CollectCandidates(Fixture fixture, string sourceSeat, string tileCode)
        {
            return CollectCandidates(fixture, CreateDiscard(fixture, sourceSeat, tileCode, false));
        }

        private static object CollectCandidates(Fixture fixture, object sourceDiscard)
        {
            object service = fixture.Reflection.CreateInstance(fixture.ChiServiceType);
            return fixture.Reflection.Invoke(service, "CollectCandidates", fixture.GameState, sourceDiscard);
        }

        private static object CreateDiscard(
            Fixture fixture,
            string sourceSeat,
            string tileCode,
            bool isLastLiveWallDiscard)
        {
            return fixture.Reflection.CreateInstance(
                fixture.Types.DiscardRecord,
                fixture.DataFactory.ParseSeat(sourceSeat),
                fixture.DataFactory.CreateTile(tileCode),
                7,
                Enum.Parse(fixture.DiscardSourceType, "Hand"),
                isLastLiveWallDiscard);
        }

        private static void AssertOption(
            Fixture fixture,
            object options,
            int index,
            int optionId,
            string expectedHandTiles,
            string expectedMeldTiles)
        {
            object option = fixture.Collections.Item(options, index);
            Assert.That((int)fixture.Reflection.GetProperty(option, "OptionId"), Is.EqualTo(optionId));
            Assert.That(
                TileCodes(fixture, fixture.Reflection.GetProperty(option, "HandTiles")),
                Is.EqualTo(expectedHandTiles));
            Assert.That(
                TileCodes(fixture, fixture.Reflection.GetProperty(option, "MeldTiles")),
                Is.EqualTo(expectedMeldTiles));
        }

        private static string TileCodes(Fixture fixture, object tiles)
        {
            int count = fixture.Collections.Count(tiles);
            string[] codes = new string[count];
            for (int i = 0; i < count; i++)
                codes[i] = TileCode(fixture, fixture.Collections.Item(tiles, i));

            return string.Join(" ", codes);
        }

        private static string TileCode(Fixture fixture, object tile)
        {
            return (string)fixture.Reflection.GetProperty(tile, "Code");
        }

        private static IList CreateTileList(Fixture fixture, params string[] tileCodes)
        {
            IList tiles = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(fixture.Types.Tile));
            for (int i = 0; i < tileCodes.Length; i++)
                tiles.Add(fixture.DataFactory.CreateTile(tileCodes[i]));

            return tiles;
        }

        private static IList CreateOptionList(Fixture fixture, params object[] options)
        {
            IList copiedOptions = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(fixture.ChiOptionType));
            for (int i = 0; i < options.Length; i++)
                copiedOptions.Add(options[i]);

            return copiedOptions;
        }

        private static IList CreateCandidateList(Fixture fixture, params object[] candidates)
        {
            IList copiedCandidates = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(fixture.ReactionWindowCandidateType));
            for (int i = 0; i < candidates.Length; i++)
                copiedCandidates.Add(candidates[i]);

            return copiedCandidates;
        }

        private static object CreateChiOption(
            Fixture fixture,
            int optionId,
            string calledTile,
            IList handTiles,
            IList meldTiles)
        {
            return fixture.Reflection.CreateInstance(
                fixture.ChiOptionType,
                optionId,
                fixture.DataFactory.CreateTile(calledTile),
                handTiles,
                meldTiles);
        }

        private static object CreateChiDetail(Fixture fixture, string calledTile, IList options)
        {
            return fixture.Reflection.CreateInstance(
                fixture.ChiDetailType,
                fixture.DataFactory.CreateTile(calledTile),
                options);
        }

        private static object CreateChiCandidate(
            Fixture fixture,
            string seat,
            string calledTile,
            int optionId,
            string firstHandTile,
            string secondHandTile,
            string firstMeldTile,
            string secondMeldTile,
            string thirdMeldTile)
        {
            object option = CreateChiOption(
                fixture,
                optionId,
                calledTile,
                CreateTileList(fixture, firstHandTile, secondHandTile),
                CreateTileList(fixture, firstMeldTile, secondMeldTile, thirdMeldTile));
            return fixture.Reflection.InvokeStatic(
                fixture.ReactionWindowCandidateType,
                "CreateChi",
                fixture.DataFactory.ParseSeat(seat),
                fixture.DataFactory.CreateTile(calledTile),
                CreateOptionList(fixture, option));
        }

        private static object CreateRonCandidate(Fixture fixture, string seat)
        {
            object notWin = fixture.Reflection.GetStaticProperty(fixture.WinCheckResultType, "NotWin");
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

        private static string[] SplitTiles(string tileCodes)
        {
            return tileCodes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private sealed class Fixture
        {
            public Fixture(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory,
                object gameState,
                Type chiServiceType,
                Type reactionKindType,
                Type reactionWindowType,
                Type reactionWindowCandidateType,
                Type chiOptionType,
                Type chiDetailType,
                Type winCheckResultType,
                Type winDeclarationEvaluationResultType,
                Type discardSourceType)
            {
                Reflection = reflection;
                Collections = collections;
                Types = types;
                DataFactory = dataFactory;
                GameState = gameState;
                ChiServiceType = chiServiceType;
                ReactionKindType = reactionKindType;
                ReactionWindowType = reactionWindowType;
                ReactionWindowCandidateType = reactionWindowCandidateType;
                ChiOptionType = chiOptionType;
                ChiDetailType = chiDetailType;
                WinCheckResultType = winCheckResultType;
                WinDeclarationEvaluationResultType = winDeclarationEvaluationResultType;
                DiscardSourceType = discardSourceType;
            }

            public ReflectionTestAccess Reflection { get; }
            public CollectionTestAccess Collections { get; }
            public MahjongTestTypes Types { get; }
            public MahjongTestDataFactory DataFactory { get; }
            public object GameState { get; }
            public Type ChiServiceType { get; }
            public Type ReactionKindType { get; }
            public Type ReactionWindowType { get; }
            public Type ReactionWindowCandidateType { get; }
            public Type ChiOptionType { get; }
            public Type ChiDetailType { get; }
            public Type WinCheckResultType { get; }
            public Type WinDeclarationEvaluationResultType { get; }
            public Type DiscardSourceType { get; }
        }
    }
}
