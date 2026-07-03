using MahjongPrototype.Tests.TestSupport.Features.Reach;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReachTurnAutomationPolicyGameFlowTests
    {
        [Test]
        public void ReachDeclared_StartTurn_UsesCommonAutoDrawPolicy()
        {
            using (ReachTurnAutomationPolicyGameFlowTestDriver driver =
                ReachTurnAutomationPolicyGameFlowTestDriver.Create(2, false))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);

                object policy = driver.BuildTurnAutomationPolicy("East");
                Assert.That(driver.PolicyIsCpu(policy), Is.False);
                Assert.That(driver.PolicyAutoDrawAtTurnStart(policy), Is.True);
                Assert.That(driver.PolicyAutoDiscardDrawnTileAfterDraw(policy), Is.True);
                Assert.That(driver.PolicyUseCpuController(policy), Is.False);

                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("9m"));
            }
        }

        [Test]
        public void NormalLocalHuman_EnableAutoDrawFalse_DoesNotAutoDraw()
        {
            using (ReachTurnAutomationPolicyGameFlowTestDriver driver =
                ReachTurnAutomationPolicyGameFlowTestDriver.Create(1, false))
            {
                driver.StartNewRound();

                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.HasDrawnTile("East"), Is.False);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDraw"));
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void NormalLocalHuman_EnableAutoDrawTrue_AutoDrawsButDoesNotAutoDiscard()
        {
            using (ReachTurnAutomationPolicyGameFlowTestDriver driver =
                ReachTurnAutomationPolicyGameFlowTestDriver.Create(1, true))
            {
                driver.StartNewRound();

                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.HasDrawnTile("East"), Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void CpuSeat_TurnAutomationPolicy_UsesCpuController()
        {
            using (ReachTurnAutomationPolicyGameFlowTestDriver driver =
                ReachTurnAutomationPolicyGameFlowTestDriver.Create(2, false))
            {
                driver.StartNewRound();

                object policy = driver.BuildTurnAutomationPolicy("West");

                Assert.That(driver.PolicyIsCpu(policy), Is.True);
                Assert.That(driver.PolicyAutoDrawAtTurnStart(policy), Is.False);
                Assert.That(driver.PolicyAutoDiscardDrawnTileAfterDraw(policy), Is.False);
                Assert.That(driver.PolicyUseCpuController(policy), Is.True);
            }
        }
    }
}
