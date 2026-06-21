using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandSortTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string HandTypeName = "MahjongPrototype.Domain.Hand, Assembly-CSharp";

        [Test]
        public void SortByTypeIndex_SortsTilesInNaturalMahjongOrder()
        {
            object hand = CreateHand("C 9m 1m E 3p 2s 1p F 9s 5m S W N P 2m 1s");

            SortByTypeIndex(hand);

            Assert.That(
                ToDisplayString(hand),
                Is.EqualTo("1m 2m 5m 9m 1p 3p 1s 2s 9s E S W N P F C"));
        }

        [Test]
        public void SortByTypeIndex_MovesInvalidTilesToEnd()
        {
            object hand = CreateHand();
            AddTile(hand, CreateInvalidTile());
            AddTile(hand, CreateTile("C"));
            AddTile(hand, CreateTile("1m"));

            SortByTypeIndex(hand);

            Array tiles = GetTiles(hand);
            Assert.That(tiles.Length, Is.EqualTo(3));
            Assert.That(GetTypeIndex(tiles.GetValue(0)), Is.EqualTo(0));
            Assert.That(GetTypeIndex(tiles.GetValue(1)), Is.EqualTo(33));
            Assert.That(GetTypeIndex(tiles.GetValue(2)), Is.EqualTo(-1));
        }

        [Test]
        public void SortByTypeIndex_PreservesTileCountAndDuplicates()
        {
            object hand = CreateHand("3m 1m 3m E 1m E");

            SortByTypeIndex(hand);

            Assert.That(GetTiles(hand).Length, Is.EqualTo(6));
            Assert.That(ToDisplayString(hand), Is.EqualTo("1m 1m 3m 3m E E"));
        }

        private static object CreateHand(params string[] tileCodes)
        {
            Type handType = GetHandType();
            object hand = Activator.CreateInstance(handType);

            for (int i = 0; i < tileCodes.Length; i++)
                AddTile(hand, CreateTile(tileCodes[i]));

            return hand;
        }

        private static object CreateHand(string handText)
        {
            return CreateHand(SplitCodes(handText));
        }

        private static object CreateTile(string code)
        {
            Type tileType = GetTileType();
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object CreateInvalidTile()
        {
            return Activator.CreateInstance(GetTileType());
        }

        private static void AddTile(object hand, object tile)
        {
            MethodInfo addMethod = GetHandType().GetMethod("Add");
            Assert.That(addMethod, Is.Not.Null);
            addMethod.Invoke(hand, new[] { tile });
        }

        private static void SortByTypeIndex(object hand)
        {
            MethodInfo sortMethod = GetHandType().GetMethod("SortByTypeIndex");
            Assert.That(sortMethod, Is.Not.Null);
            sortMethod.Invoke(hand, null);
        }

        private static Array GetTiles(object hand)
        {
            MethodInfo getTilesMethod = GetHandType().GetMethod("GetTiles");
            Assert.That(getTilesMethod, Is.Not.Null);
            return (Array)getTilesMethod.Invoke(hand, null);
        }

        private static string ToDisplayString(object hand)
        {
            MethodInfo displayMethod = GetHandType().GetMethod("ToDisplayString");
            Assert.That(displayMethod, Is.Not.Null);
            return (string)displayMethod.Invoke(hand, null);
        }

        private static int GetTypeIndex(object tile)
        {
            PropertyInfo typeIndexProperty = GetTileType().GetProperty("TypeIndex");
            Assert.That(typeIndexProperty, Is.Not.Null);
            return (int)typeIndexProperty.GetValue(tile);
        }

        private static string[] SplitCodes(string handText)
        {
            return handText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static Type GetTileType()
        {
            return Type.GetType(TileTypeName, true);
        }

        private static Type GetHandType()
        {
            return Type.GetType(HandTypeName, true);
        }
    }
}
