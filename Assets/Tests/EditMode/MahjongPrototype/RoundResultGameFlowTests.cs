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
        public void WallEmptyDrawFailure_CreatesExhaustiveDrawAndAdvancesOnlyAfterConfirmation()
        {
            using (Driver driver = Driver.Create(participantCount: 1))
            {
                driver.StartNewRound();
                driver.ClearWall();

                Assert.That(driver.TryDrawForCurrentTurn(), Is.False);

                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("ExhaustiveDraw"));
                Assert.That(driver.RoundResultSelectedCandidate, Is.Null);
                Assert.That(driver.RoundResultYakuCount, Is.EqualTo(0));
                Assert.That(driver.RoundResultTotalHan, Is.EqualTo(0));
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(1));

                driver.AdvanceFromRoundResult();

                Assert.That(driver.IsRoundEnded, Is.False);
                Assert.That(driver.IsRoundResultPending, Is.False);
                Assert.That(driver.CurrentRoundResultIsNull, Is.True);
                Assert.That(driver.WindProgressHandNumber, Is.EqualTo(2));
            }
        }

        [Test]
        public void FinalRoundConfirmation_MovesToGameEndedAndKeepsFinalResult()
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
                driver.AdvanceFromRoundResult();

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
        public void RoundResultPending_BlocksRepresentativeOperationsUntilAdvanceRequest()
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
        public void Notifications_FinalRoundConfirmation_ConfirmsBeforeGameEnded()
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

                driver.AdvanceFromRoundResult();

                Assert.That(trace.IndexOf("RoundResultReady"), Is.GreaterThanOrEqualTo(0));
                Assert.That(trace.IndexOf("RoundResultConfirmed"), Is.LessThan(trace.IndexOf("GameEnded")));
                Assert.That(trace.FirstPayload("RoundResultReady"), Is.SameAs(result));
                Assert.That(trace.FirstPayload("RoundResultConfirmed"), Is.SameAs(result));
                Assert.That(trace.FirstPayload("GameEnded"), Is.SameAs(result));
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

            public static Driver Create(int participantCount)
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory =
                    new MahjongTestDataFactory(reflection, types);
                object catalog =
                    MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "RoundResultGameFlowTest",
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
            public object CurrentState => session.CurrentState;
            public object CurrentRoundResult => Query.CurrentRoundResult;
            public bool CurrentRoundResultIsNull => Query.CurrentRoundResultIsNull;
            public bool IsRoundEnded => Query.IsRoundEnded;
            public bool IsRoundResultPending => Query.IsRoundResultPending;
            public bool IsGameEnded => Query.IsGameEnded;
            public string TurnPhaseName => Query.TurnPhaseName;
            public string CurrentTurnName => Query.CurrentTurnName;
            public string WindProgressRoundWindName => Query.WindProgressRoundWindName;
            public int WindProgressHandNumber => Query.WindProgressHandNumber;
            public int WallCount => Query.WallCount;
            public int DiscardCount => Query.DiscardCount;
            public string RoundResultTypeName => Query.RoundResultTypeName;
            public string RoundResultWinnerSeatName => Query.RoundResultWinnerSeatNameOrNull;
            public string RoundResultWinTypeName => Query.RoundResultWinTypeNameOrNull;
            public string RoundResultSourceSeatName => Query.RoundResultSourceSeatNameOrNull;
            public string RoundResultWinningTileCode => Query.RoundResultWinningTileCodeOrNull;
            public object RoundResultSelectedCandidate => Query.RoundResultSelectedCandidate;
            public int RoundResultYakuCount => Query.RoundResultYakuCount;
            public int RoundResultTotalHan => Query.RoundResultTotalHan;
            public bool RoundResultIsFinalRound => Query.RoundResultIsFinalRound;

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

            public void RequestDraw()
            {
                Commands.RequestDraw();
            }

            public void RequestDiscardDrawnTile()
            {
                Commands.RequestDiscardDrawnTile();
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
