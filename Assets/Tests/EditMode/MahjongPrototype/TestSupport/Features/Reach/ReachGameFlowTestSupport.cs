using System;
using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests.TestSupport.Features.Reach
{
    internal sealed class ReachGameFlowTestSupport : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private ReachGameFlowTestSupport(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
        }

        public object GameFlow => flow.GameFlow;
        public object CurrentState => flow.CurrentState;
        public ReflectionTestAccess Reflection => flow.Reflection;
        public CollectionTestAccess Collections => flow.Collections;
        public MahjongTestTypes Types => flow.Types;
        public MahjongTestDataFactory DataFactory => flow.DataFactory;

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
            object catalog = dataFactory.CreateYakuCatalog(
                dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"),
                dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                dataFactory.CreateYakuDefinition("Tanyao", "One", "One"));
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

            MahjongGameFlowTestHarness flow = MahjongGameFlowTestHarness.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            flow.RegisterOwnedScriptableObject(catalog);
            return new ReachGameFlowTestSupport(flow);
        }

        public void StartNewRound()
        {
            Reflection.Invoke(GameFlow, "StartNewRound");
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
            DataFactory.AddHandTiles(PlayerSeat(seatName), tileCodes);
        }

        public void SetParticipantType(string seatName, string participantTypeName)
        {
            DataFactory.SetParticipantType(CurrentState, seatName, participantTypeName);
        }

        public void ForceDraw(string tileCode)
        {
            Reflection.Invoke(GameFlow, "RequestForceDrawSkill", tileCode);
        }

        public void ForceDrawForSeat(string seatName, string tileCode)
        {
            Reflection.Invoke(
                GameFlow,
                "RequestForceDrawSkillForSeat",
                DataFactory.ParseSeat(seatName),
                tileCode);
        }

        public void RequestDraw()
        {
            Reflection.Invoke(GameFlow, "RequestDraw");
        }

        public void DrawAndDiscardForSeat(string seatName, string tileCode)
        {
            object seat = DataFactory.ParseSeat(seatName);
            Reflection.Invoke(GameFlow, "RequestForceDrawSkillForSeat", seat, tileCode);
            Assert.That(Reflection.Invoke(GameFlow, "TryRequestDrawForSeat", seat), Is.True);
            Assert.That(Reflection.Invoke(GameFlow, "TryRequestDiscardDrawnTileForSeat", seat), Is.True);
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
            Reflection.Invoke(GameFlow, "RequestDiscard", handIndex);
        }

        public void RequestDiscardDrawnTile()
        {
            Reflection.Invoke(GameFlow, "RequestDiscardDrawnTile");
        }

        public void RequestDeclineWin()
        {
            Reflection.Invoke(GameFlow, "RequestDeclineWin");
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
                TurnIndex,
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

        public bool IsWinDecisionPending =>
            (bool)Reflection.GetProperty(CurrentState, "IsWinDecisionPending");

        public bool IsReachDecisionPending =>
            (bool)Reflection.GetProperty(CurrentState, "IsReachDecisionPending");

        public bool IsReachDiscardSelectionPending =>
            (bool)Reflection.GetProperty(CurrentState, "IsReachDiscardSelectionPending");

        public string TurnPhaseName =>
            Reflection.GetProperty(CurrentState, "TurnPhase").ToString();

        public string CurrentTurnName =>
            Reflection.GetProperty(CurrentState, "CurrentTurn").ToString();

        public int TurnIndex =>
            (int)Reflection.GetProperty(CurrentState, "TurnIndex");

        public int ReachDiscardCandidateCount =>
            Collections.Count(Reflection.GetProperty(CurrentState, "ReachDiscardCandidates"));

        public int DiscardCount =>
            Collections.Count(Reflection.GetProperty(CurrentState, "Discards"));

        public string WinDecisionTypeName =>
            Reflection.GetProperty(CurrentState, "WinDecisionType").ToString();

        public string WinDecisionSeatName =>
            Reflection.GetProperty(CurrentState, "WinDecisionSeat").ToString();

        public string WinSourceSeatName =>
            Reflection.GetProperty(CurrentState, "WinSourceSeat").ToString();

        public bool IsReachDeclared(string seatName)
        {
            return (bool)Reflection.GetProperty(PlayerSeat(seatName), "IsReachDeclared");
        }

        public int ReachDeclaredTurnIndex(string seatName)
        {
            return (int)Reflection.GetProperty(PlayerSeat(seatName), "ReachDeclaredTurnIndex");
        }

        public bool HasDrawnTile(string seatName)
        {
            return (bool)Reflection.GetProperty(PlayerSeat(seatName), "HasDrawnTile");
        }

        public string DrawnTileCode(string seatName)
        {
            return Reflection.GetProperty(PlayerSeat(seatName), "DrawnTile").ToString();
        }

        public string DiscardActorSeatNameAt(int index)
        {
            return Reflection.GetProperty(DiscardAt(index), "ActorSeat").ToString();
        }

        public string DiscardSourceNameAt(int index)
        {
            return Reflection.GetProperty(DiscardAt(index), "Source").ToString();
        }

        public string DiscardTileCodeAt(int index)
        {
            return Reflection.GetProperty(DiscardAt(index), "Tile").ToString();
        }

        public string LastDiscardActorSeatName =>
            Reflection.GetProperty(LastDiscard, "ActorSeat").ToString();

        public string LastDiscardSourceName =>
            Reflection.GetProperty(LastDiscard, "Source").ToString();

        public string LastDiscardTileCode =>
            Reflection.GetProperty(LastDiscard, "Tile").ToString();

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
            flow.Dispose();
        }

        private object PlayerSeat(string seatName)
        {
            return DataFactory.GetPlayerSeat(CurrentState, seatName);
        }

        private object DiscardAt(int index)
        {
            return Collections.Item(Reflection.GetProperty(CurrentState, "Discards"), index);
        }

        private object LastDiscard =>
            Collections.Last(Reflection.GetProperty(CurrentState, "Discards"));
    }
}
