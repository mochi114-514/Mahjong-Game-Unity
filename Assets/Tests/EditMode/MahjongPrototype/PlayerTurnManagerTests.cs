using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerTurnManagerTests
    {
        [Test]
        public void InitializeRound_SetsCurrentTurnAndTurnIndex()
        {
            PlayerTurnManagerTestDriver driver =
                PlayerTurnManagerTestDriver.Create("East", "South");

            driver.InitializeRound("South");

            Assert.That(driver.CurrentTurnName, Is.EqualTo("South"));
            Assert.That(driver.TurnIndex, Is.EqualTo(1));
            Assert.That(driver.TurnPhaseName, Is.EqualTo("WaitingForDraw"));
        }

        [Test]
        public void EndTurnAndSelectNext_AdvancesSeatAndTurnIndex()
        {
            PlayerTurnManagerTestDriver driver =
                PlayerTurnManagerTestDriver.Create("East", "South", "West", "North");
            driver.InitializeRound("East");

            string nextSeat = driver.EndTurnAndSelectNext("East", "South", "West", "North");

            Assert.That(nextSeat, Is.EqualTo("South"));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("South"));
            Assert.That(driver.TurnIndex, Is.EqualTo(2));
        }

        [Test]
        public void EndTurnAndSelectNext_ReturnsSameSeat_WhenOnlyEastIsActive()
        {
            PlayerTurnManagerTestDriver driver = PlayerTurnManagerTestDriver.Create("East");
            driver.InitializeRound("East");

            string nextSeat = driver.EndTurnAndSelectNext("East");

            Assert.That(nextSeat, Is.EqualTo("East"));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
            Assert.That(driver.TurnIndex, Is.EqualTo(2));
        }
    }
}
