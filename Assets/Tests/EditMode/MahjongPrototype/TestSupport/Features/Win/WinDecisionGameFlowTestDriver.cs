using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinDecisionGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private object previousState;
        private bool disposed;

        private WinDecisionGameFlowTestDriver(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
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
            Reflection.Invoke(GameFlow, "StartNewRound");
        }

        public void RetryPrototype()
        {
            Reflection.Invoke(GameFlow, "RetryPrototype");
        }

        public void CheckWinPrototype()
        {
            Reflection.Invoke(GameFlow, "CheckWinPrototype");
        }

        public void RequestDeclareWin()
        {
            previousState = flow.CurrentState;
            Reflection.Invoke(GameFlow, "RequestDeclareWin");
        }

        public void RequestDeclineWin()
        {
            Reflection.Invoke(GameFlow, "RequestDeclineWin");
        }

        public void SetWinDecisionPendingForCurrentTurn()
        {
            Reflection.Invoke(GameFlow, "SetWinDecisionPending", true, CurrentTurn, TurnIndex);
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
            Reflection.SetProperty(flow.CurrentState, "CurrentTurn", CpuSeat);
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
            Reflection.SetProperty(flow.CurrentState, "CurrentTurn", CpuSeat);
        }

        public bool TryDiscardCpuDrawnTile()
        {
            return (bool)Reflection.Invoke(GameFlow, "TryRequestDiscardDrawnTileForSeat", CpuSeat);
        }

        public bool IsWinDecisionPending =>
            (bool)Reflection.GetProperty(flow.CurrentState, "IsWinDecisionPending");

        public bool PreviousStateIsWinDecisionPending =>
            (bool)Reflection.GetProperty(previousState, "IsWinDecisionPending");

        public string WinDecisionSeatName =>
            Reflection.GetProperty(flow.CurrentState, "WinDecisionSeat").ToString();

        public string WinDecisionTypeName =>
            NullablePropertyString(flow.CurrentState, "WinDecisionType");

        public string WinSourceSeatNameOrNull =>
            NullablePropertyString(flow.CurrentState, "WinSourceSeat");

        public int WinDecisionTurnIndex =>
            (int)Reflection.GetProperty(flow.CurrentState, "WinDecisionTurnIndex");

        public string WinningTileCodeOrNull =>
            NullablePropertyString(flow.CurrentState, "WinningTile");

        public bool PendingWinDeclarationEvaluationIsNull =>
            Reflection.GetProperty(flow.CurrentState, "PendingWinDeclarationEvaluation") == null;

        public bool FlowIsWinDecisionPending =>
            (bool)Reflection.GetProperty(GameFlow, "IsWinDecisionPending");

        public bool IsRoundEnded =>
            (bool)Reflection.GetProperty(flow.CurrentState, "IsRoundEnded");

        public bool IsInteractionLocked =>
            (bool)Reflection.GetProperty(flow.CurrentState, "IsInteractionLocked");

        public string CurrentTurnName => CurrentTurn.ToString();
        public int TurnIndex => (int)Reflection.GetProperty(flow.CurrentState, "TurnIndex");
        public string SelfSeatName => SelfSeat.ToString();
        public string CpuSeatName => CpuSeat.ToString();

        public string WindProgressRoundWindName =>
            Reflection.GetProperty(WindProgress, "RoundWind").ToString();

        public int WindProgressHandNumber =>
            (int)Reflection.GetProperty(WindProgress, "HandNumber");

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
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
            object catalog = dataFactory.CreateYakuCatalog(
                dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"),
                dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                dataFactory.CreateYakuDefinition("Tanyao", "One", "One"));
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

            MahjongGameFlowTestHarness flow = MahjongGameFlowTestHarness.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            flow.RegisterOwnedScriptableObject(catalog);
            return new WinDecisionGameFlowTestDriver(flow);
        }

        private object GameFlow => flow.GameFlow;
        private ReflectionTestAccess Reflection => flow.Reflection;
        private MahjongTestDataFactory DataFactory => flow.DataFactory;

        private object CurrentTurn =>
            Reflection.GetProperty(flow.CurrentState, "CurrentTurn");

        private object CurrentPlayerSeat =>
            Reflection.Invoke(flow.CurrentState, "GetPlayerSeat", CurrentTurn);

        private object SelfSeat =>
            Reflection.GetProperty(flow.CurrentState, "SelfSeat");

        private object CpuSeat =>
            Reflection.Invoke(
                flow.CurrentState,
                "GetSeatByPlayerId",
                DataFactory.ParsePlayerId("Player2"));

        private object SelfPlayerSeat =>
            Reflection.Invoke(flow.CurrentState, "GetPlayerSeat", SelfSeat);

        private object CpuPlayerSeat =>
            Reflection.Invoke(flow.CurrentState, "GetPlayerSeat", CpuSeat);

        private object WindProgress =>
            Reflection.GetProperty(flow.CurrentState, "WindProgress");

        private string NullablePropertyString(object target, string propertyName)
        {
            object value = Reflection.GetProperty(target, propertyName);
            return value == null ? null : value.ToString();
        }
    }
}
