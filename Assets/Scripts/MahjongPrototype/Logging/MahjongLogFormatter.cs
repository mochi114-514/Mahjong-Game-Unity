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
    }
}
