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
            HandEvaluationCandidateResult selectedCandidate)
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
        public IReadOnlyList<EvaluatedYaku> Yakus =>
            SelectedCandidate == null ? EmptyYakus : SelectedCandidate.Yakus;
        public int TotalHan => SelectedCandidate == null ? 0 : SelectedCandidate.TotalHan;
        public bool HasYakuman => SelectedCandidate != null && SelectedCandidate.HasYakuman;
        public int YakumanCount => CountYakuman(SelectedCandidate);

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
                selectedCandidate);
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
                null);
        }

        private static int CountYakuman(HandEvaluationCandidateResult candidate)
        {
            if (candidate == null || candidate.Yakus == null)
                return 0;

            int count = 0;
            for (int i = 0; i < candidate.Yakus.Count; i++)
            {
                if (candidate.Yakus[i].IsYakuman)
                    count++;
            }

            return count;
        }
    }
}
