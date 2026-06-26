using MahjongPrototype.Domain;
using MahjongPrototype.Notifications;
using MahjongPrototype.Services;
using UnityEngine;

namespace MahjongPrototype.Logging
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Logging/Mahjong Game Log Recorder")]
    public sealed class MahjongGameLogRecorder : MonoBehaviour
    {
        [SerializeField] private MahjongGameFlow gameFlow;
        [SerializeField] private MahjongEventNotifier eventNotifier;

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
            eventNotifier.RoundEnded += HandleRoundEnded;
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
            eventNotifier.RoundEnded -= HandleRoundEnded;
            isSubscribed = false;
        }

        private void HandleRunStarted(string logFilePath)
        {
            DevLog.Record(
                "GameFlow",
                "RunStarted",
                $"LogFile={logFilePath}");
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
