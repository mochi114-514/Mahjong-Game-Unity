using System;
using MahjongPrototype.Tests.TestSupport.Core;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongGameStateTestQuery
    {
        private readonly Func<object> stateProvider;
        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;

        private MahjongGameStateTestQuery(
            Func<object> stateProvider,
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory)
        {
            this.stateProvider = stateProvider;
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
        }

        public static MahjongGameStateTestQuery ForHarness(MahjongGameFlowTestHarness harness)
        {
            return new MahjongGameStateTestQuery(
                () => harness.CurrentState,
                harness.Reflection,
                harness.Collections,
                harness.DataFactory);
        }

        public static MahjongGameStateTestQuery ForState(
            object gameState,
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory)
        {
            return new MahjongGameStateTestQuery(
                () => gameState,
                reflection,
                collections,
                dataFactory);
        }

        public object StateToken => State;
        public string CurrentTurnName => CurrentTurn.ToString();
        public string CurrentTurnPlayerIdName => GetStateProperty("CurrentTurnPlayerId").ToString();
        public int TurnIndex => (int)GetStateProperty("TurnIndex");
        public string TurnPhaseName => GetStateProperty("TurnPhase").ToString();
        public bool IsRoundEnded => (bool)GetStateProperty("IsRoundEnded");
        public bool IsRoundResultPending => (bool)GetStateProperty("IsRoundResultPending");
        public bool IsGameEnded => (bool)GetStateProperty("IsGameEnded");
        public object CurrentRoundResult => GetStateProperty("CurrentRoundResult");
        public bool CurrentRoundResultIsNull => CurrentRoundResult == null;
        public bool IsInteractionLocked => (bool)GetStateProperty("IsInteractionLocked");
        public string SelfSeatName => GetStateProperty("SelfSeat").ToString();
        public string SelfWindName => GetStateProperty("SelfWind").ToString();
        public string SelfPlayerIdName => GetStateProperty("SelfPlayerId").ToString();
        public bool IsSelfTurn => (bool)GetStateProperty("IsSelfTurn");
        public string[] OccupiedSeatNames => SeatNames(GetStateProperty("OccupiedSeats"));
        public string[] ActiveTurnSeatNames => SeatNames(GetStateProperty("ActiveTurnSeats"));
        public string[] ActiveSeatNames => SeatNames(GetStateProperty("ActiveSeats"));
        public int WallCount => (int)reflection.GetProperty(GetStateProperty("Wall"), "Count");
        public int DiscardCount => collections.Count(GetStateProperty("Discards"));
        public int ActiveSkillEffectCount => collections.Count(GetStateProperty("ActiveSkillEffects"));
        public bool CurrentPlayerHasDrawnTile => (bool)reflection.GetProperty(CurrentPlayerSeat, "HasDrawnTile");
        public string CurrentPlayerDrawnTileCodeOrNull =>
            NullablePropertyString(CurrentPlayerSeat, "DrawnTile");
        public int SeatSlotCount => collections.Count(GetStateProperty("SeatSlots"));
        public string SelfSeatSlotWindName =>
            reflection.GetProperty(reflection.Invoke(State, "GetSelfSeatSlot"), "Wind").ToString();
        public string CurrentTurnSlotWindName =>
            reflection.GetProperty(GetStateProperty("CurrentTurnSlot"), "Wind").ToString();
        public bool IsWinDecisionPending => (bool)GetStateProperty("IsWinDecisionPending");
        public bool IsReactionWindowPending =>
            (bool)GetStateProperty("IsReactionWindowPending");
        public object CurrentReactionWindow => GetStateProperty("CurrentReactionWindow");
        public int ReactionWindowId =>
            (int)reflection.GetProperty(CurrentReactionWindow, "WindowId");
        public int ReactionWindowCandidateCount => collections.Count(
            reflection.GetProperty(CurrentReactionWindow, "Candidates"));
        public string ReactionWindowCandidateKindAt(int index) => reflection.GetProperty(
            collections.Item(reflection.GetProperty(CurrentReactionWindow, "Candidates"), index),
            "Kind").ToString();
        public string ReactionWindowSourceSeatName => reflection.GetProperty(
            reflection.GetProperty(CurrentReactionWindow, "SourceDiscard"),
            "ActorSeat").ToString();
        public string ReactionWindowSourceTileCode => reflection.GetProperty(
            reflection.GetProperty(CurrentReactionWindow, "SourceDiscard"),
            "Tile").ToString();
        public string WinDecisionSeatName => GetStateProperty("WinDecisionSeat").ToString();
        public string WinDecisionSeatNameOrNull => NullablePropertyString(State, "WinDecisionSeat");
        public string WinDecisionTypeName => GetStateProperty("WinDecisionType").ToString();
        public string WinDecisionTypeNameOrNull => NullablePropertyString(State, "WinDecisionType");
        public string WinSourceSeatName => GetStateProperty("WinSourceSeat").ToString();
        public string WinSourceSeatNameOrNull => NullablePropertyString(State, "WinSourceSeat");
        public int WinDecisionTurnIndex => (int)GetStateProperty("WinDecisionTurnIndex");
        public string WinningTileCodeOrNull => NullablePropertyString(State, "WinningTile");
        public object PendingWinDeclarationEvaluation =>
            GetStateProperty("PendingWinDeclarationEvaluation");
        public bool PendingWinDeclarationEvaluationIsNull =>
            PendingWinDeclarationEvaluation == null;
        public bool HasLastTurnDraw => LastTurnDraw() != null;
        public string LastTurnDrawActorSeatName =>
            reflection.GetProperty(LastTurnDraw(), "ActorSeat").ToString();
        public string LastTurnDrawTileCode =>
            reflection.GetProperty(LastTurnDraw(), "Tile").ToString();
        public int LastTurnDrawTurnIndex =>
            (int)reflection.GetProperty(LastTurnDraw(), "TurnIndex");
        public bool LastTurnDrawIsLastLiveWallDraw =>
            (bool)reflection.GetProperty(LastTurnDraw(), "IsLastLiveWallDraw");
        public string WindProgressRoundWindName =>
            reflection.GetProperty(WindProgress, "RoundWind").ToString();
        public int WindProgressHandNumber =>
            (int)reflection.GetProperty(WindProgress, "HandNumber");
        public string RoundResultTypeName => RoundResultProperty("Type").ToString();
        public string RoundResultRoundWindName =>
            reflection.GetProperty(RoundResultProperty("WindProgress"), "RoundWind").ToString();
        public int RoundResultHandNumber =>
            (int)reflection.GetProperty(RoundResultProperty("WindProgress"), "HandNumber");
        public int RoundResultTurnIndex => (int)RoundResultProperty("TurnIndex");
        public bool RoundResultIsFinalRound => (bool)RoundResultProperty("IsFinalRound");
        public string RoundResultWinnerSeatNameOrNull =>
            NullablePropertyString(CurrentRoundResult, "WinnerSeat");
        public string RoundResultWinTypeNameOrNull =>
            NullablePropertyString(CurrentRoundResult, "WinType");
        public string RoundResultSourceSeatNameOrNull =>
            NullablePropertyString(CurrentRoundResult, "SourceSeat");
        public string RoundResultWinningTileCodeOrNull =>
            NullablePropertyString(CurrentRoundResult, "WinningTile");
        public object RoundResultSelectedCandidate =>
            RoundResultProperty("SelectedCandidate");
        public bool RoundResultSelectedCandidateIsNull =>
            RoundResultSelectedCandidate == null;
        public int RoundResultYakuCount =>
            collections.Count(RoundResultProperty("Yakus"));
        public int RoundResultTotalHan => (int)RoundResultProperty("TotalHan");
        public bool RoundResultHasYakuman => (bool)RoundResultProperty("HasYakuman");
        public int RoundResultYakumanCount => (int)RoundResultProperty("YakumanCount");

        public string SeatByPlayerIdName(string playerIdName)
        {
            return SeatByPlayerIdValue(playerIdName).ToString();
        }

        public object GetPlayerSeat(string seatName)
        {
            return dataFactory.GetPlayerSeat(State, seatName);
        }

        public object GetPlayerSeatByPlayerId(string playerIdName)
        {
            return reflection.Invoke(State, "GetPlayerSeat", SeatByPlayerIdValue(playerIdName));
        }

        public bool IsSelfSeat(string seatName)
        {
            return (bool)reflection.Invoke(State, "IsSelfSeat", dataFactory.ParseSeat(seatName));
        }

        public string SeatSlotWindAt(int index)
        {
            return reflection.GetProperty(SeatSlotAt(index), "Wind").ToString();
        }

        public string SeatSlotPlayerIdNameOrNullAt(int index)
        {
            return NullablePropertyString(SeatSlotAt(index), "PlayerId");
        }

        public bool SeatSlotHasPlayerAt(int index)
        {
            return (bool)reflection.GetProperty(SeatSlotAt(index), "HasPlayer");
        }

        public bool SeatSlotIsEmptyAt(int index)
        {
            return (bool)reflection.GetProperty(SeatSlotAt(index), "IsEmpty");
        }

        public string SeatSlotStateLabelAt(int index)
        {
            return reflection.GetProperty(SeatSlotAt(index), "StateLabel").ToString();
        }

        public string ParticipantTypeNameOrNull(string seatName)
        {
            return NullablePropertyString(
                reflection.Invoke(State, "GetSeatSlot", dataFactory.ParseSeat(seatName)),
                "ParticipantType");
        }

        public string ParticipantTypeNameOrNullForPlayerId(string playerIdName)
        {
            return NullablePropertyString(
                reflection.Invoke(State, "GetSeatSlot", SeatByPlayerIdValue(playerIdName)),
                "ParticipantType");
        }

        public bool HasDrawnTile(string seatName)
        {
            return (bool)reflection.GetProperty(GetPlayerSeat(seatName), "HasDrawnTile");
        }

        public int HandCount(string seatName)
        {
            return (int)reflection.GetProperty(
                reflection.GetProperty(GetPlayerSeat(seatName), "Hand"),
                "Count");
        }

        public int MeldCount(string seatName)
        {
            return collections.Count(reflection.GetProperty(GetPlayerSeat(seatName), "Melds"));
        }

        public bool IsClosed(string seatName)
        {
            return (bool)reflection.GetProperty(GetPlayerSeat(seatName), "IsClosed");
        }

        public bool IsTemporaryFuriten(string seatName)
        {
            return (bool)reflection.GetProperty(
                GetPlayerSeat(seatName),
                "IsTemporaryFuriten");
        }

        public object MeldAt(string seatName, int index)
        {
            return collections.Item(
                reflection.GetProperty(GetPlayerSeat(seatName), "Melds"),
                index);
        }

        public bool HasDrawnTileForPlayerId(string playerIdName)
        {
            return (bool)reflection.GetProperty(GetPlayerSeatByPlayerId(playerIdName), "HasDrawnTile");
        }

        public string DrawnTileCode(string seatName)
        {
            return reflection.GetProperty(GetPlayerSeat(seatName), "DrawnTile").ToString();
        }

        public string DrawnTileCodeOrNull(string seatName)
        {
            return NullablePropertyString(GetPlayerSeat(seatName), "DrawnTile");
        }

        public string DrawnTileCodeOrNullForPlayerId(string playerIdName)
        {
            return NullablePropertyString(GetPlayerSeatByPlayerId(playerIdName), "DrawnTile");
        }

        public int HandCountForPlayerId(string playerIdName)
        {
            return collections.Count(reflection.GetProperty(GetPlayerSeatByPlayerId(playerIdName), "Hand"));
        }

        public string HandDisplayString(string seatName)
        {
            return (string)reflection.Invoke(
                reflection.GetProperty(GetPlayerSeat(seatName), "Hand"),
                "ToDisplayString");
        }

        public string HandDisplayStringForPlayerId(string playerIdName)
        {
            return (string)reflection.Invoke(
                reflection.GetProperty(GetPlayerSeatByPlayerId(playerIdName), "Hand"),
                "ToDisplayString");
        }

        public object DiscardAt(int index)
        {
            return collections.Item(GetStateProperty("Discards"), index);
        }

        public object LastDiscard()
        {
            return collections.Last(GetStateProperty("Discards"));
        }

        public string DiscardActorSeatNameAt(int index)
        {
            return reflection.GetProperty(DiscardAt(index), "ActorSeat").ToString();
        }

        public string DiscardTileCodeAt(int index)
        {
            return reflection.GetProperty(DiscardAt(index), "Tile").ToString();
        }

        public string DiscardSourceNameAt(int index)
        {
            return reflection.GetProperty(DiscardAt(index), "Source").ToString();
        }

        public string LastDiscardActorSeatName =>
            reflection.GetProperty(LastDiscard(), "ActorSeat").ToString();

        public string LastDiscardTileCode =>
            reflection.GetProperty(LastDiscard(), "Tile").ToString();

        public string LastDiscardSourceName =>
            reflection.GetProperty(LastDiscard(), "Source").ToString();
        public bool LastDiscardIsLastLiveWallDiscard =>
            (bool)reflection.GetProperty(LastDiscard(), "IsLastLiveWallDiscard");
        public int LastDiscardId => (int)reflection.GetProperty(LastDiscard(), "Id");

        public bool TryGetDiscardClaim(int discardId, out object discardClaim)
        {
            object[] arguments = { discardId, null };
            bool found = (bool)reflection.Invoke(State, "TryGetDiscardClaim", arguments);
            discardClaim = arguments[1];
            return found;
        }

        public object ActiveSkillEffectAt(int index)
        {
            return collections.Item(GetStateProperty("ActiveSkillEffects"), index);
        }

        public string ActiveSkillEffectOwnerSeatNameAt(int index)
        {
            return reflection.GetProperty(ActiveSkillEffectAt(index), "OwnerSeat").ToString();
        }

        private object State => stateProvider();

        private object CurrentTurn => GetStateProperty("CurrentTurn");

        private object CurrentPlayerSeat =>
            reflection.Invoke(State, "GetPlayerSeat", CurrentTurn);

        private object WindProgress => GetStateProperty("WindProgress");

        private object LastTurnDraw()
        {
            return GetStateProperty("LastTurnDraw");
        }

        private object RoundResultProperty(string propertyName)
        {
            return reflection.GetProperty(CurrentRoundResult, propertyName);
        }

        private object GetStateProperty(string propertyName)
        {
            return reflection.GetProperty(State, propertyName);
        }

        private object SeatByPlayerIdValue(string playerIdName)
        {
            return reflection.Invoke(
                State,
                "GetSeatByPlayerId",
                dataFactory.ParsePlayerId(playerIdName));
        }

        private object SeatSlotAt(int index)
        {
            return collections.Item(GetStateProperty("SeatSlots"), index);
        }

        private string[] SeatNames(object seats)
        {
            int count = collections.Count(seats);
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
                names[i] = collections.Item(seats, i).ToString();

            return names;
        }

        private string NullablePropertyString(object target, string propertyName)
        {
            object value = reflection.GetProperty(target, propertyName);
            return value == null ? null : value.ToString();
        }
    }
}
