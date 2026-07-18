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
    public sealed class PlayerActionDecisionContractTests
    {
        private const string DecisionRequestTypeName =
            "MahjongPrototype.Domain.DecisionRequest, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";
        private const string DecisionResponseResultTypeName =
            "MahjongPrototype.Domain.DecisionResponseResult, Assembly-CSharp";
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
        private const string SelfKanCandidateTypeName =
            "MahjongPrototype.Domain.SelfKanCandidate, Assembly-CSharp";
        private const string SelfKanKindTypeName =
            "MahjongPrototype.Domain.SelfKanKind, Assembly-CSharp";
        private const string SelfKanTileLocationTypeName =
            "MahjongPrototype.Domain.SelfKanTileLocation, Assembly-CSharp";
        private const string SelfKanDecisionRequestTypeName =
            "MahjongPrototype.Domain.SelfKanDecisionRequest, Assembly-CSharp";
        private const string SelfKanDecisionResponseTypeName =
            "MahjongPrototype.Domain.SelfKanDecisionResponse, Assembly-CSharp";
        private const string DecisionCoordinatorTypeName =
            "MahjongPrototype.DecisionCoordinator, Assembly-CSharp";
        private const string LocalUiDecisionProviderTypeName =
            "MahjongPrototype.LocalUiDecisionProvider, Assembly-CSharp";
        private const string CpuAgentDecisionProviderTypeName =
            "MahjongPrototype.CpuAgentDecisionProvider, Assembly-CSharp";
        private const string DecisionProviderRegistryTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistry, Assembly-CSharp";
        private const string DecisionProviderRegistrationTypeName =
            "MahjongPrototype.Domain.DecisionProviderRegistration, Assembly-CSharp";
        private const string DecisionProviderRouteTypeName =
            "MahjongPrototype.Domain.DecisionProviderRoute, Assembly-CSharp";

        [Test]
        public void SelfKanDecisionRequest_CopiesOptionsAndRejectsInvalidPayloads()
        {
            Fixture fixture = CreateFixture();
            IList candidates = CreateTypedList(
                fixture.SelfKanCandidateType,
                CreateSelfKanCandidate(
                    fixture,
                    "Ankan",
                    "East",
                    "5m",
                    "Hand",
                    12,
                    -1),
                CreateSelfKanCandidate(
                    fixture,
                    "Kakan",
                    "East",
                    "P",
                    "DrawnTile",
                    12,
                    3));
            object request = fixture.Reflection.CreateInstance(
                fixture.SelfKanDecisionRequestType,
                candidates);
            candidates.Clear();

            object options = fixture.Reflection.GetProperty(request, "Options");
            Assert.That(fixture.Collections.Count(options), Is.EqualTo(2));
            Assert.That(
                fixture.Reflection.GetProperty(
                    fixture.Collections.Item(options, 0),
                    "Kind").ToString(),
                Is.EqualTo("Ankan"));
            Assert.That(
                (int)fixture.Reflection.GetProperty(
                    fixture.Collections.Item(options, 1),
                    "SourcePonMeldIndex"),
                Is.EqualTo(3));

            object declineWithOption = CreateSelfKanDecisionResponse(
                fixture,
                401,
                "Player1",
                "East",
                12,
                false,
                0);
            Assert.That(
                TryValidateSelfKanResponse(
                    fixture,
                    request,
                    declineWithOption,
                    out string declineReason),
                Is.False);
            Assert.That(declineReason, Is.EqualTo("SelfKanOptionNotAllowed"));

            object staleOption = CreateSelfKanDecisionResponse(
                fixture,
                401,
                "Player1",
                "East",
                12,
                true,
                7);
            Assert.That(
                TryValidateSelfKanResponse(
                    fixture,
                    request,
                    staleOption,
                    out string staleReason),
                Is.False);
            Assert.That(staleReason, Is.EqualTo("SelfKanOptionMissing"));

            object valid = CreateSelfKanDecisionResponse(
                fixture,
                401,
                "Player1",
                "East",
                12,
                true,
                1);
            Assert.That(
                TryValidateSelfKanResponse(fixture, request, valid, out _),
                Is.True);
        }

        [Test]
        public void SelfKanDecisionCoordinator_QueuesOnlyOneValidRequestBoundResponse()
        {
            using (MahjongGameFlowTestSession session = CreateSession())
            {
                Fixture fixture = CreateFixture(session);
                object provider;
                object coordinator = CreateCoordinator(
                    fixture,
                    session.GameFlow,
                    "Player1",
                    "LocalUi",
                    LocalUiDecisionProviderTypeName,
                    out provider);
                object request = CreateSelfKanDecisionRequest(
                    fixture,
                    402,
                    "Player1",
                    "East",
                    13,
                    CreateTypedList(
                        fixture.SelfKanCandidateType,
                        CreateSelfKanCandidate(
                            fixture,
                            "Ankan",
                            "East",
                            "6m",
                            "DrawnTile",
                            13,
                            -1)));
                object invalid = CreateSelfKanDecisionResponse(
                    fixture,
                    402,
                    "Player1",
                    "East",
                    13,
                    true,
                    3);
                object valid = CreateSelfKanDecisionResponse(
                    fixture,
                    402,
                    "Player1",
                    "East",
                    13,
                    true,
                    0);

                AssertAccepted(fixture.Reflection.Invoke(coordinator, "Request", request));
                Assert.That(
                    (bool)fixture.Reflection.Invoke(provider, "TrySubmitResponse", invalid),
                    Is.False);
                Assert.That(
                    (bool)fixture.Reflection.Invoke(provider, "TrySubmitResponse", valid),
                    Is.True);
                Assert.That(
                    (bool)fixture.Reflection.Invoke(provider, "TrySubmitResponse", valid),
                    Is.False);
                Assert.That(
                    (int)fixture.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(1));

                fixture.Reflection.Invoke(coordinator, "Pump");

                Assert.That(
                    (int)fixture.Reflection.GetProperty(coordinator, "PendingCount"),
                    Is.EqualTo(0));
                Assert.That(
                    (int)fixture.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void CpuDecisionProvider_DeclinesSelfKanInsteadOfLeavingItPending()
        {
            Fixture fixture = CreateFixture();
            object provider = fixture.Reflection.CreateInstance(
                fixture.CpuAgentDecisionProviderType);
            object request = CreateSelfKanDecisionRequest(
                fixture,
                403,
                "Player2",
                "South",
                14,
                CreateTypedList(
                    fixture.SelfKanCandidateType,
                    CreateSelfKanCandidate(
                        fixture,
                        "Ankan",
                        "South",
                        "7m",
                        "Hand",
                        14,
                        -1)));
            DecisionResponseRecorder recorder = new DecisionResponseRecorder();
            Delegate callback = CreateAcceptedResponseRecorder(
                provider,
                fixture.DecisionResponseResultType,
                recorder);

            fixture.Reflection.Invoke(provider, "RequestDecision", request, callback);

            Assert.That(recorder.Response, Is.Not.Null);
            Assert.That(
                (long)fixture.Reflection.GetProperty(recorder.Response, "RequestId"),
                Is.EqualTo(403L));
            Assert.That(
                (bool)fixture.Reflection.GetProperty(recorder.Response, "Accepted"),
                Is.False);
        }

        private static Fixture CreateFixture(MahjongGameFlowTestSession session = null)
        {
            ReflectionTestAccess reflection = session != null
                ? session.Reflection
                : new ReflectionTestAccess();
            MahjongTestTypes types = session != null
                ? session.Types
                : new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = session != null
                ? session.DataFactory
                : new MahjongTestDataFactory(reflection, types);
            return new Fixture(
                reflection,
                new CollectionTestAccess(reflection),
                types,
                dataFactory,
                reflection.RequireType(DecisionRequestTypeName),
                reflection.RequireType(DecisionResponseTypeName),
                reflection.RequireType(DecisionResponseResultTypeName),
                reflection.RequireType(DecisionKindTypeName),
                reflection.RequireType(SelfKanCandidateTypeName),
                reflection.RequireType(SelfKanKindTypeName),
                reflection.RequireType(SelfKanTileLocationTypeName),
                reflection.RequireType(SelfKanDecisionRequestTypeName),
                reflection.RequireType(SelfKanDecisionResponseTypeName),
                reflection.RequireType(DecisionCoordinatorTypeName),
                reflection.RequireType(LocalUiDecisionProviderTypeName),
                reflection.RequireType(CpuAgentDecisionProviderTypeName),
                reflection.RequireType(DecisionProviderRegistryTypeName),
                reflection.RequireType(DecisionProviderRegistrationTypeName),
                reflection.RequireType(DecisionProviderRouteTypeName));
        }

        private static MahjongGameFlowTestSession CreateSession()
        {
            return MahjongGameFlowTestSession.Create(new MahjongGameFlowTestOptions
            {
                RootName = "PlayerActionDecisionContractTest",
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
            string playerId,
            string route,
            string providerTypeName,
            out object provider)
        {
            provider = fixture.Reflection.CreateInstance(
                fixture.Reflection.RequireType(providerTypeName));
            IList registrations = CreateTypedList(
                fixture.DecisionProviderRegistrationType,
                fixture.Reflection.CreateInstance(
                    fixture.DecisionProviderRegistrationType,
                    Enum.Parse(fixture.Types.PlayerId, playerId),
                    Enum.Parse(fixture.DecisionProviderRouteType, route),
                    provider));
            object registry = fixture.Reflection.CreateInstance(
                fixture.DecisionProviderRegistryType,
                registrations);
            return fixture.Reflection.CreateInstance(
                fixture.DecisionCoordinatorType,
                registry,
                authority);
        }

        private static object CreateSelfKanDecisionRequest(
            Fixture fixture,
            long requestId,
            string playerId,
            string seat,
            int turnIndex,
            IList candidates)
        {
            object selfKanRequest = fixture.Reflection.CreateInstance(
                fixture.SelfKanDecisionRequestType,
                candidates);
            return fixture.Reflection.CreateInstance(
                fixture.DecisionRequestType,
                requestId,
                Enum.Parse(fixture.DecisionKindType, "SelfKan"),
                Enum.Parse(fixture.Types.PlayerId, playerId),
                Enum.Parse(fixture.Types.SeatId, seat),
                turnIndex,
                selfKanRequest);
        }

        private static object CreateSelfKanCandidate(
            Fixture fixture,
            string kind,
            string seat,
            string tileCode,
            string location,
            int turnIndex,
            int sourcePonMeldIndex)
        {
            return fixture.Reflection.CreateInstance(
                fixture.SelfKanCandidateType,
                Enum.Parse(fixture.SelfKanKindType, kind),
                Enum.Parse(fixture.Types.SeatId, seat),
                fixture.DataFactory.CreateTile(tileCode),
                Enum.Parse(fixture.SelfKanTileLocationType, location),
                turnIndex,
                sourcePonMeldIndex,
                null);
        }

        private static object CreateSelfKanDecisionResponse(
            Fixture fixture,
            long requestId,
            string playerId,
            string seat,
            int turnIndex,
            bool accepted,
            int optionId)
        {
            object selfKanResponse = fixture.Reflection.CreateInstance(
                fixture.SelfKanDecisionResponseType,
                optionId);
            return fixture.Reflection.CreateInstance(
                fixture.DecisionResponseType,
                requestId,
                Enum.Parse(fixture.DecisionKindType, "SelfKan"),
                Enum.Parse(fixture.Types.PlayerId, playerId),
                Enum.Parse(fixture.Types.SeatId, seat),
                turnIndex,
                accepted,
                selfKanResponse);
        }

        private static bool TryValidateSelfKanResponse(
            Fixture fixture,
            object request,
            object response,
            out string reason)
        {
            object[] arguments = { response, null };
            bool valid = (bool)fixture.Reflection.Invoke(
                request,
                "TryValidateResponse",
                arguments);
            reason = arguments[1] as string;
            return valid;
        }

        private static Delegate CreateAcceptedResponseRecorder(
            object provider,
            Type decisionResponseResultType,
            DecisionResponseRecorder recorder)
        {
            MethodInfo requestDecision = provider.GetType().GetMethod(
                "RequestDecision",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(requestDecision, Is.Not.Null);
            Type callbackType = requestDecision.GetParameters()[1].ParameterType;
            MethodInfo callbackInvoke = callbackType.GetMethod("Invoke");
            ParameterInfo[] callbackParameters = callbackInvoke.GetParameters();
            Assert.That(callbackParameters, Has.Length.EqualTo(1));

            ParameterExpression response = Expression.Parameter(
                callbackParameters[0].ParameterType,
                "response");
            MethodInfo record = typeof(DecisionResponseRecorder).GetMethod(
                nameof(DecisionResponseRecorder.Record),
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo succeeded = decisionResponseResultType.GetMethod(
                "Succeeded",
                BindingFlags.Static | BindingFlags.Public);
            Assert.That(record, Is.Not.Null);
            Assert.That(succeeded, Is.Not.Null);

            Expression body = Expression.Block(
                Expression.Call(
                    Expression.Constant(recorder),
                    record,
                    Expression.Convert(response, typeof(object))),
                Expression.Call(succeeded));
            return Expression.Lambda(callbackType, body, response).Compile();
        }

        private static IList CreateTypedList(Type itemType, params object[] values)
        {
            IList list = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(itemType));
            for (int i = 0; i < values.Length; i++)
                list.Add(values[i]);

            return list;
        }

        private static void AssertAccepted(object result)
        {
            Assert.That((bool)result.GetType().GetProperty("Accepted").GetValue(result), Is.True);
        }

        private sealed class DecisionResponseRecorder
        {
            public object Response { get; private set; }

            public void Record(object response)
            {
                Response = response;
            }
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
                Type decisionResponseResultType,
                Type decisionKindType,
                Type selfKanCandidateType,
                Type selfKanKindType,
                Type selfKanTileLocationType,
                Type selfKanDecisionRequestType,
                Type selfKanDecisionResponseType,
                Type decisionCoordinatorType,
                Type localUiDecisionProviderType,
                Type cpuAgentDecisionProviderType,
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
                DecisionResponseResultType = decisionResponseResultType;
                DecisionKindType = decisionKindType;
                SelfKanCandidateType = selfKanCandidateType;
                SelfKanKindType = selfKanKindType;
                SelfKanTileLocationType = selfKanTileLocationType;
                SelfKanDecisionRequestType = selfKanDecisionRequestType;
                SelfKanDecisionResponseType = selfKanDecisionResponseType;
                DecisionCoordinatorType = decisionCoordinatorType;
                LocalUiDecisionProviderType = localUiDecisionProviderType;
                CpuAgentDecisionProviderType = cpuAgentDecisionProviderType;
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
            public Type DecisionResponseResultType { get; }
            public Type DecisionKindType { get; }
            public Type SelfKanCandidateType { get; }
            public Type SelfKanKindType { get; }
            public Type SelfKanTileLocationType { get; }
            public Type SelfKanDecisionRequestType { get; }
            public Type SelfKanDecisionResponseType { get; }
            public Type DecisionCoordinatorType { get; }
            public Type LocalUiDecisionProviderType { get; }
            public Type CpuAgentDecisionProviderType { get; }
            public Type DecisionProviderRegistryType { get; }
            public Type DecisionProviderRegistrationType { get; }
            public Type DecisionProviderRouteType { get; }
        }
    }
}
