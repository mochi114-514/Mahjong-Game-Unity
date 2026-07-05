using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class WinDeclarationEvaluationContext
    {
        public WinDeclarationEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
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
                winnerSeat,
                sourceSeat,
                roundWind,
                seatWind,
                isReachDeclared,
                isClosed,
                false)
        {
        }

        public WinDeclarationEvaluationContext(
            IReadOnlyList<Tile> handTiles,
            Tile winningTile,
            WinType winType,
            SeatId winnerSeat,
            SeatId? sourceSeat,
            RoundWind roundWind,
            SeatId seatWind,
            bool isReachDeclared,
            bool isClosed,
            bool isIppatsuEligible)
        {
            HandTiles = handTiles ?? throw new ArgumentNullException(nameof(handTiles));
            WinningTile = winningTile;
            WinType = winType;
            WinnerSeat = winnerSeat;
            SourceSeat = sourceSeat;
            RoundWind = roundWind;
            SeatWind = seatWind;
            IsReachDeclared = isReachDeclared;
            IsClosed = isClosed;
            IsIppatsuEligible = isIppatsuEligible;
        }

        public IReadOnlyList<Tile> HandTiles { get; }
        public Tile WinningTile { get; }
        public WinType WinType { get; }
        public SeatId WinnerSeat { get; }
        public SeatId? SourceSeat { get; }
        public RoundWind RoundWind { get; }
        public SeatId SeatWind { get; }
        public bool IsReachDeclared { get; }
        public bool IsClosed { get; }
        public bool IsIppatsuEligible { get; }
    }
}
