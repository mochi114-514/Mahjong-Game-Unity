using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MahjongPrototype.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MahjongPrototype.Logging
{
    public static class DevLog
    {
        private const int MaxRecentLines = 120;

        private static readonly List<string> RecentLinesInternal = new List<string>(MaxRecentLines);
        private static readonly List<string> RecentDisplayLinesInternal = new List<string>(MaxRecentLines);
        private static bool isInitialized;
        private static bool isEnabled;
        private static bool enableInReleaseBuild;

        public static event Action<string> LineWritten;
        public static event Action<string> DisplayLineWritten;

        public static string CurrentLogFilePath { get; private set; }
        public static IReadOnlyList<string> RecentLines => RecentLinesInternal;
        public static IReadOnlyList<string> RecentDisplayLines => RecentDisplayLinesInternal;

        public static void Initialize(bool enableLog, bool allowReleaseBuildLogging)
        {
            enableInReleaseBuild = allowReleaseBuildLogging;
            isEnabled = enableLog && IsLoggingAllowed();
            isInitialized = true;
            RecentLinesInternal.Clear();
            RecentDisplayLinesInternal.Clear();
            CurrentLogFilePath = string.Empty;

            if (!isEnabled)
                return;

            string directory = Path.Combine(Application.persistentDataPath, "DevLogs");
            Directory.CreateDirectory(directory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            CurrentLogFilePath = Path.Combine(directory, $"mahjong_prototype_{timestamp}.jsonl");
        }

        public static void EnsureInitialized(bool enableLog, bool allowReleaseBuildLogging)
        {
            if (isInitialized)
                return;

            Initialize(enableLog, allowReleaseBuildLogging);
        }

        public static void Record(
            string category,
            string eventName,
            string message = null,
            SeatId? seat = null,
            Tile? tile = null,
            string hand = null,
            int? wallCount = null,
            int? turnIndex = null,
            string activeSkill = null,
            string stackTrace = null)
        {
            if (!isInitialized)
                Initialize(true, false);

            if (!isEnabled)
                return;

            MahjongLogEntry entry = new MahjongLogEntry
            {
                Time = DateTime.Now.ToString("o"),
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                Category = category,
                EventName = eventName,
                Message = message,
                Seat = seat.HasValue ? seat.Value.ToString() : null,
                Tile = tile.HasValue ? tile.Value.ToString() : null,
                Hand = hand,
                WallCount = wallCount,
                TurnIndex = turnIndex,
                ActiveSkill = activeSkill,
                StackTrace = stackTrace
            };

            string line = MahjongLogFormatter.FormatJsonLine(entry);
            string displayLine = MahjongLogFormatter.FormatDisplayLine(entry);
            WriteLine(line, displayLine);
        }

        private static void WriteLine(string line, string displayLine)
        {
            if (string.IsNullOrEmpty(CurrentLogFilePath))
                return;

            using (StreamWriter writer = new StreamWriter(CurrentLogFilePath, true, Encoding.UTF8))
            {
                writer.WriteLine(line);
            }

            RecentLinesInternal.Add(line);
            if (RecentLinesInternal.Count > MaxRecentLines)
                RecentLinesInternal.RemoveAt(0);

            if (!string.IsNullOrEmpty(displayLine))
            {
                RecentDisplayLinesInternal.Add(displayLine);
                if (RecentDisplayLinesInternal.Count > MaxRecentLines)
                    RecentDisplayLinesInternal.RemoveAt(0);

                DisplayLineWritten?.Invoke(displayLine);
            }

            LineWritten?.Invoke(line);
        }

        private static bool IsLoggingAllowed()
        {
            if (enableInReleaseBuild)
                return true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
