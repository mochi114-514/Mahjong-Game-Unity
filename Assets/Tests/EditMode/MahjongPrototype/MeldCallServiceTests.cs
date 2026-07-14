using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class MeldCallServiceTests
    {
        private const string MeldCallServiceTypeName =
            "MahjongPrototype.Services.MeldCallService, Assembly-CSharp";
        private const string PonServiceTypeName =
            "MahjongPrototype.Services.PonService, Assembly-CSharp";
        private const string ChiServiceTypeName =
            "MahjongPrototype.Services.ChiService, Assembly-CSharp";
        private const string PreparedMeldCallTypeName =
            "MahjongPrototype.Services.PreparedMeldCall, Assembly-CSharp";
        private const string MeldCallKindTypeName =
            "MahjongPrototype.Domain.MeldCallKind, Assembly-CSharp";
        private const string ReactionKindTypeName =
            "MahjongPrototype.Domain.ReactionKind, Assembly-CSharp";
        private const string ReactionWindowCandidateTypeName =
            "MahjongPrototype.Domain.ReactionWindowCandidate, Assembly-CSharp";
        private const string OpenMeldTypeName =
            "MahjongPrototype.Domain.OpenMeld, Assembly-CSharp";
        private const string OpenMeldKindTypeName =
            "MahjongPrototype.Domain.OpenMeldType, Assembly-CSharp";
        private const string WinCheckResultTypeName =
            "MahjongPrototype.Domain.WinCheckResult, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";

        [Test]
        public void AvailableKinds_ReportsOnlyTheCallsPresentInTheReactionWindow()
        {
            Fixture ponFixture = CreateFixture("East", "West");
            AddHandTiles(ponFixture, "East", "5m", "5m");
            object ponWindow = BeginMeldCallWindow(ponFixture, "West", "5m");
            Assert.That(AvailableKinds(ponFixture, ponWindow, "East"), Is.EqualTo("Pon"));

            Fixture chiFixture = CreateFixture("East", "West");
            AddHandTiles(chiFixture, "East", "3m", "4m");
            object chiWindow = BeginMeldCallWindow(chiFixture, "West", "5m");
            Assert.That(AvailableKinds(chiFixture, chiWindow, "East"), Is.EqualTo("Chi"));

            Fixture bothFixture = CreateFixture("East", "West");
            AddHandTiles(bothFixture, "East", "3m", "4m", "5m", "5m");
            object bothWindow = BeginMeldCallWindow(bothFixture, "West", "5m");
            Assert.That(AvailableKinds(bothFixture, bothWindow, "East"), Is.EqualTo("Pon Chi"));
        }

        [Test]
        public void AvailableKinds_PrioritizesPonFromAnotherSeat_ButKeepsOwnPonAndChiSelectable()
        {
            Fixture fixture = CreateFixture("East", "South", "North");
            fixture.DataFactory.SetParticipantType(fixture.GameState, "South", "LocalHuman");
            AddHandTiles(fixture, "East", "3m", "4m");
            AddHandTiles(fixture, "South", "5m", "5m");

            object reactionWindow = BeginMeldCallWindow(fixture, "North", "5m");

            Assert.That(AvailableKinds(fixture, reactionWindow, "South"), Is.EqualTo("Pon"));
            Assert.That(AvailableKinds(fixture, reactionWindow, "East"), Is.Empty);
        }

        [Test]
        public void TryDeclareChi_CommitsOnlySelectedOptionAndClosesOtherCallCandidates()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "3m", "4m", "5m", "5m");
            object reactionWindow = BeginMeldCallWindow(fixture, "West", "5m");
            int sourceDiscardId = (int)fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(reactionWindow, "SourceDiscard"),
                "Id");

            object result = TryDeclare(fixture, reactionWindow, "East", "Chi", 3);

            Assert.That((bool)fixture.Reflection.GetProperty(result, "Declared"), Is.True);
            Assert.That(fixture.Reflection.GetProperty(result, "Kind").ToString(), Is.EqualTo("Chi"));
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(2));
            Assert.That(HandTileCodes(fixture, "East"), Is.EqualTo("5m 5m"));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.True);
            Assert.That(OpenMeldCount(fixture, "East"), Is.EqualTo(1));

            object openMeld = OpenMeldAt(fixture, "East", 0);
            Assert.That(fixture.Reflection.GetProperty(openMeld, "Type").ToString(), Is.EqualTo("Chi"));
            Assert.That(TileCodes(fixture, fixture.Reflection.GetProperty(openMeld, "Tiles")), Is.EqualTo("3m 4m 5m"));
            Assert.That(
                (bool)fixture.Reflection.Invoke(
                    fixture.GameState,
                    "TryGetDiscardClaim",
                    sourceDiscardId,
                    null),
                Is.True);

            Assert.That(CandidateResponseState(fixture, reactionWindow, "Pon"), Is.EqualTo("Declined"));
            Assert.That(CandidateResponseState(fixture, reactionWindow, "Chi"), Is.EqualTo("Declared"));
            Assert.That(
                (bool)fixture.Reflection.GetProperty(
                    TryDeclare(fixture, reactionWindow, "East", "Pon", 0),
                    "Declared"),
                Is.False);
        }

        [Test]
        public void TryDeclarePon_UsesTheSharedCommitPath()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object reactionWindow = BeginMeldCallWindow(fixture, "West", "5m");

            object result = TryDeclare(fixture, reactionWindow, "East", "Pon", 0);

            Assert.That((bool)fixture.Reflection.GetProperty(result, "Declared"), Is.True);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(OpenMeldCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(
                fixture.Reflection.GetProperty(OpenMeldAt(fixture, "East", 0), "Type").ToString(),
                Is.EqualTo("Pon"));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.True);
        }

        [Test]
        public void TryDecline_DeclinesAllMeldKindsForTheSelectedSeat()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "3m", "4m", "5m", "5m");
            object reactionWindow = BeginMeldCallWindow(fixture, "West", "5m");

            object result = TryDecline(fixture, reactionWindow, "East");

            Assert.That((bool)fixture.Reflection.GetProperty(result, "Declined"), Is.True);
            Assert.That(CandidateResponseState(fixture, reactionWindow, "Pon"), Is.EqualTo("Declined"));
            Assert.That(CandidateResponseState(fixture, reactionWindow, "Chi"), Is.EqualTo("Declined"));
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(4));
            Assert.That(OpenMeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
        }

        [Test]
        public void TryDeclare_RejectsUnavailableKindsInvalidOptionsAndStaleWindowsWithoutMutation()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "3m", "4m");
            object reactionWindow = BeginMeldCallWindow(fixture, "West", "5m");

            AssertRejectedWithoutMutation(fixture, reactionWindow, "Pon", 0, 2);
            AssertRejectedWithoutMutation(fixture, reactionWindow, "Kan", 0, 2);
            AssertRejectedWithoutMutation(fixture, reactionWindow, "Chi", 9, 2);

            int windowId = (int)fixture.Reflection.GetProperty(reactionWindow, "WindowId");
            Assert.That((bool)fixture.Reflection.Invoke(fixture.GameState, "CloseReactionWindow", windowId), Is.True);
            AssertRejectedWithoutMutation(fixture, reactionWindow, "Chi", 3, 2);
        }

        [Test]
        public void TryDeclareChi_DoesNotPartiallyRemoveHandTilesWhenTheCandidateHasBecomeStale()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "3m", "4m");
            object reactionWindow = BeginMeldCallWindow(fixture, "West", "5m");
            object hand = fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, "East"),
                "Hand");

            Assert.That((bool)fixture.Reflection.Invoke(hand, "TryRemoveAt", 0, null), Is.True);
            AssertRejectedWithoutMutation(fixture, reactionWindow, "Chi", 3, 1);
            Assert.That(HandTileCodes(fixture, "East"), Is.EqualTo("4m"));
        }

        [Test]
        public void TryCommitMeldCall_RejectsMissingTilesAndClaimedDiscardsWithoutAdditionalMutation()
        {
            Fixture missingTilesFixture = CreateFixture("East", "West");
            AddHandTiles(missingTilesFixture, "East", "5m", "5m");
            object missingTilesWindow = BeginMeldCallWindow(missingTilesFixture, "West", "5m");
            object missingTilesCandidate = FindCandidate(missingTilesFixture, missingTilesWindow, "Pon");
            object missingTilesPreparedCall = PreparePon(
                missingTilesFixture,
                missingTilesWindow,
                missingTilesCandidate);
            object hand = missingTilesFixture.Reflection.GetProperty(
                missingTilesFixture.DataFactory.GetPlayerSeat(missingTilesFixture.GameState, "East"),
                "Hand");
            Assert.That((bool)missingTilesFixture.Reflection.Invoke(hand, "TryRemoveAt", 0, null), Is.True);

            Assert.That(
                TryCommitPreparedCall(
                    missingTilesFixture,
                    missingTilesWindow,
                    missingTilesPreparedCall),
                Is.False);
            AssertUncommittedState(
                missingTilesFixture,
                missingTilesWindow,
                missingTilesCandidate,
                expectedHandCount: 1,
                expectedClaimCount: 0);

            Fixture claimedDiscardFixture = CreateFixture("East", "West");
            AddHandTiles(claimedDiscardFixture, "East", "5m", "5m");
            object claimedDiscardWindow = BeginMeldCallWindow(claimedDiscardFixture, "West", "5m");
            object claimedDiscardCandidate = FindCandidate(claimedDiscardFixture, claimedDiscardWindow, "Pon");
            object claimedDiscardPreparedCall = PreparePon(
                claimedDiscardFixture,
                claimedDiscardWindow,
                claimedDiscardCandidate);
            Assert.That(
                (bool)claimedDiscardFixture.Reflection.Invoke(
                    claimedDiscardFixture.GameState,
                    "TryClaimDiscard",
                    claimedDiscardFixture.Reflection.GetProperty(claimedDiscardPreparedCall, "OpenMeld")),
                Is.True);

            Assert.That(
                TryCommitPreparedCall(
                    claimedDiscardFixture,
                    claimedDiscardWindow,
                    claimedDiscardPreparedCall),
                Is.False);
            AssertUncommittedState(
                claimedDiscardFixture,
                claimedDiscardWindow,
                claimedDiscardCandidate,
                expectedHandCount: 2,
                expectedClaimCount: 1);
        }

        [Test]
        public void TryCommitMeldCall_RejectsStaleAndDetachedCandidatesWithoutMutation()
        {
            Fixture staleFixture = CreateFixture("East", "West");
            AddHandTiles(staleFixture, "East", "5m", "5m");
            object staleWindow = BeginMeldCallWindow(staleFixture, "West", "5m");
            object staleCandidate = FindCandidate(staleFixture, staleWindow, "Pon");
            object stalePreparedCall = PreparePon(staleFixture, staleWindow, staleCandidate);
            int staleWindowId = (int)staleFixture.Reflection.GetProperty(staleWindow, "WindowId");
            Assert.That(
                (bool)staleFixture.Reflection.Invoke(
                    staleFixture.GameState,
                    "CloseReactionWindow",
                    staleWindowId),
                Is.True);

            Assert.That(TryCommitPreparedCall(staleFixture, staleWindow, stalePreparedCall), Is.False);
            AssertUncommittedState(
                staleFixture,
                staleWindow,
                staleCandidate,
                expectedHandCount: 2,
                expectedClaimCount: 0);

            Fixture detachedFixture = CreateFixture("East", "West");
            AddHandTiles(detachedFixture, "East", "5m", "5m");
            object detachedWindow = BeginMeldCallWindow(detachedFixture, "West", "5m");
            object detachedCandidate = detachedFixture.Reflection.InvokeStatic(
                detachedFixture.ReactionWindowCandidateType,
                "CreatePon",
                detachedFixture.DataFactory.ParseSeat("East"),
                detachedFixture.DataFactory.CreateTile("5m"));
            object detachedPreparedCall = PreparePon(
                detachedFixture,
                detachedWindow,
                detachedCandidate);

            Assert.That(
                TryCommitPreparedCall(detachedFixture, detachedWindow, detachedPreparedCall),
                Is.False);
            AssertUncommittedState(
                detachedFixture,
                detachedWindow,
                detachedCandidate,
                expectedHandCount: 2,
                expectedClaimCount: 0);
        }

        [Test]
        public void TryCommitMeldCall_RejectsInconsistentCallerAndSourceWithoutMutation()
        {
            Fixture fixture = CreateFixture("East", "South", "West");
            AddHandTiles(fixture, "East", "5m", "5m");
            object reactionWindow = BeginMeldCallWindow(fixture, "West", "5m");
            object candidate = FindCandidate(fixture, reactionWindow, "Pon");
            int sourceDiscardId = (int)fixture.Reflection.GetProperty(
                fixture.Reflection.GetProperty(reactionWindow, "SourceDiscard"),
                "Id");
            object forgedOpenMeld = fixture.Reflection.CreateInstance(
                fixture.Reflection.RequireType(OpenMeldTypeName),
                Enum.Parse(fixture.Reflection.RequireType(OpenMeldKindTypeName), "Pon"),
                fixture.DataFactory.CreateTileArray("5m", "5m", "5m"),
                fixture.DataFactory.ParseSeat("South"),
                fixture.DataFactory.ParseSeat("South"),
                fixture.DataFactory.CreateTile("5m"),
                sourceDiscardId);
            object forgedPreparedCall = fixture.Reflection.CreateInstance(
                fixture.Reflection.RequireType(PreparedMeldCallTypeName),
                candidate,
                fixture.DataFactory.CreateTileArray("5m", "5m"),
                forgedOpenMeld);

            Assert.That(TryCommitPreparedCall(fixture, reactionWindow, forgedPreparedCall), Is.False);
            AssertUncommittedState(
                fixture,
                reactionWindow,
                candidate,
                expectedHandCount: 2,
                expectedClaimCount: 0);
        }

        [Test]
        public void TryCommitMeldCall_AppliesPonAndChiOnlyOnce()
        {
            Fixture ponFixture = CreateFixture("East", "West");
            AddHandTiles(ponFixture, "East", "5m", "5m");
            object ponWindow = BeginMeldCallWindow(ponFixture, "West", "5m");
            object ponCandidate = FindCandidate(ponFixture, ponWindow, "Pon");
            object ponPreparedCall = PreparePon(ponFixture, ponWindow, ponCandidate);

            Assert.That(TryCommitPreparedCall(ponFixture, ponWindow, ponPreparedCall), Is.True);
            AssertCommittedState(ponFixture, ponCandidate, "Pon");
            Assert.That(TryCommitPreparedCall(ponFixture, ponWindow, ponPreparedCall), Is.False);
            AssertCommittedState(ponFixture, ponCandidate, "Pon");

            Fixture chiFixture = CreateFixture("East", "West");
            AddHandTiles(chiFixture, "East", "3m", "4m");
            object chiWindow = BeginMeldCallWindow(chiFixture, "West", "5m");
            object chiCandidate = FindCandidate(chiFixture, chiWindow, "Chi");
            object chiPreparedCall = PrepareChi(chiFixture, chiWindow, chiCandidate, 3);

            Assert.That(TryCommitPreparedCall(chiFixture, chiWindow, chiPreparedCall), Is.True);
            AssertCommittedState(chiFixture, chiCandidate, "Chi");
            Assert.That(TryCommitPreparedCall(chiFixture, chiWindow, chiPreparedCall), Is.False);
            AssertCommittedState(chiFixture, chiCandidate, "Chi");
        }

        [Test]
        public void AvailableKinds_HidesAllMeldCallsWhileRonIsPending()
        {
            Fixture fixture = CreateFixture("East", "West");
            AddHandTiles(fixture, "East", "3m", "4m", "5m", "5m");
            object sourceDiscard = AddDiscard(fixture, "West", "5m");
            object callCandidates = CollectCandidates(fixture, sourceDiscard);
            IList candidates = CreateCandidateList(fixture, callCandidates);
            candidates.Add(CreateRonCandidate(fixture, "East"));
            object reactionWindow = fixture.Reflection.Invoke(
                fixture.GameState,
                "BeginReactionWindow",
                sourceDiscard,
                candidates);

            Assert.That(AvailableKinds(fixture, reactionWindow, "East"), Is.Empty);
        }

        private static object PreparePon(
            Fixture fixture,
            object reactionWindow,
            object candidate)
        {
            object[] arguments =
            {
                fixture.GameState,
                reactionWindow,
                candidate,
                null,
                null
            };
            bool prepared = (bool)fixture.Reflection.Invoke(
                fixture.Reflection.CreateInstance(
                    fixture.Reflection.RequireType(PonServiceTypeName)),
                "TryPrepareDeclaration",
                arguments);
            Assert.That(prepared, Is.True);
            return arguments[3];
        }

        private static object PrepareChi(
            Fixture fixture,
            object reactionWindow,
            object candidate,
            int optionId)
        {
            object[] arguments =
            {
                fixture.GameState,
                reactionWindow,
                candidate,
                optionId,
                null,
                null
            };
            bool prepared = (bool)fixture.Reflection.Invoke(
                fixture.Reflection.CreateInstance(
                    fixture.Reflection.RequireType(ChiServiceTypeName)),
                "TryPrepareDeclaration",
                arguments);
            Assert.That(prepared, Is.True);
            return arguments[4];
        }

        private static bool TryCommitPreparedCall(
            Fixture fixture,
            object reactionWindow,
            object preparedCall)
        {
            object[] arguments = { reactionWindow, preparedCall, null };
            return (bool)fixture.Reflection.Invoke(
                fixture.GameState,
                "TryCommitMeldCall",
                arguments);
        }

        private static object FindCandidate(
            Fixture fixture,
            object reactionWindow,
            string kindName)
        {
            object candidates = fixture.Reflection.GetProperty(reactionWindow, "Candidates");
            for (int i = 0; i < fixture.Collections.Count(candidates); i++)
            {
                object candidate = fixture.Collections.Item(candidates, i);
                if (fixture.Reflection.GetProperty(candidate, "Kind").ToString() == kindName)
                    return candidate;
            }

            Assert.Fail($"Missing {kindName} candidate.");
            return null;
        }

        private static void AssertUncommittedState(
            Fixture fixture,
            object reactionWindow,
            object candidate,
            int expectedHandCount,
            int expectedClaimCount)
        {
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(expectedHandCount));
            Assert.That(OpenMeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(
                fixture.Collections.Count(
                    fixture.Reflection.GetProperty(fixture.GameState, "DiscardClaims")),
                Is.EqualTo(expectedClaimCount));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
            Assert.That(
                fixture.Reflection.GetProperty(candidate, "ResponseState").ToString(),
                Is.EqualTo("Pending"));
            Assert.That(
                fixture.Reflection.GetProperty(reactionWindow, "Candidates"),
                Is.Not.Null);
        }

        private static void AssertCommittedState(
            Fixture fixture,
            object candidate,
            string expectedMeldType)
        {
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(0));
            Assert.That(OpenMeldCount(fixture, "East"), Is.EqualTo(1));
            Assert.That(
                fixture.Reflection.GetProperty(OpenMeldAt(fixture, "East", 0), "Type").ToString(),
                Is.EqualTo(expectedMeldType));
            Assert.That(
                fixture.Collections.Count(
                    fixture.Reflection.GetProperty(fixture.GameState, "DiscardClaims")),
                Is.EqualTo(1));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.True);
            Assert.That(
                fixture.Reflection.GetProperty(candidate, "ResponseState").ToString(),
                Is.EqualTo("Declared"));
        }

        private static void AssertRejectedWithoutMutation(
            Fixture fixture,
            object reactionWindow,
            string kindName,
            int optionId,
            int expectedHandCount)
        {
            object result = TryDeclare(fixture, reactionWindow, "East", kindName, optionId);
            Assert.That((bool)fixture.Reflection.GetProperty(result, "Declared"), Is.False);
            Assert.That(HandCount(fixture, "East"), Is.EqualTo(expectedHandCount));
            Assert.That(OpenMeldCount(fixture, "East"), Is.EqualTo(0));
            Assert.That((bool)fixture.Reflection.GetProperty(fixture.GameState, "HasCallOccurred"), Is.False);
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
                reflection.RequireType(MeldCallServiceTypeName),
                reflection.RequireType(MeldCallKindTypeName),
                reflection.RequireType(ReactionKindTypeName),
                reflection.RequireType(ReactionWindowCandidateTypeName),
                reflection.RequireType(WinCheckResultTypeName),
                reflection.RequireType(WinDeclarationEvaluationResultTypeName));
        }

        private static object BeginMeldCallWindow(
            Fixture fixture,
            string sourceSeat,
            string tileCode)
        {
            object sourceDiscard = AddDiscard(fixture, sourceSeat, tileCode);
            object candidates = CollectCandidates(fixture, sourceDiscard);
            return fixture.Reflection.Invoke(
                fixture.GameState,
                "BeginReactionWindow",
                sourceDiscard,
                candidates);
        }

        private static object AddDiscard(Fixture fixture, string sourceSeat, string tileCode)
        {
            object discard = fixture.DataFactory.CreateDiscardRecord(sourceSeat, tileCode, 7);
            return fixture.Reflection.Invoke(fixture.GameState, "AddDiscard", discard);
        }

        private static object CollectCandidates(Fixture fixture, object sourceDiscard)
        {
            object service = fixture.Reflection.CreateInstance(fixture.MeldCallServiceType);
            return fixture.Reflection.Invoke(service, "CollectCandidates", fixture.GameState, sourceDiscard);
        }

        private static object TryDeclare(
            Fixture fixture,
            object reactionWindow,
            string seatName,
            string kindName,
            int optionId)
        {
            object service = fixture.Reflection.CreateInstance(fixture.MeldCallServiceType);
            return fixture.Reflection.Invoke(
                service,
                "TryDeclare",
                fixture.GameState,
                reactionWindow,
                fixture.DataFactory.ParseSeat(seatName),
                Enum.Parse(fixture.MeldCallKindType, kindName),
                optionId);
        }

        private static object TryDecline(
            Fixture fixture,
            object reactionWindow,
            string seatName)
        {
            object service = fixture.Reflection.CreateInstance(fixture.MeldCallServiceType);
            return fixture.Reflection.Invoke(
                service,
                "TryDecline",
                fixture.GameState,
                reactionWindow,
                fixture.DataFactory.ParseSeat(seatName));
        }

        private static string AvailableKinds(Fixture fixture, object reactionWindow, string seatName)
        {
            object service = fixture.Reflection.CreateInstance(fixture.MeldCallServiceType);
            object kinds = fixture.Reflection.Invoke(
                service,
                "GetAvailableKinds",
                reactionWindow,
                fixture.DataFactory.ParseSeat(seatName));
            int count = fixture.Collections.Count(kinds);
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = fixture.Collections.Item(kinds, i).ToString();

            return string.Join(" ", names);
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

        private static int OpenMeldCount(Fixture fixture, string seatName)
        {
            return fixture.Collections.Count(fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                "OpenMelds"));
        }

        private static string HandTileCodes(Fixture fixture, string seatName)
        {
            object hand = fixture.Reflection.GetProperty(
                fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                "Hand");
            return TileCodes(fixture, fixture.Reflection.Invoke(hand, "GetTiles"));
        }

        private static object OpenMeldAt(Fixture fixture, string seatName, int index)
        {
            return fixture.Collections.Item(
                fixture.Reflection.GetProperty(
                    fixture.DataFactory.GetPlayerSeat(fixture.GameState, seatName),
                    "OpenMelds"),
                index);
        }

        private static string CandidateResponseState(
            Fixture fixture,
            object reactionWindow,
            string reactionKindName)
        {
            object candidates = fixture.Reflection.GetProperty(reactionWindow, "Candidates");
            for (int i = 0; i < fixture.Collections.Count(candidates); i++)
            {
                object candidate = fixture.Collections.Item(candidates, i);
                if (fixture.Reflection.GetProperty(candidate, "Kind").ToString() == reactionKindName)
                    return fixture.Reflection.GetProperty(candidate, "ResponseState").ToString();
            }

            return null;
        }

        private static string TileCodes(Fixture fixture, object tiles)
        {
            int count = fixture.Collections.Count(tiles);
            string[] codes = new string[count];
            for (int i = 0; i < count; i++)
            {
                codes[i] = (string)fixture.Reflection.GetProperty(
                    fixture.Collections.Item(tiles, i),
                    "Code");
            }

            return string.Join(" ", codes);
        }

        private static IList CreateCandidateList(Fixture fixture, object candidates)
        {
            IList copiedCandidates = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(fixture.ReactionWindowCandidateType));
            int count = fixture.Collections.Count(candidates);
            for (int i = 0; i < count; i++)
                copiedCandidates.Add(fixture.Collections.Item(candidates, i));

            return copiedCandidates;
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

        private sealed class Fixture
        {
            public Fixture(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory,
                object gameState,
                Type meldCallServiceType,
                Type meldCallKindType,
                Type reactionKindType,
                Type reactionWindowCandidateType,
                Type winCheckResultType,
                Type winDeclarationEvaluationResultType)
            {
                Reflection = reflection;
                Collections = collections;
                Types = types;
                DataFactory = dataFactory;
                GameState = gameState;
                MeldCallServiceType = meldCallServiceType;
                MeldCallKindType = meldCallKindType;
                ReactionKindType = reactionKindType;
                ReactionWindowCandidateType = reactionWindowCandidateType;
                WinCheckResultType = winCheckResultType;
                WinDeclarationEvaluationResultType = winDeclarationEvaluationResultType;
            }

            public ReflectionTestAccess Reflection { get; }
            public CollectionTestAccess Collections { get; }
            public MahjongTestTypes Types { get; }
            public MahjongTestDataFactory DataFactory { get; }
            public object GameState { get; }
            public Type MeldCallServiceType { get; }
            public Type MeldCallKindType { get; }
            public Type ReactionKindType { get; }
            public Type ReactionWindowCandidateType { get; }
            public Type WinCheckResultType { get; }
            public Type WinDeclarationEvaluationResultType { get; }
        }
    }
}
