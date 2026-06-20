using UnityEngine;

namespace MahjongPrototype.Logging
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/Unity Console Log Bridge")]
    public sealed class UnityConsoleLogBridge : MonoBehaviour
    {
        [Header("Console Capture")]
        [SerializeField] private bool enableConsoleCapture = true;
        [SerializeField] private bool captureWarnings = true;
        [SerializeField] private bool captureErrors = true;
        [SerializeField] private bool captureExceptions = true;

        [Header("Slow Frame")]
        [SerializeField] private bool enableSlowFrameLog = true;
        [SerializeField, Min(1f)] private float slowFrameThresholdMs = 50f;

        [Header("DevLog")]
        [SerializeField] private bool enableDevLogIfFlowHasNotStarted = true;
        [SerializeField] private bool enableReleaseBuildLogging = false;

        private void OnEnable()
        {
            DevLog.EnsureInitialized(enableDevLogIfFlowHasNotStarted, enableReleaseBuildLogging);

            if (enableConsoleCapture)
                Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDisable()
        {
            if (enableConsoleCapture)
                Application.logMessageReceived -= HandleUnityLog;
        }

        private void Update()
        {
            if (!enableSlowFrameLog)
                return;

            float frameMs = Time.unscaledDeltaTime * 1000f;
            if (frameMs < slowFrameThresholdMs)
                return;

            DevLog.Record(
                "Performance",
                "SlowFrame",
                $"Slow frame detected: {frameMs:0.0} ms",
                stackTrace: null);
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    if (!captureWarnings)
                        return;

                    DevLog.Record("UnityConsole", "Unity Console Warning", condition, stackTrace: stackTrace);
                    return;

                case LogType.Error:
                case LogType.Assert:
                    if (!captureErrors)
                        return;

                    DevLog.Record("UnityConsole", "Unity Console Error", condition, stackTrace: stackTrace);
                    return;

                case LogType.Exception:
                    if (!captureExceptions)
                        return;

                    DevLog.Record("UnityConsole", "Unity Console Exception", condition, stackTrace: stackTrace);
                    return;
            }
        }
    }
}
