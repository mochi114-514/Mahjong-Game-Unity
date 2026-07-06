using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorChuurenPoutouTests
    {
        private const string JunseiManHand =
            "1m 1m 1m 2m 3m 4m 5m 6m 7m 8m 9m 9m 9m";
        private const string JunseiPinHand =
            "1p 1p 1p 2p 3p 4p 5p 6p 7p 8p 9p 9p 9p";
        private const string JunseiSouHand =
            "1s 1s 1s 2s 3s 4s 5s 6s 7s 8s 9s 9s 9s";
        private const string NormalManHand =
            "1m 1m 1m 2m 3m 5m 5m 6m 7m 8m 9m 9m 9m";
        private const string NormalPinHand =
            "1p 1p 1p 2p 3p 5p 5p 6p 7p 8p 9p 9p 9p";
        private const string MissingRequiredCountHand =
            "1m 1m 2m 2m 3m 3m 4m 5m 6m 7m 8m 9m 9m";
        private const string MultipleSuitHand =
            "1m 1m 1m 2m 3m 4m 5m 6m 7m 7p 8p 9p 9p";
        private const string HonorMixedHand =
            "1m 1m 1m 2m 3m 4m 5m 6m 7m 9m 9m 9m E";

        [TestCase("1m")]
        [TestCase("2m")]
        [TestCase("3m")]
        [TestCase("4m")]
        [TestCase("5m")]
        [TestCase("6m")]
        [TestCase("7m")]
        [TestCase("8m")]
        [TestCase("9m")]
        public void EvaluateWithTile_JunseiManBaseRon_AddsJunseiOnly(
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    JunseiManHand,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "JunseiChuurenPoutou");

                AssertJunseiOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_JunseiManBaseTsumo_AddsJunseiOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    JunseiManHand,
                    "5m",
                    "Tsumo");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "JunseiChuurenPoutou");

                AssertJunseiOnly(driver, candidate);
            }
        }

        [TestCase(JunseiPinHand, "5p")]
        [TestCase(JunseiSouHand, "5s")]
        public void EvaluateWithTile_JunseiOtherSuits_AddsJunseiOnly(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "JunseiChuurenPoutou");

                AssertJunseiOnly(driver, candidate);
            }
        }

        [TestCase("Ron")]
        [TestCase("Tsumo")]
        public void EvaluateWithTile_NormalChuurenMan_AddsChuurenOnly(
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    NormalManHand,
                    "4m",
                    winTypeName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "ChuurenPoutou");

                AssertChuurenOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_NormalChuurenPin_AddsChuurenOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    NormalPinHand,
                    "4p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "ChuurenPoutou");

                AssertChuurenOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_JunseiMissing_FallsBackToChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateChuurenDefinition(driver)),
                    JunseiManHand,
                    "5m",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "ChuurenPoutou");

                AssertChuurenOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_JunseiDisabled_FallsBackToChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "JunseiChuurenPoutou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        CreateChuurenDefinition(driver)),
                    JunseiManHand,
                    "5m",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "ChuurenPoutou");

                AssertChuurenOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_BothChuurenKindsUnavailable_KeepsChinitsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    JunseiManHand,
                    "5m",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinitsu");

                Assert.That(candidate, Is.Not.Null);
                AssertNoChuurenYaku(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(6));
            }
        }

        [Test]
        public void EvaluateWithTile_NormalChuurenDoesNotFallbackToJunsei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateJunseiDefinition(driver),
                        driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    NormalManHand,
                    "4m",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinitsu");

                Assert.That(candidate, Is.Not.Null);
                AssertNoChuurenYaku(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(6));
            }
        }

        [Test]
        public void EvaluateWithTile_SingleSuitMissingRequiredCount_DoesNotAddChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    MissingRequiredCountHand,
                    "9m",
                    "Ron");

                AssertNoChuurenYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_MultipleNumberSuits_DoesNotAddChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    MultipleSuitHand,
                    "9p",
                    "Ron");

                AssertNoChuurenYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_HonorTile_DoesNotAddChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChuurenCatalog(driver),
                    HonorMixedHand,
                    "E",
                    "Ron");

                AssertNoChuurenYakuInResult(driver, result);
            }
        }

        [TestCase(JunseiManHand, "5m")]
        [TestCase(NormalManHand, "4m")]
        public void EvaluateWithTile_OpenHand_DoesNotAddChuurenAndKeepsOpenChinitsu(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateJunseiDefinition(driver),
                        CreateChuurenDefinition(driver),
                        driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    handText,
                    winningTileCode,
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinitsu");

                Assert.That(candidate, Is.Not.Null);
                AssertNoChuurenYaku(driver, candidate);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(5));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateJunseiDefinition(driver),
                        CreateChuurenDefinition(driver),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m",
                    "7m",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                AssertNoChuurenYaku(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddChuuren()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateJunseiDefinition(driver),
                        CreateChuurenDefinition(driver),
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
                AssertNoChuurenYaku(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_ChuurenWithNormalYaku_RemovesNormalYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateChuurenDefinition(driver),
                        driver.CreateDefinition("Chinitsu", "Six", "Five"),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    NormalManHand,
                    "4m",
                    "Tsumo",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "ChuurenPoutou");

                AssertChuurenOnly(driver, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_JunseiWithNormalYaku_RemovesNormalYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateJunseiDefinition(driver),
                        CreateChuurenDefinition(driver),
                        driver.CreateDefinition("Chinitsu", "Six", "Five"),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    JunseiManHand,
                    "5m",
                    "Tsumo",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "JunseiChuurenPoutou");

                AssertJunseiOnly(driver, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
            }
        }

        private static object CreateChuurenCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                CreateChuurenDefinition(driver),
                CreateJunseiDefinition(driver));
        }

        private static object CreateChuurenDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "ChuurenPoutou",
                "None",
                "None",
                isYakuman: true);
        }

        private static object CreateJunseiDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "JunseiChuurenPoutou",
                "None",
                "None",
                isYakuman: true);
        }

        private static void AssertJunseiOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, "JunseiChuurenPoutou"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "ChuurenPoutou"), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertChuurenOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, "ChuurenPoutou"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "JunseiChuurenPoutou"), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertNoChuurenYakuInResult(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
                AssertNoChuurenYaku(driver, driver.CandidateResultAt(result, i));
        }

        private static void AssertNoChuurenYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "ChuurenPoutou"), Is.False);
            Assert.That(
                driver.CandidateContainsYaku(candidate, "JunseiChuurenPoutou"),
                Is.False);
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
    }
}
