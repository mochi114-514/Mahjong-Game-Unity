using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorYakuhaiTests
    {
        [TestCase("East", "South", "E")]
        [TestCase("South", "East", "S")]
        [TestCase("West", "East", "W")]
        [TestCase("North", "East", "N")]
        public void EvaluateWithTile_SelfWindTriplet_AddsSeatWindYakuhai(
            string seatWindName,
            string roundWindName,
            string tripletCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateYakuhaiCatalog(driver),
                    tripletCode,
                    roundWindName,
                    seatWindName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiSeatWind");

                AssertYakuhaiCandidate(driver, result, candidate, "YakuhaiSeatWind", 1);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "YakuhaiRoundWind"),
                    Is.False);
            }
        }

        [TestCase("East", "South", "E")]
        [TestCase("South", "East", "S")]
        public void EvaluateWithTile_RoundWindTriplet_AddsRoundWindYakuhai(
            string roundWindName,
            string seatWindName,
            string tripletCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateYakuhaiCatalog(driver),
                    tripletCode,
                    roundWindName,
                    seatWindName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiRoundWind");

                AssertYakuhaiCandidate(driver, result, candidate, "YakuhaiRoundWind", 1);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "YakuhaiSeatWind"),
                    Is.False);
            }
        }

        [TestCase("East", "E")]
        [TestCase("South", "S")]
        public void EvaluateWithTile_RenfonTriplet_AddsSeatAndRoundWindYakuhai(
            string windName,
            string tripletCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateYakuhaiCatalog(driver),
                    tripletCode,
                    windName,
                    windName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiSeatWind");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "YakuhaiRoundWind"),
                    Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
                Assert.That(driver.ContainsYaku(result, "YakuhaiSeatWind"), Is.False);
                Assert.That(driver.ContainsYaku(result, "YakuhaiRoundWind"), Is.False);
            }
        }

        [TestCase("South", "East", "W")]
        [TestCase("East", "South", "N")]
        public void EvaluateWithTile_UnrelatedWindTriplet_DoesNotAddWindYakuhai(
            string seatWindName,
            string roundWindName,
            string tripletCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateYakuhaiCatalog(driver),
                    tripletCode,
                    roundWindName,
                    seatWindName);

                AssertNoCandidateYaku(
                    driver,
                    result,
                    "YakuhaiSeatWind",
                    "YakuhaiRoundWind");
            }
        }

        [TestCase("P", "YakuhaiWhiteDragon")]
        [TestCase("F", "YakuhaiGreenDragon")]
        [TestCase("C", "YakuhaiRedDragon")]
        public void EvaluateWithTile_DragonTriplet_AddsDragonYakuhai(
            string tripletCode,
            string yakuKindName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateYakuhaiCatalog(driver),
                    tripletCode,
                    "East",
                    "South");
                object candidate =
                    driver.FindCandidateContainingYaku(result, yakuKindName);

                AssertYakuhaiCandidate(driver, result, candidate, yakuKindName, 1);
            }
        }

        [Test]
        public void EvaluateWithTile_MultipleDragonTriplets_AddsEachDragonYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateYakuhaiCatalog(driver),
                    "P P P F F F 1m 2m 3m 1p 2p 3p 5s",
                    "5s",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"),
                    Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
                Assert.That(driver.ContainsYaku(result, "YakuhaiWhiteDragon"), Is.False);
                Assert.That(driver.ContainsYaku(result, "YakuhaiGreenDragon"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DragonPair_DoesNotAddDragonYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateYakuhaiCatalog(driver),
                    HandWithPair("P"),
                    "P",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "East");

                AssertNoCandidateYaku(driver, result, "YakuhaiWhiteDragon");
            }
        }

        [Test]
        public void EvaluateWithTile_WindPair_DoesNotAddWindYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateYakuhaiCatalog(driver),
                    HandWithPair("E"),
                    "E",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "East");

                AssertNoCandidateYaku(
                    driver,
                    result,
                    "YakuhaiSeatWind",
                    "YakuhaiRoundWind");
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsWithHonorPairs_DoesNotAddYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateYakuhaiCatalog(driver),
                    "E E P P C C 1m 1m 2m 2m 3p 3p 4p",
                    "4p",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "East");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoCandidateYaku(
                    driver,
                    result,
                    "YakuhaiSeatWind",
                    "YakuhaiRoundWind",
                    "YakuhaiWhiteDragon",
                    "YakuhaiRedDragon");
            }
        }

        [Test]
        public void EvaluateWithTile_YakuhaiDefinitionMissing_DoesNotAddYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    driver.CreateCatalog(),
                    "E",
                    "South",
                    "East");

                AssertNoCandidateYaku(driver, result, "YakuhaiSeatWind");
            }
        }

        [Test]
        public void EvaluateWithTile_YakuhaiDefinitionDisabled_DoesNotAddYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "YakuhaiSeatWind",
                            "One",
                            "One",
                            isEnabled: false)),
                    "E",
                    "South",
                    "East");

                AssertNoCandidateYaku(driver, result, "YakuhaiSeatWind");
            }
        }

        [Test]
        public void EvaluateWithTile_YakuhaiClosedHanNone_DoesNotAddYakuWhenClosed()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateSingleYakuhaiCatalog(
                        driver,
                        "YakuhaiSeatWind",
                        "None",
                        "One"),
                    "E",
                    "South",
                    "East");

                AssertNoCandidateYaku(driver, result, "YakuhaiSeatWind");
            }
        }

        [Test]
        public void EvaluateWithTile_YakuhaiOpenHanNone_DoesNotAddYakuWhenOpen()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateSingleYakuhaiCatalog(
                        driver,
                        "YakuhaiSeatWind",
                        "One",
                        "None"),
                    "E",
                    "South",
                    "East",
                    isClosed: false);

                AssertNoCandidateYaku(driver, result, "YakuhaiSeatWind");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHandYakuhai_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateTriplet(
                    driver,
                    CreateSingleYakuhaiCatalog(
                        driver,
                        "YakuhaiSeatWind",
                        "One",
                        "One"),
                    "E",
                    "South",
                    "East",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiSeatWind");

                AssertYakuhaiCandidate(driver, result, candidate, "YakuhaiSeatWind", 1);
                Assert.That(
                    driver.CandidateYakuHanName(candidate, "YakuhaiSeatWind"),
                    Is.EqualTo("One"));
            }
        }

        private static object EvaluateTriplet(
            WinDeclarationEvaluatorTestDriver driver,
            object catalog,
            string tripletCode,
            string roundWindName,
            string seatWindName,
            bool isClosed = true)
        {
            return driver.EvaluateWithTile(
                catalog,
                HandWithTriplet(tripletCode),
                "5m",
                "Ron",
                roundWindName: roundWindName,
                seatWindName: seatWindName,
                isClosed: isClosed);
        }

        private static string HandWithTriplet(string tripletCode)
        {
            return tripletCode + " " +
                   tripletCode + " " +
                   tripletCode +
                   " 1m 2m 3m 1p 2p 3p 1s 2s 3s 5m";
        }

        private static string HandWithPair(string pairCode)
        {
            return "1m 2m 3m 4m 5m 6m 1p 2p 3p 4p 5p 6p " + pairCode;
        }

        private static object CreateYakuhaiCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("YakuhaiSeatWind", "One", "One"),
                driver.CreateDefinition("YakuhaiRoundWind", "One", "One"),
                driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
        }

        private static object CreateSingleYakuhaiCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string yakuKindName,
            string closedHanName,
            string openHanName)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(yakuKindName, closedHanName, openHanName));
        }

        private static void AssertYakuhaiCandidate(
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

        private static void AssertNoCandidateYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            params string[] yakuKindNames)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));

            for (int i = 0; i < yakuKindNames.Length; i++)
            {
                Assert.That(
                    driver.CountCandidatesContainingYaku(result, yakuKindNames[i]),
                    Is.EqualTo(0));
                Assert.That(driver.ContainsYaku(result, yakuKindNames[i]), Is.False);
            }
        }
    }
}
