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
        private readonly Dictionary<int, DiscardClaim> discardClaims =
            new Dictionary<int, DiscardClaim>();
        private readonly List<ActiveSkillEffect> activeSkillEffects = new List<ActiveSkillEffect>();
        private readonly List<ReachDiscardCandidate> reachDiscardCandidates =
            new List<ReachDiscardCandidate>();
        private TurnDrawRecord? lastTurnDraw;
        private TurnPhaseType turnPhase = TurnPhaseType.WaitingForDraw;
        private static int nextReactionWindowId = 1;
        private int nextDiscardId = 1;
        private ReactionWindow reactionWindow;
        private SeatId winDecisionSeat;
        private WinType? winDecisionType;
        private Tile? winningTile;
        private SeatId? winSourceSeat;
        private int winDecisionTurnIndex;
        private WinDeclarationEvaluationResult pendingWinDeclarationEvaluation;

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
        public bool HasCallOccurred { get; private set; }
        public bool IsRoundEnded
        {
            get =>
                turnPhase == TurnPhaseType.RoundEnded ||
                turnPhase == TurnPhaseType.RoundResult ||
                turnPhase == TurnPhaseType.GameEnded;
            set
            {
                if (value && !IsRoundEnded)
                {
                    EndRoundWithoutResult();
                }
                else if (IsRoundEnded)
                {
                    TransitionTo(ResolveNormalTurnPhase());
                }
            }
        }
        public RoundResult CurrentRoundResult { get; private set; }
        public bool IsRoundResultPending => turnPhase == TurnPhaseType.RoundResult;
        public bool IsGameEnded => turnPhase == TurnPhaseType.GameEnded;
        public bool IsReactionWindowPending => turnPhase == TurnPhaseType.ReactionWindow;
        public ReactionWindow CurrentReactionWindow => reactionWindow;
        public bool IsWinDecisionPending => turnPhase == TurnPhaseType.WinDecision ||
            GetPendingRonCandidate() != null;
        public SeatId WinDecisionSeat => GetPendingRonCandidate() != null
            ? GetPendingRonCandidate().Seat
            : winDecisionSeat;
        public WinType? WinDecisionType => GetPendingRonCandidate() != null
            ? WinType.Ron
            : winDecisionType;
        public Tile? WinningTile => GetPendingRonCandidate() != null
            ? reactionWindow.SourceDiscard.Tile
            : winningTile;
        public SeatId? WinSourceSeat => GetPendingRonCandidate() != null
            ? reactionWindow.SourceDiscard.ActorSeat
            : winSourceSeat;
        public int WinDecisionTurnIndex => GetPendingRonCandidate() != null
            ? reactionWindow.TurnIndex
            : winDecisionTurnIndex;
        public WinDeclarationEvaluationResult PendingWinDeclarationEvaluation =>
            GetPendingRonCandidate() != null
                ? GetPendingRonCandidate().WinDeclarationEvaluation
                : pendingWinDeclarationEvaluation;
        public TurnDrawRecord? LastTurnDraw => lastTurnDraw;
        public bool IsReachDecisionPending => turnPhase == TurnPhaseType.ReachDecision;
        public bool IsReachDiscardSelectionPending =>
            turnPhase == TurnPhaseType.ReachDiscardSelection;
        public SeatId ReachDecisionSeat { get; private set; }
        public int ReachDecisionTurnIndex { get; private set; }
        public TurnPhaseType TurnPhase => turnPhase;
        public bool IsInteractionLocked =>
            TurnPhase == TurnPhaseType.WinDecision ||
            TurnPhase == TurnPhaseType.ReactionWindow ||
            TurnPhase == TurnPhaseType.ReachDecision ||
            TurnPhase == TurnPhaseType.RoundEnded ||
            TurnPhase == TurnPhaseType.RoundResult ||
            TurnPhase == TurnPhaseType.GameEnded;
        public IReadOnlyList<SeatId> ActiveSeats => activeSeats;
        public IReadOnlyList<SeatId> ActiveTurnSeats => activeSeats;
        public IReadOnlyList<SeatId> OccupiedSeats => GetOccupiedSeats();
        public IReadOnlyList<SeatSlot> SeatSlots => seatSlots;
        public IReadOnlyList<DiscardRecord> Discards => discards;
        public IReadOnlyDictionary<int, DiscardClaim> DiscardClaims => discardClaims;
        public IReadOnlyList<ActiveSkillEffect> ActiveSkillEffects => activeSkillEffects;
        public IReadOnlyList<ReachDiscardCandidate> ReachDiscardCandidates => reachDiscardCandidates;

        public void EnterWaitingForDraw()
        {
            if (GetPlayerSeat(CurrentTurn).HasDrawnTile)
            {
                throw new InvalidOperationException(
                    $"Cannot wait for draw while the current seat {CurrentTurn} has a drawn tile.");
            }

            TransitionTo(TurnPhaseType.WaitingForDraw);
        }

        public void EnterWaitingForDiscard()
        {
            if (!GetPlayerSeat(CurrentTurn).HasDrawnTile)
            {
                throw new InvalidOperationException(
                    "Cannot wait for discard while the current seat has no drawn tile.");
            }

            TransitionTo(TurnPhaseType.WaitingForDiscard);
        }

        public void EnterWaitingForDiscardAfterCall()
        {
            if (GetPlayerSeat(CurrentTurn).HasDrawnTile)
            {
                throw new InvalidOperationException(
                    "Cannot wait for a post-call discard while the current seat has a drawn tile.");
            }

            TransitionTo(TurnPhaseType.WaitingForDiscardAfterCall);
        }

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

        public DiscardRecord AddDiscard(DiscardRecord record)
        {
            if (record.Id <= 0)
                record = record.WithId(nextDiscardId++);
            else
                nextDiscardId = Math.Max(nextDiscardId, record.Id + 1);

            discards.Add(record);
            return record;
        }

        public bool CanClaimDiscard(int discardId, SeatId callerSeat, SeatId sourceSeat, Tile calledTile)
        {
            if (discardId <= 0 || callerSeat == sourceSeat || !calledTile.IsValid ||
                discardClaims.ContainsKey(discardId))
            {
                return false;
            }

            for (int i = 0; i < discards.Count; i++)
            {
                DiscardRecord record = discards[i];
                if (record.Id != discardId)
                    continue;

                return record.ActorSeat == sourceSeat && record.Tile == calledTile;
            }

            return false;
        }

        public bool TryClaimDiscard(OpenMeld openMeld)
        {
            if (openMeld == null || !CanClaimDiscard(
                    openMeld.SourceDiscardId,
                    openMeld.CallerSeat,
                    openMeld.SourceSeat,
                    openMeld.CalledTile))
            {
                return false;
            }

            discardClaims.Add(
                openMeld.SourceDiscardId,
                new DiscardClaim(openMeld.SourceDiscardId, openMeld.CallerSeat, openMeld));
            return true;
        }

        public void MarkCallOccurred()
        {
            HasCallOccurred = true;
        }

        public bool TryGetDiscardClaim(int discardId, out DiscardClaim discardClaim)
        {
            return discardClaims.TryGetValue(discardId, out discardClaim);
        }

        public void RecordTurnDraw(
            SeatId actorSeat,
            Tile tile,
            int turnIndex,
            bool isLastLiveWallDraw)
        {
            lastTurnDraw = new TurnDrawRecord(
                actorSeat,
                tile,
                turnIndex,
                isLastLiveWallDraw);
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
            ClearWinDecisionData();
            TransitionTo(TurnPhaseType.WinDecision);
            winDecisionSeat = seat;
            winDecisionType = winType;
            this.winningTile = winningTile;
            winSourceSeat = sourceSeat;
            winDecisionTurnIndex = turnIndex;
            pendingWinDeclarationEvaluation = evaluationResult;
        }

        public void ClearWinDecision()
        {
            if (turnPhase == TurnPhaseType.WinDecision)
            {
                TurnPhaseType nextPhase =
                    winDecisionType == WinType.Tsumo &&
                    GetPlayerSeat(CurrentTurn).HasDrawnTile
                        ? TurnPhaseType.WaitingForDiscard
                        : TurnPhaseType.WaitingForDraw;
                TransitionTo(nextPhase);
                return;
            }

            ClearWinDecisionData();
        }

        public ReactionWindow BeginReactionWindow(
            DiscardRecord sourceDiscard,
            IReadOnlyList<ReactionWindowCandidate> candidates)
        {
            ClearReactionWindowData();
            TransitionTo(TurnPhaseType.ReactionWindow);
            reactionWindow = new ReactionWindow(
                nextReactionWindowId++,
                sourceDiscard,
                TurnIndex,
                candidates);
            return reactionWindow;
        }

        public bool CloseReactionWindow(int windowId)
        {
            if (!IsReactionWindowPending || reactionWindow == null ||
                reactionWindow.WindowId != windowId)
            {
                return false;
            }

            TransitionTo(TurnPhaseType.WaitingForDraw);
            return true;
        }

        public void BeginRoundResult(RoundResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            CurrentRoundResult = result;
            TransitionTo(TurnPhaseType.RoundResult);
        }

        public void CompleteRoundResult(bool gameEnded)
        {
            TransitionTo(gameEnded
                ? TurnPhaseType.GameEnded
                : TurnPhaseType.RoundEnded);
        }

        public void EndRoundWithoutResult()
        {
            TransitionTo(TurnPhaseType.RoundEnded);
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

            TransitionTo(TurnPhaseType.ReachDecision);
            ReachDecisionSeat = seat;
            ReachDecisionTurnIndex = turnIndex;
        }

        public void BeginReachDiscardSelection(SeatId seat)
        {
            if (!IsReachDecisionPending || ReachDecisionSeat != seat || reachDiscardCandidates.Count <= 0)
                return;

            TransitionTo(TurnPhaseType.ReachDiscardSelection);
        }

        public bool CancelReachDiscardSelection()
        {
            if (!IsReachDiscardSelectionPending || reachDiscardCandidates.Count <= 0)
                return false;

            TransitionTo(TurnPhaseType.ReachDecision);
            return true;
        }

        public void ClearReachDecision()
        {
            if (turnPhase == TurnPhaseType.ReachDecision ||
                turnPhase == TurnPhaseType.ReachDiscardSelection)
            {
                TransitionTo(ResolveNormalTurnPhase());
                return;
            }

            ClearReachDecisionData();
        }

        private TurnPhaseType ResolveNormalTurnPhase()
        {
            return GetPlayerSeat(CurrentTurn).HasDrawnTile
                ? TurnPhaseType.WaitingForDiscard
                : TurnPhaseType.WaitingForDraw;
        }

        private void TransitionTo(TurnPhaseType nextPhase)
        {
            if (nextPhase != TurnPhaseType.WinDecision)
                ClearWinDecisionData();
            if (nextPhase != TurnPhaseType.ReachDecision &&
                nextPhase != TurnPhaseType.ReachDiscardSelection)
            {
                ClearReachDecisionData();
            }
            if (nextPhase != TurnPhaseType.ReactionWindow)
                ClearReactionWindowData();
            if (nextPhase != TurnPhaseType.RoundResult &&
                nextPhase != TurnPhaseType.GameEnded)
            {
                CurrentRoundResult = null;
            }

            turnPhase = nextPhase;
        }

        private void ClearWinDecisionData()
        {
            winDecisionSeat = default;
            winDecisionType = null;
            winningTile = null;
            winSourceSeat = null;
            winDecisionTurnIndex = 0;
            pendingWinDeclarationEvaluation = null;
        }

        private void ClearReachDecisionData()
        {
            ReachDecisionSeat = default;
            ReachDecisionTurnIndex = 0;
            reachDiscardCandidates.Clear();
        }

        private ReactionWindowCandidate GetPendingRonCandidate()
        {
            return IsReactionWindowPending && reactionWindow != null
                ? reactionWindow.PendingRonCandidate
                : null;
        }

        private void ClearReactionWindowData()
        {
            reactionWindow = null;
        }

        public void ClearIppatsuEligibilityForAllPlayers()
        {
            for (int i = 0; i < ActiveTurnSeats.Count; i++)
                GetPlayerSeat(ActiveTurnSeats[i]).ClearIppatsuEligibility();
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
