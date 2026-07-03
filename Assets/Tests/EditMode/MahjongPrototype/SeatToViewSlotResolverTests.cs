using MahjongPrototype.Tests.TestSupport.Features.ViewSlot;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class SeatToViewSlotResolverTests
    {
        [TestCase("East", "East", "SelfBottom")]
        [TestCase("East", "South", "NextLeft")]
        [TestCase("East", "West", "AcrossTop")]
        [TestCase("East", "North", "PreviousRight")]
        [TestCase("South", "South", "SelfBottom")]
        [TestCase("South", "West", "NextLeft")]
        [TestCase("South", "North", "AcrossTop")]
        [TestCase("South", "East", "PreviousRight")]
        [TestCase("West", "West", "SelfBottom")]
        [TestCase("West", "North", "NextLeft")]
        [TestCase("West", "East", "AcrossTop")]
        [TestCase("West", "South", "PreviousRight")]
        [TestCase("North", "North", "SelfBottom")]
        [TestCase("North", "East", "NextLeft")]
        [TestCase("North", "South", "AcrossTop")]
        [TestCase("North", "West", "PreviousRight")]
        public void Resolve_ReturnsRelativeViewSlot(string selfSeat, string targetSeat, string expectedViewSlot)
        {
            SeatToViewSlotResolverTestDriver driver = SeatToViewSlotResolverTestDriver.Create();

            string resolved = driver.Resolve(selfSeat, targetSeat);

            Assert.That(resolved, Is.EqualTo(expectedViewSlot));
        }
    }
}
