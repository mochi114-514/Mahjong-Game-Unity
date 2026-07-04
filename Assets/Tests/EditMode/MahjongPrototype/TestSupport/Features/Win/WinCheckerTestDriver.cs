using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinCheckerTestDriver
    {
        private const string WinCheckerTypeName =
            "MahjongPrototype.Services.WinChecker, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object winChecker;

        private WinCheckerTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory,
            object winChecker)
        {
            this.reflection = reflection;
            this.types = types;
            this.dataFactory = dataFactory;
            this.winChecker = winChecker;
        }

        public static WinCheckerTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object winChecker = reflection.CreateInstance(
                reflection.RequireType(WinCheckerTypeName));
            return new WinCheckerTestDriver(reflection, types, dataFactory, winChecker);
        }

        public bool CanWinStandardHand(string handText)
        {
            return CanWinStandardHand(CreateTiles(handText));
        }

        public bool CanWinStandardHand(object tiles)
        {
            return (bool)reflection.Invoke(winChecker, "CanWinStandardHand", tiles);
        }

        public bool CanWinWithTile(string handText, string winningTileCode)
        {
            return (bool)reflection.Invoke(
                winChecker,
                "CanWinWithTile",
                CreateTiles(handText),
                dataFactory.CreateTile(winningTileCode));
        }

        public object CheckWinWithTile(string handText, string winningTileCode)
        {
            return reflection.Invoke(
                winChecker,
                "CheckWinWithTile",
                CreateTiles(handText),
                dataFactory.CreateTile(winningTileCode));
        }

        public object CheckCompletedHand(string handText)
        {
            return CheckCompletedHand(CreateTiles(handText));
        }

        public object CheckCompletedHand(object tiles)
        {
            return reflection.Invoke(winChecker, "CheckCompletedHand", tiles);
        }

        public object CreateTiles(string handText)
        {
            return dataFactory.CreateTileArrayFromText(handText);
        }

        public object CreateTiles(string handText, int length)
        {
            string[] codes = MahjongTileTextParser.ParseTileCodes(handText);
            Array tiles = Array.CreateInstance(types.Tile, length);
            for (int i = 0; i < codes.Length; i++)
                tiles.SetValue(dataFactory.CreateTile(codes[i]), i);

            return tiles;
        }

        public object CreateInvalidTile()
        {
            return dataFactory.CreateInvalidTile();
        }

        public object TileAt(object tiles, int index)
        {
            return ((Array)tiles).GetValue(index);
        }

        public bool ResultCanWin(object result)
        {
            return (bool)reflection.GetProperty(result, "CanWin");
        }

        public string ResultShapeName(object result)
        {
            return reflection.GetProperty(result, "Shape").ToString();
        }
    }
}
