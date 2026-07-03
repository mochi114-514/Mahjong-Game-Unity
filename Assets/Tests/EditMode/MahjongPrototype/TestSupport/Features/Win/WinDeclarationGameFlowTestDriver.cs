using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinDeclarationGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private WinDeclarationGameFlowTestDriver(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
        }

        public static WinDeclarationGameFlowTestDriver CreateWithoutYakuCatalog()
        {
            return Create("NoYakuWinDeclarationFlowTest", null);
        }

        public static WinDeclarationGameFlowTestDriver CreateWithRegisteredYaku(
            string yakuKindName,
            string closedHanName,
            string openHanName)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog = dataFactory.CreateYakuCatalog(
                dataFactory.CreateYakuDefinition(yakuKindName, closedHanName, openHanName));
            WinDeclarationGameFlowTestDriver driver = Create(
                "YakuWinDeclarationFlowTest",
                catalog,
                reflection,
                collections,
                types,
                dataFactory);

            driver.flow.RegisterOwnedScriptableObject(catalog);
            return driver;
        }

        public bool IsWinDecisionPending =>
            (bool)flow.Reflection.GetProperty(flow.CurrentState, "IsWinDecisionPending");

        public object PendingWinDeclarationEvaluation =>
            flow.Reflection.GetProperty(flow.CurrentState, "PendingWinDeclarationEvaluation");

        public void DrawStandardClosedTsumoShape()
        {
            flow.StartRound();
            flow.DataFactory.AddHandTiles(
                flow.GetPlayerSeat("East"),
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");

            flow.Reflection.Invoke(flow.GameFlow, "RequestForceDrawSkill", "C");
            flow.Reflection.Invoke(flow.GameFlow, "RequestDraw");
        }

        public bool CanDeclareWin(object evaluation)
        {
            return (bool)flow.Reflection.GetProperty(evaluation, "CanDeclareWin");
        }

        public int TotalHan(object evaluation)
        {
            object handEvaluation = flow.Reflection.GetProperty(evaluation, "HandEvaluationResult");
            return (int)flow.Reflection.GetProperty(handEvaluation, "TotalHan");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
        }

        private static WinDeclarationGameFlowTestDriver Create(string rootName, object catalog)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return Create(rootName, catalog, reflection, collections, types, dataFactory);
        }

        private static WinDeclarationGameFlowTestDriver Create(
            string rootName,
            object catalog,
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory)
        {
            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = rootName,
                AddEventNotifier = true,
                LogWarnings = false,
                ParticipantCount = 1,
                InitialHandTileCount = 0,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = false,
                RandomizeSelfSeat = false,
                FixedSelfSeatName = "East",
                YakuDefinitionCatalog = catalog
            };

            MahjongGameFlowTestHarness flow = MahjongGameFlowTestHarness.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            return new WinDeclarationGameFlowTestDriver(flow);
        }
    }
}

