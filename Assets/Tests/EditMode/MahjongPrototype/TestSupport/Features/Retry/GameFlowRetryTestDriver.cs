using System;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Retry
{
    internal sealed class GameFlowRetryTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private GameFlowRetryTestDriver(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
        }

        public static GameFlowRetryTestDriver CreateDiscardResetScenario()
        {
            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = "RetryClearsDiscardsTest",
                LogWarnings = false,
                InitialHandTileCount = 1,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = false,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East"
            };

            return new GameFlowRetryTestDriver(MahjongGameFlowTestHarness.Create(options));
        }

        public int DiscardCount =>
            flow.Collections.Count(flow.Reflection.GetProperty(flow.CurrentState, "Discards"));

        public void StartRound()
        {
            flow.StartRound();
        }

        public void RequestDraw()
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestDraw");
        }

        public void RequestDiscard(int handIndex)
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestDiscard", handIndex);
        }

        public void Retry()
        {
            flow.Reflection.Invoke(flow.GameFlow, "RetryPrototype");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
        }
    }
}

