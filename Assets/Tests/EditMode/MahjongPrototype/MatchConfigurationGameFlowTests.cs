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
    public sealed class MatchConfigurationGameFlowTests
    {
        private const string MatchRosterTypeName =
            "MahjongPrototype.Domain.MatchRoster, Assembly-CSharp";
        private const string MatchParticipantTypeName =
            "MahjongPrototype.Domain.MatchParticipant, Assembly-CSharp";
        private const string ParticipantKindTypeName =
            "MahjongPrototype.Domain.ParticipantKind, Assembly-CSharp";
        private const string DecisionProviderRegistryTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistry, Assembly-CSharp";
        private const string DecisionProviderRegistrationTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistration, Assembly-CSharp";
        private const string DecisionProviderRouteTypeName =
            "MahjongPrototype.Domain.DecisionProviderRoute, Assembly-CSharp";
        private const string ParticipantTypeAdapterTypeName =
            "MahjongPrototype.Domain.ParticipantTypeAdapter, Assembly-CSharp";

        [Test]
        public void LegacySceneConfiguration_StartsLocalHumanAndCpuMatchThroughRosterProjection()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                object result = session.Reflection.Invoke(session.GameFlow, "TryStartNewRound");

                Assert.That((bool)session.Reflection.GetProperty(result, "IsValid"), Is.True);
                object roster = session.Reflection.GetProperty(session.GameFlow, "MatchRoster");
                object registry = session.Reflection.GetProperty(
                    session.GameFlow,
                    "DecisionProviderRegistry");
                IList participants = (IList)session.Reflection.GetProperty(roster, "Participants");

                Assert.That(participants.Count, Is.EqualTo(2));
                Assert.That(
                    session.Reflection.GetProperty(participants[0], "PlayerId").ToString(),
                    Is.EqualTo("Player1"));
                Assert.That(
                    session.Reflection.GetProperty(participants[0], "Kind").ToString(),
                    Is.EqualTo("Human"));
                Assert.That(
                    session.Reflection.GetProperty(participants[1], "PlayerId").ToString(),
                    Is.EqualTo("Player2"));
                Assert.That(
                    session.Reflection.GetProperty(participants[1], "Kind").ToString(),
                    Is.EqualTo("Cpu"));
                Assert.That(ResolveRegistration(session.Reflection, registry, "Player1", session.Types),
                    Is.EqualTo("LocalUi"));
                Assert.That(ResolveRegistration(session.Reflection, registry, "Player2", session.Types),
                    Is.EqualTo("CpuAgent"));
                Assert.That(
                    session.Query.ParticipantTypeNameOrNullForPlayerId("Player1"),
                    Is.EqualTo("LocalHuman"));
                Assert.That(
                    session.Query.ParticipantTypeNameOrNullForPlayerId("Player2"),
                    Is.EqualTo("Cpu"));
            }
        }

        [TestCase("LocalHuman", "Human", "LocalUi", true)]
        [TestCase("Cpu", "Cpu", "CpuAgent", true)]
        [TestCase("RemoteHuman", "Human", "Network", false)]
        public void ParticipantTypeAdapter_MapsLegacyValuesOneWay(
            string legacyTypeName,
            string expectedKind,
            string expectedRoute,
            bool expectedAvailable)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            Type adapterType = reflection.RequireType(ParticipantTypeAdapterTypeName);
            object playerId = Enum.Parse(types.PlayerId, "Player2");
            object participantType = Enum.Parse(types.ParticipantType, legacyTypeName);

            object participant = reflection.InvokeStatic(
                adapterType,
                "ToMatchParticipant",
                playerId,
                participantType);
            object registration = reflection.InvokeStatic(
                adapterType,
                "ToDecisionProviderRegistration",
                playerId,
                participantType);

            Assert.That(reflection.GetProperty(participant, "Kind").ToString(), Is.EqualTo(expectedKind));
            Assert.That(reflection.GetProperty(registration, "PlayerId").ToString(), Is.EqualTo("Player2"));
            Assert.That(reflection.GetProperty(registration, "Route").ToString(), Is.EqualTo(expectedRoute));
            Assert.That(
                (bool)reflection.GetProperty(registration, "IsAvailable"),
                Is.EqualTo(expectedAvailable));
        }

        [Test]
        public void DecisionProviderRegistry_ResolvesByPlayerIdRatherThanSeat()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            object registry = CreateRegistry(
                reflection,
                types,
                new ProviderDefinition("Player1", "LocalUi", true),
                new ProviderDefinition("Player2", "CpuAgent", true));
            object[] arguments = { Enum.Parse(types.PlayerId, "Player2"), null };

            Assert.That((bool)reflection.Invoke(registry, "TryResolve", arguments), Is.True);
            Assert.That(reflection.GetProperty(arguments[1], "Route").ToString(), Is.EqualTo("CpuAgent"));
        }

        [Test]
        public void NextRound_ChangesSeatButPreservesPlayerProviderBinding()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                string firstPlayer2Seat = session.Query.SeatByPlayerIdName("Player2");
                object registryBefore = session.Reflection.GetProperty(
                    session.GameFlow,
                    "DecisionProviderRegistry");

                session.Reflection.Invoke(session.GameFlow, "EndRound", "WallEmpty");
                session.Commands.RequestAdvanceFromRoundResult();

                Assert.That(session.Query.SeatByPlayerIdName("Player2"), Is.Not.EqualTo(firstPlayer2Seat));
                Assert.That(
                    session.Reflection.GetProperty(session.GameFlow, "DecisionProviderRegistry"),
                    Is.SameAs(registryBefore));
                Assert.That(
                    ResolveRegistration(session.Reflection, registryBefore, "Player2", session.Types),
                    Is.EqualTo("CpuAgent"));
                Assert.That(
                    session.Query.ParticipantTypeNameOrNullForPlayerId("Player2"),
                    Is.EqualTo("Cpu"));
            }
        }

        [TestCase("MissingProvider", "does not have a decision provider")]
        [TestCase("DuplicateProvider", "multiple decision providers")]
        [TestCase("KindMismatch", "incompatible")]
        [TestCase("UnavailableNetwork", "is unavailable")]
        [TestCase("ConfiguredProviderWithoutRuntimeInstance", "runtime implementation")]
        [TestCase("DuplicatePlayer", "duplicate PlayerId")]
        public void InvalidProviderConfiguration_IsRejectedBeforeRoundStateOrNotifications(
            string invalidConfiguration,
            string expectedReason)
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                object previousState = session.CurrentState;
                CreateInvalidConfiguration(
                    session.Reflection,
                    session.Types,
                    invalidConfiguration,
                    out object roster,
                    out object registry);
                session.Reflection.Invoke(session.GameFlow, "ConfigureMatch", roster, registry);

                using (EventCounter events = EventCounter.Subscribe(
                    session.EventNotifier,
                    "RunStarted",
                    "SeatSlotsAssigned",
                    "RoundStarted",
                    "TileDrawn",
                    "RoundSetupCompleted"))
                {
                    object result = session.Reflection.Invoke(session.GameFlow, "TryStartNewRound");

                    Assert.That((bool)session.Reflection.GetProperty(result, "IsValid"), Is.False);
                    Assert.That(
                        (string)session.Reflection.GetProperty(result, "FailureReason"),
                        Does.Contain(expectedReason));
                    Assert.That(session.CurrentState, Is.SameAs(previousState));
                    Assert.That(events.TotalCount, Is.EqualTo(0));
                }
            }
        }

        private static MahjongGameFlowTestSession CreateSession(int participantCount)
        {
            return MahjongGameFlowTestSession.Create(new MahjongGameFlowTestOptions
            {
                RootName = "MatchConfigurationGameFlowTest",
                AddEventNotifier = true,
                LogWarnings = false,
                ParticipantCount = participantCount,
                InitialHandTileCount = 1,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = false,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East"
            });
        }

        private static void CreateInvalidConfiguration(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            string invalidConfiguration,
            out object roster,
            out object registry)
        {
            switch (invalidConfiguration)
            {
                case "MissingProvider":
                    roster = CreateRoster(
                        reflection,
                        types,
                        new ParticipantDefinition("Player1", "Human"),
                        new ParticipantDefinition("Player2", "Cpu"));
                    registry = CreateRegistry(
                        reflection,
                        types,
                        new ProviderDefinition("Player1", "LocalUi", true));
                    return;
                case "DuplicateProvider":
                    roster = CreateRoster(
                        reflection,
                        types,
                        new ParticipantDefinition("Player1", "Human"));
                    registry = CreateRegistry(
                        reflection,
                        types,
                        new ProviderDefinition("Player1", "LocalUi", true),
                        new ProviderDefinition("Player1", "LocalUi", true));
                    return;
                case "KindMismatch":
                    roster = CreateRoster(
                        reflection,
                        types,
                        new ParticipantDefinition("Player1", "Human"));
                    registry = CreateRegistry(
                        reflection,
                        types,
                        new ProviderDefinition("Player1", "CpuAgent", true));
                    return;
                case "UnavailableNetwork":
                    roster = CreateRoster(
                        reflection,
                        types,
                        new ParticipantDefinition("Player1", "Human"));
                    registry = CreateRegistry(
                        reflection,
                        types,
                        new ProviderDefinition("Player1", "Network", false));
                    return;
                case "ConfiguredProviderWithoutRuntimeInstance":
                    roster = CreateRoster(
                        reflection,
                        types,
                        new ParticipantDefinition("Player1", "Human"));
                    registry = CreateRegistry(
                        reflection,
                        types,
                        new ProviderDefinition("Player1", "LocalUi", true));
                    return;
                case "DuplicatePlayer":
                    roster = CreateRoster(
                        reflection,
                        types,
                        new ParticipantDefinition("Player1", "Human"),
                        new ParticipantDefinition("Player1", "Human"));
                    registry = CreateRegistry(
                        reflection,
                        types,
                        new ProviderDefinition("Player1", "LocalUi", true));
                    return;
                default:
                    Assert.Fail($"Unknown invalid configuration: {invalidConfiguration}");
                    roster = null;
                    registry = null;
                    return;
            }
        }

        private static object CreateRoster(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            params ParticipantDefinition[] definitions)
        {
            Type matchParticipantType = reflection.RequireType(MatchParticipantTypeName);
            IList participants = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(matchParticipantType));
            Type participantKindType = reflection.RequireType(ParticipantKindTypeName);

            for (int i = 0; i < definitions.Length; i++)
            {
                ParticipantDefinition definition = definitions[i];
                participants.Add(reflection.CreateInstance(
                    matchParticipantType,
                    Enum.Parse(types.PlayerId, definition.PlayerId),
                    Enum.Parse(participantKindType, definition.Kind)));
            }

            return reflection.CreateInstance(
                reflection.RequireType(MatchRosterTypeName),
                participants);
        }

        private static object CreateRegistry(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            params ProviderDefinition[] definitions)
        {
            Type registrationType = reflection.RequireType(DecisionProviderRegistrationTypeName);
            IList registrations = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(registrationType));
            Type routeType = reflection.RequireType(DecisionProviderRouteTypeName);

            for (int i = 0; i < definitions.Length; i++)
            {
                ProviderDefinition definition = definitions[i];
                registrations.Add(reflection.CreateInstance(
                    registrationType,
                    Enum.Parse(types.PlayerId, definition.PlayerId),
                    Enum.Parse(routeType, definition.Route),
                    definition.IsAvailable));
            }

            return reflection.CreateInstance(
                reflection.RequireType(DecisionProviderRegistryTypeName),
                registrations);
        }

        private static string ResolveRegistration(
            ReflectionTestAccess reflection,
            object registry,
            string playerIdName,
            MahjongTestTypes types)
        {
            object[] arguments = { Enum.Parse(types.PlayerId, playerIdName), null };
            Assert.That((bool)reflection.Invoke(registry, "TryResolve", arguments), Is.True);
            return reflection.GetProperty(arguments[1], "Route").ToString();
        }

        private readonly struct ParticipantDefinition
        {
            public ParticipantDefinition(string playerId, string kind)
            {
                PlayerId = playerId;
                Kind = kind;
            }

            public string PlayerId { get; }
            public string Kind { get; }
        }

        private readonly struct ProviderDefinition
        {
            public ProviderDefinition(string playerId, string route, bool isAvailable)
            {
                PlayerId = playerId;
                Route = route;
                IsAvailable = isAvailable;
            }

            public string PlayerId { get; }
            public string Route { get; }
            public bool IsAvailable { get; }
        }

        private sealed class EventCounter : IDisposable
        {
            private readonly object source;
            private readonly List<EventSubscription> subscriptions =
                new List<EventSubscription>();
            private bool disposed;

            private EventCounter(object source)
            {
                this.source = source;
            }

            public int TotalCount { get; private set; }

            public static EventCounter Subscribe(object source, params string[] eventNames)
            {
                EventCounter counter = new EventCounter(source);
                for (int i = 0; i < eventNames.Length; i++)
                    counter.Subscribe(eventNames[i]);

                return counter;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                for (int i = subscriptions.Count - 1; i >= 0; i--)
                    subscriptions[i].EventInfo.RemoveEventHandler(source, subscriptions[i].Handler);
            }

            private void Subscribe(string eventName)
            {
                EventInfo eventInfo = source.GetType().GetEvent(
                    eventName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");

                MethodInfo invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
                ParameterInfo[] parameterInfos = invoke.GetParameters();
                ParameterExpression[] parameters = new ParameterExpression[parameterInfos.Length];
                for (int i = 0; i < parameterInfos.Length; i++)
                    parameters[i] = Expression.Parameter(parameterInfos[i].ParameterType, parameterInfos[i].Name);

                MethodInfo record = GetType().GetMethod(
                    nameof(Record),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Delegate handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Call(Expression.Constant(this), record),
                    parameters).Compile();
                eventInfo.AddEventHandler(source, handler);
                subscriptions.Add(new EventSubscription(eventInfo, handler));
            }

            private void Record()
            {
                TotalCount++;
            }

            private readonly struct EventSubscription
            {
                public EventSubscription(EventInfo eventInfo, Delegate handler)
                {
                    EventInfo = eventInfo;
                    Handler = handler;
                }

                public EventInfo EventInfo { get; }
                public Delegate Handler { get; }
            }
        }
    }
}
