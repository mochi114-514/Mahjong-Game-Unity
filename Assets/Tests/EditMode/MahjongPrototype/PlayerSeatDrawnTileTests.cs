using MahjongPrototype.Tests.TestSupport.Features.Hand;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerSeatDrawnTileTests
    {
        [Test]
        public void DrawnTile_CanBeCommittedToHand()
        {
            PlayerSeatDrawnTileTestDriver driver = PlayerSeatDrawnTileTestDriver.Create();

            driver.SetDrawnTile("9m");

            bool committed = driver.CommitDrawnTileToHand();

            Assert.That(committed, Is.True);
            Assert.That(driver.HasDrawnTile, Is.False);
            Assert.That(driver.HandDisplayString, Is.EqualTo("9m"));
        }

        [Test]
        public void DrawnTile_CanBeTakenAndCleared()
        {
            PlayerSeatDrawnTileTestDriver driver = PlayerSeatDrawnTileTestDriver.Create();
            driver.SetDrawnTile("E");

            bool taken = driver.TryTakeDrawnTile(out string tile);

            Assert.That(taken, Is.True);
            Assert.That(tile, Is.EqualTo("E"));
            Assert.That(driver.HasDrawnTile, Is.False);
        }

        [Test]
        public void DrawnTile_CanBeClearedWithoutChangingHand()
        {
            PlayerSeatDrawnTileTestDriver driver = PlayerSeatDrawnTileTestDriver.Create();
            driver.AddHandTile("1m");
            driver.SetDrawnTile("E");

            driver.ClearDrawnTile();

            Assert.That(driver.HasDrawnTile, Is.False);
            Assert.That(driver.HandDisplayString, Is.EqualTo("1m"));
        }

        [Test]
        public void SortingHand_DoesNotChangeDrawnTile()
        {
            PlayerSeatDrawnTileTestDriver driver = PlayerSeatDrawnTileTestDriver.Create();
            driver.AddHandTile("9m");
            driver.AddHandTile("1m");
            driver.SetDrawnTile("C");

            driver.SortHand();

            Assert.That(driver.HandDisplayString, Is.EqualTo("1m 9m"));
            Assert.That(driver.DrawnTileCode, Is.EqualTo("C"));
        }
    }
}
