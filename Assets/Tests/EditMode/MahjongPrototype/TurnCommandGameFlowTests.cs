using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class TurnCommandGameFlowTests
    {
        [Test]
        public void GameFlow_UsesDrawnTileAsDiscardGuard()
        {
            using (TurnCommandGameFlowTestDriver driver =
                TurnCommandGameFlowTestDriver.CreateWithInitialHand())
            {
                driver.StartNewRound();

                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.False);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDraw"));

                driver.RequestDiscard(0);
                Assert.That(driver.TurnIndex, Is.EqualTo(1));
                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.False);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDraw"));

                driver.RequestDraw();
                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDiscard"));

                driver.RequestDiscard(0);

                Assert.That(driver.CurrentPlayerHasDrawnTile, Is.False);
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
                Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDraw"));
            }
        }

        [Test]
        public void AutoSortToggle_DuringOpponentTurn_SortsOnlySelfHand()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.AddUnsortedHandsToSelfAndOpponent();
                driver.SetCurrentTurnToOpponent();

                driver.RequestSetAutoSortEnabled(true);

                Assert.That(driver.SelfHandDisplay, Is.EqualTo("1m 9m"));
                Assert.That(driver.OpponentHandDisplay, Is.EqualTo("9p 1p"));
            }
        }

        [Test]
        public void InitialDealAutoSort_SortsOnlySelfHand()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.AddUnsortedHandsToSelfAndOpponent();
                driver.SetAutoSortEnabled(true);

                driver.DealInitialHands();

                Assert.That(driver.SelfHandDisplay, Is.EqualTo("1m 9m"));
                Assert.That(driver.OpponentHandDisplay, Is.EqualTo("9p 1p"));
            }
        }

        [Test]
        public void AutoSortAfterHandChange_IgnoresOpponentSeat()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.AddUnsortedHandsToSelfAndOpponent();
                driver.SetAutoSortEnabled(true);

                driver.ApplyAutoSortForOpponentThenSelf();

                Assert.That(driver.SelfHandDisplay, Is.EqualTo("1m 9m"));
                Assert.That(driver.OpponentHandDisplay, Is.EqualTo("9p 1p"));
            }
        }

        [Test]
        public void RequestDraw_DuringOpponentTurn_DoesNotDraw()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetCurrentTurnToOpponent();
                int wallCount = driver.WallCount;

                driver.RequestDraw();

                Assert.That(driver.WallCount, Is.EqualTo(wallCount));
                Assert.That(driver.OpponentHasDrawnTile, Is.False);
            }
        }

        [Test]
        public void RequestDiscard_DuringOpponentTurn_DoesNotDiscard()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.AddOpponentHandTiles("9p");
                driver.SetOpponentDrawnTile("1p");
                driver.SetCurrentTurnToOpponent();

                driver.RequestDiscard(0);

                Assert.That(driver.OpponentHandCount, Is.EqualTo(1));
                Assert.That(driver.OpponentHasDrawnTile, Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void RequestDiscardDrawnTile_DuringOpponentTurn_DoesNotDiscard()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetOpponentDrawnTile("1p");
                driver.SetCurrentTurnToOpponent();

                driver.RequestDiscardDrawnTile();

                Assert.That(driver.OpponentHasDrawnTile, Is.True);
                Assert.That(driver.DiscardCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void RequestForceDrawSkill_DuringOpponentTurn_DoesNotActivateSkill()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetCurrentTurnToOpponent();

                driver.RequestForceDrawSkill("5m");

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void RequestForceDrawSkill_DuringSelfTurn_UsesSelfSeatAsOwner()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                driver.RequestForceDrawSkill("5m");

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(1));
                Assert.That(driver.ActiveSkillEffectOwnerSeatNameAt(0), Is.EqualTo(driver.SelfSeatName));
            }
        }

        [Test]
        public void SeatSpecificDrawAndDiscardApis_UseActorSeatAndAdvanceTurn()
        {
            using (TurnCommandGameFlowTestDriver driver = TurnCommandGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.SetCurrentTurnToOpponent();

                bool drew = driver.TryDrawForOpponentSeat();
                string drawnTile = driver.OpponentDrawnTileCodeOrNull;
                bool discarded = driver.TryDiscardOpponentDrawnTile();

                Assert.That(drew, Is.True);
                Assert.That(discarded, Is.True);
                Assert.That(driver.LastDiscardActorSeatName, Is.EqualTo(driver.OpponentSeatName));
                Assert.That(driver.LastDiscardTileCode, Is.EqualTo(drawnTile));
                Assert.That(driver.LastDiscardSourceName, Is.EqualTo("DrawnTile"));
                Assert.That(driver.CurrentTurnName, Is.EqualTo(driver.SelfSeatName));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }
    }
}
