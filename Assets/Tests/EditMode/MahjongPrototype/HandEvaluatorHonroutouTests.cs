using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorHonroutouTests
    {
        private const string HonroutouStandardHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p E E E P";
        private const string HonroutouSevenPairsHand =
            "1m 1m 9m 9m 1p 1p 9p 9p 1s 1s 9s 9s E";
        private const string MiddleTileHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p E E E 2s";
        private const string TsuuiisouStandardHand =
            "E E E S S S W W W P P P C";
        private const string TsuuiisouSevenPairsHand =
            "E E S S W W N N P P F F C";
        private const string ChinroutouHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p 9p 9p 9p 1s";
        private const string KokushiHand =
            "1m 9m 1p 9p 1s 9s E S W N P F C";

        [Test]
        public void EvaluateWithTile_StandardHonroutou_AddsHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateHonroutouDefinition(driver)),
                    HonroutouStandardHand,
                    "P",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honroutou");

                AssertHonroutouOnly(driver, candidate, 2);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsHonroutou_AddsHonroutouAndSevenPairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    HonroutouSevenPairsHand,
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.True);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHonroutou_AddsHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateHonroutouDefinition(driver)),
                    HonroutouStandardHand,
                    "P",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honroutou");

                AssertHonroutouOnly(driver, candidate, 2);
            }
        }

        [Test]
        public void EvaluateWithTile_MiddleTile_DoesNotAddHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateHonroutouDefinition(driver)),
                    MiddleTileHand,
                    "2s",
                    "Ron");

                AssertNoYakuInResult(driver, result, "Honroutou");
            }
        }

        [Test]
        public void EvaluateWithTile_TsuuiisouAvailable_RemovesHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        CreateTsuuiisouDefinition(driver)),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Tsuuiisou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsTsuuiisouAvailable_RemovesHonroutouAndSevenPairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        CreateTsuuiisouDefinition(driver),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    TsuuiisouSevenPairsHand,
                    "C",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_ChinroutouAvailable_RemovesHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        CreateChinroutouDefinition(driver)),
                    ChinroutouHand,
                    "1s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinroutou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_TsuuiisouMissing_KeepsHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateHonroutouDefinition(driver)),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honroutou");

                AssertHonroutouOnly(driver, candidate, 2);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_TsuuiisouDisabled_KeepsHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        driver.CreateDefinition(
                            "Tsuuiisou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false)),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honroutou");

                AssertHonroutouOnly(driver, candidate, 2);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_ChinroutouMissing_KeepsHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateHonroutouDefinition(driver)),
                    ChinroutouHand,
                    "1s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honroutou");

                AssertHonroutouOnly(driver, candidate, 2);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinroutou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_ChinroutouDisabled_KeepsHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        driver.CreateDefinition(
                            "Chinroutou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false)),
                    ChinroutouHand,
                    "1s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honroutou");

                AssertHonroutouOnly(driver, candidate, 2);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinroutou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_HonroutouMissing_KeepsSevenPairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    HonroutouSevenPairsHand,
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_HonroutouDisabled_KeepsSevenPairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Honroutou",
                            "Two",
                            "Two",
                            isEnabled: false),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    HonroutouSevenPairsHand,
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddHonroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateHonroutouDefinition(driver),
                        driver.CreateDefinition(
                            "KokushiMusou",
                            "None",
                            "None",
                            isYakuman: true)),
                    KokushiHand,
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.False);
            }
        }

        private static object CreateHonroutouDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition("Honroutou", "Two", "Two");
        }

        private static object CreateTsuuiisouDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "Tsuuiisou",
                "None",
                "None",
                isYakuman: true);
        }

        private static object CreateChinroutouDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "Chinroutou",
                "None",
                "None",
                isYakuman: true);
        }

        private static void AssertHonroutouOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate,
            int expectedTotalHan)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateContainsYaku(candidate, "Honroutou"), Is.True);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
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

            return null;
        }

        private static void AssertNoYakuInResult(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
            {
                Assert.That(
                    driver.CandidateContainsYaku(
                        driver.CandidateResultAt(result, i),
                        yakuKindName),
                    Is.False);
            }
        }
    }
}
