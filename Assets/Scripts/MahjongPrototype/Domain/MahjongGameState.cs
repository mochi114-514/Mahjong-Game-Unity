using System;
using System.Collections.Generic;
using MahjongPrototype.Services;
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
        private readonly List<ReachDiscardCandidate> reachDiscardCandidates =
            new List<ReachDiscardCandidate>();

        public MahjongGameState(Wall wall)
            : this(wall, WindProgress.East1)
        {
        }

        public MahjongGameState(Wall wall, WindProgress windProgress)
        {
            Wall = wall ?? throw new ArgumentNullException(nameof(wall));
            WindProgress = windProgress;

            InitializeSeatSlots();
            SetSelfSeat(SeatId.East);
            RebuildActiveTurnSeatsFromSeatSlots();
            TurnIndex = 1;
        }

        public Wall Wall { get; }
        public WindProgress WindProgress { get; }
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
        public WinType? WinDecisionType { get; private set; }
        public Tile? WinningTile { get; private set; }
        public SeatId? WinSourceSeat { get; private set; }
        public int WinDecisionTurnIndex { get; private set; }
        public WinDeclarationEvaluationResult PendingWinDeclarationEvaluation { get; private set; }
        public bool IsReachDecisionPending { get; private set; }
        public bool IsReachDiscardSelectionPending { get; private set; }
        public SeatId ReachDecisionSeat { get; private set; }
        public int ReachDecisionTurnIndex { get; private set; }
        public TurnPhaseType TurnPhase =>
            IsRoundEnded
                ? TurnPhaseType.RoundEnded
                : IsWinDecisionPending
                    ? TurnPhaseType.WinDecision
                    : IsReachDecisionPending
                        ? TurnPhaseType.ReachDecision
                        : IsReachDiscardSelectionPending
                            ? TurnPhaseType.ReachDiscardSelection
                            : GetPlayerSeat(CurrentTurn).HasDrawnTile
                                ? TurnPhaseType.WaitingForDiscard
                                : TurnPhaseType.WaitingForDraw;
        public bool IsInteractionLocked =>
            TurnPhase == TurnPhaseType.WinDecision ||
            TurnPhase == TurnPhaseType.ReachDecision ||
            TurnPhase == TurnPhaseType.RoundEnded;
        public IReadOnlyList<SeatId> ActiveSeats => activeSeats;
        public IReadOnlyList<SeatId> ActiveTurnSeats => activeSeats;
        public IReadOnlyList<SeatId> OccupiedSeats => GetOccupiedSeats();
        public IReadOnlyList<SeatSlot> SeatSlots => seatSlots;
        public IReadOnlyList<DiscardRecord> Discards => discards;
        public IReadOnlyList<ActiveSkillEffect> ActiveSkillEffects => activeSkillEffects;
        public IReadOnlyList<ReachDiscardCandidate> ReachDiscardCandidates => reachDiscardCandidates;

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
            // PROTOTYPE: Non-local participants default to CPU until network setup assigns RemoteHuman.
            ParticipantType participantType = playerId == SelfPlayerId
                ? ParticipantType.LocalHuman
                : ParticipantType.Cpu;
            ClearPlayerFromSeatSlots(playerId);
            GetSeatSlot(seat).AssignPlayer(playerId, participantType);
        }

        public void SetParticipantType(SeatId seat, ParticipantType participantType)
        {
            SeatSlot slot = GetSeatSlot(seat);
            if (!slot.HasPlayer)
                throw new InvalidOperationException($"Cannot set participant type for empty seat {seat}.");

            slot.SetParticipantType(participantType);
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
            BeginWinDecisionDetailed(
                seat,
                WinType.Tsumo,
                GetPlayerSeat(seat).DrawnTile,
                null,
                turnIndex);
        }

        public void BeginWinDecisionDetailed(
            SeatId seat,
            WinType winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex)
        {
            BeginWinDecisionDetailed(
                seat,
                winType,
                winningTile,
                sourceSeat,
                turnIndex,
                null);
        }

        public void BeginWinDecisionDetailed(
            SeatId seat,
            WinType winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            WinDeclarationEvaluationResult evaluationResult)
        {
            ClearReachDecision();
            IsWinDecisionPending = true;
            WinDecisionSeat = seat;
            WinDecisionType = winType;
            WinningTile = winningTile;
            WinSourceSeat = sourceSeat;
            WinDecisionTurnIndex = turnIndex;
            PendingWinDeclarationEvaluation = evaluationResult;
        }

        public void ClearWinDecision()
        {
            IsWinDecisionPending = false;
            WinDecisionSeat = default;
            WinDecisionType = null;
            WinningTile = null;
            WinSourceSeat = null;
            WinDecisionTurnIndex = 0;
            PendingWinDeclarationEvaluation = null;
        }

        public void BeginReachDecision(
            SeatId seat,
            IReadOnlyList<ReachDiscardCandidate> candidates,
            int turnIndex)
        {
            List<ReachDiscardCandidate> copiedCandidates = new List<ReachDiscardCandidate>();
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                    copiedCandidates.Add(candidates[i]);
            }

            reachDiscardCandidates.Clear();
            for (int i = 0; i < copiedCandidates.Count; i++)
                reachDiscardCandidates.Add(copiedCandidates[i]);

            if (reachDiscardCandidates.Count <= 0)
            {
                ClearReachDecision();
                return;
            }

            IsReachDecisionPending = true;
            IsReachDiscardSelectionPending = false;
            ReachDecisionSeat = seat;
            ReachDecisionTurnIndex = turnIndex;
        }

        public void BeginReachDiscardSelection(SeatId seat)
        {
            if (!IsReachDecisionPending || ReachDecisionSeat != seat || reachDiscardCandidates.Count <= 0)
                return;

            IsReachDecisionPending = false;
            IsReachDiscardSelectionPending = true;
        }

        public bool CancelReachDiscardSelection()
        {
            if (!IsReachDiscardSelectionPending || reachDiscardCandidates.Count <= 0)
                return false;

            IsReachDecisionPending = true;
            IsReachDiscardSelectionPending = false;
            return true;
        }

        public void ClearReachDecision()
        {
            IsReachDecisionPending = false;
            IsReachDiscardSelectionPending = false;
            ReachDecisionSeat = default;
            ReachDecisionTurnIndex = 0;
            reachDiscardCandidates.Clear();
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
        public ParticipantType? ParticipantType { get; private set; }
        public bool HasPlayer => PlayerId.HasValue;
        public bool IsEmpty => !PlayerId.HasValue;
        public string StateLabel => PlayerId.HasValue ? PlayerId.Value.ToString() : "Empty";

        internal void AssignPlayer(PlayerId playerId, ParticipantType participantType)
        {
            PlayerId = playerId;
            ParticipantType = participantType;
        }

        internal void SetParticipantType(ParticipantType participantType)
        {
            ParticipantType = participantType;
        }

        internal void Clear()
        {
            PlayerId = null;
            ParticipantType = null;
        }
    }
}
