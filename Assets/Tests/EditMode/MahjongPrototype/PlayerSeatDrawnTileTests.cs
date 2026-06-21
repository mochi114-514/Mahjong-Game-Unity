using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerSeatDrawnTileTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string PlayerSeatTypeName = "MahjongPrototype.Domain.PlayerSeat, Assembly-CSharp";

        [Test]
        public void DrawnTile_CanBeCommittedToHand()
        {
            object playerSeat = CreatePlayerSeat();

            SetDrawnTile(playerSeat, CreateTile("9m"));

            bool committed = CommitDrawnTileToHand(playerSeat);

            Assert.That(committed, Is.True);
            Assert.That(HasDrawnTile(playerSeat), Is.False);
            Assert.That(GetHandDisplayString(playerSeat), Is.EqualTo("9m"));
        }

        [Test]
        public void DrawnTile_CanBeTakenAndCleared()
        {
            object playerSeat = CreatePlayerSeat();
            SetDrawnTile(playerSeat, CreateTile("E"));

            bool taken = TryTakeDrawnTile(playerSeat, out object tile);

            Assert.That(taken, Is.True);
            Assert.That(tile.ToString(), Is.EqualTo("E"));
            Assert.That(HasDrawnTile(playerSeat), Is.False);
        }

        [Test]
        public void SortingHand_DoesNotChangeDrawnTile()
        {
            object playerSeat = CreatePlayerSeat();
            AddHandTile(playerSeat, "9m");
            AddHandTile(playerSeat, "1m");
            SetDrawnTile(playerSeat, CreateTile("C"));

            SortNormalHand(playerSeat);

            Assert.That(GetHandDisplayString(playerSeat), Is.EqualTo("1m 9m"));
            Assert.That(GetDrawnTile(playerSeat).ToString(), Is.EqualTo("C"));
        }

        private static object CreatePlayerSeat()
        {
            Type seatIdType = Type.GetType(SeatIdTypeName, true);
            object east = Enum.Parse(seatIdType, "East");
            return Activator.CreateInstance(GetPlayerSeatType(), east);
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static void SetDrawnTile(object playerSeat, object tile)
        {
            MethodInfo method = GetPlayerSeatType().GetMethod("SetDrawnTile");
            Assert.That(method, Is.Not.Null);
            method.Invoke(playerSeat, new[] { tile });
        }

        private static bool TryTakeDrawnTile(object playerSeat, out object tile)
        {
            MethodInfo method = GetPlayerSeatType().GetMethod("TryTakeDrawnTile");
            Assert.That(method, Is.Not.Null);
            object[] args = { null };
            bool result = (bool)method.Invoke(playerSeat, args);
            tile = args[0];
            return result;
        }

        private static bool CommitDrawnTileToHand(object playerSeat)
        {
            MethodInfo method = GetPlayerSeatType().GetMethod("CommitDrawnTileToHand");
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(playerSeat, null);
        }

        private static bool HasDrawnTile(object playerSeat)
        {
            PropertyInfo property = GetPlayerSeatType().GetProperty("HasDrawnTile");
            Assert.That(property, Is.Not.Null);
            return (bool)property.GetValue(playerSeat);
        }

        private static object GetDrawnTile(object playerSeat)
        {
            PropertyInfo property = GetPlayerSeatType().GetProperty("DrawnTile");
            Assert.That(property, Is.Not.Null);
            object tile = property.GetValue(playerSeat);
            Assert.That(tile, Is.Not.Null);
            return tile;
        }

        private static void AddHandTile(object playerSeat, string tileCode)
        {
            object hand = GetHand(playerSeat);
            MethodInfo method = hand.GetType().GetMethod("Add");
            Assert.That(method, Is.Not.Null);
            method.Invoke(hand, new[] { CreateTile(tileCode) });
        }

        private static void SortNormalHand(object playerSeat)
        {
            object hand = GetHand(playerSeat);
            MethodInfo method = hand.GetType().GetMethod("SortByTypeIndex");
            Assert.That(method, Is.Not.Null);
            method.Invoke(hand, null);
        }

        private static string GetHandDisplayString(object playerSeat)
        {
            object hand = GetHand(playerSeat);
            MethodInfo method = hand.GetType().GetMethod("ToDisplayString");
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(hand, null);
        }

        private static object GetHand(object playerSeat)
        {
            PropertyInfo property = GetPlayerSeatType().GetProperty("Hand");
            Assert.That(property, Is.Not.Null);
            return property.GetValue(playerSeat);
        }

        private static Type GetPlayerSeatType()
        {
            return Type.GetType(PlayerSeatTypeName, true);
        }
    }
}
