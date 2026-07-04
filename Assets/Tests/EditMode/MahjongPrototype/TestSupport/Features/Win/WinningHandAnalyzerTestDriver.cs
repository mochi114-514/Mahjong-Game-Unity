using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinningHandAnalyzerTestDriver
    {
        private const string WinningHandAnalyzerTypeName =
            "MahjongPrototype.Services.WinningHandAnalyzer, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object analyzer;

        private WinningHandAnalyzerTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory,
            object analyzer)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.types = types;
            this.dataFactory = dataFactory;
            this.analyzer = analyzer;
        }

        public static WinningHandAnalyzerTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object analyzer = reflection.CreateInstance(
                reflection.RequireType(WinningHandAnalyzerTypeName));
            return new WinningHandAnalyzerTestDriver(
                reflection,
                collections,
                types,
                dataFactory,
                analyzer);
        }

        public object AnalyzeCompletedHand(string handText)
        {
            return AnalyzeCompletedHand(CreateTiles(handText));
        }

        public object AnalyzeCompletedHand(object tiles)
        {
            return reflection.Invoke(
                analyzer,
                "AnalyzeCompletedHand",
                new object[] { tiles });
        }

        public object AnalyzeWithTile(string handText, string winningTileCode)
        {
            return AnalyzeWithTile(
                CreateTiles(handText),
                dataFactory.CreateTile(winningTileCode));
        }

        public object AnalyzeWithTile(object handTiles, object winningTile)
        {
            return reflection.Invoke(
                analyzer,
                "AnalyzeWithTile",
                new[] { handTiles, winningTile });
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

        public void SetTile(object tiles, int index, string tileCode)
        {
            ((Array)tiles).SetValue(dataFactory.CreateTile(tileCode), index);
        }

        public bool CanWin(object analysisResult)
        {
            return (bool)reflection.GetProperty(analysisResult, "CanWin");
        }

        public object StandardDecompositions(object analysisResult)
        {
            return reflection.GetProperty(analysisResult, "StandardDecompositions");
        }

        public int StandardDecompositionCount(object analysisResult)
        {
            return collections.Count(StandardDecompositions(analysisResult));
        }

        public object StandardDecompositionAt(object analysisResult, int index)
        {
            return collections.Item(StandardDecompositions(analysisResult), index);
        }

        public object FirstStandardDecomposition(object analysisResult)
        {
            return StandardDecompositionAt(analysisResult, 0);
        }

        public string PairTileCode(object decomposition)
        {
            return TileCode(reflection.GetProperty(decomposition, "PairTile"));
        }

        public object Melds(object decomposition)
        {
            return reflection.GetProperty(decomposition, "Melds");
        }

        public object FirstMeld(object decomposition)
        {
            return collections.Item(Melds(decomposition), 0);
        }

        public string[] MeldKeys(object decomposition)
        {
            object melds = Melds(decomposition);
            List<string> keys = new List<string>();
            for (int i = 0; i < collections.Count(melds); i++)
                keys.Add(MeldKey(collections.Item(melds, i)));

            keys.Sort(StringComparer.Ordinal);
            return keys.ToArray();
        }

        public object SevenPairsAnalysis(object analysisResult)
        {
            return reflection.GetProperty(analysisResult, "SevenPairsAnalysis");
        }

        public bool SevenPairsIsWin(object analysisResult)
        {
            return (bool)reflection.GetProperty(SevenPairsAnalysis(analysisResult), "IsWin");
        }

        public string[] SevenPairTileCodes(object analysisResult)
        {
            object pairTiles = reflection.GetProperty(SevenPairsAnalysis(analysisResult), "PairTiles");
            return TileCodes(pairTiles);
        }

        public object ThirteenOrphansAnalysis(object analysisResult)
        {
            return reflection.GetProperty(analysisResult, "ThirteenOrphansAnalysis");
        }

        public bool ThirteenOrphansIsWin(object analysisResult)
        {
            return (bool)reflection.GetProperty(ThirteenOrphansAnalysis(analysisResult), "IsWin");
        }

        public string ThirteenOrphansPairTileCode(object analysisResult)
        {
            return TileCode(reflection.GetProperty(ThirteenOrphansAnalysis(analysisResult), "PairTile"));
        }

        public string[] ThirteenOrphansRequiredTileCodes(object analysisResult)
        {
            object requiredTiles =
                reflection.GetProperty(ThirteenOrphansAnalysis(analysisResult), "RequiredTiles");
            return TileCodes(requiredTiles);
        }

        private string MeldKey(object meld)
        {
            string typeName = reflection.GetProperty(meld, "Type").ToString();
            object tiles = reflection.GetProperty(meld, "Tiles");
            return typeName + ":" + string.Join(",", TileCodes(tiles));
        }

        private string[] TileCodes(object tiles)
        {
            List<string> codes = new List<string>();
            for (int i = 0; i < collections.Count(tiles); i++)
                codes.Add(TileCode(collections.Item(tiles, i)));

            return codes.ToArray();
        }

        private string TileCode(object tile)
        {
            return (string)reflection.GetProperty(tile, "Code");
        }
    }
}
