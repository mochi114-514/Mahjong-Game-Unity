using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorIppatsuTests
    {
        private const string BasicWinningHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";

        [Test]
        public void EvaluateWithTile_ReachIppatsuClosed_AddsIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinitionWithDisplayName(
                            "Ippatsu",
                            "\u4E00\u767A",
                            "One",
                            "None")),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isIppatsuEligible: true);
                object candidate = driver.FindCandidateContainingYaku(result, "Ippatsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                Assert.That(driver.CandidateYakuDisplayName(candidate, "Ippatsu"), Is.EqualTo("\u4E00\u767A"));
                Assert.That(driver.CandidateYakuHanName(candidate, "Ippatsu"), Is.EqualTo("One"));
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
                Assert.That(driver.CanDeclareWin(result), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_ReachCatalogMissingStillAddsIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Ippatsu", "One", "None")),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isIppatsuEligible: true);
                object candidate = driver.FindCandidateContainingYaku(result, "Ippatsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_IppatsuEligibleFalse_DoesNotAddIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIppatsuCatalog(driver),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true);

                AssertNoIppatsu(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ReachNotDeclared_DoesNotAddIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIppatsuCatalog(driver),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isIppatsuEligible: true);

                AssertNoIppatsu(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHand_DoesNotAddIppatsuEvenIfCatalogHasOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Ippatsu", "One", "One")),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isClosed: false,
                    isIppatsuEligible: true);

                AssertNoIppatsu(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_IppatsuCatalogMissing_DoesNotAddIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isIppatsuEligible: true);

                AssertNoIppatsu(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_IppatsuCatalogDisabled_DoesNotAddIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Ippatsu",
                            "One",
                            "None",
                            isEnabled: false)),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isIppatsuEligible: true);

                AssertNoIppatsu(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_IppatsuClosedHanNone_DoesNotAddIppatsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Ippatsu", "None", "None")),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isIppatsuEligible: true);

                AssertNoIppatsu(driver, result);
            }
        }

        private static object CreateIppatsuCatalog(WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(driver.CreateDefinition("Ippatsu", "One", "None"));
        }

        private static void AssertNoIppatsu(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Ippatsu"), Is.EqualTo(0));
        }
    }
}
