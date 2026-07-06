using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorFlushTests
    {
        private const string HonitsuManHand =
            "1m 2m 3m 4m 5m 6m 7m 8m 9m 1m 1m 1m E";
        private const string HonitsuPinHand =
            "1p 2p 3p 4p 5p 6p 7p 8p 9p 1p 1p 1p E";
        private const string HonitsuSouHand =
            "1s 2s 3s 4s 5s 6s 7s 8s 9s 1s 1s 1s E";
        private const string ChinitsuManHand =
            "1m 2m 3m 4m 5m 6m 7m 8m 9m 1m 1m 1m 2m";
        private const string ChinitsuPinHand =
            "1p 2p 3p 4p 5p 6p 7p 8p 9p 1p 1p 1p 2p";
        private const string ChinitsuSouHand =
            "1s 2s 3s 4s 5s 6s 7s 8s 9s 1s 1s 1s 2s";

        [TestCase(HonitsuManHand, "E")]
        [TestCase(HonitsuPinHand, "E")]
        [TestCase(HonitsuSouHand, "E")]
        public void EvaluateWithTile_OneNumberSuitAndHonors_AddsHonitsuOnly(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFlushCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honitsu");

                AssertFlushCandidate(driver, result, candidate, "Honitsu", 3);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateYakuHanName(candidate, "Honitsu"), Is.EqualTo("Three"));
            }
        }

        [TestCase(ChinitsuManHand, "2m")]
        [TestCase(ChinitsuPinHand, "2p")]
        [TestCase(ChinitsuSouHand, "2s")]
        public void EvaluateWithTile_OneNumberSuitWithoutHonors_AddsChinitsuOnly(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFlushCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinitsu");

                AssertFlushCandidate(driver, result, candidate, "Chinitsu", 6);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honitsu"), Is.False);
                Assert.That(driver.CandidateYakuHanName(candidate, "Chinitsu"), Is.EqualTo("Six"));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsOneSuitAndHonors_AddsHonitsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("SevenPairs", "Two", "None"),
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    "1m 1m 2m 2m 3m 3m 4m 4m E E P P F",
                    "F",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "SevenPairs",
                    "Honitsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("SevenPairs"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(5));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsOneSuitWithoutHonors_AddsChinitsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("SevenPairs", "Two", "None"),
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m",
                    "7m",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "SevenPairs",
                    "Chinitsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("SevenPairs"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Honitsu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(8));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHonitsu_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFlushCatalog(driver),
                    HonitsuManHand,
                    "E",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honitsu");

                AssertFlushCandidate(driver, result, candidate, "Honitsu", 2);
                Assert.That(driver.CandidateYakuHanName(candidate, "Honitsu"), Is.EqualTo("Two"));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenChinitsu_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFlushCatalog(driver),
                    ChinitsuManHand,
                    "2m",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinitsu");

                AssertFlushCandidate(driver, result, candidate, "Chinitsu", 5);
                Assert.That(driver.CandidateYakuHanName(candidate, "Chinitsu"), Is.EqualTo("Five"));
            }
        }

        [TestCase(
            "1m 2m 3m 4m 5m 6m 1p 2p 3p 7p 8p 9p E",
            "E")]
        [TestCase(
            "1m 2m 3m 4m 5m 6m 1s 2s 3s 7s 8s 9s E",
            "E")]
        [TestCase(
            "1p 2p 3p 4p 5p 6p 1s 2s 3s 7s 8s 9s E",
            "E")]
        [TestCase(
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
            "C")]
        [TestCase(
            "E E E S S S W W W P P P C",
            "C")]
        public void EvaluateWithTile_NonFlushShapes_DoNotAddFlushYaku(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFlushCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoFlushYaku(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddFlushYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFlushCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoFlushYaku(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_HonitsuCombinesWithIttsuu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("Ittsuu", "Two", "One")),
                    HonitsuManHand,
                    "E",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "Honitsu",
                    "Ittsuu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(5));
            }
        }

        [Test]
        public void EvaluateWithTile_ChinitsuCombinesWithIttsuu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Chinitsu", "Six", "Five"),
                        driver.CreateDefinition("Ittsuu", "Two", "One")),
                    ChinitsuManHand,
                    "2m",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "Chinitsu",
                    "Ittsuu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honitsu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(8));
            }
        }

        [Test]
        public void EvaluateWithTile_HonitsuCombinesWithYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("YakuhaiSeatWind", "One", "One")),
                    "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E 1m",
                    "1m",
                    "Ron",
                    roundWindName: "South",
                    seatWindName: "East");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "Honitsu",
                    "YakuhaiSeatWind");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
            }
        }

        [TestCase("Missing", true)]
        [TestCase("Disabled", true)]
        [TestCase("ClosedHanNone", true)]
        [TestCase("OpenHanNone", false)]
        public void EvaluateWithTile_HonitsuUnavailable_DoesNotSwitchToChinitsu(
            string unavailableReason,
            bool isClosed)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonitsuUnavailableCatalog(driver, unavailableReason),
                    HonitsuManHand,
                    "E",
                    "Ron",
                    isClosed: isClosed);

                AssertNoFlushYaku(driver, result);
            }
        }

        [TestCase("Missing", true)]
        [TestCase("Disabled", true)]
        [TestCase("ClosedHanNone", true)]
        [TestCase("OpenHanNone", false)]
        public void EvaluateWithTile_ChinitsuUnavailable_DoesNotFallbackToHonitsu(
            string unavailableReason,
            bool isClosed)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChinitsuUnavailableCatalog(driver, unavailableReason),
                    ChinitsuManHand,
                    "2m",
                    "Ron",
                    isClosed: isClosed);

                AssertNoFlushYaku(driver, result);
            }
        }

        private static object CreateFlushCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Honitsu", "Three", "Two"),
                driver.CreateDefinition("Chinitsu", "Six", "Five"));
        }

        private static object CreateHonitsuUnavailableCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chinitsu", "Six", "Five"));
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chinitsu", "Six", "Five"),
                        driver.CreateDefinition(
                            "Honitsu",
                            "Three",
                            "Two",
                            isEnabled: false));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chinitsu", "Six", "Five"),
                        driver.CreateDefinition("Honitsu", "None", "Two"));
                case "OpenHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chinitsu", "Six", "Five"),
                        driver.CreateDefinition("Honitsu", "Three", "None"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static object CreateChinitsuUnavailableCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"));
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition(
                            "Chinitsu",
                            "Six",
                            "Five",
                            isEnabled: false));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("Chinitsu", "None", "Five"));
                case "OpenHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("Chinitsu", "Six", "None"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static void AssertFlushCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            string yakuKindName,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateContainsYaku(candidate, yakuKindName), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.ContainsYaku(result, yakuKindName), Is.False);
        }

        private static void AssertNoFlushYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Honitsu"), Is.EqualTo(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Chinitsu"), Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, "Honitsu"), Is.False);
            Assert.That(driver.ContainsYaku(result, "Chinitsu"), Is.False);
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
