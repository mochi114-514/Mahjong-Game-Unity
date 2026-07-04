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

        private readonly MahjongGameFlowTestHarness flow;
        private bool disposed;

        private TurnGameFlowTestSupport(MahjongGameFlowTestHarness flow)
        {
            this.flow = flow;

            if (flow.EventNotifier != null)
                Reflection.SetPrivateField(GameFlow, "eventNotifier", flow.EventNotifier);
        }

        public object GameFlow => flow.GameFlow;
        public object EventNotifier => flow.EventNotifier;
        public object CurrentState => flow.CurrentState;
        public ReflectionTestAccess Reflection => flow.Reflection;
        public CollectionTestAccess Collections => flow.Collections;
        public MahjongTestDataFactory DataFactory => flow.DataFactory;

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
            object catalog = dataFactory.CreateYakuCatalog(
                dataFactory.CreateYakuDefinition("MenzenTsumo", "One", "None"),
                dataFactory.CreateYakuDefinition("Reach", "One", "None"),
                dataFactory.CreateYakuDefinition("Tanyao", "One", "One"));
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

            MahjongGameFlowTestHarness flow = MahjongGameFlowTestHarness.Create(
                options,
                reflection,
                collections,
                types,
                dataFactory);
            flow.RegisterOwnedScriptableObject(catalog);
            return new TurnGameFlowTestSupport(flow);
        }

        public void StartNewRound() => Reflection.Invoke(GameFlow, "StartNewRound");
        public void RetryPrototype() => Reflection.Invoke(GameFlow, "RetryPrototype");
        public void RequestDraw() => Reflection.Invoke(GameFlow, "RequestDraw");
        public void RequestDiscard(int handIndex) => Reflection.Invoke(GameFlow, "RequestDiscard", handIndex);
        public void RequestDiscardDrawnTile() => Reflection.Invoke(GameFlow, "RequestDiscardDrawnTile");
        public void RequestForceDrawSkill(string tileCode) =>
            Reflection.Invoke(GameFlow, "RequestForceDrawSkill", tileCode);
        public void RequestSetAutoSortEnabled(bool enabled) =>
            Reflection.Invoke(GameFlow, "RequestSetAutoSortEnabled", enabled);
        public void DealInitialHands() => Reflection.Invoke(GameFlow, "DealInitialHands");
        public void CheckWinPrototype() => Reflection.Invoke(GameFlow, "CheckWinPrototype");
        public void RequestDeclareWin() => Reflection.Invoke(GameFlow, "RequestDeclareWin");
        public void RequestDeclineWin() => Reflection.Invoke(GameFlow, "RequestDeclineWin");

        public void StartCurrentTurnAgain()
        {
            Reflection.Invoke(GameFlow, "StartTurn", CurrentTurn, TurnIndex);
        }

        public void BeginWinDecisionForCurrentTurn()
        {
            Reflection.Invoke(CurrentState, "BeginWinDecision", CurrentTurn, TurnIndex);
        }

        public void SetWinDecisionPendingForCurrentTurn()
        {
            Reflection.Invoke(GameFlow, "SetWinDecisionPending", true, CurrentTurn, TurnIndex);
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
            Reflection.SetProperty(CurrentState, "CurrentTurn", SeatByPlayerIdValue(playerIdName));
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
                SeatByPlayerIdValue(playerIdName),
                DataFactory.ParseParticipantType(participantTypeName));
        }

        public void AddHandTiles(string seatName, params string[] tileCodes)
        {
            DataFactory.AddHandTiles(PlayerSeat(seatName), tileCodes);
        }

        public void AddHandTilesForPlayerId(string playerIdName, params string[] tileCodes)
        {
            DataFactory.AddHandTiles(PlayerSeatByPlayerId(playerIdName), tileCodes);
        }

        public void SetDrawnTile(string seatName, string tileCode)
        {
            DataFactory.SetDrawnTile(CurrentState, seatName, tileCode);
        }

        public void SetDrawnTileForPlayerId(string playerIdName, string tileCode)
        {
            Reflection.Invoke(
                PlayerSeatByPlayerId(playerIdName),
                "SetDrawnTile",
                DataFactory.CreateTile(tileCode));
        }

        public void ClearCurrentPlayerDrawnTile()
        {
            Reflection.Invoke(CurrentPlayerSeat, "ClearDrawnTile");
        }

        public void DeclareReach(string seatName, int turnIndex)
        {
            Reflection.Invoke(PlayerSeat(seatName), "DeclareReach", turnIndex);
        }

        public bool TryRequestDrawForSeat(string seatName)
        {
            return (bool)Reflection.Invoke(
                GameFlow,
                "TryRequestDrawForSeat",
                DataFactory.ParseSeat(seatName));
        }

        public bool TryRequestDrawForPlayerId(string playerIdName)
        {
            return (bool)Reflection.Invoke(
                GameFlow,
                "TryRequestDrawForSeat",
                SeatByPlayerIdValue(playerIdName));
        }

        public bool TryRequestDiscardDrawnTileForSeat(string seatName)
        {
            return (bool)Reflection.Invoke(
                GameFlow,
                "TryRequestDiscardDrawnTileForSeat",
                DataFactory.ParseSeat(seatName));
        }

        public bool TryRequestDiscardDrawnTileForPlayerId(string playerIdName)
        {
            return (bool)Reflection.Invoke(
                GameFlow,
                "TryRequestDiscardDrawnTileForSeat",
                SeatByPlayerIdValue(playerIdName));
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
                SeatByPlayerIdValue(playerIdName),
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

        public string SeatByPlayerId(string playerIdName) =>
            SeatByPlayerIdValue(playerIdName).ToString();

        public string[] OccupiedSeatNames =>
            SeatNames(Reflection.GetProperty(CurrentState, "OccupiedSeats"));

        public string[] ActiveTurnSeatNames =>
            SeatNames(Reflection.GetProperty(CurrentState, "ActiveTurnSeats"));

        public string[] ActiveSeatNames =>
            SeatNames(Reflection.GetProperty(CurrentState, "ActiveSeats"));

        public string SelfSeatName =>
            Reflection.GetProperty(CurrentState, "SelfSeat").ToString();

        public string SelfWindName =>
            Reflection.GetProperty(CurrentState, "SelfWind").ToString();

        public string SelfPlayerIdName =>
            Reflection.GetProperty(CurrentState, "SelfPlayerId").ToString();

        public string CurrentTurnName => CurrentTurn.ToString();

        public string CurrentTurnPlayerIdName =>
            Reflection.GetProperty(CurrentState, "CurrentTurnPlayerId").ToString();

        public bool IsSelfTurn =>
            (bool)Reflection.GetProperty(CurrentState, "IsSelfTurn");

        public int TurnIndex =>
            (int)Reflection.GetProperty(CurrentState, "TurnIndex");

        public string TurnPhaseName =>
            Reflection.GetProperty(CurrentState, "TurnPhase").ToString();

        public bool IsRoundEnded =>
            (bool)Reflection.GetProperty(CurrentState, "IsRoundEnded");

        public bool IsInteractionLocked =>
            (bool)Reflection.GetProperty(CurrentState, "IsInteractionLocked");

        public int WallCount =>
            (int)Reflection.GetProperty(Reflection.GetProperty(CurrentState, "Wall"), "Count");

        public int DiscardCount =>
            Collections.Count(Reflection.GetProperty(CurrentState, "Discards"));

        public int ActiveSkillEffectCount =>
            Collections.Count(Reflection.GetProperty(CurrentState, "ActiveSkillEffects"));

        public bool CurrentPlayerHasDrawnTile =>
            (bool)Reflection.GetProperty(CurrentPlayerSeat, "HasDrawnTile");

        public string CurrentPlayerDrawnTileCodeOrNull =>
            NullablePropertyString(CurrentPlayerSeat, "DrawnTile");

        public bool HasDrawnTile(string seatName) =>
            (bool)Reflection.GetProperty(PlayerSeat(seatName), "HasDrawnTile");

        public bool HasDrawnTileForPlayerId(string playerIdName) =>
            (bool)Reflection.GetProperty(PlayerSeatByPlayerId(playerIdName), "HasDrawnTile");

        public string DrawnTileCodeOrNullForPlayerId(string playerIdName) =>
            NullablePropertyString(PlayerSeatByPlayerId(playerIdName), "DrawnTile");

        public int HandCountForPlayerId(string playerIdName) =>
            Collections.Count(Reflection.GetProperty(PlayerSeatByPlayerId(playerIdName), "Hand"));

        public string HandDisplayString(string seatName)
        {
            return (string)Reflection.Invoke(
                Reflection.GetProperty(PlayerSeat(seatName), "Hand"),
                "ToDisplayString");
        }

        public string HandDisplayStringForPlayerId(string playerIdName)
        {
            return (string)Reflection.Invoke(
                Reflection.GetProperty(PlayerSeatByPlayerId(playerIdName), "Hand"),
                "ToDisplayString");
        }

        public int SeatSlotCount =>
            Collections.Count(Reflection.GetProperty(CurrentState, "SeatSlots"));

        public string SeatSlotWindAt(int index) =>
            Reflection.GetProperty(SeatSlotAt(index), "Wind").ToString();

        public string SeatSlotPlayerIdNameOrNullAt(int index) =>
            NullablePropertyString(SeatSlotAt(index), "PlayerId");

        public bool SeatSlotHasPlayerAt(int index) =>
            (bool)Reflection.GetProperty(SeatSlotAt(index), "HasPlayer");

        public bool SeatSlotIsEmptyAt(int index) =>
            (bool)Reflection.GetProperty(SeatSlotAt(index), "IsEmpty");

        public string SeatSlotStateLabelAt(int index) =>
            Reflection.GetProperty(SeatSlotAt(index), "StateLabel").ToString();

        public string SelfSeatSlotWindName =>
            Reflection.GetProperty(Reflection.Invoke(CurrentState, "GetSelfSeatSlot"), "Wind").ToString();

        public string CurrentTurnSlotWindName =>
            Reflection.GetProperty(Reflection.GetProperty(CurrentState, "CurrentTurnSlot"), "Wind").ToString();

        public bool IsSelfSeat(string seatName)
        {
            return (bool)Reflection.Invoke(CurrentState, "IsSelfSeat", DataFactory.ParseSeat(seatName));
        }

        public string ParticipantTypeNameOrNull(string seatName)
        {
            return NullablePropertyString(
                Reflection.Invoke(CurrentState, "GetSeatSlot", DataFactory.ParseSeat(seatName)),
                "ParticipantType");
        }

        public string ParticipantTypeNameOrNullForPlayerId(string playerIdName)
        {
            return NullablePropertyString(
                Reflection.Invoke(CurrentState, "GetSeatSlot", SeatByPlayerIdValue(playerIdName)),
                "ParticipantType");
        }

        public string ActiveSkillEffectOwnerSeatNameAt(int index)
        {
            object effect = Collections.Item(
                Reflection.GetProperty(CurrentState, "ActiveSkillEffects"),
                index);
            return Reflection.GetProperty(effect, "OwnerSeat").ToString();
        }

        public string LastDiscardActorSeatName =>
            Reflection.GetProperty(LastDiscard, "ActorSeat").ToString();

        public string LastDiscardTileCode =>
            Reflection.GetProperty(LastDiscard, "Tile").ToString();

        public string LastDiscardSourceName =>
            Reflection.GetProperty(LastDiscard, "Source").ToString();

        public bool IsWinDecisionPending =>
            (bool)Reflection.GetProperty(CurrentState, "IsWinDecisionPending");

        public string WinDecisionSeatName =>
            Reflection.GetProperty(CurrentState, "WinDecisionSeat").ToString();

        public string WinDecisionTypeName =>
            NullablePropertyString(CurrentState, "WinDecisionType");

        public string WinSourceSeatNameOrNull =>
            NullablePropertyString(CurrentState, "WinSourceSeat");

        public int WinDecisionTurnIndex =>
            (int)Reflection.GetProperty(CurrentState, "WinDecisionTurnIndex");

        public string WinningTileCodeOrNull =>
            NullablePropertyString(CurrentState, "WinningTile");

        public bool PendingWinDeclarationEvaluationIsNull =>
            Reflection.GetProperty(CurrentState, "PendingWinDeclarationEvaluation") == null;

        public bool FlowIsWinDecisionPending =>
            (bool)Reflection.GetProperty(GameFlow, "IsWinDecisionPending");

        public string WindProgressRoundWindName =>
            Reflection.GetProperty(WindProgress, "RoundWind").ToString();

        public int WindProgressHandNumber =>
            (int)Reflection.GetProperty(WindProgress, "HandNumber");

        public object StateToken => CurrentState;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            flow.Dispose();
        }

        private object CurrentTurn =>
            Reflection.GetProperty(CurrentState, "CurrentTurn");

        private object CurrentPlayerSeat =>
            Reflection.Invoke(CurrentState, "GetPlayerSeat", CurrentTurn);

        private object PlayerSeat(string seatName)
        {
            return DataFactory.GetPlayerSeat(CurrentState, seatName);
        }

        private object PlayerSeatByPlayerId(string playerIdName)
        {
            return Reflection.Invoke(CurrentState, "GetPlayerSeat", SeatByPlayerIdValue(playerIdName));
        }

        private object SeatByPlayerIdValue(string playerIdName)
        {
            return Reflection.Invoke(
                CurrentState,
                "GetSeatByPlayerId",
                DataFactory.ParsePlayerId(playerIdName));
        }

        private object SeatSlotAt(int index)
        {
            return Collections.Item(Reflection.GetProperty(CurrentState, "SeatSlots"), index);
        }

        private object LastDiscard =>
            Collections.Last(Reflection.GetProperty(CurrentState, "Discards"));

        private object WindProgress =>
            Reflection.GetProperty(CurrentState, "WindProgress");

        private string[] SeatNames(object seats)
        {
            int count = Collections.Count(seats);
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = Collections.Item(seats, i).ToString();

            return names;
        }

        private string NullablePropertyString(object target, string propertyName)
        {
            object value = Reflection.GetProperty(target, propertyName);
            return value == null ? null : value.ToString();
        }

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
