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
        // Compatibility projection for result presentation that has not yet migrated.
        public int YakumanCount => TotalYakumanMultiplier;

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
}
