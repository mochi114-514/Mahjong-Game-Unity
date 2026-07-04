using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinDecisionGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private object previousState;
        private bool disposed;

        private WinDecisionGameFlowTestDriver(MahjongGameFlowTestSession session)
        {
            this.session = session;
        }

        public static WinDecisionGameFlowTestDriver Create(
            int participantCount = 1,
            int initialHandTileCount = 1,
            bool enableAutoDraw = false,
            string fixedSelfSeatName = "East")
        {
            return Create(
                "WinDecisionGameFlowTest",
                participantCount,
                initialHandTileCount,
                enableAutoDraw,
                fixedSelfSeatName);
        }

        public static WinDecisionGameFlowTestDriver CreateTwoPlayerWithoutInitialHand()
        {
            return Create(
                "WinDecisionTwoPlayerGameFlowTest",
                participantCount: 2,
                initialHandTileCount: 0,
                enableAutoDraw: false,
                fixedSelfSeatName: "East");
        }

        public void StartNewRound()
        {
            Commands.StartNewRound();
        }

        public void RetryPrototype()
        {
            Commands.RetryPrototype();
        }

        public void CheckWinPrototype()
        {
            Commands.CheckWinPrototype();
        }

        public void RequestDeclareWin()
        {
            previousState = session.CurrentState;
            Commands.RequestDeclareWin();
        }

        public void RequestDeclineWin()
        {
            Commands.RequestDeclineWin();
        }

        public void SetWinDecisionPendingForCurrentTurn()
        {
            Commands.SetWinDecisionPending(CurrentTurnName, TurnIndex);
        }

        public void SetSelfWinningTsumoHand()
        {
            DataFactory.AddHandTiles(
                CurrentPlayerSeat,
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");
            Reflection.Invoke(CurrentPlayerSeat, "SetDrawnTile", DataFactory.CreateTile("C"));
        }

        public void SetupSelfCanRonFromCpuDrawnTile()
        {
            DataFactory.AddHandTiles(
                SelfPlayerSeat,
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");
            Reflection.Invoke(SelfPlayerSeat, "DeclareReach", 1);
            Reflection.Invoke(CpuPlayerSeat, "SetDrawnTile", DataFactory.CreateTile("C"));
            Reflection.SetProperty(session.CurrentState, "CurrentTurn", CpuSeat);
        }

        public void SetupSelfCannotRonFromCpuDrawnTile()
        {
            DataFactory.AddHandTiles(
                SelfPlayerSeat,
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "P");
            Reflection.Invoke(CpuPlayerSeat, "SetDrawnTile", DataFactory.CreateTile("C"));
            Reflection.SetProperty(session.CurrentState, "CurrentTurn", CpuSeat);
        }

        public bool TryDiscardCpuDrawnTile()
        {
            return Commands.TryRequestDiscardDrawnTileForSeat(CpuSeatName);
        }

        public bool IsWinDecisionPending => Query.IsWinDecisionPending;

        public bool PreviousStateIsWinDecisionPending =>
            (bool)Reflection.GetProperty(previousState, "IsWinDecisionPending");

        public string WinDecisionSeatName => Query.WinDecisionSeatName;

        public string WinDecisionTypeName => Query.WinDecisionTypeNameOrNull;

        public string WinSourceSeatNameOrNull => Query.WinSourceSeatNameOrNull;

        public int WinDecisionTurnIndex => Query.WinDecisionTurnIndex;

        public string WinningTileCodeOrNull => Query.WinningTileCodeOrNull;

        public bool PendingWinDeclarationEvaluationIsNull =>
            Query.PendingWinDeclarationEvaluationIsNull;

        public bool FlowIsWinDecisionPending =>
            (bool)Reflection.GetProperty(GameFlow, "IsWinDecisionPending");

        public bool IsRoundEnded => Query.IsRoundEnded;

        public bool IsInteractionLocked => Query.IsInteractionLocked;

        public string CurrentTurnName => Query.CurrentTurnName;
        public int TurnIndex => Query.TurnIndex;
        public string SelfSeatName => Query.SelfSeatName;
        public string CpuSeatName => Query.SeatByPlayerIdName("Player2");

        public string WindProgressRoundWindName => Query.WindProgressRoundWindName;

        public int WindProgressHandNumber => Query.WindProgressHandNumber;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private static WinDecisionGameFlowTestDriver Create(
            string rootName,
            int participantCount,
            int initialHandTileCount,
            bool enableAutoDraw,
            string fixedSelfSeatName)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog =
                MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);
            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = rootName,
                AddEventNotifier = false,
                LogWarnings = false,
                ParticipantCount = participantCount,
                InitialHandTileCount = initialHandTileCount,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = enableAutoDraw,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = fixedSelfSeatName,
                YakuDefinitionCatalog = catalog
            };

            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            session.RegisterOwnedScriptableObject(catalog);
            return new WinDecisionGameFlowTestDriver(session);
        }

        private object GameFlow => session.GameFlow;
        private ReflectionTestAccess Reflection => session.Reflection;
        private MahjongTestDataFactory DataFactory => session.DataFactory;
        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;

        private object CurrentPlayerSeat =>
            Query.GetPlayerSeat(CurrentTurnName);

        private object CpuSeat =>
            DataFactory.ParseSeat(CpuSeatName);

        private object SelfPlayerSeat =>
            Query.GetPlayerSeat(SelfSeatName);

        private object CpuPlayerSeat =>
            Query.GetPlayerSeat(CpuSeatName);
    }
}
