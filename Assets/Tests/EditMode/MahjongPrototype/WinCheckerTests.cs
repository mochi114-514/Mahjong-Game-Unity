using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinCheckerTests
    {
        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m 5m")]
        [TestCase("1m 1m 1m 2m 2m 2m 3p 4p 5p C C C 9s 9s")]
        public void CanWinStandardHand_ReturnsTrueForStandardHand(string handText)
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            Assert.That(driver.CanWinStandardHand(handText), Is.True);
        }

        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m")]
        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s E S W 5m 5m")]
        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s P F C 5m 5m")]
        [TestCase("8m 9m 1p 3p 4p 5p 2s 3s 4s E E E 5m 5m")]
        [TestCase("1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E S")]
        [TestCase("1m 2m 3m 4m 5m 6m 2p 3p 4p E E E 5m 7p")]
        public void CanWinStandardHand_ReturnsFalseForNonWinningHand(string handText)
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            Assert.That(driver.CanWinStandardHand(handText), Is.False);
        }

        [Test]
        public void CanWinWithTile_CompletesStandardHand()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            Assert.That(
                driver.CanWinWithTile(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C"),
                Is.True);
        }

        [Test]
        public void CheckWinWithTile_ReturnsStandardForStandardHand()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckWinWithTile(
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                "C");

            AssertWinCheckResult(driver, result, true, "Standard");
            Assert.That(
                driver.CanWinWithTile(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C"),
                Is.True);
        }

        [Test]
        public void CheckWinWithTile_ReturnsSevenPairsForSevenPairs()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckWinWithTile(
                "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                "C");

            AssertWinCheckResult(driver, result, true, "SevenPairs");
            Assert.That(
                driver.CanWinWithTile(
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C"),
                Is.True);
        }

        [Test]
        public void CheckWinWithTile_ReturnsThirteenOrphansForThirteenWait()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckWinWithTile(
                "1m 9m 1p 9p 1s 9s E S W N P F C",
                "E");

            AssertWinCheckResult(driver, result, true, "ThirteenOrphans");
            Assert.That(
                driver.CanWinWithTile(
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E"),
                Is.True);
        }

        [Test]
        public void CheckWinWithTile_ReturnsThirteenOrphansForSingleWait()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckWinWithTile(
                "1m 9m 1p 9p 1s 9s E E S W N P F",
                "C");

            AssertWinCheckResult(driver, result, true, "ThirteenOrphans");
            Assert.That(
                driver.CanWinWithTile(
                    "1m 9m 1p 9p 1s 9s E E S W N P F",
                    "C"),
                Is.True);
        }

        [Test]
        public void CheckCompletedHand_ReturnsNoneForNonWinningHand()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckCompletedHand(
                "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E S");

            AssertWinCheckResult(driver, result, false, "None");
        }

        [Test]
        public void CheckWinWithTile_ReturnsNoneWhenFiveCopies()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckWinWithTile(
                "1m 1m 1m 1m 2m 3m 4m 5m 6m 7m 8m 9m E",
                "1m");

            AssertWinCheckResult(driver, result, false, "None");
            Assert.That(
                driver.CanWinWithTile(
                    "1m 1m 1m 1m 2m 3m 4m 5m 6m 7m 8m 9m E",
                    "1m"),
                Is.False);
        }

        [Test]
        public void CanWinStandardHand_ReturnsFalseForSevenPairs()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();
            object tiles = driver.CreateTiles(
                "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C C");

            Assert.That(driver.CanWinStandardHand(tiles), Is.False);
            AssertWinCheckResult(driver, driver.CheckCompletedHand(tiles), true, "SevenPairs");
        }

        [Test]
        public void CanWinStandardHand_ReturnsFalseForThirteenOrphans()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();
            object tiles = driver.CreateTiles(
                "1m 9m 1p 9p 1s 9s E E S W N P F C");

            Assert.That(driver.CanWinStandardHand(tiles), Is.False);
            AssertWinCheckResult(driver, driver.CheckCompletedHand(tiles), true, "ThirteenOrphans");
        }

        [Test]
        public void CheckCompletedHand_ReturnsNoneFor13Tiles()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckCompletedHand(
                "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E");

            AssertWinCheckResult(driver, result, false, "None");
        }

        [Test]
        public void CheckCompletedHand_ReturnsNoneFor15Tiles()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();

            object result = driver.CheckCompletedHand(
                "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E S W");

            AssertWinCheckResult(driver, result, false, "None");
        }

        [Test]
        public void CanWinStandardHand_ReturnsFalseWhenHandContainsInvalidTile()
        {
            WinCheckerTestDriver driver = WinCheckerTestDriver.Create();
            object tiles = driver.CreateTiles(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                14);

            Assert.That(driver.TileAt(tiles, 13), Is.EqualTo(driver.CreateInvalidTile()));
            Assert.That(driver.CanWinStandardHand(tiles), Is.False);
        }

        private static void AssertWinCheckResult(
            WinCheckerTestDriver driver,
            object result,
            bool expectedCanWin,
            string expectedShape)
        {
            Assert.That(driver.ResultCanWin(result), Is.EqualTo(expectedCanWin));
            Assert.That(driver.ResultShapeName(result), Is.EqualTo(expectedShape));
        }
    }
}
