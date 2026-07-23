using System.Collections;
using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class CpuTurnGameFlowTests
    {
        [UnityTest]
        public IEnumerator OpeningCpuTurn_WhenEastIsNotSelf_AdvancesThroughCpuController()
        {
            using (CpuTurnGameFlowTestDriver driver =
                CpuTurnGameFlowTestDriver.CreateForOpeningCpuTurn())
            {
                driver.SubscribeCpuTurnEventTrace();
                driver.StartNewRound();

                Assert.That(driver.SelfSeatName, Is.EqualTo("West"));
                Assert.That(driver.Player2SeatName, Is.EqualTo("East"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(1));
                Assert.That(driver.Player2HasDrawnTile, Is.True);

                yield return null;
                yield return null;

                Assert.That(driver.HasCpuDiscardRecord, Is.True);
                Assert.That(driver.CpuDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_AutoDrawsAndDiscardsDrawnTileThroughGameFlow()
        {
            using (CpuTurnGameFlowTestDriver driver = CpuTurnGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SubscribeCpuTurnEventTrace();

                driver.RequestSelfDiscardDrawnTile();

                Assert.That(driver.Player2HasDrawnTile, Is.True);

                yield return null;
                yield return null;

                Assert.That(driver.HasCpuDiscardRecord, Is.True);
                Assert.That(driver.CpuDiscardActorSeatName, Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.CpuDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.EventIndex("CpuTileDrawn"), Is.GreaterThanOrEqualTo(0));
                Assert.That(
                    driver.EventIndex("CpuTileDiscarded"),
                    Is.GreaterThan(driver.EventIndex("CpuTileDrawn")));
                Assert.That(
                    driver.EventIndex("NextTurnStarted"),
                    Is.GreaterThan(driver.EventIndex("CpuTileDiscarded")));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(3));
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_AutoDrawWinningTsumo_DeclaresWinWithoutDiscard()
        {
            using (CpuTurnGameFlowTestDriver driver =
                CpuTurnGameFlowTestDriver.CreateForCpuWinningTsumo(enableAutoDraw: true))
            {
                driver.StartNewRound();
                driver.PreparePlayer2WinningTsumoOnNextDraw();
                driver.SubscribeCpuTurnEventTrace();

                driver.RequestSelfDiscardDrawnTile();

                yield return null;
                driver.PumpDecisionCoordinator();
                yield return null;

                Assert.That(driver.IsRoundEnded, Is.True);
                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(driver.RoundResultWinnerSeatName, Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
                Assert.That(driver.HasCpuDiscardRecord, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_ControllerDrawWinningTsumo_DeclaresWinWithoutDiscard()
        {
            using (CpuTurnGameFlowTestDriver driver =
                CpuTurnGameFlowTestDriver.CreateForCpuWinningTsumo(enableAutoDraw: false))
            {
                driver.StartNewRound();
                driver.PreparePlayer2WinningTsumoOnNextDraw();
                driver.SetSelfDrawnTile("1m");
                driver.SubscribeCpuTurnEventTrace();

                driver.RequestSelfDiscardDrawnTile();

                yield return null;
                driver.PumpDecisionCoordinator();
                yield return null;

                Assert.That(driver.IsRoundEnded, Is.True);
                Assert.That(driver.IsRoundResultPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
                Assert.That(driver.RoundResultTypeName, Is.EqualTo("Win"));
                Assert.That(driver.RoundResultWinnerSeatName, Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
                Assert.That(driver.HasCpuDiscardRecord, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_NineTerminalsDecision_AutoDeclinesAndContinues()
        {
            using (CpuTurnGameFlowTestDriver driver =
                CpuTurnGameFlowTestDriver.CreateForCpuNineTerminals())
            {
                driver.StartNewRound();
                driver.PreparePlayer2NineTerminalsOnNextDraw();
                driver.SubscribeCpuTurnEventTrace();

                driver.RequestSelfDiscardDrawnTile();

                yield return null;
                Assert.That(
                    driver.TurnPhaseName,
                    Is.EqualTo("AbortiveDrawDecision"));
                driver.PumpDecisionCoordinator();
                for (int frame = 0; frame < 5; frame++)
                    yield return null;

                Assert.That(driver.IsRoundEnded, Is.False);
                Assert.That(
                    driver.HasCpuDiscardRecord,
                    Is.True,
                    $"phase={driver.TurnPhaseName}; current={driver.CurrentTurnName}; " +
                    $"discards={driver.DiscardCount}");
                Assert.That(
                    driver.CpuDiscardActorSeatName,
                    Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnPhaseName, Is.Not.EqualTo("AbortiveDrawDecision"));
                Assert.That(driver.DiscardCount, Is.EqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator RemoteHumanTurn_DoesNotStartCpuAutoDiscard()
        {
            using (CpuTurnGameFlowTestDriver driver = CpuTurnGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetPlayer2ParticipantType("RemoteHuman");

                driver.RequestSelfDiscardDrawnTile();

                yield return null;
                yield return null;

                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
                Assert.That(driver.Player2HasDrawnTile, Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_DoesNotDiscardWhileWinDecisionIsPending()
        {
            using (CpuTurnGameFlowTestDriver driver = CpuTurnGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                driver.RequestSelfDiscardDrawnTile();
                driver.BeginWinDecisionForPlayer2();

                yield return null;
                yield return null;

                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.Player2HasDrawnTile, Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator CpuTurn_DoesNotDiscardAfterRoundEnds()
        {
            using (CpuTurnGameFlowTestDriver driver = CpuTurnGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                driver.RequestSelfDiscardDrawnTile();
                driver.SetRoundEnded(true);

                yield return null;
                yield return null;

                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.Player2SeatName));
                Assert.That(driver.Player2HasDrawnTile, Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator RetryPrototype_CancelsCpuActionFromPreviousGameState()
        {
            using (CpuTurnGameFlowTestDriver driver = CpuTurnGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                object previousState = driver.CurrentStateToken;

                driver.RequestSelfDiscardDrawnTile();
                driver.RetryPrototype();

                yield return null;
                yield return null;

                Assert.That(driver.CurrentStateToken, Is.Not.SameAs(previousState));
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(1));
            }
        }
    }
}
