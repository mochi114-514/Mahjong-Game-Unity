using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class WinDecisionService
    {
        private readonly WinDeclarationEvaluator winDeclarationEvaluator;
        private readonly FuritenEvaluator furitenEvaluator;
        private readonly NoYakuTenpaiEvaluator noYakuTenpaiEvaluator;

        public WinDecisionService(
            WinDeclarationEvaluator winDeclarationEvaluator,
            FuritenEvaluator furitenEvaluator,
            NoYakuTenpaiEvaluator noYakuTenpaiEvaluator)
        {
            this.winDeclarationEvaluator = winDeclarationEvaluator ??
                throw new ArgumentNullException(nameof(winDeclarationEvaluator));
            this.furitenEvaluator = furitenEvaluator ??
                throw new ArgumentNullException(nameof(furitenEvaluator));
            this.noYakuTenpaiEvaluator = noYakuTenpaiEvaluator;
        }

        public FuritenEvaluationResultSet EvaluateAllFuriten(MahjongGameState gameState)
        {
            return furitenEvaluator.EvaluateAll(gameState);
        }

        public NoYakuTenpaiEvaluationResult EvaluateSelfNoYakuTenpai(MahjongGameState gameState)
        {
            if (gameState == null || noYakuTenpaiEvaluator == null || gameState.IsRoundEnded)
                return NoYakuTenpaiEvaluationResult.NotTenpai;

            PlayerSeat selfPlayerSeat = gameState.GetPlayerSeat(gameState.SelfSeat);
            if (selfPlayerSeat.Hand.Count != 13 || selfPlayerSeat.HasDrawnTile)
                return NoYakuTenpaiEvaluationResult.NotTenpai;

            return noYakuTenpaiEvaluator.Evaluate(
                selfPlayerSeat.Hand.GetTiles(),
                gameState.SelfSeat,
                gameState.WindProgress.RoundWind,
                gameState.SelfSeat,
                selfPlayerSeat.IsReachDeclared,
                true);
        }

        public WinDecisionEvaluation EvaluateTsumo(MahjongGameState gameState)
        {
            if (gameState == null)
                return WinDecisionEvaluation.None;

            SeatId seat = gameState.CurrentTurn;
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            Tile? winningTile = playerSeat.DrawnTile;
            WinDeclarationEvaluationResult evaluationResult = winningTile.HasValue
                ? winDeclarationEvaluator.EvaluateWithTile(CreateContext(
                    gameState,
                    playerSeat,
                    WinType.Tsumo,
                    winningTile.Value,
                    null))
                : WinDeclarationEvaluationResult.NotWinningShape(WinCheckResult.NotWin);
            bool canDeclareWin = evaluationResult.CanDeclareWin;

            if (canDeclareWin)
            {
                gameState.BeginWinDecisionDetailed(
                    seat,
                    WinType.Tsumo,
                    winningTile,
                    null,
                    gameState.TurnIndex,
                    evaluationResult);
            }
            else
            {
                gameState.ClearWinDecision();
            }

            return new WinDecisionEvaluation(
                new[] { new WinCheckNotification(seat, WinType.Tsumo, winningTile, null, gameState.TurnIndex, canDeclareWin) },
                canDeclareWin);
        }

        public WinDecisionEvaluation EvaluateRon(MahjongGameState gameState, DiscardRecord discard)
        {
            if (gameState == null)
                return WinDecisionEvaluation.None;

            FuritenEvaluationResultSet furitenResults = furitenEvaluator.EvaluateAll(gameState);
            List<WinCheckNotification> notifications = new List<WinCheckNotification>();
            RonWinCandidate? candidate = null;

            // PROTOTYPE: only local participants currently receive the single ron decision.
            for (int i = 0; i < gameState.SeatSlots.Count; i++)
            {
                SeatSlot slot = gameState.SeatSlots[i];
                if (!slot.HasPlayer || slot.Wind == discard.ActorSeat ||
                    slot.ParticipantType != ParticipantType.LocalHuman)
                {
                    continue;
                }

                PlayerSeat playerSeat = gameState.GetPlayerSeat(slot.Wind);
                WinDeclarationEvaluationResult evaluationResult =
                    winDeclarationEvaluator.EvaluateWithTile(CreateContext(
                        gameState,
                        playerSeat,
                        WinType.Ron,
                        discard.Tile,
                        discard.ActorSeat,
                        discard));
                if (IsNoYakuWinningShape(evaluationResult, playerSeat))
                    playerSeat.MarkTemporaryFuriten();

                bool passesFuritenCheck = furitenResults.TryGet(
                    slot.Wind,
                    out FuritenSeatEvaluationResult furitenResult) &&
                    furitenResult.IsEvaluated && !furitenResult.IsFuriten;
                bool canDeclareWin = evaluationResult.CanDeclareWin && passesFuritenCheck;
                notifications.Add(new WinCheckNotification(
                    slot.Wind,
                    WinType.Ron,
                    discard.Tile,
                    discard.ActorSeat,
                    discard.TurnIndex,
                    canDeclareWin));

                if (!canDeclareWin)
                    continue;

                candidate = new RonWinCandidate(slot.Wind, evaluationResult);
                break;
            }

            return new WinDecisionEvaluation(notifications, candidate.HasValue, candidate);
        }

        public WinDecisionDeclineResult Decline(MahjongGameState gameState)
        {
            if (gameState == null || gameState.TurnPhase != TurnPhase.WinDecision)
                return WinDecisionDeclineResult.None;

            SeatId seat = gameState.WinDecisionSeat;
            WinType? winType = gameState.WinDecisionType;
            int turnIndex = gameState.WinDecisionTurnIndex;
            bool shouldEndAfterLastRon =
                winType == WinType.Ron && IsLastDiscardLastLiveWallDiscard(gameState);
            MarkDeclinedRonFuriten(gameState, seat, winType);
            gameState.ClearWinDecision();
            return new WinDecisionDeclineResult(
                true,
                seat,
                winType,
                turnIndex,
                shouldEndAfterLastRon);
        }

        public void MarkDeclinedRonFuriten(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null)
                return;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.IsReachDeclared)
                playerSeat.MarkReachPassFuriten();
            else
                playerSeat.MarkTemporaryFuriten();
        }

        public void SetPending(MahjongGameState gameState, bool isPending, SeatId seat, int turnIndex)
        {
            if (gameState == null)
                return;

            if (isPending)
                gameState.BeginWinDecision(seat, turnIndex);
            else
                gameState.ClearWinDecision();
        }

        public void SetPendingDetailed(
            MahjongGameState gameState,
            SeatId seat,
            WinType winType,
            Tile winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            WinDeclarationEvaluationResult evaluationResult)
        {
            if (gameState == null)
                return;

            gameState.BeginWinDecisionDetailed(
                seat, winType, winningTile, sourceSeat, turnIndex, evaluationResult);
        }

        private static WinDeclarationEvaluationContext CreateContext(
            MahjongGameState gameState,
            PlayerSeat playerSeat,
            WinType winType,
            Tile winningTile,
            SeatId? sourceSeat,
            DiscardRecord? sourceDiscard = null)
        {
            SeatId winnerSeat = playerSeat.SeatId;
            return new WinDeclarationEvaluationContext(
                playerSeat.Hand.GetTiles(),
                winningTile,
                winType,
                winnerSeat,
                sourceSeat,
                gameState.WindProgress.RoundWind,
                winnerSeat,
                playerSeat.IsReachDeclared,
                true,
                playerSeat.IsIppatsuEligible,
                playerSeat.IsDoubleReachDeclared,
                IsFirstTurnTsumoEligible(gameState, winnerSeat, winType),
                IsLastLiveWallDraw(gameState, winnerSeat, winningTile, winType),
                winType == WinType.Ron && sourceDiscard.HasValue &&
                    sourceDiscard.Value.IsLastLiveWallDiscard);
        }

        private static bool IsNoYakuWinningShape(
            WinDeclarationEvaluationResult evaluationResult,
            PlayerSeat playerSeat)
        {
            return evaluationResult != null && evaluationResult.IsWinningShape &&
                !evaluationResult.HasYaku && playerSeat != null &&
                !playerSeat.IsReachDeclared;
        }

        private void MarkDeclinedRonFuriten(
            MahjongGameState gameState,
            SeatId seat,
            WinType? winType)
        {
            if (winType != WinType.Ron)
                return;

            MarkDeclinedRonFuriten(gameState, seat);
        }

        private static bool IsLastDiscardLastLiveWallDiscard(MahjongGameState gameState)
        {
            return gameState.Discards.Count > 0 &&
                gameState.Discards[gameState.Discards.Count - 1].IsLastLiveWallDiscard;
        }

        private static bool IsFirstTurnTsumoEligible(
            MahjongGameState gameState,
            SeatId seat,
            WinType winType)
        {
            if (winType != WinType.Tsumo)
                return false;

            bool hasAnyDiscard = false;
            for (int i = 0; i < gameState.Discards.Count; i++)
            {
                hasAnyDiscard = true;
                if (gameState.Discards[i].ActorSeat == seat)
                    return false;
            }

            return seat != SeatId.East || !hasAnyDiscard;
        }

        private static bool IsLastLiveWallDraw(
            MahjongGameState gameState,
            SeatId seat,
            Tile winningTile,
            WinType winType)
        {
            if (winType != WinType.Tsumo || !gameState.LastTurnDraw.HasValue)
                return false;

            TurnDrawRecord record = gameState.LastTurnDraw.Value;
            return record.IsLastLiveWallDraw && record.ActorSeat == seat &&
                record.TurnIndex == gameState.TurnIndex && record.Tile.Equals(winningTile);
        }
    }

    public readonly struct WinCheckNotification
    {
        public WinCheckNotification(SeatId seat, WinType winType, Tile? tile, SeatId? sourceSeat, int turnIndex, bool canDeclareWin)
        {
            Seat = seat; WinType = winType; Tile = tile; SourceSeat = sourceSeat;
            TurnIndex = turnIndex; CanDeclareWin = canDeclareWin;
        }
        public SeatId Seat { get; }
        public WinType WinType { get; }
        public Tile? Tile { get; }
        public SeatId? SourceSeat { get; }
        public int TurnIndex { get; }
        public bool CanDeclareWin { get; }
    }

    public readonly struct WinDecisionEvaluation
    {
        public static WinDecisionEvaluation None => new WinDecisionEvaluation(
            Array.Empty<WinCheckNotification>(), false, null);
        public WinDecisionEvaluation(
            IReadOnlyList<WinCheckNotification> notifications,
            bool decisionStarted,
            RonWinCandidate? ronCandidate = null)
        {
            Notifications = notifications ?? Array.Empty<WinCheckNotification>();
            DecisionStarted = decisionStarted;
            RonCandidate = ronCandidate;
        }
        public IReadOnlyList<WinCheckNotification> Notifications { get; }
        public bool DecisionStarted { get; }
        public RonWinCandidate? RonCandidate { get; }
    }

    public readonly struct RonWinCandidate
    {
        public RonWinCandidate(SeatId seat, WinDeclarationEvaluationResult evaluationResult)
        {
            Seat = seat;
            EvaluationResult = evaluationResult;
        }

        public SeatId Seat { get; }
        public WinDeclarationEvaluationResult EvaluationResult { get; }
    }

    public readonly struct WinDecisionDeclineResult
    {
        public static WinDecisionDeclineResult None => new WinDecisionDeclineResult(false, default, null, 0, false);
        public WinDecisionDeclineResult(bool wasPending, SeatId seat, WinType? winType, int turnIndex, bool shouldEndAfterLastRon)
        { WasPending = wasPending; Seat = seat; WinType = winType; TurnIndex = turnIndex; ShouldEndAfterLastRon = shouldEndAfterLastRon; }
        public bool WasPending { get; }
        public SeatId Seat { get; }
        public WinType? WinType { get; }
        public int TurnIndex { get; }
        public bool ShouldEndAfterLastRon { get; }
    }
}
