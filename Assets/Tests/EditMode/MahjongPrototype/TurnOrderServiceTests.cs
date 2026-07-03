using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class TurnOrderServiceTests
    {
        [Test]
        public void GetNextSeat_ReturnsSameSeat_WhenOnlyOneSeatIsActive()
        {
            TurnOrderServiceTestDriver driver = TurnOrderServiceTestDriver.Create();

            string result = driver.GetNextSeat("East", "East");

            Assert.That(result, Is.EqualTo("East"));
        }

        [TestCase("East", "South")]
        [TestCase("South", "West")]
        [TestCase("West", "North")]
        [TestCase("North", "East")]
        public void GetNextSeat_ReturnsNextSeat_WhenMultipleSeatsAreActive(
            string currentTurn,
            string expectedSeat)
        {
            TurnOrderServiceTestDriver driver = TurnOrderServiceTestDriver.Create();

            string result = driver.GetNextSeat(currentTurn, "East", "South", "West", "North");

            Assert.That(result, Is.EqualTo(expectedSeat));
        }

        [Test]
        public void GetNextSeat_ReturnsFirstActiveSeat_WhenCurrentTurnIsNotActive()
        {
            TurnOrderServiceTestDriver driver = TurnOrderServiceTestDriver.Create();

            string result = driver.GetNextSeat("West", "East", "South");

            Assert.That(result, Is.EqualTo("East"));
        }

        [Test]
        public void GetNextSeat_ReturnsEast_WhenActiveSeatsIsEmpty()
        {
            TurnOrderServiceTestDriver driver = TurnOrderServiceTestDriver.Create();

            string result = driver.GetNextSeat("East");

            Assert.That(result, Is.EqualTo("East"));
        }

        [Test]
        public void GetNextSeat_ReturnsEast_WhenActiveSeatsIsNull()
        {
            TurnOrderServiceTestDriver driver = TurnOrderServiceTestDriver.Create();

            string result = driver.GetNextSeatWithNullActiveSeats("East");

            Assert.That(result, Is.EqualTo("East"));
        }
    }
}
