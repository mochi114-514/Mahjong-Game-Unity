using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class ReactionWindowDecisionGameFlowTests
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
        private const string DecisionKindTypeName =
            "MahjongPrototype.Domain.DecisionKind, Assembly-CSharp";
        private const string DecisionResponseTypeName =
            "MahjongPrototype.Domain.DecisionResponse, Assembly-CSharp";
        private const string ReactionDecisionResponseTypeName =
            "MahjongPrototype.Domain.ReactionDecisionResponse, Assembly-CSharp";
        private const string ReactionWindowSeatAnswerKindTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswerKind, Assembly-CSharp";
        private const string ReactionWindowSeatAnswerTypeName =
            "MahjongPrototype.Domain.ReactionWindowSeatAnswer, Assembly-CSharp";
        private const string ReactionWindowCandidateTypeName =
            "MahjongPrototype.Domain.ReactionWindowCandidate, Assembly-CSharp";
        private const string ReactionKindTypeName =
            "MahjongPrototype.Domain.ReactionKind, Assembly-CSharp";
        private const string ChiOptionTypeName =
            "MahjongPrototype.Domain.ChiOption, Assembly-CSharp";
        private const string WinCheckResultTypeName =
            "MahjongPrototype.Domain.WinCheckResult, Assembly-CSharp";
        private const string WinDeclarationEvaluationResultTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationResult, Assembly-CSharp";
        private const string SelfKanCandidateTypeName =
            "MahjongPrototype.Domain.SelfKanCandidate, Assembly-CSharp";
        private const string SelfKanKindTypeName =
            "MahjongPrototype.Domain.SelfKanKind, Assembly-CSharp";
        private const string SelfKanTileLocationTypeName =
            "MahjongPrototype.Domain.SelfKanTileLocation, Assembly-CSharp";
        private const string LocalUiDecisionProviderTypeName =
            "MahjongPrototype.LocalUiDecisionProvider, Assembly-CSharp";
        private const string MahjongUiInputControllerTypeName =
            "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
        private const string MahjongUiCommandRouterTypeName =
            "MahjongPrototype.UI.MahjongUiCommandRouter, Assembly-CSharp";
        private const string MahjongViewContextTypeName =
            "MahjongPrototype.Domain.MahjongViewContext, Assembly-CSharp";

        [Test]
        public void ReactionRequests_IssueOneImmutableRequestPerSeat_AndAggregateCandidates()
        {
            using (Scenario scenario = CreateScenario())
            {
                object ponCandidate = CreatePonCandidate(scenario, scenario.FirstTargetSeat);
                object chiCandidate = CreateChiCandidate(scenario, scenario.FirstTargetSeat, 3);
                object ronCandidate = CreateRonCandidate(scenario, scenario.SecondTargetSeat);
                object window = BeginDiscardWindow(
                    scenario,
                    ponCandidate,
                    chiCandidate,
                    ronCandidate);

                BeginRequests(scenario, window);

                object firstRequest = GetPendingRequest(scenario, scenario.FirstTargetSeat);
                object secondRequest = GetPendingRequest(scenario, scenario.SecondTargetSeat);

                Assert.That(firstRequest, Is.Not.Null);
                Assert.That(secondRequest, Is.Not.Null);
                Assert.That(firstRequest, Is.Not.SameAs(secondRequest));
                Assert.That(
                    scenario.Reflection.GetProperty(firstRequest, "Kind").ToString(),
                    Is.EqualTo("Reaction"));
                Assert.That(
                    scenario.Reflection.GetProperty(secondRequest, "Kind").ToString(),
                    Is.EqualTo("Reaction"));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(2));

                AssertReactionOptionKinds(firstRequest, "Pass", "Pon", "Chi");
                AssertReactionOptionKinds(secondRequest, "Pass", "Ron");
                object chiOption = FindReactionOption(firstRequest, "Chi");
                IList chiOptions = (IList)scenario.Reflection.GetProperty(chiOption, "ChiOptions");
                Assert.That(chiOptions.Count, Is.EqualTo(1));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(chiOptions[0], "OptionId"),
                    Is.EqualTo(3));

                object firstReaction = scenario.Reflection.GetProperty(firstRequest, "Reaction");
                Assert.That(
                    (int)scenario.Reflection.GetProperty(firstReaction, "WindowId"),
                    Is.EqualTo((int)scenario.Reflection.GetProperty(window, "WindowId")));
                Assert.That(
                    scenario.Reflection.GetProperty(firstReaction, "SourceSeat"),
                    Is.EqualTo(scenario.SourceSeat));
                Assert.That(
                    scenario.Reflection.GetProperty(firstReaction, "SourceKind").ToString(),
                    Is.EqualTo("Discard"));
            }
        }

        [Test]
        public void DiscardReactionWindow_StartsSeatRequestsThroughTheCoordinator()
        {
            using (Scenario scenario = CreateScenario())
            {
                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        scenario.FirstTargetSeat.ToString()),
                    "5m",
                    "5m");
                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        scenario.SecondTargetSeat.ToString()),
                    "5m",
                    "5m");
                scenario.Session.DataFactory.SetDrawnTile(
                    scenario.CurrentState,
                    scenario.SourceSeat.ToString(),
                    "5m");

                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryRequestDiscardDrawnTileForSeat",
                        scenario.SourceSeat),
                    Is.True);

                object reactionWindow = scenario.Reflection.GetProperty(
                    scenario.CurrentState,
                    "CurrentReactionWindow");
                Assert.That(reactionWindow, Is.Not.Null);
                AssertReactionOptionKinds(
                    GetPendingRequest(scenario, scenario.FirstTargetSeat),
                    "Pass",
                    "Pon");
                AssertReactionOptionKinds(
                    GetPendingRequest(scenario, scenario.SecondTargetSeat),
                    "Pass",
                    "Pon");
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(2));
            }
        }

        [Test]
        public void ReactionRequests_AggregateEveryChiCandidateOption_InStableOptionIdOrder()
        {
            using (Scenario scenario = CreateScenario())
            {
                object chiOptionFour = CreateChiCandidate(
                    scenario,
                    scenario.FirstTargetSeat,
                    4);
                object chiOptionThree = CreateChiCandidate(
                    scenario,
                    scenario.FirstTargetSeat,
                    3);
                object window = BeginDiscardWindow(
                    scenario,
                    chiOptionFour,
                    chiOptionThree);

                BeginRequests(scenario, window);

                object request = GetPendingRequest(scenario, scenario.FirstTargetSeat);
                object chi = FindReactionOption(request, "Chi");
                Assert.That(GetChiOptionIds(scenario, chi), Is.EqualTo(new[] { 3, 4 }));
            }
        }

        [Test]
        public void ReactionResponses_DoNotCommitUntilEverySeatAnswers_ThenPassResolvesAndMarksRonFuriten()
        {
            using (Scenario scenario = CreateScenario())
            {
                object ponCandidate = CreatePonCandidate(scenario, scenario.FirstTargetSeat);
                object ronCandidate = CreateRonCandidate(scenario, scenario.SecondTargetSeat);
                object window = BeginDiscardWindow(scenario, ponCandidate, ronCandidate);
                BeginRequests(scenario, window);

                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(scenario.SecondTargetSeat.ToString()),
                    Is.False);

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Pass");

                Assert.That(
                    scenario.Reflection.GetProperty(scenario.CurrentState, "CurrentReactionWindow"),
                    Is.SameAs(window));
                Assert.That(
                    scenario.Reflection.GetProperty(window, "State").ToString(),
                    Is.EqualTo("AcceptingAnswers"));
                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Pending"));
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Pending"));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(1));
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(scenario.SecondTargetSeat.ToString()),
                    Is.False);

                SubmitAndPump(scenario, scenario.SecondTargetSeat, "Pass");

                Assert.That(
                    scenario.Reflection.GetProperty(scenario.CurrentState, "CurrentReactionWindow"),
                    Is.Null);
                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Declined"));
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Declined"));
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(scenario.SecondTargetSeat.ToString()),
                    Is.True,
                    "A ron-capable seat that answers Pass is marked only after the final response resolves.");
            }
        }

        [Test]
        public void LegacyReactionDeclaration_UsesSerialCompatibilityPathWhenExplicitlyInvoked()
        {
            using (Scenario scenario = CreateScenario())
            {
                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        scenario.FirstTargetSeat.ToString()),
                    "3m",
                    "4m");
                object ronCandidate = CreateRonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object chiCandidate = CreateChiCandidate(
                    scenario,
                    scenario.FirstTargetSeat,
                    3);
                object window = BeginDiscardWindow(scenario, ronCandidate, chiCandidate);
                BeginRequests(scenario, window);

                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryRequestDeclineRonForSeat",
                        scenario.FirstTargetSeat,
                        scenario.Reflection.GetProperty(window, "WindowId")),
                    Is.True);
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Declined"));
                Assert.That(CandidateState(scenario, chiCandidate), Is.EqualTo("Pending"));
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.CurrentState,
                        "CurrentReactionWindow"),
                    Is.SameAs(window));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(0));

                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryRequestDeclareChiForSeat",
                        scenario.FirstTargetSeat,
                        scenario.Reflection.GetProperty(window, "WindowId"),
                        3),
                    Is.True);

                Assert.That(CandidateState(scenario, chiCandidate), Is.EqualTo("Declared"));
                Assert.That(
                    scenario.Session.Query.MeldCount(scenario.FirstTargetSeat.ToString()),
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void LegacyReactionDeclaration_CancelsAQueuedSeatAnswerRetryBeforeUsingSerialPath()
        {
            using (Scenario scenario = CreateScenario())
            {
                object ronCandidate = CreateRonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object ponCandidate = CreatePonCandidate(
                    scenario,
                    scenario.SecondTargetSeat);
                object window = BeginDiscardWindow(
                    scenario,
                    ronCandidate,
                    ponCandidate);
                BeginRequests(scenario, window);

                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "ScheduleReactionSeatAnswerRequestRetry",
                    window);
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(
                            scenario.GameFlow,
                            "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(0));

                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryRequestDeclineRonForSeat",
                        scenario.FirstTargetSeat,
                        scenario.Reflection.GetProperty(window, "WindowId")),
                    Is.True);
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Declined"));
                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Pending"));

                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryResumeReactionSeatAnswerRequests");
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(
                            scenario.GameFlow,
                            "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(0),
                    "A serial fallback must clear the queued multi-seat retry.");
            }
        }

        [Test]
        public void QueuedSeatAnswer_BlocksLegacyFallback_ThenPumpsThroughTheNewPath()
        {
            using (Scenario scenario = CreateScenario())
            {
                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        scenario.FirstTargetSeat.ToString()),
                    "5m",
                    "5m");
                object ponCandidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object window = BeginDiscardWindow(scenario, ponCandidate);
                BeginRequests(scenario, window);

                QueueResponse(
                    scenario,
                    scenario.FirstTargetSeat,
                    "Pon");
                object coordinator = scenario.Reflection.GetProperty(
                    scenario.GameFlow,
                    "DecisionCoordinator");
                int windowId = (int)scenario.Reflection.GetProperty(window, "WindowId");

                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryRequestDeclarePonForSeat",
                        scenario.FirstTargetSeat,
                        windowId),
                    Is.False,
                    "A queued seat answer must prevent switching the same window to the serial path.");
                Assert.That(
                    (int)scenario.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(1));
                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Pending"));
                Assert.That(scenario.Session.Query.MeldCount(
                    scenario.FirstTargetSeat.ToString()), Is.EqualTo(0));

                scenario.Reflection.Invoke(coordinator, "Pump");

                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Declared"));
                Assert.That(scenario.Session.Query.MeldCount(
                    scenario.FirstTargetSeat.ToString()), Is.EqualTo(1));
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.CurrentState,
                        "CurrentReactionWindow"),
                    Is.Null);
            }
        }

        [Test]
        public void CoordinatorCancel_RemovesAQueuedReactionResponseBeforePump()
        {
            using (Scenario scenario = CreateScenario())
            {
                object candidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object window = BeginDiscardWindow(scenario, candidate);
                BeginRequests(scenario, window);
                object request = QueueResponse(
                    scenario,
                    scenario.FirstTargetSeat,
                    "Pass");
                object coordinator = scenario.Reflection.GetProperty(
                    scenario.GameFlow,
                    "DecisionCoordinator");

                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        coordinator,
                        "Cancel",
                        scenario.Reflection.GetProperty(request, "RequestId")),
                    Is.True);
                Assert.That(
                    (int)scenario.Reflection.GetProperty(coordinator, "QueuedResponseCount"),
                    Is.EqualTo(0));

                scenario.Reflection.Invoke(coordinator, "Pump");

                Assert.That(CandidateState(scenario, candidate), Is.EqualTo("Pending"));
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.CurrentState,
                        "CurrentReactionWindow"),
                    Is.SameAs(window));
            }
        }

        [Test]
        public void LocalUiReactionInput_UsesProviderAndCoordinator_AndRejectsCapturedIdsFromClosedWindow()
        {
            using (Scenario scenario = CreateScenario())
            {
                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "ConfigureViewContext",
                    scenario.Reflection.CreateInstance(
                        scenario.Reflection.RequireType(MahjongViewContextTypeName),
                        PlayerIdForSeat(scenario, scenario.FirstTargetSeat)));

                object firstCandidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object firstWindow = BeginDiscardWindow(scenario, firstCandidate);
                BeginRequests(scenario, firstWindow);
                object firstRequest = GetPendingRequest(
                    scenario,
                    scenario.FirstTargetSeat);
                object coordinator = scenario.Reflection.GetProperty(
                    scenario.GameFlow,
                    "DecisionCoordinator");

                GameObject uiRoot = new GameObject("ReactionRouterIntegrationUi");
                uiRoot.SetActive(false);
                try
                {
                    Component input = uiRoot.AddComponent(
                        scenario.Reflection.RequireType(
                            MahjongUiInputControllerTypeName));
                    Component router = uiRoot.AddComponent(
                        scenario.Reflection.RequireType(
                            MahjongUiCommandRouterTypeName));
                    scenario.Reflection.SetPrivateField(
                        router,
                        "gameFlow",
                        scenario.GameFlow);
                    scenario.Reflection.SetPrivateField(
                        router,
                        "inputController",
                        input);
                    scenario.Reflection.Invoke(router, "OnEnable");

                    scenario.Reflection.Invoke(
                        input,
                        "RequestReactionResponse",
                        scenario.Reflection.GetProperty(firstRequest, "RequestId"),
                        scenario.Reflection.GetProperty(
                            scenario.Reflection.GetProperty(firstRequest, "Reaction"),
                            "WindowId"),
                        Enum.Parse(
                            scenario.Reflection.RequireType(
                                ReactionWindowSeatAnswerKindTypeName),
                            "Pass"),
                        null);

                    Assert.That(
                        (int)scenario.Reflection.GetProperty(coordinator, "PendingCount"),
                        Is.EqualTo(0));
                    Assert.That(
                        (int)scenario.Reflection.GetProperty(
                            coordinator,
                            "QueuedResponseCount"),
                        Is.EqualTo(1));
                    Assert.That(CandidateState(scenario, firstCandidate), Is.EqualTo("Pending"));

                    scenario.Reflection.Invoke(coordinator, "Pump");
                    Assert.That(
                        scenario.Reflection.GetProperty(
                            scenario.CurrentState,
                            "CurrentReactionWindow"),
                        Is.Null);

                    object nextCandidate = CreatePonCandidate(
                        scenario,
                        scenario.FirstTargetSeat);
                    object nextWindow = BeginDiscardWindow(scenario, nextCandidate);
                    BeginRequests(scenario, nextWindow);
                    object nextRequest = GetPendingRequest(
                        scenario,
                        scenario.FirstTargetSeat);

                    // The old UI callback still has the first immutable ids.
                    // Router must not rewrite it into the new request.
                    scenario.Reflection.Invoke(
                        input,
                        "RequestReactionResponse",
                        scenario.Reflection.GetProperty(firstRequest, "RequestId"),
                        scenario.Reflection.GetProperty(
                            scenario.Reflection.GetProperty(firstRequest, "Reaction"),
                            "WindowId"),
                        Enum.Parse(
                            scenario.Reflection.RequireType(
                                ReactionWindowSeatAnswerKindTypeName),
                            "Pass"),
                        null);

                    Assert.That(
                        (int)scenario.Reflection.GetProperty(
                            coordinator,
                            "QueuedResponseCount"),
                        Is.EqualTo(0));
                    Assert.That(CandidateState(scenario, nextCandidate), Is.EqualTo("Pending"));
                    Assert.That(
                        scenario.Reflection.GetProperty(nextRequest, "RequestId"),
                        Is.Not.EqualTo(
                            scenario.Reflection.GetProperty(firstRequest, "RequestId")));

                    scenario.Reflection.Invoke(router, "OnDisable");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(uiRoot);
                }
            }
        }

        [Test]
        public void ReactionResponses_UseRonPriorityOverPon_EvenWhenPonArrivesFirst()
        {
            using (Scenario scenario = CreateScenario())
            {
                object ponCandidate = CreatePonCandidate(scenario, scenario.FirstTargetSeat);
                object ronCandidate = CreateRonCandidate(scenario, scenario.SecondTargetSeat);
                object window = BeginDiscardWindow(scenario, ponCandidate, ronCandidate);
                BeginRequests(scenario, window);

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Pon");

                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Pending"));
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Pending"));
                Assert.That(scenario.Session.Query.IsRoundEnded, Is.False);

                SubmitAndPump(scenario, scenario.SecondTargetSeat, "Ron");

                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Declined"));
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Declared"));
                Assert.That(scenario.Session.Query.IsRoundEnded, Is.True);
            }
        }

        [Test]
        public void MultipleRonResponses_SelectNearestSeat_WhenFartherResponseArrivesFirst()
        {
            using (Scenario scenario = CreateScenario())
            {
                Assert.That(scenario.SourceSeat.ToString(), Is.EqualTo("East"));
                object nearestSeat = scenario.Session.DataFactory.ParseSeat("South");
                object fartherSeat = scenario.Session.DataFactory.ParseSeat("West");
                object nearestRon = CreateRonCandidate(scenario, nearestSeat);
                object fartherRon = CreateRonCandidate(scenario, fartherSeat);
                object window = BeginDiscardWindow(scenario, fartherRon, nearestRon);
                BeginRequests(scenario, window);

                SubmitAndPump(scenario, fartherSeat, "Ron");
                Assert.That(CandidateState(scenario, nearestRon), Is.EqualTo("Pending"));
                Assert.That(CandidateState(scenario, fartherRon), Is.EqualTo("Pending"));

                SubmitAndPump(scenario, nearestSeat, "Ron");

                Assert.That(CandidateState(scenario, nearestRon), Is.EqualTo("Declared"));
                Assert.That(CandidateState(scenario, fartherRon), Is.EqualTo("Declined"));
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(nearestSeat.ToString()),
                    Is.False);
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(fartherSeat.ToString()),
                    Is.False,
                    "A Ron answer must not become pass furiten when another Ron seat is selected.");
            }
        }

        [Test]
        public void ReactionResponses_CommitSelectedChiThroughTheExistingMeldFollowUp()
        {
            using (Scenario scenario = CreateScenario())
            {
                Assert.That(scenario.SourceSeat.ToString(), Is.EqualTo("East"));
                object chiSeat = scenario.Session.DataFactory.ParseSeat("South");
                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        chiSeat.ToString()),
                    "3m",
                    "4m");
                object chiCandidate = CreateChiCandidate(scenario, chiSeat, 3);
                object window = BeginDiscardWindow(scenario, chiCandidate);
                int sourceDiscardId = (int)scenario.Reflection.GetProperty(
                    scenario.Reflection.GetProperty(window, "SourceDiscard"),
                    "Id");

                BeginRequests(scenario, window);
                SubmitAndPump(scenario, chiSeat, "Chi", chiOptionId: 3);

                Assert.That(CandidateState(scenario, chiCandidate), Is.EqualTo("Declared"));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Session.DataFactory.GetPlayerSeat(
                            scenario.CurrentState,
                            chiSeat.ToString()),
                        "Hand").GetType().GetProperty("Count").GetValue(
                            scenario.Reflection.GetProperty(
                                scenario.Session.DataFactory.GetPlayerSeat(
                                    scenario.CurrentState,
                                    chiSeat.ToString()),
                                "Hand")),
                    Is.EqualTo(0));
                Assert.That(
                    scenario.Session.Query.MeldCount(chiSeat.ToString()),
                    Is.EqualTo(1));
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.Session.Query.MeldAt(chiSeat.ToString(), 0),
                        "Type").ToString(),
                    Is.EqualTo("Chi"));
                AssertDiscardClaimed(scenario, sourceDiscardId);
                Assert.That(
                    (bool)scenario.Reflection.GetProperty(scenario.CurrentState, "HasCallOccurred"),
                    Is.True);
                Assert.That(
                    scenario.Reflection.GetProperty(scenario.CurrentState, "CurrentTurn"),
                    Is.EqualTo(chiSeat));
            }
        }

        [Test]
        public void ReactionResponses_CommitSelectedDaiminkanOnce_AndEnterRinshanFollowUp()
        {
            using (Scenario scenario = CreateScenario())
            {
                object daiminkanSeat = scenario.FirstTargetSeat;
                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        daiminkanSeat.ToString()),
                    "5m",
                    "5m",
                    "5m");
                object daiminkanCandidate = CreateDaiminkanCandidate(
                    scenario,
                    daiminkanSeat);
                object window = BeginDiscardWindow(scenario, daiminkanCandidate);
                int sourceDiscardId = (int)scenario.Reflection.GetProperty(
                    scenario.Reflection.GetProperty(window, "SourceDiscard"),
                    "Id");

                BeginRequests(scenario, window);
                SubmitAndPump(scenario, daiminkanSeat, "Daiminkan");

                Assert.That(CandidateState(scenario, daiminkanCandidate), Is.EqualTo("Declared"));
                Assert.That(
                    scenario.Session.Query.MeldCount(daiminkanSeat.ToString()),
                    Is.EqualTo(1));
                object meld = scenario.Session.Query.MeldAt(daiminkanSeat.ToString(), 0);
                Assert.That(
                    scenario.Reflection.GetProperty(meld, "Type").ToString(),
                    Is.EqualTo("Daiminkan"));
                Assert.That(
                    scenario.Collections.Count(
                        scenario.Reflection.GetProperty(meld, "PhysicalTiles")),
                    Is.EqualTo(4));
                AssertDiscardClaimed(scenario, sourceDiscardId);
                Assert.That(
                    (bool)scenario.Reflection.GetProperty(scenario.CurrentState, "HasCallOccurred"),
                    Is.True);
                Assert.That(
                    scenario.Reflection.GetProperty(scenario.CurrentState, "CurrentTurn"),
                    Is.EqualTo(daiminkanSeat));
                Assert.That(
                    scenario.Session.Query.HasDrawnTile(daiminkanSeat.ToString()),
                    Is.True,
                    "The existing Daiminkan flow must continue into its automatic rinshan draw.");

                Assert.That(
                    scenario.Session.Query.MeldCount(daiminkanSeat.ToString()),
                    Is.EqualTo(1),
                    "The submitted declaration must not be committed twice.");
            }
        }

        [Test]
        public void ReactionResponses_RejectInvalidDuplicateAndDelayedResponses_WithoutChangingTheWindow()
        {
            using (Scenario scenario = CreateScenario())
            {
                object candidate = CreatePonCandidate(scenario, scenario.FirstTargetSeat);
                object window = BeginDiscardWindow(scenario, candidate);
                BeginRequests(scenario, window);

                object request = GetPendingRequest(scenario, scenario.FirstTargetSeat);
                object provider = GetProvider(scenario, scenario.FirstTargetSeat);
                object wrongWindowResponse = CreateResponse(
                    scenario,
                    request,
                    "Pass",
                    windowIdOverride: (int)scenario.Reflection.GetProperty(window, "WindowId") + 1);
                object wrongPlayerResponse = CreateResponse(
                    scenario,
                    request,
                    "Pass",
                    playerIdOverride: PlayerIdForSeat(scenario, scenario.SourceSeat));

                Assert.That(
                    (bool)scenario.Reflection.Invoke(provider, "TrySubmitResponse", wrongWindowResponse),
                    Is.False);
                Assert.That(
                    (bool)scenario.Reflection.Invoke(provider, "TrySubmitResponse", wrongPlayerResponse),
                    Is.False);
                Assert.That(CandidateState(scenario, candidate), Is.EqualTo("Pending"));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(1));

                object validResponse = CreateResponse(scenario, request, "Pass");
                Assert.That(
                    (bool)scenario.Reflection.Invoke(provider, "TrySubmitResponse", validResponse),
                    Is.True);
                AssertRejected(
                    scenario.Reflection.Invoke(
                        scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                        "ReceiveResponse",
                        validResponse),
                    "DecisionRequestMissingOrCancelled");
                Assert.That(
                    (bool)scenario.Reflection.Invoke(provider, "TrySubmitResponse", validResponse),
                    Is.False);

                scenario.Reflection.Invoke(
                    scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                    "Pump");

                AssertRejected(
                    scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryExecuteDecisionResponse",
                        validResponse),
                    "ReactionDecisionRequestMissing");
            }
        }

        [Test]
        public void AuthorityRejectedQueuedResponse_ReissuesAUsableReactionRequest()
        {
            using (Scenario scenario = CreateScenario())
            {
                object candidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object window = BeginDiscardWindow(scenario, candidate);
                BeginRequests(scenario, window);
                object originalRequest = GetPendingRequest(
                    scenario,
                    scenario.FirstTargetSeat);
                object answers = scenario.Reflection.GetPrivateField(
                    scenario.GameFlow,
                    "reactionWindowSeatAnswers");
                object preRegisteredAnswer = scenario.Reflection.CreateInstance(
                    scenario.Reflection.RequireType(ReactionWindowSeatAnswerTypeName),
                    scenario.Reflection.GetProperty(window, "WindowId"),
                    scenario.FirstTargetSeat,
                    Enum.Parse(
                        scenario.Reflection.RequireType(
                            ReactionWindowSeatAnswerKindTypeName),
                        "Pass"),
                    null);
                object registration = scenario.Reflection.Invoke(
                    answers,
                    "TryRegister",
                    preRegisteredAnswer);
                Assert.That(
                    (bool)scenario.Reflection.GetProperty(registration, "Accepted"),
                    Is.True);

                QueueResponse(scenario, scenario.FirstTargetSeat, "Pass");
                object coordinator = scenario.Reflection.GetProperty(
                    scenario.GameFlow,
                    "DecisionCoordinator");
                scenario.Reflection.Invoke(coordinator, "Pump");

                Assert.That(
                    scenario.Reflection.GetPrivateField(
                        scenario.GameFlow,
                        "reactionWindowAwaitingRequestRetry"),
                    Is.SameAs(window));
                Assert.That(
                    (int)scenario.Reflection.GetProperty(coordinator, "PendingCount"),
                    Is.EqualTo(0));

                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryResumeReactionSeatAnswerRequests");
                object retriedRequest = GetPendingRequest(
                    scenario,
                    scenario.FirstTargetSeat);
                Assert.That(
                    scenario.Reflection.GetProperty(retriedRequest, "RequestId"),
                    Is.Not.EqualTo(
                        scenario.Reflection.GetProperty(originalRequest, "RequestId")));

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Pass");

                Assert.That(CandidateState(scenario, candidate), Is.EqualTo("Declined"));
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.CurrentState,
                        "CurrentReactionWindow"),
                    Is.Null);
            }
        }

        [Test]
        public void NonPendingCandidate_DoesNotLeaveAnEndlessReactionRequestRetry()
        {
            using (Scenario scenario = CreateScenario())
            using (EventSequenceRecorder events = new EventSequenceRecorder(
                scenario.Session.EventNotifier,
                "ReactionWindowAnswered"))
            {
                object candidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object window = BeginDiscardWindow(scenario, candidate);
                BeginRequests(scenario, window);
                scenario.Reflection.Invoke(candidate, "Decline");

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Pass");

                Assert.That(
                    scenario.Reflection.GetPrivateField(
                        scenario.GameFlow,
                        "reactionWindowAwaitingRequestRetry"),
                    Is.Null);
                Assert.That(
                    (int)scenario.Reflection.GetProperty(
                        scenario.Reflection.GetProperty(
                            scenario.GameFlow,
                            "DecisionCoordinator"),
                        "PendingCount"),
                    Is.EqualTo(0));
                Assert.That(events.Count("ReactionWindowAnswered"), Is.EqualTo(0));

                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryResumeReactionSeatAnswerRequests");
                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryResumeReactionSeatAnswerRequests");

                Assert.That(
                    scenario.Reflection.GetPrivateField(
                        scenario.GameFlow,
                        "reactionWindowAwaitingRequestRetry"),
                    Is.Null);
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.CurrentState,
                        "CurrentReactionWindow"),
                    Is.SameAs(window));
                Assert.That(CandidateState(scenario, candidate), Is.EqualTo("Declined"));
            }
        }

        [Test]
        public void PrepareFailure_RetriesWithoutDuplicateAnswerNotifications_AndAppliesRonPassFuritenOnce()
        {
            using (Scenario scenario = CreateScenario())
            using (EventSequenceRecorder events = new EventSequenceRecorder(
                scenario.Session.EventNotifier,
                "ReactionWindowAnswered",
                "ReactionWindowResolved",
                "ReactionWindowClosed"))
            {
                object ronCandidate = CreateRonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object ponCandidate = CreatePonCandidate(
                    scenario,
                    scenario.SecondTargetSeat);
                object window = BeginDiscardWindow(
                    scenario,
                    ronCandidate,
                    ponCandidate);
                BeginRequests(scenario, window);

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Pass");
                SubmitAndPump(scenario, scenario.SecondTargetSeat, "Pon");

                Assert.That(events.Count("ReactionWindowAnswered"), Is.EqualTo(1));
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Pending"));
                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Pending"));
                Assert.That(
                    scenario.Reflection.GetPrivateField(
                        scenario.GameFlow,
                        "reactionWindowAwaitingRequestRetry"),
                    Is.SameAs(window));
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(
                        scenario.FirstTargetSeat.ToString()),
                    Is.False);

                scenario.Session.DataFactory.AddHandTiles(
                    scenario.Session.DataFactory.GetPlayerSeat(
                        scenario.CurrentState,
                        scenario.SecondTargetSeat.ToString()),
                    "5m",
                    "5m");
                scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryResumeReactionSeatAnswerRequests");

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Pass");
                Assert.That(
                    events.Count("ReactionWindowAnswered"),
                    Is.EqualTo(1),
                    "The previously notified intermediate seat must not be notified again.");
                SubmitAndPump(scenario, scenario.SecondTargetSeat, "Pon");

                Assert.That(events.Count("ReactionWindowAnswered"), Is.EqualTo(2));
                Assert.That(events.Count("ReactionWindowResolved"), Is.EqualTo(1));
                Assert.That(events.Count("ReactionWindowClosed"), Is.EqualTo(1));
                Assert.That(
                    events.LastIndexOf("ReactionWindowAnswered"),
                    Is.LessThan(events.IndexOf("ReactionWindowResolved")),
                    events.Describe());
                Assert.That(
                    events.IndexOf("ReactionWindowResolved"),
                    Is.LessThan(events.IndexOf("ReactionWindowClosed")),
                    events.Describe());
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(
                        scenario.FirstTargetSeat.ToString()),
                    Is.True);
                Assert.That(
                    scenario.Session.Query.IsTemporaryFuriten(
                        scenario.SecondTargetSeat.ToString()),
                    Is.False);
                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Declined"));
                Assert.That(CandidateState(scenario, ponCandidate), Is.EqualTo("Declared"));
                Assert.That(
                    scenario.Session.Query.MeldCount(
                        scenario.SecondTargetSeat.ToString()),
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void DelayedResponseFromClosedWindow_CannotAnswerTheNextWindow()
        {
            using (Scenario scenario = CreateScenario())
            {
                object firstCandidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object firstWindow = BeginDiscardWindow(scenario, firstCandidate);
                BeginRequests(scenario, firstWindow);
                object firstRequest = GetPendingRequest(
                    scenario,
                    scenario.FirstTargetSeat);
                object delayedResponse = CreateResponse(scenario, firstRequest, "Pass");
                Assert.That(
                    (bool)scenario.Reflection.Invoke(
                        GetProvider(scenario, scenario.FirstTargetSeat),
                        "TrySubmitResponse",
                        delayedResponse),
                    Is.True);
                scenario.Reflection.Invoke(
                    scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                    "Pump");

                object nextCandidate = CreatePonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object nextWindow = BeginDiscardWindow(scenario, nextCandidate);
                BeginRequests(scenario, nextWindow);

                AssertRejected(
                    scenario.Reflection.Invoke(
                        scenario.GameFlow,
                        "TryExecuteDecisionResponse",
                        delayedResponse),
                    "ReactionDecisionRequestMissing");
                Assert.That(CandidateState(scenario, nextCandidate), Is.EqualTo("Pending"));
                Assert.That(
                    GetPendingRequest(scenario, scenario.FirstTargetSeat),
                    Is.Not.SameAs(firstRequest));
            }
        }

        [Test]
        public void KakanReactionRequest_UsesCopiedKakanSourceInformation()
        {
            using (Scenario scenario = CreateScenario())
            {
                object ronCandidate = CreateRonCandidate(scenario, scenario.FirstTargetSeat);
                object window = BeginKakanWindow(scenario, ronCandidate);

                BeginRequests(scenario, window);

                object request = GetPendingRequest(scenario, scenario.FirstTargetSeat);
                object reaction = scenario.Reflection.GetProperty(request, "Reaction");
                Assert.That(
                    scenario.Reflection.GetProperty(reaction, "SourceKind").ToString(),
                    Is.EqualTo("Kakan"));
                Assert.That(
                    scenario.Reflection.GetProperty(reaction, "SourceSeat"),
                    Is.EqualTo(scenario.SourceSeat));
                Assert.That(
                    scenario.Reflection.GetProperty(reaction, "SourceTile").ToString(),
                    Is.EqualTo("5m"));
                AssertReactionOptionKinds(request, "Pass", "Ron");
            }
        }

        [Test]
        public void KakanReactionResponse_UsesTheExistingRonResolutionPath()
        {
            using (Scenario scenario = CreateScenario())
            {
                object ronCandidate = CreateRonCandidate(
                    scenario,
                    scenario.FirstTargetSeat);
                object window = BeginKakanWindow(scenario, ronCandidate);
                BeginRequests(scenario, window);

                SubmitAndPump(scenario, scenario.FirstTargetSeat, "Ron");

                Assert.That(CandidateState(scenario, ronCandidate), Is.EqualTo("Declared"));
                Assert.That(
                    scenario.Reflection.GetProperty(
                        scenario.CurrentState,
                        "CurrentReactionWindow"),
                    Is.Null);
                Assert.That(scenario.Session.Query.IsRoundEnded, Is.True);
            }
        }

        private static Scenario CreateScenario()
        {
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "ReactionWindowDecisionGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = 3,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    RandomizeSelfSeat = false,
                    FixedSelfSeatName = "East"
                });

            try
            {
                ReflectionTestAccess reflection = session.Reflection;
                Dictionary<string, object> providers = ConfigureAllLocalHumanPlayers(session);
                object result = reflection.Invoke(session.GameFlow, "TryStartNewRound");
                Assert.That((bool)reflection.GetProperty(result, "IsValid"), Is.True);

                object state = session.CurrentState;
                object sourceSeat = reflection.GetProperty(state, "CurrentTurn");
                List<object> targetSeats = new List<object>();
                foreach (object seat in (IEnumerable)reflection.GetProperty(state, "ActiveTurnSeats"))
                {
                    if (!seat.Equals(sourceSeat))
                        targetSeats.Add(seat);
                }

                Assert.That(targetSeats.Count, Is.EqualTo(2));
                return new Scenario(
                    session,
                    providers,
                    sourceSeat,
                    targetSeats[0],
                    targetSeats[1]);
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private static Dictionary<string, object> ConfigureAllLocalHumanPlayers(
            MahjongGameFlowTestSession session)
        {
            ReflectionTestAccess reflection = session.Reflection;
            Type participantType = reflection.RequireType(MatchParticipantTypeName);
            Type participantKindType = reflection.RequireType(ParticipantKindTypeName);
            Type registrationType = reflection.RequireType(DecisionProviderRegistrationTypeName);
            Type routeType = reflection.RequireType(DecisionProviderRouteTypeName);
            Type providerType = reflection.RequireType(LocalUiDecisionProviderTypeName);
            IList participants = CreateTypedList(participantType);
            IList registrations = CreateTypedList(registrationType);
            Dictionary<string, object> providers = new Dictionary<string, object>();

            for (int i = 1; i <= 3; i++)
            {
                string playerName = $"Player{i}";
                object playerId = session.DataFactory.ParsePlayerId(playerName);
                participants.Add(reflection.CreateInstance(
                    participantType,
                    playerId,
                    Enum.Parse(participantKindType, "Human")));

                object provider = reflection.CreateInstance(providerType);
                providers.Add(playerName, provider);
                registrations.Add(reflection.CreateInstance(
                    registrationType,
                    playerId,
                    Enum.Parse(routeType, "LocalUi"),
                    provider));
            }

            object roster = reflection.CreateInstance(
                reflection.RequireType(MatchRosterTypeName),
                participants);
            object registry = reflection.CreateInstance(
                reflection.RequireType(DecisionProviderRegistryTypeName),
                registrations);
            reflection.Invoke(session.GameFlow, "ConfigureMatch", roster, registry);
            return providers;
        }

        private static object BeginDiscardWindow(Scenario scenario, params object[] candidates)
        {
            int turnIndex = (int)scenario.Reflection.GetProperty(
                scenario.CurrentState,
                "TurnIndex");
            object sourceDiscard = scenario.Reflection.Invoke(
                scenario.CurrentState,
                "AddDiscard",
                scenario.Session.DataFactory.CreateDiscardRecord(
                    scenario.SourceSeat.ToString(),
                    "5m",
                    turnIndex));
            return scenario.Reflection.Invoke(
                scenario.CurrentState,
                "BeginReactionWindow",
                sourceDiscard,
                CreateCandidateList(scenario, candidates));
        }

        private static object BeginKakanWindow(Scenario scenario, object ronCandidate)
        {
            scenario.Session.DataFactory.SetDrawnTile(
                scenario.CurrentState,
                scenario.SourceSeat.ToString(),
                "5m");
            int turnIndex = (int)scenario.Reflection.GetProperty(
                scenario.CurrentState,
                "TurnIndex");
            object pendingKakan = scenario.Reflection.CreateInstance(
                scenario.Reflection.RequireType(SelfKanCandidateTypeName),
                Enum.Parse(scenario.Reflection.RequireType(SelfKanKindTypeName), "Kakan"),
                scenario.SourceSeat,
                scenario.Session.DataFactory.CreateTile("5m"),
                Enum.Parse(
                    scenario.Reflection.RequireType(SelfKanTileLocationTypeName),
                    "DrawnTile"),
                turnIndex,
                0,
                null);
            return scenario.Reflection.Invoke(
                scenario.CurrentState,
                "BeginKakanReactionWindow",
                pendingKakan,
                CreateCandidateList(scenario, ronCandidate));
        }

        private static void BeginRequests(Scenario scenario, object window)
        {
            object[] arguments = { window, null };
            Assert.That(
                (bool)scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryBeginReactionSeatAnswerRequests",
                    arguments),
                Is.True,
                arguments[1] as string);
            Assert.That(arguments[1], Is.EqualTo(string.Empty));
        }

        private static object GetPendingRequest(Scenario scenario, object seat)
        {
            object[] arguments = { PlayerIdForSeat(scenario, seat), null };
            Assert.That(
                (bool)scenario.Reflection.Invoke(
                    scenario.GameFlow,
                    "TryGetPendingReactionDecisionRequest",
                    arguments),
                Is.True);
            return arguments[1];
        }

        private static object GetProvider(Scenario scenario, object seat)
        {
            return scenario.Providers[PlayerIdForSeat(scenario, seat).ToString()];
        }

        private static object PlayerIdForSeat(Scenario scenario, object seat)
        {
            return scenario.Reflection.GetProperty(
                scenario.Reflection.Invoke(scenario.CurrentState, "GetSeatSlot", seat),
                "PlayerId");
        }

        private static void SubmitAndPump(
            Scenario scenario,
            object seat,
            string answerKind,
            int? chiOptionId = null)
        {
            QueueResponse(scenario, seat, answerKind, chiOptionId);
            scenario.Reflection.Invoke(
                scenario.Reflection.GetProperty(scenario.GameFlow, "DecisionCoordinator"),
                "Pump");
        }

        private static object QueueResponse(
            Scenario scenario,
            object seat,
            string answerKind,
            int? chiOptionId = null)
        {
            object request = GetPendingRequest(scenario, seat);
            object response = CreateResponse(
                scenario,
                request,
                answerKind,
                chiOptionId: chiOptionId);
            Assert.That(
                (bool)scenario.Reflection.Invoke(
                    GetProvider(scenario, seat),
                    "TrySubmitResponse",
                    response),
                Is.True);
            return request;
        }

        private static object CreateResponse(
            Scenario scenario,
            object request,
            string answerKind,
            int? windowIdOverride = null,
            object playerIdOverride = null,
            int? chiOptionId = null)
        {
            object reactionRequest = scenario.Reflection.GetProperty(request, "Reaction");
            int windowId = windowIdOverride ??
                (int)scenario.Reflection.GetProperty(reactionRequest, "WindowId");
            object reactionResponse = scenario.Reflection.CreateInstance(
                scenario.Reflection.RequireType(ReactionDecisionResponseTypeName),
                windowId,
                Enum.Parse(
                    scenario.Reflection.RequireType(ReactionWindowSeatAnswerKindTypeName),
                    answerKind),
                chiOptionId);
            return scenario.Reflection.CreateInstance(
                scenario.Reflection.RequireType(DecisionResponseTypeName),
                scenario.Reflection.GetProperty(request, "RequestId"),
                Enum.Parse(scenario.Reflection.RequireType(DecisionKindTypeName), "Reaction"),
                playerIdOverride ?? scenario.Reflection.GetProperty(request, "PlayerId"),
                scenario.Reflection.GetProperty(request, "ActorSeat"),
                scenario.Reflection.GetProperty(request, "TurnIndex"),
                true,
                reactionResponse);
        }

        private static object CreatePonCandidate(Scenario scenario, object seat)
        {
            return scenario.Reflection.InvokeStatic(
                scenario.Reflection.RequireType(ReactionWindowCandidateTypeName),
                "CreatePon",
                seat,
                scenario.Session.DataFactory.CreateTile("5m"));
        }

        private static object CreateRonCandidate(Scenario scenario, object seat)
        {
            object evaluation = scenario.Reflection.InvokeStatic(
                scenario.Reflection.RequireType(WinDeclarationEvaluationResultTypeName),
                "NotWinningShape",
                scenario.Reflection.GetStaticProperty(
                    scenario.Reflection.RequireType(WinCheckResultTypeName),
                    "NotWin"));
            return scenario.Reflection.CreateInstance(
                scenario.Reflection.RequireType(ReactionWindowCandidateTypeName),
                seat,
                Enum.Parse(scenario.Reflection.RequireType(ReactionKindTypeName), "Ron"),
                evaluation);
        }

        private static object CreateDaiminkanCandidate(Scenario scenario, object seat)
        {
            return scenario.Reflection.InvokeStatic(
                scenario.Reflection.RequireType(ReactionWindowCandidateTypeName),
                "CreateDaiminkan",
                seat,
                scenario.Session.DataFactory.CreateTile("5m"));
        }

        private static object CreateChiCandidate(
            Scenario scenario,
            object seat,
            int optionId)
        {
            string firstHandTile;
            string secondHandTile;
            string firstMeldTile;
            string secondMeldTile;
            string thirdMeldTile;
            switch (optionId)
            {
                case 3:
                    firstHandTile = "3m";
                    secondHandTile = "4m";
                    firstMeldTile = "3m";
                    secondMeldTile = "4m";
                    thirdMeldTile = "5m";
                    break;
                case 4:
                    firstHandTile = "4m";
                    secondHandTile = "6m";
                    firstMeldTile = "4m";
                    secondMeldTile = "5m";
                    thirdMeldTile = "6m";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(optionId));
            }

            object option = scenario.Reflection.CreateInstance(
                scenario.Reflection.RequireType(ChiOptionTypeName),
                optionId,
                scenario.Session.DataFactory.CreateTile("5m"),
                CreateTypedList(
                    scenario.Session.Types.Tile,
                    scenario.Session.DataFactory.CreateTile(firstHandTile),
                    scenario.Session.DataFactory.CreateTile(secondHandTile)),
                CreateTypedList(
                    scenario.Session.Types.Tile,
                    scenario.Session.DataFactory.CreateTile(firstMeldTile),
                    scenario.Session.DataFactory.CreateTile(secondMeldTile),
                    scenario.Session.DataFactory.CreateTile(thirdMeldTile)));
            return scenario.Reflection.InvokeStatic(
                scenario.Reflection.RequireType(ReactionWindowCandidateTypeName),
                "CreateChi",
                seat,
                scenario.Session.DataFactory.CreateTile("5m"),
                CreateTypedList(scenario.Reflection.RequireType(ChiOptionTypeName), option));
        }

        private static IList CreateCandidateList(Scenario scenario, params object[] candidates)
        {
            return CreateTypedList(
                scenario.Reflection.RequireType(ReactionWindowCandidateTypeName),
                candidates);
        }

        private static IList CreateTypedList(Type elementType, params object[] values)
        {
            IList list = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(elementType));
            for (int i = 0; i < values.Length; i++)
                list.Add(values[i]);

            return list;
        }

        private static void AssertReactionOptionKinds(object request, params string[] expectedKinds)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            IList options = (IList)reflection.GetProperty(
                reflection.GetProperty(request, "Reaction"),
                "Options");
            string[] actualKinds = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
                actualKinds[i] = reflection.GetProperty(options[i], "Kind").ToString();

            Assert.That(actualKinds, Is.EqualTo(expectedKinds));
        }

        private static object FindReactionOption(object request, string kind)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            IList options = (IList)reflection.GetProperty(
                reflection.GetProperty(request, "Reaction"),
                "Options");
            for (int i = 0; i < options.Count; i++)
            {
                if (reflection.GetProperty(options[i], "Kind").ToString() == kind)
                    return options[i];
            }

            Assert.Fail($"Reaction option not found: {kind}");
            return null;
        }

        private static int[] GetChiOptionIds(Scenario scenario, object chiOption)
        {
            IList options = (IList)scenario.Reflection.GetProperty(chiOption, "ChiOptions");
            int[] optionIds = new int[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                optionIds[i] = (int)scenario.Reflection.GetProperty(
                    options[i],
                    "OptionId");
            }

            return optionIds;
        }

        private static string CandidateState(Scenario scenario, object candidate)
        {
            return scenario.Reflection.GetProperty(candidate, "ResponseState").ToString();
        }

        private static void AssertDiscardClaimed(Scenario scenario, int discardId)
        {
            object[] arguments = { discardId, null };
            Assert.That(
                (bool)scenario.Reflection.Invoke(
                    scenario.CurrentState,
                    "TryGetDiscardClaim",
                    arguments),
                Is.True);
            Assert.That(arguments[1], Is.Not.Null);
        }

        private static void AssertRejected(object result, string expectedReason)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Assert.That((bool)reflection.GetProperty(result, "Accepted"), Is.False);
            Assert.That((string)reflection.GetProperty(result, "Reason"), Is.EqualTo(expectedReason));
        }

        private sealed class EventSequenceRecorder : IDisposable
        {
            private readonly List<string> names = new List<string>();
            private readonly List<EventCallbackSubscription> subscriptions =
                new List<EventCallbackSubscription>();

            public EventSequenceRecorder(object eventSource, params string[] eventNames)
            {
                for (int i = 0; i < eventNames.Length; i++)
                {
                    string eventName = eventNames[i];
                    subscriptions.Add(EventCallbackSubscription.Create(
                        eventSource,
                        eventName,
                        () => names.Add(eventName)));
                }
            }

            public int Count(string eventName)
            {
                int count = 0;
                for (int i = 0; i < names.Count; i++)
                {
                    if (names[i] == eventName)
                        count++;
                }

                return count;
            }

            public int IndexOf(string eventName)
            {
                return names.IndexOf(eventName);
            }

            public int LastIndexOf(string eventName)
            {
                return names.LastIndexOf(eventName);
            }

            public string Describe()
            {
                return string.Join(" -> ", names);
            }

            public void Dispose()
            {
                for (int i = subscriptions.Count - 1; i >= 0; i--)
                    subscriptions[i].Dispose();
            }
        }

        private sealed class EventCallbackSubscription : IDisposable
        {
            private readonly object eventSource;
            private readonly EventInfo eventInfo;
            private readonly Delegate handler;

            private EventCallbackSubscription(
                object eventSource,
                EventInfo eventInfo,
                Delegate handler)
            {
                this.eventSource = eventSource;
                this.eventInfo = eventInfo;
                this.handler = handler;
            }

            public static EventCallbackSubscription Create(
                object eventSource,
                string eventName,
                Action callback)
            {
                Assert.That(eventSource, Is.Not.Null);
                Assert.That(callback, Is.Not.Null);

                EventInfo eventInfo = eventSource.GetType().GetEvent(
                    eventName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");

                ParameterInfo[] parameterInfos = eventInfo.EventHandlerType
                    .GetMethod("Invoke")
                    .GetParameters();
                ParameterExpression[] parameters =
                    new ParameterExpression[parameterInfos.Length];
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameters[i] = Expression.Parameter(
                        parameterInfos[i].ParameterType,
                        parameterInfos[i].Name);
                }

                MethodInfo callbackInvoke = typeof(Action).GetMethod(nameof(Action.Invoke));
                Delegate handler = Expression.Lambda(
                    eventInfo.EventHandlerType,
                    Expression.Call(Expression.Constant(callback), callbackInvoke),
                    parameters).Compile();
                eventInfo.AddEventHandler(eventSource, handler);
                return new EventCallbackSubscription(eventSource, eventInfo, handler);
            }

            public void Dispose()
            {
                eventInfo.RemoveEventHandler(eventSource, handler);
            }
        }

        private sealed class Scenario : IDisposable
        {
            public Scenario(
                MahjongGameFlowTestSession session,
                Dictionary<string, object> providers,
                object sourceSeat,
                object firstTargetSeat,
                object secondTargetSeat)
            {
                Session = session;
                Providers = providers;
                SourceSeat = sourceSeat;
                FirstTargetSeat = firstTargetSeat;
                SecondTargetSeat = secondTargetSeat;
            }

            public MahjongGameFlowTestSession Session { get; }
            public Dictionary<string, object> Providers { get; }
            public object SourceSeat { get; }
            public object FirstTargetSeat { get; }
            public object SecondTargetSeat { get; }
            public ReflectionTestAccess Reflection => Session.Reflection;
            public CollectionTestAccess Collections => Session.Collections;
            public object GameFlow => Session.GameFlow;
            public object CurrentState => Session.CurrentState;

            public void Dispose()
            {
                Session.Dispose();
            }
        }
    }
}
