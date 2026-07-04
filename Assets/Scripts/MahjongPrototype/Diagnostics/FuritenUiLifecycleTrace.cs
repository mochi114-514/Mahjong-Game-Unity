#if UNITY_EDITOR
using System;
using System.IO;
using System.Runtime.CompilerServices;
using MahjongPrototype.Domain;
using MahjongPrototype.Services;
using MahjongPrototype.UI;
using UnityEngine;

namespace MahjongPrototype.Diagnostics
{
    public static class FuritenUiLifecycleTrace
    {
        private const string Prefix = "[FuritenUiLifecycleTrace]";
        private static int sequence;
        private static bool enabled;

        public static string LogPath
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, "Temp", "FuritenUiLifecycleTrace.log");
            }
        }

        public static void Reset(string label)
        {
            enabled = true;
            sequence = 0;
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath, string.Empty);
            Log(label);
        }

        public static void Disable(string label)
        {
            Log(label);
            enabled = false;
        }

        public static void Log(string label)
        {
            if (!enabled)
                return;

            Write($"{Next()} label={label}");
        }

        public static void LogSnapshot(
            string label,
            object gameFlowObject,
            GameObject uiRoot = null,
            Component uiManager = null,
            Component furitenController = null,
            GameObject furitenTextObject = null)
        {
            if (!enabled)
                return;

            MahjongGameFlow gameFlow = gameFlowObject as MahjongGameFlow;
            MahjongGameState state = gameFlow != null ? gameFlow.CurrentState : null;
            Write($"{Next()} label={label} {DescribeUi(uiRoot, uiManager, furitenController, furitenTextObject)} {DescribeState(gameFlow, state)}");
        }

        public static void LogFuritenEvaluation(
            string label,
            MahjongGameFlow gameFlow,
            MahjongGameState state,
            FuritenEvaluationResultSet resultSet,
            bool shouldShow)
        {
            if (!enabled)
                return;

            Write($"{Next()} label={label} state={DescribeStateId(state)} {DescribeFuritenResult(state, resultSet)} shouldShow={shouldShow}");
        }

        public static void LogSetVisible(
            string label,
            Component controller,
            GameObject textObject,
            bool visible)
        {
            if (!enabled)
                return;

            Write(
                $"{Next()} label={label} controller={DescribeComponent(controller)} " +
                $"text={DescribeGameObject(textObject)} visibleArg={visible}");
        }

        private static string Next()
        {
            sequence++;
            return $"{Prefix} seq={sequence} frame={Time.frameCount}";
        }

        private static void Write(string message)
        {
            File.AppendAllText(LogPath, message + Environment.NewLine);
        }

        private static string DescribeUi(
            GameObject uiRoot,
            Component uiManager,
            Component furitenController,
            GameObject furitenTextObject)
        {
            return
                $"uiRoot={DescribeGameObject(uiRoot)} " +
                $"uiManager={DescribeComponent(uiManager)} " +
                $"furitenController={DescribeComponent(furitenController)} " +
                $"furitenText={DescribeGameObject(furitenTextObject)}";
        }

        private static string DescribeState(MahjongGameFlow gameFlow, MahjongGameState state)
        {
            if (gameFlow == null)
                return "gameFlow=null state=null";

            if (state == null)
                return "gameFlow=present state=null";

            PlayerSeat selfSeat = state.GetPlayerSeat(state.SelfSeat);
            int selfFiveManDiscards = 0;
            for (int i = 0; i < state.Discards.Count; i++)
            {
                DiscardRecord record = state.Discards[i];
                if (record.ActorSeat == state.SelfSeat && record.Tile.ToString() == "5m")
                    selfFiveManDiscards++;
            }

            FuritenEvaluationResultSet resultSet = gameFlow.EvaluateAllFuriten();
            return
                $"state={DescribeStateId(state)} selfSeat={state.SelfSeat} currentTurn={state.CurrentTurn} " +
                $"turnIndex={state.TurnIndex} isRoundEnded={state.IsRoundEnded} " +
                $"handCount={selfSeat.Hand.Count} hand=\"{selfSeat.Hand.ToDisplayString()}\" hasDrawnTile={selfSeat.HasDrawnTile} " +
                $"discardCount={state.Discards.Count} self5mDiscards={selfFiveManDiscards} " +
                DescribeFuritenResult(state, resultSet);
        }

        private static string DescribeFuritenResult(
            MahjongGameState state,
            FuritenEvaluationResultSet resultSet)
        {
            if (state == null || resultSet == null)
                return "furitenResultSet=null selfResult=false isEvaluated=false isTenpai=false isDiscardFuriten=false isFuriten=false";

            bool hasResult = resultSet.TryGet(state.SelfSeat, out FuritenSeatEvaluationResult result);
            return
                $"furitenResultSet=present selfResult={hasResult} " +
                $"isEvaluated={(hasResult && result.IsEvaluated)} " +
                $"isTenpai={(hasResult && result.IsTenpai)} " +
                $"isDiscardFuriten={(hasResult && result.IsDiscardFuriten)} " +
                $"isFuriten={(hasResult && result.IsFuriten)}";
        }

        private static string DescribeStateId(MahjongGameState state)
        {
            return state == null ? "null" : RuntimeHelpers.GetHashCode(state).ToString();
        }

        private static string DescribeComponent(Component component)
        {
            if (component == null)
                return "null";

            string enabledText = component is Behaviour behaviour ? behaviour.enabled.ToString() : "n/a";
            return $"{component.GetType().Name}#{component.GetInstanceID()} enabled={enabledText} go={DescribeGameObject(component.gameObject)}";
        }

        private static string DescribeGameObject(GameObject gameObject)
        {
            return gameObject == null
                ? "null"
                : $"{gameObject.name}#{gameObject.GetInstanceID()} activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy}";
        }
    }
}
#endif
