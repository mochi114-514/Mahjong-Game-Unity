using System;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Retry
{
    internal sealed class GameFlowRetryTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private GameFlowRetryTestDriver(MahjongGameFlowTestSession session)
        {
            this.session = session;
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

            return new GameFlowRetryTestDriver(MahjongGameFlowTestSession.Create(options));
        }

        public int DiscardCount => Query.DiscardCount;

        public void StartRound()
        {
            Commands.StartNewRound();
        }

        public void RequestDraw()
        {
            Commands.RequestDraw();
        }

        public void RequestDiscard(int handIndex)
        {
            Commands.RequestDiscard(handIndex);
        }

        public void Retry()
        {
            Commands.RetryPrototype();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;
    }
}
