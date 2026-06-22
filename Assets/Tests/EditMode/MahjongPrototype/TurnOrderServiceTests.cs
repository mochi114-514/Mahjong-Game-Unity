using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class TurnOrderServiceTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TurnOrderServiceTypeName = "MahjongPrototype.Services.TurnOrderService, Assembly-CSharp";

        [Test]
        public void GetNextSeat_ReturnsSameSeat_WhenOnlyOneSeatIsActive()
        {
            object result = GetNextSeat(CreateSeatList("East"), "East");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        [TestCase("East", "South")]
        [TestCase("South", "West")]
        [TestCase("West", "North")]
        [TestCase("North", "East")]
        public void GetNextSeat_ReturnsNextSeat_WhenMultipleSeatsAreActive(
            string currentSeat,
            string expectedSeat)
        {
            object result = GetNextSeat(CreateSeatList("East", "South", "West", "North"), currentSeat);

            Assert.That(result.ToString(), Is.EqualTo(expectedSeat));
        }

        [Test]
        public void GetNextSeat_ReturnsFirstActiveSeat_WhenCurrentSeatIsNotActive()
        {
            object result = GetNextSeat(CreateSeatList("East", "South"), "West");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        [Test]
        public void GetNextSeat_ReturnsEast_WhenActiveSeatsIsEmpty()
        {
            object result = GetNextSeat(CreateSeatList(), "East");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        [Test]
        public void GetNextSeat_ReturnsEast_WhenActiveSeatsIsNull()
        {
            object result = GetNextSeat(null, "East");

            Assert.That(result.ToString(), Is.EqualTo("East"));
        }

        private static object GetNextSeat(object activeSeats, string currentSeatName)
        {
            Type serviceType = Type.GetType(TurnOrderServiceTypeName, true);
            object service = Activator.CreateInstance(serviceType);
            MethodInfo method = serviceType.GetMethod("GetNextSeat");
            Assert.That(method, Is.Not.Null);

            return method.Invoke(service, new[] { activeSeats, ParseSeat(currentSeatName) });
        }

        private static IList CreateSeatList(params string[] seatNames)
        {
            Type seatIdType = GetSeatIdType();
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(seatIdType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(ParseSeat(seatNames[i]));

            return list;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(GetSeatIdType(), seatName);
        }

        private static Type GetSeatIdType()
        {
            return Type.GetType(SeatIdTypeName, true);
        }
    }
}
