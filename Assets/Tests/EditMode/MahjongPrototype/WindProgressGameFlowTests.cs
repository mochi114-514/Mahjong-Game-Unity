using MahjongPrototype.Tests.TestSupport.Features.WindProgress;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WindProgressGameFlowTests
    {
        [Test]
        public void MahjongGameFlow_RotateSeatForNextRoundMapsAllSeatWinds()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                Assert.That(driver.RotateSeatForNextRound("East"), Is.EqualTo("North"));
                Assert.That(driver.RotateSeatForNextRound("South"), Is.EqualTo("East"));
                Assert.That(driver.RotateSeatForNextRound("West"), Is.EqualTo("South"));
                Assert.That(driver.RotateSeatForNextRound("North"), Is.EqualTo("West"));
            }
        }

        [Test]
        public void MahjongGameFlow_StartNewRoundUsesEastOne()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                AssertCurrentWindProgress(driver, "East", 1);
                Assert.That(driver.SelfSeatName, Is.EqualTo("East"));
            }
        }

        [Test]
        public void MahjongGameFlow_WallEmptyStartsNextRound()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                driver.EndRound("WallEmpty");

                AssertRoundResultPending(driver, "ExhaustiveDraw");
                AssertCurrentWindProgress(driver, "East", 1);

                driver.AdvanceFromRoundResult();

                AssertCurrentWindProgress(driver, "East", 2);
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));
                Assert.That(driver.IsRoundEnded, Is.False);
                Assert.That(driver.IsRoundResultPending, Is.False);
            }
        }

        [Test]
        public void MahjongGameFlow_WallEmptyContinuouslyRotatesSelfSeatThroughEastAndSouthRounds()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                AssertCurrentWindProgress(driver, "East", 1);
                Assert.That(driver.SelfSeatName, Is.EqualTo("East"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "East", 2);
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "East", 3);
                Assert.That(driver.SelfSeatName, Is.EqualTo("West"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "East", 4);
                Assert.That(driver.SelfSeatName, Is.EqualTo("South"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "South", 1);
                Assert.That(driver.SelfSeatName, Is.EqualTo("East"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "South", 2);
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "South", 3);
                Assert.That(driver.SelfSeatName, Is.EqualTo("West"));

                EndWallEmptyAndAdvance(driver);
                AssertCurrentWindProgress(driver, "South", 4);
                Assert.That(driver.SelfSeatName, Is.EqualTo("South"));
            }
        }

        [Test]
        public void MahjongGameFlow_NextRoundRotatesAllPlayersTogether()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create(participantCount: 4))
            {
                driver.StartNewRound();

                Assert.That(driver.SeatByPlayerIdName("Player1"), Is.EqualTo("East"));
                Assert.That(driver.SeatByPlayerIdName("Player2"), Is.EqualTo("South"));
                Assert.That(driver.SeatByPlayerIdName("Player3"), Is.EqualTo("West"));
                Assert.That(driver.SeatByPlayerIdName("Player4"), Is.EqualTo("North"));

                EndWallEmptyAndAdvance(driver);

                Assert.That(driver.SeatByPlayerIdName("Player1"), Is.EqualTo("North"));
                Assert.That(driver.SeatByPlayerIdName("Player2"), Is.EqualTo("East"));
                Assert.That(driver.SeatByPlayerIdName("Player3"), Is.EqualTo("South"));
                Assert.That(driver.SeatByPlayerIdName("Player4"), Is.EqualTo("West"));
            }
        }

        [Test]
        public void MahjongGameFlow_WallEmptyAfterSouthFourStaysRoundEnded()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartRound("South", 4, "South");

                driver.EndRound("WallEmpty");

                AssertRoundResultPending(driver, "ExhaustiveDraw");
                AssertCurrentWindProgress(driver, "South", 4);
                Assert.That(driver.SelfSeatName, Is.EqualTo("South"));
                Assert.That(driver.IsRoundEnded, Is.True);
                Assert.That(driver.IsGameEnded, Is.False);

                driver.AdvanceFromRoundResult();

                Assert.That(driver.IsGameEnded, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("GameEnded"));
            }
        }

        [Test]
        public void MahjongGameFlow_TsumoWinStartsNextRound()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.BeginWinDecision("East", "Tsumo", null);

                driver.DeclareWin();

                AssertRoundResultPending(driver, "Win");
                AssertCurrentWindProgress(driver, "East", 1);

                driver.AdvanceFromRoundResult();

                AssertCurrentWindProgress(driver, "East", 2);
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));
                Assert.That(driver.IsRoundEnded, Is.False);
            }
        }

        [Test]
        public void MahjongGameFlow_RonWinStartsNextRound()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.BeginWinDecision("East", "Ron", "South");

                driver.DeclareWin();

                AssertRoundResultPending(driver, "Win");
                AssertCurrentWindProgress(driver, "East", 1);

                driver.AdvanceFromRoundResult();

                AssertCurrentWindProgress(driver, "East", 2);
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));
                Assert.That(driver.IsRoundEnded, Is.False);
            }
        }

        [Test]
        public void MahjongGameFlow_WinAfterSouthFourStaysRoundEnded()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartRound("South", 4, "South");
                driver.BeginWinDecision("East", "Tsumo", null);

                driver.DeclareWin();

                AssertRoundResultPending(driver, "Win");
                AssertCurrentWindProgress(driver, "South", 4);
                Assert.That(driver.SelfSeatName, Is.EqualTo("South"));
                Assert.That(driver.IsRoundEnded, Is.True);
                Assert.That(driver.IsGameEnded, Is.False);

                driver.AdvanceFromRoundResult();

                Assert.That(driver.IsGameEnded, Is.True);
                Assert.That(driver.TurnPhaseName, Is.EqualTo("GameEnded"));
            }
        }

        [Test]
        public void MahjongGameFlow_DeclineWinDoesNotStartNextRound()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                driver.BeginWinDecision("East", "Tsumo", null);

                driver.DeclineWin();

                AssertCurrentWindProgress(driver, "East", 1);
                Assert.That(driver.SelfSeatName, Is.EqualTo("East"));
                Assert.That(driver.IsRoundEnded, Is.False);
            }
        }

        [Test]
        public void MahjongGameFlow_StartNewRoundAfterProgressUsesInitialFixedSelfSeatAgain()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();
                EndWallEmptyAndAdvance(driver);
                Assert.That(driver.SelfSeatName, Is.EqualTo("North"));

                driver.StartNewRound();

                AssertCurrentWindProgress(driver, "East", 1);
                Assert.That(driver.SelfSeatName, Is.EqualTo("East"));
            }
        }

        private static void AssertCurrentWindProgress(
            WindProgressGameFlowTestDriver driver,
            string expectedRoundWind,
            int expectedHandNumber)
        {
            Assert.That(driver.CurrentRoundWindName, Is.EqualTo(expectedRoundWind));
            Assert.That(driver.CurrentHandNumber, Is.EqualTo(expectedHandNumber));
        }

        private static void EndWallEmptyAndAdvance(WindProgressGameFlowTestDriver driver)
        {
            driver.EndRound("WallEmpty");
            AssertRoundResultPending(driver, "ExhaustiveDraw");
            driver.AdvanceFromRoundResult();
        }

        private static void AssertRoundResultPending(
            WindProgressGameFlowTestDriver driver,
            string expectedTypeName)
        {
            Assert.That(driver.IsRoundEnded, Is.True);
            Assert.That(driver.IsRoundResultPending, Is.True);
            Assert.That(driver.TurnPhaseName, Is.EqualTo("RoundResult"));
            Assert.That(driver.RoundResultTypeName, Is.EqualTo(expectedTypeName));
        }
    }
}
