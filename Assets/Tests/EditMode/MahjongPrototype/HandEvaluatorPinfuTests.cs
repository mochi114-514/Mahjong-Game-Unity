using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorPinfuTests
    {
        private const string BasicPinfuHand =
            "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s 5s 5s";
        private const string BasicPinfuWinningTile = "4m";

        [Test]
        public void EvaluateWithTile_AddsPinfuToRyanmenAllSequenceStandardCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    BasicPinfuHand,
                    BasicPinfuWinningTile,
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateStandardMeldCount(candidate), Is.EqualTo(4));
                Assert.That(driver.CandidateAllStandardMeldsHaveType(candidate, "Sequence"), Is.True);
                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("5s"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
            }
        }

        [Test]
        public void EvaluateWithTile_AddsPinfuForTerminalNumberPair()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s 1s 1s",
                    "4m",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("1s"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_AddsPinfuForNonValueWindPair()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s W W",
                    "4m",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("W"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.True);
            }
        }

        [TestCase("P")]
        [TestCase("F")]
        [TestCase("C")]
        public void EvaluateWithTile_DoesNotAddPinfuForDragonPair(string pairTileCode)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    $"2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s {pairTileCode} {pairTileCode}",
                    "4m",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo(pairTileCode));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForSeatWindPair()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s S S",
                    "4m",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("S"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForRoundWindPair()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E",
                    "4m",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "South");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("E"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForDoubleWindPair()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 3m 1p 2p 3p 4p 5p 6p 7s 8s 9s E E",
                    "4m",
                    "Ron",
                    roundWindName: "East",
                    seatWindName: "East");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("E"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuWhenAnyMeldIsTriplet()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 3m 1p 2p 3p 4p 5p 6p E E E 5s 5s",
                    "4m",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateAllStandardMeldsHaveType(candidate, "Sequence"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForKanchanWait()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "2m 4m 1p 2p 3p 4p 5p 6p 7s 8s 9s 5s 5s",
                    "3m",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Kanchan");

                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForPenchanWait()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "1m 2m 1p 2p 3p 4p 5p 6p 7s 8s 9s 5s 5s",
                    "3m",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Penchan");

                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForTankiWait()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "1m 2m 3m 4m 5m 6m 1p 2p 3p 7s 8s 9s 5s",
                    "5s",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Tanki");

                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuForShanponWait()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "1m 2m 3m 4m 5m 6m 1p 2p 3p 5s 5s S S",
                    "S",
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Shanpon");

                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuWhenHandIsOpenEvenIfCatalogHasOpenHan()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Pinfu", "One", "One")),
                    BasicPinfuHand,
                    BasicPinfuWinningTile,
                    "Ron",
                    isClosed: false);
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuToSevenPairsCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                    "C",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "SevenPairs");

                Assert.That(driver.CandidateSevenPairsIsWin(candidate), Is.True);
                Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo("None"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuToThirteenOrphansCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");
                object candidate = FindCandidateOfType(driver, result, "ThirteenOrphans");

                Assert.That(driver.CandidateThirteenOrphansIsWin(candidate), Is.True);
                Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo("None"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_EvaluatesPinfuPerCandidateForMultipleInterpretations()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    "1m 1m 2m 2m 3m 3m 4m 4m 5m 5m 6m 6m 7m",
                    "7m",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "Standard"), Is.GreaterThan(1));
                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));

                int pinfuStandardCount = 0;
                int nonPinfuStandardCount = 0;
                for (int i = 0; i < driver.CandidateResultCount(result); i++)
                {
                    object candidate = driver.CandidateResultAt(result, i);
                    bool hasPinfu = driver.CandidateContainsYaku(candidate, "Pinfu");

                    if (driver.CandidateTypeName(candidate) == "Standard")
                    {
                        if (hasPinfu)
                            pinfuStandardCount++;
                        else
                            nonPinfuStandardCount++;

                        continue;
                    }

                    if (driver.CandidateTypeName(candidate) == "SevenPairs")
                        Assert.That(hasPinfu, Is.False);
                }

                Assert.That(pinfuStandardCount, Is.GreaterThan(0));
                Assert.That(nonPinfuStandardCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void EvaluateWithTile_CombinesPinfuWithReachAndTanyaoInSameCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("Tanyao", "One", "One"),
                        driver.CreateDefinition("Pinfu", "One", "None")),
                    "2m 3m 2p 3p 4p 4p 5p 6p 2s 3s 4s 5s 5s",
                    "4m",
                    "Ron",
                    isReachDeclared: true);
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Tanyao"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(3));
            }
        }

        [Test]
        public void EvaluateWithTile_CombinesPinfuWithMenzenTsumoInSameCandidate()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("MenzenTsumo", "One", "None"),
                        driver.CreateDefinition("Pinfu", "One", "None")),
                    BasicPinfuHand,
                    BasicPinfuWinningTile,
                    "Tsumo");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_DoesNotAddPinfuWhenCatalogDoesNotRegisterPinfu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(),
                    BasicPinfuHand,
                    BasicPinfuWinningTile,
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateAllStandardMeldsHaveType(candidate, "Sequence"), Is.True);
                Assert.That(driver.CandidatePairTileCode(candidate), Is.EqualTo("5s"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Pinfu"), Is.False);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            }
        }

        [Test]
        public void EvaluateWithTile_KeepsPinfuOutOfTopLevelEvaluation()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreatePinfuCatalog(driver),
                    BasicPinfuHand,
                    BasicPinfuWinningTile,
                    "Ron");
                object candidate = FindStandardCandidateWithWaitType(driver, result, "Ryanmen");

                Assert.That(driver.CandidateHasYaku(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(1));
                Assert.That(driver.ContainsYaku(result, "Pinfu"), Is.False);
                Assert.That(driver.HandEvaluationHasYaku(result), Is.False);
                Assert.That(driver.HasYaku(result), Is.False);
                Assert.That(driver.CanDeclareWin(result), Is.False);
            }
        }

        private static object CreatePinfuCatalog(WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(driver.CreateDefinition("Pinfu", "One", "None"));
        }

        private static object FindStandardCandidateWithWaitType(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string waitTypeName)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
            {
                object candidate = driver.CandidateResultAt(result, i);
                if (driver.CandidateTypeName(candidate) == "Standard" &&
                    driver.CandidateWaitTypeName(candidate) == waitTypeName)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Standard candidate not found for wait type: {waitTypeName}");
            return null;
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
