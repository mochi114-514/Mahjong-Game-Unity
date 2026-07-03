using MahjongPrototype.Tests.TestSupport.Features.WindProgress;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WindProgressGameFlowTests
    {
        [Test]
        public void MahjongGameFlow_StartNewRoundUsesEastOne()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartNewRound();

                AssertCurrentWindProgress(driver, "East", 1);
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

                AssertCurrentWindProgress(driver, "East", 2);
                Assert.That(driver.IsRoundEnded, Is.False);
            }
        }

        [Test]
        public void MahjongGameFlow_WallEmptyAfterSouthFourStaysRoundEnded()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartRound("South", 4);

                driver.EndRound("WallEmpty");

                AssertCurrentWindProgress(driver, "South", 4);
                Assert.That(driver.IsRoundEnded, Is.True);
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

                AssertCurrentWindProgress(driver, "East", 2);
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

                AssertCurrentWindProgress(driver, "East", 2);
                Assert.That(driver.IsRoundEnded, Is.False);
            }
        }

        [Test]
        public void MahjongGameFlow_WinAfterSouthFourStaysRoundEnded()
        {
            using (WindProgressGameFlowTestDriver driver =
                WindProgressGameFlowTestDriver.Create())
            {
                driver.StartRound("South", 4);
                driver.BeginWinDecision("East", "Tsumo", null);

                driver.DeclareWin();

                AssertCurrentWindProgress(driver, "South", 4);
                Assert.That(driver.IsRoundEnded, Is.True);
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
                Assert.That(driver.IsRoundEnded, Is.False);
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
    }
}
