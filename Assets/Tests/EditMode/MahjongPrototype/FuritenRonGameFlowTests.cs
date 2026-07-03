using MahjongPrototype.Tests.TestSupport.Features.Furiten;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenRonGameFlowTests
    {
        [Test]
        public void RonDecision_NonFuritenCandidate_CanRon()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
                Assert.That(driver.WinSourceSeat, Is.EqualTo("West"));
            }
        }

        [Test]
        public void RonDecision_OwnDiscardedWait_DoesNotStartRonDecisionAndAdvancesTurn()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("East", "5m", 0);
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDecision_OtherSeatDiscardedWait_DoesNotCauseFuriten()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("West", "5m", 0);
                driver.SetDrawnTile("West", "5m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
            }
        }

        [Test]
        public void RonDecision_MultiWaitWithOneOwnDiscardedWait_BlocksDifferentWaitRon()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.RonMultiWait());
                driver.AddDiscard("East", "3m", 0);
                Assert.That(driver.IsSeatDiscardFuriten("East"), Is.True);
                Assert.That(driver.IsSeatFuriten("East"), Is.True);
                driver.SetDrawnTile("West", "6m");
                driver.SetCurrentTurn("West");

                bool discarded = driver.DiscardDrawnTile("West");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("East"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void RonDecision_FirstCandidateFuriten_ContinuesToLaterCandidate()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(3))
            {
                driver.StartRound();
                driver.SetSeatParticipantType("South", "LocalHuman");
                driver.SetSeatParticipantType("West", "LocalHuman");
                driver.SetHand("South", FuritenTestHands.SimpleFiveManWait());
                driver.SetHand("West", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("South", "5m", 0);
                driver.SetDrawnTile("East", "5m");
                driver.SetCurrentTurn("East");

                bool discarded = driver.DiscardDrawnTile("East");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("West"));
                Assert.That(driver.WinDecisionType, Is.EqualTo("Ron"));
            }
        }

        [Test]
        public void RonDecision_AllCandidatesFuriten_AdvancesTurnOnce()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(3))
            {
                driver.StartRound();
                driver.SetSeatParticipantType("South", "LocalHuman");
                driver.SetSeatParticipantType("West", "LocalHuman");
                driver.SetHand("South", FuritenTestHands.SimpleFiveManWait());
                driver.SetHand("West", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("South", "5m", 0);
                driver.AddDiscard("West", "5m", 0);
                driver.SetDrawnTile("East", "5m");
                driver.SetCurrentTurn("East");

                bool discarded = driver.DiscardDrawnTile("East");

                Assert.That(discarded, Is.True);
                Assert.That(driver.IsWinDecisionPending, Is.False);
                Assert.That(driver.CurrentTurn, Is.EqualTo("South"));
                Assert.That(driver.TurnIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void TsumoDecision_OwnDiscardFuriten_DoesNotBlockTsumo()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(1))
            {
                driver.StartRound();
                driver.SetHand("East", FuritenTestHands.SimpleFiveManWait());
                driver.AddDiscard("East", "5m", 0);

                driver.DrawSelfTile("5m");

                Assert.That(driver.IsWinDecisionPending, Is.True);
                Assert.That(driver.WinDecisionType, Is.EqualTo("Tsumo"));
                Assert.That(driver.WinDecisionSeat, Is.EqualTo("East"));
            }
        }

        [Test]
        public void EvaluateAllFuriten_ReturnsOccupiedSeatsWithoutChangingGameState()
        {
            using (FuritenRonTestDriver driver = FuritenRonTestDriver.Create(2))
            {
                driver.StartRound();
                string before = driver.GameStateSnapshot();

                object resultSet = driver.EvaluateAllFuriten();

                Assert.That(driver.ResultCount(resultSet), Is.EqualTo(2));
                Assert.That(driver.TryGetSeatResult(resultSet, "East", out _), Is.True);
                Assert.That(driver.TryGetSeatResult(resultSet, "West", out _), Is.True);
                Assert.That(driver.TryGetSeatResult(resultSet, "South", out _), Is.False);
                Assert.That(driver.GameStateSnapshot(), Is.EqualTo(before));
            }
        }
    }
}
