using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReactionDecisionContractTests
    {
        private const string DecisionRequestTypeName =
            "MahjongPrototype.Domain.DecisionRequest, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
        private const string ReactionDecisionRequestTypeName =
            "MahjongPrototype.Domain.ReactionDecisionRequest, Assembly-CSharp";
        private const string ReactionDecisionResponseTypeName =
            "MahjongPrototype.Domain.ReactionDecisionResponse, Assembly-CSharp";
        private const string ReactionDecisionOptionTypeName =
            "MahjongPrototype.Domain.ReactionDecisionOption, Assembly-CSharp";
        private const string ReactionDecisionChiOptionTypeName =
            "MahjongPrototype.Domain.ReactionDecisionChiOption, Assembly-CSharp";
        private const string ReactionWindowSourceKindTypeName =
            "MahjongPrototype.Domain.ReactionWindowSourceKind, Assembly-CSharp";
        private const string ReactionWindowSeatAnswerKindTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerKind, Assembly-CSharp";
        private const string DecisionCoordinatorTypeName =
            "MahjongPrototype.DecisionCoordinator, Assembly-CSharp";
        private const string LocalUiDecisionProviderTypeName =
            "MahjongPrototype.LocalUiDecisionProvider, Assembly-CSharp";
        private const string DecisionProviderRegistryTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistry, Assembly-CSharp";
        private const string DecisionProviderRegistrationTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistration, Assembly-CSharp";
        private const string DecisionProviderRouteTypeName =
            "MahjongPrototype.Domain.DecisionProviderRoute, Assembly-CSharp";

        [Test]
        public void ReactionDecisionRequest_CopiesSourceAndOptionProjection()
        {
            Fixture fixture = CreateFixture();
            IList handTiles = CreateTypedList(
                fixture.Types.Tile,
                fixture.DataFactory.CreateTile("3m"),
                fixture.DataFactory.CreateTile("4m"));
            IList meldTiles = CreateTypedList(
                fixture.Types.Tile,
                fixture.DataFactory.CreateTile("3m"),
                fixture.DataFactory.CreateTile("4m"),
                fixture.DataFactory.CreateTile("5m"));
            object chiOption = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionChiOptionType,
                3,
                handTiles,
                meldTiles);
            IList chiOptions = CreateTypedList(fixture.ReactionDecisionChiOptionType, chiOption);
            object pass = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionOptionType,
                ParseAnswerKind(fixture, "Pass"),
                null);
            object chi = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionOptionType,
                ParseAnswerKind(fixture, "Chi"),
                chiOptions);
            IList options = CreateTypedList(fixture.ReactionDecisionOptionType, pass, chi);
            object sourceTile = fixture.DataFactory.CreateTile("5m");
            object request = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionRequestType,
                71,
                Enum.Parse(fixture.ReactionWindowSourceKindType, "Discard"),
                fixture.DataFactory.ParseSeat("East"),
                sourceTile,
                19,
                options);

            handTiles[0] = fixture.DataFactory.CreateTile("1m");
            meldTiles[0] = fixture.DataFactory.CreateTile("1m");
            chiOptions.Clear();
            options.Clear();

            Assert.That((int)fixture.Reflection.GetProperty(request, "WindowId"), Is.EqualTo(71));
            Assert.That(fixture.Reflection.GetProperty(request, "SourceKind").ToString(),
                Is.EqualTo("Discard"));
            Assert.That(fixture.Reflection.GetProperty(request, "SourceSeat").ToString(),
                Is.EqualTo("East"));
            Assert.That(fixture.Reflection.GetProperty(request, "SourceTile"), Is.EqualTo(sourceTile));
            Assert.That((int)fixture.Reflection.GetProperty(request, "SourceTurnIndex"), Is.EqualTo(19));

            object copiedOptions = fixture.Reflection.GetProperty(request, "Options");
            Assert.That(fixture.Collections.Count(copiedOptions), Is.EqualTo(2));
            object copiedChi = fixture.Collections.Item(copiedOptions, 1);
            Assert.That(fixture.Reflection.GetProperty(copiedChi, "Kind").ToString(), Is.EqualTo("Chi"));
            object copiedChiOptions = fixture.Reflection.GetProperty(copiedChi, "ChiOptions");
            Assert.That(fixture.Collections.Count(copiedChiOptions), Is.EqualTo(1));
            object copiedChiOption = fixture.Collections.Item(copiedChiOptions, 0);
            Assert.That((int)fixture.Reflection.GetProperty(copiedChiOption, "OptionId"), Is.EqualTo(3));
            Assert.That(
                fixture.Reflection.GetProperty(
                    fixture.Collections.Item(
                        fixture.Reflection.GetProperty(copiedChiOption, "HandTiles"),
                        0),
                    "Code"),
                Is.EqualTo("3m"));
            Assert.That(
                fixture.Reflection.GetProperty(
                    fixture.Collections.Item(
                        fixture.Reflection.GetProperty(copiedChiOption, "MeldTiles"),
                        0),
                    "Code"),
                Is.EqualTo("3m"));
        }

        [Test]
        public void Coordinator_RejectsInvalidReactionPayloadsWithoutDroppingLocalUiRetry()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                Fixture fixture = CreateFixture(session);
                object provider;
                object coordinator = CreateCoordinator(fixture, session.GameFlow, out provider);
                object reactionRequest = CreateChiRequest(fixture, 81, 3);
                object request = CreateDecisionRequest(
                    fixture,
                    811,
                    "Player1",
                    "East",
                    19,
                    reactionRequest);

                AssertAccepted(fixture.Reflection.Invoke(coordinator, "Request", request));
                Assert.That((int)fixture.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(1));

                object wrongWindow = CreateReactionResponse(
                    fixture,
                    812,
                    "Chi",
                    3);
                AssertRejected(
                    fixture.Reflection.Invoke(
                        coordinator,
                        "ReceiveResponse",
                        CreateDecisionResponse(fixture, 811, "Player1", "East", 19, wrongWindow)),
                    "ReactionWindowMismatch");
                AssertPendingWithoutQueue(fixture, coordinator);

                object unavailablePon = CreateReactionResponse(fixture, 81, "Pon");
                AssertRejected(
                    fixture.Reflection.Invoke(
                        coordinator,
                        "ReceiveResponse",
                        CreateDecisionResponse(fixture, 811, "Player1", "East", 19, unavailablePon)),
                    "ReactionKindUnavailable");
                AssertPendingWithoutQueue(fixture, coordinator);

                object invalidChi = CreateReactionResponse(fixture, 81, "Chi", 4);
                Assert.That(
                    (bool)fixture.Reflection.Invoke(
                        provider,
                        "TrySubmitResponse",
                        CreateDecisionResponse(fixture, 811, "Player1", "East", 19, invalidChi)),
                    Is.False,
                    "The Local UI provider must retain its callback after an invalid payload.");
                AssertPendingWithoutQueue(fixture, coordinator);

                object chiOptionOnPass = CreateReactionResponse(fixture, 81, "Pass", 3);
                AssertRejected(
                    fixture.Reflection.Invoke(
                        coordinator,
                        "ReceiveResponse",
                        CreateDecisionResponse(fixture, 811, "Player1", "East", 19, chiOptionOnPass)),
                    "ChiOptionNotAllowed");
                AssertPendingWithoutQueue(fixture, coordinator);

                object validChi = CreateReactionResponse(fixture, 81, "Chi", 3);
                Assert.That(
                    (bool)fixture.Reflection.Invoke(
                        provider,
                        "TrySubmitResponse",
                        CreateDecisionResponse(fixture, 811, "Player1", "East", 19, validChi)),
                    Is.True,
                    "The same Local UI request must accept a corrected response.");
                Assert.That((int)fixture.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(0));
                Assert.That((int)fixture.Reflection.GetProperty(coordinator, "QueuedResponseCount"), Is.EqualTo(1));
                Assert.That(
                    (bool)fixture.Reflection.Invoke(
                        provider,
                        "TrySubmitResponse",
                        CreateDecisionResponse(fixture, 811, "Player1", "East", 19, validChi)),
                    Is.False,
                    "A valid response remains one-shot after it has been queued.");
            }
        }

        private static Fixture CreateFixture(MahjongGameFlowTestSession session = null)
        {
            ReflectionTestAccess reflection = session != null
                ? session.Reflection
                : new ReflectionTestAccess();
            CollectionTestAccess collections = session != null
                ? session.Collections
                : new CollectionTestAccess(reflection);
            MahjongTestTypes types = session != null
                ? session.Types
                : new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = session != null
                ? session.DataFactory
                : new MahjongTestDataFactory(reflection, types);
            return new Fixture(
                reflection,
                collections,
                types,
                dataFactory,
                reflection.RequireType(DecisionRequestTypeName),
                reflection.RequireType(DecisionResponseTypeName),
                reflection.RequireType(DecisionKindTypeName),
                reflection.RequireType(ReactionDecisionRequestTypeName),
                reflection.RequireType(ReactionDecisionResponseTypeName),
                reflection.RequireType(ReactionDecisionOptionTypeName),
                reflection.RequireType(ReactionDecisionChiOptionTypeName),
                reflection.RequireType(ReactionWindowSourceKindTypeName),
                reflection.RequireType(ReactionWindowSeatAnswerKindTypeName),
                reflection.RequireType(DecisionCoordinatorTypeName),
                reflection.RequireType(LocalUiDecisionProviderTypeName),
                reflection.RequireType(DecisionProviderRegistryTypeName),
                reflection.RequireType(DecisionProviderRegistrationTypeName),
                reflection.RequireType(DecisionProviderRouteTypeName));
        }

        private static MahjongGameFlowTestSession CreateSession()
        {
            return MahjongGameFlowTestSession.Create(new MahjongGameFlowTestOptions
            {
                RootName = "ReactionDecisionContractTest",
                AddEventNotifier = true,
                LogWarnings = false,
                ParticipantCount = 1,
                InitialHandTileCount = 1,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = false,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East"
            });
        }

        private static object CreateCoordinator(
            Fixture fixture,
            object authority,
            out object provider)
        {
            provider = fixture.Reflection.CreateInstance(fixture.LocalUiDecisionProviderType);
            IList registrations = CreateTypedList(
                fixture.DecisionProviderRegistrationType,
                fixture.Reflection.CreateInstance(
                    fixture.DecisionProviderRegistrationType,
                    fixture.DataFactory.ParsePlayerId("Player1"),
                    Enum.Parse(fixture.DecisionProviderRouteType, "LocalUi"),
                    provider));
            object registry = fixture.Reflection.CreateInstance(
                fixture.DecisionProviderRegistryType,
                registrations);
            return fixture.Reflection.CreateInstance(
                fixture.DecisionCoordinatorType,
                registry,
                authority);
        }

        private static object CreateChiRequest(Fixture fixture, int windowId, int chiOptionId)
        {
            IList handTiles = CreateTypedList(
                fixture.Types.Tile,
                fixture.DataFactory.CreateTile("3m"),
                fixture.DataFactory.CreateTile("4m"));
            IList meldTiles = CreateTypedList(
                fixture.Types.Tile,
                fixture.DataFactory.CreateTile("3m"),
                fixture.DataFactory.CreateTile("4m"),
                fixture.DataFactory.CreateTile("5m"));
            object chiOption = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionChiOptionType,
                chiOptionId,
                handTiles,
                meldTiles);
            object pass = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionOptionType,
                ParseAnswerKind(fixture, "Pass"),
                null);
            object chi = fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionOptionType,
                ParseAnswerKind(fixture, "Chi"),
                CreateTypedList(fixture.ReactionDecisionChiOptionType, chiOption));
            return fixture.Reflection.CreateInstance(
                fixture.ReactionDecisionRequestType,
                windowId,
                Enum.Parse(fixture.ReactionWindowSourceKindType, "Discard"),
                fixture.DataFactory.ParseSeat("East"),
                fixture.DataFactory.CreateTile("5m"),
                19,
                CreateTypedList(fixture.ReactionDecisionOptionType, pass, chi));
        }

        private static object CreateDecisionRequest(
            Fixture fixture,
            long requestId,
            string playerId,
            string seat,
            int turnIndex,
            object reaction)
        {
            return fixture.Reflection.CreateInstance(
                fixture.DecisionRequestType,
                requestId,
                Enum.Parse(fixture.DecisionKindType, "Reaction"),
                fixture.DataFactory.ParsePlayerId(playerId),
                fixture.DataFactory.ParseSeat(seat),
                turnIndex,
                reaction);
        }

        private static object CreateDecisionResponse(
            Fixture fixture,
            long requestId,
            string playerId,
            string seat,
            int turnIndex,
            object reaction)
        {
            return fixture.Reflection.CreateInstance(
                fixture.DecisionResponseType,
                requestId,
                Enum.Parse(fixture.DecisionKindType, "Reaction"),
                fixture.DataFactory.ParsePlayerId(playerId),
                fixture.DataFactory.ParseSeat(seat),
                turnIndex,
                true,
                reaction);
        }

        private static object CreateReactionResponse(
            Fixture fixture,
            int windowId,
            string kind,
            int? chiOptionId = null)
        {
            return chiOptionId.HasValue
                ? fixture.Reflection.CreateInstance(
                    fixture.ReactionDecisionResponseType,
                    windowId,
                    ParseAnswerKind(fixture, kind),
                    chiOptionId.Value)
                : fixture.Reflection.CreateInstance(
                    fixture.ReactionDecisionResponseType,
                    windowId,
                    ParseAnswerKind(fixture, kind),
                    null);
        }

        private static object ParseAnswerKind(Fixture fixture, string kind)
        {
            return Enum.Parse(fixture.ReactionWindowSeatAnswerKindType, kind);
        }

        private static IList CreateTypedList(Type itemType, params object[] values)
        {
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
            for (int i = 0; i < values.Length; i++)
                list.Add(values[i]);

            return list;
        }

        private static void AssertPendingWithoutQueue(Fixture fixture, object coordinator)
        {
            Assert.That((int)fixture.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(1));
            Assert.That((int)fixture.Reflection.GetProperty(coordinator, "QueuedResponseCount"), Is.EqualTo(0));
        }

        private static void AssertAccepted(object result)
        {
            Assert.That((bool)result.GetType().GetProperty("Accepted").GetValue(result), Is.True);
        }

        private static void AssertRejected(object result, string expectedReason)
        {
            Assert.That((bool)result.GetType().GetProperty("Accepted").GetValue(result), Is.False);
            Assert.That(
                (string)result.GetType().GetProperty("Reason").GetValue(result),
                Is.EqualTo(expectedReason));
        }

        private sealed class Fixture
        {
            public Fixture(
                ReflectionTestAccess reflection,
                CollectionTestAccess collections,
                MahjongTestTypes types,
                MahjongTestDataFactory dataFactory,
                Type decisionRequestType,
                Type decisionResponseType,
                Type decisionKindType,
                Type reactionDecisionRequestType,
                Type reactionDecisionResponseType,
                Type reactionDecisionOptionType,
                Type reactionDecisionChiOptionType,
                Type reactionWindowSourceKindType,
                Type reactionWindowSeatAnswerKindType,
                Type decisionCoordinatorType,
                Type localUiDecisionProviderType,
                Type decisionProviderRegistryType,
                Type decisionProviderRegistrationType,
                Type decisionProviderRouteType)
            {
                Reflection = reflection;
                Collections = collections;
                Types = types;
                DataFactory = dataFactory;
                DecisionRequestType = decisionRequestType;
                DecisionResponseType = decisionResponseType;
                DecisionKindType = decisionKindType;
                ReactionDecisionRequestType = reactionDecisionRequestType;
                ReactionDecisionResponseType = reactionDecisionResponseType;
                ReactionDecisionOptionType = reactionDecisionOptionType;
                ReactionDecisionChiOptionType = reactionDecisionChiOptionType;
                ReactionWindowSourceKindType = reactionWindowSourceKindType;
                ReactionWindowSeatAnswerKindType = reactionWindowSeatAnswerKindType;
                DecisionCoordinatorType = decisionCoordinatorType;
                LocalUiDecisionProviderType = localUiDecisionProviderType;
                DecisionProviderRegistryType = decisionProviderRegistryType;
                DecisionProviderRegistrationType = decisionProviderRegistrationType;
                DecisionProviderRouteType = decisionProviderRouteType;
            }

            public ReflectionTestAccess Reflection { get; }
            public CollectionTestAccess Collections { get; }
            public MahjongTestTypes Types { get; }
            public MahjongTestDataFactory DataFactory { get; }
            public Type DecisionRequestType { get; }
            public Type DecisionResponseType { get; }
            public Type DecisionKindType { get; }
            public Type ReactionDecisionRequestType { get; }
            public Type ReactionDecisionResponseType { get; }
            public Type ReactionDecisionOptionType { get; }
            public Type ReactionDecisionChiOptionType { get; }
            public Type ReactionWindowSourceKindType { get; }
            public Type ReactionWindowSeatAnswerKindType { get; }
            public Type DecisionCoordinatorType { get; }
            public Type LocalUiDecisionProviderType { get; }
            public Type DecisionProviderRegistryType { get; }
            public Type DecisionProviderRegistrationType { get; }
            public Type DecisionProviderRouteType { get; }
        }
    }
}
