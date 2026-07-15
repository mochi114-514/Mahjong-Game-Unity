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

    public sealed class HandEvaluatorKanYakuTests
    {
        private const string FourTripletTankiHand =
            "1m 1m 1m 2m 2m 2m 3p 3p 3p 4s 4s 4s 5s";
        private const string ThreeTripletOneSequenceTankiHand =
            "1m 1m 1m 2p 2p 2p 3s 3s 3s 4m 5m 6m 7p";
        private const string SevenPairsHand =
            "1m 1m 2m 2m 3m 3m 4p 4p 5p 5p 6s 6s C";
        private const string OpenTripletTankiHand =
            "2m 2m 2m 3p 3p 3p 4s 4s 4s 5s";
        private const string FixedKanTripletTankiHand =
            "2m 2m 2m 3p 3p 3p 4s 4s 4s 5s";

        [TestCase("Ron")]
        [TestCase("Tsumo")]
        public void EvaluateWithTile_FourConcealedTriplets_AddsToitoi(
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Toitoi", "Two", "Two")),
                    FourTripletTankiHand,
                    "5s",
                    winTypeName);

                AssertYaku(driver, result, "Toitoi");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenPon_AddsToitoi()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Toitoi", "Two", "Two")),
                    OpenTripletTankiHand,
                    "5s",
                    "Ron",
                    isClosed: false,
                    melds: driver.CreateOpenPonMelds("1m"));

                AssertYaku(driver, result, "Toitoi");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EvaluateWithTile_DaiminkanOrAnkan_AddsToitoi(bool useAnkan)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object melds = driver.CreateMelds(
                    ankanTileCodes: useAnkan ? new[] { "1m" } : null,
                    daiminkanTileCodes: useAnkan ? null : new[] { "1m" });
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Toitoi", "Two", "Two")),
                    FixedKanTripletTankiHand,
                    "5s",
                    "Tsumo",
                    isClosed: useAnkan,
                    melds: melds);
                object candidate = driver.FindCandidateContainingYaku(result, "Toitoi");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateStandardMeldCount(candidate), Is.EqualTo(4));
                Assert.That(
                    driver.CandidateAllStandardMeldsHaveType(candidate, "Triplet"),
                    Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_SequenceOrSevenPairs_DoesNotAddToitoi()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object catalog = driver.CreateCatalog(
                    driver.CreateDefinition("Toitoi", "Two", "Two"));
                object sequenceResult = driver.EvaluateWithTile(
                    catalog,
                    ThreeTripletOneSequenceTankiHand,
                    "7p",
                    "Ron");
                object sevenPairsResult = driver.EvaluateWithTile(
                    catalog,
                    SevenPairsHand,
                    "C",
                    "Ron");

                AssertNoYaku(driver, sequenceResult, "Toitoi");
                AssertNoYaku(driver, sevenPairsResult, "Toitoi");
            }
        }

        [Test]
        public void EvaluateWithTile_TwoAnkanOneDaiminkanAndConcealedTriplet_AddsSanankouSankantsuAndToitoi()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKanCatalog(driver),
                    "4m 4m 4m 5p",
                    "5p",
                    "Ron",
                    isClosed: false,
                    melds: driver.CreateMelds(
                        ankanTileCodes: new[] { "1m", "2m" },
                        daiminkanTileCodes: new[] { "3m" }));
                object candidate = driver.FindCandidateContainingYaku(result, "Sankantsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo("Tanki"));
                Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Sankantsu"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Toitoi"), Is.True);
            }
        }

        [Test]
        public void EvaluateWithTile_ThreeKansRegardlessOfOpenClosedMix_AddsSankantsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Sankantsu", "Two", "Two")),
                    "4m 4m 4m 5p",
                    "5p",
                    "Tsumo",
                    isClosed: false,
                    melds: driver.CreateMelds(
                        ankanTileCodes: new[] { "1m" },
                        daiminkanTileCodes: new[] { "2m", "3m" }));

                AssertYaku(driver, result, "Sankantsu");
            }
        }

        [Test]
        public void EvaluateWithTile_TwoKansAndPon_DoesNotCountPonAsSankantsu()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(driver.CreateDefinition("Sankantsu", "Two", "Two")),
                    "4m 4m 4m 5p",
                    "5p",
                    "Ron",
                    isClosed: false,
                    melds: driver.CreateMelds(
                        ankanTileCodes: new[] { "1m", "2m" },
                        ponTileCodes: new[] { "3m" }));

                AssertNoYaku(driver, result, "Sankantsu");
            }
        }

        [Test]
        public void EvaluateWithTile_FourKans_AddsSuukantsuAndRemovesNormalYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateKanCatalog(driver),
                    "5p",
                    "5p",
                    "Tsumo",
                    isClosed: false,
                    melds: driver.CreateMelds(
                        ankanTileCodes: new[] { "1m", "2m" },
                        daiminkanTileCodes: new[] { "3m", "4m" }));
                object candidate = driver.FindCandidateContainingYaku(result, "Suukantsu");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateStandardMeldCount(candidate), Is.EqualTo(4));
                Assert.That(
                    driver.CandidateAllStandardMeldsHaveType(candidate, "Triplet"),
                    Is.True);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
                Assert.That(driver.CandidateContainsYaku(candidate, "Suukantsu"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Sankantsu"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Toitoi"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_RinshanTsumo_AddsRinshanKaihouForClosedAndOpenHands()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object catalog = driver.CreateCatalog(
                    driver.CreateDefinition("RinshanKaihou", "One", "One"));
                object closedResult = driver.EvaluateWithTile(
                    catalog,
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Tsumo",
                    isRinshanDraw: true);
                object openResult = driver.EvaluateWithTile(
                    catalog,
                    OpenTripletTankiHand,
                    "5s",
                    "Tsumo",
                    isClosed: false,
                    melds: driver.CreateOpenPonMelds("1m"),
                    isRinshanDraw: true);

                AssertYaku(driver, closedResult, "RinshanKaihou");
                AssertYaku(driver, openResult, "RinshanKaihou");
                Assert.That(
                    driver.CandidateYakuHanName(
                        driver.FindCandidateContainingYaku(openResult, "RinshanKaihou"),
                        "RinshanKaihou"),
                    Is.EqualTo("One"));
            }
        }

        [Test]
        public void EvaluateWithTile_NormalTsumoRonOrPriorKan_DoesNotAddRinshanKaihou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object catalog = driver.CreateCatalog(
                    driver.CreateDefinition("RinshanKaihou", "One", "One"));
                object normalTsumo = driver.EvaluateWithTile(
                    catalog,
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Tsumo");
                object ron = driver.EvaluateWithTile(
                    catalog,
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Ron",
                    isRinshanDraw: true);
                object withAnkan = driver.EvaluateWithTile(
                    catalog,
                    FixedKanTripletTankiHand,
                    "5s",
                    "Tsumo",
                    melds: driver.CreateAnkanMelds("1m"));

                AssertNoYaku(driver, normalTsumo, "RinshanKaihou");
                AssertNoYaku(driver, ron, "RinshanKaihou");
                AssertNoYaku(driver, withAnkan, "RinshanKaihou");
            }
        }

        [Test]
        public void EvaluateWithTile_RinshanAndLastLiveWallFlag_DoNotCombineRinshanKaihouAndHaitei()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition("RinshanKaihou", "One", "One"),
                        driver.CreateDefinition("HaiteiRaoyue", "One", "One")),
                    "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                    "C",
                    "Tsumo",
                    isLastLiveWallDraw: true,
                    isRinshanDraw: true);
                object candidate = driver.FindCandidateContainingYaku(result, "HaiteiRaoyue");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "RinshanKaihou"), Is.False);
            }
        }

        private static object CreateKanCatalog(WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                driver.CreateDefinition("Toitoi", "Two", "Two"),
                driver.CreateDefinition("Sanankou", "Two", "Two"),
                driver.CreateDefinition("Sankantsu", "Two", "Two"),
                driver.CreateDefinition(
                    "Suukantsu",
                    "None",
                    "None",
                    isYakuman: true));
        }

        private static void AssertYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(driver.FindCandidateContainingYaku(result, yakuKindName), Is.Not.Null);
            Assert.That(driver.CanDeclareWin(result), Is.True);
        }

        private static void AssertNoYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object result,
            string yakuKindName)
        {
            Assert.That(driver.IsWinningShape(result), Is.True);
            Assert.That(
                driver.CountCandidatesContainingYaku(result, yakuKindName),
                Is.EqualTo(0));
            Assert.That(driver.CanDeclareWin(result), Is.False);
        }
    }

    public sealed class HandEvaluatorUnimplementedYakuGuardTests
    {
        private const string BasicWinningHand =
            "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C";

        [TestCase("Chankan", "Ron")]
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
