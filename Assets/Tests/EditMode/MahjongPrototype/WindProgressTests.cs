using System;
using MahjongPrototype.Tests.TestSupport.Features.WindProgress;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WindProgressTests
    {
        [Test]
        public void East1_ReturnsEastFirstHand()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object east1 = driver.EastOne();

            AssertWindProgress(driver, east1, "East", 1);
        }

        [Test]
        public void TryGetNext_AdvancesWithinEastRound()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object east1 = driver.EastOne();

            bool hasNext = driver.TryGetNext(east1, out object next);

            Assert.That(hasNext, Is.True);
            AssertWindProgress(driver, next, "East", 2);
        }

        [Test]
        public void TryGetNext_AdvancesEastFourToSouthOne()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object east4 = driver.CreateProgress("East", 4);

            bool hasNext = driver.TryGetNext(east4, out object next);

            Assert.That(hasNext, Is.True);
            AssertWindProgress(driver, next, "South", 1);
        }

        [Test]
        public void TryGetNext_AdvancesSouthThreeToSouthFour()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object south3 = driver.CreateProgress("South", 3);

            bool hasNext = driver.TryGetNext(south3, out object next);

            Assert.That(hasNext, Is.True);
            AssertWindProgress(driver, next, "South", 4);
        }

        [Test]
        public void TryGetNext_ReturnsFalseAfterSouthFour()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object south4 = driver.CreateProgress("South", 4);

            bool hasNext = driver.TryGetNext(south4, out object next);

            Assert.That(hasNext, Is.False);
            AssertWindProgress(driver, next, "South", 4);
        }

        [Test]
        public void Constructor_RejectsHandNumberBelowOne()
        {
            AssertInvalidHandNumberThrowsOutOfRange(0);
        }

        [Test]
        public void Constructor_RejectsHandNumberAboveFour()
        {
            AssertInvalidHandNumberThrowsOutOfRange(5);
        }

        [Test]
        public void MahjongGameState_DefaultConstructorUsesEastOne()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object state = driver.CreateDefaultGameState();

            AssertWindProgress(driver, driver.WindProgressOf(state), "East", 1);
        }

        [Test]
        public void MahjongGameState_WindProgressConstructorStoresProgress()
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            object state = driver.CreateGameState("South", 3);

            AssertWindProgress(driver, driver.WindProgressOf(state), "South", 3);
        }

        private static void AssertWindProgress(
            WindProgressTestDriver driver,
            object progress,
            string expectedRoundWind,
            int expectedHandNumber)
        {
            Assert.That(driver.RoundWindName(progress), Is.EqualTo(expectedRoundWind));
            Assert.That(driver.HandNumber(progress), Is.EqualTo(expectedHandNumber));
        }

        private static void AssertInvalidHandNumberThrowsOutOfRange(int handNumber)
        {
            WindProgressTestDriver driver = WindProgressTestDriver.Create();
            Exception exception = driver.CaptureCreateException("East", handNumber);

            Assert.That(exception, Is.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
