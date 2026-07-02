using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class ReachCheckerTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string ReachCheckerTypeName = "MahjongPrototype.Services.ReachChecker, Assembly-CSharp";

        [Test]
        public void CheckReach_ReturnsCandidatesForReadyHand()
        {
            object result = CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            Assert.That(GetProperty(result, "CanReach"), Is.True);
            Assert.That(GetCandidateCount(result), Is.GreaterThan(0));
        }

        [Test]
        public void CheckReach_ReturnsMultipleCandidates()
        {
            object result = CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            Assert.That(GetCandidateCount(result), Is.GreaterThanOrEqualTo(2));
            Assert.That(FindCandidate(result, "Hand", "5m"), Is.Not.Null);
            Assert.That(FindCandidate(result, "DrawnTile", "6m"), Is.Not.Null);
        }

        [Test]
        public void CheckReach_ReturnsDrawnTileCandidateForTsumogiriReach()
        {
            object result = CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            object candidate = FindCandidate(result, "DrawnTile", "6m");

            Assert.That(candidate, Is.Not.Null);
            Assert.That(GetProperty(candidate, "HandIndex"), Is.EqualTo(-1));
        }

        [Test]
        public void CheckReach_ReturnsHandIndexForHandDiscardCandidate()
        {
            object result = CheckReach(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m",
                "6m");

            object candidate = FindCandidate(result, "Hand", "5m");

            Assert.That(candidate, Is.Not.Null);
            Assert.That(GetProperty(candidate, "HandIndex"), Is.EqualTo(12));
        }

        [Test]
        public void CheckReach_ReturnsNotReadyForNonReadyHand()
        {
            object result = CheckReach(
                "1m 4m 7m 2p 5p 8p 3s 6s 9s E S W N",
                "P");

            Assert.That(GetProperty(result, "CanReach"), Is.False);
            Assert.That(GetCandidateCount(result), Is.EqualTo(0));
        }

        [Test]
        public void CheckReach_ReturnsNotReadyForInvalidInput()
        {
            object shortHandResult = CheckReach("1m 2m 3m", "4m");
            object missingDrawnTileResult = CheckReach(CreateTileArray(
                "1m 2m 3m 2p 3p 4p 7s 8s 9s E E E 5m"), CreateInvalidTile());

            Assert.That(GetProperty(shortHandResult, "CanReach"), Is.False);
            Assert.That(GetCandidateCount(shortHandResult), Is.EqualTo(0));
            Assert.That(GetProperty(missingDrawnTileResult, "CanReach"), Is.False);
            Assert.That(GetCandidateCount(missingDrawnTileResult), Is.EqualTo(0));
        }

        private static object CheckReach(string handText, string drawnTileCode)
        {
            return CheckReach(CreateTileArray(handText), CreateTile(drawnTileCode));
        }

        private static object CheckReach(Array handTiles, object drawnTile)
        {
            Type checkerType = Type.GetType(ReachCheckerTypeName, true);
            object checker = Activator.CreateInstance(checkerType);
            MethodInfo method = checkerType.GetMethod("CheckReach");
            Assert.That(method, Is.Not.Null);

            return method.Invoke(checker, new[] { handTiles, drawnTile });
        }

        private static int GetCandidateCount(object result)
        {
            object candidates = GetProperty(result, "Candidates");
            return (int)GetProperty(candidates, "Count");
        }

        private static object FindCandidate(object result, string sourceName, string tileCode)
        {
            object candidates = GetProperty(result, "Candidates");
            int count = (int)GetProperty(candidates, "Count");

            for (int i = 0; i < count; i++)
            {
                object candidate = GetListItem(candidates, i);
                if (GetProperty(candidate, "Source").ToString() == sourceName &&
                    GetProperty(candidate, "Tile").ToString() == tileCode)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Array CreateTileArray(string handText)
        {
            string[] codes = SplitCodes(handText);
            Type tileType = GetTileType();
            Array tiles = Array.CreateInstance(tileType, codes.Length);

            for (int i = 0; i < codes.Length; i++)
                tiles.SetValue(CreateTile(codes[i]), i);

            return tiles;
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

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
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
