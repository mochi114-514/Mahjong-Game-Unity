using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class RoundSetupGameFlowTests
    {
        [Test]
        public void StartNewRound_PreservesSetupNotificationOrder()
        {
            using (MahjongGameFlowTestSession session = CreateSession(initialHandTileCount: 2))
            using (EventTrace trace = EventTrace.Subscribe(
                session.EventNotifier,
                "RunStarted",
                "SeatSlotsAssigned",
                "RoundStarted",
                "TileDrawn",
                "RoundSetupCompleted",
                "TurnStarted"))
            {
                session.Commands.StartNewRound();

                Assert.That(
                    trace.Names,
                    Is.EqualTo(new[]
                    {
                        "RunStarted",
                        "SeatSlotsAssigned",
                        "RoundStarted",
                        "TileDrawn",
                        "TileDrawn",
                        "RoundSetupCompleted",
                        "TurnStarted"
                    }));
            }
        }

        [Test]
        public void StartNewRound_InitialDealFailurePreservesRoundEndAndNotificationOrder()
        {
            using (MahjongGameFlowTestSession session = CreateSession(initialHandTileCount: 137))
            using (EventTrace trace = EventTrace.Subscribe(
                session.EventNotifier,
                "RunStarted",
                "SeatSlotsAssigned",
                "RoundStarted",
                "TileDrawn",
                "RoundEnded",
                "RoundSetupCompleted",
                "TurnStarted"))
            {
                session.Commands.StartNewRound();

                List<string> expectedEvents = new List<string>
                {
                    "RunStarted",
                    "SeatSlotsAssigned",
                    "RoundStarted"
                };
                for (int i = 0; i < 136; i++)
                    expectedEvents.Add("TileDrawn");

                expectedEvents.Add("RoundEnded");
                expectedEvents.Add("RoundSetupCompleted");
                expectedEvents.Add("TurnStarted");

                Assert.That(session.Query.IsRoundEnded, Is.True);
                Assert.That(session.Query.IsRoundResultPending, Is.False);
                Assert.That(trace.Names, Is.EqualTo(expectedEvents));
            }
        }

        private static MahjongGameFlowTestSession CreateSession(int initialHandTileCount)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = "RoundSetupGameFlowTest",
                AddEventNotifier = true,
                LogWarnings = false,
                ParticipantCount = 1,
                InitialHandTileCount = initialHandTileCount,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = false,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East"
            };
            return MahjongGameFlowTestSession.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
        }

        private sealed class EventTrace : IDisposable
        {
            private readonly object source;
            private readonly List<EventSubscription> subscriptions =
                new List<EventSubscription>();
            private readonly List<string> names = new List<string>();
            private bool disposed;

            private EventTrace(object source)
            {
                this.source = source;
            }

            public IReadOnlyList<string> Names => names;

            public static EventTrace Subscribe(object source, params string[] eventNames)
            {
                EventTrace trace = new EventTrace(source);
                for (int i = 0; i < eventNames.Length; i++)
                    trace.Subscribe(eventNames[i]);

                return trace;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                for (int i = subscriptions.Count - 1; i >= 0; i--)
                    subscriptions[i].Remove(source);

                subscriptions.Clear();
            }

            private void Subscribe(string eventName)
            {
                EventInfo eventInfo = source.GetType().GetEvent(
                    eventName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");

                Delegate handler = CreateHandler(eventInfo.EventHandlerType, eventName);
                eventInfo.AddEventHandler(source, handler);
                subscriptions.Add(new EventSubscription(eventInfo, handler));
            }

            private Delegate CreateHandler(Type eventHandlerType, string eventName)
            {
                MethodInfo invoke = eventHandlerType.GetMethod("Invoke");
                ParameterInfo[] parameterInfos = invoke.GetParameters();
                ParameterExpression[] parameters =
                    new ParameterExpression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                    parameters[i] = Expression.Parameter(parameterInfos[i].ParameterType, parameterInfos[i].Name);

                MethodInfo record = GetType().GetMethod(
                    nameof(Record),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                MethodCallExpression body = Expression.Call(
                    Expression.Constant(this),
                    record,
                    Expression.Constant(eventName));
                return Expression.Lambda(eventHandlerType, body, parameters).Compile();
            }

            private void Record(string eventName)
            {
                names.Add(eventName);
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

                public void Remove(object eventSource)
                {
                    EventInfo.RemoveEventHandler(eventSource, Handler);
                }
            }
        }
    }
}
