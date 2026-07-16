using System;
using MahjongPrototype.Domain;
using MahjongPrototype.Skills;

namespace MahjongPrototype.Services
{
    public sealed class SkillFlowService
    {
        private readonly SkillSystem skillSystem;
        private readonly SkillReservationService reservationService;

        public SkillFlowService(SkillSystem skillSystem, SkillReservationService reservationService)
        {
            this.skillSystem = skillSystem ?? throw new ArgumentNullException(nameof(skillSystem));
            this.reservationService = reservationService ??
                throw new ArgumentNullException(nameof(reservationService));
        }

        public SkillFlowResult RequestForceDraw(
            MahjongGameState gameState,
            SeatId ownerSeat,
            string targetTileCode)
        {
            if (gameState == null)
                return SkillFlowResult.Rejected("GameState is not available.");
            if (gameState.IsRoundEnded)
                return SkillFlowResult.Rejected("Round already ended. Press Retry.");
            if (gameState.IsWinDecisionPending)
                return SkillFlowResult.Rejected("Declare or decline win before activating another skill.");
            if (gameState.IsReactionWindowPending)
                return SkillFlowResult.Rejected("Resolve reactions before activating another skill.");
            if (gameState.TurnPhase == TurnPhase.WaitingForDiscardAfterCall)
            {
                return SkillFlowResult.Rejected(
                    "Skills cannot be activated before the mandatory post-call discard.");
            }
            if (gameState.IsReachDiscardSelectionPending)
                return SkillFlowResult.Rejected("Resolve reach discard selection before activating another skill.");
            if (!Tile.TryParse(targetTileCode, out Tile targetTile))
            {
                return SkillFlowResult.Rejected(
                    "Invalid target tile. Use 1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C.");
            }

            if (ownerSeat == gameState.CurrentTurn)
                return ActivateForceDraw(gameState, ownerSeat, targetTile, false);

            if (!IsActiveSeat(gameState, ownerSeat))
                return SkillFlowResult.Rejected("Owner seat is not active.", ownerSeat, targetTile);
            if (gameState.HasActiveSkillEffect(ownerSeat, SkillEffectKind.ForceDrawTile))
            {
                return SkillFlowResult.Rejected(
                    "Force draw skill is already active.", ownerSeat, targetTile);
            }

            PendingSkillReservation reservation = new PendingSkillReservation(
                ownerSeat,
                SkillEffectKind.ForceDrawTile,
                targetTile,
                gameState.CurrentTurn,
                gameState.TurnIndex);
            if (!reservationService.Reserve(reservation, out string reason))
                return SkillFlowResult.Rejected(reason, ownerSeat, targetTile);

            return SkillFlowResult.Reserved(reservation);
        }

        /// <summary>
        /// Reports whether ForceDraw can be requested for the seat in the
        /// current match phase. Tile text validation remains an execution-time
        /// concern so editing an incomplete input never disables the field.
        /// </summary>
        public bool CanRequestForceDraw(MahjongGameState gameState, SeatId ownerSeat)
        {
            if (gameState == null ||
                gameState.IsRoundEnded ||
                gameState.IsWinDecisionPending ||
                gameState.IsReactionWindowPending ||
                gameState.TurnPhase == TurnPhase.WaitingForDiscardAfterCall ||
                gameState.IsReachDiscardSelectionPending ||
                !IsActiveSeat(gameState, ownerSeat) ||
                reservationService.HasReservation(ownerSeat) ||
                gameState.HasActiveSkillEffect(ownerSeat, SkillEffectKind.ForceDrawTile))
            {
                return false;
            }

            return true;
        }

        public SkillFlowResult ResolveReservedBeforeDraw(MahjongGameState gameState, SeatId seat)
        {
            if (gameState == null || gameState.IsRoundEnded || gameState.IsWinDecisionPending ||
                gameState.IsReactionWindowPending ||
                gameState.TurnPhase == TurnPhase.WaitingForDiscardAfterCall ||
                !reservationService.TryConsumeForTurn(seat, out PendingSkillReservation reservation))
            {
                return SkillFlowResult.None;
            }

            if (reservation.SkillEffectKind != SkillEffectKind.ForceDrawTile)
            {
                return SkillFlowResult.UnsupportedReservation(
                    reservation,
                    "Unsupported skill reservation.");
            }

            SkillFlowResult activation = ActivateForceDraw(
                gameState,
                reservation.OwnerSeat,
                reservation.TargetTile,
                true);
            return activation.WithConsumedReservation(reservation);
        }

        public SkillDrawResolutionResult ResolveDrawResult(DrawResult drawResult)
        {
            if (!drawResult.SkillWasPresent || drawResult.ResolvedSkillEffect == null)
                return SkillDrawResolutionResult.None;
            return new SkillDrawResolutionResult(true, drawResult.ResolvedSkillEffect);
        }

        public void ClearReservations()
        {
            reservationService.Clear();
        }

        private SkillFlowResult ActivateForceDraw(
            MahjongGameState gameState,
            SeatId seat,
            Tile targetTile,
            bool beforeDraw)
        {
            SkillActivationResult result = skillSystem.ActivateForceDrawTile(gameState, seat, targetTile);
            return result.Success
                ? SkillFlowResult.Activated(seat, targetTile, result.Effect, beforeDraw)
                : SkillFlowResult.Rejected(result.Reason, seat, targetTile, beforeDraw);
        }

        private static bool IsActiveSeat(MahjongGameState gameState, SeatId seat)
        {
            for (int i = 0; i < gameState.ActiveTurnSeats.Count; i++)
            {
                if (gameState.ActiveTurnSeats[i] == seat)
                    return true;
            }
            return false;
        }
    }

    public readonly struct SkillFlowResult
    {
        private SkillFlowResult(
            SkillFlowResultType type,
            SeatId seat,
            Tile targetTile,
            ActiveSkillEffect effect,
            PendingSkillReservation reservation,
            bool hasReservation,
            bool beforeDraw,
            string reason)
        {
            Type = type; Seat = seat; TargetTile = targetTile; Effect = effect;
            Reservation = reservation; HasReservation = hasReservation;
            BeforeDraw = beforeDraw; Reason = reason ?? string.Empty;
        }
        public static SkillFlowResult None => new SkillFlowResult(SkillFlowResultType.None, default, default, null, default, false, false, string.Empty);
        public static SkillFlowResult Reserved(PendingSkillReservation reservation) => new SkillFlowResult(SkillFlowResultType.Reserved, reservation.OwnerSeat, reservation.TargetTile, null, reservation, true, false, string.Empty);
        public static SkillFlowResult Activated(SeatId seat, Tile tile, ActiveSkillEffect effect, bool beforeDraw) => new SkillFlowResult(SkillFlowResultType.Activated, seat, tile, effect, default, false, beforeDraw, string.Empty);
        public static SkillFlowResult Rejected(string reason, SeatId seat = default, Tile tile = default, bool beforeDraw = false) => new SkillFlowResult(SkillFlowResultType.Rejected, seat, tile, null, default, false, beforeDraw, reason);
        public static SkillFlowResult UnsupportedReservation(PendingSkillReservation reservation, string reason) => new SkillFlowResult(SkillFlowResultType.UnsupportedReservation, reservation.OwnerSeat, reservation.TargetTile, null, reservation, true, true, reason);
        public SkillFlowResult WithConsumedReservation(PendingSkillReservation reservation) => new SkillFlowResult(Type, Seat, TargetTile, Effect, reservation, true, BeforeDraw, Reason);
        public SkillFlowResultType Type { get; }
        public SeatId Seat { get; }
        public Tile TargetTile { get; }
        public ActiveSkillEffect Effect { get; }
        public PendingSkillReservation Reservation { get; }
        public bool HasReservation { get; }
        public bool BeforeDraw { get; }
        public string Reason { get; }
        public bool Success => Type == SkillFlowResultType.Activated || Type == SkillFlowResultType.Reserved;
    }

    public enum SkillFlowResultType { None, Reserved, Activated, Rejected, UnsupportedReservation }

    public readonly struct SkillDrawResolutionResult
    {
        public static SkillDrawResolutionResult None => new SkillDrawResolutionResult(false, null);
        public SkillDrawResolutionResult(bool resolved, ActiveSkillEffect effect) { Resolved = resolved; Effect = effect; }
        public bool Resolved { get; }
        public ActiveSkillEffect Effect { get; }
    }
}
