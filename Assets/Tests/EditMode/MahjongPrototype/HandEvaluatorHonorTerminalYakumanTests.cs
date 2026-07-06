using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorHonorTerminalYakumanTests
    {
        private const string TsuuiisouStandardHand =
            "E E E S S S W W W P P P C";
        private const string TsuuiisouSevenPairsHand =
            "E E S S W W N N P P F F C";
        private const string TsuuiisouWithNumberHand =
            "E E E S S S W W W P P P 1m";
        private const string DaisuushiiTsuuiisouHand =
            "E E E S S S W W W N N N P";
        private const string ChinroutouHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p 9p 9p 9p 1s";
        private const string ChinroutouWithMiddleHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p 9p 9p 9p 2s";
        private const string ChinroutouWithHonorHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p 9p 9p 9p E";

        [Test]
        public void EvaluateWithTile_StandardTsuuiisou_AddsTsuuiisouOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Tsuuiisou");

                AssertYakumanOnlyCandidate(driver, candidate, "Tsuuiisou", 1);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsTsuuiisou_AddsTsuuiisouAndRemovesSevenPairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateTsuuiisouDefinition(driver),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    TsuuiisouSevenPairsHand,
                    "C",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenTsuuiisou_AddsTsuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron",
                    isClosed: false);

                Assert.That(
                    driver.FindCandidateContainingYaku(result, "Tsuuiisou"),
                    Is.Not.Null);
            }
        }

        [Test]
        public void EvaluateWithTile_NumberTileInHonorShape_DoesNotAddTsuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    TsuuiisouWithNumberHand,
                    "1m",
                    "Ron");

                AssertNoYakuInResult(driver, result, "Tsuuiisou");
            }
        }

        [Test]
        public void EvaluateWithTile_TsuuiisouMissing_KeepsYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One")),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_TsuuiisouDisabled_KeepsYakuhai()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Tsuuiisou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One")),
                    TsuuiisouStandardHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_TsuuiisouAndDaisuushii_KeepsBothYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateTsuuiisouDefinition(driver),
                        driver.CreateDefinition(
                            "Daisuushii",
                            "None",
                            "None",
                            isYakuman: true)),
                    DaisuushiiTsuuiisouHand,
                    "P",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisuushii");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Daisuushii"), Is.True);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddTsuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateTsuuiisouDefinition(driver),
                        driver.CreateDefinition(
                            "KokushiMusou",
                            "None",
                            "None",
                            isYakuman: true)),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tsuuiisou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_Chinroutou_AddsChinroutouOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    ChinroutouHand,
                    "1s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinroutou");

                AssertYakumanOnlyCandidate(driver, candidate, "Chinroutou", 1);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenChinroutou_AddsChinroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    ChinroutouHand,
                    "1s",
                    "Ron",
                    isClosed: false);

                Assert.That(
                    driver.FindCandidateContainingYaku(result, "Chinroutou"),
                    Is.Not.Null);
            }
        }

        [Test]
        public void EvaluateWithTile_MiddleTileInTerminalShape_DoesNotAddChinroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    ChinroutouWithMiddleHand,
                    "2s",
                    "Ron");

                AssertNoYakuInResult(driver, result, "Chinroutou");
            }
        }

        [Test]
        public void EvaluateWithTile_HonorInTerminalShape_DoesNotAddChinroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateHonorTerminalCatalog(driver),
                    ChinroutouWithHonorHand,
                    "E",
                    "Ron");

                AssertNoYakuInResult(driver, result, "Chinroutou");
            }
        }

        [Test]
        public void EvaluateWithTile_ChinroutouAndSuuankouTanki_KeepsBothYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateChinroutouDefinition(driver),
                        driver.CreateDefinition(
                            "SuuankouTanki",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition(
                            "Suuankou",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition("Sanankou", "Two", "Two")),
                    ChinroutouHand,
                    "1s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinroutou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinroutou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_ChinroutouMissing_KeepsCommonYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    ChinroutouHand,
                    "1s",
                    "Tsumo",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Reach");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinroutou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.True);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_ChinroutouDisabled_KeepsMenzenTsumo()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Chinroutou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    ChinroutouHand,
                    "1s",
                    "Tsumo");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "MenzenTsumo");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinroutou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsAndThirteenOrphans_DoNotAddChinroutou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object sevenPairsResult = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateChinroutouDefinition(driver),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "1m 1m 9m 9m 1p 1p 9p 9p 1s 1s 9s 9s E",
                    "E",
                    "Ron");
                object sevenPairsCandidate =
                    FindCandidateOfType(driver, sevenPairsResult, "SevenPairs");

                Assert.That(sevenPairsCandidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(sevenPairsCandidate, "Chinroutou"),
                    Is.False);

                object thirteenOrphansResult = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateChinroutouDefinition(driver),
                        driver.CreateDefinition(
                            "KokushiMusou",
                            "None",
                            "None",
                            isYakuman: true)),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");
                object thirteenOrphansCandidate =
                    FindCandidateOfType(driver, thirteenOrphansResult, "ThirteenOrphans");

                Assert.That(thirteenOrphansCandidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(thirteenOrphansCandidate, "Chinroutou"),
                    Is.False);
            }
        }

        private static object CreateHonorTerminalCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                CreateTsuuiisouDefinition(driver),
                CreateChinroutouDefinition(driver));
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

        private static void AssertYakumanOnlyCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate,
            string yakuKindName,
            int expectedYakuCount)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateContainsYaku(candidate, yakuKindName), Is.True);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(expectedYakuCount));
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
