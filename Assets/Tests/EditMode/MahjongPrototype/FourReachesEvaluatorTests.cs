using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FourReachesEvaluatorTests
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.FourReachesEvaluator, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection = new ReflectionTestAccess();
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory data;
        private readonly object evaluator;

        public FourReachesEvaluatorTests()
        {
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            evaluator = reflection.CreateInstance(reflection.RequireType(EvaluatorTypeName));
        }

        [Test]
        public void IsSatisfied_FourActiveReachPlayersAndFourthDeclarationDiscard_ReturnsTrue()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);
            DeclareReach(gameState, "North", 4);

            Assert.That(IsSatisfied(gameState, LastDiscard(gameState)), Is.True);
        }

        [Test]
        public void IsSatisfied_WhenOnlyThreePlayersReached_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);

            Assert.That(IsSatisfied(gameState, LastDiscard(gameState)), Is.False);
        }

        [Test]
        public void IsSatisfied_WhenLatestDiscardIsNotReachDeclarationDiscard_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m", "5m");
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);
            DeclareReach(gameState, "North", 4);

            Assert.That(IsSatisfied(gameState, LastDiscard(gameState)), Is.False);
        }

        [Test]
        public void IsSatisfied_WhenLatestDiscardTurnDoesNotMatchDeclarer_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);
            DeclareReach(gameState, "North", 99);

            Assert.That(IsSatisfied(gameState, LastDiscard(gameState)), Is.False);
        }

        [Test]
        public void IsSatisfied_WithThreeActivePlayers_ReturnsFalse()
        {
            object gameState = data.CreateGameState("East", "South", "West");
            data.AddDiscard(gameState, "East", "1m", 1);
            data.AddDiscard(gameState, "South", "2m", 2);
            data.AddDiscard(gameState, "West", "3m", 3);
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);

            Assert.That(IsSatisfied(gameState, LastDiscard(gameState)), Is.False);
        }

        [Test]
        public void IsSatisfied_WithDuplicateActiveSeats_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareAllFour(gameState);
            Array duplicateActiveSeats = CreateSeatArray("East", "South", "West", "West");

            Assert.That(
                IsSatisfied(
                    duplicateActiveSeats,
                    CreatePlayerSeatArray(gameState, "East", "South", "West", "North"),
                    reflection.GetProperty(gameState, "Discards"),
                    LastDiscard(gameState)),
                Is.False);
        }

        [Test]
        public void IsSatisfied_DoesNotCountReachPlayerOutsideActiveSeats()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);
            object outsidePlayerSeat = reflection.CreateInstance(
                types.PlayerSeat,
                Enum.ToObject(types.SeatId, 99));
            reflection.Invoke(outsidePlayerSeat, "DeclareReach", 4);

            Assert.That(
                IsSatisfied(
                    reflection.GetProperty(gameState, "ActiveTurnSeats"),
                    CreatePlayerSeatArray(
                        data.GetPlayerSeat(gameState, "East"),
                        data.GetPlayerSeat(gameState, "South"),
                        data.GetPlayerSeat(gameState, "West"),
                        outsidePlayerSeat),
                    reflection.GetProperty(gameState, "Discards"),
                    LastDiscard(gameState)),
                Is.False);
        }

        [Test]
        public void IsSatisfied_WithNullOrMalformedInput_ReturnsFalseWithoutThrowing()
        {
            Assert.That(
                () => reflection.Invoke(evaluator, "IsSatisfied", null, null, null, null),
                Throws.Nothing);
            Assert.That(
                (bool)reflection.Invoke(evaluator, "IsSatisfied", null, null, null, null),
                Is.False);

            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareAllFour(gameState);
            Assert.That(
                IsSatisfied(
                    reflection.GetProperty(gameState, "ActiveTurnSeats"),
                    CreatePlayerSeatArray(gameState, "East", "South", "West"),
                    reflection.GetProperty(gameState, "Discards"),
                    LastDiscard(gameState)),
                Is.False);
        }

        [Test]
        public void IsSatisfied_DoesNotMutatePlayerOrGameState()
        {
            object gameState = CreateFourPlayerState();
            AddDiscards(gameState, "1m", "2m", "3m", "4m");
            DeclareAllFour(gameState);
            object north = data.GetPlayerSeat(gameState, "North");
            int reachTurnBefore = (int)reflection.GetProperty(
                north,
                "ReachDeclaredTurnIndex");
            int discardCountBefore = ((ICollection)reflection.GetProperty(
                gameState,
                "Discards")).Count;

            Assert.That(IsSatisfied(gameState, LastDiscard(gameState)), Is.True);

            Assert.That(
                (int)reflection.GetProperty(north, "ReachDeclaredTurnIndex"),
                Is.EqualTo(reachTurnBefore));
            Assert.That(
                (bool)reflection.GetProperty(north, "IsReachDeclared"),
                Is.True);
            Assert.That(
                ((ICollection)reflection.GetProperty(gameState, "Discards")).Count,
                Is.EqualTo(discardCountBefore));
        }

        private object CreateFourPlayerState()
        {
            return data.CreateGameState("East", "South", "West", "North");
        }

        private void AddDiscards(object gameState, params string[] tileCodes)
        {
            string[] seats = { "East", "South", "West", "North", "East" };
            for (int i = 0; i < tileCodes.Length; i++)
                data.AddDiscard(gameState, seats[i], tileCodes[i], i + 1);
        }

        private void DeclareAllFour(object gameState)
        {
            DeclareReach(gameState, "East", 1);
            DeclareReach(gameState, "South", 2);
            DeclareReach(gameState, "West", 3);
            DeclareReach(gameState, "North", 4);
        }

        private void DeclareReach(object gameState, string seatName, int turnIndex)
        {
            reflection.Invoke(
                data.GetPlayerSeat(gameState, seatName),
                "DeclareReach",
                turnIndex);
        }

        private object LastDiscard(object gameState)
        {
            object discards = reflection.GetProperty(gameState, "Discards");
            return ((IList)discards)[((IList)discards).Count - 1];
        }

        private Array CreateSeatArray(params string[] seatNames)
        {
            Array seats = Array.CreateInstance(types.SeatId, seatNames.Length);
            for (int i = 0; i < seatNames.Length; i++)
                seats.SetValue(data.ParseSeat(seatNames[i]), i);

            return seats;
        }

        private Array CreatePlayerSeatArray(object gameState, params string[] seatNames)
        {
            object[] playerSeats = new object[seatNames.Length];
            for (int i = 0; i < seatNames.Length; i++)
                playerSeats[i] = data.GetPlayerSeat(gameState, seatNames[i]);

            return CreatePlayerSeatArray(playerSeats);
        }

        private Array CreatePlayerSeatArray(params object[] playerSeats)
        {
            Array seats = Array.CreateInstance(types.PlayerSeat, playerSeats.Length);
            for (int i = 0; i < playerSeats.Length; i++)
                seats.SetValue(playerSeats[i], i);

            return seats;
        }

        private bool IsSatisfied(object gameState, object resolvedDiscard)
        {
            return IsSatisfied(
                reflection.GetProperty(gameState, "ActiveTurnSeats"),
                CreatePlayerSeatArray(gameState, "East", "South", "West", "North"),
                reflection.GetProperty(gameState, "Discards"),
                resolvedDiscard);
        }

        private bool IsSatisfied(
            object activeSeats,
            object playerSeats,
            object discards,
            object resolvedDiscard)
        {
            return (bool)reflection.Invoke(
                evaluator,
                "IsSatisfied",
                activeSeats,
                playerSeats,
                discards,
                resolvedDiscard);
        }
    }

    public sealed class FourReachesGameFlowTests
    {
        [Test]
        public void FourthReachDeclaration_AfterNoReaction_EndsAsFourReachesWithoutNextTurn()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                StartWithThreeReachedPlayers(session);

                DeclareReachAndDiscardOnCurrentTurn(session, "North", "5m");

                AssertFourReachesResult(session);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("North"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(4));
                object cpuTurnController = session.Reflection.GetPrivateField(
                    session.GameFlow,
                    "cpuTurnController");
                Assert.That(
                    session.Reflection.GetProperty(cpuTurnController, "IsCpuTurnRunning"),
                    Is.False);
                Assert.That(session.Commands.TryEndAbortiveDraw("FourReaches"), Is.False);
            }
        }

        [Test]
        public void ThirdReachDeclaration_DoesNotEndRound()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                DeclareReach(session, "East", 1);
                DiscardOnCurrentTurn(session, "East", "1m");
                DeclareReach(session, "South", 2);
                DiscardOnCurrentTurn(session, "South", "2m");

                DeclareReachAndDiscardOnCurrentTurn(session, "West", "3m");

                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("North"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(4));
            }
        }

        [Test]
        public void FourthReachDeclaration_WaitsForRonReactionThenEndsWhenRonIsDeclined()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                StartWithThreeReachedPlayers(session);
                AddRonWaitingHand(session, "East");

                DeclareReachAndDiscardOnCurrentTurn(session, "North", "5m");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);
                int windowId = session.Query.ReactionWindowId;

                Assert.That(
                    session.Commands.TryRequestDeclineRonForSeat("East", windowId),
                    Is.True);

                AssertFourReachesResult(session);
                Assert.That(
                    session.Commands.TryRequestDeclineRonForSeat("East", windowId),
                    Is.False);
            }
        }

        [Test]
        public void FourthReachDeclaration_WhenRonIsDeclared_PrioritizesWin()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                StartWithThreeReachedPlayers(session);
                AddRonWaitingHand(session, "East");

                DeclareReachAndDiscardOnCurrentTurn(session, "North", "5m");

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(
                    session.Commands.TryRequestDeclareRonForSeat(
                        "East",
                        session.Query.ReactionWindowId),
                    Is.True);

                Assert.That(session.Query.IsRoundResultPending, Is.True);
                Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(session.Query.RoundResultWinTypeNameOrNull, Is.EqualTo("Ron"));
            }
        }

        [Test]
        public void ThreePlayerRound_DoesNotEndAsFourReaches()
        {
            using (MahjongGameFlowTestSession session = CreateSession(3))
            {
                session.Commands.StartNewRound();
                DeclareReach(session, "East", 1);
                DiscardOnCurrentTurn(session, "East", "1m");
                DeclareReach(session, "South", 2);
                DiscardOnCurrentTurn(session, "South", "2m");

                DeclareReachAndDiscardOnCurrentTurn(session, "West", "3m");

                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(4));
            }
        }

        [Test]
        public void FourthDiscard_WhenOnePlayerHasNotReached_AdvancesNormally()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                StartWithThreeReachedPlayers(session);

                DiscardOnCurrentTurn(session, "North", "5m");

                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(5));
            }
        }

        [Test]
        public void FourWindsAndFourReaches_WhenBothEligible_PrioritizesFourWinds()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                session.Commands.StartNewRound();
                DeclareReach(session, "East", 1);
                DiscardOnCurrentTurn(session, "East", "E");
                DeclareReach(session, "South", 2);
                DiscardOnCurrentTurn(session, "South", "E");
                DeclareReach(session, "West", 3);
                DiscardOnCurrentTurn(session, "West", "E");

                DeclareReachAndDiscardOnCurrentTurn(session, "North", "E");

                Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
                Assert.That(
                    session.Query.RoundResultAbortiveDrawKindNameOrNull,
                    Is.EqualTo("FourWinds"));
            }
        }

        [Test]
        public void FourReachesResult_OnSouthFour_RestartsSameRoundWithoutGameEnd()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                StartRound(session, "South", 4);
                object previousState = session.CurrentState;
                object previousWall = session.Reflection.GetProperty(previousState, "Wall");
                StartWithThreeReachedPlayers(session);

                DeclareReachAndDiscardOnCurrentTurn(session, "North", "5m");

                AssertFourReachesResult(session);
                Assert.That(session.Query.RoundResultIsFinalRound, Is.False);

                session.Commands.RequestAdvanceFromRoundResult();

                Assert.That(session.CurrentState, Is.Not.SameAs(previousState));
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "WindProgress").ToString(),
                    Is.EqualTo("South 4"));
                Assert.That(
                    session.Reflection.GetProperty(session.CurrentState, "Wall"),
                    Is.Not.SameAs(previousWall));
                Assert.That(session.Query.IsGameEnded, Is.False);
                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(session.Query.DiscardCount, Is.Zero);
            }
        }

        private static MahjongGameFlowTestSession CreateSession(int participantCount = 4)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog = MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "FourReachesGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = participantCount,
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

        private static void StartRound(
            MahjongGameFlowTestSession session,
            string roundWindName,
            int handNumber)
        {
            session.Reflection.Invoke(
                session.GameFlow,
                "StartRound",
                session.DataFactory.CreateWindProgress(roundWindName, handNumber),
                false,
                session.DataFactory.ParseSeat("East"));
        }

        private static void StartWithThreeReachedPlayers(MahjongGameFlowTestSession session)
        {
            if (session.CurrentState == null)
                session.Commands.StartNewRound();

            DeclareReach(session, "East", 1);
            DiscardOnCurrentTurn(session, "East", "1m");
            DeclareReach(session, "South", 2);
            DiscardOnCurrentTurn(session, "South", "2m");
            DeclareReach(session, "West", 3);
            DiscardOnCurrentTurn(session, "West", "3m");
        }

        private static void DeclareReachAndDiscardOnCurrentTurn(
            MahjongGameFlowTestSession session,
            string seatName,
            string tileCode)
        {
            Assert.That(session.Query.CurrentTurnName, Is.EqualTo(seatName));
            SetDrawnTile(session, seatName, tileCode);
            Type candidateType = session.Reflection.RequireType(
                "MahjongPrototype.Services.ReachDiscardCandidate, Assembly-CSharp");
            Type discardSourceType = session.Reflection.RequireType(
                "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp");
            IList candidates = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(candidateType));
            candidates.Add(session.Reflection.CreateInstance(
                candidateType,
                Enum.Parse(discardSourceType, "DrawnTile"),
                -1,
                session.DataFactory.CreateTile(tileCode)));
            object seat = session.DataFactory.ParseSeat(seatName);
            session.Reflection.Invoke(
                session.CurrentState,
                "BeginReachDecision",
                seat,
                candidates,
                session.Query.TurnIndex);
            session.Reflection.Invoke(
                session.CurrentState,
                "BeginReachDiscardSelection",
                seat);

            Assert.That(
                session.Commands.TryRequestDiscardDrawnTileForSeat(seatName),
                Is.True);
        }

        private static void DiscardOnCurrentTurn(
            MahjongGameFlowTestSession session,
            string seatName,
            string tileCode)
        {
            Assert.That(session.Query.CurrentTurnName, Is.EqualTo(seatName));
            SetDrawnTile(session, seatName, tileCode);
            Assert.That(
                session.Commands.TryRequestDiscardDrawnTileForSeat(seatName),
                Is.True);
        }

        private static void SetDrawnTile(
            MahjongGameFlowTestSession session,
            string seatName,
            string tileCode)
        {
            object playerSeat = session.Query.GetPlayerSeat(seatName);
            if ((bool)session.Reflection.GetProperty(playerSeat, "HasDrawnTile"))
                session.DataFactory.ClearDrawnTile(session.CurrentState, seatName);

            session.DataFactory.SetDrawnTile(session.CurrentState, seatName, tileCode);
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

        private static void AssertFourReachesResult(MahjongGameFlowTestSession session)
        {
            Assert.That(session.Query.IsReactionWindowPending, Is.False);
            Assert.That(session.Query.IsRoundResultPending, Is.True);
            Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
            Assert.That(
                session.Query.RoundResultAbortiveDrawKindNameOrNull,
                Is.EqualTo("FourReaches"));
        }
    }
}
