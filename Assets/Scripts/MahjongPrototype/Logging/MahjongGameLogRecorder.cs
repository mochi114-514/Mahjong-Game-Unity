using System.Collections.Generic;
using System.Text;
using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using MahjongPrototype.Skills;
using UnityEngine;

namespace MahjongPrototype.Logging
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Logging/Mahjong Game Log Recorder")]
    public sealed class MahjongGameLogRecorder : MonoBehaviour
    {
        [SerializeField] private MahjongGameFlow gameFlow;
        [SerializeField] private MahjongEventNotifier eventNotifier;

        [Header("Dev Log")]
        [SerializeField] private bool enableDevLog = true;
        [SerializeField] private bool enableReleaseBuildLogging = false;
        [SerializeField] private bool enableTurnDebugLog;

        private bool isSubscribed;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (isSubscribed || eventNotifier == null)
                return;

            eventNotifier.RunStarted += HandleRunStarted;
            eventNotifier.RoundStarted += HandleRoundStarted;
            eventNotifier.TurnStarted += HandleTurnStarted;
            eventNotifier.TileDrawn += HandleTileDrawn;
            eventNotifier.TileDiscarded += HandleTileDiscarded;
            eventNotifier.ReactionWindowStarted += HandleReactionWindowStarted;
            eventNotifier.ReactionWindowAnswered += HandleReactionWindowAnswered;
            eventNotifier.ReactionWindowResolved += HandleReactionWindowResolved;
            eventNotifier.ReactionWindowClosed += HandleReactionWindowClosed;
            eventNotifier.RoundEnded += HandleRoundEnded;
            eventNotifier.SeatSlotsAssigned += HandleSeatSlotsAssigned;
            eventNotifier.TurnDebug += HandleTurnDebug;
            eventNotifier.WinCheckedDetailed += HandleWinCheckedDetailed;
            eventNotifier.WinDeclaredEvaluated += HandleWinDeclaredEvaluated;
            eventNotifier.WinDeclinedDetailed += HandleWinDeclinedDetailed;
            eventNotifier.SkillActivatedDetailed += HandleSkillActivatedDetailed;
            eventNotifier.SkillEffectRegistered += HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved += HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired += HandleSkillEffectExpired;
            eventNotifier.SkillReserved += HandleSkillReserved;
            eventNotifier.SkillReservationConsumed += HandleSkillReservationConsumed;
            eventNotifier.SkillReservationRejected += HandleSkillReservationRejected;
            eventNotifier.AutoSortChanged += HandleAutoSortChanged;
            eventNotifier.HandAutoSortedDetailed += HandleHandAutoSortedDetailed;
            isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!isSubscribed || eventNotifier == null)
                return;

            eventNotifier.RunStarted -= HandleRunStarted;
            eventNotifier.RoundStarted -= HandleRoundStarted;
            eventNotifier.TurnStarted -= HandleTurnStarted;
            eventNotifier.TileDrawn -= HandleTileDrawn;
            eventNotifier.TileDiscarded -= HandleTileDiscarded;
            eventNotifier.ReactionWindowStarted -= HandleReactionWindowStarted;
            eventNotifier.ReactionWindowAnswered -= HandleReactionWindowAnswered;
            eventNotifier.ReactionWindowResolved -= HandleReactionWindowResolved;
            eventNotifier.ReactionWindowClosed -= HandleReactionWindowClosed;
            eventNotifier.RoundEnded -= HandleRoundEnded;
            eventNotifier.SeatSlotsAssigned -= HandleSeatSlotsAssigned;
            eventNotifier.TurnDebug -= HandleTurnDebug;
            eventNotifier.WinCheckedDetailed -= HandleWinCheckedDetailed;
            eventNotifier.WinDeclaredEvaluated -= HandleWinDeclaredEvaluated;
            eventNotifier.WinDeclinedDetailed -= HandleWinDeclinedDetailed;
            eventNotifier.SkillActivatedDetailed -= HandleSkillActivatedDetailed;
            eventNotifier.SkillEffectRegistered -= HandleSkillEffectRegistered;
            eventNotifier.SkillEffectResolved -= HandleSkillEffectResolved;
            eventNotifier.SkillEffectExpired -= HandleSkillEffectExpired;
            eventNotifier.SkillReserved -= HandleSkillReserved;
            eventNotifier.SkillReservationConsumed -= HandleSkillReservationConsumed;
            eventNotifier.SkillReservationRejected -= HandleSkillReservationRejected;
            eventNotifier.AutoSortChanged -= HandleAutoSortChanged;
            eventNotifier.HandAutoSortedDetailed -= HandleHandAutoSortedDetailed;
            isSubscribed = false;
        }

        private void HandleRunStarted(string logFilePath)
        {
            DevLog.Initialize(enableDevLog, enableReleaseBuildLogging);
            DevLog.Record(
                "GameFlow",
                "RunStarted",
                $"LogFile={DevLog.CurrentLogFilePath}");
        }

        private void HandleRoundStarted(int turnIndex, int wallCount)
        {
            MahjongGameState state = GetCurrentState();
            DevLog.Record(
                "GameFlow",
                "RoundStarted",
                "Round started.",
                seat: state != null ? state.CurrentTurn : (SeatId?)null,
                wallCount: wallCount,
                turnIndex: turnIndex);
        }

        private void HandleTurnStarted(SeatId seat, int turnIndex)
        {
            DevLog.Record(
                "GameFlow",
                "TurnStarted",
                "Turn started.",
                seat: seat,
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleTileDrawn(DrawResult result)
        {
            DevLog.Record(
                "Mahjong",
                "TileDrawn",
                $"source={result.Source}; purpose={result.Purpose}; {result.Message}",
                seat: result.Seat,
                tile: result.Tile,
                hand: GetHandText(result.Seat),
                wallCount: result.WallCountAfterDraw,
                turnIndex: GetTurnIndex(),
                activeSkill: result.ResolvedSkillEffect != null ? result.ResolvedSkillEffect.ToLogText() : null);
        }

        private void HandleTileDiscarded(DiscardRecord record)
        {
            DevLog.Record(
                "Mahjong",
                "TileDiscarded",
                "Tile discarded.",
                seat: record.ActorSeat,
                tile: record.Tile,
                hand: GetHandText(record.ActorSeat),
                wallCount: GetWallCount(),
                turnIndex: record.TurnIndex);
        }

        private void HandleReactionWindowStarted(ReactionWindow reactionWindow)
        {
            if (reactionWindow == null)
                return;

            DevLog.Record(
                "Reaction",
                "ReactionWindowStarted",
                $"windowId={reactionWindow.WindowId}; sourceDiscardId={reactionWindow.SourceDiscard.Id}; candidates={reactionWindow.Candidates.Count}",
                seat: reactionWindow.SourceDiscard.ActorSeat,
                tile: reactionWindow.SourceDiscard.Tile,
                wallCount: GetWallCount(),
                turnIndex: reactionWindow.TurnIndex);
        }

        private void HandleReactionWindowAnswered(ReactionWindowAnswerResult result)
        {
            if (!result.Accepted || result.Candidate == null)
                return;

            DevLog.Record(
                "Reaction",
                "ReactionWindowAnswered",
                $"windowId={result.WindowId}; sourceDiscardId={result.Resolution.SourceDiscard.Id}; reaction={result.Candidate.Kind}; answer={result.Candidate.ResponseState}",
                seat: result.Candidate.Seat,
                tile: result.Resolution.SourceDiscard.Tile,
                wallCount: GetWallCount(),
                turnIndex: result.Resolution.SourceDiscard.TurnIndex);
        }

        private void HandleReactionWindowResolved(ReactionWindowResolution resolution)
        {
            if (!resolution.IsResolved)
                return;

            DevLog.Record(
                "Reaction",
                "ReactionWindowResolved",
                $"resolution={resolution.Type}; sourceDiscardId={resolution.SourceDiscard.Id}; caller={resolution.Candidate?.Seat}; openMeld={resolution.OpenMeld?.Type}",
                seat: resolution.SourceDiscard.ActorSeat,
                tile: resolution.SourceDiscard.Tile,
                wallCount: GetWallCount(),
                turnIndex: resolution.SourceDiscard.TurnIndex);
        }

        private void HandleReactionWindowClosed(int windowId)
        {
            DevLog.Record(
                "Reaction",
                "ReactionWindowClosed",
                $"windowId={windowId}",
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex());
        }

        private void HandleRoundEnded(string reason)
        {
            MahjongGameState state = GetCurrentState();
            DevLog.Record(
                "GameFlow",
                "RoundEnded",
                reason,
                seat: state != null ? state.CurrentTurn : (SeatId?)null,
                wallCount: state != null ? state.Wall.Count : (int?)null,
                turnIndex: state != null ? state.TurnIndex : (int?)null);
        }

        private void HandleSeatSlotsAssigned()
        {
            MahjongGameState state = GetCurrentState();
            if (state == null)
                return;

            for (int i = 0; i < state.SeatSlots.Count; i++)
            {
                SeatSlot slot = state.SeatSlots[i];
                DevLog.Record(
                    "GameFlow",
                    "SeatSlotAssigned",
                    $"Seat {slot.Wind} = {GetSeatSlotLogLabel(state, slot)}",
                    seat: slot.Wind,
                    turnIndex: state.TurnIndex);
            }
        }

        private void HandleTurnDebug(
            string eventName,
            string message,
            SeatId? seat,
            Tile? tile,
            int? turnIndex)
        {
            if (!enableTurnDebugLog)
                return;

            DevLog.Record(
                "Turn",
                eventName,
                message,
                seat: seat,
                tile: tile,
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleWinCheckedDetailed(
            SeatId seat,
            WinType winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            bool isWin)
        {
            DevLog.Record(
                "Mahjong",
                "WinChecked",
                GetWinCheckedMessage(winType, sourceSeat, isWin),
                seat: seat,
                tile: winningTile,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleWinDeclaredEvaluated(
            SeatId seat,
            WinType? winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            int turnIndex,
            WinDeclarationEvaluationResult evaluationResult)
        {
            string message = BuildWinDeclaredEvaluationText(
                seat,
                winType,
                winningTile,
                sourceSeat,
                evaluationResult);

            DevLog.Record(
                "Mahjong",
                "WinDeclared",
                message,
                seat: seat,
                tile: winningTile,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message, this);
#endif
        }

        private void HandleWinDeclinedDetailed(SeatId seat, WinType? winType, int turnIndex)
        {
            DevLog.Record(
                "Mahjong",
                "WinDeclined",
                $"winType={winType}; winning hand declined.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private void HandleSkillActivatedDetailed(
            SeatId actorSeat,
            ActiveSkillEffect effect,
            bool beforeDraw)
        {
            if (beforeDraw)
            {
                DevLog.Record(
                    "Skill",
                    "SkillActivatedBeforeDraw",
                    $"skillType={effect.Kind}; currentTurnSeat={GetCurrentTurnText()}",
                    seat: actorSeat,
                    tile: effect.TargetTile,
                    hand: GetHandText(actorSeat),
                    wallCount: GetWallCount(),
                    turnIndex: GetTurnIndex(),
                    activeSkill: effect.ToLogText());
                return;
            }

            DevLog.Record(
                "Skill",
                "SkillActivated",
                "Force draw skill activated.",
                seat: actorSeat,
                tile: effect.TargetTile,
                hand: GetHandText(actorSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillEffectRegistered(ActiveSkillEffect effect)
        {
            DevLog.Record(
                "Skill",
                "SkillEffectRegistered",
                "ActiveSkillEffect registered.",
                seat: effect.OwnerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(effect.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillEffectResolved(DrawResult result)
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
                turnIndex: GetTurnIndex(),
                activeSkill: effect != null ? effect.ToLogText() : null);

            if (effect == null)
                return;

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
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillEffectExpired(ActiveSkillEffect effect, string reason)
        {
            DevLog.Record(
                "Skill",
                "SkillEffectExpired",
                reason,
                seat: effect.OwnerSeat,
                tile: effect.TargetTile,
                hand: GetHandText(effect.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: effect.ToLogText());
        }

        private void HandleSkillReserved(PendingSkillReservation reservation)
        {
            DevLog.Record(
                "Skill",
                "SkillReserved",
                $"skillType={reservation.SkillEffectKind}; reservedOnTurnSeat={reservation.ReservedOnTurnSeat}; reservedTurnIndex={reservation.ReservedTurnIndex}",
                seat: reservation.OwnerSeat,
                tile: reservation.TargetTile,
                hand: GetHandText(reservation.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: reservation.ToLogText());
        }

        private void HandleSkillReservationConsumed(PendingSkillReservation reservation)
        {
            DevLog.Record(
                "Skill",
                "ReservationConsumed",
                $"skillType={reservation.SkillEffectKind}",
                seat: reservation.OwnerSeat,
                tile: reservation.TargetTile,
                hand: GetHandText(reservation.OwnerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: reservation.ToLogText());
        }

        private void HandleSkillReservationRejected(
            SeatId ownerSeat,
            SkillEffectKind skillEffectKind,
            Tile targetTile,
            string reason)
        {
            DevLog.Record(
                "Skill",
                "SkillReservationRejected",
                $"skillType={skillEffectKind}; reason={reason}; currentTurnSeat={GetCurrentTurnText()}",
                seat: ownerSeat,
                tile: targetTile,
                hand: GetHandText(ownerSeat),
                wallCount: GetWallCount(),
                turnIndex: GetTurnIndex(),
                activeSkill: $"{skillEffectKind}:{targetTile}:ReservationRejected");
        }

        private void HandleAutoSortChanged(bool enabled)
        {
            MahjongGameState state = GetCurrentState();
            if (state == null)
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
                seat: state.SelfSeat,
                hand: GetHandText(state.SelfSeat),
                wallCount: state.Wall.Count,
                turnIndex: state.TurnIndex);
        }

        private void HandleHandAutoSortedDetailed(SeatId seat, int turnIndex, string reason)
        {
            DevLog.Record(
                "Mahjong",
                "HandAutoSorted",
                $"reason={reason}; hand sorted by TypeIndex.",
                seat: seat,
                hand: GetHandText(seat),
                wallCount: GetWallCount(),
                turnIndex: turnIndex);
        }

        private static string BuildWinDeclaredEvaluationText(
            SeatId seat,
            WinType? winType,
            Tile? winningTile,
            SeatId? sourceSeat,
            WinDeclarationEvaluationResult evaluationResult)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[和了結果]");
            builder.AppendLine("和了者=" + seat);
            builder.AppendLine("和了方法=" + FormatWinType(winType));
            builder.AppendLine("和了牌=" + FormatTile(winningTile));

            if (winType == WinType.Ron && sourceSeat.HasValue)
                builder.AppendLine("放銃者=" + sourceSeat.Value);

            if (evaluationResult == null)
            {
                builder.Append("役=評価結果なし");
                return builder.ToString();
            }

            HandEvaluationResult handEvaluation = evaluationResult.HandEvaluationResult;
            if (handEvaluation == null)
            {
                builder.Append("役=評価結果なし");
                return builder.ToString();
            }

            if (AppendCandidateResults(builder, handEvaluation.CandidateResults, winningTile))
                return builder.ToString();

            if (AppendLegacyYakuResult(builder, handEvaluation))
                return builder.ToString();

            builder.Append("役=役なし");
            return builder.ToString();
        }

        private static bool AppendCandidateResults(
            StringBuilder builder,
            IReadOnlyList<HandEvaluationCandidateResult> candidateResults,
            Tile? winningTile)
        {
            if (candidateResults == null || candidateResults.Count == 0)
                return false;

            int displayedCount = 0;
            for (int i = 0; i < candidateResults.Count; i++)
            {
                HandEvaluationCandidateResult candidateResult = candidateResults[i];
                if (candidateResult == null || !candidateResult.HasYaku)
                    continue;

                displayedCount++;
                AppendCandidateResult(
                    builder,
                    displayedCount,
                    candidateResult,
                    winningTile);
            }

            return displayedCount > 0;
        }

        private static void AppendCandidateResult(
            StringBuilder builder,
            int displayIndex,
            HandEvaluationCandidateResult candidateResult,
            Tile? winningTile)
        {
            HandEvaluationCandidate candidate = candidateResult.Candidate;
            builder.Append("成立候補");
            builder.Append(displayIndex);
            builder.Append(": 形=");
            builder.Append(FormatCandidateType(candidate));
            builder.Append(" / 待ち=");
            builder.Append(FormatCandidateWait(candidate, winningTile));
            builder.Append(" / 役=");
            builder.Append(FormatYakuList(candidateResult.Yakus));
            builder.Append(" / 合計=");
            builder.Append(FormatTotalHan(candidateResult.HasYakuman, candidateResult.TotalHan));
            builder.AppendLine();
        }

        private static bool AppendLegacyYakuResult(
            StringBuilder builder,
            HandEvaluationResult handEvaluation)
        {
            if (handEvaluation.Yakus == null || handEvaluation.Yakus.Count == 0)
                return false;

            builder.Append("成立候補1: 形=旧評価 / 待ち=不明 / 役=");
            builder.Append(FormatYakuList(handEvaluation.Yakus));
            builder.Append(" / 合計=");
            builder.Append(FormatTotalHan(handEvaluation.HasYakuman, handEvaluation.TotalHan));
            return true;
        }

        private static string FormatYakuList(IReadOnlyList<EvaluatedYaku> yakus)
        {
            if (yakus == null || yakus.Count == 0)
                return "なし";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < yakus.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                EvaluatedYaku yaku = yakus[i];
                builder.Append(yaku.DisplayName);
                builder.Append(yaku.IsYakuman ? "(役満)" : "(" + (int)yaku.Han + "翻)");
            }

            return builder.ToString();
        }

        private static string FormatTotalHan(bool hasYakuman, int totalHan)
        {
            return hasYakuman ? "役満" : totalHan + "翻";
        }

        private static string FormatCandidateType(HandEvaluationCandidate candidate)
        {
            if (candidate == null)
                return "不明";

            switch (candidate.Type)
            {
                case HandEvaluationCandidateType.Standard:
                    return "通常形";
                case HandEvaluationCandidateType.SevenPairs:
                    return "七対子";
                case HandEvaluationCandidateType.ThirteenOrphans:
                    return "国士無双";
                default:
                    return "不明";
            }
        }

        private static string FormatCandidateWait(
            HandEvaluationCandidate candidate,
            Tile? winningTile)
        {
            if (candidate == null)
                return "不明";

            switch (candidate.Type)
            {
                case HandEvaluationCandidateType.Standard:
                    return FormatWaitType(
                        candidate.StandardInterpretation != null
                            ? candidate.StandardInterpretation.WaitType
                            : WaitType.None);
                case HandEvaluationCandidateType.SevenPairs:
                    return "単騎";
                case HandEvaluationCandidateType.ThirteenOrphans:
                    return FormatThirteenOrphansWait(
                        candidate.ThirteenOrphansAnalysis,
                        winningTile);
                default:
                    return "不明";
            }
        }

        private static string FormatWaitType(WaitType waitType)
        {
            switch (waitType)
            {
                case WaitType.Ryanmen:
                    return "両面";
                case WaitType.Kanchan:
                    return "嵌張";
                case WaitType.Penchan:
                    return "辺張";
                case WaitType.Tanki:
                    return "単騎";
                case WaitType.Shanpon:
                    return "双碰";
                default:
                    return "不明";
            }
        }

        private static string FormatThirteenOrphansWait(
            ThirteenOrphansAnalysis analysis,
            Tile? winningTile)
        {
            if (analysis == null ||
                !analysis.IsWin ||
                !analysis.PairTile.IsValid ||
                !winningTile.HasValue ||
                !winningTile.Value.IsValid)
            {
                return "国士無双";
            }

            return winningTile.Value == analysis.PairTile
                ? "国士十三面"
                : "国士単騎";
        }

        private static string FormatWinType(WinType? winType)
        {
            if (!winType.HasValue)
                return "不明";

            switch (winType.Value)
            {
                case WinType.Tsumo:
                    return "ツモ";
                case WinType.Ron:
                    return "ロン";
                default:
                    return "不明";
            }
        }

        private static string FormatTile(Tile? tile)
        {
            return tile.HasValue && tile.Value.IsValid
                ? tile.Value.Code
                : "不明";
        }

        private string GetSeatSlotLogLabel(MahjongGameState state, SeatSlot slot)
        {
            if (slot == null || slot.IsEmpty)
                return "Empty";

            return state.IsSelfSeat(slot.Wind)
                ? $"Self:{slot.ParticipantType}"
                : $"{slot.StateLabel}:{slot.ParticipantType}";
        }

        private static string GetWinCheckedMessage(WinType winType, SeatId? sourceSeat, bool isWin)
        {
            if (winType == WinType.Tsumo)
            {
                return isWin
                    ? "winType=Tsumo; isWin=true; standard hand shape complete."
                    : "winType=Tsumo; isWin=false; standard hand shape incomplete.";
            }

            return $"winType={winType}; sourceSeat={sourceSeat}; isWin={isWin}";
        }

        private string GetCurrentTurnText()
        {
            MahjongGameState state = GetCurrentState();
            return state != null ? state.CurrentTurn.ToString() : string.Empty;
        }

        private void CacheReferences()
        {
            if (gameFlow == null)
                gameFlow = GetComponent<MahjongGameFlow>();

            if (gameFlow == null)
                gameFlow = GetComponentInParent<MahjongGameFlow>();

            if (gameFlow == null && transform.root != null)
                gameFlow = transform.root.GetComponentInChildren<MahjongGameFlow>(true);

            if (eventNotifier == null && gameFlow != null)
                eventNotifier = gameFlow.EventNotifier;

            if (eventNotifier == null)
                eventNotifier = GetComponent<MahjongEventNotifier>();

            if (eventNotifier == null)
                eventNotifier = GetComponentInParent<MahjongEventNotifier>();

            if (eventNotifier == null && transform.root != null)
                eventNotifier = transform.root.GetComponentInChildren<MahjongEventNotifier>(true);
        }

        private MahjongGameState GetCurrentState()
        {
            return gameFlow != null ? gameFlow.CurrentState : null;
        }

        private int? GetWallCount()
        {
            MahjongGameState state = GetCurrentState();
            return state != null ? state.Wall.Count : (int?)null;
        }

        private int? GetTurnIndex()
        {
            MahjongGameState state = GetCurrentState();
            return state != null ? state.TurnIndex : (int?)null;
        }

        private string GetHandText(SeatId seat)
        {
            MahjongGameState state = GetCurrentState();
            return state == null ? string.Empty : state.GetPlayerSeat(seat).Hand.ToDisplayString();
        }
    }
}
