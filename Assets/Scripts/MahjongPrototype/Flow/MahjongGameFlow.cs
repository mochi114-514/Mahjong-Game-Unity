using System.Collections.Generic;
using MahjongPrototype.Domain;
using MahjongPrototype.Logging;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;
using UnityEngine.Serialization;

namespace MahjongPrototype
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Mahjong Game Flow")]
    public sealed class MahjongGameFlow : MonoBehaviour
    {
        [Header("Prototype Players")]
        [SerializeField, Range(1, 4)] private int participantCount = 1;

        [Header("Prototype View")]
        [SerializeField] private SeatId viewerSeat = SeatId.East;

        [Header("Self Seat")]
        [FormerlySerializedAs("randomizeSelfWind")]
        [SerializeField] private bool randomizeSelfSeat = true;
        [FormerlySerializedAs("fixedSelfWind")]
        [SerializeField] private SeatId fixedSelfSeat = SeatId.East;

        [Header("Round Setup")]
        [SerializeField, Min(1)] private int initialHandTileCount = 13;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool enableAutoDraw;
        [SerializeField] private bool useFixedRandomSeed = false;
        [SerializeField] private int fixedRandomSeed = 12345;

        [Header("Scene References")]
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Dev Log")]
        [SerializeField] private bool enableDevLog = true;
        [SerializeField] private bool enableReleaseBuildLogging = false;
        [SerializeField] private bool enableTurnDebugLog;

        [Header("Warnings")]
        [SerializeField] private bool logWarnings = true;

        [Header("Hand Sort")]
        [SerializeField] private bool autoSortEnabled;

        private readonly PlayerTurnManager playerTurnManager = new PlayerTurnManager(new TurnOrderService());
        private readonly DrawService drawService = new DrawService();
        private readonly DiscardService discardService = new DiscardService();
        private readonly HandWinChecker handWinChecker = new HandWinChecker();
        private readonly SkillSystem skillSystem = new SkillSystem();
        private readonly SkillReservationService skillReservationService = new SkillReservationService();

        private MahjongGameState gameState;
        private bool warnedMissingNotifier;

        public MahjongGameState CurrentState => gameState;
        public MahjongEventNotifier EventNotifier => eventNotifier;
        public SeatId ViewerSeat => viewerSeat;
        public bool IsWinDecisionPending => gameState != null && gameState.IsWinDecisionPending;
        public bool IsAutoSortEnabled => autoSortEnabled;
        public bool IsInteractionLocked => gameState != null && gameState.IsInteractionLocked;

        private void Reset()
        {
            CacheReferences();
            NormalizeParticipantCount();
        }

        private void Awake()
        {
            CacheReferences();
            NormalizeParticipantCount();
        }

        private void Start()
        {
            if (autoStart)
                StartNewRound();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeParticipantCount();
            initialHandTileCount = Mathf.Max(1, initialHandTileCount);
        }
#endif

        [ContextMenu("Prototype/Start New Round")]
        public void StartNewRound()
        {
            CacheReferences();
            NormalizeParticipantCount();

            DevLog.Initialize(enableDevLog, enableReleaseBuildLogging);
            ClearWinDecision();
            skillReservationService.Clear();
            NotifyRunStarted();
            LogRunStarted();

            int? seed = useFixedRandomSeed ? fixedRandomSeed : (int?)null;
            gameState = new MahjongGameState(Wall.CreateStandardShuffled(seed));
            SeatId selfSeat = ResolveSelfSeat();
            AssignParticipantsToSeats(selfSeat);
            gameState.RebuildActiveTurnSeatsFromSeatSlots();
            playerTurnManager.InitializeRound(gameState, selfSeat);

            LogSeatSlotsAssigned();
            NotifyRoundStarted();
            LogRoundStarted();

            DealInitialHands();
            NotifyRoundSetupCompleted();
            StartTurn(gameState.CurrentTurn, gameState.TurnIndex);
        }

        public void RetryPrototype()
        {
            // PROTOTYPE: Reset only the current flow state without reloading the scene.
            StartNewRound();
        }

        public void RequestDraw()
        {
            if (!CanUseSelfTurnInput("DrawBlocked"))
                return;

            TryDrawForSeat(gameState.SelfSeat, "DrawCompleted", "DrawBlocked", true);
        }

        private bool TryDrawForSeat(
            SeatId seat,
            string completedEventName,
            string blockedEventName,
            bool warnOnBlocked)
        {
            if (gameState.CurrentTurn != seat)
            {
                if (warnOnBlocked)
                    Warn("Only the current seat can draw.");

                LogTurnBlocked(blockedEventName, "NotCurrentTurn");
                return false;
            }

            if (gameState.IsRoundEnded)
            {
                if (warnOnBlocked)
                    Warn("Round already ended. Press Retry.");

                LogTurnBlocked(blockedEventName, "RoundEnded");
                return false;
            }

            if (gameState.IsWinDecisionPending)
            {
                if (warnOnBlocked)
                    Warn("Declare or decline win before drawing.");

                LogTurnBlocked(blockedEventName, "WinDecisionPending");
                return false;
            }

            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (playerSeat.HasDrawnTile)
            {
                if (warnOnBlocked)
                    Warn("Already drew this turn. Discard a tile first.");

                LogTurnBlocked(blockedEventName, "DrawnTileExists");
                return false;
            }

            DrawResult result = drawService.DrawTile(seat, gameState, DrawPurpose.TurnDraw);

            if (!result.Success)
            {
                HandleSkillResolutionLogs(result);
                EndRound("WallEmpty");
                return false;
            }

            playerSeat.SetDrawnTile(result.Tile);
            LogTurnDebug(
                completedEventName,
                $"phase={gameState.TurnPhase}; drawnTile={result.Tile}",
                seat: seat,
                tile: result.Tile,
                turnIndex: gameState.TurnIndex);
            HandleSkillResolutionLogs(result);
            NotifyTileDrawn(result);
            LogTileDrawn(result);
            CheckWinPrototype();
            return true;
        }

        public void RequestDiscard(int handIndex)
        {
            if (!CanUseSelfTurnInput("DiscardBlocked"))
                return;

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                LogTurnBlocked("DiscardBlocked", "RoundEnded");
                return;
            }

            SeatId selfSeat = gameState.SelfSeat;
            PlayerSeat selfPlayerSeat = gameState.GetPlayerSeat(selfSeat);
            if (!selfPlayerSeat.HasDrawnTile)
            {
                Warn("Draw before discarding.");
                LogTurnBlocked("DiscardBlocked", "DrawnTileMissing");
                return;
            }

            if (gameState.IsWinDecisionPending)
            {
                Warn("Declare or decline win before discarding.");
                LogTurnBlocked("DiscardBlocked", "WinDecisionPending");
                return;
            }

            DiscardResult result = discardService.DiscardTile(gameState, selfSeat, handIndex);
            if (!result.Success)
            {
                Warn(result.Reason);
                return;
            }

            CommitDrawnTileToHandIfPresent(selfSeat);
            LogTurnDebug(
                "DiscardCompleted",
                $"phase={gameState.TurnPhase}; discardTile={result.Record.Tile}",
                seat: result.Record.ActorSeat,
                tile: result.Record.Tile,
                turnIndex: result.Record.TurnIndex);
            NotifyTileDiscarded(result.Record);
            LogTileDiscarded(result.Record);
            AdvanceTurn();
        }

        public void RequestDiscardDrawnTile()
        {
            if (!CanUseSelfTurnInput("DiscardBlocked"))
                return;

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                LogTurnBlocked("DiscardBlocked", "RoundEnded");
                return;
            }

            SeatId selfSeat = gameState.SelfSeat;
            PlayerSeat selfPlayerSeat = gameState.GetPlayerSeat(selfSeat);
            if (!selfPlayerSeat.HasDrawnTile)
            {
                Warn("Draw before discarding.");
                LogTurnBlocked("DiscardBlocked", "DrawnTileMissing");
                return;
            }

            if (gameState.IsWinDecisionPending)
            {
                Warn("Declare or decline win before discarding.");
                LogTurnBlocked("DiscardBlocked", "WinDecisionPending");
                return;
            }

            DiscardResult result = discardService.DiscardDrawnTile(gameState, selfSeat);
            if (!result.Success)
            {
                Warn(result.Reason);
                return;
            }

            LogTurnDebug(
                "DiscardCompleted",
                $"phase={gameState.TurnPhase}; discardTile={result.Record.Tile}",
                seat: result.Record.ActorSeat,
                tile: result.Record.Tile,
                turnIndex: result.Record.TurnIndex);
            NotifyTileDiscarded(result.Record);
            LogTileDiscarded(result.Record);
            AdvanceTurn();
        }

        public void RequestForceDrawSkill(string targetTileCode)
        {
            if (!CanUseSelfTurnInput("SkillBlocked"))
                return;

            RequestForceDrawSkillForSeat(gameState.SelfSeat, targetTileCode);
        }

        public void RequestForceDrawSkillForSeat(SeatId ownerSeat, string targetTileCode)
        {
            if (!CanUseGameState())
                return;

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                return;
            }

            if (gameState.IsWinDecisionPending)
            {
                Warn("Declare or decline win before activating another skill.");
                return;
            }

            if (!Tile.TryParse(targetTileCode, out Tile targetTile))
            {
                Warn("Invalid target tile. Use 1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C.");
                return;
            }

            if (ownerSeat != gameState.CurrentTurn)
            {
                ReserveForceDrawSkill(ownerSeat, targetTile);
                return;
            }

            ActivateForceDrawSkill(ownerSeat, targetTile, false);
        }

        private void ReserveForceDrawSkill(SeatId ownerSeat, Tile targetTile)
        {
            if (!IsActiveSeat(ownerSeat))
            {
                string reason = "Owner seat is not active.";
                Warn(reason);
                LogSkillReservationRejected(ownerSeat, SkillEffectKind.ForceDrawTile, targetTile, reason);
                return;
            }

            if (gameState.HasActiveSkillEffect(ownerSeat, SkillEffectKind.ForceDrawTile))
            {
                string reason = "Force draw skill is already active.";
                Warn(reason);
                LogSkillReservationRejected(ownerSeat, SkillEffectKind.ForceDrawTile, targetTile, reason);
                return;
            }

            PendingSkillReservation reservation = new PendingSkillReservation(
                ownerSeat,
                SkillEffectKind.ForceDrawTile,
                targetTile,
                gameState.CurrentTurn,
                gameState.TurnIndex);

            if (!skillReservationService.Reserve(reservation, out string reserveReason))
            {
                Warn(reserveReason);
                LogSkillReservationRejected(ownerSeat, SkillEffectKind.ForceDrawTile, targetTile, reserveReason);
                return;
            }

            LogSkillReserved(reservation);
        }

        private bool ActivateForceDrawSkill(SeatId actorSeat, Tile targetTile, bool beforeDraw)
        {
            SkillActivationResult result = skillSystem.ActivateForceDrawTile(
                gameState,
                actorSeat,
                targetTile);

            if (!result.Success)
            {
                Warn(result.Reason);
                if (beforeDraw)
                    LogSkillReservationRejected(actorSeat, SkillEffectKind.ForceDrawTile, targetTile, result.Reason);

                return false;
            }

            NotifySkillActivated(actorSeat, result.Effect);
            if (beforeDraw)
                LogSkillActivatedBeforeDraw(actorSeat, result.Effect);
            else
                LogSkillActivated(actorSeat, result.Effect);

            NotifySkillEffectRegistered(result.Effect);
            LogSkillEffectRegistered(result.Effect);
            return true;
        }

        public void RequestSetAutoSortEnabled(bool enabled)
        {
            if (autoSortEnabled == enabled)
                return;

            autoSortEnabled = enabled;
            LogAutoSortChanged(enabled);

            if (enabled && gameState != null)
                ApplyAutoSort(gameState.SelfSeat, "ToggleEnabled", true);
        }

        public void RequestDeclareWin()
        {
            if (!CanUseGameState())
                return;

            if (!gameState.IsWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return;
            }

            SeatId seat = gameState.WinDecisionSeat;
            int turnIndex = gameState.WinDecisionTurnIndex;
            ClearWinDecision();
            gameState.IsRoundEnded = true;
            LogTurnDebug(
                "RoundEnded",
                $"phase={gameState.TurnPhase}; reason=WinDeclared",
                seat: seat,
                turnIndex: turnIndex);

            NotifyWinDeclared(seat, turnIndex);
            LogWinDeclared(seat, turnIndex);
        }

        public void RequestDeclineWin()
        {
            if (!CanUseGameState())
                return;

            if (!gameState.IsWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return;
            }

            SeatId seat = gameState.WinDecisionSeat;
            int turnIndex = gameState.WinDecisionTurnIndex;
            ClearWinDecision();

            NotifyWinDeclined(seat, turnIndex);
            LogWinDeclined(seat, turnIndex);
        }

        private void DealInitialHands()
        {
            // PROTOTYPE: Deal a fixed starting hand only to active turn seats.
            for (int seatIndex = 0; seatIndex < gameState.ActiveTurnSeats.Count; seatIndex++)
            {
                SeatId seat = gameState.ActiveTurnSeats[seatIndex];
                for (int i = 0; i < initialHandTileCount; i++)
                {
                    DrawResult result = drawService.DrawTile(seat, gameState, DrawPurpose.InitialDeal);
                    if (!result.Success)
                    {
                        EndRound("WallEmptyDuringInitialDeal");
                        return;
                    }

                    gameState.GetPlayerSeat(seat).Hand.Add(result.Tile);
                    NotifyTileDrawn(result);
                    LogTileDrawn(result);
                }
            }

            ApplyAutoSortToSelfHandIfEnabled("InitialDeal");
        }

        private void AdvanceTurn()
        {
            SeatId fromSeat = gameState.CurrentTurn;
            SeatId nextSeat = playerTurnManager.EndTurnAndSelectNext(gameState, gameState.ActiveTurnSeats);
            LogTurnDebug(
                "EndTurn",
                $"from={fromSeat}; to={nextSeat}; phase={gameState.TurnPhase}",
                seat: nextSeat,
                turnIndex: gameState.TurnIndex);
            StartTurn(nextSeat, gameState.TurnIndex);
        }

        private void StartTurn(SeatId seat, int turnIndex)
        {
            NotifyTurnStarted(seat, turnIndex);
            LogTurnStarted(seat, turnIndex);
            LogTurnDebug(
                "BeginTurn",
                $"phase={gameState.TurnPhase}; hasDrawnTile={gameState.GetPlayerSeat(seat).HasDrawnTile}",
                seat: seat,
                turnIndex: turnIndex);
            ResolveReservedSkillBeforeDraw(seat);
            TryAutoDrawAtTurnStart(seat, turnIndex);
        }

        private void ResolveReservedSkillBeforeDraw(SeatId seat)
        {
            if (gameState.IsRoundEnded || gameState.IsWinDecisionPending)
                return;

            if (!skillReservationService.TryConsumeForTurn(seat, out PendingSkillReservation reservation))
                return;

            LogSkillReservationConsumed(reservation);

            switch (reservation.SkillEffectKind)
            {
                case SkillEffectKind.ForceDrawTile:
                    ActivateForceDrawSkill(reservation.OwnerSeat, reservation.TargetTile, true);
                    break;
                default:
                    LogSkillReservationRejected(
                        reservation.OwnerSeat,
                        reservation.SkillEffectKind,
                        reservation.TargetTile,
                        "Unsupported skill reservation.");
                    break;
            }
        }

        private void TryAutoDrawAtTurnStart(SeatId seat, int turnIndex)
        {
            if (!enableAutoDraw)
                return;

            LogTurnDebug(
                "AutoDrawStarted",
                $"phase={gameState.TurnPhase}; hasDrawnTile={gameState.GetPlayerSeat(seat).HasDrawnTile}",
                seat: seat,
                turnIndex: turnIndex);

            TryDrawForSeat(seat, "AutoDrawCompleted", "AutoDrawSkipped", false);
        }

        private void CheckWinPrototype()
        {
            // PROTOTYPE: Check only a simple self-draw win shape.
            IReadOnlyList<Tile> handTiles = BuildWinCheckTiles(gameState.CurrentTurn);
            bool isWin = handWinChecker.CanWinStandardHand(handTiles);
            SetWinDecisionPending(isWin, gameState.CurrentTurn, gameState.TurnIndex);
            eventNotifier?.NotifyWinChecked(gameState.CurrentTurn, gameState.TurnIndex, isWin);

            DevLog.Record(
                "Mahjong",
                "WinChecked",
                isWin
                    ? "isWin=true; standard hand shape complete."
                    : "isWin=false; standard hand shape incomplete.",
                seat: gameState.CurrentTurn,
                hand: GetCurrentHandText(),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex);
        }

        private IReadOnlyList<Tile> BuildWinCheckTiles(SeatId seat)
        {
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            List<Tile> tiles = new List<Tile>(playerSeat.Hand.GetTiles());
            if (playerSeat.DrawnTile.HasValue)
                tiles.Add(playerSeat.DrawnTile.Value);

            return tiles;
        }

        private void CommitDrawnTileToHandIfPresent(SeatId seat)
        {
            PlayerSeat playerSeat = gameState.GetPlayerSeat(seat);
            if (!playerSeat.CommitDrawnTileToHand())
                return;

            ApplyAutoSortIfEnabled(seat, "DrawnTileCommitted", false);
        }

        private void SetWinDecisionPending(bool isPending, SeatId seat, int turnIndex)
        {
            if (gameState == null)
                return;

            if (isPending)
            {
                gameState.BeginWinDecision(seat, turnIndex);
                LogTurnDebug(
                    "WinDecision",
                    $"phase={gameState.TurnPhase}",
                    seat: seat,
                    turnIndex: turnIndex);
                return;
            }

            gameState.ClearWinDecision();
        }

        private void ClearWinDecision()
        {
            SetWinDecisionPending(false, default, 0);
        }

        private void EndRound(string reason)
        {
            gameState.ClearWinDecision();
            gameState.IsRoundEnded = true;
            LogTurnDebug(
                "RoundEnded",
                $"phase={gameState.TurnPhase}; reason={reason}",
                seat: gameState.CurrentTurn,
                turnIndex: gameState.TurnIndex);
            eventNotifier?.NotifyRoundEnded(reason);

            DevLog.Record(
                "GameFlow",
                "RoundEnded",
                reason,
                seat: gameState.CurrentTurn,
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex);
        }

        private void HandleSkillResolutionLogs(DrawResult result)
        {
            if (!result.SkillWasPresent || result.ResolvedSkillEffect == null)
                return;

            ActiveSkillEffect effect = result.ResolvedSkillEffect;
            NotifySkillEffectResolved(result);
            LogSkillEffectResolved(result);

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
                turnIndex: gameState.TurnIndex,
                activeSkill: effect.ToLogText());

            NotifySkillEffectExpired(effect, "ConsumedByDraw");
            LogSkillEffectExpired(effect, "ConsumedByDraw");
        }

        private void CacheReferences()
        {
            if (eventNotifier == null)
                eventNotifier = GetComponent<MahjongEventNotifier>();
        }

        private void NormalizeParticipantCount()
        {
            participantCount = Mathf.Clamp(participantCount, 1, 4);
        }

        private void AssignParticipantsToSeats(SeatId selfSeat)
        {
            gameState.SetSelfSeat(selfSeat);

            if (participantCount <= 1)
                return;

            if (participantCount == 2)
            {
                gameState.AssignPlayerToSeat(PlayerId.Player2, GetRelativeSeat(selfSeat, 2));
                return;
            }

            gameState.AssignPlayerToSeat(PlayerId.Player2, GetRelativeSeat(selfSeat, 1));
            gameState.AssignPlayerToSeat(PlayerId.Player3, GetRelativeSeat(selfSeat, 2));

            if (participantCount >= 4)
                gameState.AssignPlayerToSeat(PlayerId.Player4, GetRelativeSeat(selfSeat, 3));
        }

        private static SeatId GetRelativeSeat(SeatId originSeat, int offset)
        {
            return (SeatId)(((int)originSeat + offset) % 4);
        }

        private SeatId ResolveSelfSeat()
        {
            if (!randomizeSelfSeat)
                return fixedSelfSeat;

            return (SeatId)Random.Range(0, 4);
        }

        private bool IsActiveSeat(SeatId seat)
        {
            if (gameState == null)
                return false;

            for (int i = 0; i < gameState.ActiveTurnSeats.Count; i++)
            {
                if (gameState.ActiveTurnSeats[i] == seat)
                    return true;
            }

            return false;
        }

        private bool CanUseGameState()
        {
            if (gameState != null)
                return true;

            Warn("GameState is not initialized. StartNewRound first.");
            return false;
        }

        private bool CanUseSelfTurnInput(string blockedEventName)
        {
            if (!CanUseGameState())
                return false;

            if (gameState.IsSelfTurn)
                return true;

            Warn("User input is only available during the self player's turn.");
            LogTurnBlocked(blockedEventName, "NotSelfTurn");
            return false;
        }

        private void LogTurnBlocked(string eventName, string reason)
        {
            if (gameState == null)
                return;

            PlayerSeat currentPlayerSeat = gameState.GetPlayerSeat(gameState.CurrentTurn);
            LogTurnDebug(
                eventName,
                $"reason={reason}; phase={gameState.TurnPhase}; hasDrawnTile={currentPlayerSeat.HasDrawnTile}",
                seat: gameState.CurrentTurn,
                turnIndex: gameState.TurnIndex);
        }

        private void LogTurnDebug(
            string eventName,
            string message,
            SeatId? seat = null,
            Tile? tile = null,
            int? turnIndex = null)
        {
            if (!enableTurnDebugLog)
                return;

            DevLog.Record(
                "Turn",
                eventName,
                message,
                seat: seat,
                tile: tile,
                wallCount: gameState == null ? (int?)null : gameState.Wall.Count,
                turnIndex: turnIndex);
        }

        private void NotifyRunStarted()
        {
            if (eventNotifier == null)
            {
                WarnMissingOnce(ref warnedMissingNotifier, "MahjongEventNotifier is not assigned.");
                return;
            }

            eventNotifier.NotifyRunStarted(DevLog.CurrentLogFilePath);
        }

        private void NotifyRoundStarted()
        {
            eventNotifier?.NotifyRoundStarted(gameState.TurnIndex, gameState.Wall.Count);
        }

        private void NotifyRoundSetupCompleted()
        {
            eventNotifier?.NotifyRoundSetupCompleted();
        }

        private void NotifyTurnStarted(SeatId seat, int turnIndex)
        {
            eventNotifier?.NotifyTurnStarted(seat, turnIndex);
        }

        private void NotifyTileDrawn(DrawResult result)
        {
            eventNotifier?.NotifyTileDrawn(result);
        }

        private void NotifyTileDiscarded(DiscardRecord record)
        {
            eventNotifier?.NotifyTileDiscarded(record);
        }

        private void NotifySkillActivated(SeatId actorSeat, ActiveSkillEffect effect)
        {
            eventNotifier?.NotifySkillActivated(actorSeat, effect);
        }

        private void NotifySkillEffectRegistered(ActiveSkillEffect effect)
        {
            eventNotifier?.NotifySkillEffectRegistered(effect);
        }

        private void NotifySkillEffectResolved(DrawResult result)
        {
            eventNotifier?.NotifySkillEffectResolved(result);
        }

        private void NotifySkillEffectExpired(ActiveSkillEffect effect, string reason)
        {
            eventNotifier?.NotifySkillEffectExpired(effect, reason);
        }

        private void NotifyWinDeclared(SeatId seat, int turnIndex)
        {
            eventNotifier?.NotifyWinDeclared(seat, turnIndex);
        }

        private void NotifyWinDeclined(SeatId seat, int turnIndex)
        {
            eventNotifier?.NotifyWinDeclined(seat, turnIndex);
        }

        private void NotifyHandAutoSorted(SeatId seat, int turnIndex)
        {
            eventNotifier?.NotifyHandAutoSorted(seat, turnIndex);
        }

        private void ApplyAutoSortToSelfHandIfEnabled(string reason)
        {
            if (!autoSortEnabled || gameState == null)
                return;

            ApplyAutoSort(gameState.SelfSeat, reason, false);
        }

        private void ApplyAutoSortIfEnabled(SeatId seat, string reason, bool notify)
        {
            if (!autoSortEnabled || gameState == null || !gameState.IsSelfSeat(seat))
                return;

            ApplyAutoSort(seat, reason, notify);
        }

        private void ApplyAutoSort(SeatId seat, string reason, bool notify)
        {
            gameState.GetPlayerSeat(seat).Hand.SortByTypeIndex();
            LogHandAutoSorted(seat, gameState.TurnIndex, reason);

            if (notify)
                NotifyHandAutoSorted(seat, gameState.TurnIndex);
        }

        private void LogRunStarted()
        {
            DevLog.Record(
                "GameFlow",
                "RunStarted",
                $"LogFile={DevLog.CurrentLogFilePath}");
        }

        private void LogRoundStarted()
        {
            DevLog.Record(
                "GameFlow",
                "RoundStarted",
                "Round started.",
                seat: gameState.CurrentTurn,
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex);
        }

        private void LogSeatSlotsAssigned()
        {
            for (int i = 0; i < gameState.SeatSlots.Count; i++)
            {
                SeatSlot slot = gameState.SeatSlots[i];
                DevLog.Record(
                    "GameFlow",
                    "SeatSlotAssigned",
                    $"Seat {slot.Wind} = {GetSeatSlotLogLabel(slot)}",
                    seat: slot.Wind,
                    turnIndex: gameState.TurnIndex);
            }
        }

        private string GetSeatSlotLogLabel(SeatSlot slot)
        {
            if (slot == null || slot.IsEmpty)
                return "Empty";

            return gameState.IsSelfSeat(slot.Wind) ? "Self" : slot.StateLabel;
        }

        private void LogTurnStarted(SeatId seat, int turnIndex)
        {
            DevLog.Record(
                "GameFlow",
                "TurnStarted",
                "Turn started.",
                seat: seat,
                wallCount: gameState.Wall.Count,
                turnIndex: turnIndex);
        }

        private void LogTileDrawn(DrawResult result)
        {
            DevLog.Record(
                "Mahjong",
                "TileDrawn",
                $"source={result.Source}; purpose={result.Purpose}; {result.Message}",
                seat: result.Seat,
                tile: result.Tile,
                hand: GetHandText(result.Seat),
                wallCount: result.WallCountAfterDraw,
                turnIndex: gameState.TurnIndex,
                activeSkill: result.ResolvedSkillEffect != null ? result.ResolvedSkillEffect.ToLogText() : null);
        }

        private void LogTileDiscarded(DiscardRecord record)
        {
            DevLog.Record(
                "Mahjong",
                "TileDiscarded",
                "Tile discarded.",
                seat: record.ActorSeat,
                tile: record.Tile,
                hand: GetHandText(record.ActorSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: record.TurnIndex);
        }

        private void LogSkillActivated(SeatId actorSeat, ActiveSkillEffect effect)
        {
            DevLog.Record(
                "Skill",
                "SkillActivated",
                "Force draw skill activated.",
                seat: actorSeat,
                tile: effect.TargetTile,
                hand: GetHandText(actorSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: effect.ToLogText());
        }

        private void LogSkillEffectRegistered(ActiveSkillEffect effect)
        {
            DevLog.Record(
                "Skill",
                "SkillEffectRegistered",
                "ActiveSkillEffect registered.",
                seat: effect.OwnerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(effect.OwnerSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: effect.ToLogText());
        }

        private void LogSkillReserved(PendingSkillReservation reservation)
        {
            DevLog.Record(
                "Skill",
                "SkillReserved",
                $"skillType={reservation.SkillEffectKind}; reservedOnTurnSeat={reservation.ReservedOnTurnSeat}; reservedTurnIndex={reservation.ReservedTurnIndex}",
                seat: reservation.OwnerSeat,
                tile: reservation.TargetTile,
                hand: GetHandText(reservation.OwnerSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: reservation.ToLogText());
        }

        private void LogSkillActivatedBeforeDraw(SeatId ownerSeat, ActiveSkillEffect effect)
        {
            DevLog.Record(
                "Skill",
                "SkillActivatedBeforeDraw",
                $"skillType={effect.Kind}; currentTurnSeat={gameState.CurrentTurn}",
                seat: ownerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(ownerSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: effect.ToLogText());
        }

        private void LogSkillReservationConsumed(PendingSkillReservation reservation)
        {
            DevLog.Record(
                "Skill",
                "ReservationConsumed",
                $"skillType={reservation.SkillEffectKind}",
                seat: reservation.OwnerSeat,
                tile: reservation.TargetTile,
                hand: GetHandText(reservation.OwnerSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: reservation.ToLogText());
        }

        private void LogSkillReservationRejected(
            SeatId ownerSeat,
            SkillEffectKind skillEffectKind,
            Tile targetTile,
            string reason)
        {
            DevLog.Record(
                "Skill",
                "SkillReservationRejected",
                $"skillType={skillEffectKind}; reason={reason}; currentTurnSeat={gameState.CurrentTurn}",
                seat: ownerSeat,
                tile: targetTile,
                hand: GetHandText(ownerSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: $"{skillEffectKind}:{targetTile}:ReservationRejected");
        }

        private void LogSkillEffectResolved(DrawResult result)
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
                turnIndex: gameState.TurnIndex,
                activeSkill: effect != null ? effect.ToLogText() : null);
        }

        private void LogSkillEffectExpired(ActiveSkillEffect effect, string reason)
        {
            DevLog.Record(
                "Skill",
                "SkillEffectExpired",
                reason,
                seat: effect.OwnerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(effect.OwnerSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex,
                activeSkill: effect.ToLogText());
        }

        private void LogWinDeclared(SeatId seat, int turnIndex)
        {
            DevLog.Record(
                "Mahjong",
                "WinDeclared",
                "Self draw win declared.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: gameState.Wall.Count,
                turnIndex: turnIndex);
        }

        private void LogWinDeclined(SeatId seat, int turnIndex)
        {
            DevLog.Record(
                "Mahjong",
                "WinDeclined",
                "Winning hand declined.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: gameState.Wall.Count,
                turnIndex: turnIndex);
        }

        private void LogHandAutoSorted(SeatId seat, int turnIndex, string reason)
        {
            DevLog.Record(
                "Mahjong",
                "HandAutoSorted",
                $"reason={reason}; hand sorted by TypeIndex.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: gameState.Wall.Count,
                turnIndex: turnIndex);
        }

        private void LogAutoSortChanged(bool enabled)
        {
            if (gameState == null)
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
                seat: gameState.SelfSeat,
                hand: GetHandText(gameState.SelfSeat),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex);
        }

        private string GetCurrentHandText()
        {
            return gameState == null ? string.Empty : GetHandText(gameState.CurrentTurn);
        }

        private string GetHandText(SeatId seat)
        {
            if (gameState == null)
                return string.Empty;

            return gameState.GetPlayerSeat(seat).Hand.ToDisplayString();
        }

        private void Warn(string message)
        {
            if (!logWarnings)
                return;

            Debug.LogWarning($"{nameof(MahjongGameFlow)}: {message}", this);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Warn(message);
        }
    }
}
