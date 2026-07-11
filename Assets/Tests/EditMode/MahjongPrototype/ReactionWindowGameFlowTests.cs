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
    public sealed class ReactionWindowGameFlowTests
    {
        [Test]
        public void HandDiscardAndDrawnTileDiscard_BothStartReactionWindowBeforeResolution()
        {
            Assert.That(CaptureReactionSourceAfterDiscard(false), Is.EqualTo("Hand"));
            Assert.That(CaptureReactionSourceAfterDiscard(true), Is.EqualTo("DrawnTile"));
        }

        [Test]
        public void NoReaction_ResolvesOnceThenAdvancesAfterReactionNotifications()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            using (EventSequenceRecorder events = new EventSequenceRecorder(
                session.EventNotifier,
                "TileDiscarded",
                "ReactionWindowStarted",
                "ReactionWindowResolved",
                "ReactionWindowClosed",
                "TurnStarted"))
            {
                events.ReactionWindowPendingProvider =
                    () => session.Query.IsReactionWindowPending;
                session.Commands.StartNewRound();
                session.DataFactory.SetDrawnTile(session.CurrentState, "East", "2m");

                session.Commands.RequestDiscardDrawnTile();

                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.CurrentReactionWindow, Is.Null);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(2));
                Assert.That(events.WasReactionWindowPendingWhenTileDiscarded, Is.True);
                Assert.That(events.Count("ReactionWindowStarted"), Is.EqualTo(1));
                Assert.That(events.Count("ReactionWindowResolved"), Is.EqualTo(1));
                Assert.That(events.Count("ReactionWindowClosed"), Is.EqualTo(1));
                Assert.That(
                    events.IndexOf("ReactionWindowStarted"),
                    Is.LessThan(events.IndexOf("ReactionWindowResolved")),
                    events.Describe());
                Assert.That(
                    events.IndexOf("ReactionWindowClosed"),
                    Is.LessThan(events.LastIndexOf("TurnStarted")),
                    events.Describe());
            }
        }

        [Test]
        public void PendingRon_RejectsWrongStaleAndDuplicateAnswers_ThenDeclineAdvancesOnce()
        {
            using (MahjongGameFlowTestSession session = CreateRonSession())
            using (EventSequenceRecorder events = new EventSequenceRecorder(
                session.EventNotifier,
                "ReactionWindowAnswered",
                "WinDeclined",
                "ReactionWindowResolved",
                "ReactionWindowClosed"))
            {
                int turnIndex = session.Query.TurnIndex;
                int windowId = session.Query.ReactionWindowId;

                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("ReactionWindow"));
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(turnIndex));
                Assert.That(session.Query.ReactionWindowCandidateCount, Is.EqualTo(1));
                Assert.That(session.Query.ReactionWindowSourceSeatName, Is.EqualTo("West"));
                Assert.That(session.Query.ReactionWindowSourceTileCode, Is.EqualTo("5m"));

                Assert.That(session.Commands.TryRequestDeclineRonForSeat("West", windowId), Is.False);
                Assert.That(session.Commands.TryRequestDeclineRonForSeat("East", windowId + 1), Is.False);
                Assert.That(session.Query.IsReactionWindowPending, Is.True);

                Assert.That(session.Commands.TryRequestDeclineRonForSeat("East", windowId), Is.True);

                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(turnIndex + 1));
                Assert.That(session.Commands.TryRequestDeclineRonForSeat("East", windowId), Is.False);
                Assert.That(session.Query.TurnIndex, Is.EqualTo(turnIndex + 1));
                Assert.That(events.Count("ReactionWindowAnswered"), Is.EqualTo(1));
                Assert.That(events.Count("ReactionWindowResolved"), Is.EqualTo(1));
                Assert.That(events.Count("ReactionWindowClosed"), Is.EqualTo(1));
                Assert.That(events.IndexOf("ReactionWindowAnswered"), Is.LessThan(events.IndexOf("WinDeclined")));
                Assert.That(events.IndexOf("WinDeclined"), Is.LessThan(events.IndexOf("ReactionWindowResolved")));
            }
        }

        [Test]
        public void PendingRon_BlocksNormalDrawAndSkill_AndRonDeclarationEndsRound()
        {
            using (MahjongGameFlowTestSession session = CreateRonSession())
            {
                int windowId = session.Query.ReactionWindowId;

                Assert.That(session.Commands.TryRequestDrawForSeat("West"), Is.False);
                session.Commands.RequestForceDrawSkillForSeat("West", "5m");
                Assert.That(session.Query.ActiveSkillEffectCount, Is.EqualTo(0));
                Assert.That(session.Query.IsInteractionLocked, Is.True);

                Assert.That(session.Commands.TryRequestDeclareRonForSeat("East", windowId), Is.True);

                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.IsRoundResultPending, Is.True);
                Assert.That(session.Query.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(session.Query.RoundResultWinnerSeatNameOrNull, Is.EqualTo("East"));
                Assert.That(session.Query.RoundResultWinTypeNameOrNull, Is.EqualTo("Ron"));
                Assert.That(session.Query.RoundResultSourceSeatNameOrNull, Is.EqualTo("West"));
                Assert.That(session.Query.RoundResultWinningTileCodeOrNull, Is.EqualTo("5m"));
            }
        }

        [Test]
        public void RetryPrototype_DiscardsPendingReactionWindowData()
        {
            using (MahjongGameFlowTestSession session = CreateRonSession())
            {
                Assert.That(session.Query.IsReactionWindowPending, Is.True);

                session.Commands.RetryPrototype();

                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.CurrentReactionWindow, Is.Null);
            }
        }

        [Test]
        public void PonCandidate_RequiresTwoMatchingTiles_AndPonCreatesPersistentClaimedMeld()
        {
            using (MahjongGameFlowTestSession session = CreatePonSession(false))
            {
                int windowId = session.Query.ReactionWindowId;
                int turnIndex = session.Query.TurnIndex;
                int sourceDiscardId = session.Query.LastDiscardId;

                Assert.That(session.Query.ReactionWindowCandidateCount, Is.EqualTo(1));
                Assert.That(session.Query.ReactionWindowCandidateKindAt(0), Is.EqualTo("Pon"));
                Assert.That(session.Commands.TryRequestDeclareRonForSeat("East", windowId), Is.False);
                Assert.That(session.Commands.TryRequestDeclarePonForSeat("West", windowId), Is.False);
                Assert.That(session.Commands.TryRequestDeclarePonForSeat("East", windowId + 1), Is.False);

                Assert.That(session.Commands.TryRequestDeclarePonForSeat("East", windowId), Is.True);

                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(turnIndex + 1));
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDiscardAfterCall"));
                Assert.That(session.Query.HasDrawnTile("East"), Is.False);
                Assert.That(session.Query.HandCount("East"), Is.EqualTo(11));
                Assert.That(session.Query.OpenMeldCount("East"), Is.EqualTo(1));
                Assert.That(session.Query.IsClosed("East"), Is.False);
                Assert.That(session.Query.DiscardCount, Is.EqualTo(1));
                Assert.That(session.Query.TryGetDiscardClaim(sourceDiscardId, out object claim), Is.True);
                Assert.That(session.Reflection.GetProperty(claim, "CallerSeat").ToString(), Is.EqualTo("East"));

                object openMeld = session.Query.OpenMeldAt("East", 0);
                Assert.That(session.Reflection.GetProperty(openMeld, "Type").ToString(), Is.EqualTo("Pon"));
                Assert.That(session.Collections.Count(session.Reflection.GetProperty(openMeld, "Tiles")), Is.EqualTo(3));
                Assert.That(session.Reflection.GetProperty(openMeld, "SourceSeat").ToString(), Is.EqualTo("West"));
                Assert.That((int)session.Reflection.GetProperty(openMeld, "SourceDiscardId"), Is.EqualTo(sourceDiscardId));

                Assert.That(session.Commands.TryRequestDrawForSeat("East"), Is.False);
                session.Commands.RequestForceDrawSkillForSeat("East", "1m");
                Assert.That(session.Query.ActiveSkillEffectCount, Is.EqualTo(0));
                session.Commands.RequestDiscard(0);

                Assert.That(session.Query.DiscardCount, Is.EqualTo(2));
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(session.Query.TurnIndex, Is.EqualTo(turnIndex + 2));
                Assert.That(session.Query.TurnPhaseName, Is.EqualTo("WaitingForDraw"));
            }
        }

        [Test]
        public void RonDecline_LeavesPonCandidateAndAppliesOnlyRonFuriten()
        {
            using (MahjongGameFlowTestSession session = CreatePonSession(true))
            {
                int windowId = session.Query.ReactionWindowId;

                Assert.That(session.Query.ReactionWindowCandidateCount, Is.EqualTo(2));
                Assert.That(session.Query.ReactionWindowCandidateKindAt(0), Is.EqualTo("Ron"));
                Assert.That(session.Query.ReactionWindowCandidateKindAt(1), Is.EqualTo("Pon"));
                Assert.That(session.Commands.TryRequestDeclarePonForSeat("East", windowId), Is.False);

                Assert.That(session.Commands.TryRequestDeclineRonForSeat("East", windowId), Is.True);
                Assert.That(session.Query.IsReactionWindowPending, Is.True);
                Assert.That(session.Query.IsTemporaryFuriten("East"), Is.True);
                Assert.That(session.Commands.TryRequestDeclineRonForSeat("East", windowId), Is.False);

                Assert.That(session.Commands.TryRequestDeclinePonForSeat("East", windowId), Is.True);
                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.OpenMeldCount("East"), Is.EqualTo(0));
                Assert.That(session.Query.IsTemporaryFuriten("East"), Is.True);
            }
        }

        [Test]
        public void PonCandidate_DoesNotAppearWithOnlyOneMatchingTile()
        {
            using (MahjongGameFlowTestSession session = CreateSession(2))
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTiles(
                    session.Query.GetPlayerSeat("East"),
                    "5m", "1m", "2m", "3m", "4m", "5p", "6p", "7p", "2s", "3s", "4s", "E", "S");
                session.DataFactory.SetCurrentTurn(session.CurrentState, "West");
                session.DataFactory.SetDrawnTile(session.CurrentState, "West", "5m");

                Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("West"), Is.True);
                Assert.That(session.Query.IsReactionWindowPending, Is.False);
                Assert.That(session.Query.CurrentTurnName, Is.EqualTo("East"));
            }
        }

        [Test]
        public void OpenPonHand_CanUseReducedConcealedTilesForStandardWin()
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            {
                Type openMeldType = session.Reflection.RequireType(
                    "MahjongPrototype.Domain.OpenMeld, Assembly-CSharp");
                Type openMeldKindType = session.Reflection.RequireType(
                    "MahjongPrototype.Domain.OpenMeldType, Assembly-CSharp");
                IList openMelds = (IList)Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(openMeldType));
                object openMeld = session.Reflection.CreateInstance(
                    openMeldType,
                    Enum.Parse(openMeldKindType, "Pon"),
                    session.DataFactory.CreateTileArray("5m", "5m", "5m"),
                    session.DataFactory.ParseSeat("East"),
                    session.DataFactory.ParseSeat("West"),
                    session.DataFactory.CreateTile("5m"),
                    1);
                openMelds.Add(openMeld);

                object winChecker = session.Reflection.CreateInstance(
                    session.Reflection.RequireType("MahjongPrototype.Services.WinChecker, Assembly-CSharp"));
                bool canWin = (bool)session.Reflection.Invoke(
                    winChecker,
                    "CanWinWithTile",
                    session.DataFactory.CreateTileArray(
                        "2m", "3m", "4m",
                        "3p", "4p", "5p",
                        "6s", "6s",
                        "6p", "6p"),
                    session.DataFactory.CreateTile("6s"),
                    openMelds);

                Assert.That(canWin, Is.True);
            }
        }

        private static string CaptureReactionSourceAfterDiscard(bool discardDrawnTile)
        {
            using (MahjongGameFlowTestSession session = CreateSession(1))
            using (EventSequenceRecorder events = new EventSequenceRecorder(
                session.EventNotifier,
                "ReactionWindowStarted"))
            {
                session.Commands.StartNewRound();
                if (discardDrawnTile)
                {
                    session.DataFactory.SetDrawnTile(session.CurrentState, "East", "2m");
                    session.Commands.RequestDiscardDrawnTile();
                }
                else
                {
                    session.DataFactory.AddHandTiles(
                        session.Query.GetPlayerSeat("East"),
                        "1m");
                    session.DataFactory.SetDrawnTile(session.CurrentState, "East", "2m");
                    session.Commands.RequestDiscard(0);
                }

                object reactionWindow = events.FirstPayload("ReactionWindowStarted");
                object discard = session.Reflection.GetProperty(reactionWindow, "SourceDiscard");
                return session.Reflection.GetProperty(discard, "Source").ToString();
            }
        }

        private static MahjongGameFlowTestSession CreateRonSession()
        {
            MahjongGameFlowTestSession session = CreateSession(2);
            session.Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                "2m", "3m", "4m",
                "2p", "3p", "4p",
                "2s", "3s", "4s",
                "6m", "7m", "8m",
                "5m");
            session.DataFactory.SetCurrentTurn(session.CurrentState, "West");
            session.DataFactory.SetDrawnTile(session.CurrentState, "West", "5m");

            Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("West"), Is.True);
            return session;
        }

        private static MahjongGameFlowTestSession CreatePonSession(bool includeRonCandidate)
        {
            MahjongGameFlowTestSession session = CreateSession(2);
            session.Commands.StartNewRound();
            string[] handTiles = includeRonCandidate
                ? new[]
                {
                    "5m", "5m", "2m", "3m", "4m", "3p", "4p", "5p", "4s", "5s", "6s", "6p", "6p"
                }
                : new[]
                {
                    "5m", "5m", "1m", "2m", "3m", "4p", "5p", "6p", "2s", "3s", "4s", "E", "S"
                };
            session.DataFactory.AddHandTiles(
                session.Query.GetPlayerSeat("East"),
                handTiles);
            session.DataFactory.SetCurrentTurn(session.CurrentState, "West");
            session.DataFactory.SetDrawnTile(session.CurrentState, "West", "5m");

            Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat("West"), Is.True);
            return session;
        }

        private static MahjongGameFlowTestSession CreateSession(int participantCount)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog = MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                new MahjongGameFlowTestOptions
                {
                    RootName = "ReactionWindowGameFlowTest",
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
                },
                reflection,
                collections,
                types,
                dataFactory);
            session.RegisterOwnedScriptableObject(catalog);
            return session;
        }

        private sealed class EventSequenceRecorder : IDisposable
        {
            private readonly object eventSource;
            private readonly List<string> names = new List<string>();
            private readonly List<object> payloads = new List<object>();
            private readonly List<EventSubscription> subscriptions = new List<EventSubscription>();

            public EventSequenceRecorder(object eventSource, params string[] eventNames)
            {
                this.eventSource = eventSource;
                for (int i = 0; i < eventNames.Length; i++)
                    Subscribe(eventNames[i]);
            }

            public Func<bool> ReactionWindowPendingProvider { get; set; }
            public bool WasReactionWindowPendingWhenTileDiscarded { get; private set; }

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
                int index = names.IndexOf(eventName);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), $"Event not found: {eventName}");
                return index;
            }

            public int LastIndexOf(string eventName)
            {
                int index = names.LastIndexOf(eventName);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), $"Event not found: {eventName}");
                return index;
            }

            public object FirstPayload(string eventName)
            {
                int index = IndexOf(eventName);
                return payloads[index];
            }

            public string Describe()
            {
                return string.Join(" -> ", names);
            }

            public void Dispose()
            {
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
                ParameterInfo[] parameterInfos = eventHandlerType.GetMethod("Invoke").GetParameters();
                ParameterExpression[] parameters = new ParameterExpression[parameterInfos.Length];
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameters[i] = Expression.Parameter(
                        parameterInfos[i].ParameterType,
                        parameterInfos[i].Name);
                }

                Expression payload = parameters.Length <= 0
                    ? Expression.Constant(null, typeof(object))
                    : Expression.Convert(parameters[0], typeof(object));
                MethodInfo record = GetType().GetMethod(
                    nameof(Record),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodCallExpression body = Expression.Call(
                    Expression.Constant(this),
                    record,
                    Expression.Constant(eventName),
                    payload);
                return Expression.Lambda(eventHandlerType, body, parameters).Compile();
            }

            private void Record(string eventName, object payload)
            {
                if (eventName == "TileDiscarded" &&
                    ReactionWindowPendingProvider != null)
                {
                    WasReactionWindowPendingWhenTileDiscarded =
                        ReactionWindowPendingProvider();
                }

                names.Add(eventName);
                payloads.Add(payload);
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
