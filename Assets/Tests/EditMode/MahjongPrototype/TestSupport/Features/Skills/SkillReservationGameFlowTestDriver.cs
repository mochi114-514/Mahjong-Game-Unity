using System;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Skills
{
    internal sealed class SkillReservationGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private SkillReservationGameFlowTestDriver(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
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

        public int ActiveSkillEffectCount =>
            flow.Collections.Count(flow.Reflection.GetProperty(flow.CurrentState, "ActiveSkillEffects"));

        public void StartRound()
        {
            flow.StartRound();
        }

        public void SetCurrentTurn(string seatName)
        {
            flow.SetCurrentTurn(seatName);
        }

        public void RequestForceDrawSkillForSeat(string ownerSeat, string targetTile)
        {
            flow.Reflection.Invoke(
                flow.GameFlow,
                "RequestForceDrawSkillForSeat",
                flow.DataFactory.ParseSeat(ownerSeat),
                targetTile);
        }

        public void RequestForceDrawSkill(string targetTile)
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestForceDrawSkill", targetTile);
        }

        public void RequestDraw()
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestDraw");
        }

        public void StartTurn(string seatName)
        {
            flow.Reflection.Invoke(
                flow.GameFlow,
                "StartTurn",
                flow.DataFactory.ParseSeat(seatName),
                flow.Reflection.GetProperty(flow.CurrentState, "TurnIndex"));
        }

        public void ClearDrawnTile(string seatName)
        {
            flow.Reflection.Invoke(flow.GetPlayerSeat(seatName), "ClearDrawnTile");
        }

        public object ActiveSkillEffectAt(int index)
        {
            return flow.Collections.Item(
                flow.Reflection.GetProperty(flow.CurrentState, "ActiveSkillEffects"),
                index);
        }

        public string EffectOwnerSeat(object effect)
        {
            return flow.Reflection.GetProperty(effect, "OwnerSeat").ToString();
        }

        public string EffectTargetTile(object effect)
        {
            return flow.Reflection.GetProperty(effect, "TargetTile").ToString();
        }

        public string DrawnTile(string seatName)
        {
            return flow.Reflection.GetProperty(flow.GetPlayerSeat(seatName), "DrawnTile").ToString();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
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

            return new SkillReservationGameFlowTestDriver(MahjongGameFlowTestHarness.Create(options));
        }
    }
}

