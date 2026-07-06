using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorRyuuiisouTests
    {
        private const string GreenWithHatsuPairHand =
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s F";
        private const string GreenWithHatsuTripletHand =
            "2s 3s 4s 2s 3s 4s 6s 6s 6s F F F 8s";
        private const string GreenWithoutHatsuHand =
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 2s";

        [Test]
        public void EvaluateWithTile_GreenTilesWithHatsu_AddsRyuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateRyuuiisouCatalog(driver),
                    GreenWithHatsuPairHand,
                    "F",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ryuuiisou");

                AssertRyuuiisouCandidate(driver, result, candidate);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_GreenTilesWithoutHatsu_AddsRyuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateRyuuiisouCatalog(driver),
                    GreenWithoutHatsuHand,
                    "2s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ryuuiisou");

                AssertRyuuiisouCandidate(driver, result, candidate);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_OpenGreenTiles_AddsRyuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateRyuuiisouCatalog(driver),
                    GreenWithHatsuPairHand,
                    "F",
                    "Ron",
                    isClosed: false);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ryuuiisou");

                AssertRyuuiisouCandidate(driver, result, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_RyuuiisouWithNormalYaku_LeavesOnlyYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Ryuuiisou",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition("YakuhaiGreenDragon", "One", "One"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None"),
                        driver.CreateDefinition("Reach", "One", "None")),
                    GreenWithHatsuTripletHand,
                    "8s",
                    "Tsumo",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ryuuiisou");

                AssertRyuuiisouCandidate(driver, result, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Honitsu"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "YakuhaiGreenDragon"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_RyuuiisouWithoutHatsu_RemovesChinitsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Ryuuiisou",
                            "None",
                            "None",
                            isYakuman: true),
                        driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    GreenWithoutHatsuHand,
                    "2s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Ryuuiisou");

                AssertRyuuiisouCandidate(driver, result, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Chinitsu"), Is.False);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
            }
        }

        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 1s",
            "1s")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 5s",
            "5s")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 7s",
            "7s")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 9s",
            "9s")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 1m",
            "1m")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 1p",
            "1p")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s E",
            "E")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s P",
            "P")]
        [TestCase(
            "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s C",
            "C")]
        public void EvaluateWithTile_NonGreenTiles_DoNotAddRyuuiisou(
            string handText,
            string winningTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateRyuuiisouCatalog(driver),
                    handText,
                    winningTileCode,
                    "Ron");

                AssertNoRyuuiisou(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddRyuuiisou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateRyuuiisouCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoRyuuiisou(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_RyuuiisouMissing_KeepsChinitsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Chinitsu", "Six", "Five")),
                    GreenWithoutHatsuHand,
                    "2s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Chinitsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Ryuuiisou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(6));
            }
        }

        [Test]
        public void EvaluateWithTile_RyuuiisouDisabled_KeepsHonitsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Honitsu", "Three", "Two"),
                        driver.CreateDefinition(
                            "Ryuuiisou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false)),
                    GreenWithHatsuPairHand,
                    "F",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Honitsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Ryuuiisou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
            }
        }

        private static object CreateRyuuiisouCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition(
                    "Ryuuiisou",
                    "None",
                    "None",
                    isYakuman: true));
        }

        private static void AssertRyuuiisouCandidate(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateContainsYaku(candidate, "Ryuuiisou"), Is.True);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, "Ryuuiisou"), Is.False);
        }

        private static void AssertNoRyuuiisou(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, "Ryuuiisou"), Is.EqualTo(0));
            Assert.That(driver.ContainsYaku(result, "Ryuuiisou"), Is.False);
        }
    }
}
