using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class HandAutoSortService
    {
        private bool deferredUntilReachDecisionResolved;

        public HandAutoSortResult Apply(
            MahjongGameState gameState,
            bool enabled,
            SeatId seat,
            string reason)
        {
            if (!enabled || gameState == null || !gameState.IsSelfSeat(seat))
                return HandAutoSortResult.None;
            if (ShouldDefer(gameState, seat))
            {
                deferredUntilReachDecisionResolved = true;
                return HandAutoSortResult.Deferred(seat, gameState.TurnIndex, reason);
            }

            gameState.GetPlayerSeat(seat).Hand.SortByTypeIndex();
            return HandAutoSortResult.Applied(seat, gameState.TurnIndex, reason);
        }

        public HandAutoSortResult ApplyDeferredIfReady(
            MahjongGameState gameState,
            bool enabled,
            string reason)
        {
            if (!deferredUntilReachDecisionResolved || gameState == null ||
                gameState.IsReachDecisionPending || gameState.IsReachDiscardSelectionPending)
            {
                return HandAutoSortResult.None;
            }

            deferredUntilReachDecisionResolved = false;
            return Apply(gameState, enabled, gameState.SelfSeat, reason);
        }

        public void ClearDeferred()
        {
            deferredUntilReachDecisionResolved = false;
        }

        private static bool ShouldDefer(MahjongGameState gameState, SeatId seat)
        {
            return gameState.IsSelfSeat(seat) &&
                (gameState.IsReachDecisionPending || gameState.IsReachDiscardSelectionPending);
        }
    }

    public readonly struct HandAutoSortResult
    {
        private HandAutoSortResult(HandAutoSortResultType type, SeatId seat, int turnIndex, string reason)
        { Type = type; Seat = seat; TurnIndex = turnIndex; Reason = reason ?? string.Empty; }
        public static HandAutoSortResult None => new HandAutoSortResult(HandAutoSortResultType.None, default, 0, string.Empty);
        public static HandAutoSortResult Applied(SeatId seat, int turnIndex, string reason) => new HandAutoSortResult(HandAutoSortResultType.Applied, seat, turnIndex, reason);
        public static HandAutoSortResult Deferred(SeatId seat, int turnIndex, string reason) => new HandAutoSortResult(HandAutoSortResultType.Deferred, seat, turnIndex, reason);
        public HandAutoSortResultType Type { get; }
        public SeatId Seat { get; }
        public int TurnIndex { get; }
        public string Reason { get; }
        public bool WasApplied => Type == HandAutoSortResultType.Applied;
    }

    public enum HandAutoSortResultType { None, Deferred, Applied }
}
