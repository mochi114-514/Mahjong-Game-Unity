using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class IppatsuGameFlowTests
    {
        [Test]
        public void RequestDeclareReach_DoesNotStartIppatsuDuringDiscardSelection()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();

                driver.RequestDeclareReach();

                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.IsIppatsuEligible("East"), Is.False);
            }
        }

        [Test]
        public void ReachDeclarationHandDiscard_StartsIppatsuWithoutExpiringOnReachDiscard()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");

                driver.DeclareReachWithHandDiscard(12);

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void ReachDeclarationDrawnTileDiscard_StartsIppatsuWithoutExpiringOnReachDiscard()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");

                driver.DeclareReachWithDrawnTileDiscard();

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
            }
        }

        [Test]
        public void OtherSeatDiscard_DoesNotExpireIppatsu()
        {
            using (Driver driver = Driver.Create(3))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("South", "LocalHuman");
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);

                driver.DrawAndDiscardForSeat("South", "C");

                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("South"));
            }
        }

        [Test]
        public void RonDuringIppatsu_AddsReachAndIppatsu()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);

                driver.DrawAndDiscardForSeat("West", "6m");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Ron"));
                Assert.That(driver.PendingCandidateContainsAllYakus("Reach", "Ippatsu"), Is.True);
                Assert.That(driver.PendingCandidateTotalHanContaining("Ippatsu"), Is.EqualTo(2));
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void TsumoDuringIppatsu_AddsReachIppatsuAndMenzenTsumo()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);

                driver.ForceDrawForSeat("East", "6m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Tsumo"));
                Assert.That(
                    driver.PendingCandidateContainsAllYakus(
                        "Reach",
                        "Ippatsu",
                        "MenzenTsumo"),
                    Is.True);
                Assert.That(driver.PendingCandidateTotalHanContaining("Ippatsu"), Is.EqualTo(3));
            }
        }

        [Test]
        public void NextSuccessfulDiscardAfterReach_ExpiresIppatsuAndKeepsReach()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);

                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.False);
                Assert.That(driver.HasDrawnTile("East"), Is.False);
                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("9m"));
            }
        }

        [Test]
        public void AfterIppatsuExpired_LaterTsumoWinDoesNotAddIppatsu()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");

                driver.ForceDrawForSeat("East", "6m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsAllYakus("Reach", "MenzenTsumo"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Ippatsu"), Is.False);
                Assert.That(driver.PendingCandidateTotalHanContaining("Reach"), Is.EqualTo(2));
            }
        }

        [Test]
        public void BlockedDiscard_DoesNotExpireIppatsu()
        {
            using (Driver driver = Driver.Create(2, autoDiscardDelaySeconds: 0.05f))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");
                int discardCountBeforeBlockedDiscard = driver.DiscardCount;

                driver.RequestDiscard(0);

                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeBlockedDiscard));
                Assert.That(driver.HasDrawnTile("East"), Is.True);
            }
        }

        [Test]
        public void DeclineRon_DoesNotDirectlyExpireIppatsu()
        {
            using (Driver driver = Driver.Create(2, autoDiscardDelaySeconds: 0.05f))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "6m");

                driver.RequestDeclineWin();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.HasDrawnTile("East"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);

                Assert.That(driver.TryDiscardDrawnTileForSeat("East"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.False);
            }
        }

        [Test]
        public void ClearIppatsuEligibilityForAllPlayers_ClearsOnlyIppatsu()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.StartNewRound();
                driver.DeclareReachDirect("East", 1);
                driver.DeclareReachDirect("West", 2);

                driver.ClearIppatsuEligibilityForAllPlayers();

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsReachDeclared("West"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.False);
                Assert.That(driver.IsIppatsuEligible("West"), Is.False);
            }
        }

        [Test]
        public void DeclareWin_WritesIppatsuInWinResultLogOnlyWhenWinDeclared()
        {
            using (WinResultLogCapture capture = new WinResultLogCapture())
            using (Driver driver = Driver.Create(2, addGameLogRecorder: true))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                driver.ForceDrawForSeat("East", "6m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(capture.Count, Is.EqualTo(0));

                driver.RequestDeclareWin();

                Assert.That(capture.Count, Is.EqualTo(1));
                StringAssert.Contains("\u4E00\u767A(1", capture.SingleMessage);
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

            public static Driver Create(
                int participantCount,
                float autoDiscardDelaySeconds = 0f,
                bool addGameLogRecorder = false)
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
                object catalog = dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinitionWithDisplayName(
                        "Reach",
                        "\u7ACB\u76F4",
                        "One",
                        "None"),
                    dataFactory.CreateYakuDefinitionWithDisplayName(
                        "Ippatsu",
                        "\u4E00\u767A",
                        "One",
                        "None"),
                    dataFactory.CreateYakuDefinitionWithDisplayName(
                        "MenzenTsumo",
                        "\u9580\u524D\u6E05\u81EA\u6478\u548C",
                        "One",
                        "None"));

                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "IppatsuGameFlowTest",
                    AddEventNotifier = true,
                    AddGameLogRecorder = addGameLogRecorder,
                    LogWarnings = false,
                    ParticipantCount = participantCount,
                    InitialHandTileCount = 0,
                    AutoStart = false,
                    UseFixedRandomSeed = true,
                    FixedRandomSeed = 12345,
                    EnableAutoDraw = false,
                    AutoDiscardDrawnTileDelaySeconds = autoDiscardDelaySeconds,
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

            public bool IsReachDiscardSelectionPending =>
                (bool)session.Reflection.GetProperty(
                    session.CurrentState,
                    "IsReachDiscardSelectionPending");

            public bool IsWinDecisionPending => session.Query.IsWinDecisionPending;
            public string WinDecisionTypeName => session.Query.WinDecisionTypeName;
            public string CurrentTurnName => session.Query.CurrentTurnName;
            public int DiscardCount => session.Query.DiscardCount;
            public string LastDiscardActorSeatName => session.Query.LastDiscardActorSeatName;
            public string LastDiscardSourceName => session.Query.LastDiscardSourceName;
            public string LastDiscardTileCode => session.Query.LastDiscardTileCode;

            public void StartNewRound()
            {
                session.Commands.StartNewRound();
            }

            public void DrawReachableHand()
            {
                StartNewRound();
                session.DataFactory.AddHandTilesFromText(
                    session.Query.GetPlayerSeat("East"),
                    "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m");
                session.Commands.RequestForceDrawSkill("6m");
                session.Commands.RequestDraw();
            }

            public void DeclareReachWithHandDiscard(int handIndex)
            {
                RequestDeclareReach();
                RequestDiscard(handIndex);
            }

            public void DeclareReachWithDrawnTileDiscard()
            {
                RequestDeclareReach();
                session.Commands.RequestDiscardDrawnTile();
            }

            public void RequestDeclareReach()
            {
                session.Reflection.Invoke(session.GameFlow, "RequestDeclareReach");
            }

            public void RequestDiscard(int handIndex)
            {
                session.Commands.RequestDiscard(handIndex);
            }

            public void RequestDeclareWin()
            {
                session.Commands.RequestDeclareWin();
            }

            public void RequestDeclineWin()
            {
                session.Commands.RequestDeclineWin();
            }

            public void SetParticipantType(string seatName, string participantTypeName)
            {
                session.DataFactory.SetParticipantType(
                    session.CurrentState,
                    seatName,
                    participantTypeName);
            }

            public void ForceDrawForSeat(string seatName, string tileCode)
            {
                session.Commands.RequestForceDrawSkillForSeat(seatName, tileCode);
            }

            public void DrawAndDiscardForSeat(string seatName, string tileCode)
            {
                ForceDrawForSeat(seatName, tileCode);
                Assert.That(session.Commands.TryRequestDrawForSeat(seatName), Is.True);
                Assert.That(session.Commands.TryRequestDiscardDrawnTileForSeat(seatName), Is.True);
            }

            public bool TryDiscardDrawnTileForSeat(string seatName)
            {
                return session.Commands.TryRequestDiscardDrawnTileForSeat(seatName);
            }

            public void DeclareReachDirect(string seatName, int turnIndex)
            {
                session.Reflection.Invoke(
                    session.Query.GetPlayerSeat(seatName),
                    "DeclareReach",
                    turnIndex);
            }

            public void ClearIppatsuEligibilityForAllPlayers()
            {
                session.Reflection.Invoke(
                    session.CurrentState,
                    "ClearIppatsuEligibilityForAllPlayers");
            }

            public bool IsReachDeclared(string seatName)
            {
                return (bool)session.Reflection.GetProperty(
                    session.Query.GetPlayerSeat(seatName),
                    "IsReachDeclared");
            }

            public bool IsIppatsuEligible(string seatName)
            {
                return (bool)session.Reflection.GetProperty(
                    session.Query.GetPlayerSeat(seatName),
                    "IsIppatsuEligible");
            }

            public bool HasDrawnTile(string seatName)
            {
                return session.Query.HasDrawnTile(seatName);
            }

            public bool PendingCandidateContainsYaku(string yakuKindName)
            {
                return FindPendingCandidateContainingYaku(yakuKindName) != null;
            }

            public bool PendingCandidateContainsAllYakus(params string[] yakuKindNames)
            {
                return FindPendingCandidateContainingYakus(yakuKindNames) != null;
            }

            public int PendingCandidateTotalHanContaining(string yakuKindName)
            {
                object candidate = FindPendingCandidateContainingYaku(yakuKindName);
                Assert.That(candidate, Is.Not.Null);
                return (int)session.Reflection.GetProperty(candidate, "TotalHan");
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                session.Dispose();
            }

            private object FindPendingCandidateContainingYaku(string yakuKindName)
            {
                return FindPendingCandidateContainingYakus(yakuKindName);
            }

            private object FindPendingCandidateContainingYakus(params string[] yakuKindNames)
            {
                object evaluation = session.Query.PendingWinDeclarationEvaluation;
                if (evaluation == null)
                    return null;

                object handEvaluation =
                    session.Reflection.GetProperty(evaluation, "HandEvaluationResult");
                object candidateResults =
                    session.Reflection.GetProperty(handEvaluation, "CandidateResults");
                int candidateCount = session.Collections.Count(candidateResults);

                for (int i = 0; i < candidateCount; i++)
                {
                    object candidate = session.Collections.Item(candidateResults, i);
                    if (CandidateContainsAllYakus(candidate, yakuKindNames))
                        return candidate;
                }

                return null;
            }

            private bool CandidateContainsAllYakus(object candidate, string[] yakuKindNames)
            {
                for (int i = 0; i < yakuKindNames.Length; i++)
                {
                    if (!CandidateContainsYaku(candidate, yakuKindNames[i]))
                        return false;
                }

                return true;
            }

            private bool CandidateContainsYaku(object candidate, string yakuKindName)
            {
                object yakus = session.Reflection.GetProperty(candidate, "Yakus");
                int yakuCount = session.Collections.Count(yakus);

                for (int i = 0; i < yakuCount; i++)
                {
                    object yaku = session.Collections.Item(yakus, i);
                    if (session.Reflection.GetProperty(yaku, "Kind").ToString() == yakuKindName)
                        return true;
                }

                return false;
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
                    condition.Contains("\u4E00\u767A"))
                {
                    messages.Add(condition);
                }
            }
        }
    }
}
