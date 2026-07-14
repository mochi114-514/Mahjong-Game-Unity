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
            bool isClosed,
            bool isIppatsuEligible,
            bool isDoubleReachDeclared,
            bool isFirstTurnTsumoEligible,
            bool isLastLiveWallDraw,
            bool isLastLiveWallDiscard)
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
                isIppatsuEligible,
                isDoubleReachDeclared,
                isFirstTurnTsumoEligible,
                isLastLiveWallDraw,
                isLastLiveWallDiscard,
                null)
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
                false,
                false,
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
                isIppatsuEligible,
                false,
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
            bool isIppatsuEligible,
            bool isDoubleReachDeclared)
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
                isIppatsuEligible,
                isDoubleReachDeclared,
                false,
                false,
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
            bool isIppatsuEligible,
            bool isDoubleReachDeclared,
            bool isFirstTurnTsumoEligible)
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
                isIppatsuEligible,
                isDoubleReachDeclared,
                isFirstTurnTsumoEligible,
                false,
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
            bool isIppatsuEligible,
            bool isDoubleReachDeclared,
            bool isFirstTurnTsumoEligible,
            bool isLastLiveWallDraw,
            bool isLastLiveWallDiscard,
            IReadOnlyList<PlayerMeld> melds)
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
            IsDoubleReachDeclared = isDoubleReachDeclared;
            IsFirstTurnTsumoEligible = isFirstTurnTsumoEligible;
            IsLastLiveWallDraw = isLastLiveWallDraw;
            IsLastLiveWallDiscard = isLastLiveWallDiscard;
            Melds = melds ?? Array.Empty<PlayerMeld>();
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
        public bool IsDoubleReachDeclared { get; }
        public bool IsFirstTurnTsumoEligible { get; }
        public bool IsLastLiveWallDraw { get; }
        public bool IsLastLiveWallDiscard { get; }
        public IReadOnlyList<PlayerMeld> Melds { get; }
    }
}
