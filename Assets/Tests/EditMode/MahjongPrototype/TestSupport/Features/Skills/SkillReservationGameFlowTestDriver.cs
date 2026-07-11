using System;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Skills
{
    internal sealed class SkillReservationGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private SkillReservationGameFlowTestDriver(MahjongGameFlowTestSession session)
        {
            this.session = session;
        }

        public static SkillReservationGameFlowTestDriver CreateReservationResolutionScenario()
        {
            return Create(
                "ReservationResolutionRegistersSkillTest",
                enableAutoDraw: false,
                participantCount: 2);
        }

        public static SkillReservationGameFlowTestDriver CreateAutoDrawReservationScenario()
        {
            return Create(
                "ReservationAutoDrawAppliesSkillTest",
                enableAutoDraw: true,
                participantCount: 2);
        }

        public static SkillReservationGameFlowTestDriver CreateCurrentTurnSkillScenario()
        {
            return Create(
                "CurrentTurnSkillKeepsDrawnTileTest",
                enableAutoDraw: false,
                participantCount: 1);
        }

        public int ActiveSkillEffectCount => Query.ActiveSkillEffectCount;

        public void StartRound()
        {
            Commands.StartNewRound();
        }

        public void SetCurrentTurn(string seatName)
        {
            session.DataFactory.SetCurrentTurn(session.CurrentState, seatName);
        }

        public void RequestForceDrawSkillForSeat(string ownerSeat, string targetTile)
        {
            Commands.RequestForceDrawSkillForSeat(ownerSeat, targetTile);
        }

        public void RequestForceDrawSkill(string targetTile)
        {
            Commands.RequestForceDrawSkill(targetTile);
        }

        public void RequestDraw()
        {
            Commands.RequestDraw();
        }

        public void StartTurn(string seatName)
        {
            Commands.StartTurn(seatName, Query.TurnIndex);
        }

        public void ClearDrawnTile(string seatName)
        {
            session.DataFactory.ClearDrawnTile(session.CurrentState, seatName);
        }

        public object ActiveSkillEffectAt(int index)
        {
            return Query.ActiveSkillEffectAt(index);
        }

        public string EffectOwnerSeat(object effect)
        {
            return session.Reflection.GetProperty(effect, "OwnerSeat").ToString();
        }

        public string EffectTargetTile(object effect)
        {
            return session.Reflection.GetProperty(effect, "TargetTile").ToString();
        }

        public string DrawnTile(string seatName)
        {
            return Query.DrawnTileCode(seatName);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private static SkillReservationGameFlowTestDriver Create(
            string rootName,
            bool enableAutoDraw,
            int participantCount)
        {
            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = rootName,
                LogWarnings = false,
                InitialHandTileCount = 1,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = enableAutoDraw,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East",
                ParticipantCount = participantCount
            };

            return new SkillReservationGameFlowTestDriver(MahjongGameFlowTestSession.Create(options));
        }

        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;
    }
}
