using System;
using System.Collections.Generic;
using MahjongPrototype.Skills;

namespace MahjongPrototype.Domain
{
    public sealed class MahjongGameState
    {
        private readonly Dictionary<SeatId, PlayerSeat> playerSeats = new Dictionary<SeatId, PlayerSeat>();
        private readonly List<SeatId> activeSeats = new List<SeatId>();
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

            CurrentSeat = activeSeats[0];
            TurnIndex = 1;
        }

        public Wall Wall { get; }
        public SeatId CurrentSeat { get; set; }
        public int TurnIndex { get; set; }
        public bool IsRoundEnded { get; set; }
        public IReadOnlyList<SeatId> ActiveSeats => activeSeats;
        public IReadOnlyList<DiscardRecord> Discards => discards;
        public IReadOnlyList<ActiveSkillEffect> ActiveSkillEffects => activeSkillEffects;

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
    }
}
