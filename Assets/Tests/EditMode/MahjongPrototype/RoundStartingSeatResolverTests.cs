using System;
using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class RoundStartingSeatResolverTests
    {
        [TestCase("East", "South")]
        [TestCase("South", "East")]
        public void Resolve_ReturnsEastWhenEastIsActive(
            string firstSeat,
            string secondSeat)
        {
            RoundStartingSeatResolverTestDriver driver = RoundStartingSeatResolverTestDriver.Create();

            string startingSeat = driver.Resolve(firstSeat, secondSeat);

            Assert.That(startingSeat, Is.EqualTo("East"));
        }

        [Test]
        public void Resolve_ReturnsEastWhenEastIsAmongMultipleActiveSeats()
        {
            RoundStartingSeatResolverTestDriver driver = RoundStartingSeatResolverTestDriver.Create();

            string startingSeat = driver.Resolve("South", "West", "East", "North");

            Assert.That(startingSeat, Is.EqualTo("East"));
        }

        [TestCase("South", "West", "South")]
        [TestCase("West", "North", "West")]
        public void Resolve_ReturnsFirstActiveSeatWhenEastIsNotActive(
            string firstSeat,
            string secondSeat,
            string expectedSeat)
        {
            RoundStartingSeatResolverTestDriver driver = RoundStartingSeatResolverTestDriver.Create();

            string startingSeat = driver.Resolve(firstSeat, secondSeat);

            Assert.That(startingSeat, Is.EqualTo(expectedSeat));
        }

        [Test]
        public void Resolve_ThrowsWhenNoActiveSeatIsAvailable()
        {
            RoundStartingSeatResolverTestDriver driver = RoundStartingSeatResolverTestDriver.Create();

            Exception exception = driver.ResolveEmptyException();

            Assert.That(exception, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Resolve_ThrowsWhenActiveSeatsAreNull()
        {
            RoundStartingSeatResolverTestDriver driver = RoundStartingSeatResolverTestDriver.Create();

            Exception exception = driver.ResolveNullException();

            Assert.That(exception, Is.TypeOf<ArgumentNullException>());
        }
    }
}
