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
        private SelfKanDecision selfKanDecision;
        private SelfKanCandidate pendingKakan;
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
            ? reactionWindow.Source.Tile
            : winningTile;
        public SeatId? WinSourceSeat => GetPendingRonCandidate() != null
            ? reactionWindow.Source.ActorSeat
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
        public bool IsSelfKanDecisionPending => turnPhase == TurnPhaseType.SelfKanDecision;
        public SelfKanDecision CurrentSelfKanDecision => selfKanDecision;
        public SelfKanCandidate PendingKakan => pendingKakan;
        public bool IsReachDiscardSelectionPending =>
            turnPhase == TurnPhaseType.ReachDiscardSelection;
        public SeatId ReachDecisionSeat { get; private set; }
        public int ReachDecisionTurnIndex { get; private set; }
        public TurnPhaseType TurnPhase => turnPhase;
        public bool IsInteractionLocked =>
            TurnPhase == TurnPhaseType.WinDecision ||
            TurnPhase == TurnPhaseType.ReactionWindow ||
            TurnPhase == TurnPhaseType.ReachDecision ||
            TurnPhase == TurnPhaseType.SelfKanDecision ||
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

        public void EnterWaitingForRinshanDraw()
        {
            if (GetPlayerSeat(CurrentTurn).HasDrawnTile)
            {
                throw new InvalidOperationException(
                    "Cannot wait for a rinshan draw while the current seat has a drawn tile.");
            }
            if (!Wall.CanDrawRinshan)
                throw new InvalidOperationException("A rinshan draw is not available.");

            TransitionTo(TurnPhaseType.WaitingForRinshanDraw);
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
            AssignPlayerToSeat(playerId, seat, participantType);
        }

        /// <summary>
        /// Applies the current round's compatibility projection. The shared
        /// participant configuration remains match-lifetime data outside this
        /// round state while existing rules continue to read SeatSlot.ParticipantType.
        /// </summary>
        public void AssignPlayerToSeat(
            PlayerId playerId,
            SeatId seat,
            ParticipantType participantType)
        {
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

        public bool TryClaimDiscard(PlayerMeld meld)
        {
            if (meld == null || !meld.HasDiscardSource || !CanClaimDiscard(
                    meld.SourceDiscardId.Value,
                    meld.OwnerSeat,
                    meld.SourceSeat.Value,
                    meld.AcquiredTile.Value))
            {
                return false;
            }

            DiscardClaim claim = new DiscardClaim(meld);
            discardClaims.Add(claim.DiscardId, claim);
            return true;
        }

        internal bool TryCommitMeldCall(
            ReactionWindow expectedWindow,
            PreparedMeldCall preparedCall,
            out string reason)
        {
            reason = string.Empty;
            if (!IsReactionWindowPending || reactionWindow == null ||
                !ReferenceEquals(reactionWindow, expectedWindow) ||
                !reactionWindow.IsAcceptingAnswers)
            {
                reason = "MeldCallWindowMissing";
                return false;
            }

            if (preparedCall == null || preparedCall.Candidate == null ||
                preparedCall.Meld == null || preparedCall.HandTiles == null)
            {
                reason = "MeldCallCandidateMissing";
                return false;
            }

            ReactionWindowCandidate candidate = preparedCall.Candidate;
            PlayerMeld meld = preparedCall.Meld;
            if (!ContainsPendingMeldCallCandidate(reactionWindow, candidate) ||
                !IsPreparedMeldCallConsistent(
                    reactionWindow.SourceDiscard,
                    candidate,
                    preparedCall.HandTiles,
                    meld))
            {
                reason = "MeldCallStateChanged";
                return false;
            }

            PlayerSeat playerSeat = GetPlayerSeat(candidate.Seat);
            if (!playerSeat.CanAddMeld(meld) ||
                !playerSeat.Hand.ContainsTilesByValue(preparedCall.HandTiles))
            {
                reason = "MeldCallTilesMissing";
                return false;
            }

            if (!PlayerMeldRules.TryGetStructuralMeldCount(
                    playerSeat.Melds,
                    out int structuralMeldCount) ||
                structuralMeldCount >= 4 ||
                (meld.Type == PlayerMeldType.Daiminkan && !Wall.CanDrawRinshan))
            {
                reason = "MeldCallStateChanged";
                return false;
            }

            if (!meld.HasDiscardSource || !CanClaimDiscard(
                    meld.SourceDiscardId.Value,
                    meld.OwnerSeat,
                    meld.SourceSeat.Value,
                    meld.AcquiredTile.Value))
            {
                reason = "MeldCallStateChanged";
                return false;
            }

            // All failure conditions are checked above. This block contains only
            // deterministic state writes and deliberately raises no notifications.
            if (!playerSeat.Hand.TryRemoveTilesByValue(preparedCall.HandTiles))
            {
                reason = "MeldCallTilesMissing";
                return false;
            }

            playerSeat.AddMeld(meld);
            DiscardClaim claim = new DiscardClaim(meld);
            discardClaims.Add(claim.DiscardId, claim);
            HasCallOccurred = true;
            candidate.Declare();
            reactionWindow.CloseMeldCallsExcept(candidate);
            return true;
        }

        /// <summary>
        /// Commits only the declaration state of a ron chosen by the
        /// multi-seat reaction answer path.  Round completion and notifications
        /// deliberately remain the responsibility of MahjongGameFlow after it
        /// receives the resulting ReactionWindowResolution.
        /// </summary>
        internal bool TryCommitReactionRon(
            ReactionWindow expectedWindow,
            ReactionWindowCandidate candidate,
            out string reason)
        {
            reason = string.Empty;
            if (!IsReactionWindowPending || reactionWindow == null ||
                !ReferenceEquals(reactionWindow, expectedWindow) ||
                !reactionWindow.IsAcceptingAnswers)
            {
                reason = "ReactionRonWindowMissing";
                return false;
            }

            if (!ContainsPendingRonCandidate(reactionWindow, candidate))
            {
                reason = "ReactionRonCandidateMissing";
                return false;
            }

            candidate.Declare();
            reactionWindow.CloseCandidatesExcept(candidate);
            return true;
        }

        internal bool TryCommitAnkan(
            SeatId seat,
            Tile tile,
            out PlayerMeld meld,
            out string reason)
        {
            meld = null;
            reason = string.Empty;
            if (!tile.IsValid || IsRoundEnded || CurrentTurn != seat ||
                turnPhase != TurnPhaseType.WaitingForDiscard)
            {
                reason = "AnkanTurnUnavailable";
                return false;
            }

            SeatSlot slot = GetSeatSlot(seat);
            PlayerSeat playerSeat = GetPlayerSeat(seat);
            if (!slot.HasPlayer || slot.ParticipantType != ParticipantType.LocalHuman ||
                !playerSeat.HasDrawnTile)
            {
                reason = "AnkanSeatUnavailable";
                return false;
            }

            if (!Wall.CanDrawRinshan)
            {
                reason = "RinshanDrawUnavailable";
                return false;
            }

            if (!PlayerMeldRules.TryGetStructuralMeldCount(
                    playerSeat.Melds,
                    out int structuralMeldCount) ||
                structuralMeldCount >= 4)
            {
                reason = "AnkanMeldLimit";
                return false;
            }

            int logicalTileCount = playerSeat.Hand.CountTilesByValue(tile) +
                (playerSeat.DrawnTile.Value == tile ? 1 : 0);
            if (logicalTileCount != 4)
            {
                reason = "AnkanTilesMissing";
                return false;
            }

            PlayerMeld preparedMeld = PlayerMeld.CreateAnkan(
                new[] { tile, tile, tile, tile },
                seat);

            // All validation is complete. The seat mutation and phase transition do
            // not notify observers and cannot fail for the validated snapshot.
            if (!playerSeat.TryCommitAnkan(tile, preparedMeld))
            {
                reason = "AnkanTilesMissing";
                return false;
            }

            HasCallOccurred = true;
            meld = preparedMeld;
            TransitionTo(TurnPhaseType.WaitingForRinshanDraw);
            return true;
        }

        internal bool TryCommitKakan(
            ReactionWindow expectedWindow,
            SelfKanCandidate expectedCandidate,
            out PlayerMeld meld,
            out string reason)
        {
            meld = null;
            reason = string.Empty;
            if (!IsReactionWindowPending || reactionWindow == null ||
                !ReferenceEquals(reactionWindow, expectedWindow) ||
                !reactionWindow.IsClosed || !reactionWindow.Source.IsKakan ||
                pendingKakan == null || expectedCandidate == null ||
                !pendingKakan.Matches(expectedCandidate))
            {
                reason = "KakanWindowMissing";
                return false;
            }

            SelfKanCandidate candidate = pendingKakan;
            if (candidate.Kind != SelfKanKind.Kakan ||
                candidate.TurnIndex != TurnIndex || CurrentTurn != candidate.Seat ||
                reactionWindow.Source.ActorSeat != candidate.Seat ||
                reactionWindow.Source.Tile != candidate.Tile ||
                reactionWindow.Source.TurnIndex != candidate.TurnIndex ||
                !Wall.CanDrawRinshan)
            {
                reason = "KakanStateChanged";
                return false;
            }

            PlayerSeat playerSeat = GetPlayerSeat(candidate.Seat);
            if (playerSeat.IsReachDeclared ||
                candidate.SourcePonMeldIndex < 0 ||
                candidate.SourcePonMeldIndex >= playerSeat.Melds.Count)
            {
                reason = "KakanStateChanged";
                return false;
            }

            PlayerMeld sourcePon = playerSeat.Melds[candidate.SourcePonMeldIndex];
            if (sourcePon == null || sourcePon.Type != PlayerMeldType.Pon ||
                !sourcePon.HasDiscardSource ||
                (candidate.SourcePon != null &&
                 !ReferenceEquals(sourcePon, candidate.SourcePon)) ||
                sourcePon.AcquiredTile.Value != candidate.Tile ||
                !discardClaims.ContainsKey(sourcePon.SourceDiscardId.Value))
            {
                reason = "KakanClaimMissing";
                return false;
            }

            if (!playerSeat.TryCommitKakan(
                    candidate.SourcePonMeldIndex,
                    candidate.Tile,
                    candidate.AddedTileLocation,
                    out PlayerMeld preparedMeld))
            {
                reason = "KakanStateChanged";
                return false;
            }

            // The source discard was already claimed by the pon.  Keep its
            // history and replace only the claim's meld projection.
            discardClaims[preparedMeld.SourceDiscardId.Value] =
                new DiscardClaim(preparedMeld);
            HasCallOccurred = true;
            pendingKakan = null;
            meld = preparedMeld;
            TransitionTo(TurnPhaseType.WaitingForRinshanDraw);
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
            ClearPendingKakan();
            TransitionTo(TurnPhaseType.ReactionWindow);
            reactionWindow = new ReactionWindow(
                nextReactionWindowId++,
                sourceDiscard,
                TurnIndex,
                candidates);
            return reactionWindow;
        }

        public ReactionWindow BeginKakanReactionWindow(
            SelfKanCandidate candidate,
            IReadOnlyList<ReactionWindowCandidate> candidates)
        {
            if (candidate == null || candidate.Kind != SelfKanKind.Kakan ||
                IsRoundEnded || CurrentTurn != candidate.Seat ||
                turnPhase != TurnPhaseType.WaitingForDiscard ||
                candidate.TurnIndex != TurnIndex)
            {
                throw new InvalidOperationException("Kakan reaction state is unavailable.");
            }

            ClearReactionWindowData();
            pendingKakan = candidate;
            TransitionTo(TurnPhaseType.ReactionWindow);
            reactionWindow = new ReactionWindow(
                nextReactionWindowId++,
                ReactionWindowSource.FromKakan(
                    candidate.Seat,
                    candidate.Tile,
                    candidate.TurnIndex),
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

            reactionWindow.TryClose();
            TransitionTo(TurnPhaseType.WaitingForDraw);
            return true;
        }

        public bool BeginReactionWindowResolution(int windowId)
        {
            return IsReactionWindowPending && reactionWindow != null &&
                reactionWindow.WindowId == windowId &&
                (reactionWindow.IsResolving || reactionWindow.TryBeginResolution());
        }

        public bool CompleteReactionWindowResolution(int windowId)
        {
            return IsReactionWindowPending && reactionWindow != null &&
                reactionWindow.WindowId == windowId && reactionWindow.IsResolving &&
                reactionWindow.TryClose();
        }

        public bool BeginSelfKanDecision(
            SeatId seat,
            IReadOnlyList<SelfKanCandidate> candidates)
        {
            if (IsRoundEnded || CurrentTurn != seat ||
                turnPhase != TurnPhaseType.WaitingForDiscard ||
                candidates == null || candidates.Count <= 0)
            {
                return false;
            }

            SelfKanCandidate[] copiedCandidates = new SelfKanCandidate[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                SelfKanCandidate candidate = candidates[i];
                if (candidate == null || candidate.Seat != seat ||
                    candidate.TurnIndex != TurnIndex)
                {
                    return false;
                }

                copiedCandidates[i] = candidate;
            }

            selfKanDecision = new SelfKanDecision(seat, TurnIndex, copiedCandidates);
            TransitionTo(TurnPhaseType.SelfKanDecision);
            return true;
        }

        public bool TryDeclineSelfKanDecision(SeatId seat)
        {
            if (!IsSelfKanDecisionPending || selfKanDecision == null ||
                selfKanDecision.Seat != seat ||
                selfKanDecision.TurnIndex != TurnIndex || CurrentTurn != seat ||
                !GetPlayerSeat(seat).HasDrawnTile)
            {
                return false;
            }

            TransitionTo(TurnPhaseType.WaitingForDiscard);
            return true;
        }

        public bool TryAcceptSelfKanDecision(SelfKanCandidate candidate)
        {
            if (!IsSelfKanDecisionPending || selfKanDecision == null ||
                candidate == null || candidate.Seat != CurrentTurn ||
                candidate.TurnIndex != TurnIndex ||
                selfKanDecision.Seat != candidate.Seat ||
                selfKanDecision.TurnIndex != candidate.TurnIndex)
            {
                return false;
            }

            bool found = false;
            for (int i = 0; i < selfKanDecision.Candidates.Count; i++)
            {
                if (selfKanDecision.Candidates[i].Matches(candidate))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;

            TransitionTo(TurnPhaseType.WaitingForDiscard);
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
            {
                ClearReactionWindowData();
                ClearPendingKakan();
            }
            if (nextPhase != TurnPhaseType.SelfKanDecision)
                ClearSelfKanDecisionData();
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

        private void ClearPendingKakan()
        {
            pendingKakan = null;
        }

        private void ClearSelfKanDecisionData()
        {
            selfKanDecision = null;
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

        private static bool IsPreparedMeldCallConsistent(
            DiscardRecord sourceDiscard,
            ReactionWindowCandidate candidate,
            IReadOnlyList<Tile> handTiles,
            PlayerMeld meld)
        {
            if (candidate == null || handTiles == null ||
                meld == null || !meld.HasDiscardSource || meld.OwnerSeat != candidate.Seat ||
                meld.SourceDiscardId.Value != sourceDiscard.Id ||
                meld.SourceSeat.Value != sourceDiscard.ActorSeat ||
                meld.AcquiredTile.Value != sourceDiscard.Tile)
            {
                return false;
            }

            if (candidate.Kind == ReactionKind.Pon)
            {
                return handTiles.Count == 2 && candidate.PonDetail != null &&
                    candidate.PonDetail.CalledTile == sourceDiscard.Tile &&
                    meld.Type == PlayerMeldType.Pon &&
                    ContainsOnlyTile(handTiles, sourceDiscard.Tile) &&
                    ContainsOnlyTile(meld.PhysicalTiles, sourceDiscard.Tile);
            }

            if (candidate.Kind == ReactionKind.Daiminkan)
            {
                return handTiles.Count == 3 && candidate.DaiminkanDetail != null &&
                    candidate.DaiminkanDetail.CalledTile == sourceDiscard.Tile &&
                    meld.Type == PlayerMeldType.Daiminkan &&
                    ContainsOnlyTile(handTiles, sourceDiscard.Tile) &&
                    meld.PhysicalTileCount == 4 &&
                    ContainsOnlyTile(meld.PhysicalTiles, sourceDiscard.Tile);
            }

            if (handTiles.Count != 2 || candidate.Kind != ReactionKind.Chi ||
                candidate.ChiDetail == null ||
                candidate.ChiDetail.CalledTile != sourceDiscard.Tile ||
                meld.Type != PlayerMeldType.Chi)
            {
                return false;
            }

            for (int i = 0; i < candidate.ChiDetail.Options.Count; i++)
            {
                ChiOption option = candidate.ChiDetail.Options[i];
                if (HasSameTileValues(option.HandTiles, handTiles) &&
                    HasSameTileValues(option.MeldTiles, meld.PhysicalTiles))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsOnlyTile(IReadOnlyList<Tile> tiles, Tile expectedTile)
        {
            if (tiles == null || tiles.Count <= 0)
                return false;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != expectedTile)
                    return false;
            }

            return true;
        }

        private static bool HasSameTileValues(
            IReadOnlyList<Tile> firstTiles,
            IReadOnlyList<Tile> secondTiles)
        {
            if (firstTiles == null || secondTiles == null || firstTiles.Count != secondTiles.Count)
                return false;

            for (int i = 0; i < firstTiles.Count; i++)
            {
                int firstCount = 0;
                int secondCount = 0;
                for (int j = 0; j < firstTiles.Count; j++)
                {
                    if (firstTiles[j] == firstTiles[i])
                        firstCount++;
                    if (secondTiles[j] == firstTiles[i])
                        secondCount++;
                }

                if (firstCount != secondCount)
                    return false;
            }

            return true;
        }

        private static bool ContainsPendingMeldCallCandidate(
            ReactionWindow window,
            ReactionWindowCandidate candidate)
        {
            if (candidate == null || !candidate.IsPending ||
                (candidate.Kind != ReactionKind.Pon &&
                    candidate.Kind != ReactionKind.Daiminkan &&
                    candidate.Kind != ReactionKind.Chi))
            {
                return false;
            }

            for (int i = 0; i < window.Candidates.Count; i++)
            {
                if (ReferenceEquals(window.Candidates[i], candidate))
                    return true;
            }

            return false;
        }

        private static bool ContainsPendingRonCandidate(
            ReactionWindow window,
            ReactionWindowCandidate candidate)
        {
            if (candidate == null || !candidate.IsPending ||
                candidate.Kind != ReactionKind.Ron)
            {
                return false;
            }

            for (int i = 0; i < window.Candidates.Count; i++)
            {
                if (ReferenceEquals(window.Candidates[i], candidate))
                    return true;
            }

            return false;
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
