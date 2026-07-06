using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorLastTileYakuTests
    {
        private const string StandardHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";
        private const string SevenPairsHand =
            "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E P";
        private const string ThirteenOrphansHand =
            "1m 9m 1p 9p 1s 9s E S W N P F C";

        [Test]
        public void EvaluateWithTile_LastDrawStandardTsumo_AddsHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isLastLiveWallDraw: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "HaiteiRaoyue");

                AssertSingleLastTileYaku(
                    driver,
                    candidate,
                    "HaiteiRaoyue",
                    "HouteiRaoyui");
            }
        }

        [Test]
        public void EvaluateWithTile_LastDrawSevenPairsTsumo_AddsHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HaiteiRaoyue", "One", "One"),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    SevenPairsHand,
                    "P",
                    "Tsumo",
                    isLastLiveWallDraw: true);
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "HaiteiRaoyue"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "HouteiRaoyui"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
            }
        }

        [Test]
        public void EvaluateWithTile_LastDrawThirteenOrphansTsumo_AddsHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HaiteiRaoyue", "One", "One")),
                    ThirteenOrphansHand,
                    "E",
                    "Tsumo",
                    isLastLiveWallDraw: true);
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "HaiteiRaoyue"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "HouteiRaoyui"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_LastLiveWallDiscardRon_AddsHoutei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    "Ron",
                    isLastLiveWallDiscard: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "HouteiRaoyui");

                AssertSingleLastTileYaku(
                    driver,
                    candidate,
                    "HouteiRaoyui",
                    "HaiteiRaoyue");
            }
        }

        [Test]
        public void EvaluateWithTile_NormalTsumo_DoesNotAddHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo");

                AssertNoLastTileYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_NormalRon_DoesNotAddHoutei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    "Ron");

                AssertNoLastTileYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_LastDrawFlagOnRon_DoesNotAddHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    "Ron",
                    isLastLiveWallDraw: true);

                AssertNoLastTileYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_LastDiscardFlagOnTsumo_DoesNotAddHoutei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isLastLiveWallDiscard: true);

                AssertNoLastTileYakuInResult(driver, result);
            }
        }

        [TestCase("Tsumo", true, false, "HaiteiRaoyue")]
        [TestCase("Ron", false, true, "HouteiRaoyui")]
        public void EvaluateWithTile_OpenHand_AddsLastTileYaku(
            string winTypeName,
            bool isLastLiveWallDraw,
            bool isLastLiveWallDiscard,
            string expectedYakuKindName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    winTypeName,
                    isClosed: false,
                    isLastLiveWallDraw: isLastLiveWallDraw,
                    isLastLiveWallDiscard: isLastLiveWallDiscard);
                object candidate =
                    driver.FindCandidateContainingYaku(result, expectedYakuKindName);

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_HaiteiCombinesWithMenzenTsumo()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HaiteiRaoyue", "One", "One"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isLastLiveWallDraw: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "HaiteiRaoyue");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_HouteiCombinesWithReach()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HouteiRaoyui", "One", "One"),
                        driver.CreateDefinition("Reach", "One", "None")),
                    StandardHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isLastLiveWallDiscard: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "HouteiRaoyui");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_KokushiYakuman_RemovesHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HaiteiRaoyue", "One", "One"),
                        driver.CreateDefinition(
                            "KokushiMusou",
                            "None",
                            "None",
                            isYakuman: true)),
                    ThirteenOrphansHand,
                    "E",
                    "Tsumo",
                    isLastLiveWallDraw: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "KokushiMusou");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "HaiteiRaoyue"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_HaiteiMissing_KeepsMenzenTsumoAndDoesNotFallbackToHoutei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HouteiRaoyui", "One", "One"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    StandardHand,
                    "C",
                    "Tsumo",
                    isLastLiveWallDraw: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "MenzenTsumo");

                Assert.That(candidate, Is.Not.Null);
                AssertNoLastTileYaku(driver, candidate);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_HouteiDisabled_KeepsReachAndDoesNotFallbackToHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("HaiteiRaoyue", "One", "One"),
                        driver.CreateDefinition(
                            "HouteiRaoyui",
                            "One",
                            "One",
                            isEnabled: false),
                        driver.CreateDefinition("Reach", "One", "None")),
                    StandardHand,
                    "C",
                    "Ron",
                    isReachDeclared: true,
                    isLastLiveWallDiscard: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Reach");

                Assert.That(candidate, Is.Not.Null);
                AssertNoLastTileYaku(driver, candidate);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [TestCase("Tsumo", true, true, "HaiteiRaoyue", "HouteiRaoyui")]
        [TestCase("Ron", true, true, "HouteiRaoyui", "HaiteiRaoyue")]
        public void EvaluateWithTile_BothFlagsTrue_AddsOnlyWinTypeMatchedYaku(
            string winTypeName,
            bool isLastLiveWallDraw,
            bool isLastLiveWallDiscard,
            string expectedYakuKindName,
            string unexpectedYakuKindName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateLastTileCatalog(driver),
                    StandardHand,
                    "C",
                    winTypeName,
                    isLastLiveWallDraw: isLastLiveWallDraw,
                    isLastLiveWallDiscard: isLastLiveWallDiscard);
                object candidate =
                    driver.FindCandidateContainingYaku(result, expectedYakuKindName);

                Assert.That(candidate, Is.Not.Null);
                Assert.That(
                    driver.CandidateContainsYaku(candidate, unexpectedYakuKindName),
                    Is.False);
            }
        }

        [Test]
        public void ExistingConstructors_DefaultLastTileFlagsFalse()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object handContext = driver.CreateLegacyHandEvaluationContext();
                object winContext = driver.CreateLegacyWinDeclarationEvaluationContext();

                Assert.That(driver.IsLastLiveWallDraw(handContext), Is.False);
                Assert.That(driver.IsLastLiveWallDiscard(handContext), Is.False);
                Assert.That(driver.IsLastLiveWallDraw(winContext), Is.False);
                Assert.That(driver.IsLastLiveWallDiscard(winContext), Is.False);
            }
        }

        [Test]
        public void NewConstructors_StoreLastTileFlagsTrue()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object handContext = driver.CreateLastLiveWallDrawHandEvaluationContext();
                object winContext =
                    driver.CreateLastLiveWallDiscardWinDeclarationEvaluationContext();

                Assert.That(driver.IsLastLiveWallDraw(handContext), Is.True);
                Assert.That(driver.IsLastLiveWallDiscard(handContext), Is.False);
                Assert.That(driver.IsLastLiveWallDraw(winContext), Is.False);
                Assert.That(driver.IsLastLiveWallDiscard(winContext), Is.True);
            }
        }

        private static object CreateLastTileCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("HaiteiRaoyue", "One", "One"),
                driver.CreateDefinition("HouteiRaoyui", "One", "One"));
        }

        private static void AssertSingleLastTileYaku(
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
            Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
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

        private static void AssertNoLastTileYakuInResult(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
                AssertNoLastTileYaku(driver, driver.CandidateResultAt(result, i));
        }

        private static void AssertNoLastTileYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "HaiteiRaoyue"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "HouteiRaoyui"), Is.False);
        }
    }
}
