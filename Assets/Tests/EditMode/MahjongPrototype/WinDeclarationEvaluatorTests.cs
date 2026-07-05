using System;
using System.Collections;
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
        public void EvaluateWithTile_CreatesStandardCandidateForEachStandardInterpretation()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E",
                    "4m",
                    "Ron");

                int interpretationCount =
                    driver.AnalysisStandardWinningInterpretationCount(result);

                Assert.That(interpretationCount, Is.GreaterThan(0));
                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(interpretationCount));
                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.EqualTo(interpretationCount));

                for (int i = 0; i < driver.CandidateResultCount(result); i++)
                {
                    object candidate = driver.CandidateResultAt(result, i);
                    Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                    Assert.That(driver.CandidateHasStandardInterpretation(candidate), Is.True);
                }
            }
        }

        [Test]
        public void EvaluateWithTile_KeepsMultipleStandardCandidatesWithoutChoosingOne()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m",
                    "7m",
                    "Ron");

                int standardInterpretationCount =
                    driver.AnalysisStandardWinningInterpretationCount(result);

                Assert.That(standardInterpretationCount, Is.GreaterThan(1));
                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.EqualTo(standardInterpretationCount));
                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(standardInterpretationCount + 1));
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
                object tanyaoCandidate = driver.FindCandidateContainingYaku(result, "Tanyao");

                AssertCanDeclareWithCandidateTotalHan(driver, result, tanyaoCandidate, 1);
                Assert.That(driver.ContainsYaku(result, "Tanyao"), Is.False);
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
                object sevenPairsCandidate = driver.FindCandidateContainingYaku(result, "SevenPairs");

                AssertCanDeclareWithCandidateTotalHan(driver, result, sevenPairsCandidate, 2);
                Assert.That(driver.ContainsYaku(result, "SevenPairs"), Is.False);
                Assert.That(driver.AnalysisSevenPairsIsWin(result), Is.True);
                Assert.That(driver.AnalysisStandardWinningInterpretationCount(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_CreatesSevenPairsCandidateOnlyForSevenPairsInterpretation()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C",
                    "Ron");

                object sevenPairsCandidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(1));
                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.EqualTo(0));
                Assert.That(driver.CandidateSevenPairsIsWin(sevenPairsCandidate), Is.True);
                Assert.That(driver.CandidateContainsYaku(sevenPairsCandidate, "SevenPairs"), Is.True);
                Assert.That(driver.CandidateTotalHan(sevenPairsCandidate), Is.EqualTo(2));
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
                object kokushiCandidate = driver.FindCandidateContainingYaku(result, "KokushiMusou");

                Assert.That(driver.CanDeclareWin(result), Is.True);
                Assert.That(driver.HandEvaluationHasYakuman(result), Is.True);
                Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
                Assert.That(driver.TotalHan(result), Is.EqualTo(0));
                Assert.That(driver.TopLevelYakuCount(result), Is.EqualTo(0));
                Assert.That(driver.ContainsYaku(result, "KokushiMusou"), Is.False);
                Assert.That(kokushiCandidate, Is.Not.Null);
                Assert.That(driver.CandidateHasYakuman(kokushiCandidate), Is.True);
                Assert.That(driver.AnalysisThirteenOrphansIsWin(result), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_CreatesThirteenOrphansCandidateWithKokushiYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("KokushiMusou", "None", "None", true)),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                object thirteenOrphansCandidate =
                    FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(1));
                Assert.That(driver.CandidateThirteenOrphansIsWin(thirteenOrphansCandidate), Is.True);
                Assert.That(driver.CandidateContainsYaku(thirteenOrphansCandidate, "KokushiMusou"), Is.True);
                Assert.That(driver.CandidateHasYakuman(thirteenOrphansCandidate), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_EvaluatesStandardAndSevenPairsCandidatesIndependently()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("Tanyao", "One", "One"),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m 8m",
                    "8m",
                    "Ron",
                    isReachDeclared: true);

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.GreaterThan(0));
                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));

                for (int i = 0; i < driver.CandidateResultCount(result); i++)
                {
                    object candidate = driver.CandidateResultAt(result, i);
                    Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                    Assert.That(driver.CandidateContainsYaku(candidate, "Tanyao"), Is.True);

                    if (driver.CandidateTypeName(candidate) == "SevenPairs")
                    {
                        Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.True);
                        Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(4));
                    }
                    else
                    {
                        Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
                        Assert.That(driver.CandidateContainsYaku(candidate, "SevenPairs"), Is.False);
                        Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
                    }
                }
            }
        }

        [Test]
        public void EvaluateWithTile_UsesYakuCandidateWhenRepresentativeShapeDiffers()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m 8m",
                    "8m",
                    "Ron");
                object sevenPairsCandidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(driver.IsWinningShape(result), Is.True);
                Assert.That(driver.HasYaku(result), Is.True);
                Assert.That(driver.CanDeclareWin(result), Is.True);
                Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
                Assert.That(driver.ContainsYaku(result, "SevenPairs"), Is.False);
                Assert.That(driver.CandidateHasYaku(sevenPairsCandidate), Is.True);
                Assert.That(driver.CandidateContainsYaku(sevenPairsCandidate, "SevenPairs"), Is.True);
                Assert.That(driver.TotalHan(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_AppliesCommonYakuToEveryCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None"),
                        driver.CreateDefinition("Tanyao", "One", "One"),
                        driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m 8m",
                    "8m",
                    "Tsumo",
                    isReachDeclared: true);

                Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(1));
                for (int i = 0; i < driver.CandidateResultCount(result); i++)
                {
                    object candidate = driver.CandidateResultAt(result, i);
                    Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                    Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.True);
                    Assert.That(driver.CandidateContainsYaku(candidate, "Tanyao"), Is.True);
                }
            }
        }

        [Test]
        public void EvaluateWithTile_CreatesCandidatesEvenWhenCatalogIsEmpty()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m 8m",
                    "8m",
                    "Ron");

                Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
                Assert.That(driver.HandEvaluationHasYaku(result), Is.False);
                Assert.That(driver.HasYaku(result), Is.False);
                Assert.That(driver.CanDeclareWin(result), Is.False);
                for (int i = 0; i < driver.CandidateResultCount(result); i++)
                {
                    object candidate = driver.CandidateResultAt(result, i);
                    Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(0));
                    Assert.That(driver.CandidateHasYaku(candidate), Is.False);
                }
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotCreateCandidatesWhenHandCannotWin()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Reach", "One", "None")),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E S W C",
                    "5m",
                    "Ron",
                    isReachDeclared: true);

                Assert.That(driver.IsWinningShape(result), Is.False);
                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_CandidateResultsAreImmutableSnapshots()
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
                object candidate = driver.CandidateResultAt(result, 0);
                IList candidateResults = (IList)driver.CandidateResultsCollection(result);
                IList candidateYakus = (IList)driver.CandidateYakus(candidate);

                Assert.Throws<NotSupportedException>(() => candidateResults.Add(candidate));
                Assert.Throws<NotSupportedException>(
                    () => candidateYakus.Add(driver.CandidateYakuAt(candidate, 0)));
                Assert.That(driver.CandidatePropertyCanWrite(candidate, "Type"), Is.False);
                Assert.That(driver.CandidatePropertyCanWrite(candidate, "StandardInterpretation"), Is.False);
                Assert.That(driver.CandidatePropertyCanWrite(candidate, "SevenPairsAnalysis"), Is.False);
                Assert.That(driver.CandidatePropertyCanWrite(candidate, "ThirteenOrphansAnalysis"), Is.False);
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
                object reachCandidate = driver.FindCandidateContainingYaku(result, "Reach");

                AssertCanDeclareWithCandidateTotalHan(driver, result, reachCandidate, 1);
                Assert.That(driver.ContainsYaku(result, "Reach"), Is.False);
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
                object tsumoCandidate = driver.FindCandidateContainingYaku(result, "MenzenTsumo");

                AssertCanDeclareWithCandidateTotalHan(driver, result, tsumoCandidate, 1);
                Assert.That(driver.ContainsYaku(result, "MenzenTsumo"), Is.False);
            }
        }

        [Test]
        public void HandEvaluationResult_UsesLegacyYakusOnlyWhenCandidatesAreMissing()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result =
                    driver.CreateLegacyHandEvaluationResultWithYaku("Reach", "One");

                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(0));
                Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
                Assert.That(driver.TopLevelYakuCount(result), Is.EqualTo(1));
                Assert.That(driver.TotalHan(result), Is.EqualTo(1));
            }
        }

        [Test]
        public void HandEvaluationResult_PrefersCandidateResultsOverLegacyYakus()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object candidateSource = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    "2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m 7m 8m",
                    "8m",
                    "Ron");
                object result = driver.CreateHandEvaluationResultWithLegacyYakuAndCandidateResults(
                    "Reach",
                    "One",
                    candidateSource);

                Assert.That(driver.CandidateResultCount(result), Is.GreaterThan(0));
                Assert.That(driver.AnyCandidateHasYaku(result), Is.False);
                Assert.That(driver.HandEvaluationHasYaku(result), Is.False);
                Assert.That(driver.TopLevelYakuCount(result), Is.EqualTo(0));
                Assert.That(driver.TotalHan(result), Is.EqualTo(0));
            }
        }

        [Test]
        public void HandEvaluator_DoesNotFallbackToRepresentativeShapeWithoutDetailedAnalysis()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateLegacyHandEvaluationContext(
                    driver.CreateCatalog(driver.CreateDefinition("SevenPairs", "Two", "None")),
                    "SevenPairs");

                Assert.That(driver.CandidateResultCount(result), Is.EqualTo(0));
                Assert.That(driver.HandEvaluationHasYaku(result), Is.False);
                Assert.That(driver.TopLevelYakuCount(result), Is.EqualTo(0));
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
                object legacyHandEvaluationResult = driver.CreateLegacyHandEvaluationResult();
                Assert.That(legacyHandEvaluationResult, Is.Not.Null);
                Assert.That(driver.CandidateResultCount(legacyHandEvaluationResult), Is.EqualTo(0));
            }
        }

        private static void AssertCanDeclareWithCandidateTotalHan(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            object candidate,
            int expectedTotalHan)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.HasYaku(result), Is.True);
            Assert.That(driver.CanDeclareWin(result), Is.True);
            Assert.That(driver.HandEvaluationHasYaku(result), Is.True);
            Assert.That(driver.TotalHan(result), Is.EqualTo(0));
            Assert.That(driver.TopLevelYakuCount(result), Is.EqualTo(0));
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateHasYaku(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(expectedTotalHan));
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

            Assert.Fail($"Candidate not found: {typeName}");
            return null;
        }
    }
}
