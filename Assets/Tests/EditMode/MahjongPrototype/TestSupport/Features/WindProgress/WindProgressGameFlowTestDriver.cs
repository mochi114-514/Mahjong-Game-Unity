using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.WindProgress
{
    internal sealed class WindProgressGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private WindProgressGameFlowTestDriver(MahjongGameFlowTestSession session)
        {
            this.session = session;
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
                MahjongGameFlowTestSession.Create(
                    options,
                    reflection,
                    collections,
                    types,
                    dataFactory));
        }

        public string CurrentRoundWindName => Query.WindProgressRoundWindName;

        public int CurrentHandNumber => Query.WindProgressHandNumber;

        public bool IsRoundEnded => Query.IsRoundEnded;

        public void StartNewRound()
        {
            Commands.StartNewRound();
        }

        public void StartRound(string roundWindName, int handNumber)
        {
            object windProgress = session.DataFactory.CreateWindProgress(roundWindName, handNumber);
            session.Reflection.InvokeWithSignature(
                session.GameFlow,
                "StartRound",
                new[] { session.Types.WindProgress, typeof(bool) },
                windProgress,
                false);
        }

        public void EndRound(string reason)
        {
            session.Reflection.InvokeWithSignature(
                session.GameFlow,
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
                : session.DataFactory.ParseSeat(sourceSeatName);

            session.Reflection.InvokeWithSignature(
                session.CurrentState,
                "BeginWinDecisionDetailed",
                new[]
                {
                    session.Types.SeatId,
                    session.Types.WinType,
                    typeof(Nullable<>).MakeGenericType(session.Types.Tile),
                    typeof(Nullable<>).MakeGenericType(session.Types.SeatId),
                    typeof(int)
                },
                session.DataFactory.ParseSeat(winnerSeatName),
                session.DataFactory.ParseWinType(winTypeName),
                null,
                sourceSeat,
                TurnIndex);
        }

        public void DeclareWin()
        {
            Commands.RequestDeclareWin();
        }

        public void DeclineWin()
        {
            Commands.RequestDeclineWin();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private int TurnIndex => Query.TurnIndex;
        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;
    }
}
