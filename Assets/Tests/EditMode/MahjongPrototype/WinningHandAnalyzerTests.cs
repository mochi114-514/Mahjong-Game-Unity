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
        public void AnalyzeCompletedHand_DoesNotInferWinningTilePlacement()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeCompletedHand(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m 5m");

            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(1));
            Assert.That(driver.StandardWinningInterpretationCount(result), Is.EqualTo(0));
        }

        [TestCase("2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "1m", "Sequence", "Sequence:1m,2m,3m")]
        [TestCase("2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "4m", "Sequence", "Sequence:2m,3m,4m")]
        [TestCase("7m 8m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "6m", "Sequence", "Sequence:6m,7m,8m")]
        [TestCase("7m 8m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "9m", "Sequence", "Sequence:7m,8m,9m")]
        public void AnalyzeWithTile_DetectsRyanmenWait(
            string handText,
            string winningTileCode,
            string expectedMeldType,
            string expectedTargetMeldKey)
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(handText, winningTileCode);

            object interpretation = AssertSingleWait(
                driver,
                result,
                "Ryanmen",
                "Meld");
            Assert.That(driver.PlacementTargetMeldIndex(interpretation), Is.InRange(0, 3));
            Assert.That(driver.PlacementTargetMeldTypeName(interpretation), Is.EqualTo(expectedMeldType));
            Assert.That(driver.PlacementTargetMeldKey(interpretation), Is.EqualTo(expectedTargetMeldKey));
        }

        [TestCase("1m 2m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "3m", "Sequence:1m,2m,3m")]
        [TestCase("8m 9m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "7m", "Sequence:7m,8m,9m")]
        public void AnalyzeWithTile_DetectsPenchanWait(
            string handText,
            string winningTileCode,
            string expectedTargetMeldKey)
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(handText, winningTileCode);

            object interpretation = AssertSingleWait(
                driver,
                result,
                "Penchan",
                "Meld");
            Assert.That(driver.PlacementTargetMeldTypeName(interpretation), Is.EqualTo("Sequence"));
            Assert.That(driver.PlacementTargetMeldKey(interpretation), Is.EqualTo(expectedTargetMeldKey));
        }

        [TestCase("2m 4m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "3m", "Sequence:2m,3m,4m")]
        [TestCase("4m 6m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E", "5m", "Sequence:4m,5m,6m")]
        public void AnalyzeWithTile_DetectsKanchanWait(
            string handText,
            string winningTileCode,
            string expectedTargetMeldKey)
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(handText, winningTileCode);

            object interpretation = AssertSingleWait(
                driver,
                result,
                "Kanchan",
                "Meld");
            Assert.That(driver.PlacementTargetMeldTypeName(interpretation), Is.EqualTo("Sequence"));
            Assert.That(driver.PlacementTargetMeldKey(interpretation), Is.EqualTo(expectedTargetMeldKey));
        }

        [Test]
        public void AnalyzeWithTile_DetectsTankiWait()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(
                "1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s E",
                "E");

            object interpretation = AssertSingleWait(driver, result, "Tanki", "Pair");
            Assert.That(driver.InterpretationWinningTileCode(interpretation), Is.EqualTo("E"));
            Assert.That(driver.PlacementTargetMeldIndex(interpretation), Is.EqualTo(-1));
            Assert.That(driver.PlacementTargetMeldTypeName(interpretation), Is.EqualTo(string.Empty));
        }

        [Test]
        public void AnalyzeWithTile_DetectsShanponWait()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(
                "1m 2m 3m 4p 5p 6p 7s 8s 9s E E C C",
                "E");

            object interpretation = AssertSingleWait(driver, result, "Shanpon", "Meld");
            Assert.That(driver.InterpretationWinningTileCode(interpretation), Is.EqualTo("E"));
            Assert.That(driver.PlacementTargetMeldIndex(interpretation), Is.InRange(0, 3));
            Assert.That(driver.PlacementTargetMeldTypeName(interpretation), Is.EqualTo("Triplet"));
            Assert.That(driver.PlacementTargetMeldKey(interpretation), Is.EqualTo("Triplet:E,E,E"));
        }

        [Test]
        public void AnalyzeWithTile_KeepsMultipleStandardWinningInterpretations()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(
                "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m",
                "7m");

            Assert.That(driver.SevenPairsIsWin(result), Is.True);
            AssertHasWait(driver, result, "Ryanmen");
            AssertHasWait(driver, result, "Tanki");
            AssertStandardWinningInterpretationsAreUnique(driver, result);
        }

        [Test]
        public void AnalyzeWithTile_DeduplicatesSameMeldPlacementByMeaning()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            object result = driver.AnalyzeWithTile(
                "1m 1m 2m 2m 3m 1p 2p 3p 4p 5p 6p E E",
                "3m");

            Assert.That(
                CountInterpretations(
                    driver,
                    result,
                    "Penchan",
                    "Meld",
                    "Sequence:1m,2m,3m"),
                Is.EqualTo(1));
            AssertStandardWinningInterpretationsAreUnique(driver, result);
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
        public void AnalyzeWithTile_ReturnsNotWinForInvalidInput()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();

            AssertNotWin(
                driver,
                driver.AnalyzeWithTile(
                    (object)null,
                    driver.CreateTile("1m")));
            AssertNotWin(
                driver,
                driver.AnalyzeWithTile(
                    driver.CreateTiles("1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s"),
                    driver.CreateTile("E")));
            AssertNotWin(
                driver,
                driver.AnalyzeWithTile(
                    driver.CreateTiles("1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s E E"),
                    driver.CreateTile("E")));
            AssertNotWin(
                driver,
                driver.AnalyzeWithTile(
                    driver.CreateTiles(
                        "1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s",
                        13),
                    driver.CreateTile("E")));
            AssertNotWin(
                driver,
                driver.AnalyzeWithTile(
                    driver.CreateTiles("1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s E"),
                    driver.CreateInvalidTile()));
            AssertNotWin(
                driver,
                driver.AnalyzeWithTile(
                    driver.CreateTiles("1m 1m 1m 1m 2m 3m 4m 5m 6m 7m 8m 9m E"),
                    driver.CreateTile("1m")));
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
        public void StandardWinningInterpretations_DoNotChangeWhenSourceTilesAreMutated()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();
            object tiles = driver.CreateTiles(
                "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E");

            object result = driver.AnalyzeWithTile(tiles, driver.CreateTile("1m"));
            driver.SetTile(tiles, 0, "9m");

            object interpretation = driver.StandardWinningInterpretationAt(result, 0);
            Assert.That(driver.InterpretationWinningTileCode(interpretation), Is.EqualTo("1m"));
            Assert.That(driver.InterpretationWaitTypeName(interpretation), Is.EqualTo("Ryanmen"));
            Assert.That(driver.PlacementTargetMeldKey(interpretation), Is.EqualTo("Sequence:1m,2m,3m"));
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

        [Test]
        public void StandardWinningInterpretation_CollectionsCannotBeModifiedExternally()
        {
            WinningHandAnalyzerTestDriver driver = WinningHandAnalyzerTestDriver.Create();
            object result = driver.AnalyzeWithTile(
                "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E",
                "1m");
            object interpretation = driver.StandardWinningInterpretationAt(result, 0);
            object decomposition = driver.InterpretationDecomposition(interpretation);

            IList interpretations = (IList)driver.StandardWinningInterpretations(result);
            IList melds = (IList)driver.Melds(decomposition);
            IList targetMeldTiles = (IList)driver.PlacementTargetMeldTiles(interpretation);

            Assert.Throws<NotSupportedException>(() =>
            {
                interpretations.Add(interpretation);
            });
            Assert.Throws<NotSupportedException>(() =>
            {
                melds.Add(driver.FirstMeld(decomposition));
            });
            Assert.Throws<NotSupportedException>(() =>
            {
                targetMeldTiles.Add(driver.CreateTile("1m"));
            });
        }

        private static void AssertNotWin(
            WinningHandAnalyzerTestDriver driver,
            object result)
        {
            Assert.That(driver.CanWin(result), Is.False);
            Assert.That(driver.StandardDecompositionCount(result), Is.EqualTo(0));
            Assert.That(driver.StandardWinningInterpretationCount(result), Is.EqualTo(0));
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

        private static object AssertSingleWait(
            WinningHandAnalyzerTestDriver driver,
            object result,
            string expectedWaitType,
            string expectedPlacementType)
        {
            Assert.That(driver.CanWin(result), Is.True);
            Assert.That(driver.StandardWinningInterpretationCount(result), Is.EqualTo(1));

            object interpretation = driver.StandardWinningInterpretationAt(result, 0);
            Assert.That(driver.InterpretationWaitTypeName(interpretation), Is.EqualTo(expectedWaitType));
            Assert.That(driver.PlacementWaitTypeName(interpretation), Is.EqualTo(expectedWaitType));
            Assert.That(driver.PlacementTypeName(interpretation), Is.EqualTo(expectedPlacementType));
            return interpretation;
        }

        private static void AssertHasWait(
            WinningHandAnalyzerTestDriver driver,
            object result,
            string expectedWaitType)
        {
            int count = driver.StandardWinningInterpretationCount(result);
            for (int i = 0; i < count; i++)
            {
                if (driver.InterpretationWaitTypeName(
                        driver.StandardWinningInterpretationAt(result, i)) == expectedWaitType)
                {
                    return;
                }
            }

            Assert.Fail("Expected wait type was not found: " + expectedWaitType);
        }

        private static int CountInterpretations(
            WinningHandAnalyzerTestDriver driver,
            object result,
            string waitType,
            string placementType,
            string targetMeldKey)
        {
            int matches = 0;
            int count = driver.StandardWinningInterpretationCount(result);
            for (int i = 0; i < count; i++)
            {
                object interpretation = driver.StandardWinningInterpretationAt(result, i);
                if (driver.InterpretationWaitTypeName(interpretation) == waitType &&
                    driver.PlacementTypeName(interpretation) == placementType &&
                    driver.PlacementTargetMeldKey(interpretation) == targetMeldKey)
                {
                    matches++;
                }
            }

            return matches;
        }

        private static void AssertStandardWinningInterpretationsAreUnique(
            WinningHandAnalyzerTestDriver driver,
            object result)
        {
            HashSet<string> keys = new HashSet<string>();
            string[] interpretationKeys = driver.StandardWinningInterpretationKeys(result);
            for (int i = 0; i < interpretationKeys.Length; i++)
            {
                Assert.That(
                    keys.Add(interpretationKeys[i]),
                    Is.True,
                    "Duplicate standard winning interpretation: " + interpretationKeys[i]);
            }
        }
    }
}
