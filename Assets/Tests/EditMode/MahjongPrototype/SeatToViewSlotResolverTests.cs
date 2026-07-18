using MahjongPrototype.Tests.TestSupport.Features.ViewSlot;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class SeatToViewSlotResolverTests
    {
        [TestCase("East", "East", "SelfBottom")]
        [TestCase("East", "South", "PreviousRight")]
        [TestCase("East", "West", "AcrossTop")]
        [TestCase("East", "North", "NextLeft")]
        [TestCase("South", "South", "SelfBottom")]
        [TestCase("South", "West", "PreviousRight")]
        [TestCase("South", "North", "AcrossTop")]
        [TestCase("South", "East", "NextLeft")]
        [TestCase("West", "West", "SelfBottom")]
        [TestCase("West", "North", "PreviousRight")]
        [TestCase("West", "East", "AcrossTop")]
        [TestCase("West", "South", "NextLeft")]
        [TestCase("North", "North", "SelfBottom")]
        [TestCase("North", "East", "PreviousRight")]
        [TestCase("North", "South", "AcrossTop")]
        [TestCase("North", "West", "NextLeft")]
        public void Resolve_ReturnsRelativeViewSlot(string selfSeat, string targetSeat, string expectedViewSlot)
        {
            SeatToViewSlotResolverTestDriver driver = SeatToViewSlotResolverTestDriver.Create();

            string resolved = driver.Resolve(selfSeat, targetSeat);

            Assert.That(resolved, Is.EqualTo(expectedViewSlot));
        }
    }
}
