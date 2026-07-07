using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorBasicYakuCoverageTests
    {
        private const string BasicWinningHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";
        private const string TanyaoHand =
            "2m 3m 4m 2p 3p 4p 2s 3s 4s 6s 7s 8s 5m";

        [Test]
        public void EvaluateWithTile_DoesNotReturnMenzenTsumoForRon()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    BasicWinningHand,
                    "C",
                    "Ron");

                AssertNoYakuOnWinningShape(driver, result, "MenzenTsumo");
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotReturnMenzenTsumoForOpenTsumo()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("MenzenTsumo", "One", "One")),
                    BasicWinningHand,
                    "C",
                    "Tsumo",
                    isClosed: false);

                AssertNoYakuOnWinningShape(driver, result, "MenzenTsumo");
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsTanyaoForOpenHandContext()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Tanyao", "One", "One")),
                    TanyaoHand,
                    "5m",
                    "Ron",
                    isClosed: false);
                object tanyaoCandidate = driver.FindCandidateContainingYaku(result, "Tanyao");

                Assert.That(driver.IsWinningShape(result), Is.True);
                Assert.That(tanyaoCandidate, Is.Not.Null);
                Assert.That(driver.CandidateYakuHanName(tanyaoCandidate, "Tanyao"), Is.EqualTo("One"));
                Assert.That(driver.CandidateTotalHan(tanyaoCandidate), Is.EqualTo(1));
                Assert.That(driver.HasYaku(result), Is.True);
                Assert.That(driver.CanDeclareWin(result), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotReturnTanyaoWhenHandContainsTerminal()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateTanyaoCatalog(driver),
                    "1m 2m 3m 2p 3p 4p 2s 3s 4s 6s 7s 8s 5m",
                    "5m",
                    "Ron");

                AssertNoYakuOnWinningShape(driver, result, "Tanyao");
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotReturnTanyaoWhenHandContainsHonor()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateTanyaoCatalog(driver),
                    "2m 3m 4m 2p 3p 4p 2s 3s 4s 6s 7s 8s E",
                    "E",
                    "Ron");

                AssertNoYakuOnWinningShape(driver, result, "Tanyao");
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotReturnTanyaoWhenWinningTileIsTerminal()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateTanyaoCatalog(driver),
                    "2m 3m 2p 3p 4p 2s 3s 4s 6s 7s 8s 5m 5m",
                    "1m",
                    "Ron");

                AssertNoYakuOnWinningShape(driver, result, "Tanyao");
            }
        }

        private static object CreateTanyaoCatalog(WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(driver.CreateDefinition("Tanyao", "One", "One"));
        }

        private static void AssertNoYakuOnWinningShape(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, yakuKindName), Is.EqualTo(0));
            Assert.That(driver.HasYaku(result), Is.False);
            Assert.That(driver.CanDeclareWin(result), Is.False);
        }
    }

    public sealed class HandEvaluatorUnimplementedYakuGuardTests
    {
        private const string BasicWinningHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";

        [TestCase("RinshanKaihou", "Tsumo")]
        [TestCase("Chankan", "Ron")]
        [TestCase("Toitoi", "Ron")]
        [TestCase("Sankantsu", "Ron")]
        [TestCase("Suukantsu", "Ron")]
        [TestCase("Renhou", "Ron")]
        public void EvaluateWithTile_DefinitionAloneDoesNotEmitCurrentlyUnimplementedYaku(
            string yakuKindName,
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition(yakuKindName, "One", "One")),
                    BasicWinningHand,
                    "C",
                    winTypeName);

                AssertNoYakuOnWinningShape(driver, result, yakuKindName);
            }
        }

        private static void AssertNoYakuOnWinningShape(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
            Assert.That(driver.CountCandidatesContainingYaku(result, yakuKindName), Is.EqualTo(0));
            Assert.That(driver.HasYaku(result), Is.False);
            Assert.That(driver.CanDeclareWin(result), Is.False);
        }
    }
}
