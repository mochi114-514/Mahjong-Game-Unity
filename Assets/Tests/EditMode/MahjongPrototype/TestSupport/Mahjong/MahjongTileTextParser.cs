using System;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal static class MahjongTileTextParser
    {
        public static string[] ParseTileCodes(string text)
        {
            return text.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
