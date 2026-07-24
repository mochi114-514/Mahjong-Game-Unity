using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorKokushiMusouWaitTests
    {
        private const string ThirteenWaitHand =
            "1m 9m 1p 9p 1s 9s E S W N P F C";
        private const string NormalKokushiHand =
            "1m 1m 9m 1p 9p 1s 9s E S W N P F";
        private const string StandardTerminalHonorHand =
            "1m 1m 1m 9m 9m 9m 1p 1p 1p E E E P";
        private const string SevenPairsTerminalHonorHand =
            "1m 1m 9m 9m 1p 1p 9p 9p 1s 1s 9s 9s E";

        [TestCase("1m")]
        [TestCase("9m")]
        [TestCase("1p")]
        [TestCase("9p")]
        [TestCase("1s")]
        [TestCase("9s")]
        [TestCase("E")]
        [TestCase("S")]
        [TestCase("W")]
        [TestCase("N")]
        [TestCase("P")]
        [TestCase("F")]
        [TestCase("C")]
        public void EvaluateWithTile_AllThirteenWaitWinningTiles_AddsThirteenWaitOnly(
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKokushiCatalog(driver),
                    ThirteenWaitHand,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(
                        result,
                        "KokushiMusouThirteenWait");

                AssertThirteenWaitOnly(driver, candidate);
            }
        }

        [TestCase("Ron")]
        [TestCase("Tsumo")]
        public void EvaluateWithTile_ThirteenWaitRonAndTsumo_AddsThirteenWaitOnly(
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKokushiCatalog(driver),
                    ThirteenWaitHand,
                    "E",
                    winTypeName);
                object candidate =
                    driver.FindCandidateContainingYaku(
                        result,
                        "KokushiMusouThirteenWait");

                AssertThirteenWaitOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_NormalKokushi_AddsKokushiOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKokushiCatalog(driver),
                    NormalKokushiHand,
                    "C",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "KokushiMusou");

                AssertKokushiOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenWaitMissing_FallsBackToKokushi()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateKokushiDefinition(driver)),
                    ThirteenWaitHand,
                    "E",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "KokushiMusou");

                AssertKokushiOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenWaitDisabled_FallsBackToKokushi()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "KokushiMusouThirteenWait",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        CreateKokushiDefinition(driver)),
                    ThirteenWaitHand,
                    "E",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "KokushiMusou");

                AssertKokushiOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_BothKokushiKindsUnavailable_KeepsReach()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None")),
                    ThirteenWaitHand,
                    "E",
                    "Ron",
                    isReachDeclared: true);
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                AssertNoKokushiYaku(driver, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_NormalKokushiDoesNotFallbackToThirteenWait()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateThirteenWaitDefinition(driver)),
                    NormalKokushiHand,
                    "C",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                AssertNoKokushiYaku(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_NormalKokushiDisabled_DoesNotFallbackToThirteenWait()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "KokushiMusou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        CreateThirteenWaitDefinition(driver)),
                    NormalKokushiHand,
                    "C",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                AssertNoKokushiYaku(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [TestCase(ThirteenWaitHand, "E")]
        [TestCase(NormalKokushiHand, "C")]
        public void EvaluateWithTile_OpenHand_DoesNotAddKokushiYaku(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKokushiCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron",
                    isClosed: false);
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                AssertNoKokushiYaku(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenWaitWithNormalYaku_RemovesNormalYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateThirteenWaitDefinition(driver),
                        CreateKokushiDefinition(driver),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    ThirteenWaitHand,
                    "E",
                    "Tsumo",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(
                        result,
                        "KokushiMusouThirteenWait");

                AssertThirteenWaitOnly(driver, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_StandardCandidate_DoesNotAddKokushiYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKokushiCatalog(driver),
                    StandardTerminalHonorHand,
                    "P",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "Standard");

                Assert.That(candidate, Is.Not.Null);
                AssertNoKokushiYaku(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsCandidate_DoesNotAddKokushiYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateThirteenWaitDefinition(driver),
                        CreateKokushiDefinition(driver),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    SevenPairsTerminalHonorHand,
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                AssertNoKokushiYaku(driver, candidate);
            }
        }

        private static object CreateKokushiCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                CreateThirteenWaitDefinition(driver),
                CreateKokushiDefinition(driver));
        }

        private static object CreateKokushiDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "KokushiMusou",
                "None",
                "None",
                yakumanMultiplier: 1);
        }

        private static object CreateThirteenWaitDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "KokushiMusouThirteenWait",
                "None",
                "None",
                yakumanMultiplier: 2);
        }

        private static void AssertThirteenWaitOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("ThirteenOrphans"));
            Assert.That(
                driver.CandidateContainsYaku(candidate, "KokushiMusouThirteenWait"),
                Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "KokushiMusou"), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateTotalYakumanMultiplier(candidate), Is.EqualTo(2));
            Assert.That(
                driver.CandidateYakuYakumanMultiplier(
                    candidate,
                    "KokushiMusouThirteenWait"),
                Is.EqualTo(2));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertKokushiOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("ThirteenOrphans"));
            Assert.That(driver.CandidateContainsYaku(candidate, "KokushiMusou"), Is.True);
            Assert.That(
                driver.CandidateContainsYaku(candidate, "KokushiMusouThirteenWait"),
                Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateTotalYakumanMultiplier(candidate), Is.EqualTo(1));
            Assert.That(
                driver.CandidateYakuYakumanMultiplier(candidate, "KokushiMusou"),
                Is.EqualTo(1));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertNoKokushiYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "KokushiMusou"), Is.False);
            Assert.That(
                driver.CandidateContainsYaku(candidate, "KokushiMusouThirteenWait"),
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
