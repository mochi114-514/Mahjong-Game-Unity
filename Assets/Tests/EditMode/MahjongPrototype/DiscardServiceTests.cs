using MahjongPrototype.Tests.TestSupport.Features.Discard;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class DiscardServiceTests
    {
        [Test]
        public void DiscardTile_RecordsHandSourceAndExistingFields()
        {
            DiscardServiceTestDriver driver = DiscardServiceTestDriver.Create();
            object gameState = driver.CreateGameState("East");
            driver.AddHandTile(gameState, "East", "1m");

            object result = driver.DiscardHandTile(gameState, "East", 0);
            object record = driver.RecordOf(result);

            Assert.That(driver.RecordSource(record), Is.EqualTo("Hand"));
            Assert.That(driver.RecordActorSeat(record), Is.EqualTo("East"));
            Assert.That(driver.RecordTile(record), Is.EqualTo("1m"));
            Assert.That(driver.RecordTurnIndex(record), Is.EqualTo(1));
        }

        [Test]
        public void DiscardDrawnTile_RecordsDrawnTileSourceAndExistingFields()
        {
            DiscardServiceTestDriver driver = DiscardServiceTestDriver.Create();
            object gameState = driver.CreateGameState("East");
            driver.SetDrawnTile(gameState, "East", "2m");

            object result = driver.DiscardDrawnTile(gameState, "East");
            object record = driver.RecordOf(result);

            Assert.That(driver.RecordSource(record), Is.EqualTo("DrawnTile"));
            Assert.That(driver.RecordActorSeat(record), Is.EqualTo("East"));
            Assert.That(driver.RecordTile(record), Is.EqualTo("2m"));
            Assert.That(driver.RecordTurnIndex(record), Is.EqualTo(1));
        }

        [Test]
        public void MultipleDiscards_PreserveOrder()
        {
            DiscardServiceTestDriver driver = DiscardServiceTestDriver.Create();
            object gameState = driver.CreateGameState("East");
            driver.AddHandTile(gameState, "East", "1m");
            driver.AddHandTile(gameState, "East", "2m");

            driver.DiscardHandTile(gameState, "East", 0);
            driver.DiscardHandTile(gameState, "East", 0);

            Assert.That(driver.DiscardCount(gameState), Is.EqualTo(2));
            Assert.That(driver.RecordTile(driver.DiscardAt(gameState, 0)), Is.EqualTo("1m"));
            Assert.That(driver.RecordTile(driver.DiscardAt(gameState, 1)), Is.EqualTo("2m"));
        }
    }
}
