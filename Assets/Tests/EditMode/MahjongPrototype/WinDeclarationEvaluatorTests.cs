using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WinDeclarationEvaluatorTests
    {
        [Test]
        public void WinCheckerCanWinWithTile_RemainsShapeOnlyWithoutYakuCatalog()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                bool canWin = driver.CanWinWithTileShapeOnly(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C");

                Assert.That(canWin, Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsFalseWhenShapeIsMissing()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E S W C",
                    "5m",
                    "Ron");

                Assert.That(driver.IsWinningShape(result), Is.False);
                Assert.That(driver.HasYaku(result), Is.False);
                Assert.That(driver.CanDeclareWin(result), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsFalseWhenShapeHasNoRegisteredYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Ron");

                Assert.That(driver.IsWinningShape(result), Is.True);
                Assert.That(driver.HasYaku(result), Is.False);
                Assert.That(driver.CanDeclareWin(result), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_KeepsDetailedStandardAnalysisInResult()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E",
                    "4m",
                    "Ron");

                Assert.That(driver.IsWinningShape(result), Is.True);
                Assert.That(driver.HasYaku(result), Is.False);
                Assert.That(driver.CanDeclareWin(result), Is.False);
                Assert.That(driver.AnalysisCanWin(result), Is.True);
                Assert.That(driver.AnalysisStandardDecompositionCount(result), Is.GreaterThan(0));
                Assert.That(driver.AnalysisStandardWinningInterpretationCount(result), Is.GreaterThan(0));
                Assert.That(driver.AnalysisHasWaitType(result, "Ryanmen"), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_KeepsTankiWaitAnalysisInResult()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s E",
                    "E",
                    "Ron");

                Assert.That(driver.IsWinningShape(result), Is.True);
                Assert.That(driver.CanDeclareWin(result), Is.False);
                Assert.That(driver.AnalysisHasWaitType(result, "Tanki"), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_KeepsNotWinAnalysisWhenShapeIsMissing()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E S W C",
                    "5m",
                    "Ron");

                Assert.That(driver.IsWinningShape(result), Is.False);
                Assert.That(driver.CanDeclareWin(result), Is.False);
                Assert.That(driver.WinningHandAnalysis(result), Is.Not.Null);
                Assert.That(driver.AnalysisCanWin(result), Is.False);
                Assert.That(driver.AnalysisStandardWinningInterpretationCount(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsTrueForRegisteredTanyao()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Tanyao", "One", "One")),
                    "2m 3m 4m 2p 3p 4p 2s 3s 4s 6s 7s 8s 5m",
                    "5m",
                    "Ron");

                AssertCanDeclareWithTotalHan(driver, result, 1);
                Assert.That(driver.ContainsYaku(result, "Tanyao"), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsTrueForRegisteredSevenPairs()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C",
                    "Ron");

                AssertCanDeclareWithTotalHan(driver, result, 2);
                Assert.That(driver.ContainsYaku(result, "SevenPairs"), Is.True);
                Assert.That(driver.AnalysisSevenPairsIsWin(result), Is.True);
                Assert.That(driver.AnalysisStandardWinningInterpretationCount(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsYakumanForRegisteredKokushiMusou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("KokushiMusou", "None", "None", true)),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CanDeclareWin(result), Is.True);
                Assert.That(driver.HandEvaluationHasYakuman(result), Is.True);
                Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
                Assert.That(driver.ContainsYaku(result, "KokushiMusou"), Is.True);
                Assert.That(driver.AnalysisThirteenOrphansIsWin(result), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsReachWhenReachIsDeclared()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Reach", "One", "None")),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Ron",
                    isReachDeclared: true);

                AssertCanDeclareWithTotalHan(driver, result, 1);
                Assert.That(driver.ContainsYaku(result, "Reach"), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_ReturnsMenzenTsumoForClosedTsumo()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Tsumo");

                AssertCanDeclareWithTotalHan(driver, result, 1);
                Assert.That(driver.ContainsYaku(result, "MenzenTsumo"), Is.True);
            }
        }

        [Test]
        public void ExistingConstructors_RemainCompatible()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                Assert.That(driver.CanWinWithTileShapeOnly(
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C"), Is.True);
                Assert.That(driver.CreateWinDeclarationEvaluatorWithEmptyCatalog(), Is.Not.Null);
                Assert.That(driver.CreateLegacyHandEvaluationContext(), Is.Not.Null);
                Assert.That(driver.CreateLegacyWinDeclarationEvaluationResult(), Is.Not.Null);
            }
        }

        private static void AssertCanDeclareWithTotalHan(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.HasYaku(result), Is.True);
            Assert.That(driver.CanDeclareWin(result), Is.True);
            Assert.That(driver.TotalHan(result), Is.EqualTo(expectedTotalHan));
            Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
        }
    }
}
