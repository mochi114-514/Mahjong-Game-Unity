using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandEvaluatorConcealedTripletYakuTests
    {
        private const string FourTripletTankiHand =
            "1m 1m 1m 2m 2m 2m 3p 3p 3p 4s 4s 4s 5s";
        private const string FourTripletShanponHand =
            "1m 1m 1m 2m 2m 2m 3p 3p 3p 4s 4s 5s 5s";
        private const string ThreeTripletTankiHand =
            "1m 1m 1m 2p 2p 2p 3s 3s 3s 4m 5m 6m 7p";
        private const string TwoTripletShanponHand =
            "1m 1m 1m 2p 2p 2p 3s 3s 4m 5m 6m 7p 7p";
        private const string TwoTripletTwoSequenceHand =
            "1m 1m 1m 2p 2p 2p 3m 4m 5m 6s 7s 8s 9p";
        private const string OpenPonThreeConcealedTripletTankiHand =
            "2m 2m 2m 3p 3p 3p 4s 4s 4s 5s";
        private const string OpenPonTwoConcealedTripletShanponHand =
            "2m 2m 2m 3p 3p 3p 4s 4s 5s 5s";
        private const string DaisuushiiTankiHand =
            "E E E S S S W W W N N N 5m";

        [TestCase("Ron")]
        [TestCase("Tsumo")]
        public void EvaluateWithTile_TankiFourConcealedTriplets_AddsSuuankouTankiOnly(
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    FourTripletTankiHand,
                    "5s",
                    winTypeName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SuuankouTanki");

                AssertSuuankouTankiOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_ShanponTsumoFourConcealedTriplets_AddsSuuankouOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    FourTripletShanponHand,
                    "4s",
                    "Tsumo");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Suuankou");

                AssertSuuankouOnly(driver, candidate, "Shanpon");
            }
        }

        [Test]
        public void EvaluateWithTile_ShanponRonFourTriplets_AddsSanankouOnly()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    FourTripletShanponHand,
                    "4s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Shanpon");
            }
        }

        [TestCase("Ron")]
        [TestCase("Tsumo")]
        public void EvaluateWithTile_ThreeConcealedTriplets_AddsSanankouOnly(
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    ThreeTripletTankiHand,
                    "7p",
                    winTypeName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Tanki");
            }
        }

        [Test]
        public void EvaluateWithTile_TwoConcealedTripletsAndShanponTsumo_AddsSanankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    TwoTripletShanponHand,
                    "3s",
                    "Tsumo");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Shanpon");
            }
        }

        [Test]
        public void EvaluateWithTile_TwoConcealedTripletsAndShanponRon_AddsNoConcealedTripletYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    TwoTripletShanponHand,
                    "3s",
                    "Ron");
                object candidate =
                    driver.FindStandardCandidateWithWaitType(result, "Shanpon");

                Assert.That(candidate, Is.Not.Null);
                AssertNoConcealedTripletYaku(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_TwoTripletsAndTwoSequences_AddsNoConcealedTripletYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    TwoTripletTwoSequenceHand,
                    "9p",
                    "Ron");

                AssertNoConcealedTripletYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_OpenPonAndThreeConcealedTriplets_AddsSanankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object melds = driver.CreateOpenPonMelds("1m");
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    OpenPonThreeConcealedTripletTankiHand,
                    "5s",
                    "Tsumo",
                    isClosed: false,
                    melds: melds);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                Assert.That(driver.CanDeclareWin(result), Is.True);
                AssertSanankouOnly(driver, candidate, "Tanki");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenPonAndShanponTsumo_AddsSanankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object melds = driver.CreateOpenPonMelds("1m");
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    OpenPonTwoConcealedTripletShanponHand,
                    "4s",
                    "Tsumo",
                    isClosed: false,
                    melds: melds);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Shanpon");
            }
        }

        [Test]
        public void EvaluateWithTile_OpenPonAndShanponRon_DoesNotCountRonCompletedTriplet()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object melds = driver.CreateOpenPonMelds("1m");
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    OpenPonTwoConcealedTripletShanponHand,
                    "4s",
                    "Ron",
                    isClosed: false,
                    melds: melds);
                object candidate =
                    driver.FindStandardCandidateWithWaitType(result, "Shanpon");

                Assert.That(candidate, Is.Not.Null);
                AssertNoConcealedTripletYaku(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_OpenPon_DoesNotAddSuuankouOrSuuankouTanki()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object melds = driver.CreateOpenPonMelds("1m");
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    OpenPonThreeConcealedTripletTankiHand,
                    "5s",
                    "Tsumo",
                    isClosed: false,
                    melds: melds);
                object candidate =
                    driver.FindStandardCandidateWithWaitType(result, "Tanki");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.False);
            }
        }

        [Test]
        public void EvaluateWithTile_AnkanCountsAsConcealedAndKeepsSuuankouTankiEligible()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object melds = driver.CreateAnkanMelds("1m");
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    OpenPonThreeConcealedTripletTankiHand,
                    "5s",
                    "Tsumo",
                    isClosed: true,
                    melds: melds);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SuuankouTanki");

                Assert.That(driver.CanDeclareWin(result), Is.True);
                AssertSuuankouTankiOnly(driver, candidate);
            }
        }

        [Test]
        public void EvaluateWithTile_TankiWhenSuuankouTankiMissing_FallsBackToSuuankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateSanankouDefinition(driver),
                        CreateSuuankouDefinition(driver)),
                    FourTripletTankiHand,
                    "5s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Suuankou");

                AssertSuuankouOnly(driver, candidate, "Tanki");
            }
        }

        [Test]
        public void EvaluateWithTile_TankiWhenSuuankouTankiDisabled_FallsBackToSuuankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateSanankouDefinition(driver),
                        CreateSuuankouDefinition(driver),
                        driver.CreateDefinition(
                            "SuuankouTanki",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false)),
                    FourTripletTankiHand,
                    "5s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Suuankou");

                AssertSuuankouOnly(driver, candidate, "Tanki");
            }
        }

        [Test]
        public void EvaluateWithTile_ShanponTsumoWhenSuuankouMissing_KeepsSanankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateSanankouDefinition(driver)),
                    FourTripletShanponHand,
                    "4s",
                    "Tsumo");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Shanpon");
            }
        }

        [Test]
        public void EvaluateWithTile_ShanponTsumoWhenSuuankouDisabled_KeepsSanankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateSanankouDefinition(driver),
                        driver.CreateDefinition(
                            "Suuankou",
                            "None",
                            "None",
                            isYakuman: true,
                            isEnabled: false)),
                    FourTripletShanponHand,
                    "4s",
                    "Tsumo");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Shanpon");
            }
        }

        [Test]
        public void EvaluateWithTile_TankiWhenSuuankouTankiAndSuuankouMissing_KeepsSanankou()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(CreateSanankouDefinition(driver)),
                    FourTripletTankiHand,
                    "5s",
                    "Ron");
                object candidate =
                    driver.FindCandidateContainingYaku(result, "Sanankou");

                AssertSanankouOnly(driver, candidate, "Tanki");
            }
        }

        [Test]
        public void EvaluateWithTile_YakumanWithNormalYaku_LeavesOnlyYakuman()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        CreateSanankouDefinition(driver),
                        CreateSuuankouTankiDefinition(driver),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("MenzenTsumo", "One", "None")),
                    FourTripletTankiHand,
                    "5s",
                    "Tsumo",
                    isReachDeclared: true);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SuuankouTanki");

                AssertSuuankouTankiOnly(driver, candidate);
                Assert.That(driver.CandidateContainsYaku(candidate, "Reach"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "MenzenTsumo"), Is.False);
            }
        }

        [TestCase("Ron")]
        [TestCase("Tsumo")]
        public void EvaluateWithTile_DaisuushiiTanki_CombinesWithSuuankouTanki(
            string winTypeName)
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    driver.CreateCatalog(
                        driver.CreateDefinition(
                            "Daisuushii",
                            "None",
                            "None",
                            isYakuman: true),
                        CreateSanankouDefinition(driver),
                        CreateSuuankouDefinition(driver),
                        CreateSuuankouTankiDefinition(driver)),
                    DaisuushiiTankiHand,
                    "5m",
                    winTypeName);
                object candidate =
                    driver.FindCandidateContainingYaku(result, "SuuankouTanki");

                Assert.That(candidate, Is.Not.Null);
                Assert.That(driver.CandidateContainsYaku(candidate, "Daisuushii"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.True);
                Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.False);
                Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.False);
                Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
                Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
                Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(2));
            }
        }

        [Test]
        public void EvaluateWithTile_SevenPairs_DoesNotAddConcealedTripletYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E P",
                    "P",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "SevenPairs"), Is.EqualTo(1));
                AssertNoConcealedTripletYakuInResult(driver, result);
            }
        }

        [Test]
        public void EvaluateWithTile_ThirteenOrphans_DoesNotAddConcealedTripletYaku()
        {
            using (WinDeclarationEvaluatorTestDriver driver =
                WinDeclarationEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithTile(
                    CreateConcealedTripletCatalog(driver),
                    "1m 9m 1p 9p 1s 9s E S W N P F C",
                    "E",
                    "Ron");

                Assert.That(driver.CountCandidatesOfType(result, "ThirteenOrphans"), Is.EqualTo(1));
                AssertNoConcealedTripletYakuInResult(driver, result);
            }
        }

        private static object CreateConcealedTripletCatalog(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateCatalog(
                CreateSanankouDefinition(driver),
                CreateSuuankouDefinition(driver),
                CreateSuuankouTankiDefinition(driver));
        }

        private static object CreateSanankouDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition("Sanankou", "Two", "Two");
        }

        private static object CreateSuuankouDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "Suuankou",
                "None",
                "None",
                isYakuman: true);
        }

        private static object CreateSuuankouTankiDefinition(
            WinDeclarationEvaluatorTestDriver driver)
        {
            return driver.CreateDefinition(
                "SuuankouTanki",
                "None",
                "None",
                isYakuman: true);
        }

        private static void AssertSuuankouTankiOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo("Tanki"));
            Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertSuuankouOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate,
            string expectedWaitTypeName)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo(expectedWaitTypeName));
            Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.True);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(0));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertSanankouOnly(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate,
            string expectedWaitTypeName)
        {
            Assert.That(candidate, Is.Not.Null);
            Assert.That(driver.CandidateTypeName(candidate), Is.EqualTo("Standard"));
            Assert.That(driver.CandidateWaitTypeName(candidate), Is.EqualTo(expectedWaitTypeName));
            Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.True);
            Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.False);
            Assert.That(driver.CandidateHasYakuman(candidate), Is.False);
            Assert.That(driver.CandidateTotalHan(candidate), Is.EqualTo(2));
            Assert.That(driver.CandidateYakuCount(candidate), Is.EqualTo(1));
        }

        private static void AssertNoConcealedTripletYakuInResult(
            WinDeclarationEvaluatorTestDriver driver,
            object result)
        {
            for (int i = 0; i < driver.CandidateResultCount(result); i++)
                AssertNoConcealedTripletYaku(driver, driver.CandidateResultAt(result, i));
        }

        private static void AssertNoConcealedTripletYaku(
            WinDeclarationEvaluatorTestDriver driver,
            object candidate)
        {
            Assert.That(driver.CandidateContainsYaku(candidate, "Sanankou"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "Suuankou"), Is.False);
            Assert.That(driver.CandidateContainsYaku(candidate, "SuuankouTanki"), Is.False);
        }
    }
}
