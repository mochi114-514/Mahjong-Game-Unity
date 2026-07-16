using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class AuthorityCommandDecisionAndViewContextTests
    {
        private const string AuthorityCommandTypeName =
            "MahjongPrototype.Domain.MahjongAuthorityCommand, Assembly-CSharp";
        private const string AuthorityCommandKindTypeName =
            "MahjongPrototype.Domain.MahjongAuthorityCommandKind, Assembly-CSharp";
        private const string DecisionRequestTypeName =
            "MahjongPrototype.Domain.DecisionRequest, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
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
        private const string MahjongViewContextTypeName =
            "MahjongPrototype.Domain.MahjongViewContext, Assembly-CSharp";
        private const string ViewSlotResolverTypeName =
            "MahjongPrototype.UI.SeatToViewSlotResolver, Assembly-CSharp";

        [Test]
        public void AuthorityCommand_RejectsPlayerSeatAndTurnMismatchesBeforeChangingState()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                object state = session.CurrentState;
                object currentSeat = session.Reflection.GetProperty(state, "CurrentTurn");
                int turnIndex = (int)session.Reflection.GetProperty(state, "TurnIndex");
                int wallCount = (int)session.Reflection.GetProperty(
                    session.Reflection.GetProperty(state, "Wall"),
                    "Count");

                object wrongPlayer = CreateCommand(
                    session.Reflection,
                    session.Types,
                    "Draw",
                    "Player2",
                    currentSeat,
                    turnIndex);
                object wrongSeat = CreateCommand(
                    session.Reflection,
                    session.Types,
                    "Draw",
                    "Player1",
                    Enum.Parse(session.Types.SeatId, "South"),
                    turnIndex);
                object staleTurn = CreateCommand(
                    session.Reflection,
                    session.Types,
                    "Draw",
                    "Player1",
                    currentSeat,
                    turnIndex + 1);

                AssertRejected(session.Reflection.Invoke(
                    session.GameFlow, "TryExecuteCommand", wrongPlayer), "PlayerSeatMismatch");
                AssertRejected(session.Reflection.Invoke(
                    session.GameFlow, "TryExecuteCommand", wrongSeat), "PlayerSeatMismatch");
                AssertRejected(session.Reflection.Invoke(
                    session.GameFlow, "TryExecuteCommand", staleTurn), "StaleTurnIndex");
                Assert.That(
                    (int)session.Reflection.GetProperty(
                        session.Reflection.GetProperty(state, "Wall"),
                        "Count"),
                    Is.EqualTo(wallCount));
            }
        }

        [Test]
        public void DecisionCoordinator_QueuesSingleValidResponseAndRejectsDuplicateOrCancelledResponses()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                object provider;
                object coordinator = CreateCoordinator(session, out provider);
                object request = CreateDecisionRequest(
                    session.Reflection,
                    session.Types,
                    101,
                    "Player1",
                    "East",
                    3);
                object response = CreateDecisionResponse(
                    session.Reflection,
                    session.Types,
                    101,
                    "Player1",
                    "East",
                    3,
                    true);

                AssertAccepted(session.Reflection.Invoke(coordinator, "Request", request));
                Assert.That(
                    (bool)session.Reflection.Invoke(provider, "TrySubmitResponse", response),
                    Is.True);
                Assert.That((int)session.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(0));
                Assert.That(
                    (int)session.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(1),
                    "Synchronous provider input must be queued before authority processing.");
                AssertRejected(
                    session.Reflection.Invoke(coordinator, "ReceiveResponse", response),
                    "DecisionRequestMissingOrCancelled");

                session.Reflection.Invoke(coordinator, "Pump");
                Assert.That(
                    (int)session.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(0));

                object cancelledRequest = CreateDecisionRequest(
                    session.Reflection,
                    session.Types,
                    102,
                    "Player1",
                    "East",
                    3);
                object cancelledResponse = CreateDecisionResponse(
                    session.Reflection,
                    session.Types,
                    102,
                    "Player1",
                    "East",
                    3,
                    false);
                AssertAccepted(session.Reflection.Invoke(coordinator, "Request", cancelledRequest));
                Assert.That((bool)session.Reflection.Invoke(coordinator, "Cancel", 102L), Is.True);
                AssertRejected(
                    session.Reflection.Invoke(coordinator, "ReceiveResponse", cancelledResponse),
                    "DecisionRequestMissingOrCancelled");
            }
        }

        [Test]
        public void DecisionCoordinator_RejectsStaleIdentityWithoutEnqueuingAuthorityWork()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                object ignoredProvider;
                object coordinator = CreateCoordinator(session, out ignoredProvider);
                object request = CreateDecisionRequest(
                    session.Reflection,
                    session.Types,
                    201,
                    "Player1",
                    "East",
                    4);
                object staleResponse = CreateDecisionResponse(
                    session.Reflection,
                    session.Types,
                    201,
                    "Player1",
                    "East",
                    3,
                    true);

                AssertAccepted(session.Reflection.Invoke(coordinator, "Request", request));
                AssertRejected(
                    session.Reflection.Invoke(coordinator, "ReceiveResponse", staleResponse),
                    "DecisionResponseIdentityMismatch");
                Assert.That((int)session.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(1));
                Assert.That(
                    (int)session.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void NewRoundAndRoundEnd_CancelOutstandingCoordinatorDecisions()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                session.Commands.StartNewRound();
                object coordinator = session.Reflection.GetProperty(
                    session.GameFlow,
                    "DecisionCoordinator");
                Assert.That(coordinator, Is.Not.Null);
                AssertAccepted(session.Reflection.Invoke(
                    coordinator,
                    "Request",
                    CreateRequestForCurrentTurn(session, 301)));
                Assert.That((int)session.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(1));

                session.Reflection.Invoke(session.GameFlow, "TryStartNewRound");
                Assert.That((int)session.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(0));

                AssertAccepted(session.Reflection.Invoke(
                    coordinator,
                    "Request",
                    CreateRequestForCurrentTurn(session, 302)));
                Assert.That((int)session.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(1));

                session.Reflection.Invoke(session.GameFlow, "EndRound", "TestRoundEnd");
                Assert.That((int)session.Reflection.GetProperty(coordinator, "PendingCount"), Is.EqualTo(0));
            }
        }

        [Test]
        public void ViewContexts_DeriveDifferentSelfBottomSeatsFromTheSameRoundAssignments()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                object state = session.CurrentState;
                Type viewContextType = session.Reflection.RequireType(MahjongViewContextTypeName);
                object player1Context = session.Reflection.CreateInstance(
                    viewContextType,
                    Enum.Parse(session.Types.PlayerId, "Player1"));
                object player2Context = session.Reflection.CreateInstance(
                    viewContextType,
                    Enum.Parse(session.Types.PlayerId, "Player2"));
                object[] player1Arguments = { state, null };
                object[] player2Arguments = { state, null };

                Assert.That(
                    (bool)session.Reflection.Invoke(player1Context, "TryGetSelfSeat", player1Arguments),
                    Is.True);
                Assert.That(
                    (bool)session.Reflection.Invoke(player2Context, "TryGetSelfSeat", player2Arguments),
                    Is.True);
                Assert.That(player1Arguments[1], Is.Not.EqualTo(player2Arguments[1]));

                Type resolverType = session.Reflection.RequireType(ViewSlotResolverTypeName);
                Assert.That(
                    session.Reflection.InvokeStatic(
                        resolverType,
                        "Resolve",
                        player1Arguments[1],
                        player1Arguments[1]).ToString(),
                    Is.EqualTo("SelfBottom"));
                Assert.That(
                    session.Reflection.InvokeStatic(
                        resolverType,
                        "Resolve",
                        player2Arguments[1],
                        player2Arguments[1]).ToString(),
                    Is.EqualTo("SelfBottom"));
            }
        }

        private static MahjongGameFlowTestSession CreateSession(int participantCount)
        {
            return MahjongGameFlowTestSession.Create(new MahjongGameFlowTestOptions
            {
                RootName = "AuthorityCommandDecisionAndViewContextTest",
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

        private static object CreateCommand(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            string commandKind,
            string playerId,
            object seat,
            int turnIndex)
        {
            return reflection.CreateInstance(
                reflection.RequireType(AuthorityCommandTypeName),
                Enum.Parse(reflection.RequireType(AuthorityCommandKindTypeName), commandKind),
                Enum.Parse(types.PlayerId, playerId),
                seat,
                turnIndex,
                -1);
        }

        private static object CreateCoordinator(
            MahjongGameFlowTestSession session,
            out object provider)
        {
            ReflectionTestAccess reflection = session.Reflection;
            provider = reflection.CreateInstance(
                reflection.RequireType(LocalUiDecisionProviderTypeName));
            Type registrationType = reflection.RequireType(DecisionProviderRegistrationTypeName);
            IList registrations = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(registrationType));
            registrations.Add(reflection.CreateInstance(
                registrationType,
                Enum.Parse(session.Types.PlayerId, "Player1"),
                Enum.Parse(reflection.RequireType(DecisionProviderRouteTypeName), "LocalUi"),
                provider));
            object registry = reflection.CreateInstance(
                reflection.RequireType(DecisionProviderRegistryTypeName),
                registrations);
            return reflection.CreateInstance(
                reflection.RequireType(DecisionCoordinatorTypeName),
                registry,
                session.GameFlow);
        }

        private static object CreateDecisionRequest(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            long requestId,
            string playerId,
            string seat,
            int turnIndex)
        {
            return reflection.CreateInstance(
                reflection.RequireType(DecisionRequestTypeName),
                requestId,
                Enum.Parse(reflection.RequireType(DecisionKindTypeName), "WinDeclaration"),
                Enum.Parse(types.PlayerId, playerId),
                Enum.Parse(types.SeatId, seat),
                turnIndex);
        }

        private static object CreateRequestForCurrentTurn(
            MahjongGameFlowTestSession session,
            long requestId)
        {
            object state = session.CurrentState;
            object seat = session.Reflection.GetProperty(state, "CurrentTurn");
            object seatSlot = session.Reflection.Invoke(state, "GetSeatSlot", seat);
            string playerId = session.Reflection.GetProperty(seatSlot, "PlayerId").ToString();
            return CreateDecisionRequest(
                session.Reflection,
                session.Types,
                requestId,
                playerId,
                seat.ToString(),
                (int)session.Reflection.GetProperty(state, "TurnIndex"));
        }

        private static object CreateDecisionResponse(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            long requestId,
            string playerId,
            string seat,
            int turnIndex,
            bool accepted)
        {
            return reflection.CreateInstance(
                reflection.RequireType(DecisionResponseTypeName),
                requestId,
                Enum.Parse(reflection.RequireType(DecisionKindTypeName), "WinDeclaration"),
                Enum.Parse(types.PlayerId, playerId),
                Enum.Parse(types.SeatId, seat),
                turnIndex,
                accepted);
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
    }
}
