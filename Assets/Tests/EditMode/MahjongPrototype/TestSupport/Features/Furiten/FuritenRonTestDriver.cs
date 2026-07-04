using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Furiten
{
    internal sealed class FuritenRonTestDriver : IDisposable
    {
        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private FuritenRonTestDriver(MahjongGameFlowTestSession session)
        {
            this.session = session;
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

            MahjongGameFlowTestSession session = MahjongGameFlowTestSession.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            session.RegisterOwnedScriptableObject(catalog);
            return new FuritenRonTestDriver(session);
        }

        public object CurrentState => session.CurrentState;

        public void StartRound()
        {
            Commands.StartNewRound();
        }

        public void SetHand(string seatName, params string[] tileCodes)
        {
            session.DataFactory.AddHandTiles(Query.GetPlayerSeat(seatName), tileCodes);
        }

        public void AddDiscard(string seatName, string tileCode, int turnIndex)
        {
            session.DataFactory.AddDiscard(CurrentState, seatName, tileCode, turnIndex);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            session.DataFactory.SetDrawnTile(CurrentState, seatName, tileCode);
        }

        public void SetCurrentTurn(string seatName)
        {
            session.DataFactory.SetCurrentTurn(CurrentState, seatName);
        }

        public void SetSeatParticipantType(string seatName, string participantTypeName)
        {
            session.DataFactory.SetParticipantType(CurrentState, seatName, participantTypeName);
        }

        public bool DiscardDrawnTile(string seatName)
        {
            return Commands.TryRequestDiscardDrawnTileForSeat(seatName);
        }

        public void DrawSelfTile(string tileCode)
        {
            Commands.RequestForceDrawSkill(tileCode);
            Commands.RequestDraw();
        }

        public object EvaluateAllFuriten()
        {
            return session.Reflection.Invoke(session.GameFlow, "EvaluateAllFuriten");
        }

        public bool TryGetSeatResult(object resultSet, string seatName, out object result)
        {
            object[] args = { session.DataFactory.ParseSeat(seatName), null };
            bool found = (bool)session.Reflection.Invoke(resultSet, "TryGet", args);
            result = args[1];
            return found;
        }

        public bool IsSeatDiscardFuriten(string seatName)
        {
            object resultSet = EvaluateAllFuriten();
            NUnit.Framework.Assert.That(TryGetSeatResult(resultSet, seatName, out object result), NUnit.Framework.Is.True);
            return (bool)session.Reflection.GetProperty(result, "IsDiscardFuriten");
        }

        public bool IsSeatFuriten(string seatName)
        {
            object resultSet = EvaluateAllFuriten();
            NUnit.Framework.Assert.That(TryGetSeatResult(resultSet, seatName, out object result), NUnit.Framework.Is.True);
            return (bool)session.Reflection.GetProperty(result, "IsFuriten");
        }

        public int ResultCount(object resultSet)
        {
            return (int)session.Reflection.GetProperty(resultSet, "Count");
        }

        public bool IsWinDecisionPending => Query.IsWinDecisionPending;

        public string WinDecisionType => Query.WinDecisionTypeNameOrNull;

        public string WinDecisionSeat => Query.WinDecisionSeatNameOrNull;

        public string WinSourceSeat => Query.WinSourceSeatNameOrNull;

        public string CurrentTurn => Query.CurrentTurnName;

        public int TurnIndex => Query.TurnIndex;

        public string GameStateSnapshot()
        {
            return string.Join(
                "|",
                session.Reflection.GetProperty(CurrentState, "CurrentTurn"),
                session.Reflection.GetProperty(CurrentState, "TurnIndex"),
                session.Reflection.GetProperty(CurrentState, "IsRoundEnded"),
                session.Reflection.GetProperty(CurrentState, "IsWinDecisionPending"),
                session.Reflection.GetProperty(CurrentState, "IsReachDecisionPending"),
                session.Reflection.GetProperty(CurrentState, "IsReachDiscardSelectionPending"),
                session.Collections.Count(session.Reflection.GetProperty(CurrentState, "Discards")),
                SeatSlotsSnapshot());
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private string SeatSlotsSnapshot()
        {
            object seatSlots = session.Reflection.GetProperty(CurrentState, "SeatSlots");
            int count = session.Collections.Count(seatSlots);
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
            {
                object slot = session.Collections.Item(seatSlots, i);
                object playerId = session.Reflection.GetProperty(slot, "PlayerId");
                object participantType = session.Reflection.GetProperty(slot, "ParticipantType");
                snapshot += "|" +
                    session.Reflection.GetProperty(slot, "Wind") + ":" +
                    (playerId == null ? "Empty" : playerId.ToString()) + ":" +
                    (participantType == null ? "None" : participantType.ToString());
            }

            return snapshot;
        }

        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;
    }
}
