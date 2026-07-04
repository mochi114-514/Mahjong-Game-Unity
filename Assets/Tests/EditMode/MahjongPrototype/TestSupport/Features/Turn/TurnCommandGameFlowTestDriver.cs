using System;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class TurnCommandGameFlowTestDriver : IDisposable
    {
        private readonly TurnGameFlowTestSupport support;
        private bool disposed;

        private TurnCommandGameFlowTestDriver(TurnGameFlowTestSupport support)
        {
            this.support = support;
        }

        public static TurnCommandGameFlowTestDriver Create()
        {
            return Create(participantCount: 2, initialHandTileCount: 0);
        }

        public static TurnCommandGameFlowTestDriver CreateWithInitialHand()
        {
            return Create(participantCount: 1, initialHandTileCount: 1);
        }

        private static TurnCommandGameFlowTestDriver Create(
            int participantCount,
            int initialHandTileCount)
        {
            return new TurnCommandGameFlowTestDriver(
                TurnGameFlowTestSupport.Create(
                    "TurnCommandGameFlowTest",
                    participantCount: participantCount,
                    initialHandTileCount: initialHandTileCount,
                    enableAutoDraw: false));
        }

        public void StartNewRound() => support.StartNewRound();
        public void RequestDraw() => support.RequestDraw();
        public void RequestDiscard(int handIndex) => support.RequestDiscard(handIndex);
        public void RequestDiscardDrawnTile() => support.RequestDiscardDrawnTile();
        public void RequestForceDrawSkill(string tileCode) => support.RequestForceDrawSkill(tileCode);
        public void RequestSetAutoSortEnabled(bool enabled) =>
            support.RequestSetAutoSortEnabled(enabled);
        public void SetAutoSortEnabled(bool enabled) => support.SetAutoSortEnabled(enabled);
        public void DealInitialHands() => support.DealInitialHands();

        public void AddUnsortedHandsToSelfAndOpponent()
        {
            support.AddHandTiles(support.SelfSeatName, "9m", "1m");
            support.AddHandTilesForPlayerId("Player2", "9p", "1p");
        }

        public void SetCurrentTurnToOpponent()
        {
            support.SetCurrentTurnToPlayerId("Player2");
        }

        public void AddOpponentHandTiles(params string[] tileCodes)
        {
            support.AddHandTilesForPlayerId("Player2", tileCodes);
        }

        public void SetOpponentDrawnTile(string tileCode)
        {
            support.SetDrawnTileForPlayerId("Player2", tileCode);
        }

        public void ApplyAutoSortForOpponentThenSelf()
        {
            support.ApplyAutoSortIfEnabledForPlayerId("Player2", "Test", false);
            support.ApplyAutoSortIfEnabled(support.SelfSeatName, "Test", false);
        }

        public bool TryDrawForOpponentSeat()
        {
            return support.TryRequestDrawForPlayerId("Player2");
        }

        public bool TryDiscardOpponentDrawnTile()
        {
            return support.TryRequestDiscardDrawnTileForPlayerId("Player2");
        }

        public bool CurrentPlayerHasDrawnTile => support.CurrentPlayerHasDrawnTile;
        public string TurnPhaseName => support.TurnPhaseName;
        public int TurnIndex => support.TurnIndex;
        public int WallCount => support.WallCount;
        public bool OpponentHasDrawnTile => support.HasDrawnTileForPlayerId("Player2");
        public string OpponentDrawnTileCodeOrNull =>
            support.DrawnTileCodeOrNullForPlayerId("Player2");
        public int OpponentHandCount => support.HandCountForPlayerId("Player2");
        public int DiscardCount => support.DiscardCount;
        public int ActiveSkillEffectCount => support.ActiveSkillEffectCount;
        public string SelfSeatName => support.SelfSeatName;
        public string OpponentSeatName => support.SeatByPlayerId("Player2");
        public string CurrentTurnName => support.CurrentTurnName;
        public string SelfHandDisplay => support.HandDisplayString(support.SelfSeatName);
        public string OpponentHandDisplay => support.HandDisplayStringForPlayerId("Player2");
        public string LastDiscardActorSeatName => support.LastDiscardActorSeatName;
        public string LastDiscardTileCode => support.LastDiscardTileCode;
        public string LastDiscardSourceName => support.LastDiscardSourceName;

        public string ActiveSkillEffectOwnerSeatNameAt(int index) =>
            support.ActiveSkillEffectOwnerSeatNameAt(index);

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            support.Dispose();
        }
    }
}
