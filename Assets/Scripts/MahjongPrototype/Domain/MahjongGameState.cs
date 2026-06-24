using System;
using System.Collections.Generic;
using MahjongPrototype.Skills;
using TurnPhaseType = MahjongPrototype.Domain.TurnPhase;

namespace MahjongPrototype.Domain
{
    public enum PlayerId
    {
        Player1 = 1,
        Player2 = 2,
        Player3 = 3,
        Player4 = 4
    }

    public sealed class MahjongGameState
    {
        private readonly Dictionary<SeatId, PlayerSeat> playerSeats = new Dictionary<SeatId, PlayerSeat>();
        private readonly List<SeatId> activeSeats = new List<SeatId>();
        private readonly List<SeatSlot> seatSlots = new List<SeatSlot>();
        private readonly List<DiscardRecord> discards = new List<DiscardRecord>();
        private readonly List<ActiveSkillEffect> activeSkillEffects = new List<ActiveSkillEffect>();

        public MahjongGameState(Wall wall)
        {
            Wall = wall ?? throw new ArgumentNullException(nameof(wall));

            InitializeSeatSlots();
            SetSelfSeat(SeatId.East);
            RebuildActiveTurnSeatsFromSeatSlots();
            TurnIndex = 1;
        }

        public Wall Wall { get; }
        public PlayerId SelfPlayerId { get; } = PlayerId.Player1;
        public SeatId SelfSeat => GetSelfSeatSlot().Wind;
        public SeatId SelfWind => SelfSeat;
        public SeatId CurrentTurn { get; set; }
        public SeatSlot CurrentTurnSlot => GetSeatSlot(CurrentTurn);
        public PlayerId? CurrentTurnPlayerId => CurrentTurnSlot.PlayerId;
        public bool IsSelfTurn => CurrentTurnPlayerId == SelfPlayerId;
        public int TurnIndex { get; set; }
        public bool IsRoundEnded { get; set; }
        public bool IsWinDecisionPending { get; private set; }
        public SeatId WinDecisionSeat { get; private set; }
        public int WinDecisionTurnIndex { get; private set; }
        public TurnPhaseType TurnPhase =>
            IsRoundEnded
                ? TurnPhaseType.RoundEnded
                : IsWinDecisionPending
                    ? TurnPhaseType.WinDecision
                    : GetPlayerSeat(CurrentTurn).HasDrawnTile
                        ? TurnPhaseType.WaitingForDiscard
                        : TurnPhaseType.WaitingForDraw;
        public bool IsInteractionLocked =>
            TurnPhase == TurnPhaseType.WinDecision ||
            TurnPhase == TurnPhaseType.RoundEnded;
        public IReadOnlyList<SeatId> ActiveSeats => activeSeats;
        public IReadOnlyList<SeatId> ActiveTurnSeats => activeSeats;
        public IReadOnlyList<SeatId> OccupiedSeats => GetOccupiedSeats();
        public IReadOnlyList<SeatSlot> SeatSlots => seatSlots;
        public IReadOnlyList<DiscardRecord> Discards => discards;
        public IReadOnlyList<ActiveSkillEffect> ActiveSkillEffects => activeSkillEffects;

        public void SetSelfWind(SeatId selfWind)
        {
            SetSelfSeat(selfWind);
        }

        public void SetSelfSeat(SeatId selfSeat)
        {
            AssignPlayerToSeat(SelfPlayerId, selfSeat);
        }

        public void RebuildActiveTurnSeatsFromSeatSlots()
        {
            activeSeats.Clear();
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (!slot.HasPlayer)
                    continue;

                activeSeats.Add(slot.Wind);
                GetPlayerSeat(slot.Wind);
            }

            if (activeSeats.Count <= 0)
                throw new InvalidOperationException("Cannot rebuild active turn seats because no seat slots have players.");

            if (!ContainsActiveTurnSeat(CurrentTurn))
                CurrentTurn = activeSeats[0];
        }

        public void AssignPlayerToSeat(PlayerId playerId, SeatId seat)
        {
            ClearPlayerFromSeatSlots(playerId);
            GetSeatSlot(seat).AssignPlayer(playerId);
        }

        public SeatSlot GetSelfSeatSlot()
        {
            return GetSeatSlot(GetSeatByPlayerId(SelfPlayerId));
        }

        public IReadOnlyList<SeatId> GetOccupiedSeats()
        {
            List<SeatId> occupiedSeats = new List<SeatId>();
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (slot.HasPlayer)
                    occupiedSeats.Add(slot.Wind);
            }

            return occupiedSeats;
        }

        public bool IsSelfSeat(SeatId seat)
        {
            return GetSeatSlot(seat).PlayerId == SelfPlayerId;
        }

        public SeatId GetSeatByPlayerId(PlayerId playerId)
        {
            SeatSlot playerSlot = null;
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (slot.PlayerId != playerId)
                    continue;

                if (playerSlot != null)
                    throw new InvalidOperationException($"Player {playerId} is assigned to multiple seat slots.");

                playerSlot = slot;
            }

            if (playerSlot == null)
                throw new InvalidOperationException($"Player {playerId} is not assigned to a seat slot.");

            return playerSlot.Wind;
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

        public void BeginWinDecision(SeatId seat, int turnIndex)
        {
            IsWinDecisionPending = true;
            WinDecisionSeat = seat;
            WinDecisionTurnIndex = turnIndex;
        }

        public void ClearWinDecision()
        {
            IsWinDecisionPending = false;
            WinDecisionSeat = default;
            WinDecisionTurnIndex = 0;
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

        private bool ContainsActiveTurnSeat(SeatId seat)
        {
            for (int i = 0; i < activeSeats.Count; i++)
            {
                if (activeSeats[i] == seat)
                    return true;
            }

            return false;
        }

        private void InitializeSeatSlots()
        {
            seatSlots.Clear();
            seatSlots.Add(new SeatSlot(SeatId.East));
            seatSlots.Add(new SeatSlot(SeatId.South));
            seatSlots.Add(new SeatSlot(SeatId.West));
            seatSlots.Add(new SeatSlot(SeatId.North));
        }

        private void ClearPlayerFromSeatSlots(PlayerId playerId)
        {
            for (int i = 0; i < seatSlots.Count; i++)
            {
                SeatSlot slot = seatSlots[i];
                if (slot.PlayerId == playerId)
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
        public PlayerId? PlayerId { get; private set; }
        public bool HasPlayer => PlayerId.HasValue;
        public bool IsEmpty => !PlayerId.HasValue;
        public string StateLabel => PlayerId.HasValue ? PlayerId.Value.ToString() : "Empty";

        internal void AssignPlayer(PlayerId playerId)
        {
            PlayerId = playerId;
        }

        internal void Clear()
        {
            PlayerId = null;
        }
    }
}
