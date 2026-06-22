using System;
using System.Collections.Generic;
using MahjongPrototype.Skills;

namespace MahjongPrototype.Domain
{
    public sealed class MahjongGameState
    {
        private readonly Dictionary<SeatId, PlayerSeat> playerSeats = new Dictionary<SeatId, PlayerSeat>();
        private readonly List<SeatId> activeSeats = new List<SeatId>();
        private readonly List<SeatSlot> seatSlots = new List<SeatSlot>();
        private readonly List<DiscardRecord> discards = new List<DiscardRecord>();
        private readonly List<ActiveSkillEffect> activeSkillEffects = new List<ActiveSkillEffect>();

        public MahjongGameState(Wall wall, IEnumerable<SeatId> initialActiveSeats)
        {
            Wall = wall ?? throw new ArgumentNullException(nameof(wall));

            if (initialActiveSeats == null)
                throw new ArgumentNullException(nameof(initialActiveSeats));

            foreach (SeatId seat in initialActiveSeats)
            {
                if (activeSeats.Contains(seat))
                    continue;

                activeSeats.Add(seat);
                playerSeats[seat] = new PlayerSeat(seat);
            }

            if (activeSeats.Count <= 0)
            {
                activeSeats.Add(SeatId.East);
                playerSeats[SeatId.East] = new PlayerSeat(SeatId.East);
            }

            InitializeSeatSlots();
            SetSelfWind(SeatId.East);
            CurrentSeat = activeSeats[0];
            TurnIndex = 1;
        }

        public Wall Wall { get; }
        public SeatId SelfWind { get; private set; } = SeatId.East;
        public SeatId CurrentSeat { get; set; }
        public int TurnIndex { get; set; }
        public bool IsRoundEnded { get; set; }
        public IReadOnlyList<SeatId> ActiveSeats => activeSeats;
        public IReadOnlyList<SeatSlot> SeatSlots => seatSlots;
        public IReadOnlyList<DiscardRecord> Discards => discards;
        public IReadOnlyList<ActiveSkillEffect> ActiveSkillEffects => activeSkillEffects;

        public void SetSelfWind(SeatId selfWind)
        {
            SelfWind = selfWind;
            AssignSelfSeat(selfWind);
        }

        public SeatSlot GetSeatSlot(SeatId wind)
        {
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (slot.Wind == wind)
                    return slot;
            }

            throw new ArgumentOutOfRangeException(nameof(wind), wind, "Seat slot is not initialized.");
        }

        public PlayerSeat GetPlayerSeat(SeatId seatId)
        {
            if (!playerSeats.TryGetValue(seatId, out PlayerSeat playerSeat))
            {
                playerSeat = new PlayerSeat(seatId);
                playerSeats[seatId] = playerSeat;
            }

            return playerSeat;
        }

        public void AddDiscard(DiscardRecord record)
        {
            discards.Add(record);
        }

        public void AddActiveSkillEffect(ActiveSkillEffect effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            activeSkillEffects.Add(effect);
        }

        public bool HasActiveSkillEffect(SeatId ownerSeat, SkillEffectKind kind)
        {
            for (int i = 0; i < activeSkillEffects.Count; i++)
            {
                ActiveSkillEffect effect = activeSkillEffects[i];
                if (effect.OwnerSeat == ownerSeat && effect.Kind == kind)
                    return true;
            }

            return false;
        }

        public ActiveSkillEffect FindNextDrawEffect(SeatId ownerSeat)
        {
            for (int i = 0; i < activeSkillEffects.Count; i++)
            {
                ActiveSkillEffect effect = activeSkillEffects[i];
                if (effect.OwnerSeat == ownerSeat &&
                    effect.Kind == SkillEffectKind.ForceDrawTile &&
                    effect.Duration == SkillEffectDuration.NextDraw)
                {
                    return effect;
                }
            }

            return null;
        }

        public bool RemoveActiveSkillEffect(ActiveSkillEffect effect)
        {
            return effect != null && activeSkillEffects.Remove(effect);
        }

        private void InitializeSeatSlots()
        {
            seatSlots.Clear();
            seatSlots.Add(new SeatSlot(SeatId.East));
            seatSlots.Add(new SeatSlot(SeatId.South));
            seatSlots.Add(new SeatSlot(SeatId.West));
            seatSlots.Add(new SeatSlot(SeatId.North));
        }

        private void AssignSelfSeat(SeatId selfWind)
        {
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (slot.Wind == selfWind)
                    slot.AssignSelf();
                else
                    slot.Clear();
            }
        }
    }

    public sealed class SeatSlot
    {
        public SeatSlot(SeatId wind)
        {
            Wind = wind;
            Clear();
        }

        public SeatId Wind { get; }
        public bool HasSelfPlayer { get; private set; }
        public bool IsEmpty => !HasSelfPlayer;
        public string StateLabel => HasSelfPlayer ? "Self" : "Empty";

        public void AssignSelf()
        {
            HasSelfPlayer = true;
        }

        public void Clear()
        {
            HasSelfPlayer = false;
        }
    }
}
