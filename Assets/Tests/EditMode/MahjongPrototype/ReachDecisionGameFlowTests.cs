using MahjongPrototype.Tests.TestSupport.Features.Reach;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReachDecisionGameFlowTests
    {
        [Test]
        public void DrawReachableHand_BeginsReachDecision()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("ReachDecision"));
                Assert.That(driver.ReachDiscardCandidateCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void DrawWinningHand_PrioritizesWinDecisionOverReachDecision()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawWinningHand();

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WinDecision"));
            }
        }

        [Test]
        public void RequestDeclareReach_MovesToReachDiscardSelection()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();

                driver.RequestDeclareReach();

                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("ReachDiscardSelection"));
            }
        }

        [Test]
        public void RequestCancelReachDiscardSelection_ReturnsToReachDecisionAndKeepsCandidates()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                driver.RequestDeclareReach();
                int candidateCountBefore = driver.ReachDiscardCandidateCount;
                int discardCountBefore = driver.DiscardCount;

                driver.RequestCancelReachDiscardSelection();

                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.False);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("ReachDecision"));
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(candidateCountBefore));
                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));

                driver.RequestDeclareReach();

                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
            }
        }

        [Test]
        public void RequestDeclineReach_ClearsReachDecision()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();

                driver.RequestDeclineReach();

                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.False);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));
            }
        }

        [Test]
        public void ReachDecision_RequestForceDrawSkill_RegistersEffectAndKeepsReachDecision()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                string reachDecisionSeatBefore = driver.ReachDecisionSeatName;
                int reachDecisionTurnIndexBefore = driver.ReachDecisionTurnIndex;
                int candidateCountBefore = driver.ReachDiscardCandidateCount;
                string currentTurnBefore = driver.CurrentTurnName;
                int turnIndexBefore = driver.TurnIndex;
                string handBefore = driver.HandDisplayString("East");
                string drawnTileBefore = driver.DrawnTileCodeOrNull("East");
                int wallCountBefore = driver.WallCount;
                int discardCountBefore = driver.DiscardCount;

                driver.RequestForceDrawSkill("5m");

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(1));
                Assert.That(driver.ActiveSkillEffectOwnerSeatNameAt(0), Is.EqualTo("East"));
                Assert.That(driver.ActiveSkillEffectTargetTileCodeAt(0), Is.EqualTo("5m"));
                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.False);
                Assert.That(driver.ReachDecisionSeatName, Is.EqualTo(reachDecisionSeatBefore));
                Assert.That(driver.ReachDecisionTurnIndex, Is.EqualTo(reachDecisionTurnIndexBefore));
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(candidateCountBefore));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(currentTurnBefore));
                Assert.That(driver.TurnIndex, Is.EqualTo(turnIndexBefore));
                Assert.That(driver.HandDisplayString("East"), Is.EqualTo(handBefore));
                Assert.That(driver.DrawnTileCodeOrNull("East"), Is.EqualTo(drawnTileBefore));
                Assert.That(driver.WallCount, Is.EqualTo(wallCountBefore));
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));
            }
        }

        [Test]
        public void ReachDecision_RequestForceDrawSkill_AllowsDeclareReachAndKeepsEffect()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                driver.RequestForceDrawSkill("5m");
                int candidateCountBefore = driver.ReachDiscardCandidateCount;

                driver.RequestDeclareReach();

                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("ReachDiscardSelection"));
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(candidateCountBefore));
                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(1));
                Assert.That(driver.ActiveSkillEffectTargetTileCodeAt(0), Is.EqualTo("5m"));
            }
        }

        [Test]
        public void ReachDiscardSelection_RequestForceDrawSkill_IsRejectedAndKeepsSelection()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                driver.RequestDeclareReach();
                int candidateCountBefore = driver.ReachDiscardCandidateCount;
                string currentTurnBefore = driver.CurrentTurnName;
                int turnIndexBefore = driver.TurnIndex;
                string handBefore = driver.HandDisplayString("East");
                string drawnTileBefore = driver.DrawnTileCodeOrNull("East");
                int wallCountBefore = driver.WallCount;
                int discardCountBefore = driver.DiscardCount;

                driver.RequestForceDrawSkill("5m");

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(0));
                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(candidateCountBefore));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(currentTurnBefore));
                Assert.That(driver.TurnIndex, Is.EqualTo(turnIndexBefore));
                Assert.That(driver.HandDisplayString("East"), Is.EqualTo(handBefore));
                Assert.That(driver.DrawnTileCodeOrNull("East"), Is.EqualTo(drawnTileBefore));
                Assert.That(driver.WallCount, Is.EqualTo(wallCountBefore));
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));
            }
        }

        [Test]
        public void ReachDecision_RequestDraw_DoesNotChangeState()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                string reachDecisionSeatBefore = driver.ReachDecisionSeatName;
                int reachDecisionTurnIndexBefore = driver.ReachDecisionTurnIndex;
                int candidateCountBefore = driver.ReachDiscardCandidateCount;
                string currentTurnBefore = driver.CurrentTurnName;
                int turnIndexBefore = driver.TurnIndex;
                string handBefore = driver.HandDisplayString("East");
                string drawnTileBefore = driver.DrawnTileCodeOrNull("East");
                int wallCountBefore = driver.WallCount;
                int discardCountBefore = driver.DiscardCount;

                driver.RequestDraw();

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(0));
                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.False);
                Assert.That(driver.ReachDecisionSeatName, Is.EqualTo(reachDecisionSeatBefore));
                Assert.That(driver.ReachDecisionTurnIndex, Is.EqualTo(reachDecisionTurnIndexBefore));
                Assert.That(driver.ReachDiscardCandidateCount, Is.EqualTo(candidateCountBefore));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(currentTurnBefore));
                Assert.That(driver.TurnIndex, Is.EqualTo(turnIndexBefore));
                Assert.That(driver.HandDisplayString("East"), Is.EqualTo(handBefore));
                Assert.That(driver.DrawnTileCodeOrNull("East"), Is.EqualTo(drawnTileBefore));
                Assert.That(driver.WallCount, Is.EqualTo(wallCountBefore));
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));
            }
        }

        [Test]
        public void ReachDiscardSelection_RejectsNonCandidateHandDiscard()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                driver.RequestDeclareReach();
                int discardCountBefore = driver.DiscardCount;

                driver.RequestDiscard(0);

                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));
                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
            }
        }

        [Test]
        public void ReachDiscardSelection_DeclaresReachAfterCandidateHandDiscard()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.RequestDeclareReach();

                driver.RequestDiscard(12);

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.ReachDeclaredTurnIndex("East"), Is.EqualTo(1));
                Assert.That(driver.IsReachDecisionPending, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.False);
                Assert.That(driver.DiscardCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void ReachDiscardSelection_DeclaresReachAfterDrawnTileDiscardCandidate()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                driver.RequestDeclareReach();

                driver.RequestDiscardDrawnTile();

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.DiscardSourceNameAt(0), Is.EqualTo("DrawnTile"));
                Assert.That(driver.DiscardTileCodeAt(0), Is.EqualTo("6m"));
            }
        }

        [Test]
        public void ReachDiscardSelection_DoesNotAutoDiscardBeforeReachConfirmed()
        {
            using (ReachDecisionGameFlowTestDriver driver =
                ReachDecisionGameFlowTestDriver.Create())
            {
                driver.DrawReachableHand();
                driver.RequestDeclareReach();
                int discardCountBefore = driver.DiscardCount;

                bool shouldAutoDiscard = driver.ShouldAutoDiscardDrawnTileAfterDraw("East");
                driver.TryAutoDiscardDrawnTileAfterDraw("East");

                Assert.That(shouldAutoDiscard, Is.False);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.True);
                Assert.That(driver.IsReachDeclared("East"), Is.False);
                Assert.That(driver.HasDrawnTile("East"), Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));

                driver.RequestCancelReachDiscardSelection();

                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.IsReachDiscardSelectionPending, Is.False);
                Assert.That(driver.IsReachDeclared("East"), Is.False);
            }
        }
    }
}
