using System.Collections.Generic;
using MahjongPrototype.Domain;
using MahjongPrototype.Logging;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;

namespace MahjongPrototype
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Mahjong Game Flow")]
    public sealed class MahjongGameFlow : MonoBehaviour
    {
        [Header("Prototype Seats")]
        [SerializeField] private SeatId viewerSeat = SeatId.East;
        [SerializeField] private List<SeatId> initialActiveSeats = new List<SeatId> { SeatId.East };

        [Header("Round Setup")]
        [SerializeField, Min(1)] private int initialHandTileCount = 13;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool useFixedRandomSeed = false;
        [SerializeField] private int fixedRandomSeed = 12345;

        [Header("Scene References")]
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Dev Log")]
        [SerializeField] private bool enableDevLog = true;
        [SerializeField] private bool enableReleaseBuildLogging = false;

        [Header("Warnings")]
        [SerializeField] private bool logWarnings = true;

        private readonly TurnOrderService turnOrderService = new TurnOrderService();
        private readonly DrawService drawService = new DrawService();
        private readonly DiscardService discardService = new DiscardService();
        private readonly HandWinChecker handWinChecker = new HandWinChecker();
        private readonly SkillSystem skillSystem = new SkillSystem();

        private MahjongGameState gameState;
        private bool isWinDecisionPending;
        private SeatId pendingWinSeat;
        private int pendingWinTurnIndex;
        private bool warnedMissingNotifier;

        public MahjongGameState CurrentState => gameState;
        public MahjongEventNotifier EventNotifier => eventNotifier;
        public SeatId ViewerSeat => viewerSeat;
        public bool IsWinDecisionPending => isWinDecisionPending;

        private void Reset()
        {
            CacheReferences();
            NormalizeInitialActiveSeats();
        }

        private void Awake()
        {
            CacheReferences();
            NormalizeInitialActiveSeats();
        }

        private void Start()
        {
            if (autoStart)
                StartNewRound();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            initialHandTileCount = Mathf.Max(1, initialHandTileCount);
            NormalizeInitialActiveSeats();
        }
#endif

        [ContextMenu("Prototype/Start New Round")]
        public void StartNewRound()
        {
            CacheReferences();
            NormalizeInitialActiveSeats();

            DevLog.Initialize(enableDevLog, enableReleaseBuildLogging);
            ClearWinDecision();
            NotifyRunStarted();
            LogRunStarted();

            int? seed = useFixedRandomSeed ? fixedRandomSeed : (int?)null;
            gameState = new MahjongGameState(Wall.CreateStandardShuffled(seed), initialActiveSeats);

            NotifyRoundStarted();
            LogRoundStarted();

            DealInitialHands();
            StartTurn(gameState.CurrentSeat, gameState.TurnIndex);
        }

        public void RetryPrototype()
        {
            // PROTOTYPE: シーン再読み込みではなく、現在のFlow内状態だけを初期化する。
            StartNewRound();
        }

        public void RequestDraw()
        {
            if (!CanUseGameState())
                return;

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                return;
            }

            if (gameState.HasDrawnThisTurn)
            {
                Warn("Already drew this turn. Discard a tile first.");
                return;
            }

            DrawResult result = drawService.DrawTile(gameState.CurrentSeat, gameState, DrawPurpose.TurnDraw);
            HandleSkillResolutionLogs(result);

            if (!result.Success)
            {
                EndRound("WallEmpty");
                return;
            }

            gameState.HasDrawnThisTurn = true;
            NotifyTileDrawn(result);
            LogTileDrawn(result);
            CheckWinPrototype();
        }

        public void RequestDiscard(int handIndex)
        {
            if (!CanUseGameState())
                return;

            if (gameState.IsRoundEnded)
            {
                Warn("Round already ended. Press Retry.");
                return;
            }

            if (!gameState.HasDrawnThisTurn)
            {
                Warn("Draw before discarding.");
                return;
            }

            if (isWinDecisionPending)
            {
                Warn("Declare or decline win before discarding.");
                return;
            }

            DiscardResult result = discardService.DiscardTile(gameState, gameState.CurrentSeat, handIndex);
            if (!result.Success)
            {
                Warn(result.Reason);
                return;
            }

            gameState.HasDrawnThisTurn = false;
            NotifyTileDiscarded(result.Record);
            LogTileDiscarded(result.Record);
            AdvanceTurn();
        }

        public void RequestForceDrawSkill(string targetTileCode)
        {
            if (!CanUseGameState())
                return;

            if (isWinDecisionPending)
            {
                Warn("Declare or decline win before activating another skill.");
                return;
            }

            if (!Tile.TryParse(targetTileCode, out Tile targetTile))
            {
                Warn("Invalid target tile. Use 1m-9m, 1p-9p, 1s-9s, E/S/W/N/P/F/C.");
                return;
            }

            SkillActivationResult result = skillSystem.ActivateForceDrawTile(
                gameState,
                gameState.CurrentSeat,
                targetTile);

            if (!result.Success)
            {
                Warn(result.Reason);
                return;
            }

            NotifySkillActivated(gameState.CurrentSeat, result.Effect);
            LogSkillActivated(gameState.CurrentSeat, result.Effect);
            NotifySkillEffectRegistered(result.Effect);
            LogSkillEffectRegistered(result.Effect);
        }

        public void RequestSortHand()
        {
            if (!CanUseGameState())
                return;

            SeatId seat = gameState.CurrentSeat;
            gameState.GetPlayerSeat(seat).Hand.SortByTypeIndex();
            NotifyHandSorted(seat, gameState.TurnIndex);
            LogHandSorted(seat, gameState.TurnIndex);
        }

        public void RequestDeclareWin()
        {
            if (!CanUseGameState())
                return;

            if (!isWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return;
            }

            SeatId seat = pendingWinSeat;
            int turnIndex = pendingWinTurnIndex;
            ClearWinDecision();

            NotifyWinDeclared(seat, turnIndex);
            LogWinDeclared(seat, turnIndex);
        }

        public void RequestDeclineWin()
        {
            if (!CanUseGameState())
                return;

            if (!isWinDecisionPending)
            {
                Warn("No winning hand decision is pending.");
                return;
            }

            SeatId seat = pendingWinSeat;
            int turnIndex = pendingWinTurnIndex;
            ClearWinDecision();

            NotifyWinDeclined(seat, turnIndex);
            LogWinDeclined(seat, turnIndex);
        }

        private void DealInitialHands()
        {
            // PROTOTYPE: 最初はactiveSeatsだけに固定枚数を配る。正式な配牌順や親処理は後回し。
            for (int seatIndex = 0; seatIndex < gameState.ActiveSeats.Count; seatIndex++)
            {
                SeatId seat = gameState.ActiveSeats[seatIndex];
                for (int i = 0; i < initialHandTileCount; i++)
                {
                    DrawResult result = drawService.DrawTile(seat, gameState, DrawPurpose.InitialDeal);
                    if (!result.Success)
                    {
                        EndRound("WallEmptyDuringInitialDeal");
                        return;
                    }

                    NotifyTileDrawn(result);
                    LogTileDrawn(result);
                }
            }

            gameState.HasDrawnThisTurn = false;
        }

        private void AdvanceTurn()
        {
            SeatId nextSeat = turnOrderService.GetNextSeat(gameState.ActiveSeats, gameState.CurrentSeat);
            gameState.CurrentSeat = nextSeat;
            gameState.TurnIndex++;
            StartTurn(nextSeat, gameState.TurnIndex);
        }

        private void StartTurn(SeatId seat, int turnIndex)
        {
            NotifyTurnStarted(seat, turnIndex);
            LogTurnStarted(seat, turnIndex);
        }

        private void CheckWinPrototype()
        {
            // PROTOTYPE: 役判定、点数計算、ロン、鳴き面子はまだ扱わない。
            IReadOnlyList<Tile> handTiles = gameState.GetPlayerSeat(gameState.CurrentSeat).Hand.GetTiles();
            bool isWin = handWinChecker.CanWinStandardHand(handTiles);
            SetWinDecisionPending(isWin, gameState.CurrentSeat, gameState.TurnIndex);
            eventNotifier?.NotifyWinChecked(gameState.CurrentSeat, gameState.TurnIndex, isWin);

            DevLog.Record(
                "Mahjong",
                "WinChecked",
                isWin
                    ? "isWin=true; standard hand shape complete."
                    : "isWin=false; standard hand shape incomplete.",
                seat: gameState.CurrentSeat,
                hand: GetCurrentHandText(),
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex);
        }

        private void SetWinDecisionPending(bool isPending, SeatId seat, int turnIndex)
        {
            isWinDecisionPending = isPending;
            pendingWinSeat = isPending ? seat : default;
            pendingWinTurnIndex = isPending ? turnIndex : 0;
        }

        private void ClearWinDecision()
        {
            SetWinDecisionPending(false, default, 0);
        }

        private void EndRound(string reason)
        {
            gameState.IsRoundEnded = true;
            eventNotifier?.NotifyRoundEnded(reason);

            DevLog.Record(
                "GameFlow",
                "RoundEnded",
                reason,
                seat: gameState.CurrentSeat,
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

        private void NormalizeInitialActiveSeats()
        {
            if (initialActiveSeats == null)
                initialActiveSeats = new List<SeatId>();

            if (initialActiveSeats.Count <= 0)
                initialActiveSeats.Add(SeatId.East);
        }

        private bool CanUseGameState()
        {
            if (gameState != null)
                return true;

            Warn("GameState is not initialized. StartNewRound first.");
            return false;
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

        private void NotifyHandSorted(SeatId seat, int turnIndex)
        {
            eventNotifier?.NotifyHandSorted(seat, turnIndex);
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
                seat: gameState.CurrentSeat,
                wallCount: gameState.Wall.Count,
                turnIndex: gameState.TurnIndex);
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

        private void LogHandSorted(SeatId seat, int turnIndex)
        {
            DevLog.Record(
                "Mahjong",
                "HandSorted",
                "Hand sorted by TypeIndex.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: gameState.Wall.Count,
                turnIndex: turnIndex);
        }

        private string GetCurrentHandText()
        {
            return gameState == null ? string.Empty : GetHandText(gameState.CurrentSeat);
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
