using System;
using MahjongPrototype.Domain;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;

namespace MahjongPrototype.Notifications
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Mahjong Event Notifier")]
    public sealed class MahjongEventNotifier : MonoBehaviour
    {
        public event Action<string> RunStarted;
        public event Action<int, int> RoundStarted;
        public event Action RoundSetupCompleted;
        public event Action<SeatId, int> TurnStarted;
        public event Action<DrawResult> TileDrawn;
        public event Action<DiscardRecord> TileDiscarded;
        public event Action<SeatId, ActiveSkillEffect> SkillActivated;
        public event Action<ActiveSkillEffect> SkillEffectRegistered;
        public event Action<DrawResult> SkillEffectResolved;
        public event Action<ActiveSkillEffect, string> SkillEffectExpired;
        public event Action<SeatId, int, bool> WinChecked;
        public event Action<SeatId, int> WinDeclared;
        public event Action<SeatId, int> WinDeclined;
        public event Action<SeatId, int> HandAutoSorted;
        public event Action<string> RoundEnded;
        public event Action SeatSlotsAssigned;
        public event Action<string, string, SeatId?, Tile?, int?> TurnDebug;
        public event Action<SeatId, WinType, Tile?, SeatId?, int, bool> WinCheckedDetailed;
        public event Action<SeatId, WinType?, int> WinDeclaredDetailed;
        public event Action<SeatId, WinType?, int> WinDeclinedDetailed;
        public event Action<SeatId, ActiveSkillEffect, bool> SkillActivatedDetailed;
        public event Action<PendingSkillReservation> SkillReserved;
        public event Action<PendingSkillReservation> SkillReservationConsumed;
        public event Action<SeatId, SkillEffectKind, Tile, string> SkillReservationRejected;
        public event Action<bool> AutoSortChanged;
        public event Action<SeatId, int, string> HandAutoSortedDetailed;
        public event Action AnyEventNotified;

        public void NotifyRunStarted()
        {
            RunStarted?.Invoke(string.Empty);
            NotifyAny();
        }

        public void NotifyRunStarted(string logFilePath)
        {
            RunStarted?.Invoke(logFilePath);
            NotifyAny();
        }

        public void NotifyRoundStarted(int turnIndex, int wallCount)
        {
            RoundStarted?.Invoke(turnIndex, wallCount);
            NotifyAny();
        }

        public void NotifyRoundSetupCompleted()
        {
            RoundSetupCompleted?.Invoke();
            NotifyAny();
        }

        public void NotifyTurnStarted(SeatId seat, int turnIndex)
        {
            TurnStarted?.Invoke(seat, turnIndex);
            NotifyAny();
        }

        public void NotifyTileDrawn(DrawResult drawResult)
        {
            TileDrawn?.Invoke(drawResult);
            NotifyAny();
        }

        public void NotifyTileDiscarded(DiscardRecord discardRecord)
        {
            TileDiscarded?.Invoke(discardRecord);
            NotifyAny();
        }

        public void NotifySkillActivated(SeatId actorSeat, ActiveSkillEffect effect)
        {
            SkillActivated?.Invoke(actorSeat, effect);
            NotifyAny();
        }

        public void NotifySkillEffectRegistered(ActiveSkillEffect effect)
        {
            SkillEffectRegistered?.Invoke(effect);
            NotifyAny();
        }

        public void NotifySkillEffectResolved(DrawResult drawResult)
        {
            SkillEffectResolved?.Invoke(drawResult);
            NotifyAny();
        }

        public void NotifySkillEffectExpired(ActiveSkillEffect effect, string reason)
        {
            SkillEffectExpired?.Invoke(effect, reason);
            NotifyAny();
        }

        public void NotifyWinChecked(SeatId seat, int turnIndex, bool isWin)
        {
            WinChecked?.Invoke(seat, turnIndex, isWin);
            NotifyAny();
        }

        public void NotifyWinDeclared(SeatId seat, int turnIndex)
        {
            WinDeclared?.Invoke(seat, turnIndex);
            NotifyAny();
        }

        public void NotifyWinDeclined(SeatId seat, int turnIndex)
        {
            WinDeclined?.Invoke(seat, turnIndex);
            NotifyAny();
        }

        public void NotifyHandAutoSorted(SeatId seat, int turnIndex)
        {
            HandAutoSorted?.Invoke(seat, turnIndex);
            NotifyAny();
        }

        public void NotifyRoundEnded(string reason)
        {
            RoundEnded?.Invoke(reason);
            NotifyAny();
        }

        public void NotifySeatSlotsAssigned()
        {
            SeatSlotsAssigned?.Invoke();
            NotifyAny();
        }

        public void NotifyTurnDebug(
            string eventName,
            string message,
            SeatId? seat = null,
            Tile? tile = null,
            int? turnIndex = null)
        {
            TurnDebug?.Invoke(eventName, message, seat, tile, turnIndex);
            NotifyAny();
        }

        public void NotifyWinCheckedDetailed(
            SeatId seat,
            WinType winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            bool isWin)
        {
            WinCheckedDetailed?.Invoke(seat, winType, winningTile, sourceSeat, turnIndex, isWin);
            NotifyAny();
        }

        public void NotifyWinDeclaredDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            WinDeclaredDetailed?.Invoke(seat, winType, turnIndex);
            NotifyAny();
        }

        public void NotifyWinDeclinedDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            WinDeclinedDetailed?.Invoke(seat, winType, turnIndex);
            NotifyAny();
        }

        public void NotifySkillActivatedDetailed(
            SeatId actorSeat,
            ActiveSkillEffect effect,
            bool beforeDraw)
        {
            SkillActivatedDetailed?.Invoke(actorSeat, effect, beforeDraw);
            NotifyAny();
        }

        public void NotifySkillReserved(PendingSkillReservation reservation)
        {
            SkillReserved?.Invoke(reservation);
            NotifyAny();
        }

        public void NotifySkillReservationConsumed(PendingSkillReservation reservation)
        {
            SkillReservationConsumed?.Invoke(reservation);
            NotifyAny();
        }

        public void NotifySkillReservationRejected(
            SeatId ownerSeat,
            SkillEffectKind skillEffectKind,
            Tile targetTile,
            string reason)
        {
            SkillReservationRejected?.Invoke(ownerSeat, skillEffectKind, targetTile, reason);
            NotifyAny();
        }

        public void NotifyAutoSortChanged(bool enabled)
        {
            AutoSortChanged?.Invoke(enabled);
            NotifyAny();
        }

        public void NotifyHandAutoSortedDetailed(SeatId seat, int turnIndex, string reason)
        {
            HandAutoSortedDetailed?.Invoke(seat, turnIndex, reason);
            NotifyAny();
        }

        private void NotifyAny()
        {
            AnyEventNotified?.Invoke();
        }
    }
}
