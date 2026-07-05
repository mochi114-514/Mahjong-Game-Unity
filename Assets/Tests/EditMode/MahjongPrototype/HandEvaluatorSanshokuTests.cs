using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorSanshokuTests
    {
        private const string SanshokuDoujunRankOneHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s 7m 8m 9m 5p";
        private const string SanshokuDoujunRankFourHand =
            "4m 5m 6m 4p 5p 6p 4s 5s 6s 1m 2m 3m 7p";
        private const string SanshokuDoukouRankFiveHand =
            "5m 5m 5m 5p 5p 5p 5s 5s 5s 1m 2m 3m 7p";

        [TestCase(SanshokuDoujunRankOneHand, "5p", 2)]
        [TestCase(SanshokuDoujunRankFourHand, "7p", 2)]
        public void EvaluateWithTile_SameStartSequencesInThreeSuits_AddsSanshokuDoujun(
            string handText,
            string winningTileCode,
            int expectedHan)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SanshokuDoujun");

                AssertSanshokuCandidate(
                    driver,
                    result,
                    candidate,
                    "SanshokuDoujun",
                    expectedHan);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "SanshokuDoukou"),
                    Is.False);
            }
        }

        [TestCase(
            "1m 2m 3m 2p 3p 4p 1s 2s 3s 7m 8m 9m 5p",
            "5p")]
        [TestCase(
            "1m 2m 3m 1p 2p 3p 4m 5m 6m 7s 8s 9s 5p",
            "5p")]
        [TestCase(
            "1m 2m 3m 1m 2m 3m 1m 2m 3m 4p 5p 6p 5s",
            "5s")]
        [TestCase(
            "1m 2m 3m 1p 2p 3p 1s 1s 1s 7m 8m 9m 5p",
            "5p")]
        public void EvaluateWithTile_NonMatchingSequences_DoNotAddSanshokuDoujun(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, "SanshokuDoujun");
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddSanshokuDoujun()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "SanshokuDoujun");
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddSanshokuDoujun()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "SanshokuDoujun");
            }
        }

        [Test]
        public void EvaluateWithTile_SameRankTripletsInThreeSuits_AddsSanshokuDoukou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    SanshokuDoukouRankFiveHand,
                    "7p",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SanshokuDoukou");

                AssertSanshokuCandidate(
                    driver,
                    result,
                    candidate,
                    "SanshokuDoukou",
                    2);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, "SanshokuDoujun"),
                    Is.False);
            }
        }

        [TestCase(
            "5m 5m 5m 5p 5p 5p 6s 6s 6s 1m 2m 3m 7p",
            "7p")]
        [TestCase(
            "5m 5m 5m 5p 5p 5p 6m 6m 6m 1s 2s 3s 7p",
            "7p")]
        [TestCase(
            "5m 5m 5m 5p 5p 5p E E E 1s 2s 3s 7p",
            "7p")]
        [TestCase(
            "5m 5m 5m 5p 5p 5p 5s 6s 7s 1m 2m 3m 7p",
            "7p")]
        public void EvaluateWithTile_NonMatchingTriplets_DoNotAddSanshokuDoukou(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, "SanshokuDoukou");
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddSanshokuDoukou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "SanshokuDoukou");
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddSanshokuDoukou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateSanshokuCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoCandidateYaku(driver, result, "SanshokuDoukou");
            }
        }

        [Test]
        public void EvaluateWithTile_SanshokuDoujunOpenHand_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("SanshokuDoujun", "Two", "One")),
                    SanshokuDoujunRankOneHand,
                    "5p",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SanshokuDoujun");

                AssertSanshokuCandidate(
                    driver,
                    result,
                    candidate,
                    "SanshokuDoujun",
                    1);
                Assert.That(
                    driver.CandidateYakuHanName(candidate, "SanshokuDoujun"),
                    Is.EqualTo("One"));
            }
        }

        [Test]
        public void EvaluateWithTile_SanshokuDoukouOpenHand_UsesOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("SanshokuDoukou", "Three", "Two")),
                    SanshokuDoukouRankFiveHand,
                    "7p",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SanshokuDoukou");

                AssertSanshokuCandidate(
                    driver,
                    result,
                    candidate,
                    "SanshokuDoukou",
                    2);
                Assert.That(
                    driver.CandidateYakuHanName(candidate, "SanshokuDoukou"),
                    Is.EqualTo("Two"));
            }
        }

        [TestCase("SanshokuDoujun", SanshokuDoujunRankOneHand, "5p")]
        [TestCase("SanshokuDoukou", SanshokuDoukouRankFiveHand, "7p")]
        public void EvaluateWithTile_SanshokuDefinitionMissing_DoesNotAddYaku(
            string yakuKindName,
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, yakuKindName);
            }
        }

        [TestCase("SanshokuDoujun", SanshokuDoujunRankOneHand, "5p")]
        [TestCase("SanshokuDoukou", SanshokuDoukouRankFiveHand, "7p")]
        public void EvaluateWithTile_SanshokuDefinitionDisabled_DoesNotAddYaku(
            string yakuKindName,
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            yakuKindName,
                            "Two",
                            "One",
                            isEnabled: false)),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, yakuKindName);
            }
        }

        [TestCase("SanshokuDoujun", SanshokuDoujunRankOneHand, "5p")]
        [TestCase("SanshokuDoukou", SanshokuDoukouRankFiveHand, "7p")]
        public void EvaluateWithTile_SanshokuClosedHanNone_DoesNotAddYakuWhenClosed(
            string yakuKindName,
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(yakuKindName, "None", "One")),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoCandidateYaku(driver, result, yakuKindName);
            }
        }

        [TestCase("SanshokuDoujun", SanshokuDoujunRankOneHand, "5p")]
        [TestCase("SanshokuDoukou", SanshokuDoukouRankFiveHand, "7p")]
        public void EvaluateWithTile_SanshokuOpenHanNone_DoesNotAddYakuWhenOpen(
            string yakuKindName,
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(yakuKindName, "Two", "None")),
                    handText,
                    winningTileCode,
                    "Ron",
                    isClosed: false);

                AssertNoCandidateYaku(driver, result, yakuKindName);
            }
        }

        private static object CreateSanshokuCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("SanshokuDoujun", "Two", "One"),
                driver.CreateDefinition("SanshokuDoukou", "Two", "Two"));
        }

        private static void AssertSanshokuCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            string yakuKindName,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateContainsYaku(candidate, yakuKindName), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.ContainsYaku(result, yakuKindName), Is.False);
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
