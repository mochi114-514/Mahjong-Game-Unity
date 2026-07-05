using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorChantaTests
    {
        private const string ChantaHonorTripletHand =
            "1m 2m 3m 7m 8m 9m 1p 1p 1p E E E 9s";
        private const string ChantaHonorPairHand =
            "1m 2m 3m 7m 8m 9m 1p 1p 1p 9s 9s 9s E";
        private const string JunchanHand =
            "1m 2m 3m 7m 8m 9m 1p 1p 1p 9s 9s 9s 1s";

        [Test]
        public void EvaluateWithTile_HonorTripletChantaShape_AddsChantaOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    ChantaHonorTripletHand,
                    "9s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chanta");

                AssertChantaCandidate(driver, result, candidate, 2);
                Assert.That(driver.CandidateContainsYaku(candidate, "Junchan"), Is.False);
                Assert.That(driver.CandidateYakuHanName(candidate, "Chanta"), Is.EqualTo("Two"));
            }
        }

        [Test]
        public void EvaluateWithTile_HonorPairChantaShape_AddsChantaOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    ChantaHonorPairHand,
                    "E",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chanta");

                AssertChantaCandidate(driver, result, candidate, 2);
                Assert.That(driver.CandidateContainsYaku(candidate, "Junchan"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_NoHonorJunchanShape_AddsJunchanOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    JunchanHand,
                    "1s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Junchan");

                AssertJunchanCandidate(driver, result, candidate, 3);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chanta"), Is.False);
                Assert.That(driver.CandidateYakuHanName(candidate, "Junchan"), Is.EqualTo("Three"));
            }
        }

        [TestCase(
            "1m 1m 1m 9m 9m 9m E E E P P P 1s",
            "1s")]
        [TestCase(
            "1m 1m 1m 9m 9m 9m 1p 1p 1p 9p 9p 9p 1s",
            "1s")]
        public void EvaluateWithTile_NoSequenceTerminalOrHonorShapes_DoNotAddChantaOrJunchan(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [TestCase(
            "2m 3m 4m 7m 8m 9m 1p 1p 1p E E E 9s",
            "9s")]
        [TestCase(
            "1m 2m 3m 4m 5m 6m 1p 1p 1p E E E 9s",
            "9s")]
        [TestCase(
            "1m 2m 3m 6m 7m 8m 1p 1p 1p E E E 9s",
            "9s")]
        public void EvaluateWithTile_MiddleSequenceStart_DoNotAddChantaOrJunchan(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_MiddleTriplet_DoNotAddChantaOrJunchan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    "1m 2m 3m 7m 8m 9m 2p 2p 2p E E E 9s",
                    "9s",
                    "Ron");

                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_MiddlePair_DoNotAddChantaOrJunchan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    "1m 2m 3m 7m 8m 9m 1p 1p 1p E E E 5s",
                    "5s",
                    "Ron");

                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddChantaOrJunchan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    "1m 1m 9m 9m 1p 1p 9p 9p E E P P C",
                    "C",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddChantaOrJunchan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_OpenChanta_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    ChantaHonorTripletHand,
                    "9s",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chanta");

                AssertChantaCandidate(driver, result, candidate, 1);
                Assert.That(driver.CandidateYakuHanName(candidate, "Chanta"), Is.EqualTo("One"));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenJunchan_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaCatalog(driver),
                    JunchanHand,
                    "1s",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Junchan");

                AssertJunchanCandidate(driver, result, candidate, 2);
                Assert.That(driver.CandidateYakuHanName(candidate, "Junchan"), Is.EqualTo("Two"));
            }
        }

        [TestCase("Missing", true)]
        [TestCase("Disabled", true)]
        [TestCase("ClosedHanNone", true)]
        [TestCase("OpenHanNone", false)]
        public void EvaluateWithTile_JunchanUnavailable_DoesNotFallbackToChanta(
            string unavailableReason,
            bool isClosed)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateJunchanUnavailableCatalog(driver, unavailableReason),
                    JunchanHand,
                    "1s",
                    "Ron",
                    isClosed: isClosed);

                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [TestCase("Missing", true)]
        [TestCase("Disabled", true)]
        [TestCase("ClosedHanNone", true)]
        [TestCase("OpenHanNone", false)]
        public void EvaluateWithTile_ChantaUnavailable_DoesNotSwitchToJunchan(
            string unavailableReason,
            bool isClosed)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaUnavailableCatalog(driver, unavailableReason),
                    ChantaHonorTripletHand,
                    "9s",
                    "Ron",
                    isClosed: isClosed);

                AssertNoChantaOrJunchan(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ChantaCombinesWithSanshokuDoujun()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaSanshokuCatalog(driver),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 9m",
                    "9m",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "Chanta",
                    "SanshokuDoujun");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Junchan"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
            }
        }

        [Test]
        public void EvaluateWithTile_JunchanCombinesWithSanshokuDoujun()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateChantaSanshokuCatalog(driver),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s 9m 9m 9m 9p",
                    "9p",
                    "Ron");
                object candidate = FindCandidateContainingAllYakus(
                    driver,
                    result,
                    "Junchan",
                    "SanshokuDoujun");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chanta"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(5));
            }
        }

        private static object CreateChantaCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Chanta", "Two", "One"),
                driver.CreateDefinition("Junchan", "Three", "Two"));
        }

        private static object CreateChantaSanshokuCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Chanta", "Two", "One"),
                driver.CreateDefinition("Junchan", "Three", "Two"),
                driver.CreateDefinition("SanshokuDoujun", "Two", "One"));
        }

        private static object CreateJunchanUnavailableCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chanta", "Two", "One"));
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chanta", "Two", "One"),
                        driver.CreateDefinition(
                            "Junchan",
                            "Three",
                            "Two",
                            isEnabled: false));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chanta", "Two", "One"),
                        driver.CreateDefinition("Junchan", "None", "Two"));
                case "OpenHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Chanta", "Two", "One"),
                        driver.CreateDefinition("Junchan", "Three", "None"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static object CreateChantaUnavailableCatalog(
            WinDeclarationEvaluatorTestDriver driver,
            string unavailableReason)
        {
            switch (unavailableReason)
            {
                case "Missing":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Junchan", "Three", "Two"));
                case "Disabled":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Junchan", "Three", "Two"),
                        driver.CreateDefinition(
                            "Chanta",
                            "Two",
                            "One",
                            isEnabled: false));
                case "ClosedHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Junchan", "Three", "Two"),
                        driver.CreateDefinition("Chanta", "None", "One"));
                case "OpenHanNone":
                    return driver.CreateCatalog(
                        driver.CreateDefinition("Junchan", "Three", "Two"),
                        driver.CreateDefinition("Chanta", "Two", "None"));
                default:
                    Assert.Fail("Unknown unavailable reason: " + unavailableReason);
                    return null;
            }
        }

        private static void AssertChantaCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, "Chanta"), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.ContainsYaku(result, "Chanta"), Is.False);
        }

        private static void AssertJunchanCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, "Junchan"), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.ContainsYaku(result, "Junchan"), Is.False);
        }

        private static void AssertNoChantaOrJunchan(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Chanta"), Is.EqualTo(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Junchan"), Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, "Chanta"), Is.False);
            Assert.That(driver.ContainsYaku(result, "Junchan"), Is.False);
        }

        private static object FindCandidateContainingAllYakus(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            params string[] yakuKindNames)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
            {
                object candidate = driver.CandidateResultAt(result, i);
                bool containsAll = true;

                for (int j = 0; j < yakuKindNames.Length; j++)
                {
                    if (!driver.CandidateContainsYaku(candidate, yakuKindNames[j]))
                    {
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll)
                    return candidate;
            }

            return null;
        }
    }
}
