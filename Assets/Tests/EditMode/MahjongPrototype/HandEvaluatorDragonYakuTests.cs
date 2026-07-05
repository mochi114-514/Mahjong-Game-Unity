using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorDragonYakuTests
    {
        private const string DaisangenHand =
            "P P P F F F C C C 1m 2m 3m 5p";
        private const string ShousangenRedPairHand =
            "P P P F F F C C 1m 2m 3m 7p 7p";

        [Test]
        public void EvaluateWithTile_AllDragonTriplets_AddsDaisangenOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonCatalog(driver),
                    DaisangenHand,
                    "5p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisangen");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
                Assert.That(driver.CandidateContainsYaku(candidate, "Shousangen"), Is.False);
                AssertNoYakuhaiDragonYaku(driver, candidate);
                Assert.That(driver.ContainsYaku(result, "Daisangen"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHandAllDragonTriplets_AddsDaisangen()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Daisangen",
                            "None",
                            "None",
                            isYakuman: true)),
                    DaisangenHand,
                    "5p",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisangen");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_DaisangenMissing_KeepsDragonYakuhaiAndNoShousangen()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonYakuhaiCatalog(driver),
                    DaisangenHand,
                    "5p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                AssertDaisangenUnavailableCandidate(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_DaisangenDisabled_KeepsDragonYakuhaiAndNoShousangen()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Daisangen",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        driver.CreateDefinition("Shousangen", "Two", "Two"),
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiRedDragon", "One", "One")),
                    DaisangenHand,
                    "5p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                AssertDaisangenUnavailableCandidate(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_DragonPairCompletedIntoTriplet_DoesNotFallbackToShousangen()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonYakuhaiCatalog(driver),
                    ShousangenRedPairHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                AssertDaisangenUnavailableCandidate(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_TwoDragonTripletsAndRemainingDragonPair_AddsShousangenAndTripletYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonCatalog(driver),
                    ShousangenRedPairHand,
                    "7p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Shousangen");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Daisangen"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiWhiteDragon"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRedDragon"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
            }
        }

        [TestCase(
            "F F F C C C P P 1m 2m 3m 7p 7p",
            "YakuhaiWhiteDragon")]
        [TestCase(
            "P P P C C C F F 1m 2m 3m 7p 7p",
            "YakuhaiGreenDragon")]
        [TestCase(
            ShousangenRedPairHand,
            "YakuhaiRedDragon")]
        public void EvaluateWithTile_EachDragonPairCanCompleteShousangen(
            string handText,
            string pairDragonYakuhaiName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonCatalog(driver),
                    handText,
                    "7p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Shousangen");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, pairDragonYakuhaiName), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
            }
        }

        [TestCase(
            "P P P F F 1m 2m 3m 4m 5m 6m 7p 7p",
            "7p")]
        [TestCase(
            "P P P F F F 1m 2m 3m 4m 5m 6m 7p",
            "7p")]
        [TestCase(
            "P P P F F F 1m 2m 3m 4m 5m 6m E",
            "E")]
        [TestCase(DaisangenHand, "5p")]
        public void EvaluateWithTile_NonShousangenDragonShapes_DoNotAddShousangen(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Shousangen", "Two", "Two")),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, "Shousangen");
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddDragonGroupYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonCatalog(driver),
                    "P P F F C C 1m 1m 2m 2m 3p 3p 4p",
                    "4p",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "Daisangen", "Shousangen");
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddDragonGroupYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateDragonCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "Daisangen", "Shousangen");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenShousangen_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Shousangen", "Two", "One")),
                    ShousangenRedPairHand,
                    "7p",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Shousangen");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateYakuHanName(candidate, "Shousangen"), Is.EqualTo("One"));
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [TestCase("Missing", true)]
        [TestCase("Disabled", true)]
        [TestCase("ClosedHanNone", true)]
        [TestCase("OpenHanNone", false)]
        public void EvaluateWithTile_ShousangenUnavailable_DoesNotAddShousangenButKeepsYakuhai(
            string unavailableReason,
            bool isClosed)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateShousangenUnavailableCatalog(driver, unavailableReason),
                    ShousangenRedPairHand,
                    "7p",
                    "Ron",
                    isClosed: isClosed);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Shousangen"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRedDragon"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        private static object CreateDragonCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(
                    "Daisangen",
                    "None",
                    "None",
                    isYakuman: true),
                driver.CreateDefinition("Shousangen", "Two", "Two"),
                driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
        }

        private static object CreateDragonYakuhaiCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Shousangen", "Two", "Two"),
                driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
        }

        private static object CreateShousangenUnavailableCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return CreateOnlyDragonYakuhaiCatalog(driver);
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Shousangen",
                            "Two",
                            "Two",
                            isEnabled: false),
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Shousangen", "None", "Two"),
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
                case "OpenHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Shousangen", "Two", "None"),
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static object CreateOnlyDragonYakuhaiCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
        }

        private static void AssertDaisangenUnavailableCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateContainsYaku(candidate, "Daisangen"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Shousangen"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiWhiteDragon"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRedDragon"), Is.True);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
        }

        private static void AssertNoYakuhaiDragonYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiWhiteDragon"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRedDragon"), Is.False);
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
