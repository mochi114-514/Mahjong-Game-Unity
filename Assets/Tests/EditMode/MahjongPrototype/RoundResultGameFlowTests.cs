using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class RoundResultGameFlowTests
    {
        private const string StandardHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";
        private const string DaisuushiiHand =
            "E E E S S S W W W N N N 5m";

        [Test]
        public void TsumoWin_StoresRoundResultAndSelectedCandidateWithoutStartingNextRound()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.PrepareTsumoDecision(StandardHand, "C");
                object expectedSelectedCandidate = driver.SelectPendingCandidate();

                driver.DeclareWin();

                Assert.That(driver.IsRoundEnded, Is.True);
                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(driver.RoundResultWinnerSeatName, Is.EqualTo("East"));
                Assert.That(driver.RoundResultWinTypeName, Is.EqualTo("Tsumo"));
                Assert.That(driver.RoundResultWinningTileCode, Is.EqualTo("C"));
                Assert.That(driver.RoundResultSelectedCandidate, Is.SameAs(expectedSelectedCandidate));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.RoundStartedCount, Is.EqualTo(1));

                object winState = driver.CurrentState;
                driver.RunAuthorityUpdate();
                Assert.That(driver.CurrentState, Is.SameAs(winState));
                Assert.That(driver.IsRoundResultPending, Is.True);
            }
        }

        [Test]
        public void RonWin_StoresSourceSeatAndWaitsForAdvance()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.PrepareRonDecision(StandardHand, "C", "West");

                driver.DeclareWin();

                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(driver.RoundResultWinnerSeatName, Is.EqualTo("East"));
                Assert.That(driver.RoundResultWinTypeName, Is.EqualTo("Ron"));
                Assert.That(driver.RoundResultSourceSeatName, Is.EqualTo("West"));
                Assert.That(driver.RoundResultWinningTileCode, Is.EqualTo("C"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
            }
        }

        [Test]
        public void WallEmptyDrawFailure_AutomaticallyAdvancesToNextRoundAfterDeferredProcessing()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.ClearWall();
                object endedState = driver.CurrentState;

                Assert.That(driver.TryDrawForCurrentTurn(), Is.False);

                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("ExhaustiveDraw"));
                Assert.That(driver.RoundResultSelectedCandidate, Is.Null);
                Assert.That(driver.RoundResultYakuCount, Is.EqualTo(0));
                Assert.That(driver.RoundResultTotalHan, Is.EqualTo(0));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.CurrentState, Is.SameAs(endedState));

                driver.RunAuthorityUpdate();

                Assert.That(driver.IsRoundEnded, Is.False);
                Assert.That(driver.IsRoundResultPending, Is.False);
                Assert.That(driver.CurrentRoundResultIsNull, Is.True);
                Assert.That(driver.CurrentState, Is.Not.SameAs(endedState));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(2));
            }
        }

        [Test]
        public void TsumoDoubleYakuman_TransfersCandidateMultiplierToRoundResult()
        {
            using (Driver driver = Driver.Create(
                participantCount: 1,
                additionalYakumanKindName: "Daisuushii",
                additionalYakumanMultiplier: 2))
            {
                driver.PrepareTsumoDecision(DaisuushiiHand, "5m");

                driver.DeclareWin();

                Assert.That(driver.RoundResultWinTypeName, Is.EqualTo("Tsumo"));
                Assert.That(driver.RoundResultTotalHan, Is.EqualTo(0));
                Assert.That(driver.RoundResultTotalYakumanMultiplier, Is.EqualTo(2));
                Assert.That(driver.RoundResultYakumanCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDoubleYakuman_TransfersCandidateMultiplierToRoundResult()
        {
            using (Driver driver = Driver.Create(
                participantCount: 2,
                additionalYakumanKindName: "Daisuushii",
                additionalYakumanMultiplier: 2))
            {
                driver.PrepareRonDecision(DaisuushiiHand, "5m", "West");

                driver.DeclareWin();

                Assert.That(driver.RoundResultWinTypeName, Is.EqualTo("Ron"));
                Assert.That(driver.RoundResultTotalHan, Is.EqualTo(0));
                Assert.That(driver.RoundResultTotalYakumanMultiplier, Is.EqualTo(2));
                Assert.That(driver.RoundResultYakumanCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void FinalRoundExhaustiveDraw_AutomaticallyMovesToGameEnded()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartRound("South", 4, "South");
                driver.ClearWall();

                Assert.That(driver.TryDrawForCurrentTurn(), Is.False);

                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
                Assert.That(driver.IsGameEnded, Is.False);
                Assert.That(driver.RoundResultIsFinalRound, Is.True);

                object finalResult = driver.CurrentRoundResult;
                driver.RunAuthorityUpdate();

                Assert.That(driver.IsGameEnded, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("GameEnded"));
                Assert.That(driver.CurrentRoundResult, Is.SameAs(finalResult));
                Assert.That(driver.WindProgressRoundWindName, Is.EqualTo("South"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(4));
                Assert.That(driver.RoundStartedCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void AdvanceRequest_WhenRoundResultIsNotPending_DoesNotChangeState()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                object stateBefore = driver.CurrentState;
                string turnBefore = driver.CurrentTurnName;
                int wallBefore = driver.WallCount;

                driver.AdvanceFromRoundResult();

                Assert.That(driver.CurrentState, Is.SameAs(stateBefore));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(turnBefore));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.WallCount, Is.EqualTo(wallBefore));
                Assert.That(driver.IsRoundResultPending, Is.False);
            }
        }

        [Test]
        public void RoundResultPending_BlocksRepresentativeOperationsUntilAutomaticTransition()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.ClearWall();
                driver.TryDrawForCurrentTurn();
                object result = driver.CurrentRoundResult;
                int wallCount = driver.WallCount;
                int discardCount = driver.DiscardCount;

                driver.RequestDraw();
                driver.RequestDiscardDrawnTile();
                driver.RequestForceDrawSkill("C");

                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));
                Assert.That(driver.WallCount, Is.EqualTo(wallCount));
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCount));
            }
        }

        [Test]
        public void Notifications_NormalRound_UseSpecifiedOrderAndSameRoundResult()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            using (EventTrace trace = EventTrace.Subscribe(
                driver.EventNotifier,
                "WinDeclared",
                "RoundEnded",
                "RoundResultReady",
                "RoundResultConfirmed",
                "RoundStarted"))
            {
                driver.PrepareTsumoDecision(StandardHand, "C");

                driver.DeclareWin();

                object result = driver.CurrentRoundResult;
                Assert.That(trace.IndexOf("WinDeclared"), Is.LessThan(trace.IndexOf("RoundEnded")));
                Assert.That(trace.IndexOf("RoundEnded"), Is.LessThan(trace.IndexOf("RoundResultReady")));
                Assert.That(trace.FirstPayload("RoundResultReady"), Is.SameAs(result));

                driver.AdvanceFromRoundResult();

                Assert.That(trace.IndexOf("RoundResultConfirmed"), Is.LessThan(trace.LastIndexOf("RoundStarted")));
                Assert.That(trace.FirstPayload("RoundResultConfirmed"), Is.SameAs(result));
            }
        }

        [Test]
        public void Notifications_FinalRoundAutomaticTransition_ConfirmsBeforeGameEnded()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            using (EventTrace trace = EventTrace.Subscribe(
                driver.EventNotifier,
                "RoundResultReady",
                "RoundResultConfirmed",
                "GameEnded"))
            {
                driver.StartRound("South", 4, "South");
                driver.ClearWall();
                driver.TryDrawForCurrentTurn();
                object result = driver.CurrentRoundResult;

                driver.RunAuthorityUpdate();

                Assert.That(trace.IndexOf("RoundResultReady"), Is.GreaterThanOrEqualTo(0));
                Assert.That(trace.IndexOf("RoundResultConfirmed"), Is.LessThan(trace.IndexOf("GameEnded")));
                Assert.That(trace.FirstPayload("RoundResultReady"), Is.SameAs(result));
                Assert.That(trace.FirstPayload("RoundResultConfirmed"), Is.SameAs(result));
                Assert.That(trace.FirstPayload("GameEnded"), Is.SameAs(result));
            }
        }

        [TestCase("NineTerminalsAndHonors")]
        [TestCase("FourWinds")]
        [TestCase("FourReaches")]
        [TestCase("FourKans")]
        public void AbortiveDraw_ClearsPendingWinRejectsDuplicateEndAndKeepsTypedResult(
            string kindName)
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.PrepareTsumoDecision(StandardHand, "C");
                Assert.That(driver.IsWinDecisionPending, Is.True);

                Assert.That(driver.TryEndAbortiveDraw(kindName), Is.True);

                object result = driver.CurrentRoundResult;
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsReactionWindowPending, Is.False);
                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
                Assert.That(
                    driver.RoundResultAbortiveDrawKindName,
                    Is.EqualTo(kindName));
                Assert.That(driver.RoundResultWinnerSeatName, Is.Null);
                Assert.That(driver.RoundResultWinTypeName, Is.Null);
                Assert.That(driver.RoundResultSourceSeatName, Is.Null);
                Assert.That(driver.RoundResultWinningTileCode, Is.Null);
                Assert.That(driver.RoundResultSelectedCandidate, Is.Null);
                Assert.That(driver.RoundResultYakuCount, Is.EqualTo(0));
                Assert.That(driver.RoundResultTotalHan, Is.EqualTo(0));
                Assert.That(driver.RoundResultIsFinalRound, Is.False);

                Assert.That(driver.TryEndAbortiveDraw(kindName), Is.False);
                Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
                Assert.That(
                    driver.RoundResultAbortiveDrawKindName,
                    Is.EqualTo(kindName));

                driver.RequestDeclineWin();
                driver.RequestDraw();
                driver.RequestDiscardDrawnTile();
                Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
            }
        }

        [Test]
        public void AbortiveDraw_ClearsPendingReactionWindowAndStaleRonInput()
        {
            using (Driver driver = Driver.Create(participantCount: 2))
            {
                driver.PrepareRonDecision(StandardHand, "C", "West");
                Assert.That(driver.IsReactionWindowPending, Is.True);

                Assert.That(driver.TryEndAbortiveDraw("FourWinds"), Is.True);

                object result = driver.CurrentRoundResult;
                Assert.That(driver.IsReactionWindowPending, Is.False);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentReactionWindow, Is.Null);
                driver.DeclareWin();
                Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("AbortiveDraw"));
            }
        }

        [Test]
        public void AbortiveDraw_ClearsPendingReachDecisionAndStaleReachInput()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.PrepareReachDecision();
                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.ReachDiscardCandidateCount, Is.GreaterThan(0));

                Assert.That(driver.TryEndAbortiveDraw("FourReaches"), Is.True);

                object result = driver.CurrentRoundResult;
                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(0));
                driver.RequestDeclareReach();
                Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
            }
        }

        [Test]
        public void AbortiveDraw_ClearsPendingSelfKanDecisionAndStaleKanInput()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.PrepareAnkanDecision();
                Assert.That(driver.IsSelfKanDecisionPending, Is.True);
                Assert.That(driver.CurrentSelfKanDecision, Is.Not.Null);

                Assert.That(driver.TryEndAbortiveDraw("FourKans"), Is.True);

                object result = driver.CurrentRoundResult;
                Assert.That(driver.IsSelfKanDecisionPending, Is.False);
                Assert.That(driver.CurrentSelfKanDecision, Is.Null);
                Assert.That(driver.TryRequestDeclareAnkan("P"), Is.False);
                Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
            }
        }

        [TestCase("NineTerminalsAndHonors")]
        [TestCase("FourWinds")]
        [TestCase("FourReaches")]
        [TestCase("FourKans")]
        public void AbortiveDrawConfirmation_RepeatsRoundAndSeatsWithFreshRoundState(
            string kindName)
        {
            using (Driver driver = Driver.Create(
                       participantCount: 2,
                       initialHandTileCount: 1))
            {
                driver.StartRound("East", 3, "East");
                driver.RequestDraw();
                driver.RequestDiscardDrawnTile();
                Assert.That(driver.DiscardCount, Is.EqualTo(1));

                object oldState = driver.CurrentState;
                object oldWall = driver.CurrentWall;
                object oldSelfPlayerSeat = driver.SelfPlayerSeat;
                object oldDecisionProviderRegistry = driver.DecisionProviderRegistry;
                string selfSeat = driver.SelfSeatName;
                string player2Seat = driver.SeatByPlayerIdName("Player2");
                string selfParticipantType = driver.ParticipantTypeName(selfSeat);
                string player2ParticipantType = driver.ParticipantTypeName(player2Seat);

                Assert.That(driver.TryEndAbortiveDraw(kindName), Is.True);
                object oldResult = driver.CurrentRoundResult;
                driver.RunAuthorityUpdate();

                Assert.That(driver.CurrentState, Is.Not.SameAs(oldState));
                Assert.That(driver.CurrentWall, Is.Not.SameAs(oldWall));
                Assert.That(driver.SelfPlayerSeat, Is.Not.SameAs(oldSelfPlayerSeat));
                Assert.That(
                    driver.DecisionProviderRegistry,
                    Is.SameAs(oldDecisionProviderRegistry));
                Assert.That(driver.WindProgressRoundWindName, Is.EqualTo("East"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(3));
                Assert.That(driver.SelfSeatName, Is.EqualTo(selfSeat));
                Assert.That(driver.SeatByPlayerIdName("Player2"), Is.EqualTo(player2Seat));
                Assert.That(driver.ParticipantTypeName(selfSeat), Is.EqualTo(selfParticipantType));
                Assert.That(
                    driver.ParticipantTypeName(player2Seat),
                    Is.EqualTo(player2ParticipantType));
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
                Assert.That(driver.HandCount(selfSeat), Is.EqualTo(1));
                Assert.That(driver.IsRoundResultPending, Is.False);
                Assert.That(driver.CurrentRoundResultIsNull, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsSelfKanDecisionPending, Is.False);
                Assert.That(driver.IsReactionWindowPending, Is.False);
                Assert.That(driver.PendingKakan, Is.Null);
                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(0));
                Assert.That(driver.HasLastTurnDraw, Is.False);
                Assert.That(driver.IsGameEnded, Is.False);
                Assert.That(driver.RoundStartedCount, Is.EqualTo(2));

                object repeatedState = driver.CurrentState;
                driver.RunAuthorityUpdate();
                Assert.That(driver.CurrentState, Is.SameAs(repeatedState));
                Assert.That(driver.CurrentRoundResult, Is.Null);
                Assert.That(oldResult, Is.Not.Null);
            }
        }

        [TestCase("NineTerminalsAndHonors")]
        [TestCase("FourWinds")]
        [TestCase("FourReaches")]
        [TestCase("FourKans")]
        public void AbortiveDrawAfterSouthFour_RepeatsSouthFourWithoutGameEnd(string kindName)
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartRound("South", 4, "North");
                Assert.That(driver.TryEndAbortiveDraw(kindName), Is.True);
                Assert.That(driver.RoundResultIsFinalRound, Is.False);

                driver.RunAuthorityUpdate();

                Assert.That(driver.IsGameEnded, Is.False);
                Assert.That(driver.WindProgressRoundWindName, Is.EqualTo("South"));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(4));
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));
                Assert.That(driver.RoundStartedCount, Is.EqualTo(2));
            }
        }

        [TestCase("NineTerminalsAndHonors")]
        [TestCase("FourWinds")]
        [TestCase("FourReaches")]
        [TestCase("FourKans")]
        public void AbortiveDraw_EmitsTypedResultNotificationAndKindLog(string kindName)
        {
            using (Driver driver = Driver.Create(
                       participantCount: 1,
                       addGameLogRecorder: true))
            {
                driver.StartNewRound();
                using (EventTrace trace = EventTrace.Subscribe(
                    driver.EventNotifier,
                    "AbortiveDrawResolved",
                    "RoundEnded",
                    "RoundResultReady",
                    "RoundResultConfirmed",
                    "RoundStarted"))
                {
                    Assert.That(driver.TryEndAbortiveDraw(kindName), Is.True);

                    object result = driver.CurrentRoundResult;
                    Assert.That(
                        trace.IndexOf("AbortiveDrawResolved"),
                        Is.LessThan(trace.IndexOf("RoundEnded")));
                    Assert.That(
                        trace.IndexOf("RoundEnded"),
                        Is.LessThan(trace.IndexOf("RoundResultReady")));
                    Assert.That(
                        trace.FirstPayload("AbortiveDrawResolved").ToString(),
                        Is.EqualTo(kindName));
                    Assert.That(
                        trace.FirstPayload("RoundEnded").ToString(),
                        Is.EqualTo($"AbortiveDraw:{kindName}"));
                    Assert.That(
                        trace.FirstPayload("RoundResultReady"),
                        Is.SameAs(result));
                    Assert.That(
                        trace.IndexOf("AbortiveDrawResolved"),
                        Is.EqualTo(trace.LastIndexOf("AbortiveDrawResolved")));
                    Assert.That(
                        trace.IndexOf("RoundEnded"),
                        Is.EqualTo(trace.LastIndexOf("RoundEnded")));
                    Assert.That(
                        trace.IndexOf("RoundResultReady"),
                        Is.EqualTo(trace.LastIndexOf("RoundResultReady")));
                    Assert.That(driver.RecentLogContains("AbortiveDrawResolved"), Is.True);
                    Assert.That(driver.RecentLogContains($"kind={kindName}"), Is.True);

                driver.RunAuthorityUpdate();
                driver.RunAuthorityUpdate();

                    Assert.That(
                        trace.IndexOf("RoundResultConfirmed"),
                        Is.LessThan(trace.IndexOf("RoundStarted")));
                    Assert.That(
                        trace.FirstPayload("RoundResultConfirmed"),
                        Is.SameAs(result));
                    Assert.That(
                        trace.IndexOf("RoundResultConfirmed"),
                        Is.EqualTo(trace.LastIndexOf("RoundResultConfirmed")));
                    Assert.That(
                        trace.IndexOf("RoundStarted"),
                        Is.EqualTo(trace.LastIndexOf("RoundStarted")));
                }
            }
        }

        [Test]
        public void Notifications_DoubleYakuman_KeepOriginalRoundResultAndMultiplier()
        {
            using (Driver driver = Driver.Create(
                participantCount: 1,
                additionalYakumanKindName: "Daisuushii",
                additionalYakumanMultiplier: 2))
            using (EventTrace trace = EventTrace.Subscribe(
                driver.EventNotifier,
                "RoundResultReady",
                "RoundResultConfirmed"))
            {
                driver.PrepareTsumoDecision(DaisuushiiHand, "5m");
                driver.DeclareWin();

                object result = driver.CurrentRoundResult;
                object readyResult = trace.FirstPayload("RoundResultReady");
                Assert.That(readyResult, Is.SameAs(result));
                Assert.That(driver.RoundResultTotalYakumanMultiplier, Is.EqualTo(2));
                Assert.That(
                    (int)driver.ReflectionProperty(readyResult, "TotalYakumanMultiplier"),
                    Is.EqualTo(2));

                driver.AdvanceFromRoundResult();

                Assert.That(trace.FirstPayload("RoundResultConfirmed"), Is.SameAs(result));
            }
        }

        private sealed class Driver : IDisposable
        {
            private const string SelectorTypeName =
                "MahjongPrototype.Services.WinningCandidateSelector, Assembly-CSharp";

            private readonly MahjongGameFlowTestSession session;
            private readonly object selector;
            private bool disposed;

            private Driver(MahjongGameFlowTestSession session)
            {
                this.session = session;
                selector = Reflection.CreateInstance(Reflection.RequireType(SelectorTypeName));
            }

            public static Driver Create(
                int participantCount,
                bool addGameLogRecorder = false,
                int initialHandTileCount = 0,
                string additionalYakumanKindName = null,
                int additionalYakumanMultiplier = 0)
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory =
                    new MahjongTestDataFactory(reflection, types);
                object catalog = string.IsNullOrEmpty(additionalYakumanKindName)
                    ? MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory)
                    : dataFactory.CreateYakuCatalog(
                        dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"),
                        dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                        dataFactory.CreateYakuDefinition("Tanyao", "One", "One"),
                        dataFactory.CreateYakuDefinition(
                            additionalYakumanKindName,
                            "None",
                            "None",
                            additionalYakumanMultiplier));
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "RoundResultGameFlowTest",
                    AddEventNotifier = true,
                    AddGameLogRecorder = addGameLogRecorder,
                    LogWarnings = false,
                    ParticipantCount = participantCount,
                    InitialHandTileCount = initialHandTileCount,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = 0f,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East",
                    YakuDefinitionCatalog = catalog
                };

                MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                    options,
                    reflection,
                    collections,
                    types,
                    dataFactory);
                session.RegisterOwnedScriptableObject(catalog);
                return new Driver(session);
            }

            public object EventNotifier => session.EventNotifier;
            public object DecisionProviderRegistry => Reflection.GetProperty(
                session.GameFlow,
                "DecisionProviderRegistry");
            public object CurrentState => session.CurrentState;
            public object CurrentWall => Reflection.GetProperty(CurrentState, "Wall");
            public object SelfPlayerSeat => Query.GetPlayerSeat(Query.SelfSeatName);
            public object CurrentRoundResult => Query.CurrentRoundResult;
            public bool CurrentRoundResultIsNull => Query.CurrentRoundResultIsNull;
            public bool IsRoundEnded => Query.IsRoundEnded;
            public bool IsRoundResultPending => Query.IsRoundResultPending;
            public bool IsGameEnded => Query.IsGameEnded;
            public bool IsWinDecisionPending => Query.IsWinDecisionPending;
            public bool IsReachDecisionPending => (bool)Reflection.GetProperty(
                CurrentState,
                "IsReachDecisionPending");
            public bool IsSelfKanDecisionPending => (bool)Reflection.GetProperty(
                CurrentState,
                "IsSelfKanDecisionPending");
            public bool IsReactionWindowPending => Query.IsReactionWindowPending;
            public object CurrentReactionWindow => Query.CurrentReactionWindow;
            public object PendingKakan => Reflection.GetProperty(CurrentState, "PendingKakan");
            public object CurrentSelfKanDecision => Reflection.GetProperty(
                CurrentState,
                "CurrentSelfKanDecision");
            public int ActiveSkillEffectCount => Query.ActiveSkillEffectCount;
            public bool HasLastTurnDraw => Query.HasLastTurnDraw;
            public int ReachDiscardCandidateCount => session.Collections.Count(
                Reflection.GetProperty(CurrentState, "ReachDiscardCandidates"));
            public string TurnPhaseName => Query.TurnPhaseName;
            public string CurrentTurnName => Query.CurrentTurnName;
            public string WindProgressRoundWindName => Query.WindProgressRoundWindName;
            public int WindProgressHandNumber => Query.WindProgressHandNumber;
            public int WallCount => Query.WallCount;
            public int DiscardCount => Query.DiscardCount;
            public string SelfSeatName => Query.SelfSeatName;
            public string RoundResultTypeName => Query.RoundResultTypeName;
            public string RoundResultWinnerSeatName => Query.RoundResultWinnerSeatNameOrNull;
            public string RoundResultWinTypeName => Query.RoundResultWinTypeNameOrNull;
            public string RoundResultSourceSeatName => Query.RoundResultSourceSeatNameOrNull;
            public string RoundResultWinningTileCode => Query.RoundResultWinningTileCodeOrNull;
            public object RoundResultSelectedCandidate => Query.RoundResultSelectedCandidate;
            public int RoundResultYakuCount => Query.RoundResultYakuCount;
            public int RoundResultTotalHan => Query.RoundResultTotalHan;
            public int RoundResultTotalYakumanMultiplier =>
                Query.RoundResultTotalYakumanMultiplier;
            public int RoundResultYakumanCount => Query.RoundResultYakumanCount;
            public bool RoundResultIsFinalRound => Query.RoundResultIsFinalRound;
            public string RoundResultAbortiveDrawKindName =>
                Query.RoundResultAbortiveDrawKindNameOrNull;

            public int RoundStartedCount => roundStartedCount;

            public void StartNewRound()
            {
                SubscribeRoundStartedCounter();
                Commands.StartNewRound();
            }

            public void StartRound(string roundWindName, int handNumber, string selfSeatName)
            {
                SubscribeRoundStartedCounter();
                object windProgress = DataFactory.CreateWindProgress(roundWindName, handNumber);
                Reflection.InvokeWithSignature(
                    session.GameFlow,
                    "StartRound",
                    new[] { Types.WindProgress, typeof(bool), Types.SeatId },
                    windProgress,
                    false,
                    DataFactory.ParseSeat(selfSeatName));
            }

            public void PrepareTsumoDecision(string handText, string winningTileCode)
            {
                StartNewRound();
                DataFactory.AddHandTilesFromText(Query.GetPlayerSeat("East"), handText);
                Commands.RequestForceDrawSkill(winningTileCode);
                Commands.RequestDraw();
                Assert.That(Query.IsWinDecisionPending, Is.True);
            }

            public void PrepareRonDecision(
                string handText,
                string winningTileCode,
                string sourceSeatName)
            {
                StartNewRound();
                DataFactory.SetParticipantType(session.CurrentState, sourceSeatName, "LocalHuman");
                DataFactory.AddHandTilesFromText(Query.GetPlayerSeat("East"), handText);
                Reflection.Invoke(Query.GetPlayerSeat("East"), "DeclareReach", 1);
                DataFactory.SetDrawnTile(session.CurrentState, sourceSeatName, winningTileCode);
                DataFactory.SetCurrentTurn(session.CurrentState, sourceSeatName);

                Assert.That(
                    Commands.TryRequestDiscardDrawnTileForSeat(sourceSeatName),
                    Is.True);
                Assert.That(Query.IsWinDecisionPending, Is.True);
            }

            public void PrepareReachDecision()
            {
                StartNewRound();
                DataFactory.AddHandTiles(
                    Query.GetPlayerSeat("East"),
                    "1m", "2m", "3m",
                    "2p", "3p", "4p",
                    "7s", "8s", "9s",
                    "E", "E", "E",
                    "5m");
                Commands.RequestForceDrawSkill("6m");
                Commands.RequestDraw();
                Assert.That(TurnPhaseName, Is.EqualTo("ReachDecision"));
            }

            public void PrepareAnkanDecision()
            {
                StartNewRound();
                DataFactory.AddHandTiles(
                    Query.GetPlayerSeat("East"),
                    "P", "P", "P",
                    "1m", "4m", "7m", "9m",
                    "1p", "4p", "7p",
                    "1s", "4s", "7s");
                DataFactory.SetDrawnTile(CurrentState, "East", "P");
                Commands.ResolveAfterDraw("East");
                Assert.That(TurnPhaseName, Is.EqualTo("SelfKanDecision"));
            }

            public object SelectPendingCandidate()
            {
                object evaluation = Query.PendingWinDeclarationEvaluation;
                object handEvaluation = Reflection.GetProperty(evaluation, "HandEvaluationResult");
                return Reflection.Invoke(selector, "Select", handEvaluation);
            }

            public void DeclareWin()
            {
                Commands.RequestDeclareWin();
            }

            public void AdvanceFromRoundResult()
            {
                Commands.RequestAdvanceFromRoundResult();
            }

            public void RunAuthorityUpdate()
            {
                Commands.RunAuthorityUpdate();
            }

            public bool TryEndAbortiveDraw(string kindName)
            {
                return Commands.TryEndAbortiveDraw(kindName);
            }

            public void RequestDraw()
            {
                Commands.RequestDraw();
            }

            public void RequestDiscardDrawnTile()
            {
                Commands.RequestDiscardDrawnTile();
            }

            public void RequestDeclineWin()
            {
                Commands.RequestDeclineWin();
            }

            public void RequestDeclareReach()
            {
                Reflection.Invoke(session.GameFlow, "RequestDeclareReach");
            }

            public bool TryRequestDeclareAnkan(string tileCode)
            {
                return Commands.TryRequestDeclareAnkanForSeat("East", tileCode);
            }

            public void RequestForceDrawSkill(string tileCode)
            {
                Commands.RequestForceDrawSkill(tileCode);
            }

            public bool TryDrawForCurrentTurn()
            {
                return Commands.TryRequestDrawForSeat(CurrentTurnName);
            }

            public void ClearWall()
            {
                object wall = Reflection.GetProperty(session.CurrentState, "Wall");
                IList tiles = (IList)Reflection.GetPrivateField(wall, "tiles");
                tiles.Clear();
            }

            public string SeatByPlayerIdName(string playerIdName)
            {
                return Query.SeatByPlayerIdName(playerIdName);
            }

            public string ParticipantTypeName(string seatName)
            {
                return Query.ParticipantTypeNameOrNull(seatName);
            }

            public int HandCount(string seatName)
            {
                return Query.HandCount(seatName);
            }

            public bool RecentLogContains(string expectedText)
            {
                object lines = Reflection.GetStaticProperty(
                    Reflection.RequireType(
                        "MahjongPrototype.Logging.DevLog, Assembly-CSharp"),
                    "RecentLines");
                foreach (object line in (IEnumerable)lines)
                {
                    if (line != null && line.ToString().Contains(expectedText))
                        return true;
                }

                return false;
            }

            public object ReflectionProperty(object target, string propertyName)
            {
                return Reflection.GetProperty(target, propertyName);
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                roundStartedCounter?.Dispose();
                session.Dispose();
            }

            private void SubscribeRoundStartedCounter()
            {
                if (roundStartedCounter != null || EventNotifier == null)
                    return;

                roundStartedCounter = EventTrace.Subscribe(EventNotifier, "RoundStarted");
                roundStartedCounter.Recorded += () => roundStartedCount++;
            }

            private EventTrace roundStartedCounter;
            private int roundStartedCount;
            private ReflectionTestAccess Reflection => session.Reflection;
            private MahjongGameStateTestQuery Query => session.Query;
            private MahjongGameFlowTestCommands Commands => session.Commands;
            private MahjongTestDataFactory DataFactory => session.DataFactory;
            private MahjongTestTypes Types => session.Types;
        }

        private sealed class EventTrace : IDisposable
        {
            private readonly object eventSource;
            private readonly List<EventSubscription> subscriptions =
                new List<EventSubscription>();
            private readonly List<string> names = new List<string>();
            private readonly List<object> payloads = new List<object>();
            private bool disposed;

            private EventTrace(object eventSource)
            {
                this.eventSource = eventSource;
            }

            public event Action Recorded;

            public static EventTrace Subscribe(object eventSource, params string[] eventNames)
            {
                EventTrace trace = new EventTrace(eventSource);
                for (int i = 0; i < eventNames.Length; i++)
                    trace.Subscribe(eventNames[i]);

                return trace;
            }

            public int IndexOf(string eventName)
            {
                return names.IndexOf(eventName);
            }

            public int LastIndexOf(string eventName)
            {
                return names.LastIndexOf(eventName);
            }

            public object FirstPayload(string eventName)
            {
                int index = IndexOf(eventName);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), $"Event not found: {eventName}");
                return payloads[index];
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                for (int i = subscriptions.Count - 1; i >= 0; i--)
                    subscriptions[i].Remove(eventSource);

                subscriptions.Clear();
            }

            private void Subscribe(string eventName)
            {
                EventInfo eventInfo = eventSource.GetType().GetEvent(
                    eventName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");

                Delegate handler = CreateHandler(eventInfo.EventHandlerType, eventName);
                eventInfo.AddEventHandler(eventSource, handler);
                subscriptions.Add(new EventSubscription(eventInfo, handler));
            }

            private Delegate CreateHandler(Type eventHandlerType, string eventName)
            {
                MethodInfo invoke = eventHandlerType.GetMethod("Invoke");
                ParameterInfo[] parameterInfos = invoke.GetParameters();
                ParameterExpression[] parameters =
                    new ParameterExpression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameters[i] = Expression.Parameter(
                        parameterInfos[i].ParameterType,
                        parameterInfos[i].Name);
                }

                Expression payload = parameters.Length == 0
                    ? Expression.Constant(null, typeof(object))
                    : Expression.Convert(parameters[0], typeof(object));
                MethodInfo record = GetType().GetMethod(
                    nameof(Record),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                MethodCallExpression body = Expression.Call(
                    Expression.Constant(this),
                    record,
                    Expression.Constant(eventName),
                    payload);

                return Expression.Lambda(eventHandlerType, body, parameters).Compile();
            }

            private void Record(string eventName, object payload)
            {
                names.Add(eventName);
                payloads.Add(payload);
                Recorded?.Invoke();
            }

            private readonly struct EventSubscription
            {
                public EventSubscription(EventInfo eventInfo, Delegate handler)
                {
                    EventInfo = eventInfo;
                    Handler = handler;
                }

                private EventInfo EventInfo { get; }
                private Delegate Handler { get; }

                public void Remove(object source)
                {
                    EventInfo.RemoveEventHandler(source, Handler);
                }
            }
        }
    }
}
