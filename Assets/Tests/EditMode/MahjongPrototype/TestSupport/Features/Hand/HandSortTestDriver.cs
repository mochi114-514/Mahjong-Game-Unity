using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Hand
{
    internal sealed class HandSortTestDriver
    {
        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;

        private HandSortTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
        }

        public static HandSortTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new HandSortTestDriver(reflection, dataFactory);
        }

        public object CreateHand(string handText)
        {
            return dataFactory.CreateHand(SplitCodes(handText));
        }

        public object CreateEmptyHand()
        {
            return dataFactory.CreateHand();
        }

        public void AddTile(object hand, string tileCode)
        {
            reflection.Invoke(hand, "Add", dataFactory.CreateTile(tileCode));
        }

        public void AddInvalidTile(object hand)
        {
            reflection.Invoke(hand, "Add", dataFactory.CreateInvalidTile());
        }

        public void SortByTypeIndex(object hand)
        {
            reflection.Invoke(hand, "SortByTypeIndex");
        }

        public int TileCount(object hand)
        {
            return GetTiles(hand).Length;
        }

        public int TileTypeIndexAt(object hand, int index)
        {
            return (int)reflection.GetProperty(GetTiles(hand).GetValue(index), "TypeIndex");
        }

        public string DisplayString(object hand)
        {
            return (string)reflection.Invoke(hand, "ToDisplayString");
        }

        private Array GetTiles(object hand)
        {
            return (Array)reflection.Invoke(hand, "GetTiles");
        }

        private static string[] SplitCodes(string handText)
        {
            return handText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

