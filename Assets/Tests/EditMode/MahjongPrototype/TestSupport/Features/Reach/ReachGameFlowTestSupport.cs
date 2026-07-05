using System;
using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class ReachGameFlowTestSupport : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private ReachGameFlowTestSupport(MahjongGameFlowTestSession session)
        {
            this.session = session;
        }

        public object GameFlow => session.GameFlow;
        public object CurrentState => session.CurrentState;
        public ReflectionTestAccess Reflection => session.Reflection;
        public CollectionTestAccess Collections => session.Collections;
        public MahjongTestTypes Types => session.Types;
        public MahjongTestDataFactory DataFactory => session.DataFactory;

        public static ReachGameFlowTestSupport Create(
            string rootName,
            int participantCount = 1,
            bool enableAutoDraw = false,
            float autoDiscardDelaySeconds = 0f)
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
                AddEventNotifier = true,
                LogWarnings = false,
                ParticipantCount = participantCount,
                InitialHandTileCount = 0,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = enableAutoDraw,
                AutoDiscardDrawnTileDelaySeconds = autoDiscardDelaySeconds,
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
            session.RegisterOwnedScriptableObject(catalog);
            return new ReachGameFlowTestSupport(session);
        }

        public void StartNewRound()
        {
            Commands.StartNewRound();
        }

        public void DrawReachableHand()
        {
            StartNewRound();
            AddHandTiles(
                "East",
                "1m", "2m", "3m",
                "2p", "3p", "4p",
                "7s", "8s", "9s",
                "E", "E", "E",
                "5m");

            ForceDraw("6m");
            RequestDraw();
        }

        public void DrawWinningHand()
        {
            StartNewRound();
            AddHandTiles(
                "East",
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");

            ForceDraw("C");
            RequestDraw();
        }

        public void AddHandTiles(string seatName, params string[] tileCodes)
        {
            DataFactory.AddHandTiles(Query.GetPlayerSeat(seatName), tileCodes);
        }

        public void SetParticipantType(string seatName, string participantTypeName)
        {
            DataFactory.SetParticipantType(CurrentState, seatName, participantTypeName);
        }

        public void ForceDraw(string tileCode)
        {
            Commands.RequestForceDrawSkill(tileCode);
        }

        public void ForceDrawForSeat(string seatName, string tileCode)
        {
            Commands.RequestForceDrawSkillForSeat(seatName, tileCode);
        }

        public void RequestDraw()
        {
            Commands.RequestDraw();
        }

        public void DrawAndDiscardForSeat(string seatName, string tileCode)
        {
            Commands.RequestForceDrawSkillForSeat(seatName, tileCode);
            Assert.That(Commands.TryRequestDrawForSeat(seatName), Is.True);
            Assert.That(Commands.TryRequestDiscardDrawnTileForSeat(seatName), Is.True);
        }

        public void RequestDeclareReach()
        {
            Reflection.Invoke(GameFlow, "RequestDeclareReach");
        }

        public void RequestCancelReachDiscardSelection()
        {
            Reflection.Invoke(GameFlow, "RequestCancelReachDiscardSelection");
        }

        public void RequestDeclineReach()
        {
            Reflection.Invoke(GameFlow, "RequestDeclineReach");
        }

        public void RequestDiscard(int handIndex)
        {
            Commands.RequestDiscard(handIndex);
        }

        public void RequestDiscardDrawnTile()
        {
            Commands.RequestDiscardDrawnTile();
        }

        public void RequestDeclineWin()
        {
            Commands.RequestDeclineWin();
        }

        public object BuildTurnAutomationPolicy(string seatName)
        {
            return Reflection.Invoke(
                GameFlow,
                "BuildTurnAutomationPolicy",
                DataFactory.ParseSeat(seatName));
        }

        public bool ShouldAutoDiscardDrawnTileAfterDraw(string seatName)
        {
            return (bool)Reflection.Invoke(
                GameFlow,
                "ShouldAutoDiscardDrawnTileAfterDraw",
                DataFactory.ParseSeat(seatName));
        }

        public void TryAutoDiscardDrawnTileAfterDraw(string seatName)
        {
            Reflection.Invoke(
                GameFlow,
                "TryAutoDiscardDrawnTileAfterDraw",
                DataFactory.ParseSeat(seatName));
        }

        public object BeginAutoDiscardRoutine(string seatName)
        {
            return Reflection.InvokeWithSignature(
                GameFlow,
                "RunAutoDiscardDrawnTileAfterDraw",
                new[] { Types.SeatId, typeof(int), typeof(int) },
                DataFactory.ParseSeat(seatName),
                Query.TurnIndex,
                Reflection.GetPrivateField(GameFlow, "autoDiscardDrawnTileOperationVersion"));
        }

        public bool MoveNext(object routine)
        {
            return ((IEnumerator)routine).MoveNext();
        }

        public string CurrentYieldTypeName(object routine)
        {
            object current = ((IEnumerator)routine).Current;
            return current == null ? null : current.GetType().Name;
        }

        public bool IsWinDecisionPending => Query.IsWinDecisionPending;

        public bool IsReachDecisionPending =>
            (bool)Reflection.GetProperty(CurrentState, "IsReachDecisionPending");

        public bool IsReachDiscardSelectionPending =>
            (bool)Reflection.GetProperty(CurrentState, "IsReachDiscardSelectionPending");

        public string TurnPhaseName => Query.TurnPhaseName;

        public string CurrentTurnName => Query.CurrentTurnName;

        public int TurnIndex => Query.TurnIndex;

        public int ReachDiscardCandidateCount =>
            Collections.Count(Reflection.GetProperty(CurrentState, "ReachDiscardCandidates"));

        public int DiscardCount => Query.DiscardCount;

        public string WinDecisionTypeName => Query.WinDecisionTypeName;

        public string WinDecisionSeatName => Query.WinDecisionSeatName;

        public string WinSourceSeatName => Query.WinSourceSeatName;

        public bool IsReachDeclared(string seatName)
        {
            return (bool)Reflection.GetProperty(Query.GetPlayerSeat(seatName), "IsReachDeclared");
        }

        public bool IsIppatsuEligible(string seatName)
        {
            return (bool)Reflection.GetProperty(Query.GetPlayerSeat(seatName), "IsIppatsuEligible");
        }

        public int ReachDeclaredTurnIndex(string seatName)
        {
            return (int)Reflection.GetProperty(Query.GetPlayerSeat(seatName), "ReachDeclaredTurnIndex");
        }

        public bool HasDrawnTile(string seatName)
        {
            return Query.HasDrawnTile(seatName);
        }

        public string DrawnTileCode(string seatName)
        {
            return Query.DrawnTileCode(seatName);
        }

        public string DiscardActorSeatNameAt(int index)
        {
            return Query.DiscardActorSeatNameAt(index);
        }

        public string DiscardSourceNameAt(int index)
        {
            return Query.DiscardSourceNameAt(index);
        }

        public string DiscardTileCodeAt(int index)
        {
            return Query.DiscardTileCodeAt(index);
        }

        public string LastDiscardActorSeatName => Query.LastDiscardActorSeatName;

        public string LastDiscardSourceName => Query.LastDiscardSourceName;

        public string LastDiscardTileCode => Query.LastDiscardTileCode;

        public bool PolicyIsCpu(object policy)
        {
            return (bool)Reflection.GetProperty(policy, "IsCpu");
        }

        public bool PolicyAutoDrawAtTurnStart(object policy)
        {
            return (bool)Reflection.GetProperty(policy, "AutoDrawAtTurnStart");
        }

        public bool PolicyAutoDiscardDrawnTileAfterDraw(object policy)
        {
            return (bool)Reflection.GetProperty(policy, "AutoDiscardDrawnTileAfterDraw");
        }

        public bool PolicyUseCpuController(object policy)
        {
            return (bool)Reflection.GetProperty(policy, "UseCpuController");
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
