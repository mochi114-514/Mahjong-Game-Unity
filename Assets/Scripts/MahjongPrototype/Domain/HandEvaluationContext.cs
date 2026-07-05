using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class HandEvaluationContext
    {
        public HandEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            WinningHandShape shape,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed)
            : this(
                handTiles,
                winningTile,
                winType,
                shape,
                winnerSeat,
                sourceSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                false,
                false)
        {
        }

        public HandEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            WinningHandShape shape,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed,
            bool isIppatsuEligible)
            : this(
                handTiles,
                winningTile,
                winType,
                shape,
                winnerSeat,
                sourceSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                isIppatsuEligible,
                false)
        {
        }

        public HandEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            WinningHandShape shape,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed,
            bool isIppatsuEligible,
            bool isDoubleReachDeclared)
            : this(
                handTiles,
                winningTile,
                winType,
                shape,
                WinningHandAnalysisResult.NotWin,
                winnerSeat,
                sourceSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                isIppatsuEligible,
                isDoubleReachDeclared)
        {
        }

        public HandEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            WinningHandShape shape,
            WinningHandAnalysisResult winningHandAnalysis,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed)
            : this(
                handTiles,
                winningTile,
                winType,
                shape,
                winningHandAnalysis,
                winnerSeat,
                sourceSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                false,
                false)
        {
        }

        public HandEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            WinningHandShape shape,
            WinningHandAnalysisResult winningHandAnalysis,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed,
            bool isIppatsuEligible)
            : this(
                handTiles,
                winningTile,
                winType,
                shape,
                winningHandAnalysis,
                winnerSeat,
                sourceSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                isIppatsuEligible,
                false)
        {
        }

        public HandEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            WinningHandShape shape,
            WinningHandAnalysisResult winningHandAnalysis,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed,
            bool isIppatsuEligible,
            bool isDoubleReachDeclared)
        {
            HandTiles = handTiles ?? throw new ArgumentNullException(nameof(handTiles));
            WinningTile = winningTile;
            WinType = winType;
            Shape = shape;
            WinningHandAnalysis = winningHandAnalysis ?? WinningHandAnalysisResult.NotWin;
            WinnerSeat = winnerSeat;
            SourceSeat = sourceSeat;
            RoundWind = roundWind;
            SeatWind = seatWind;
            IsReachDeclared = isReachDeclared;
            IsClosed = isClosed;
            IsIppatsuEligible = isIppatsuEligible;
            IsDoubleReachDeclared = isDoubleReachDeclared;
        }

        public IReadOnlyList<Tile> HandTiles { get; }
        public Tile WinningTile { get; }
        public WinType WinType { get; }
        public WinningHandShape Shape { get; }
        public WinningHandAnalysisResult WinningHandAnalysis { get; }
        public SeatId WinnerSeat { get; }
        public SeatId? SourceSeat { get; }
        public RoundWind RoundWind { get; }
        public SeatId SeatWind { get; }
        public bool IsReachDeclared { get; }
        public bool IsClosed { get; }
        public bool IsIppatsuEligible { get; }
        public bool IsDoubleReachDeclared { get; }
    }
}
