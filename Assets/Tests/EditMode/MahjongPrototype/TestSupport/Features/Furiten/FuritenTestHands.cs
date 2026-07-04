namespace MahjongPrototype.Tests.TestSupport.Features.Furiten
{
    internal static class FuritenTestHands
    {
        private static readonly string[] EvaluatorSingleWaitTiles =
        {
            "1m", "2m", "3m",
            "1p", "2p", "3p",
            "1s", "2s", "3s",
            "E", "E", "E",
            "C"
        };

        private static readonly string[] EvaluatorMultiWaitTiles =
        {
            "1p", "2p", "3p",
            "1s", "2s", "3s",
            "E", "E", "E",
            "P", "P",
            "4m", "5m"
        };

        private static readonly string[] SevenPairsWaitTiles =
        {
            "1m", "1m", "2m", "2m",
            "3p", "3p", "4p", "4p",
            "5s", "5s",
            "E", "E",
            "C"
        };

        private static readonly string[] ThirteenOrphansWaitTiles =
        {
            "1m", "9m", "1p", "9p", "1s", "9s",
            "E", "S", "W", "N", "P", "F", "C"
        };

        private static readonly string[] NonTenpaiTiles =
        {
            "1m", "4m", "7m",
            "2p", "5p", "8p",
            "3s", "6s", "9s",
            "E", "S", "W", "N"
        };

        private static readonly string[] SimpleFiveManWaitTiles =
        {
            "2m", "3m", "4m",
            "2p", "3p", "4p",
            "2s", "3s", "4s",
            "6s", "7s", "8s",
            "5m"
        };

        private static readonly string[] NoYakuSingleWaitTiles =
        {
            "1m", "2m", "3m",
            "4m", "5m", "6m",
            "7p", "8p", "9p",
            "1s", "2s", "3s",
            "P"
        };

        private static readonly string[] RonMultiWaitTiles =
        {
            "2p", "3p", "4p",
            "3p", "4p", "5p",
            "2s", "3s", "4s",
            "6s", "6s",
            "4m", "5m"
        };

        public static string[] EvaluatorSingleWait()
        {
            return Copy(EvaluatorSingleWaitTiles);
        }

        public static string[] EvaluatorMultiWait()
        {
            return Copy(EvaluatorMultiWaitTiles);
        }

        public static string[] SevenPairsWait()
        {
            return Copy(SevenPairsWaitTiles);
        }

        public static string[] ThirteenOrphansWait()
        {
            return Copy(ThirteenOrphansWaitTiles);
        }

        public static string[] NonTenpai()
        {
            return Copy(NonTenpaiTiles);
        }

        public static string[] SimpleFiveManWait()
        {
            return Copy(SimpleFiveManWaitTiles);
        }

        public static string[] NoYakuSingleWait()
        {
            return Copy(NoYakuSingleWaitTiles);
        }

        public static string[] RonMultiWait()
        {
            return Copy(RonMultiWaitTiles);
        }

        private static string[] Copy(string[] source)
        {
            string[] copy = new string[source.Length];
            source.CopyTo(copy, 0);
            return copy;
        }
    }
}
