using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorIttsuuTests
    {
        private const string ManIttsuuHand =
            "1m 2m 3m 4m 5m 6m 7m 8m 9m 2p 3p 4p 5s";

        [TestCase(
            ManIttsuuHand,
            "5s")]
        [TestCase(
            "1p 2p 3p 4p 5p 6p 7p 8p 9p 2m 3m 4m 5s",
            "5s")]
        [TestCase(
            "1s 2s 3s 4s 5s 6s 7s 8s 9s 2m 3m 4m 5p",
            "5p")]
        public void EvaluateWithTile_SameSuitOneThroughNineSequences_AddsIttsuu(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIttsuuCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ittsuu");

                AssertIttsuuCandidate(driver, result, candidate, 2);
            }
        }

        [TestCase(
            "1m 2m 3m 4m 5m 6m 7p 8p 9p 2s 3s 4s 5s",
            "5s")]
        [TestCase(
            "1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s 5s",
            "5s")]
        [TestCase(
            "4m 5m 6m 7m 8m 9m 2p 3p 4p 6s 7s 8s 5s",
            "5s")]
        [TestCase(
            "1m 2m 3m 7m 8m 9m 2p 3p 4p 6s 7s 8s 5s",
            "5s")]
        [TestCase(
            "1m 1m 1m 4m 5m 6m 7m 8m 9m 2p 3p 4p 5s",
            "5s")]
        [TestCase(
            "1m 2m 3m 1m 2m 3m 4p 5p 6p 7s 8s 9s 5s",
            "5s")]
        public void EvaluateWithTile_NonIttsuuShapes_DoNotAddIttsuu(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIttsuuCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, "Ittsuu");
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddIttsuu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIttsuuCatalog(driver),
                    "1m 1m 2m 2m 3m 3m 4p 4p 5p 5p 6s 6s 7s",
                    "7s",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "Ittsuu");
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddIttsuu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIttsuuCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "Ittsuu");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenIttsuu_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIttsuuCatalog(driver),
                    ManIttsuuHand,
                    "5s",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ittsuu");

                AssertIttsuuCandidate(driver, result, candidate, 1);
                Assert.That(driver.CandidateYakuHanName(candidate, "Ittsuu"), Is.EqualTo("One"));
            }
        }

        [TestCase("Missing", true)]
        [TestCase("Disabled", true)]
        [TestCase("ClosedHanNone", true)]
        [TestCase("OpenHanNone", false)]
        public void EvaluateWithTile_IttsuuUnavailable_DoesNotAddIttsuu(
            string unavailableReason,
            bool isClosed)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateIttsuuUnavailableCatalog(driver, unavailableReason),
                    ManIttsuuHand,
                    "5s",
                    "Ron",
                    isClosed: isClosed);

                AssertNoCandidateYaku(driver, result, "Ittsuu");
            }
        }

        [Test]
        public void EvaluateWithTile_IttsuuCombinesWithReachInSameCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Ittsuu", "Two", "One"),
                        driver.CreateDefinition("Reach", "One", "None")),
                    ManIttsuuHand,
                    "5s",
                    "Ron",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ittsuu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
            }
        }

        private static object CreateIttsuuCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Ittsuu", "Two", "One"));
        }

        private static object CreateIttsuuUnavailableCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return driver.CreateCatalog();
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Ittsuu",
                            "Two",
                            "One",
                            isEnabled: false));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Ittsuu", "None", "One"));
                case "OpenHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Ittsuu", "Two", "None"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static void AssertIttsuuCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, "Ittsuu"), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.ContainsYaku(result, "Ittsuu"), Is.False);
        }

        private static void AssertNoCandidateYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(
                driver.CountCandidatesContainingYaku(result, yakuKindName),
                Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, yakuKindName), Is.False);
        }
    }
}
