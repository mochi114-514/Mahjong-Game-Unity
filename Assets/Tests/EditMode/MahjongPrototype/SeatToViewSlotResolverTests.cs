using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class SeatToViewSlotResolverTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string ResolverTypeName = "MahjongPrototype.UI.SeatToViewSlotResolver, Assembly-CSharp";

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
            object resolved = InvokeResolve(ParseSeat(selfSeat), ParseSeat(targetSeat));

            Assert.That(resolved.ToString(), Is.EqualTo(expectedViewSlot));
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static object InvokeResolve(object selfSeat, object targetSeat)
        {
            Type resolverType = Type.GetType(ResolverTypeName, true);
            MethodInfo method = resolverType.GetMethod(
                "Resolve",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new[] { selfSeat, targetSeat });
        }
    }
}
