using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class DoubleReachGameFlowTests
    {
        [Test]
        public void ReachDeclarationOnOwnFirstDiscard_StoresDoubleReach()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.PrepareReachableDrawForEast();

                driver.DeclareReachWithHandDiscard(12);

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsDoubleReachDeclared("East"), Is.True);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
            }
        }

        [Test]
        public void ReachDeclarationAfterCallOnOwnFirstDiscard_StoresNormalReach()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.PrepareReachableDrawForEast();
                driver.MarkCallOccurred();

                driver.DeclareReachWithHandDiscard(12);

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsDoubleReachDeclared("East"), Is.False);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
            }
        }

        [Test]
        public void ReachDeclarationOnOwnSecondDiscard_StoresNormalReach()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DrawAndDiscardForSeat("East", "C");
                driver.DrawAndDiscardForSeat("West", "C");

                driver.PrepareReachableDrawForEastWithoutStartingRound();
                driver.DeclareReachWithHandDiscard(12);

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsDoubleReachDeclared("East"), Is.False);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
            }
        }

        [Test]
        public void ReachDeclarationAfterOtherSeatDiscardStillUsesOwnFirstDiscard()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.ForceCurrentTurn("West", 2);
                driver.DrawAndDiscardForSeat("West", "C");

                driver.PrepareReachableDrawForEastWithoutStartingRound();
                driver.DeclareReachWithHandDiscard(12);

                Assert.That(driver.DiscardCount, Is.EqualTo(2));
                Assert.That(driver.DiscardActorSeatNameAt(0), Is.EqualTo("West"));
                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsDoubleReachDeclared("East"), Is.True);
            }
        }

        [Test]
        public void StartNewRound_ResetsDoubleReachState()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.PrepareReachableDrawForEast();
                driver.DeclareReachWithHandDiscard(12);

                driver.StartNewRound();

                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.IsDoubleReachDeclared("East"), Is.False);
                Assert.That(driver.IsIppatsuEligible("East"), Is.False);
            }
        }

        [Test]
        public void RonAfterDoubleReach_PropagatesDoubleReachToWinEvaluation()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.PrepareReachableDrawForEast();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);

                driver.DrawAndDiscardForSeat("West", "6m");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("DoubleReach"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Reach"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("Ippatsu"), Is.True);
            }
        }

        [Test]
        public void RonAfterNormalReach_PropagatesReachWithoutDoubleReachToWinEvaluation()
        {
            using (Driver driver = Driver.Create(2))
            {
                driver.StartNewRound();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DrawAndDiscardForSeat("East", "C");
                driver.DrawAndDiscardForSeat("West", "C");

                driver.PrepareReachableDrawForEastWithoutStartingRound();
                driver.DeclareReachWithHandDiscard(12);
                driver.DrawAndDiscardForSeat("West", "6m");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Reach"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("DoubleReach"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("Ippatsu"), Is.True);
                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.IsDoubleReachDeclared("East"), Is.False);
                Assert.That(driver.IsIppatsuEligible("East"), Is.True);
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

            public static Driver Create(int participantCount)
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory =
                    new MahjongTestDataFactory(reflection, types);
                object catalog = dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinition("DoubleReach", "Two", "None"),
                    dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                    dataFactory.CreateYakuDefinition("Ippatsu", "One", "None"),
                    dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"));
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "DoubleReachGameFlowTest",
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

            public bool IsWinDecisionPending => session.Query.IsWinDecisionPending;
            public int DiscardCount => session.Query.DiscardCount;

            public void StartNewRound()
            {
                session.Commands.StartNewRound();
            }

            public void MarkCallOccurred()
            {
                session.Reflection.Invoke(session.CurrentState, "MarkCallOccurred");
            }

            public void PrepareReachableDrawForEast()
            {
                StartNewRound();
                PrepareReachableDrawForEastWithoutStartingRound();
            }

            public void PrepareReachableDrawForEastWithoutStartingRound()
            {
                session.DataFactory.AddHandTilesFromText(
                    session.Query.GetPlayerSeat("East"),
                    "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m");
                session.Commands.RequestForceDrawSkillForSeat("East", "6m");
                Assert.That(session.Commands.TryRequestDrawForSeat("East"), Is.True);
            }

            public void DeclareReachWithHandDiscard(int handIndex)
            {
                session.Reflection.Invoke(session.GameFlow, "RequestDeclareReach");
                session.Commands.RequestDiscard(handIndex);
            }

            public void DrawAndDiscardForSeat(string seatName, string tileCode)
            {
                session.Commands.RequestForceDrawSkillForSeat(seatName, tileCode);
                Assert.That(session.Commands.TryRequestDrawForSeat(seatName), Is.True);
                Assert.That(
                    session.Commands.TryRequestDiscardDrawnTileForSeat(seatName),
                    Is.True);
            }

            public void ForceCurrentTurn(string seatName, int turnIndex)
            {
                session.DataFactory.SetCurrentTurn(session.CurrentState, seatName);
                session.Reflection.SetProperty(
                    session.CurrentState,
                    "TurnIndex",
                    turnIndex);
                session.Commands.StartTurn(seatName, turnIndex);
            }

            public void SetParticipantType(
                string seatName,
                string participantTypeName)
            {
                session.DataFactory.SetParticipantType(
                    session.CurrentState,
                    seatName,
                    participantTypeName);
            }

            public bool IsReachDeclared(string seatName)
            {
                return (bool)session.Reflection.GetProperty(
                    session.Query.GetPlayerSeat(seatName),
                    "IsReachDeclared");
            }

            public bool IsDoubleReachDeclared(string seatName)
            {
                return (bool)session.Reflection.GetProperty(
                    session.Query.GetPlayerSeat(seatName),
                    "IsDoubleReachDeclared");
            }

            public bool IsIppatsuEligible(string seatName)
            {
                return (bool)session.Reflection.GetProperty(
                    session.Query.GetPlayerSeat(seatName),
                    "IsIppatsuEligible");
            }

            public string DiscardActorSeatNameAt(int index)
            {
                return session.Query.DiscardActorSeatNameAt(index);
            }

            public bool PendingCandidateContainsYaku(string yakuKindName)
            {
                object evaluation = session.Query.PendingWinDeclarationEvaluation;
                if (evaluation == null)
                    return false;

                object handEvaluation = session.Reflection.GetProperty(
                    evaluation,
                    "HandEvaluationResult");
                object candidateResults = session.Reflection.GetProperty(
                    handEvaluation,
                    "CandidateResults");
                int candidateCount = session.Collections.Count(candidateResults);

                for (int i = 0; i < candidateCount; i++)
                {
                    object candidate = session.Collections.Item(candidateResults, i);
                    if (CandidateContainsYaku(candidate, yakuKindName))
                        return true;
                }

                return false;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                session.Dispose();
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
    }
}
