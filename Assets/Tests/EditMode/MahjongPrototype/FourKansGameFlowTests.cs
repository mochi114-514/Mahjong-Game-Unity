using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FourKansGameFlowTests
    {
        private const string PlayerMeldTypeName =
            "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";

        [Test]
        public void FourthKanPostDiscard_WithNoRonEndsAsFourKansAndRestartsSameRound()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                object previousState = session.CurrentState;
                object previousWall = session.Reflection.GetProperty(previousState, "Wall");
                PrepareFourKansCandidate(session, "West");
                session.DataFactory.AddHandTiles(session.Query.GetPlayerSeat("South"), "P", "P");

                DiscardDrawnTile(session, "West", "P");

                AssertFourKansResult(session);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(
                    session.Commands.TryRequestDeclarePonForSeat("South", 1),
                    Is.False,
                    "The four-kans candidate discard must not accept a meld call.");
                object cpuTurnController = session.Reflection.GetPrivateField(
                    session.GameFlow,
                    "cpuTurnController");
                Assert.That(
                    session.Reflection.GetProperty(cpuTurnController, "IsCpuTurnRunning"),
                    Is.False);

                session.Commands.RequestAdvanceFromRoundResult();

                Assert.That(session.CurrentState, Is.Not.SameAs(previousState));
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "Wall"),
                    Is.Not.SameAs(previousWall));
                Assert.That(session.Query.WindProgressRoundWindName, Is.EqualTo("East"));
                Assert.That(session.Query.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(session.Query.IsGameEnded, Is.False);
            }
        }

        [Test]
        public void FourthKanPostDiscard_OffersOnlyRonAndEndsAfterRonIsDeclined()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                PrepareFourKansCandidate(session, "North");
                AddRonWaitingHand(session, "East");

                DiscardDrawnTile(session, "North", "5m");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(GetCandidateKinds(session), Is.EqualTo(new[] { "Ron" }));
                int windowId = session.Query.ReactionWindowId;
                Assert.That(
                    session.Commands.TryRequestDeclineRonForSeat("East", windowId),
                    Is.True);

                AssertFourKansResult(session);
                Assert.That(
                    session.Commands.TryRequestDeclineRonForSeat("East", windowId),
                    Is.False,
                    "A response from the closed reaction window must be rejected.");
            }
        }

        [Test]
        public void FourthKanPostDiscard_WhenRonIsDeclared_PrioritizesWin()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                PrepareFourKansCandidate(session, "North");
                AddRonWaitingHand(session, "East");

                DiscardDrawnTile(session, "North", "5m");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(
                    session.Commands.TryRequestDeclareRonForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);
                Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(session.Query.RoundResultWinTypeNameOrNull, Is.EqualTo("Ron"));
            }
        }

        [Test]
        public void OnePlayerWithFourKans_UsesNormalMeldReactionAndAdvancesTurn()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                AddAnkan(session, "East", "1m");
                AddAnkan(session, "East", "2m");
                AddAnkan(session, "East", "3m");
                AddAnkan(session, "East", "4m");
                ExhaustRinshan(session, 0);
                session.DataFactory.AddHandTiles(session.Query.GetPlayerSeat("South"), "P", "P");

                DiscardDrawnTile(session, "East", "P");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(GetCandidateKinds(session), Does.Contain("Pon"));
                Assert.That(
                    session.Commands.TryRequestDeclinePonForSeat(
                        "South",
                        session.Query.ReactionWindowId),
                    Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("South"));
            }
        }

        [Test]
        public void FourReaches_TakesPriorityOverFourKans()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                PrepareFourKansCandidate(session, "North");
                int turnIndex = session.Query.TurnIndex;
                DeclareReach(session, "East", turnIndex);
                DeclareReach(session, "South", turnIndex);
                DeclareReach(session, "West", turnIndex);
                DeclareReach(session, "North", turnIndex);

                DiscardDrawnTile(session, "North", "5m");

                Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
                Assert.That(
                    session.Query.RoundResultAbortiveDrawKindNameOrNull,
                    Is.EqualTo("FourReaches"));
            }
        }

        [Test]
        public void FourthAnkan_DrawsFinalRinshanBeforeFourKansIsResolved()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                PrepareThreePriorKans(session);
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P",
                    "1m", "4m", "7m", "9m",
                    "1p", "4p", "7p",
                    "1s", "4s", "7s");
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");

                Assert.That(
                    session.Commands.TryRequestDeclareAnkanForSeat("East", "P"),
                    Is.True);
                Assert.That(RemainingRinshanTileCount(session), Is.Zero);
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);

                Assert.That(
                    session.Commands.TryRequestDiscardDrawnTileForSeat("East"),
                    Is.True);
                AssertFourKansResult(session);
            }
        }

        [Test]
        public void FourthKakan_ResolvesChankanThenDrawsFinalRinshanBeforeFourKans()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P",
                    "1m", "4m", "7m", "9m",
                    "1p", "4p", "7p",
                    "1s", "4s", "7s", "9s");
                DiscardDrawnTile(session, "West", "P");
                Assert.That(
                    session.Commands.TryRequestDeclarePonForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);
                PrepareThreePriorKans(session);
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "P");

                Assert.That(
                    session.Commands.TryRequestDeclareKakanForSeat("East", "P", 0),
                    Is.True);
                Assert.That(RemainingRinshanTileCount(session), Is.Zero);
                Assert.That(session.Query.MeldCount("East"), Is.EqualTo(1));
                Assert.That(
                    session.Reflection.GetProperty(session.Query.MeldAt("East", 0), "Type").ToString(),
                    Is.EqualTo("Kakan"));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);

                Assert.That(
                    session.Commands.TryRequestDiscardDrawnTileForSeat("East"),
                    Is.True);
                AssertFourKansResult(session);
            }
        }

        [Test]
        public void FourthDaiminkan_DrawsFinalRinshanBeforeFourKansIsResolved()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                PrepareThreePriorKans(session);
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "P", "P", "P");

                DiscardDrawnTile(session, "West", "P");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(
                    session.Commands.TryRequestDeclareDaiminkanForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);
                Assert.That(RemainingRinshanTileCount(session), Is.Zero);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.HasDrawnTile("East"), Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);

                Assert.That(
                    session.Commands.TryRequestDiscardDrawnTileForSeat("East"),
                    Is.True);
                AssertFourKansResult(session);
            }
        }

        private static MahjongGameFlowTestSession CreateSession()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog = MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "FourKansGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = 4,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = 0.1f,
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

        private static void PrepareFourKansCandidate(
            MahjongGameFlowTestSession session,
            string actorSeatName)
        {
            if (actorSeatName == "North")
            {
                AddAnkan(session, "North", "1m");
                AddAnkan(session, "North", "2m");
                AddAnkan(session, "South", "3m");
                AddAnkan(session, "South", "4m");
            }
            else
            {
                AddAnkan(session, "East", "1m");
                AddAnkan(session, "South", "2m");
                AddAnkan(session, "West", "3m");
                AddAnkan(session, "West", "4m");
            }

            ExhaustRinshan(session, 0);
        }

        private static void PrepareThreePriorKans(MahjongGameFlowTestSession session)
        {
            AddAnkan(session, "South", "1m");
            AddAnkan(session, "South", "2m");
            AddAnkan(session, "South", "3m");
            ExhaustRinshan(session, 1);
        }

        private static void AddAnkan(
            MahjongGameFlowTestSession session,
            string ownerSeatName,
            string tileCode)
        {
            Type playerMeldType = session.Reflection.RequireType(PlayerMeldTypeName);
            object playerSeat = session.Query.GetPlayerSeat(ownerSeatName);
            object meld = session.Reflection.InvokeStatic(
                playerMeldType,
                "CreateAnkan",
                session.DataFactory.CreateTileArray(tileCode, tileCode, tileCode, tileCode),
                session.DataFactory.ParseSeat(ownerSeatName));
            session.Reflection.Invoke(playerSeat, "AddMeld", meld);
        }

        private static void ExhaustRinshan(
            MahjongGameFlowTestSession session,
            int remainingCount)
        {
            object wall = session.Reflection.GetProperty(session.CurrentState, "Wall");
            while (RemainingRinshanTileCount(session) > remainingCount)
            {
                object[] arguments = { null };
                Assert.That(
                    (bool)session.Reflection.Invoke(wall, "TryTakeRinshan", arguments),
                    Is.True);
            }
        }

        private static int RemainingRinshanTileCount(MahjongGameFlowTestSession session)
        {
            return (int)session.Reflection.GetProperty(
                session.Reflection.GetProperty(session.CurrentState, "Wall"),
                "RemainingRinshanTileCount");
        }

        private static void DiscardDrawnTile(
            MahjongGameFlowTestSession session,
            string seatName,
            string tileCode)
        {
            session.DataFactory.SetCurrentTurn(session.CurrentState, seatName);
            object playerSeat = session.Query.GetPlayerSeat(seatName);
            if ((bool)session.Reflection.GetProperty(playerSeat, "HasDrawnTile"))
                session.DataFactory.ClearDrawnTile(session.CurrentState, seatName);

            session.DataFactory.SetDrawnTile(session.CurrentState, seatName, tileCode);
            Assert.That(
                session.Commands.TryRequestDiscardDrawnTileForSeat(seatName),
                Is.True);
        }

        private static void AddRonWaitingHand(
            MahjongGameFlowTestSession session,
            string seatName)
        {
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat(seatName),
                "2p", "3p", "4p",
                "5p", "6p", "7p",
                "2s", "3s", "4s",
                "6s", "7s", "8s",
                "5m");
        }

        private static void DeclareReach(
            MahjongGameFlowTestSession session,
            string seatName,
            int turnIndex)
        {
            session.Reflection.Invoke(
                session.Query.GetPlayerSeat(seatName),
                "DeclareReach",
                turnIndex);
        }

        private static string[] GetCandidateKinds(MahjongGameFlowTestSession session)
        {
            string[] kinds = new string[session.Query.ReactionWindowCandidateCount];
            for (int i = 0; i < kinds.Length; i++)
                kinds[i] = session.Query.ReactionWindowCandidateKindAt(i);

            return kinds;
        }

        private static void AssertFourKansResult(MahjongGameFlowTestSession session)
        {
            Assert.That(session.Query.IsReactionWindowPending, Is.False);
            Assert.That(session.Query.IsRoundResultPending, Is.True);
            Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
            Assert.That(
                session.Query.RoundResultAbortiveDrawKindNameOrNull,
                Is.EqualTo("FourKans"));
        }
    }
}
