using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorPeikouTests
    {
        private const string BasicIipeikouHand =
            "1m 2m 3m 1m 2m 3m 7p 7p 7p 9s 9s 9s 5s";
        private const string RyanpeikouDifferentPairsHand =
            "1m 2m 3m 1m 2m 3m 2m 3m 4m 2m 3m 5p 5p";
        private const string FourIdenticalSequenceHand =
            "1m 2m 3m 1m 2m 3m 1m 2m 3m 1m 2m 3m 5p";
        private const string SevenPairsAndStandardHand =
            "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m";

        [Test]
        public void EvaluateWithTile_AddsIipeikouToClosedStandardCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    BasicIipeikouHand,
                    "5s",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Iipeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Ryanpeikou"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
                Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
                Assert.That(driver.HasYaku(result), Is.True);
                Assert.That(driver.CanDeclareWin(result), Is.True);
                Assert.That(driver.TopLevelYakuCount(result), Is.EqualTo(0));
                Assert.That(driver.TotalHan(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotTreatDifferentSuitSequencesAsIipeikou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    "1m 2m 3m 1p 2p 3p 4s 5s 6s 7s 8s 9s 5p",
                    "5p",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.GreaterThan(0));
                AssertNoCandidateContainsPeikou(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotTreatDifferentStartSequencesAsIipeikou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    "1m 2m 3m 2m 3m 4m 4p 5p 6p 7s 8s 9s 5p",
                    "5p",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.GreaterThan(0));
                AssertNoCandidateContainsPeikou(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ThreeIdenticalSequencesCountsAsOneIipeikouPair()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    "1m 2m 3m 1m 2m 3m 1m 2m 3m 4p 5p 6p 5s",
                    "5s",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Iipeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Ryanpeikou"), Is.False);
                Assert.That(driver.CountCandidatesContainingYaku(result, "Ryanpeikou"), Is.EqualTo(0));
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_AddsRyanpeikouForTwoDifferentIdenticalSequencePairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Ryanpeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Iipeikou"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
                Assert.That(driver.CanDeclareWin(result), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_FourIdenticalSequencesCountsAsRyanpeikou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    FourIdenticalSequenceHand,
                    "5p",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Ryanpeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Iipeikou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPeikouWhenHandIsOpenEvenIfCatalogHasOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Iipeikou", "One", "One"),
                        driver.CreateDefinition("Ryanpeikou", "Three", "One")),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron",
                    isClosed: false);

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.GreaterThan(0));
                AssertNoCandidateContainsPeikou(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_RyanpeikouSuppressesIipeikou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Ryanpeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Iipeikou"), Is.False);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
            }
        }

        [Test]
        public void EvaluateWithTile_FallsBackToIipeikouWhenRyanpeikouMissingFromCatalog()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Iipeikou", "One", "None")),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Iipeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Ryanpeikou"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_FallsBackToIipeikouWhenRyanpeikouHasNoClosedHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Iipeikou", "One", "None"),
                        driver.CreateDefinition("Ryanpeikou", "None", "None")),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron");
                object candidate = driver.FindCandidateContainingYaku(result, "Iipeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Ryanpeikou"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_EmptyCatalogKeepsCandidateWithoutPeikouYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "Standard");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateHasYaku(candidate), Is.False);
                AssertNoCandidateContainsPeikou(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPeikouToSevenPairsCandidateWhenStandardAlsoExists()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    SevenPairsAndStandardHand,
                    "7m",
                    "Ron");
                object ryanpeikouCandidate =
                    driver.FindCandidateContainingYaku(result, "Ryanpeikou");
                object sevenPairsCandidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.GreaterThan(0));
                Assert.That(ryanpeikouCandidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(ryanpeikouCandidate), Is.EqualTo("Standard"));
                AssertNoPeikou(driver, sevenPairsCandidate);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPeikouToPureSevenPairsCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.EqualTo(0));
                Assert.That(driver.CandidateSevenPairsIsWin(candidate), Is.True);
                AssertNoPeikou(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPeikouToThirteenOrphansCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePeikouCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(driver.CandidateThirteenOrphansIsWin(candidate), Is.True);
                AssertNoPeikou(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_CombinesPinfuAndRyanpeikouInSameStandardCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Pinfu", "One", "None"),
                        driver.CreateDefinition("Iipeikou", "One", "None"),
                        driver.CreateDefinition("Ryanpeikou", "Three", "None")),
                    RyanpeikouDifferentPairsHand,
                    "4m",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "Pinfu",
                    "Ryanpeikou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo("Ryanmen"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Iipeikou"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
            }
        }

        private static object CreatePeikouCatalog(WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Iipeikou", "One", "None"),
                driver.CreateDefinition("Ryanpeikou", "Three", "None"));
        }

        private static void AssertNoCandidateContainsPeikou(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            Assert.That(driver.CountCandidatesContainingYaku(result, "Iipeikou"), Is.EqualTo(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Ryanpeikou"), Is.EqualTo(0));
        }

        private static void AssertNoPeikou(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "Iipeikou"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Ryanpeikou"), Is.False);
        }

        private static object FindCandidateOfType(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string typeName)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
            {
                object candidate = driver.CandidateResultAt(result, i);
                if (driver.CandidateTypeName(candidate) == typeName)
                    return candidate;
            }

            Assert.Fail($"Candidate not found: {typeName}");
            return null;
        }

        private static object FindCandidateContainingAllYakus(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            params string[] yakuKindNames)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
            {
                object candidate = driver.CandidateResultAt(result, i);
                bool containsAll = true;

                for (int j = 0; j < yakuKindNames.Length; j++)
                {
                    if (!driver.CandidateContainsYaku(candidate, yakuKindNames[j]))
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                    return candidate;
            }

            return null;
        }
    }
}
