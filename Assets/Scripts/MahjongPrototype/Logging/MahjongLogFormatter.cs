using System.Text;

namespace MahjongPrototype.Logging
{
    public static class MahjongLogFormatter
    {
        public static string FormatJsonLine(MahjongLogEntry entry)
        {
            StringBuilder builder = new StringBuilder(256);
            builder.Append('{');

            AppendString(builder, "time", entry.Time, true);
            AppendNumber(builder, "frame", entry.Frame);
            AppendString(builder, "scene", entry.Scene);
            AppendString(builder, "category", entry.Category);
            AppendString(builder, "eventName", entry.EventName);
            AppendString(builder, "message", entry.Message);
            AppendString(builder, "seat", entry.Seat);
            AppendString(builder, "tile", entry.Tile);
            AppendString(builder, "hand", entry.Hand);
            AppendNullableNumber(builder, "wallCount", entry.WallCount);
            AppendNullableNumber(builder, "turnIndex", entry.TurnIndex);
            AppendString(builder, "activeSkill", entry.ActiveSkill);
            AppendString(builder, "stackTrace", entry.StackTrace);

            builder.Append('}');
            return builder.ToString();
        }

        public static string FormatDisplayLine(MahjongLogEntry entry)
        {
            if (entry == null)
                return string.Empty;

            string eventName = entry.EventName ?? string.Empty;
            switch (eventName)
            {
                case "TurnStarted":
                    return TrimLine($"TurnStarted {entry.Seat} turn={entry.TurnIndex} wall={entry.WallCount}");

                case "BeginTurn":
                    return TrimLine($"BeginTurn {entry.Seat} T{entry.TurnIndex} {entry.Message}");

                case "DrawCompleted":
                    return TrimLine($"DrawDone {entry.Seat} T{entry.TurnIndex} {entry.Tile} {entry.Message}");

                case "AutoDrawStarted":
                    return TrimLine($"AutoDrawStart {entry.Seat} T{entry.TurnIndex} {entry.Message}");

                case "AutoDrawCompleted":
                    return TrimLine($"AutoDrawDone {entry.Seat} T{entry.TurnIndex} {entry.Tile} {entry.Message}");

                case "AutoDrawSkipped":
                    return TrimLine($"AutoDrawSkip {entry.Message}");

                case "DiscardCompleted":
                    return TrimLine($"DiscardDone {entry.Seat} T{entry.TurnIndex} {entry.Tile} {entry.Message}");

                case "EndTurn":
                    return TrimLine($"EndTurn T{entry.TurnIndex} {entry.Message}");

                case "DrawBlocked":
                    return TrimLine($"DrawBlocked {entry.Message}");

                case "DiscardBlocked":
                    return TrimLine($"DiscardBlocked {entry.Message}");

                case "TileDrawn":
                    if (!string.IsNullOrEmpty(entry.ActiveSkill))
                        return string.Empty;

                    return TrimLine($"Draw {entry.Tile}");

                case "TileDiscarded":
                    return TrimLine($"Discard {entry.Tile} turn={entry.TurnIndex}");

                case "SkillActivated":
                    return TrimLine($"Skill target={entry.Tile}");

                case "SkillEffectRegistered":
                    return string.Empty;

                case "SkillEffectResolved":
                    return string.Empty;

                case "DrawModifiedBySkill":
                    if (!string.IsNullOrEmpty(entry.Message) &&
                        entry.Message.IndexOf("Fell back", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TrimLine($"SkillFallback {entry.Tile}");
                    }

                    return TrimLine($"SkillDraw {entry.Tile}");

                case "SkillEffectExpired":
                    return string.Empty;

                case "RoundStarted":
                    return TrimLine($"RoundStarted wall={entry.WallCount}");

                case "RoundEnded":
                    return TrimLine($"RoundEnded {entry.Message}");

                case "WinChecked":
                    return TrimLine($"WinChecked {entry.Message}");

                case "WinDeclared":
                    return TrimLine("WinDeclared self-draw");

                case "WinDeclined":
                    return TrimLine("WinDeclined");

                case "WinDecision":
                    return TrimLine($"WinDecision {entry.Seat} {entry.Message}");

                case "HandAutoSorted":
                    return string.Empty;

                case "AutoSortEnabled":
                    return TrimLine("AutoSort ON");

                case "AutoSortDisabled":
                    return TrimLine("AutoSort OFF");

                case "SlowFrame":
                    return TrimLine("SlowFrame");

                case "Unity Console Warning":
                    return TrimLine($"Warning {entry.Message}");

                case "Unity Console Error":
                    return TrimLine($"Error {entry.Message}");

                case "Unity Console Exception":
                    return TrimLine($"Exception {entry.Message}");

                case "RunStarted":
                    return "RunStarted";

                default:
                    return TrimLine($"{entry.EventName} {entry.Message}");
            }
        }

        private static void AppendString(StringBuilder builder, string name, string value, bool first = false)
        {
            if (string.IsNullOrEmpty(value))
                return;

            AppendCommaIfNeeded(builder, first);
            builder.Append('"').Append(Escape(name)).Append("\":\"");
            builder.Append(Escape(value)).Append('"');
        }

        private static void AppendNumber(StringBuilder builder, string name, int value)
        {
            AppendCommaIfNeeded(builder, false);
            builder.Append('"').Append(Escape(name)).Append("\":").Append(value);
        }

        private static void AppendNullableNumber(StringBuilder builder, string name, int? value)
        {
            if (!value.HasValue)
                return;

            AppendCommaIfNeeded(builder, false);
            builder.Append('"').Append(Escape(name)).Append("\":").Append(value.Value);
        }

        private static void AppendCommaIfNeeded(StringBuilder builder, bool first)
        {
            if (first)
                return;

            if (builder.Length > 1)
                builder.Append(',');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string TrimLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string line = value.Trim();
            const int maxLength = 64;
            if (line.Length <= maxLength)
                return line;

            return line.Substring(0, maxLength - 1) + "...";
        }
    }
}
