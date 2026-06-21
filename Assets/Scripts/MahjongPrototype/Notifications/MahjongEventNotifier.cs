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
        public event Action<string> RoundEnded;
        public event Action AnyEventNotified;

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

        public void NotifyRoundEnded(string reason)
        {
            RoundEnded?.Invoke(reason);
            NotifyAny();
        }

        private void NotifyAny()
        {
            AnyEventNotified?.Invoke();
        }
    }
}
