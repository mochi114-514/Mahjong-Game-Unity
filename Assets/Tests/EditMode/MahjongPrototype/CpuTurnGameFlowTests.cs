using System.Collections;
using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class CpuTurnGameFlowTests
    {
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
