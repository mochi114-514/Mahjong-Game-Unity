using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorFirstTurnYakumanTests
    {
        private const string StandardHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";
        private const string SevenPairsHand =
            "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E P";
        private const string ThirteenOrphansHand =
            "1m 9m 1p 9p 1s 9s E S W N P F C";

        [Test]
        public void EvaluateWithTile_EastFirstTurnTsumo_AddsTenhouOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFirstTurnYakumanCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isFirstTurnTsumoEligible: true);
                object candidate = driver.FindCandidateContainingYaku(result, "Tenhou");

                AssertFirstTurnYakumanOnly(driver, candidate, "Tenhou", "Chiihou");
            }
        }

        [TestCase("South")]
        [TestCase("West")]
        [TestCase("North")]
        public void EvaluateWithTile_NonEastFirstTurnTsumo_AddsChiihouOnly(
            string seatWindName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFirstTurnYakumanCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo",
                    seatWindName: seatWindName,
                    isFirstTurnTsumoEligible: true);
                object candidate = driver.FindCandidateContainingYaku(result, "Chiihou");

                AssertFirstTurnYakumanOnly(driver, candidate, "Chiihou", "Tenhou");
            }
        }

        [Test]
        public void EvaluateWithTile_Ron_DoesNotAddFirstTurnYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFirstTurnYakumanCatalog(driver),
                    StandardHand,
                    "C",
                    "Ron",
                    isFirstTurnTsumoEligible: true);

                AssertNoFirstTurnYakumanInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_NotEligible_DoesNotAddFirstTurnYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFirstTurnYakumanCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo");

                AssertNoFirstTurnYakumanInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHand_DoesNotAddFirstTurnYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateFirstTurnYakumanCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isClosed: false,
                    isFirstTurnTsumoEligible: true);

                AssertNoFirstTurnYakumanInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_TenhouMissing_KeepsMenzenTsumo()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isFirstTurnTsumoEligible: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "MenzenTsumo");

                Assert.That(candidate, Is.Not.Null);
                AssertNoFirstTurnYakuman(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_TenhouDisabled_KeepsMenzenTsumo()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Tenhou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isFirstTurnTsumoEligible: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "MenzenTsumo");

                Assert.That(candidate, Is.Not.Null);
                AssertNoFirstTurnYakuman(driver, candidate);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_FirstTurnYakuman_RemovesNormalYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Tenhou",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isReachDeclared: true,
                    isFirstTurnTsumoEligible: true);
                object candidate = driver.FindCandidateContainingYaku(result, "Tenhou");

                AssertFirstTurnYakumanOnly(driver, candidate, "Tenhou", "Chiihou");
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphansTenhou_KeepsBothYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Tenhou",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition(
                            "KokushiMusou",
                            "None",
                            "None",
                            isYakuman: true)),
                    ThirteenOrphansHand,
                    "E",
                    "Tsumo",
                    isFirstTurnTsumoEligible: true);
                object candidate = driver.FindCandidateContainingYaku(result, "Tenhou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("ThirteenOrphans"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Tenhou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "KokushiMusou"), Is.True);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairsCandidate_AddsTenhou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Tenhou",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    SevenPairsHand,
                    "P",
                    "Tsumo",
                    isFirstTurnTsumoEligible: true);
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tenhou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void ExistingConstructors_DefaultFirstTurnTsumoEligibleFalse()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                Assert.That(
                    driver.IsFirstTurnTsumoEligible(driver.CreateLegacyHandEvaluationContext()),
                    Is.False);
                Assert.That(
                    driver.IsFirstTurnTsumoEligible(
                        driver.CreateLegacyWinDeclarationEvaluationContext()),
                    Is.False);
            }
        }

        [Test]
        public void NewConstructors_StoreFirstTurnTsumoEligibleTrue()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                Assert.That(
                    driver.IsFirstTurnTsumoEligible(
                        driver.CreateFirstTurnTsumoHandEvaluationContext()),
                    Is.True);
                Assert.That(
                    driver.IsFirstTurnTsumoEligible(
                        driver.CreateFirstTurnTsumoWinDeclarationEvaluationContext()),
                    Is.True);
            }
        }

        private static object CreateFirstTurnYakumanCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(
                    "Tenhou",
                    "None",
                    "None",
                    isYakuman: true),
                driver.CreateDefinition(
                    "Chiihou",
                    "None",
                    "None",
                    isYakuman: true));
        }

        private static void AssertFirstTurnYakumanOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate,
            string expectedYakuKindName,
            string unexpectedYakuKindName)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(
                driver.CandidateContainsYaku(candidate, expectedYakuKindName),
                Is.True);
            Assert.That(
                driver.CandidateContainsYaku(candidate, unexpectedYakuKindName),
                Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
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

        private static void AssertNoFirstTurnYakumanInResult(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
                AssertNoFirstTurnYakuman(driver, driver.CandidateResultAt(result, i));
        }

        private static void AssertNoFirstTurnYakuman(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "Tenhou"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Chiihou"), Is.False);
        }
    }
}
