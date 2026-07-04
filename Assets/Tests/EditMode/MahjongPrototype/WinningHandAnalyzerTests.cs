using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinningHandAnalyzerTests
    {
        [Test]
        public void AnalyzeCompletedHand_DecomposesSequencesTripletAndPair()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m 5m");

            Assert.That(driver.CanWin(result), Is.True);
            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(1));
            object decomposition = driver.FirstStandardDecomposition(result);
            Assert.That(driver.PairTileCode(decomposition), Is.EqualTo("5m"));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "Sequence:1m,2m,3m",
                    "Sequence:2p,3p,4p",
                    "Sequence:7s,8s,9s",
                    "Triplet:E,E,E"
                },
                driver.MeldKeys(decomposition));
        }

        [Test]
        public void AnalyzeCompletedHand_DecomposesFourTripletsAndPair()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 1m 1m 2m 2m 2m 3p 3p 3p C C C 9s 9s");

            Assert.That(driver.CanWin(result), Is.True);
            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(1));
            object decomposition = driver.FirstStandardDecomposition(result);
            Assert.That(driver.PairTileCode(decomposition), Is.EqualTo("9s"));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "Triplet:1m,1m,1m",
                    "Triplet:2m,2m,2m",
                    "Triplet:3p,3p,3p",
                    "Triplet:C,C,C"
                },
                driver.MeldKeys(decomposition));
        }

        [Test]
        public void AnalyzeCompletedHand_ReturnsNoStandardCandidateForNonWinningShape()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E S");

            Assert.That(driver.CanWin(result), Is.False);
            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(0));
        }

        [Test]
        public void AnalyzeCompletedHand_KeepsMultipleInterpretationsWithoutDuplicateStandardDecompositions()
        {
            WinningHandAnalyzerTestDriver analyzerDriver = WinningHandAnalyzerTestDriver.Create();
            WinCheckerTestDriver winCheckerDriver = WinCheckerTestDriver.Create();

            object result = analyzerDriver.AnalyzeCompletedHand(
                "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m");

            Assert.That(analyzerDriver.StandardDecompositionCount(result), Is.GreaterThan(0));
            Assert.That(analyzerDriver.SevenPairsIsWin(result), Is.True);
            AssertStandardDecompositionsAreUnique(analyzerDriver, result);

            object winResult = winCheckerDriver.CheckCompletedHand(
                "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m");
            Assert.That(winCheckerDriver.ResultCanWin(winResult), Is.True);
            Assert.That(winCheckerDriver.ResultShapeName(winResult), Is.EqualTo("Standard"));
        }

        [Test]
        public void AnalyzeCompletedHand_DetectsSevenPairs()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C C");

            Assert.That(driver.SevenPairsIsWin(result), Is.True);
            CollectionAssert.AreEquivalent(
                new[] { "1m", "2m", "3p", "4p", "5s", "E", "C" },
                driver.SevenPairTileCodes(result));
        }

        [Test]
        public void AnalyzeCompletedHand_DoesNotTreatFourCopiesAsTwoSevenPairs()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 1m 1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E");

            Assert.That(driver.SevenPairsIsWin(result), Is.False);
        }

        [Test]
        public void AnalyzeCompletedHand_DetectsThirteenOrphansAndKeepsRequiredAndPairTiles()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 9m 1p 9p 1s 9s E S W N P F C E");

            Assert.That(driver.ThirteenOrphansIsWin(result), Is.True);
            Assert.That(driver.ThirteenOrphansPairTileCode(result), Is.EqualTo("E"));
            CollectionAssert.AreEquivalent(
                new[] { "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W", "N", "P", "F", "C" },
                driver.ThirteenOrphansRequiredTileCodes(result));
        }

        [Test]
        public void AnalyzeCompletedHand_RejectsThirteenOrphansWhenRequiredTileIsMissing()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 9m 1p 9p 1s 9s E E S W N P F F");

            Assert.That(driver.ThirteenOrphansIsWin(result), Is.False);
        }

        [Test]
        public void AnalyzeCompletedHand_RejectsThirteenOrphansWithUnneededNumberTile()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 9m 1p 9p 1s 9s E S W N P F E 2m");

            Assert.That(driver.ThirteenOrphansIsWin(result), Is.False);
        }

        [Test]
        public void AnalyzeWithTile_CompletesByAddingWinningTile()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                "C");

            Assert.That(driver.CanWin(result), Is.True);
            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(1));
        }

        [Test]
        public void AnalyzeCompletedHand_ReturnsNotWinForInvalidInput()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            AssertNotWin(driver, driver.AnalyzeCompletedHand((object)null));
            AssertNotWin(
                driver,
                driver.AnalyzeCompletedHand(
                    "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E"));
            AssertNotWin(
                driver,
                driver.AnalyzeCompletedHand(
                    "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E S W"));
            AssertNotWin(
                driver,
                driver.AnalyzeCompletedHand(
                    driver.CreateTiles(
                        "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                        14)));
            AssertNotWin(
                driver,
                driver.AnalyzeCompletedHand(
                    "1m 1m 1m 1m 1m 2m 3m 4m 5m 6m 7m 8m 9m E"));
        }

        [Test]
        public void AnalyzeWithTile_ReturnsNotWinForInvalidWinningTile()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(
                driver.CreateTiles("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C"),
                driver.CreateInvalidTile());

            AssertNotWin(driver, result);
        }

        [Test]
        public void AnalysisResult_DoesNotChangeWhenSourceTilesAreMutated()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();
            object tiles = driver.CreateTiles(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m 5m");

            object result = driver.AnalyzeCompletedHand(tiles);
            driver.SetTile(tiles, 0, "9m");

            object decomposition = driver.FirstStandardDecomposition(result);
            Assert.That(driver.PairTileCode(decomposition), Is.EqualTo("5m"));
            CollectionAssert.Contains(driver.MeldKeys(decomposition), "Sequence:1m,2m,3m");
        }

        [Test]
        public void AnalysisResult_CollectionsCannotBeModifiedExternally()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();
            object result = driver.AnalyzeCompletedHand(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m 5m");
            object decomposition = driver.FirstStandardDecomposition(result);

            IList decompositions = (IList)driver.StandardDecompositions(result);
            IList melds = (IList)driver.Melds(decomposition);

            Assert.Throws<NotSupportedException>(() =>
            {
                decompositions.Add(decomposition);
            });
            Assert.Throws<NotSupportedException>(() =>
            {
                melds.Add(driver.FirstMeld(decomposition));
            });
        }

        private static void AssertNotWin(
            WinningHandAnalyzerTestDriver driver,
            object result)
        {
            Assert.That(driver.CanWin(result), Is.False);
            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(0));
            Assert.That(driver.SevenPairsIsWin(result), Is.False);
            Assert.That(driver.ThirteenOrphansIsWin(result), Is.False);
        }

        private static void AssertStandardDecompositionsAreUnique(
            WinningHandAnalyzerTestDriver driver,
            object result)
        {
            HashSet<string> keys = new HashSet<string>();
            int count = driver.StandardDecompositionCount(result);
            for (int i = 0; i < count; i++)
            {
                object decomposition = driver.StandardDecompositionAt(result, i);
                string key = driver.PairTileCode(decomposition) +
                             "|" +
                             string.Join("|", driver.MeldKeys(decomposition));
                Assert.That(keys.Add(key), Is.True, "Duplicate standard decomposition: " + key);
            }
        }
    }
}
