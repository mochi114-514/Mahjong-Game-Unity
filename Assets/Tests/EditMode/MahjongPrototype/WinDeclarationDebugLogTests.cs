using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class WinDeclarationDebugLogTests
    {
        private const string BasicIipeikouHand =
            "1m 2m 3m 1m 2m 3m 7p 7p 7p 9s 9s 9s 5s";
        private const string RyanpeikouDifferentPairsHand =
            "1m 2m 3m 1m 2m 3m 2m 3m 4m 2m 3m 5p 5p";
        private const string MultiWaitPinfuHand =
            "1m 2m 3m 4m 5m 2p 3p 4p 5p 6p 7p 4s 4s";
        private const string AmbiguousMultiCandidateHand =
            "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m";

        [Test]
        public void WinDecisionPending_DoesNotWriteWinResultConsoleLog()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.MenzenTsumo()))
            {
                driver.PrepareTsumoDecision(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(capture.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void DeclineWin_DoesNotWriteWinResultConsoleLog()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.MenzenTsumo()))
            {
                driver.PrepareTsumoDecision(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C");

                driver.RequestDeclineWin();

                Assert.That(capture.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void DeclareWin_WritesOneWinResultConsoleLog()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.MenzenTsumo()))
            {
                driver.PrepareTsumoDecision(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C");

                driver.RequestDeclareWin();

                Assert.That(capture.Count, Is.EqualTo(1));
                StringAssert.StartsWith("[和了結果]", capture.SingleMessage);
            }
        }

        [Test]
        public void DeclareWin_IipeikouLogUsesCandidateYakuAndTankiWait()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.Iipeikou()))
            {
                driver.PrepareTsumoDecision(BasicIipeikouHand, "5s");

                driver.RequestDeclareWin();

                string message = capture.SingleMessage;
                StringAssert.Contains("一盃口(1翻)", message);
                StringAssert.Contains("合計=1翻", message);
                StringAssert.Contains("待ち=単騎", message);
                Assert.That(message, Does.Not.Contain("二盃口"));
            }
        }

        [Test]
        public void DeclareWin_RyanpeikouLogSuppressesIipeikou()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.Ryanpeikou()))
            {
                driver.PrepareTsumoDecision(RyanpeikouDifferentPairsHand, "4m");

                driver.RequestDeclareWin();

                string message = capture.SingleMessage;
                StringAssert.Contains("二盃口(3翻)", message);
                StringAssert.Contains("合計=3翻", message);
                StringAssert.Contains("待ち=両面", message);
                Assert.That(message, Does.Not.Contain("一盃口(1翻)"));
            }
        }

        [Test]
        public void DeclareWin_CombinesPinfuAndRyanpeikouWithinSameCandidateLine()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(
                1,
                YakuSpec.Pinfu(),
                YakuSpec.Ryanpeikou()))
            {
                driver.PrepareTsumoDecision(RyanpeikouDifferentPairsHand, "4m");

                driver.RequestDeclareWin();

                string line = capture.SingleCandidateLineContaining("平和(1翻)");
                StringAssert.Contains("二盃口(3翻)", line);
                StringAssert.Contains("合計=4翻", line);
            }
        }

        [Test]
        public void DeclareWin_DoesNotSelectOrSumMultipleYakuCandidates()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.MenzenTsumo()))
            {
                driver.PrepareTsumoDecision(AmbiguousMultiCandidateHand, "7m");

                driver.RequestDeclareWin();

                Assert.That(capture.CandidateLineCount, Is.GreaterThanOrEqualTo(2));
                StringAssert.Contains("成立候補1", capture.SingleMessage);
                StringAssert.Contains("成立候補2", capture.SingleMessage);
                Assert.That(capture.SingleMessage, Does.Not.Contain("合計=2翻"));
            }
        }

        [Test]
        public void DeclareWin_DoesNotDisplayNoYakuCandidates()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.Pinfu()))
            {
                driver.PrepareTsumoDecision(MultiWaitPinfuHand, "3m");

                driver.RequestDeclareWin();

                Assert.That(capture.CandidateLineCount, Is.EqualTo(1));
                StringAssert.Contains("待ち=両面", capture.SingleMessage);
                Assert.That(capture.SingleMessage, Does.Not.Contain("待ち=辺張"));
            }
        }

        [Test]
        public void DeclareWin_LogShowsTsumoWithoutDealInSeat()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(1, YakuSpec.MenzenTsumo()))
            {
                driver.PrepareTsumoDecision(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C");

                driver.RequestDeclareWin();

                StringAssert.Contains("和了方法=ツモ", capture.SingleMessage);
                Assert.That(capture.SingleMessage, Does.Not.Contain("放銃者="));
            }
        }

        [Test]
        public void DeclareWin_LogShowsRonWithDealInSeat()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(2, YakuSpec.Iipeikou()))
            {
                driver.PrepareRonDecision(BasicIipeikouHand, "5s", "West");

                driver.RequestDeclareWin();

                StringAssert.Contains("和了方法=ロン", capture.SingleMessage);
                StringAssert.Contains("放銃者=West", capture.SingleMessage);
            }
        }

        [Test]
        public void DeclareWin_KeepsLegacyWinDeclaredEvents()
        {
            using (Driver driver = Driver.Create(1, YakuSpec.MenzenTsumo()))
            using (ReflectedEventCounter winDeclared =
                new ReflectedEventCounter(driver.EventNotifier, "WinDeclared"))
            using (ReflectedEventCounter winDeclaredDetailed =
                new ReflectedEventCounter(driver.EventNotifier, "WinDeclaredDetailed"))
            {
                driver.PrepareTsumoDecision(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C");

                driver.RequestDeclareWin();

                Assert.That(winDeclared.Count, Is.EqualTo(1));
                Assert.That(winDeclaredDetailed.Count, Is.EqualTo(1));
            }
        }

        private readonly struct YakuSpec
        {
            private YakuSpec(
                string kindName,
                string displayName,
                string closedHanName,
                string openHanName)
            {
                KindName = kindName;
                DisplayName = displayName;
                ClosedHanName = closedHanName;
                OpenHanName = openHanName;
            }

            public string KindName { get; }
            public string DisplayName { get; }
            public string ClosedHanName { get; }
            public string OpenHanName { get; }

            public static YakuSpec MenzenTsumo()
            {
                return new YakuSpec("MenzenTsumo", "門前清自摸和", "One", "None");
            }

            public static YakuSpec Pinfu()
            {
                return new YakuSpec("Pinfu", "平和", "One", "None");
            }

            public static YakuSpec Iipeikou()
            {
                return new YakuSpec("Iipeikou", "一盃口", "One", "None");
            }

            public static YakuSpec Ryanpeikou()
            {
                return new YakuSpec("Ryanpeikou", "二盃口", "Three", "None");
            }
        }

        private sealed class Driver : IDisposable
        {
            private readonly MahjongGameFlowTestSession session;
            private bool disposed;

            private Driver(MahjongGameFlowTestSession session)
            {
                this.session = session;
            }

            public object EventNotifier => session.EventNotifier;

            public bool IsWinDecisionPending => session.Query.IsWinDecisionPending;

            public static Driver Create(int participantCount, params YakuSpec[] yakuSpecs)
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
                object[] definitions = new object[yakuSpecs.Length];

                for (int i = 0; i < yakuSpecs.Length; i++)
                {
                    definitions[i] = dataFactory.CreateYakuDefinitionWithDisplayName(
                        yakuSpecs[i].KindName,
                        yakuSpecs[i].DisplayName,
                        yakuSpecs[i].ClosedHanName,
                        yakuSpecs[i].OpenHanName);
                }

                object catalog = dataFactory.CreateYakuCatalog(definitions);
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "WinDeclarationDebugLogHarness",
                    AddEventNotifier = true,
                    AddGameLogRecorder = true,
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

            public void PrepareTsumoDecision(string handText, string winningTileCode)
            {
                session.Commands.StartNewRound();
                session.DataFactory.AddHandTilesFromText(
                    session.Query.GetPlayerSeat("East"),
                    handText);
                session.Commands.RequestForceDrawSkill(winningTileCode);
                session.Commands.RequestDraw();
                Assert.That(IsWinDecisionPending, Is.True);
            }

            public void PrepareRonDecision(
                string handText,
                string winningTileCode,
                string sourceSeatName)
            {
                session.Commands.StartNewRound();
                session.DataFactory.SetParticipantType(
                    session.CurrentState,
                    sourceSeatName,
                    "LocalHuman");
                session.DataFactory.AddHandTilesFromText(
                    session.Query.GetPlayerSeat("East"),
                    handText);
                session.DataFactory.SetDrawnTile(
                    session.CurrentState,
                    sourceSeatName,
                    winningTileCode);
                session.DataFactory.SetCurrentTurn(session.CurrentState, sourceSeatName);

                bool discarded =
                    session.Commands.TryRequestDiscardDrawnTileForSeat(sourceSeatName);

                Assert.That(discarded, Is.True);
                Assert.That(IsWinDecisionPending, Is.True);
            }

            public void RequestDeclareWin()
            {
                session.Commands.RequestDeclareWin();
            }

            public void RequestDeclineWin()
            {
                session.Commands.RequestDeclineWin();
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                session.Dispose();
            }
        }

        private sealed class WinResultLogCapture : IDisposable
        {
            private readonly List<string> messages = new List<string>();
            private bool disposed;

            public WinResultLogCapture()
            {
                Application.logMessageReceived += HandleLogMessageReceived;
            }

            public int Count => messages.Count;

            public string SingleMessage
            {
                get
                {
                    Assert.That(messages.Count, Is.EqualTo(1));
                    return messages[0];
                }
            }

            public int CandidateLineCount
            {
                get
                {
                    int count = 0;
                    string[] lines = SingleMessage.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("成立候補", StringComparison.Ordinal))
                            count++;
                    }

                    return count;
                }
            }

            public string SingleCandidateLineContaining(string expectedText)
            {
                string[] lines = SingleMessage.Split('\n');
                List<string> matches = new List<string>();

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(expectedText))
                        matches.Add(lines[i]);
                }

                Assert.That(matches.Count, Is.EqualTo(1));
                return matches[0];
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                Application.logMessageReceived -= HandleLogMessageReceived;
            }

            private void HandleLogMessageReceived(
                string condition,
                string stackTrace,
                LogType type)
            {
                if (type == LogType.Log &&
                    condition.StartsWith("[和了結果]", StringComparison.Ordinal))
                {
                    messages.Add(condition);
                }
            }
        }

        private sealed class ReflectedEventCounter : IDisposable
        {
            private readonly object eventSource;
            private readonly EventInfo eventInfo;
            private readonly Delegate handler;
            private bool disposed;

            public ReflectedEventCounter(object eventSource, string eventName)
            {
                this.eventSource = eventSource;
                Assert.That(eventSource, Is.Not.Null);

                eventInfo = eventSource.GetType().GetEvent(
                    eventName,
                    BindingFlags.Public | BindingFlags.Instance);
                Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");

                handler = CreateHandler(eventInfo.EventHandlerType);
                eventInfo.AddEventHandler(eventSource, handler);
            }

            public int Count { get; private set; }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                eventInfo.RemoveEventHandler(eventSource, handler);
            }

            private void Increment()
            {
                Count++;
            }

            private Delegate CreateHandler(Type eventHandlerType)
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

                MethodInfo increment = GetType().GetMethod(
                    nameof(Increment),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                MethodCallExpression body =
                    Expression.Call(Expression.Constant(this), increment);

                return Expression.Lambda(eventHandlerType, body, parameters).Compile();
            }
        }
    }
}
