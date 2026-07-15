using MahjongPrototype.Tests.TestSupport.Features.Reach;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReachAutoDiscardGameFlowTests
    {
        [Test]
        public void ReachDeclared_RejectsNormalHandDiscard()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2, 0.05f))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithDrawnTileDiscard();
                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");
                int discardCountBefore = driver.DiscardCount;

                driver.RequestDiscard(0);

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBefore));
            }
        }

        [Test]
        public void ReachDeclared_AutoDrawsAndAutoDiscards()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                int discardCountBeforeWestTurnEnds = driver.DiscardCount;
                int westTurnIndex = driver.TurnIndex;

                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));

                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.HasDrawnTile("East"), Is.False);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeWestTurnEnds + 2));
                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("9m"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(driver.TurnIndex, Is.GreaterThan(westTurnIndex));
            }
        }

        [Test]
        public void ReachDeclared_AutoDiscardDelay_HoldsDrawnTileBeforeDiscard()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2, 0.05f))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                int discardCountBeforeWestTurnEnds = driver.DiscardCount;

                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.HasDrawnTile("East"), Is.True);
                Assert.That(driver.DrawnTileCode("East"), Is.EqualTo("9m"));
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeWestTurnEnds + 1));

                object routine = driver.BeginAutoDiscardRoutine("East");
                Assert.That(driver.MoveNext(routine), Is.True);
                Assert.That(driver.CurrentYieldTypeName(routine), Is.EqualTo("Wait" + "ForSeconds"));
                Assert.That(driver.HasDrawnTile("East"), Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeWestTurnEnds + 1));

                Assert.That(driver.MoveNext(routine), Is.False);

                Assert.That(driver.HasDrawnTile("East"), Is.False);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeWestTurnEnds + 2));
                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("9m"));
            }
        }

        [Test]
        public void ReachDeclared_TurnStartAutoDiscardAllowsRonDecision()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.AddHandTiles(
                    "West",
                    "2m", "3m", "4m",
                    "2p", "3p", "4p",
                    "2s", "3s", "4s",
                    "6s", "7s", "8s",
                    "5m");
                driver.DeclareReachWithHandDiscard(9);
                int eastTurnIndexBeforeAutoDiscard = driver.TurnIndex + 1;

                driver.ForceDrawForSeat("East", "5m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("5m"));
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeatName, Is.EqualTo("West"));
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Ron"));
                Assert.That(driver.WinSourceSeatName, Is.EqualTo("East"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(eastTurnIndexBeforeAutoDiscard));
            }
        }

        [Test]
        public void ReachDeclared_DeclineTsumoWin_UsesAutoDiscardPolicy()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                driver.ForceDrawForSeat("East", "6m");
                driver.DrawAndDiscardForSeat("West", "C");
                int discardCountBeforeDecline = driver.DiscardCount;

                driver.RequestDeclineWin();

                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.HasDrawnTile("East"), Is.False);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeDecline + 1));
                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo("East"));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("6m"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));
            }
        }

        [Test]
        public void ReachDeclared_DrawNonWinningTile_AutoDiscardsDrawnTile()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithDrawnTileDiscard();
                int discardCountBeforeWestTurnEnds = driver.DiscardCount;
                int westTurnIndex = driver.TurnIndex;

                driver.ForceDrawForSeat("East", "9m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.HasDrawnTile("East"), Is.False);
                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeWestTurnEnds + 2));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("9m"));
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurnName, Is.EqualTo("West"));
                Assert.That(driver.TurnIndex, Is.GreaterThan(westTurnIndex));
            }
        }

        [Test]
        public void ReachDeclared_DrawWinningTile_DoesNotAutoDiscardAndShowsTsumoDecision()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.DeclareReachWithHandDiscard(12);
                int discardCountBeforeWestTurnEnds = driver.DiscardCount;

                driver.ForceDrawForSeat("East", "6m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.IsReachDeclared("East"), Is.True);
                Assert.That(driver.HasDrawnTile("East"), Is.True);
                Assert.That(driver.DrawnTileCode("East"), Is.EqualTo("6m"));
                Assert.That(
                    driver.DiscardCount,
                    Is.EqualTo(discardCountBeforeWestTurnEnds + 1));
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Tsumo"));
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WinDecision"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
            }
        }

        [Test]
        public void ReachDeclared_AutoDiscardAllowsRonDecision()
        {
            using (ReachAutoDiscardGameFlowTestDriver driver =
                ReachAutoDiscardGameFlowTestDriver.Create(2))
            {
                driver.DrawReachableHand();
                driver.SetParticipantType("West", "LocalHuman");
                driver.AddHandTiles(
                    "West",
                    "2m", "3m", "4m",
                    "2p", "3p", "4p",
                    "2s", "3s", "4s",
                    "6s", "7s", "8s",
                    "5m");

                driver.DeclareReachWithHandDiscard(9);
                int discardCountBeforeWestTurnEnds = driver.DiscardCount;
                int eastTurnIndexBeforeAutoDiscard = driver.TurnIndex + 1;

                driver.ForceDrawForSeat("East", "5m");
                driver.DrawAndDiscardForSeat("West", "C");

                Assert.That(driver.DiscardCount, Is.EqualTo(discardCountBeforeWestTurnEnds + 2));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo("5m"));
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeatName, Is.EqualTo("West"));
                Assert.That(driver.WinDecisionTypeName, Is.EqualTo("Ron"));
                Assert.That(driver.WinSourceSeatName, Is.EqualTo("East"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(eastTurnIndexBeforeAutoDiscard));
            }
        }
    }
}
