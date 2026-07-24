using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class RoundResult
    {
        private static readonly IReadOnlyList<EvaluatedYaku> EmptyYakus =
            new List<EvaluatedYaku>().AsReadOnly();

        private RoundResult(
            RoundResultType type,
            WindProgress windProgress,
            int turnIndex,
            bool isFinalRound,
            SeatId? winnerSeat,
            WinType? winType,
            SeatId? sourceSeat,
            Tile? winningTile,
            HandEvaluationCandidateResult selectedCandidate,
            AbortiveDrawKind? abortiveDrawKind)
        {
            Type = type;
            WindProgress = windProgress;
            TurnIndex = turnIndex;
            IsFinalRound = isFinalRound;
            WinnerSeat = winnerSeat;
            WinType = winType;
            SourceSeat = sourceSeat;
            WinningTile = winningTile;
            SelectedCandidate = selectedCandidate;
            AbortiveDrawKind = abortiveDrawKind;
        }

        public RoundResultType Type { get; }
        public WindProgress WindProgress { get; }
        public int TurnIndex { get; }
        public bool IsFinalRound { get; }
        public SeatId? WinnerSeat { get; }
        public WinType? WinType { get; }
        public SeatId? SourceSeat { get; }
        public Tile? WinningTile { get; }
        public HandEvaluationCandidateResult SelectedCandidate { get; }
        public AbortiveDrawKind? AbortiveDrawKind { get; }
        public IReadOnlyList<EvaluatedYaku> Yakus =>
            SelectedCandidate == null ? EmptyYakus : SelectedCandidate.Yakus;
        public int TotalHan => SelectedCandidate == null ? 0 : SelectedCandidate.TotalHan;
        public int TotalYakumanMultiplier =>
            SelectedCandidate == null ? 0 : SelectedCandidate.TotalYakumanMultiplier;
        public bool HasYakuman => TotalYakumanMultiplier > 0;

        public static RoundResult CreateWin(
            WindProgress windProgress,
            int turnIndex,
            SeatId winnerSeat,
            WinType winType,
            SeatId? sourceSeat,
            Tile? winningTile,
            HandEvaluationCandidateResult selectedCandidate,
            bool isFinalRound)
        {
            return new RoundResult(
                RoundResultType.Win,
                windProgress,
                turnIndex,
                isFinalRound,
                winnerSeat,
                winType,
                sourceSeat,
                winningTile,
                selectedCandidate,
                null);
        }

        public static RoundResult CreateExhaustiveDraw(
            WindProgress windProgress,
            int turnIndex,
            bool isFinalRound)
        {
            return new RoundResult(
                RoundResultType.ExhaustiveDraw,
                windProgress,
                turnIndex,
                isFinalRound,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        public static RoundResult CreateAbortiveDraw(
            WindProgress windProgress,
            int turnIndex,
            AbortiveDrawKind kind)
        {
            if (!Enum.IsDefined(typeof(AbortiveDrawKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            return new RoundResult(
                RoundResultType.AbortiveDraw,
                windProgress,
                turnIndex,
                false,
                null,
                null,
                null,
                null,
                null,
                kind);
        }

    }

    /// <summary>
    /// Formats a positive yakuman multiplier for result presentation.
    /// </summary>
    public static class YakumanMultiplierFormatter
    {
        private static readonly string[] Digits =
        {
            "零", "一", "二", "三", "四", "五", "六", "七", "八", "九"
        };

        private static readonly string[] PlaceValues = { "千", "百", "十", string.Empty };
        private static readonly string[] LargeUnits = { string.Empty, "万", "億" };

        public static string Format(int yakumanMultiplier)
        {
            if (yakumanMultiplier <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(yakumanMultiplier),
                    "Yakuman multiplier must be positive.");
            }

            return yakumanMultiplier == 1
                ? "役満"
                : FormatKanjiNumber(yakumanMultiplier) + "倍役満";
        }

        private static string FormatKanjiNumber(int value)
        {
            List<int> groups = new List<int>();
            while (value > 0)
            {
                groups.Add(value % 10000);
                value /= 10000;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                int group = groups[i];
                if (group == 0)
                    continue;

                builder.Append(FormatGroup(group));
                builder.Append(LargeUnits[i]);
            }

            return builder.ToString();
        }

        private static string FormatGroup(int value)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int divisor = 1000;
            for (int i = 0; i < PlaceValues.Length; i++)
            {
                int digit = value / divisor;
                value %= divisor;
                divisor /= 10;

                if (digit == 0)
                    continue;

                if (digit > 1 || i == PlaceValues.Length - 1)
                    builder.Append(Digits[digit]);

                builder.Append(PlaceValues[i]);
            }

            return builder.ToString();
        }
    }
}
