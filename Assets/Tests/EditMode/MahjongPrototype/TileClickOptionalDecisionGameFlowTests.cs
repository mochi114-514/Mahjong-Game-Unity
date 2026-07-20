using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class TileClickOptionalDecisionGameFlowTests
    {
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";

        [TestCase(true, "Hand")]
        [TestCase(false, "DrawnTile")]
        public void WinDecision_TileClickDeclinesAndDiscardsInOneAuthorityCommand(
            bool discardHand,
            string expectedSource)
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                PrepareWinningTsumoDecision(session);
                int discardCountBefore = session.Query.DiscardCount;
                object request = GetPendingDecision(session, "WinDeclaration");

                bool accepted = discardHand
                    ? session.Commands.TryRequestDiscardHandFromTileClickForSeat("East", 0)
                    : session.Commands.TryRequestDiscardDrawnTileFromTileClickForSeat("East");

                Assert.That(accepted, Is.True);
                Assert.That(session.Query.IsWinDecisionPending, Is.False);
                Assert.That(HasPendingDecision(session, "WinDeclaration"), Is.False);
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
                Assert.That(
                    session.Query.DiscardSourceNameAt(discardCountBefore),
                    Is.EqualTo(expectedSource));
                Assert.That(
                    TrySubmitDecisionResponse(session, request, true),
                    Is.False,
                    "The cancelled Local UI callback must not affect the later turn.");
            }
        }

        [Test]
        public void ReachDecision_TileClickDeclinesAndDiscardsWithoutEnteringSelection()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                PrepareReachDecision(session);
                int discardCountBefore = session.Query.DiscardCount;

                Assert.That(
                    session.Commands.TryRequestDiscardDrawnTileFromTileClickForSeat("East"),
                    Is.True);

                Assert.That(IsReachDecisionPending(session), Is.False);
                Assert.That(IsReachDiscardSelectionPending(session), Is.False);
                Assert.That(
                    (bool)session.Reflection.GetProperty(
                        session.Query.GetPlayerSeat("East"),
                        "IsReachDeclared"),
                    Is.False);
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
                Assert.That(
                    session.Query.DiscardSourceNameAt(discardCountBefore),
                    Is.EqualTo("DrawnTile"));
            }
        }

        [Test]
        public void ReachDecision_TileClickDiscardsTheOriginallyClickedHandTileBeforeDeferredAutoSort()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                PrepareReachDecision(session);
                object hand = session.Reflection.GetProperty(
                    session.Query.GetPlayerSeat("East"),
                    "Hand");
                object tiles = session.Reflection.Invoke(hand, "GetTiles");
                const int selectedHandIndex = 12;
                string selectedTile = session.Collections.Item(
                    tiles,
                    selectedHandIndex).ToString();
                int discardCountBefore = session.Query.DiscardCount;

                session.Reflection.Invoke(
                    session.GameFlow,
                    "RequestSetAutoSortEnabled",
                    true);
                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat(
                        "East",
                        selectedHandIndex),
                    Is.True);

                Assert.That(
                    session.Reflection.GetProperty(
                        session.Query.DiscardAt(discardCountBefore),
                        "Tile").ToString(),
                    Is.EqualTo(selectedTile));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SelfKanDecision_TileClickDeclinesAnkanOrKakanThenDiscards(bool kakan)
        {
            using (MahjongGameFlowTestSession session = kakan
                       ? CreateKakanDecisionSession()
                       : CreateAnkanDecisionSession())
            {
                int discardCountBefore = session.Query.DiscardCount;
                int meldCountBefore = session.Query.MeldCount("East");
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("SelfKanDecision"));
                Assert.That(HasPendingDecision(session, "SelfKan"), Is.True);

                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat("East", 0),
                    Is.True);

                Assert.That(session.Query.TurnPhaseName, Is.Not.EqualTo("SelfKanDecision"));
                Assert.That(HasPendingDecision(session, "SelfKan"), Is.False);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(meldCountBefore));
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
            }
        }

        [Test]
        public void ReachDiscardSelection_CandidateDeclaresReach_ButNonCandidateCancelsAndDiscardsNormally()
        {
            using (MahjongGameFlowTestSession candidateSession = CreateSession(2))
            {
                PrepareReachDiscardSelection(candidateSession);
                ReachCandidate candidate = FindReachCandidate(candidateSession, true);
                int discardCountBefore = candidateSession.Query.DiscardCount;

                Assert.That(
                    ExecuteReachCandidateTileClick(candidateSession, candidate),
                    Is.True);
                Assert.That(
                    (bool)candidateSession.Reflection.GetProperty(
                        candidateSession.Query.GetPlayerSeat("East"),
                        "IsReachDeclared"),
                    Is.True);
                Assert.That(candidateSession.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
            }

            using (MahjongGameFlowTestSession nonCandidateSession = CreateSession(2))
            {
                PrepareReachDiscardSelection(nonCandidateSession);
                ReachCandidate nonCandidate = FindReachCandidate(nonCandidateSession, false);
                int discardCountBefore = nonCandidateSession.Query.DiscardCount;

                Assert.That(
                    ExecuteReachCandidateTileClick(nonCandidateSession, nonCandidate),
                    Is.True);
                Assert.That(
                    (bool)nonCandidateSession.Reflection.GetProperty(
                        nonCandidateSession.Query.GetPlayerSeat("East"),
                        "IsReachDeclared"),
                    Is.False);
                Assert.That(IsReachDiscardSelectionPending(nonCandidateSession), Is.False);
                Assert.That(nonCandidateSession.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
            }
        }

        [Test]
        public void ReactionWindow_TileClickDoesNotPassOrDiscard()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "1m", "4m", "7m", "9m",
                    "1p", "4p", "7p", "1s", "4s", "7s", "9s");
                DiscardFromWest(session, "P");
                int discardCountBefore = session.Query.DiscardCount;

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat("East", 0),
                    Is.False);
                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore));
                Assert.That(HasPendingReactionDecision(session), Is.True);
            }
        }

        [Test]
        public void InvalidTileClick_RetainsDecisionAndQueuedButtonResponseWinsTheRace()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                PrepareWinningTsumoDecision(session);
                object request = GetPendingDecision(session, "WinDeclaration");
                int discardCountBefore = session.Query.DiscardCount;

                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat("East", 99),
                    Is.False);
                Assert.That(session.Query.IsWinDecisionPending, Is.True);
                Assert.That(HasPendingDecision(session, "WinDeclaration"), Is.True);

                Assert.That(TrySubmitDecisionResponse(session, request, true), Is.True);
                Assert.That(
                    session.Commands.TryRequestDiscardDrawnTileFromTileClickForSeat("East"),
                    Is.False,
                    "A queued button response must not be replaced by a later tile click.");
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore));

                session.Reflection.Invoke(
                    session.Reflection.GetProperty(session.GameFlow, "DecisionCoordinator"),
                    "Pump");
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore));
            }
        }

        [Test]
        public void RepeatedTileClick_ProducesOnlyOneDiscard()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                PrepareWinningTsumoDecision(session);
                int discardCountBefore = session.Query.DiscardCount;

                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat("East", 0),
                    Is.True);
                Assert.That(
                    session.Commands.TryRequestDiscardHandFromTileClickForSeat("East", 0),
                    Is.False);
                Assert.That(session.Query.DiscardCount, Is.EqualTo(discardCountBefore + 1));
            }
        }

        private static MahjongGameFlowTestSession CreateAnkanDecisionSession()
        {
            MahjongGameFlowTestSession session = CreateSession(2);
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "P", "P", "P", "1m", "4m", "7m", "9m",
                "1p", "4p", "7p", "1s", "4s", "7s");
            session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
            session.Commands.ResolveAfterDraw("East");
            Assert.That(session.Query.TurnPhaseName, Is.EqualTo("SelfKanDecision"));
            return session;
        }

        private static MahjongGameFlowTestSession CreateKakanDecisionSession()
        {
            MahjongGameFlowTestSession session = CreateSession(2);
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "P", "P", "1m", "4m", "7m", "9m",
                "1p", "4p", "7p", "1s", "4s", "7s", "9s");
            DiscardFromWest(session, "P");
            Assert.That(
                session.Commands.TryRequestDeclarePonForSeat(
                    "East",
                    session.Query.ReactionWindowId),
                Is.True);
            session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");
            session.Reflection.Invoke(session.CurrentState, "EnterWaitingForDiscard");
            session.Commands.ResolveAfterDraw("East");
            Assert.That(session.Query.TurnPhaseName, Is.EqualTo("SelfKanDecision"));
            return session;
        }

        private static void PrepareWinningTsumoDecision(MahjongGameFlowTestSession session)
        {
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "1m", "2m", "3m", "1p", "2p", "3p", "1s", "2s", "3s",
                "E", "E", "E", "C");
            session.DataFactory.SetDrawnTile(session.CurrentState, "East", "C");
            session.Commands.ResolveAfterDraw("East");
            Assert.That(session.Query.IsWinDecisionPending, Is.True);
            Assert.That(HasPendingDecision(session, "WinDeclaration"), Is.True);
        }

        private static void PrepareReachDecision(MahjongGameFlowTestSession session)
        {
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "1m", "2m", "3m", "2p", "3p", "4p", "7s", "8s", "9s",
                "E", "E", "E", "5m");
            session.DataFactory.SetDrawnTile(session.CurrentState, "East", "6m");
            session.Reflection.Invoke(session.CurrentState, "EnterWaitingForDiscard");
            session.Commands.ResolveAfterDraw("East");
            Assert.That(IsReachDecisionPending(session), Is.True);
            Assert.That(HasPendingDecision(session, "Reach"), Is.True);
        }

        private static void PrepareReachDiscardSelection(MahjongGameFlowTestSession session)
        {
            PrepareReachDecision(session);
            session.Reflection.Invoke(session.GameFlow, "RequestDeclareReach");
            Assert.That(IsReachDiscardSelectionPending(session), Is.True);
        }

        private static ReachCandidate FindReachCandidate(
            MahjongGameFlowTestSession session,
            bool candidate)
        {
            object playerSeat = session.Query.GetPlayerSeat("East");
            object candidates = session.Reflection.GetProperty(
                session.CurrentState,
                "ReachDiscardCandidates");
            int handCount = (int)session.Reflection.GetProperty(
                session.Reflection.GetProperty(playerSeat, "Hand"),
                "Count");
            bool drawnCandidate = false;
            bool[] handCandidates = new bool[handCount];
            for (int i = 0; i < session.Collections.Count(candidates); i++)
            {
                object current = session.Collections.Item(candidates, i);
                string source = session.Reflection.GetProperty(current, "Source").ToString();
                if (source == "DrawnTile")
                {
                    drawnCandidate = true;
                    continue;
                }

                int handIndex = (int)session.Reflection.GetProperty(current, "HandIndex");
                if (handIndex >= 0 && handIndex < handCandidates.Length)
                    handCandidates[handIndex] = true;
            }

            if (candidate)
            {
                for (int i = 0; i < handCandidates.Length; i++)
                {
                    if (handCandidates[i])
                        return ReachCandidate.Hand(i);
                }

                if (drawnCandidate)
                    return ReachCandidate.DrawnTile();
            }
            else
            {
                for (int i = 0; i < handCandidates.Length; i++)
                {
                    if (!handCandidates[i])
                        return ReachCandidate.Hand(i);
                }

                if (!drawnCandidate)
                    return ReachCandidate.DrawnTile();
            }

            Assert.Fail(candidate
                ? "The reach selection did not expose a candidate tile."
                : "The reach selection did not expose a non-candidate tile.");
            return default;
        }

        private static bool ExecuteReachCandidateTileClick(
            MahjongGameFlowTestSession session,
            ReachCandidate candidate)
        {
            return candidate.IsDrawnTile
                ? session.Commands.TryRequestDiscardDrawnTileFromTileClickForSeat("East")
                : session.Commands.TryRequestDiscardHandFromTileClickForSeat(
                    "East",
                    candidate.HandIndex);
        }

        private static object GetPendingDecision(
            MahjongGameFlowTestSession session,
            string kindName)
        {
            object[] arguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                Enum.Parse(session.Reflection.RequireType(DecisionKindTypeName), kindName),
                null
            };
            Assert.That(
                (bool)session.Reflection.Invoke(
                    session.GameFlow,
                    "TryGetPendingDecisionRequest",
                    arguments),
                Is.True);
            return arguments[2];
        }

        private static bool HasPendingDecision(
            MahjongGameFlowTestSession session,
            string kindName)
        {
            object[] arguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                Enum.Parse(session.Reflection.RequireType(DecisionKindTypeName), kindName),
                null
            };
            return (bool)session.Reflection.Invoke(
                session.GameFlow,
                "TryGetPendingDecisionRequest",
                arguments);
        }

        private static bool IsReachDecisionPending(MahjongGameFlowTestSession session)
        {
            return (bool)session.Reflection.GetProperty(
                session.CurrentState,
                "IsReachDecisionPending");
        }

        private static bool IsReachDiscardSelectionPending(
            MahjongGameFlowTestSession session)
        {
            return (bool)session.Reflection.GetProperty(
                session.CurrentState,
                "IsReachDiscardSelectionPending");
        }

        private static bool HasPendingReactionDecision(MahjongGameFlowTestSession session)
        {
            object[] arguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                null
            };
            return (bool)session.Reflection.Invoke(
                session.GameFlow,
                "TryGetPendingReactionDecisionRequest",
                arguments);
        }

        private static bool TrySubmitDecisionResponse(
            MahjongGameFlowTestSession session,
            object request,
            bool accepted)
        {
            object[] providerArguments =
            {
                session.DataFactory.ParsePlayerId("Player1"),
                null
            };
            Assert.That(
                (bool)session.Reflection.Invoke(
                    session.GameFlow,
                    "TryGetLocalUiDecisionProvider",
                    providerArguments),
                Is.True);
            object response = session.Reflection.CreateInstance(
                session.Reflection.RequireType(DecisionResponseTypeName),
                session.Reflection.GetProperty(request, "RequestId"),
                session.Reflection.GetProperty(request, "Kind"),
                session.Reflection.GetProperty(request, "PlayerId"),
                session.Reflection.GetProperty(request, "ActorSeat"),
                session.Reflection.GetProperty(request, "TurnIndex"),
                accepted);
            return (bool)session.Reflection.Invoke(
                providerArguments[1],
                "TrySubmitResponse",
                response);
        }

        private static void DiscardFromWest(MahjongGameFlowTestSession session, string tileCode)
        {
            session.DataFactory.SetCurrentTurn(session.CurrentState, "West");
            session.DataFactory.SetDrawnTile(session.CurrentState, "West", tileCode);
            Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("West"), Is.True);
        }

        private static MahjongGameFlowTestSession CreateSession(int participantCount)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog = MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "TileClickOptionalDecisionGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = participantCount,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = 0f,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East",
                    YakuDefinitionCatalog = catalog
                },
                reflection,
                collections,
                types,
                dataFactory);
            session.RegisterOwnedScriptableObject(catalog);
            return session;
        }

        private readonly struct ReachCandidate
        {
            private ReachCandidate(bool isDrawnTile, int handIndex)
            {
                IsDrawnTile = isDrawnTile;
                HandIndex = handIndex;
            }

            public bool IsDrawnTile { get; }
            public int HandIndex { get; }

            public static ReachCandidate Hand(int handIndex)
            {
                return new ReachCandidate(false, handIndex);
            }

            public static ReachCandidate DrawnTile()
            {
                return new ReachCandidate(true, -1);
            }
        }
    }
}
