using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorWindYakumanTests
    {
        private const string DaisuushiiHand =
            "E E E S S S W W W N N N 5m";
        private const string ShousuushiiNorthPairHand =
            "E E E S S S W W W 1m 2m 3m N";

        [Test]
        public void EvaluateWithTile_FourWindTriplets_AddsDaisuushiiOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    DaisuushiiHand,
                    "5m",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisuushii");

                AssertWindYakumanCandidate(
                    driver,
                    result,
                    candidate,
                    "Daisuushii",
                    "Shousuushii");
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenFourWindTriplets_AddsDaisuushii()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    DaisuushiiHand,
                    "5m",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisuushii");

                AssertWindYakumanCandidate(
                    driver,
                    result,
                    candidate,
                    "Daisuushii",
                    "Shousuushii");
            }
        }

        [TestCase(
            "S S S W W W N N N 1m 2m 3m E",
            "E")]
        [TestCase(
            "E E E W W W N N N 1m 2m 3m S",
            "S")]
        [TestCase(
            "E E E S S S N N N 1m 2m 3m W",
            "W")]
        [TestCase(
            ShousuushiiNorthPairHand,
            "N")]
        public void EvaluateWithTile_ThreeWindTripletsAndRemainingWindPair_AddsShousuushiiOnly(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Shousuushii");

                AssertWindYakumanCandidate(
                    driver,
                    result,
                    candidate,
                    "Shousuushii",
                    "Daisuushii");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenThreeWindTripletsAndRemainingWindPair_AddsShousuushii()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    ShousuushiiNorthPairHand,
                    "N",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Shousuushii");

                AssertWindYakumanCandidate(
                    driver,
                    result,
                    candidate,
                    "Shousuushii",
                    "Daisuushii");
            }
        }

        [TestCase(
            "E E E S S S W W W 1m 2m 3m 5p",
            "5p")]
        [TestCase(
            "E E E S S S 1m 2m 3m 4p 5p 6p N",
            "N")]
        public void EvaluateWithTile_NonWindYakumanShapes_DoNotAddWindYakuman(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoWindYakuman(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_DaisuushiiMissing_DoesNotFallbackToShousuushii()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Shousuushii",
                            "None",
                            "None",
                            isYakuman: true)),
                    DaisuushiiHand,
                    "5m",
                    "Ron");

                AssertNoWindYakuman(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_DaisuushiiDisabled_DoesNotFallbackToShousuushii()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Daisuushii",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        driver.CreateDefinition(
                            "Shousuushii",
                            "None",
                            "None",
                            isYakuman: true)),
                    DaisuushiiHand,
                    "5m",
                    "Ron");

                AssertNoWindYakuman(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ShousuushiiMissing_DoesNotSwitchToDaisuushii()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Daisuushii",
                            "None",
                            "None",
                            isYakuman: true)),
                    ShousuushiiNorthPairHand,
                    "N",
                    "Ron");

                AssertNoWindYakuman(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ShousuushiiDisabled_DoesNotSwitchToDaisuushii()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Daisuushii",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition(
                            "Shousuushii",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false)),
                    ShousuushiiNorthPairHand,
                    "N",
                    "Ron");

                AssertNoWindYakuman(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_DaisuushiiWithNormalYaku_LeavesOnlyYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanAndNormalCatalog(driver),
                    DaisuushiiHand,
                    "5m",
                    "Tsumo",
                    isReachDeclared: true,
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisuushii");

                AssertWindYakumanCandidate(
                    driver,
                    result,
                    candidate,
                    "Daisuushii",
                    "Shousuushii");
                AssertNormalYakuRemoved(driver, candidate);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_DaisuushiiUnavailable_KeepsWindYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateOnlyWindYakuhaiCatalog(driver),
                    DaisuushiiHand,
                    "5m",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiSeatWind");

                AssertUnavailableWindYakumanCandidate(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_ShousuushiiUnavailable_KeepsWindYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateOnlyWindYakuhaiCatalog(driver),
                    ShousuushiiNorthPairHand,
                    "N",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiSeatWind");

                AssertUnavailableWindYakumanCandidate(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddWindYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    "E E S S W W N N P P F F C",
                    "C",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoWindYakuman(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddWindYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateWindYakumanCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoWindYakuman(driver, result);
            }
        }

        private static object CreateWindYakumanCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(
                    "Shousuushii",
                    "None",
                    "None",
                    isYakuman: true),
                driver.CreateDefinition(
                    "Daisuushii",
                    "None",
                    "None",
                    isYakuman: true));
        }

        private static object CreateWindYakumanAndNormalCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(
                    "Shousuushii",
                    "None",
                    "None",
                    isYakuman: true),
                driver.CreateDefinition(
                    "Daisuushii",
                    "None",
                    "None",
                    isYakuman: true),
                driver.CreateDefinition("YakuhaiSeatWind", "One", "One"),
                driver.CreateDefinition("YakuhaiRoundWind", "One", "One"),
                driver.CreateDefinition("Reach", "One", "None"),
                driver.CreateDefinition("MenzenTsumo", "One", "None"));
        }

        private static object CreateOnlyWindYakuhaiCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("YakuhaiSeatWind", "One", "One"),
                driver.CreateDefinition("YakuhaiRoundWind", "One", "One"));
        }

        private static void AssertWindYakumanCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            string yakuKindName,
            string excludedYakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, yakuKindName), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, excludedYakuKindName), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, yakuKindName), Is.False);
        }

        private static void AssertNoWindYakuman(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Daisuushii"), Is.EqualTo(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Shousuushii"), Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, "Daisuushii"), Is.False);
            Assert.That(driver.ContainsYaku(result, "Shousuushii"), Is.False);
        }

        private static void AssertNormalYakuRemoved(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiSeatWind"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRoundWind"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
        }

        private static void AssertUnavailableWindYakumanCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateContainsYaku(candidate, "Daisuushii"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Shousuushii"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiSeatWind"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRoundWind"), Is.True);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
        }
    }
}
