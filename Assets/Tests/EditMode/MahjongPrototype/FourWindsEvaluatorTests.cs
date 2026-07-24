using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FourWindsEvaluatorTests
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.FourWindsEvaluator, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection = new ReflectionTestAccess();
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory data;
        private readonly object evaluator;

        public FourWindsEvaluatorTests()
        {
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            evaluator = reflection.CreateInstance(reflection.RequireType(EvaluatorTypeName));
        }

        [TestCase("E")]
        [TestCase("S")]
        [TestCase("W")]
        [TestCase("N")]
        public void IsSatisfied_FourDistinctFirstDiscardsOfSameWind_ReturnsTrue(
            string tileCode)
        {
            object gameState = CreateFourPlayerState();
            AddFourDiscards(gameState, tileCode);

            Assert.That(IsSatisfied(gameState), Is.True);
        }

        [Test]
        public void IsSatisfied_OnlyThreeDiscards_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "E", 1);
            data.AddDiscard(gameState, "South", "E", 2);
            data.AddDiscard(gameState, "West", "E", 3);

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [Test]
        public void IsSatisfied_DifferentFirstDiscards_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "E", 1);
            data.AddDiscard(gameState, "South", "E", 2);
            data.AddDiscard(gameState, "West", "E", 3);
            data.AddDiscard(gameState, "North", "S", 4);

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [TestCase("1m")]
        [TestCase("P")]
        [TestCase("F")]
        [TestCase("C")]
        public void IsSatisfied_NumberOrDragonTile_ReturnsFalse(string tileCode)
        {
            object gameState = CreateFourPlayerState();
            AddFourDiscards(gameState, tileCode);

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [Test]
        public void IsSatisfied_AfterCallOrKan_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddFourDiscards(gameState, "W");
            reflection.Invoke(gameState, "MarkCallOccurred");

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [TestCase("East")]
        [TestCase("East", "South")]
        [TestCase("East", "South", "West")]
        public void IsSatisfied_WithFewerThanFourPlayers_ReturnsFalse(
            params string[] activeSeats)
        {
            object gameState = data.CreateGameState(activeSeats);
            data.AddDiscard(gameState, "East", "N", 1);
            data.AddDiscard(gameState, "South", "N", 2);
            data.AddDiscard(gameState, "West", "N", 3);
            data.AddDiscard(gameState, "North", "N", 4);

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [Test]
        public void IsSatisfied_DuplicateDiscardingSeat_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "S", 1);
            data.AddDiscard(gameState, "East", "S", 2);
            data.AddDiscard(gameState, "West", "S", 3);
            data.AddDiscard(gameState, "North", "S", 4);

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [Test]
        public void IsSatisfied_AfterFifthDiscard_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddFourDiscards(gameState, "N");
            data.AddDiscard(gameState, "East", "N", 5);

            Assert.That(IsSatisfied(gameState), Is.False);
        }

        [Test]
        public void IsSatisfied_DoesNotMutateRoundState()
        {
            object gameState = CreateFourPlayerState();
            AddFourDiscards(gameState, "E");
            int discardCountBefore = ((ICollection)reflection.GetProperty(
                gameState,
                "Discards")).Count;

            Assert.That(IsSatisfied(gameState), Is.True);

            Assert.That(
                ((ICollection)reflection.GetProperty(gameState, "Discards")).Count,
                Is.EqualTo(discardCountBefore));
            Assert.That(
                (bool)reflection.GetProperty(gameState, "HasCallOccurred"),
                Is.False);
        }

        private object CreateFourPlayerState()
        {
            return data.CreateGameState("East", "South", "West", "North");
        }

        private void AddFourDiscards(object gameState, string tileCode)
        {
            data.AddDiscard(gameState, "East", tileCode, 1);
            data.AddDiscard(gameState, "South", tileCode, 2);
            data.AddDiscard(gameState, "West", tileCode, 3);
            data.AddDiscard(gameState, "North", tileCode, 4);
        }

        private bool IsSatisfied(object gameState)
        {
            return (bool)reflection.Invoke(
                evaluator,
                "IsSatisfied",
                reflection.GetProperty(gameState, "ActiveTurnSeats"),
                reflection.GetProperty(gameState, "Discards"),
                reflection.GetProperty(gameState, "HasCallOccurred"));
        }
    }

    public sealed class FourWindsGameFlowTests
    {
        [Test]
        public void FourthWindDiscard_AfterNoReaction_EndsAsFourWindsWithoutNextTurn()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();

                DiscardOnCurrentTurn(session, "East", "E");
                DiscardOnCurrentTurn(session, "South", "E");
                DiscardOnCurrentTurn(session, "West", "E");
                DiscardOnCurrentTurn(session, "North", "E");

                AssertFourWindsResult(session);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("North"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(4));
                Assert.That(session.Query.DiscardCount, Is.EqualTo(4));
                object cpuTurnController = session.Reflection.GetPrivateField(
                    session.GameFlow,
                    "cpuTurnController");
                Assert.That(
                    session.Reflection.GetProperty(cpuTurnController, "IsCpuTurnRunning"),
                    Is.False);
            }
        }

        [Test]
        public void FourthWindDiscard_WhileReactionWindowIsPending_DoesNotEndEarly()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                object endedState = session.CurrentState;

                DiscardOnCurrentTurn(session, "East", "E");
                DiscardOnCurrentTurn(session, "South", "E");
                DiscardOnCurrentTurn(session, "West", "E");
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "E",
                    "E");
                DiscardOnCurrentTurn(session, "North", "E");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.CurrentState, Is.SameAs(endedState));
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("North"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(4));

                int windowId = session.Query.ReactionWindowId;
                Assert.That(
                    session.Commands.TryRequestDeclineMeldCallsForSeat(
                        "East",
                        windowId),
                    Is.True);

                AssertFourWindsResult(session);
                Assert.That(session.CurrentState, Is.SameAs(endedState));

                session.Commands.RunAuthorityUpdate();
                Assert.That(session.CurrentState, Is.Not.SameAs(endedState));
            }
        }

        [Test]
        public void FourthWindDiscard_WhenPonIsDeclared_PrioritizesReaction()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();

                DiscardOnCurrentTurn(session, "East", "S");
                DiscardOnCurrentTurn(session, "South", "S");
                DiscardOnCurrentTurn(session, "West", "S");
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "S",
                    "S");
                DiscardOnCurrentTurn(session, "North", "S");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                int windowId = session.Query.ReactionWindowId;
                Assert.That(
                    session.Commands.TryRequestDeclarePonForSeat("East", windowId),
                    Is.True);

                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(
                    session.Reflection.GetProperty(
                        session.CurrentState,
                        "HasCallOccurred"),
                    Is.True);
            }
        }

        [Test]
        public void FourWindsResult_AutomaticallyRestartsSameRoundWithFreshState()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                object previousState = session.CurrentState;
                object previousWall = session.Reflection.GetProperty(
                    previousState,
                    "Wall");
                string windProgressBefore = session.Reflection.GetProperty(
                    previousState,
                    "WindProgress").ToString();
                string selfSeatBefore = session.Query.SelfSeatName;

                DiscardOnCurrentTurn(session, "East", "N");
                DiscardOnCurrentTurn(session, "South", "N");
                DiscardOnCurrentTurn(session, "West", "N");
                DiscardOnCurrentTurn(session, "North", "N");
                AssertFourWindsResult(session);

                session.Commands.RunAuthorityUpdate();

                Assert.That(session.CurrentState, Is.Not.SameAs(previousState));
                Assert.That(
                    session.Reflection.GetProperty(
                        session.CurrentState,
                        "WindProgress").ToString(),
                    Is.EqualTo(windProgressBefore));
                Assert.That(session.Query.SelfSeatName, Is.EqualTo(selfSeatBefore));
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "Wall"),
                    Is.Not.SameAs(previousWall));
                Assert.That(session.Query.DiscardCount, Is.Zero);
                Assert.That(session.Query.IsGameEnded, Is.False);
                Assert.That(session.Query.IsRoundResultPending, Is.False);
            }
        }

        private static MahjongGameFlowTestSession CreateSession()
        {
            return MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "FourWindsGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = 4,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = 0f,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East"
                });
        }

        private static void DiscardOnCurrentTurn(
            MahjongGameFlowTestSession session,
            string seatName,
            string tileCode)
        {
            Assert.That(session.Query.CurrentTurnName, Is.EqualTo(seatName));
            object playerSeat = session.Query.GetPlayerSeat(seatName);
            if ((bool)session.Reflection.GetProperty(playerSeat, "HasDrawnTile"))
                session.DataFactory.ClearDrawnTile(session.CurrentState, seatName);

            session.DataFactory.SetDrawnTile(
                session.CurrentState,
                seatName,
                tileCode);
            Assert.That(
                session.Commands.TryRequestDiscardDrawnTileForSeat(seatName),
                Is.True);
        }

        private static void AssertFourWindsResult(
            MahjongGameFlowTestSession session)
        {
            Assert.That(session.Query.IsReactionWindowPending, Is.False);
            Assert.That(session.Query.IsRoundResultPending, Is.True);
            Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
            Assert.That(
                session.Query.RoundResultAbortiveDrawKindNameOrNull,
                Is.EqualTo("FourWinds"));
        }
    }
}
