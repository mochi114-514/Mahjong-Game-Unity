using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class ReachDecisionService
    {
        private readonly ReachChecker reachChecker;

        public ReachDecisionService(ReachChecker reachChecker)
        {
            this.reachChecker = reachChecker ?? throw new ArgumentNullException(nameof(reachChecker));
        }

        public ReachDecisionResult TryBeginAfterDraw(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null || gameState.IsRoundEnded || gameState.IsWinDecisionPending ||
                gameState.IsReactionWindowPending ||
                gameState.IsReachDecisionPending || gameState.IsReachDiscardSelectionPending ||
                !gameState.IsSelfTurn || !gameState.IsSelfSeat(seat))
            {
                return ReachDecisionResult.None;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (!playerSeat.IsClosed || playerSeat.IsReachDeclared || !playerSeat.HasDrawnTile ||
                !playerSeat.DrawnTile.HasValue || playerSeat.Hand.Count != 13)
            {
                return ReachDecisionResult.None;
            }

            ReachCheckResult result = reachChecker.CheckReach(
                playerSeat.Hand.GetTiles(), playerSeat.DrawnTile.Value);
            if (!result.CanReach)
                return ReachDecisionResult.None;

            gameState.BeginReachDecision(seat, result.Candidates, gameState.TurnIndex);
            return gameState.IsReachDecisionPending
                ? new ReachDecisionResult(true, seat, gameState.TurnIndex, playerSeat.DrawnTile, string.Empty)
                : ReachDecisionResult.None;
        }

        public ReachDecisionResult BeginDiscardSelection(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null || !gameState.IsReachDecisionPending)
                return ReachDecisionResult.Rejected("ReachDecisionMissing");
            if (gameState.ReachDecisionSeat != seat)
                return ReachDecisionResult.Rejected("NotReachDecisionSeat");

            int turnIndex = gameState.ReachDecisionTurnIndex;
            gameState.BeginReachDiscardSelection(seat);
            return gameState.IsReachDiscardSelectionPending
                ? new ReachDecisionResult(true, seat, turnIndex, null, string.Empty)
                : ReachDecisionResult.Rejected("ReachCandidatesMissing");
        }

        public ReachDecisionResult CancelDiscardSelection(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null || !gameState.IsReachDiscardSelectionPending)
                return ReachDecisionResult.Rejected("ReachDiscardSelectionMissing");
            if (gameState.ReachDecisionSeat != seat)
                return ReachDecisionResult.Rejected("NotReachDecisionSeat");

            int turnIndex = gameState.ReachDecisionTurnIndex;
            return gameState.CancelReachDiscardSelection()
                ? new ReachDecisionResult(true, seat, turnIndex, null, string.Empty)
                : ReachDecisionResult.Rejected("ReachCandidatesMissing");
        }

        public ReachDecisionResult Decline(MahjongGameState gameState)
        {
            if (gameState == null || !gameState.IsReachDecisionPending)
                return ReachDecisionResult.Rejected("ReachDecisionMissing");

            ReachDecisionResult result = new ReachDecisionResult(
                true, gameState.ReachDecisionSeat, gameState.ReachDecisionTurnIndex, null, string.Empty);
            gameState.ClearReachDecision();
            return result;
        }

        public bool IsValidDiscardCandidate(
            MahjongGameState gameState,
            SeatId seat,
            DiscardSource source,
            int handIndex)
        {
            if (gameState == null || !gameState.IsReachDiscardSelectionPending)
                return true;
            if (seat != gameState.ReachDecisionSeat)
                return false;

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            for (int i = 0; i < gameState.ReachDiscardCandidates.Count; i++)
            {
                ReachDiscardCandidate candidate = gameState.ReachDiscardCandidates[i];
                if (candidate.Source != source || candidate.HandIndex != handIndex)
                    continue;
                if (source == DiscardSource.DrawnTile)
                    return playerSeat.HasDrawnTile && playerSeat.DrawnTile.HasValue &&
                        candidate.Tile == playerSeat.DrawnTile.Value;
                if (handIndex >= 0 && handIndex < playerSeat.Hand.Count &&
                    candidate.Tile == playerSeat.Hand.GetTiles()[handIndex])
                    return true;
            }
            return false;
        }

        public ReachDeclarationResult CompleteDeclarationIfPending(
            MahjongGameState gameState,
            DiscardRecord record)
        {
            if (gameState == null || !gameState.IsReachDiscardSelectionPending ||
                record.ActorSeat != gameState.ReachDecisionSeat)
            {
                return ReachDeclarationResult.None;
            }

            SeatId seat = record.ActorSeat;
            int turnIndex = gameState.TurnIndex;
            bool isDoubleReach =
                !gameState.HasCallOccurred &&
                IsFirstDiscardBySeat(gameState, seat);
            gameState.GetPlayerSeat(seat).DeclareReach(turnIndex, isDoubleReach);
            gameState.ClearReachDecision();
            return new ReachDeclarationResult(true, seat, turnIndex, isDoubleReach);
        }

        public void ExpireIppatsuAfterDiscard(MahjongGameState gameState, DiscardRecord record, bool declaredReachNow)
        {
            if (gameState == null || declaredReachNow)
                return;
            PlayerSeat playerSeat = gameState.GetPlayerSeat(record.ActorSeat);
            if (playerSeat.IsIppatsuEligible)
                playerSeat.ClearIppatsuEligibility();
        }

        private static bool IsFirstDiscardBySeat(MahjongGameState gameState, SeatId seat)
        {
            int count = 0;
            for (int i = 0; i < gameState.Discards.Count; i++)
            {
                if (gameState.Discards[i].ActorSeat != seat)
                    continue;
                if (++count > 1)
                    return false;
            }
            return count == 1;
        }
    }

    public readonly struct ReachDecisionResult
    {
        public static ReachDecisionResult None => new ReachDecisionResult(false, default, 0, null, string.Empty);
        public ReachDecisionResult(bool success, SeatId seat, int turnIndex, Tile? drawnTile, string failureReason)
        { Success = success; Seat = seat; TurnIndex = turnIndex; DrawnTile = drawnTile; FailureReason = failureReason ?? string.Empty; }
        public static ReachDecisionResult Rejected(string reason) => new ReachDecisionResult(false, default, 0, null, reason);
        public bool Success { get; }
        public SeatId Seat { get; }
        public int TurnIndex { get; }
        public Tile? DrawnTile { get; }
        public string FailureReason { get; }
    }

    public readonly struct ReachDeclarationResult
    {
        public static ReachDeclarationResult None => new ReachDeclarationResult(false, default, 0, false);
        public ReachDeclarationResult(bool declared, SeatId seat, int turnIndex, bool isDoubleReach)
        { Declared = declared; Seat = seat; TurnIndex = turnIndex; IsDoubleReach = isDoubleReach; }
        public bool Declared { get; }
        public SeatId Seat { get; }
        public int TurnIndex { get; }
        public bool IsDoubleReach { get; }
    }
}
