using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorYakumanPriorityTests
    {
        private const string DaisangenHand =
            "P P P F F F C C C 1m 2m 3m 5p";

        [Test]
        public void EvaluateWithTile_DaisangenWithCommonAndYakuhaiYaku_LeavesOnlyYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateYakumanPriorityCatalog(driver),
                    DaisangenHand,
                    "5p",
                    "Tsumo",
                    isReachDeclared: true,
                    isIppatsuEligible: true,
                    isDoubleReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Daisangen");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
                AssertNormalYakuRemoved(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_DaisangenUnavailable_DoesNotRemoveNormalYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                        driver.CreateDefinition("YakuhaiRedDragon", "One", "One")),
                    DaisangenHand,
                    "5p",
                    "Ron",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "YakuhaiWhiteDragon");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Daisangen"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRedDragon"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
            }
        }

        [Test]
        public void EvaluateWithTile_KokushiYakumanCandidate_RemovesReachFromSameCandidate()
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
                            isYakuman: true),
                        driver.CreateDefinition("Reach", "One", "None")),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "KokushiMusou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("ThirteenOrphans"));
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            }
        }

        private static object CreateYakumanPriorityCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(
                    "Daisangen",
                    "None",
                    "None",
                    isYakuman: true),
                driver.CreateDefinition("DoubleReach", "Two", "None"),
                driver.CreateDefinition("Reach", "One", "None"),
                driver.CreateDefinition("Ippatsu", "One", "None"),
                driver.CreateDefinition("MenzenTsumo", "One", "None"),
                driver.CreateDefinition("YakuhaiWhiteDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                driver.CreateDefinition("YakuhaiRedDragon", "One", "One"));
        }

        private static void AssertNormalYakuRemoved(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "DoubleReach"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Ippatsu"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiWhiteDragon"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiRedDragon"), Is.False);
        }
    }
}
