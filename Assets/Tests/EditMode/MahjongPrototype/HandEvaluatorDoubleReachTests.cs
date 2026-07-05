using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorDoubleReachTests
    {
        private const string BasicWinningHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";

        [Test]
        public void EvaluateWithTile_DoubleReachDeclared_AddsDoubleReachAndSuppressesReach()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateReach(
                    driver,
                    CreateReachCatalog(driver),
                    isReachDeclared: true,
                    isDoubleReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "DoubleReach");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_NormalReachDeclared_AddsReachOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateReach(
                    driver,
                    CreateReachCatalog(driver),
                    isReachDeclared: true,
                    isDoubleReachDeclared: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Reach");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "DoubleReach"),
                    Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_DoubleReachWithoutReachDeclared_DoesNotAddReachYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateReach(
                    driver,
                    CreateReachCatalog(driver),
                    isReachDeclared: false,
                    isDoubleReachDeclared: true);

                AssertNoCandidateYaku(driver, result, "DoubleReach");
                AssertNoCandidateYaku(driver, result, "Reach");
            }
        }

        [TestCase("Missing")]
        [TestCase("Disabled")]
        [TestCase("ClosedHanNone")]
        public void EvaluateWithTile_DoubleReachUnavailable_FallsBackToReach(
            string unavailableReason)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = EvaluateReach(
                    driver,
                    CreateFallbackCatalog(driver, unavailableReason),
                    isReachDeclared: true,
                    isDoubleReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Reach");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "DoubleReach"),
                    Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_DoubleReachAndIppatsu_AddsBothWithoutReach()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("DoubleReach", "Two", "None"),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("Ippatsu", "One", "None")),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isIppatsuEligible: true,
                    isDoubleReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "DoubleReach");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Ippatsu"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenHand_DoesNotAddReachOrDoubleReach()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("DoubleReach", "Two", "Two"),
                        driver.CreateDefinition("Reach", "One", "One")),
                    BasicWinningHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isClosed: false,
                    isDoubleReachDeclared: true);

                AssertNoCandidateYaku(driver, result, "DoubleReach");
                AssertNoCandidateYaku(driver, result, "Reach");
            }
        }

        [Test]
        public void LegacyConstructors_DefaultDoubleReachFalseAndNewConstructorsStoreTrue()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                Assert.That(
                    driver.IsDoubleReachDeclared(driver.CreateLegacyHandEvaluationContext()),
                    Is.False);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateLegacyHandEvaluationContextWithIppatsu()),
                    Is.False);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateLegacyDetailedHandEvaluationContext()),
                    Is.False);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateLegacyDetailedHandEvaluationContextWithIppatsu()),
                    Is.False);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateLegacyWinDeclarationEvaluationContext()),
                    Is.False);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateLegacyWinDeclarationEvaluationContextWithIppatsu()),
                    Is.False);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateDoubleReachHandEvaluationContext()),
                    Is.True);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateDoubleReachDetailedHandEvaluationContext()),
                    Is.True);
                Assert.That(
                    driver.IsDoubleReachDeclared(
                        driver.CreateDoubleReachWinDeclarationEvaluationContext()),
                    Is.True);
            }
        }

        private static object EvaluateReach(
            WinDeclarationEvaluatorTestDriver driver,
            object catalog,
            bool isReachDeclared,
            bool isDoubleReachDeclared)
        {
            return driver.EvaluateWithTile(
                catalog,
                BasicWinningHand,
                "C",
                "Ron",
                isReachDeclared: isReachDeclared,
                isDoubleReachDeclared: isDoubleReachDeclared);
        }

        private static object CreateReachCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("DoubleReach", "Two", "None"),
                driver.CreateDefinition("Reach", "One", "None"));
        }

        private static object CreateFallbackCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"));
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition(
                            "DoubleReach",
                            "Two",
                            "None",
                            isEnabled: false));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("DoubleReach", "None", "None"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static void AssertNoCandidateYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(
                driver.CountCandidatesContainingYaku(result, yakuKindName),
                Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, yakuKindName), Is.False);
        }
    }
}
