using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinChecker
    {
        private readonly WinningHandAnalyzer analyzer;

        public WinChecker()
            : this(new WinningHandAnalyzer())
        {
        }

        internal WinChecker(WinningHandAnalyzer analyzer)
        {
            this.analyzer = analyzer ?? new WinningHandAnalyzer();
        }

        public bool CanWinWithTile(IReadOnlyList<Tile> handTiles, Tile winningTile)
        {
            return analyzer.AnalyzeWithTile(handTiles, winningTile).CanWin;
        }

        public WinCheckResult CheckWinWithTile(IReadOnlyList<Tile> handTiles, Tile winningTile)
        {
            return ToWinCheckResult(analyzer.AnalyzeWithTile(handTiles, winningTile));
        }

        public WinCheckResult CheckCompletedHand(IReadOnlyList<Tile> tiles)
        {
            return ToWinCheckResult(analyzer.AnalyzeCompletedHand(tiles));
        }

        public bool CanWinStandardHand(IReadOnlyList<Tile> tiles)
        {
            return analyzer.AnalyzeCompletedHand(tiles).StandardDecompositions.Count > 0;
        }

        internal WinningHandAnalysisResult AnalyzeWithTileDetailed(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile)
        {
            return analyzer.AnalyzeWithTile(handTiles, winningTile);
        }

        internal static WinCheckResult ToWinCheckResult(WinningHandAnalysisResult analysisResult)
        {
            if (analysisResult == null || !analysisResult.CanWin)
                return WinCheckResult.NotWin;

            if (analysisResult.StandardDecompositions.Count > 0)
                return WinCheckResult.Win(WinningHandShape.Standard);

            if (analysisResult.SevenPairsAnalysis.IsWin)
                return WinCheckResult.Win(WinningHandShape.SevenPairs);

            if (analysisResult.ThirteenOrphansAnalysis.IsWin)
                return WinCheckResult.Win(WinningHandShape.ThirteenOrphans);

            return WinCheckResult.NotWin;
        }
    }
}
