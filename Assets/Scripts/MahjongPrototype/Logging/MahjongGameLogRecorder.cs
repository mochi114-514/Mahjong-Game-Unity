using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;

namespace MahjongPrototype.Logging
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Logging/Mahjong Game Log Recorder")]
    public sealed class MahjongGameLogRecorder : MonoBehaviour
    {
        [SerializeField] private MahjongGameFlow gameFlow;
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Dev Log")]
        [SerializeField] private bool enableDevLog = true;
        [SerializeField] private bool enableReleaseBuildLogging = false;
        [SerializeField] private bool enableTurnDebugLog;

        private bool isSubscribed;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (isSubscribed || eventNotifier == null)
                return;

            eventNotifier.RunStarted += HandleRunStarted;
            eventNotifier.RoundStarted += HandleRoundStarted;
            eventNotifier.TurnStarted += HandleTurnStarted;
            eventNotifier.TileDrawn += HandleTileDrawn;
            eventNotifier.TileDiscarded += HandleTileDiscarded;
            eventNotifier.RoundEnded += HandleRoundEnded;
            eventNotifier.SeatSlotsAssigned += HandleSeatSlotsAssigned;
            eventNotifier.TurnDebug += HandleTurnDebug;
            eventNotifier.WinCheckedDetailed += HandleWinCheckedDetailed;
            eventNotifier.WinDeclaredDetailed += HandleWinDeclaredDetailed;
            eventNotifier.WinDeclinedDetailed += HandleWinDeclinedDetailed;
            eventNotifier.SkillActivatedDetailed += HandleSkillActivatedDetailed;
            eventNotifier.SkillEffectRegistered += HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved += HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired += HandleSkillEffectExpired;
            eventNotifier.SkillReserved += HandleSkillReserved;
            eventNotifier.SkillReservationConsumed += HandleSkillReservationConsumed;
            eventNotifier.SkillReservationRejected += HandleSkillReservationRejected;
            eventNotifier.AutoSortChanged += HandleAutoSortChanged;
            eventNotifier.HandAutoSortedDetailed += HandleHandAutoSortedDetailed;
            isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!isSubscribed || eventNotifier == null)
                return;

            eventNotifier.RunStarted -= HandleRunStarted;
            eventNotifier.RoundStarted -= HandleRoundStarted;
            eventNotifier.TurnStarted -= HandleTurnStarted;
            eventNotifier.TileDrawn -= HandleTileDrawn;
            eventNotifier.TileDiscarded -= HandleTileDiscarded;
            eventNotifier.RoundEnded -= HandleRoundEnded;
            eventNotifier.SeatSlotsAssigned -= HandleSeatSlotsAssigned;
            eventNotifier.TurnDebug -= HandleTurnDebug;
            eventNotifier.WinCheckedDetailed -= HandleWinCheckedDetailed;
            eventNotifier.WinDeclaredDetailed -= HandleWinDeclaredDetailed;
            eventNotifier.WinDeclinedDetailed -= HandleWinDeclinedDetailed;
            eventNotifier.SkillActivatedDetailed -= HandleSkillActivatedDetailed;
            eventNotifier.SkillEffectRegistered -= HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved -= HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired -= HandleSkillEffectExpired;
            eventNotifier.SkillReserved -= HandleSkillReserved;
            eventNotifier.SkillReservationConsumed -= HandleSkillReservationConsumed;
            eventNotifier.SkillReservationRejected -= HandleSkillReservationRejected;
            eventNotifier.AutoSortChanged -= HandleAutoSortChanged;
            eventNotifier.HandAutoSortedDetailed -= HandleHandAutoSortedDetailed;
            isSubscribed = false;
        }

        private void HandleRunStarted(string logFilePath)
        {
            DevLog.Initialize(enableDevLog, enableReleaseBuildLogging);
            DevLog.Record(
                "GameFlow",
                "RunStarted",
                $"LogFile={DevLog.CurrentLogFilePath}");
        }

        private void HandleRoundStarted(int turnIndex, int wallCount)
        {
            MahjongGameState state = GetCurrentState();
            DevLog.Record(
                "GameFlow",
                "RoundStarted",
                "Round started.",
                seat: state != null ? state.CurrentTurn : (SeatId?)null,
                wallCount: wallCount,
                turnIndex: turnIndex);
        }

        private void HandleTurnStarted(SeatId seat, int turnIndex)
        {
            DevLog.Record(
                "GameFlow",
                "TurnStarted",
                "Turn started.",
                seat: seat,
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleTileDrawn(DrawResult result)
        {
            DevLog.Record(
                "Mahjong",
                "TileDrawn",
                $"source={result.Source}; purpose={result.Purpose}; {result.Message}",
                seat: result.Seat,
                tile: result.Tile,
                hand: GetHandText(result.Seat),
                wallCount: result.WallCountAfterDraw,
                turnIndex: GetTurnIndex(),
                activeSkill: result.ResolvedSkillEffect != null ? result.ResolvedSkillEffect.ToLogText() : null);
        }

        private void HandleTileDiscarded(DiscardRecord record)
        {
            DevLog.Record(
                "Mahjong",
                "TileDiscarded",
                "Tile discarded.",
                seat: record.ActorSeat,
                tile: record.Tile,
                hand: GetHandText(record.ActorSeat),
                wallCount: GetWallCount(),
                turnIndex: record.TurnIndex);
        }

        private void HandleRoundEnded(string reason)
        {
            MahjongGameState state = GetCurrentState();
            DevLog.Record(
                "GameFlow",
                "RoundEnded",
                reason,
                seat: state != null ? state.CurrentTurn : (SeatId?)null,
                wallCount: state != null ? state.Wall.Count : (int?)null,
                turnIndex: state != null ? state.TurnIndex : (int?)null);
        }

        private void HandleSeatSlotsAssigned()
        {
            MahjongGameState state = GetCurrentState();
            if (state == null)
                return;

            for (int i = 0; i < state.SeatSlots.Count; i++)
            {
                SeatSlot slot = state.SeatSlots[i];
                DevLog.Record(
                    "GameFlow",
                    "SeatSlotAssigned",
                    $"Seat {slot.Wind} = {GetSeatSlotLogLabel(state, slot)}",
                    seat: slot.Wind,
                    turnIndex: state.TurnIndex);
            }
        }

        private void HandleTurnDebug(
            string eventName,
            string message,
            SeatId? seat,
            Tile? tile,
            int? turnIndex)
        {
            if (!enableTurnDebugLog)
                return;

            DevLog.Record(
                "Turn",
                eventName,
                message,
                seat: seat,
                tile: tile,
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleWinCheckedDetailed(
            SeatId seat,
            WinType winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            bool isWin)
        {
            DevLog.Record(
                "Mahjong",
                "WinChecked",
                GetWinCheckedMessage(winType, sourceSeat, isWin),
                seat: seat,
                tile: winningTile,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleWinDeclaredDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            DevLog.Record(
                "Mahjong",
                "WinDeclared",
                $"winType={winType}; win declared.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleWinDeclinedDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            DevLog.Record(
                "Mahjong",
                "WinDeclined",
                $"winType={winType}; winning hand declined.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleSkillActivatedDetailed(
            SeatId actorSeat,
            ActiveSkillEffect effect,
            bool beforeDraw)
        {
            if (beforeDraw)
            {
                DevLog.Record(
                    "Skill",
                    "SkillActivatedBeforeDraw",
                    $"skillType={effect.Kind}; currentTurnSeat={GetCurrentTurnText()}",
                    seat: actorSeat,
                    tile: effect.TargetTile,
                    hand: GetHandText(actorSeat),
                    wallCount: GetWallCount(),
                    turnIndex: GetTurnIndex(),
                    activeSkill: effect.ToLogText());
                return;
            }

            DevLog.Record(
                "Skill",
                "SkillActivated",
                "Force draw skill activated.",
                seat: actorSeat,
                tile: effect.TargetTile,
                hand: GetHandText(actorSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillEffectRegistered(ActiveSkillEffect effect)
        {
            DevLog.Record(
                "Skill",
                "SkillEffectRegistered",
                "ActiveSkillEffect registered.",
                seat: effect.OwnerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(effect.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillEffectResolved(DrawResult result)
        {
            ActiveSkillEffect effect = result.ResolvedSkillEffect;
            DevLog.Record(
                "Skill",
                "SkillEffectResolved",
                result.SkillApplied ? "Target tile was drawn." : result.Message,
                seat: result.Seat,
                tile: effect != null ? effect.TargetTile : result.Tile,
                hand: result.Success ? GetHandText(result.Seat) : null,
                wallCount: result.WallCountAfterDraw,
                turnIndex: GetTurnIndex(),
                activeSkill: effect != null ? effect.ToLogText() : null);

            if (effect == null)
                return;

            DevLog.Record(
                "Skill",
                "DrawModifiedBySkill",
                result.SkillApplied
                    ? "Force draw applied."
                    : "Target tile missing. Fell back to normal draw.",
                seat: result.Seat,
                tile: result.Success ? result.Tile : effect.TargetTile,
                hand: result.Success ? GetHandText(result.Seat) : null,
                wallCount: result.WallCountAfterDraw,
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillEffectExpired(ActiveSkillEffect effect, string reason)
        {
            DevLog.Record(
                "Skill",
                "SkillEffectExpired",
                reason,
                seat: effect.OwnerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(effect.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillReserved(PendingSkillReservation reservation)
        {
            DevLog.Record(
                "Skill",
                "SkillReserved",
                $"skillType={reservation.SkillEffectKind}; reservedOnTurnSeat={reservation.ReservedOnTurnSeat}; reservedTurnIndex={reservation.ReservedTurnIndex}",
                seat: reservation.OwnerSeat,
                tile: reservation.TargetTile,
                hand: GetHandText(reservation.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: reservation.ToLogText());
        }

        private void HandleSkillReservationConsumed(PendingSkillReservation reservation)
        {
            DevLog.Record(
                "Skill",
                "ReservationConsumed",
                $"skillType={reservation.SkillEffectKind}",
                seat: reservation.OwnerSeat,
                tile: reservation.TargetTile,
                hand: GetHandText(reservation.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: reservation.ToLogText());
        }

        private void HandleSkillReservationRejected(
            SeatId ownerSeat,
            SkillEffectKind skillEffectKind,
            Tile targetTile,
            string reason)
        {
            DevLog.Record(
                "Skill",
                "SkillReservationRejected",
                $"skillType={skillEffectKind}; reason={reason}; currentTurnSeat={GetCurrentTurnText()}",
                seat: ownerSeat,
                tile: targetTile,
                hand: GetHandText(ownerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: $"{skillEffectKind}:{targetTile}:ReservationRejected");
        }

        private void HandleAutoSortChanged(bool enabled)
        {
            MahjongGameState state = GetCurrentState();
            if (state == null)
            {
                DevLog.Record(
                    "Mahjong",
                    enabled ? "AutoSortEnabled" : "AutoSortDisabled",
                    enabled ? "Auto sort enabled." : "Auto sort disabled.");
                return;
            }

            DevLog.Record(
                "Mahjong",
                enabled ? "AutoSortEnabled" : "AutoSortDisabled",
                enabled ? "Auto sort enabled." : "Auto sort disabled.",
                seat: state.SelfSeat,
                hand: GetHandText(state.SelfSeat),
                wallCount: state.Wall.Count,
                turnIndex: state.TurnIndex);
        }

        private void HandleHandAutoSortedDetailed(SeatId seat, int turnIndex, string reason)
        {
            DevLog.Record(
                "Mahjong",
                "HandAutoSorted",
                $"reason={reason}; hand sorted by TypeIndex.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private string GetSeatSlotLogLabel(MahjongGameState state, SeatSlot slot)
        {
            if (slot == null || slot.IsEmpty)
                return "Empty";

            return state.IsSelfSeat(slot.Wind)
                ? $"Self:{slot.ParticipantType}"
                : $"{slot.StateLabel}:{slot.ParticipantType}";
        }

        private static string GetWinCheckedMessage(WinType winType, SeatId? sourceSeat, bool isWin)
        {
            if (winType == WinType.Tsumo)
            {
                return isWin
                    ? "winType=Tsumo; isWin=true; standard hand shape complete."
                    : "winType=Tsumo; isWin=false; standard hand shape incomplete.";
            }

            return $"winType={winType}; sourceSeat={sourceSeat}; isWin={isWin}";
        }

        private string GetCurrentTurnText()
        {
            MahjongGameState state = GetCurrentState();
            return state != null ? state.CurrentTurn.ToString() : string.Empty;
        }

        private void CacheReferences()
        {
            if (gameFlow == null)
                gameFlow = GetComponent<MahjongGameFlow>();

            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (gameFlow == null && transform.root != null)
                gameFlow = transform.root.GetComponentInChildren<MahjongGameFlow>(true);

            if (eventNotifier == null && gameFlow != null)
                eventNotifier = gameFlow.EventNotifier;

            if (eventNotifier == null)
                eventNotifier = GetComponent<MahjongEventNotifier>();

            if (eventNotifier == null)
                eventNotifier = GetComponentInParent<MahjongEventNotifier>();

            if (eventNotifier == null && transform.root != null)
                eventNotifier = transform.root.GetComponentInChildren<MahjongEventNotifier>(true);
        }

        private MahjongGameState GetCurrentState()
        {
            return gameFlow != null ? gameFlow.CurrentState : null;
        }

        private int? GetWallCount()
        {
            MahjongGameState state = GetCurrentState();
            return state != null ? state.Wall.Count : (int?)null;
        }

        private int? GetTurnIndex()
        {
            MahjongGameState state = GetCurrentState();
            return state != null ? state.TurnIndex : (int?)null;
        }

        private string GetHandText(SeatId seat)
        {
            MahjongGameState state = GetCurrentState();
            return state == null ? string.Empty : state.GetPlayerSeat(seat).Hand.ToDisplayString();
        }
    }
}
