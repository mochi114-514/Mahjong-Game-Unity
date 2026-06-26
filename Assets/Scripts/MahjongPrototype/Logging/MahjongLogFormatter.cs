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
                    return TrimLine($"Turn {entry.Seat} {FormatWallCount(entry)}");

                case "BeginTurn":
                case "DrawCompleted":
                case "AutoDrawStarted":
                case "AutoDrawCompleted":
                case "AutoDrawSkipped":
                case "DiscardCompleted":
                case "EndTurn":
                case "DrawBlocked":
                case "DiscardBlocked":
                case "WinDecision":
                    return string.Empty;

                case "TileDrawn":
                    if (!string.IsNullOrEmpty(entry.ActiveSkill))
                        return string.Empty;

                    return TrimLine($"{entry.Seat} Draw {entry.Tile}");

                case "TileDiscarded":
                    return TrimLine($"{entry.Seat} Discard {entry.Tile}");

                case "SkillActivated":
                    return TrimLine($"{entry.Seat} Skill {entry.Tile}");

                case "SkillReserved":
                    return TrimLine($"{entry.Seat} SkillReserve {entry.Tile}");

                case "SkillActivatedBeforeDraw":
                    return TrimLine($"{entry.Seat} SkillBeforeDraw {entry.Tile}");

                case "ReservationConsumed":
                    return string.Empty;

                case "SkillReservationRejected":
                    return TrimLine($"{entry.Seat} SkillRejected {entry.Tile}");

                case "SkillEffectRegistered":
                case "SkillEffectResolved":
                case "SkillEffectExpired":
                    return string.Empty;

                case "DrawModifiedBySkill":
                    if (!string.IsNullOrEmpty(entry.Message) &&
                        entry.Message.IndexOf("Fell back", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return TrimLine($"{entry.Seat} SkillFallback {entry.Tile}");
                    }

                    return TrimLine($"{entry.Seat} SkillDraw {entry.Tile}");

                case "RoundStarted":
                    return TrimLine($"RoundStarted {FormatWallCount(entry)}");

                case "RoundEnded":
                    return TrimLine($"RoundEnded {GetMessageValue(entry.Message, "reason", entry.Message)}");

                case "WinChecked":
                    return FormatWinCheckedLine(entry);

                case "WinDeclared":
                    return FormatWinResultLine(entry, string.Empty);

                case "WinDeclined":
                    return FormatWinResultLine(entry, "Decline ");

                case "HandAutoSorted":
                    return string.Empty;

                case "AutoSortEnabled":
                    return TrimLine("AutoSort ON");

                case "AutoSortDisabled":
                    return TrimLine("AutoSort OFF");

                case "SlowFrame":
                    return string.Empty;

                case "Unity Console Warning":
                    return TrimLine($"Warning {entry.Message}");

                case "Unity Console Error":
                    return TrimLine($"Error {entry.Message}");

                case "Unity Console Exception":
                    return TrimLine($"Exception {entry.Message}");

                case "RunStarted":
                    return string.Empty;

                default:
                    return string.Empty;
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

        private static string FormatWallCount(MahjongLogEntry entry)
        {
            return entry != null && entry.WallCount.HasValue ? $"W{entry.WallCount.Value}" : string.Empty;
        }

        private static string FormatWinCheckedLine(MahjongLogEntry entry)
        {
            string isWin = GetMessageValue(entry.Message, "isWin", string.Empty);
            if (!IsTrueText(isWin))
                return string.Empty;

            string winType = GetMessageValue(entry.Message, "winType", "Win");
            string sourceSeat = GetMessageValue(entry.Message, "sourceSeat", string.Empty);
            if (string.IsNullOrEmpty(sourceSeat) || sourceSeat == "null")
                return TrimLine($"WinCheck {entry.Seat} {winType}");

            return TrimLine($"WinCheck {entry.Seat} {winType} from {sourceSeat}");
        }

        private static string FormatWinResultLine(MahjongLogEntry entry, string actionPrefix)
        {
            string winType = GetMessageValue(entry.Message, "winType", "Win");
            return TrimLine($"{entry.Seat} {actionPrefix}{winType}");
        }

        private static string GetMessageValue(string message, string key, string fallback)
        {
            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(key))
                return fallback;

            string[] parts = message.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                int separatorIndex = part.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                string partKey = part.Substring(0, separatorIndex).Trim();
                if (!string.Equals(partKey, key, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string value = part.Substring(separatorIndex + 1).Trim();
                return string.IsNullOrEmpty(value) ? fallback : value;
            }

            return fallback;
        }

        private static bool IsTrueText(string value)
        {
            return string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);
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
