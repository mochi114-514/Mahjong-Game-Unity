using MahjongPrototype.Tests.TestSupport.Features.Win;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class NoYakuTenpaiEvaluatorTests
    {
        [Test]
        public void Evaluate_ReturnsNotTenpaiForMissingWinningShapeWait()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.Evaluate(
                    driver.CreateCatalog(),
                    "1m 9m 1p 9p 1s 9s E S W N P F 5m");

                Assert.That(driver.IsEvaluated(result), Is.True);
                Assert.That(driver.IsTenpai(result), Is.False);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.False);
            }
        }

        [Test]
        public void Evaluate_ShowsWhenEveryWinningShapeWaitHasNoYaku()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.Evaluate(
                    driver.CreateCatalog(
                        driver.CreateDefinition("Tanyao", "One", "One"),
                        driver.CreateDefinition("Reach", "One", "None"),
                        driver.CreateDefinition("KokushiMusou", "None", "None", true)),
                    "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P");

                Assert.That(driver.IsEvaluated(result), Is.True);
                Assert.That(driver.IsTenpai(result), Is.True);
                Assert.That(driver.HasAnyYakuWait(result), Is.False);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.True);
            }
        }

        [Test]
        public void Evaluate_HidesWhenTanyaoWaitExists()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.Evaluate(
                    driver.CreateCatalog(driver.CreateDefinition("Tanyao", "One", "One")),
                    "2m 3m 4m 3p 4p 5p 2s 3s 4s 6s 7s 8s 5m");

                Assert.That(driver.IsTenpai(result), Is.True);
                Assert.That(driver.HasAnyYakuWait(result), Is.True);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.False);
            }
        }

        [Test]
        public void Evaluate_HidesWhenReachWaitExists()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.Evaluate(
                    driver.CreateCatalog(driver.CreateDefinition("Reach", "One", "None")),
                    "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P",
                    isReachDeclared: true);

                Assert.That(driver.IsTenpai(result), Is.True);
                Assert.That(driver.HasAnyYakuWait(result), Is.True);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.False);
            }
        }

        [Test]
        public void Evaluate_HidesWhenPinfuCandidateWaitExists()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.Evaluate(
                    driver.CreateCatalog(driver.CreateDefinition("Pinfu", "One", "None")),
                    "1m 2m 3m 4m 5m 2p 3p 4p 5p 6p 7p 4s 4s");

                Assert.That(driver.IsEvaluated(result), Is.True);
                Assert.That(driver.IsTenpai(result), Is.True);
                Assert.That(driver.HasAnyYakuWait(result), Is.True);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.False);
            }
        }

        [Test]
        public void Evaluate_HidesForYakumanWaitUsingHasYaku()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.Evaluate(
                    driver.CreateCatalog(driver.CreateDefinition("KokushiMusou", "None", "None", true)),
                    "1m 9m 1p 9p 1s 9s E S W N P F C");

                Assert.That(driver.IsTenpai(result), Is.True);
                Assert.That(driver.HasAnyYakuWait(result), Is.True);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.False);
            }
        }

        [Test]
        public void Evaluate_ReturnsNotEvaluatedWhenEvaluatorIsMissing()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object result = driver.EvaluateWithoutWinDeclarationEvaluator(
                    "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P");

                Assert.That(driver.IsEvaluated(result), Is.False);
                Assert.That(driver.ShouldShowZeroHanTenpai(result), Is.False);
            }
        }

        [Test]
        public void Evaluate_ReturnsNotTenpaiForWrongHandTileCount()
        {
            using (NoYakuTenpaiEvaluatorTestDriver driver =
                NoYakuTenpaiEvaluatorTestDriver.Create())
            {
                object twelveTileResult = driver.Evaluate(
                    driver.CreateCatalog(),
                    "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s");
                object fourteenTileResult = driver.Evaluate(
                    driver.CreateCatalog(),
                    "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P P");

                Assert.That(driver.IsTenpai(twelveTileResult), Is.False);
                Assert.That(driver.ShouldShowZeroHanTenpai(twelveTileResult), Is.False);
                Assert.That(driver.IsTenpai(fourteenTileResult), Is.False);
                Assert.That(driver.ShouldShowZeroHanTenpai(fourteenTileResult), Is.False);
            }
        }
    }
}
