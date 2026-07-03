using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.WindProgress
{
    internal sealed class WindProgressGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private WindProgressGameFlowTestDriver(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
        }

        public static WindProgressGameFlowTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = "WindProgressGameFlowTest",
                ParticipantCount = 1,
                InitialHandTileCount = 0,
                AutoStart = false,
                EnableAutoDraw = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East",
                LogWarnings = false
            };

            return new WindProgressGameFlowTestDriver(
                MahjongGameFlowTestHarness.Create(
                    options,
                    reflection,
                    collections,
                    types,
                    dataFactory));
        }

        public string CurrentRoundWindName =>
            flow.Reflection.GetProperty(CurrentWindProgress, "RoundWind").ToString();

        public int CurrentHandNumber =>
            (int)flow.Reflection.GetProperty(CurrentWindProgress, "HandNumber");

        public bool IsRoundEnded =>
            (bool)flow.Reflection.GetProperty(flow.CurrentState, "IsRoundEnded");

        public void StartNewRound()
        {
            flow.StartRound();
        }

        public void StartRound(string roundWindName, int handNumber)
        {
            object windProgress = flow.DataFactory.CreateWindProgress(roundWindName, handNumber);
            flow.Reflection.InvokeWithSignature(
                flow.GameFlow,
                "StartRound",
                new[] { flow.Types.WindProgress, typeof(bool) },
                windProgress,
                false);
        }

        public void EndRound(string reason)
        {
            flow.Reflection.InvokeWithSignature(
                flow.GameFlow,
                "EndRound",
                new[] { typeof(string) },
                reason);
        }

        public void BeginWinDecision(
            string winnerSeatName,
            string winTypeName,
            string sourceSeatName)
        {
            object sourceSeat = sourceSeatName == null
                ? null
                : flow.DataFactory.ParseSeat(sourceSeatName);

            flow.Reflection.InvokeWithSignature(
                flow.CurrentState,
                "BeginWinDecisionDetailed",
                new[]
                {
                    flow.Types.SeatId,
                    flow.Types.WinType,
                    typeof(Nullable<>).MakeGenericType(flow.Types.Tile),
                    typeof(Nullable<>).MakeGenericType(flow.Types.SeatId),
                    typeof(int)
                },
                flow.DataFactory.ParseSeat(winnerSeatName),
                flow.DataFactory.ParseWinType(winTypeName),
                null,
                sourceSeat,
                TurnIndex);
        }

        public void DeclareWin()
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestDeclareWin");
        }

        public void DeclineWin()
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestDeclineWin");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
        }

        private object CurrentWindProgress =>
            flow.Reflection.GetProperty(flow.CurrentState, "WindProgress");

        private int TurnIndex =>
            (int)flow.Reflection.GetProperty(flow.CurrentState, "TurnIndex");
    }
}
