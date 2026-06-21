using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class HandWinCheckerTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string HandWinCheckerTypeName = "MahjongPrototype.Services.HandWinChecker, Assembly-CSharp";

        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m 5m")]
        [TestCase("1m 1m 1m 2m 2m 2m 3p 4p 5p C C C 9s 9s")]
        public void CanWinStandardHand_ReturnsTrueForStandardHand(string handText)
        {
            Assert.That(CanWinStandardHand(handText), Is.True);
        }

        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m")]
        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s E S W 5m 5m")]
        [TestCase("1m 2m 3m 2p 3p 4p 7s 8s 9s P F C 5m 5m")]
        [TestCase("8m 9m 1p 3p 4p 5p 2s 3s 4s E E E 5m 5m")]
        [TestCase("1m 2m 3m 4m 5m 6m 2p 3p 4p 6s 7s 8s E S")]
        [TestCase("1m 2m 3m 4m 5m 6m 2p 3p 4p E E E 5m 7p")]
        public void CanWinStandardHand_ReturnsFalseForNonWinningHand(string handText)
        {
            Assert.That(CanWinStandardHand(handText), Is.False);
        }

        [Test]
        public void CanWinStandardHand_ReturnsFalseWhenHandContainsInvalidTile()
        {
            Type tileType = GetTileType();
            Array tiles = CreateTileArray("1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m", 14);

            Assert.That(tiles.GetValue(13), Is.EqualTo(Activator.CreateInstance(tileType)));
            Assert.That(CanWinStandardHand(tiles), Is.False);
        }

        private static bool CanWinStandardHand(string handText)
        {
            return CanWinStandardHand(CreateTileArray(handText));
        }

        private static bool CanWinStandardHand(Array tiles)
        {
            Type checkerType = Type.GetType(HandWinCheckerTypeName, true);
            object checker = Activator.CreateInstance(checkerType);
            MethodInfo method = checkerType.GetMethod("CanWinStandardHand");
            Assert.That(method, Is.Not.Null);

            return (bool)method.Invoke(checker, new object[] { tiles });
        }

        private static Array CreateTileArray(string handText)
        {
            string[] codes = SplitCodes(handText);
            return CreateTileArray(codes, codes.Length);
        }

        private static Array CreateTileArray(string handText, int length)
        {
            return CreateTileArray(SplitCodes(handText), length);
        }

        private static Array CreateTileArray(string[] codes, int length)
        {
            Type tileType = GetTileType();
            Array tiles = Array.CreateInstance(tileType, length);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);

            for (int i = 0; i < codes.Length; i++)
            {
                tiles.SetValue(constructor.Invoke(new object[] { codes[i] }), i);
            }

            return tiles;
        }

        private static string[] SplitCodes(string handText)
        {
            return handText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static Type GetTileType()
        {
            return Type.GetType(TileTypeName, true);
        }
    }
}
