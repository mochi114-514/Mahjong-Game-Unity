using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinDeclarationGameFlowTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private WinDeclarationGameFlowTestDriver(MahjongGameFlowTestSession session)
        {
            this.session = session;
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

            driver.session.RegisterOwnedScriptableObject(catalog);
            return driver;
        }

        public bool IsWinDecisionPending => Query.IsWinDecisionPending;

        public object PendingWinDeclarationEvaluation => Query.PendingWinDeclarationEvaluation;

        public void DrawStandardClosedTsumoShape()
        {
            Commands.StartNewRound();
            session.DataFactory.AddHandTiles(
                Query.GetPlayerSeat("East"),
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");

            Commands.RequestForceDrawSkill("C");
            Commands.RequestDraw();
        }

        public void DrawBasicPinfuTsumoShape()
        {
            Commands.StartNewRound();
            AddBasicPinfuTenpaiHand();

            Commands.RequestForceDrawSkill("4m");
            Commands.RequestDraw();
        }

        public object EvaluateBasicPinfuTenpai()
        {
            Commands.StartNewRound();
            AddBasicPinfuTenpaiHand();
            return session.Reflection.Invoke(session.GameFlow, "EvaluateSelfNoYakuTenpai");
        }

        public bool CanDeclareWin(object evaluation)
        {
            return (bool)session.Reflection.GetProperty(evaluation, "CanDeclareWin");
        }

        public int TotalHan(object evaluation)
        {
            object handEvaluation = session.Reflection.GetProperty(evaluation, "HandEvaluationResult");
            return (int)session.Reflection.GetProperty(handEvaluation, "TotalHan");
        }

        public int CandidateResultCount(object evaluation)
        {
            return session.Collections.Count(CandidateResults(evaluation));
        }

        public object CandidateResultAt(object evaluation, int index)
        {
            return session.Collections.Item(CandidateResults(evaluation), index);
        }

        public bool CandidateContainsYaku(object candidateResult, string yakuKindName)
        {
            object yakus = session.Reflection.GetProperty(candidateResult, "Yakus");
            int count = session.Collections.Count(yakus);

            for (int i = 0; i < count; i++)
            {
                object yaku = session.Collections.Item(yakus, i);
                if (session.Reflection.GetProperty(yaku, "Kind").ToString() == yakuKindName)
                    return true;
            }

            return false;
        }

        public int CandidateTotalHan(object candidateResult)
        {
            return (int)session.Reflection.GetProperty(candidateResult, "TotalHan");
        }

        public bool NoYakuTenpaiIsEvaluated(object result)
        {
            return (bool)session.Reflection.GetProperty(result, "IsEvaluated");
        }

        public bool NoYakuTenpaiIsTenpai(object result)
        {
            return (bool)session.Reflection.GetProperty(result, "IsTenpai");
        }

        public bool NoYakuTenpaiHasAnyYakuWait(object result)
        {
            return (bool)session.Reflection.GetProperty(result, "HasAnyYakuWait");
        }

        public bool NoYakuTenpaiShouldShowZeroHanTenpai(object result)
        {
            return (bool)session.Reflection.GetProperty(result, "ShouldShowZeroHanTenpai");
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
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

            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            return new WinDeclarationGameFlowTestDriver(session);
        }

        private void AddBasicPinfuTenpaiHand()
        {
            session.DataFactory.AddHandTiles(
                Query.GetPlayerSeat("East"),
                "2m", "3m",
                "1p", "2p", "3p",
                "4p", "5p", "6p",
                "7s", "8s", "9s",
                "5s", "5s");
        }

        private object CandidateResults(object evaluation)
        {
            object handEvaluation = session.Reflection.GetProperty(evaluation, "HandEvaluationResult");
            return session.Reflection.GetProperty(handEvaluation, "CandidateResults");
        }

        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;
    }
}
