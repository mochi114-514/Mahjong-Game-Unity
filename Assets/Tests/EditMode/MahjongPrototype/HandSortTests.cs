using MahjongPrototype.Tests.TestSupport.Features.Hand;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandSortTests
    {
        [Test]
        public void SortByTypeIndex_SortsTilesInNaturalMahjongOrder()
        {
            HandSortTestDriver driver = HandSortTestDriver.Create();
            object hand = driver.CreateHand("C 9m 1m E 3p 2s 1p F 9s 5m S W N P 2m 1s");

            driver.SortByTypeIndex(hand);

            Assert.That(
                driver.DisplayString(hand),
                Is.EqualTo("1m 2m 5m 9m 1p 3p 1s 2s 9s E S W N P F C"));
        }

        [Test]
        public void SortByTypeIndex_MovesInvalidTilesToEnd()
        {
            HandSortTestDriver driver = HandSortTestDriver.Create();
            object hand = driver.CreateEmptyHand();
            driver.AddInvalidTile(hand);
            driver.AddTile(hand, "C");
            driver.AddTile(hand, "1m");

            driver.SortByTypeIndex(hand);

            Assert.That(driver.TileCount(hand), Is.EqualTo(3));
            Assert.That(driver.TileTypeIndexAt(hand, 0), Is.EqualTo(0));
            Assert.That(driver.TileTypeIndexAt(hand, 1), Is.EqualTo(33));
            Assert.That(driver.TileTypeIndexAt(hand, 2), Is.EqualTo(-1));
        }

        [Test]
        public void SortByTypeIndex_PreservesTileCountAndDuplicates()
        {
            HandSortTestDriver driver = HandSortTestDriver.Create();
            object hand = driver.CreateHand("3m 1m 3m E 1m E");

            driver.SortByTypeIndex(hand);

            Assert.That(driver.TileCount(hand), Is.EqualTo(6));
            Assert.That(driver.DisplayString(hand), Is.EqualTo("1m 1m 3m 3m E E"));
        }
    }
}
