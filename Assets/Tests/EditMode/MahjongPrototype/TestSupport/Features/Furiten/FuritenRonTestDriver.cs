using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Furiten
{
    internal sealed class FuritenRonTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private FuritenRonTestDriver(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;
        }

        public static FuritenRonTestDriver Create(int participantCount)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object catalog =
                MahjongTestCatalogFactory.CreateStandardGameFlowYakuCatalog(dataFactory);

            MahjongGameFlowTestOptions options = new MahjongGameFlowTestOptions
            {
                RootName = "FuritenRonGameFlowHarness",
                AddEventNotifier = true,
                LogWarnings = false,
                ParticipantCount = participantCount,
                InitialHandTileCount = 0,
                AutoStart = false,
                UseFixedRandomSeed = true,
                FixedRandomSeed = 12345,
                EnableAutoDraw = false,
                AutoDiscardDrawnTileDelaySeconds = 0f,
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
            return new FuritenRonTestDriver(flow);
        }

        public object CurrentState => flow.CurrentState;

        public void StartRound()
        {
            flow.StartRound();
        }

        public void SetHand(string seatName, params string[] tileCodes)
        {
            flow.DataFactory.AddHandTiles(flow.GetPlayerSeat(seatName), tileCodes);
        }

        public void AddDiscard(string seatName, string tileCode, int turnIndex)
        {
            flow.DataFactory.AddDiscard(CurrentState, seatName, tileCode, turnIndex);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            flow.DataFactory.SetDrawnTile(CurrentState, seatName, tileCode);
        }

        public void SetCurrentTurn(string seatName)
        {
            flow.SetCurrentTurn(seatName);
        }

        public void SetSeatParticipantType(string seatName, string participantTypeName)
        {
            flow.DataFactory.SetParticipantType(CurrentState, seatName, participantTypeName);
        }

        public bool DiscardDrawnTile(string seatName)
        {
            return (bool)flow.Reflection.Invoke(
                flow.GameFlow,
                "TryRequestDiscardDrawnTileForSeat",
                flow.DataFactory.ParseSeat(seatName));
        }

        public void DrawSelfTile(string tileCode)
        {
            flow.Reflection.Invoke(flow.GameFlow, "RequestForceDrawSkill", tileCode);
            flow.Reflection.Invoke(flow.GameFlow, "RequestDraw");
        }

        public object EvaluateAllFuriten()
        {
            return flow.Reflection.Invoke(flow.GameFlow, "EvaluateAllFuriten");
        }

        public bool TryGetSeatResult(object resultSet, string seatName, out object result)
        {
            object[] args = { flow.DataFactory.ParseSeat(seatName), null };
            bool found = (bool)flow.Reflection.Invoke(resultSet, "TryGet", args);
            result = args[1];
            return found;
        }

        public bool IsSeatDiscardFuriten(string seatName)
        {
            object resultSet = EvaluateAllFuriten();
            NUnit.Framework.Assert.That(TryGetSeatResult(resultSet, seatName, out object result), NUnit.Framework.Is.True);
            return (bool)flow.Reflection.GetProperty(result, "IsDiscardFuriten");
        }

        public bool IsSeatFuriten(string seatName)
        {
            object resultSet = EvaluateAllFuriten();
            NUnit.Framework.Assert.That(TryGetSeatResult(resultSet, seatName, out object result), NUnit.Framework.Is.True);
            return (bool)flow.Reflection.GetProperty(result, "IsFuriten");
        }

        public int ResultCount(object resultSet)
        {
            return (int)flow.Reflection.GetProperty(resultSet, "Count");
        }

        public bool IsWinDecisionPending => (bool)flow.Reflection.GetProperty(CurrentState, "IsWinDecisionPending");

        public string WinDecisionType
        {
            get
            {
                object value = flow.Reflection.GetProperty(CurrentState, "WinDecisionType");
                return value == null ? null : value.ToString();
            }
        }

        public string WinDecisionSeat
        {
            get
            {
                object value = flow.Reflection.GetProperty(CurrentState, "WinDecisionSeat");
                return value == null ? null : value.ToString();
            }
        }

        public string WinSourceSeat
        {
            get
            {
                object value = flow.Reflection.GetProperty(CurrentState, "WinSourceSeat");
                return value == null ? null : value.ToString();
            }
        }

        public string CurrentTurn => flow.Reflection.GetProperty(CurrentState, "CurrentTurn").ToString();

        public int TurnIndex => (int)flow.Reflection.GetProperty(CurrentState, "TurnIndex");

        public string GameStateSnapshot()
        {
            return string.Join(
                "|",
                flow.Reflection.GetProperty(CurrentState, "CurrentTurn"),
                flow.Reflection.GetProperty(CurrentState, "TurnIndex"),
                flow.Reflection.GetProperty(CurrentState, "IsRoundEnded"),
                flow.Reflection.GetProperty(CurrentState, "IsWinDecisionPending"),
                flow.Reflection.GetProperty(CurrentState, "IsReachDecisionPending"),
                flow.Reflection.GetProperty(CurrentState, "IsReachDiscardSelectionPending"),
                flow.Collections.Count(flow.Reflection.GetProperty(CurrentState, "Discards")),
                SeatSlotsSnapshot());
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
        }

        private string SeatSlotsSnapshot()
        {
            object seatSlots = flow.Reflection.GetProperty(CurrentState, "SeatSlots");
            int count = flow.Collections.Count(seatSlots);
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
            {
                object slot = flow.Collections.Item(seatSlots, i);
                object playerId = flow.Reflection.GetProperty(slot, "PlayerId");
                object participantType = flow.Reflection.GetProperty(slot, "ParticipantType");
                snapshot += "|" +
                    flow.Reflection.GetProperty(slot, "Wind") + ":" +
                    (playerId == null ? "Empty" : playerId.ToString()) + ":" +
                    (participantType == null ? "None" : participantType.ToString());
            }

            return snapshot;
        }
    }
}
