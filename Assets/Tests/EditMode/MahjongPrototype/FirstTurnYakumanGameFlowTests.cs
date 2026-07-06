using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FirstTurnYakumanGameFlowTests
    {
        [Test]
        public void EastFirstTurnTsumoWin_AddsTenhou()
        {
            using (Driver driver = Driver.Create())
            {
                driver.StartNewRound();
                driver.DrawWinningTsumoForSeat("East");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Tenhou"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Chiihou"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void EastAfterOwnDiscardTsumoWin_DoesNotAddTenhou()
        {
            using (Driver driver = Driver.Create())
            {
                driver.StartNewRound();
                driver.AddDiscard("East", "9s", 1);
                driver.DrawWinningTsumoForSeat("East");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Tenhou"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.True);
            }
        }

        [Test]
        public void EastAfterOtherSeatDiscardTsumoWin_DoesNotAddTenhou()
        {
            using (Driver driver = Driver.Create())
            {
                driver.StartNewRound();
                driver.AddDiscard("South", "9s", 1);
                driver.DrawWinningTsumoForSeat("East");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Tenhou"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.True);
            }
        }

        [Test]
        public void ChildFirstTurnTsumoWin_AddsChiihou()
        {
            using (Driver driver = Driver.Create())
            {
                driver.StartNewRound();
                driver.ForceCurrentTurn("South", 2);
                driver.DrawWinningTsumoForSeat("South");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Chiihou"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Tenhou"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void ChildAfterOtherSeatDiscardStillFirstTsumo_AddsChiihou()
        {
            using (Driver driver = Driver.Create())
            {
                driver.StartNewRound();
                driver.AddDiscard("East", "9s", 1);
                driver.ForceCurrentTurn("South", 2);
                driver.DrawWinningTsumoForSeat("South");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Chiihou"), Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Tenhou"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void ChildAfterOwnDiscardTsumoWin_DoesNotAddChiihou()
        {
            using (Driver driver = Driver.Create())
            {
                driver.StartNewRound();
                driver.AddDiscard("South", "9s", 1);
                driver.ForceCurrentTurn("South", 2);
                driver.DrawWinningTsumoForSeat("South");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.PendingCandidateContainsYaku("Chiihou"), Is.False);
                Assert.That(driver.PendingCandidateContainsYaku("MenzenTsumo"), Is.True);
            }
        }

        private sealed class Driver : IDisposable
        {
            private const string StandardHand =
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";

            private readonly MahjongGameFlowTestSession session;
            private bool disposed;

            private Driver(MahjongGameFlowTestSession session)
            {
                this.session = session;
            }

            public static Driver Create()
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                CollectionTestAccess collections = new CollectionTestAccess(reflection);
                MahjongTestTypes types = new MahjongTestTypes(reflection);
                MahjongTestDataFactory dataFactory =
                    new MahjongTestDataFactory(reflection, types);
                object catalog = dataFactory.CreateYakuCatalog(
                    dataFactory.CreateYakuDefinition(
                        "Tenhou",
                        "None",
                        "None",
                        isYakuman: true),
                    dataFactory.CreateYakuDefinition(
                        "Chiihou",
                        "None",
                        "None",
                        isYakuman: true),
                    dataFactory.CreateYakuDefinition(
                        "MenzenTsumo",
                        "One",
                        "None"));
                MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
                {
                    RootName = "FirstTurnYakumanGameFlowTest",
                    AddEventNotifier = true,
                    LogWarnings = false,
                    ParticipantCount = 4,
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

            public void StartNewRound()
            {
                session.Commands.StartNewRound();
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

            public void AddDiscard(string seatName, string tileCode, int turnIndex)
            {
                session.DataFactory.AddDiscard(
                    session.CurrentState,
                    seatName,
                    tileCode,
                    turnIndex);
            }

            public void DrawWinningTsumoForSeat(string seatName)
            {
                object playerSeat = session.Query.GetPlayerSeat(seatName);
                session.DataFactory.AddHandTilesFromText(playerSeat, StandardHand);
                session.Commands.RequestForceDrawSkillForSeat(seatName, "C");

                Assert.That(session.Commands.TryRequestDrawForSeat(seatName), Is.True);
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
