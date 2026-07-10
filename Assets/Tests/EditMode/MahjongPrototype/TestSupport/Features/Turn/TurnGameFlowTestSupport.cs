using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Features.Turn
{
    internal sealed class TurnGameFlowTestSupport : IDisposable
    {
        private const string CpuTurnControllerTypeName =
            "MahjongPrototype.CpuTurnController, Assembly-CSharp";

        private readonly MahjongGameFlowTestSession session;
        private bool disposed;

        private TurnGameFlowTestSupport(MahjongGameFlowTestSession session)
        {
            this.session = session;

            if (session.EventNotifier != null)
                Reflection.SetPrivateField(GameFlow, "eventNotifier", session.EventNotifier);
        }

        public object GameFlow => session.GameFlow;
        public object EventNotifier => session.EventNotifier;
        public object CurrentState => session.CurrentState;
        public ReflectionTestAccess Reflection => session.Reflection;
        public CollectionTestAccess Collections => session.Collections;
        public MahjongTestDataFactory DataFactory => session.DataFactory;

        public static TurnGameFlowTestSupport Create(
            string rootName,
            int participantCount = 1,
            int initialHandTileCount = 1,
            bool enableAutoDraw = false,
            string fixedSelfSeatName = "East",
            bool addEventNotifier = false)
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
                AddEventNotifier = addEventNotifier,
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
            return new TurnGameFlowTestSupport(session);
        }

        public void StartNewRound() => Commands.StartNewRound();
        public void RetryPrototype() => Commands.RetryPrototype();
        public void RequestDraw() => Commands.RequestDraw();
        public void RequestDiscard(int handIndex) => Commands.RequestDiscard(handIndex);
        public void RequestDiscardDrawnTile() => Commands.RequestDiscardDrawnTile();
        public void RequestForceDrawSkill(string tileCode) => Commands.RequestForceDrawSkill(tileCode);
        public void RequestForceDrawSkillForSeat(string seatName, string tileCode) =>
            Commands.RequestForceDrawSkillForSeat(seatName, tileCode);
        public void RequestSetAutoSortEnabled(bool enabled) =>
            Reflection.Invoke(GameFlow, "RequestSetAutoSortEnabled", enabled);
        public void DealInitialHands() => Commands.DealInitialHands();
        public void CheckWinPrototype() => Commands.CheckWinPrototype();
        public void RequestDeclareWin() => Commands.RequestDeclareWin();
        public void RequestDeclineWin() => Commands.RequestDeclineWin();

        public void StartCurrentTurnAgain()
        {
            Commands.StartTurn(CurrentTurnName, TurnIndex);
        }

        public void BeginWinDecisionForCurrentTurn()
        {
            Reflection.Invoke(
                CurrentState,
                "BeginWinDecision",
                DataFactory.ParseSeat(CurrentTurnName),
                TurnIndex);
        }

        public void SetWinDecisionPendingForCurrentTurn()
        {
            Commands.SetWinDecisionPending(CurrentTurnName, TurnIndex);
        }

        public void SetFixedSelfSeat(string seatName)
        {
            Reflection.SetPrivateField(GameFlow, "fixedSelfSeat", DataFactory.ParseSeat(seatName));
        }

        public void SetAutoSortEnabled(bool enabled)
        {
            Reflection.SetPrivateField(GameFlow, "autoSortEnabled", enabled);
        }

        public void SetCurrentTurn(string seatName)
        {
            DataFactory.SetCurrentTurn(CurrentState, seatName);
        }

        public void SetCurrentTurnToPlayerId(string playerIdName)
        {
            Reflection.SetProperty(
                CurrentState,
                "CurrentTurn",
                DataFactory.ParseSeat(Query.SeatByPlayerIdName(playerIdName)));
        }

        public void SetRoundEnded(bool value)
        {
            Reflection.SetProperty(CurrentState, "IsRoundEnded", value);
        }

        public void SetParticipantType(string seatName, string participantTypeName)
        {
            DataFactory.SetParticipantType(CurrentState, seatName, participantTypeName);
        }

        public void SetParticipantTypeForPlayerId(string playerIdName, string participantTypeName)
        {
            Reflection.Invoke(
                CurrentState,
                "SetParticipantType",
                DataFactory.ParseSeat(Query.SeatByPlayerIdName(playerIdName)),
                DataFactory.ParseParticipantType(participantTypeName));
        }

        public void AddHandTiles(string seatName, params string[] tileCodes)
        {
            DataFactory.AddHandTiles(Query.GetPlayerSeat(seatName), tileCodes);
        }

        public void AddHandTilesForPlayerId(string playerIdName, params string[] tileCodes)
        {
            DataFactory.AddHandTiles(Query.GetPlayerSeatByPlayerId(playerIdName), tileCodes);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            DataFactory.SetDrawnTile(CurrentState, seatName, tileCode);
        }

        public void SetDrawnTileForPlayerId(string playerIdName, string tileCode)
        {
            Reflection.Invoke(
                Query.GetPlayerSeatByPlayerId(playerIdName),
                "SetDrawnTile",
                DataFactory.CreateTile(tileCode));
        }

        public void ClearCurrentPlayerDrawnTile()
        {
            Reflection.Invoke(Query.GetPlayerSeat(CurrentTurnName), "ClearDrawnTile");
        }

        public void DeclareReach(string seatName, int turnIndex)
        {
            Reflection.Invoke(Query.GetPlayerSeat(seatName), "DeclareReach", turnIndex);
        }

        public bool TryRequestDrawForSeat(string seatName)
        {
            return Commands.TryRequestDrawForSeat(seatName);
        }

        public bool TryRequestDrawForPlayerId(string playerIdName)
        {
            return Commands.TryRequestDrawForSeat(Query.SeatByPlayerIdName(playerIdName));
        }

        public bool TryRequestDiscardDrawnTileForSeat(string seatName)
        {
            return Commands.TryRequestDiscardDrawnTileForSeat(seatName);
        }

        public bool TryRequestDiscardDrawnTileForPlayerId(string playerIdName)
        {
            return Commands.TryRequestDiscardDrawnTileForSeat(Query.SeatByPlayerIdName(playerIdName));
        }

        public void ApplyAutoSortIfEnabled(string seatName, string reason, bool notify)
        {
            Reflection.Invoke(
                GameFlow,
                "ApplyAutoSortIfEnabled",
                DataFactory.ParseSeat(seatName),
                reason,
                notify);
        }

        public void ApplyAutoSortIfEnabledForPlayerId(string playerIdName, string reason, bool notify)
        {
            Reflection.Invoke(
                GameFlow,
                "ApplyAutoSortIfEnabled",
                DataFactory.ParseSeat(Query.SeatByPlayerIdName(playerIdName)),
                reason,
                notify);
        }

        public void SetCpuDiscardDelay(float delaySeconds)
        {
            Component gameFlowComponent = (Component)GameFlow;
            Type cpuTurnControllerType = Reflection.RequireType(CpuTurnControllerTypeName);
            object cpuTurnController = Reflection.GetPrivateField(GameFlow, "cpuTurnController");

            if (cpuTurnController == null)
            {
                cpuTurnController = gameFlowComponent.gameObject.GetComponent(cpuTurnControllerType);
                if (cpuTurnController == null)
                    cpuTurnController = gameFlowComponent.gameObject.AddComponent(cpuTurnControllerType);

                Reflection.SetPrivateField(GameFlow, "cpuTurnController", cpuTurnController);
            }

            Reflection.SetPrivateField(cpuTurnController, "cpuDiscardDelaySeconds", delaySeconds);
        }

        public void AddSingleArgumentEventHandler(
            string eventName,
            Action<object> callback)
        {
            AddEventHandler(
                eventName,
                parameters =>
                {
                    return Expression.Invoke(
                        Expression.Constant(callback),
                        Expression.Convert(parameters[0], typeof(object)));
                });
        }

        public void AddTwoArgumentEventHandler(
            string eventName,
            Action<object, int> callback)
        {
            AddEventHandler(
                eventName,
                parameters =>
                {
                    return Expression.Invoke(
                        Expression.Constant(callback),
                        Expression.Convert(parameters[0], typeof(object)),
                        parameters[1]);
                });
        }

        public string SeatByPlayerId(string playerIdName) => Query.SeatByPlayerIdName(playerIdName);

        public string[] OccupiedSeatNames => Query.OccupiedSeatNames;

        public string[] ActiveTurnSeatNames => Query.ActiveTurnSeatNames;

        public string[] ActiveSeatNames => Query.ActiveSeatNames;

        public string SelfSeatName => Query.SelfSeatName;

        public string SelfWindName => Query.SelfWindName;

        public string SelfPlayerIdName => Query.SelfPlayerIdName;

        public string CurrentTurnName => Query.CurrentTurnName;

        public string CurrentTurnPlayerIdName => Query.CurrentTurnPlayerIdName;

        public bool IsSelfTurn => Query.IsSelfTurn;

        public int TurnIndex => Query.TurnIndex;

        public string TurnPhaseName => Query.TurnPhaseName;

        public bool IsRoundEnded => Query.IsRoundEnded;

        public bool IsRoundResultPending => Query.IsRoundResultPending;

        public string RoundResultTypeName => Query.RoundResultTypeName;

        public string RoundResultWinnerSeatName => Query.RoundResultWinnerSeatNameOrNull;

        public bool IsInteractionLocked => Query.IsInteractionLocked;

        public int WallCount => Query.WallCount;

        public int DiscardCount => Query.DiscardCount;

        public int ActiveSkillEffectCount => Query.ActiveSkillEffectCount;

        public bool CurrentPlayerHasDrawnTile => Query.CurrentPlayerHasDrawnTile;

        public string CurrentPlayerDrawnTileCodeOrNull => Query.CurrentPlayerDrawnTileCodeOrNull;

        public bool HasDrawnTile(string seatName) => Query.HasDrawnTile(seatName);

        public bool HasDrawnTileForPlayerId(string playerIdName) =>
            Query.HasDrawnTileForPlayerId(playerIdName);

        public string DrawnTileCodeOrNullForPlayerId(string playerIdName) =>
            Query.DrawnTileCodeOrNullForPlayerId(playerIdName);

        public int HandCountForPlayerId(string playerIdName) =>
            Query.HandCountForPlayerId(playerIdName);

        public string HandDisplayString(string seatName) => Query.HandDisplayString(seatName);

        public string HandDisplayStringForPlayerId(string playerIdName) =>
            Query.HandDisplayStringForPlayerId(playerIdName);

        public int SeatSlotCount => Query.SeatSlotCount;

        public string SeatSlotWindAt(int index) => Query.SeatSlotWindAt(index);

        public string SeatSlotPlayerIdNameOrNullAt(int index) =>
            Query.SeatSlotPlayerIdNameOrNullAt(index);

        public bool SeatSlotHasPlayerAt(int index) => Query.SeatSlotHasPlayerAt(index);

        public bool SeatSlotIsEmptyAt(int index) => Query.SeatSlotIsEmptyAt(index);

        public string SeatSlotStateLabelAt(int index) => Query.SeatSlotStateLabelAt(index);

        public string SelfSeatSlotWindName => Query.SelfSeatSlotWindName;

        public string CurrentTurnSlotWindName => Query.CurrentTurnSlotWindName;

        public bool IsSelfSeat(string seatName) => Query.IsSelfSeat(seatName);

        public string ParticipantTypeNameOrNull(string seatName) =>
            Query.ParticipantTypeNameOrNull(seatName);

        public string ParticipantTypeNameOrNullForPlayerId(string playerIdName) =>
            Query.ParticipantTypeNameOrNullForPlayerId(playerIdName);

        public string ActiveSkillEffectOwnerSeatNameAt(int index) =>
            Query.ActiveSkillEffectOwnerSeatNameAt(index);

        public string LastDiscardActorSeatName => Query.LastDiscardActorSeatName;

        public string LastDiscardTileCode => Query.LastDiscardTileCode;

        public string LastDiscardSourceName => Query.LastDiscardSourceName;

        public bool IsWinDecisionPending => Query.IsWinDecisionPending;

        public string WinDecisionSeatName => Query.WinDecisionSeatName;

        public string WinDecisionTypeName => Query.WinDecisionTypeNameOrNull;

        public string WinSourceSeatNameOrNull => Query.WinSourceSeatNameOrNull;

        public int WinDecisionTurnIndex => Query.WinDecisionTurnIndex;

        public string WinningTileCodeOrNull => Query.WinningTileCodeOrNull;

        public bool PendingWinDeclarationEvaluationIsNull =>
            Query.PendingWinDeclarationEvaluationIsNull;

        public bool FlowIsWinDecisionPending =>
            (bool)Reflection.GetProperty(GameFlow, "IsWinDecisionPending");

        public string WindProgressRoundWindName => Query.WindProgressRoundWindName;

        public int WindProgressHandNumber => Query.WindProgressHandNumber;

        public object StateToken => Query.StateToken;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.Dispose();
        }

        private MahjongGameStateTestQuery Query => session.Query;
        private MahjongGameFlowTestCommands Commands => session.Commands;

        private void AddEventHandler(
            string eventName,
            Func<ParameterExpression[], InvocationExpression> createCallbackInvocation)
        {
            EventInfo eventInfo = EventNotifier.GetType().GetEvent(eventName);
            Type delegateType = eventInfo.EventHandlerType;
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            ParameterExpression[] expressions = new ParameterExpression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                expressions[i] = Expression.Parameter(parameters[i].ParameterType, $"value{i}");

            Delegate handler = Expression.Lambda(
                delegateType,
                createCallbackInvocation(expressions),
                expressions).Compile();
            eventInfo.AddEventHandler(EventNotifier, handler);
        }
    }
}
