using System;

namespace MahjongPrototype.Domain
{
    public readonly struct Tile : IEquatable<Tile>
    {
        public Tile(string code)
        {
            if (!IsValidCode(code))
                throw new ArgumentException($"Invalid tile code: {code}", nameof(code));

            Code = NormalizeCode(code);
        }

        public string Code { get; }

        public static bool TryParse(string input, out Tile tile)
        {
            tile = default;

            if (!IsValidCode(input))
                return false;

            tile = new Tile(NormalizeCode(input));
            return true;
        }

        public static bool IsValidCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string code = input.Trim();
            if (code.Length == 1)
                return IsHonorCode(char.ToUpperInvariant(code[0]));

            if (code.Length != 2)
                return false;

            char number = code[0];
            char suit = char.ToLowerInvariant(code[1]);
            // PROTOTYPE: 赤ドラは未対応。0m/0p/0s は受け付けない。
            return number >= '1' && number <= '9' &&
                   (suit == 'm' || suit == 'p' || suit == 's');
        }

        public override string ToString()
        {
            return Code ?? string.Empty;
        }

        public bool Equals(Tile other)
        {
            return string.Equals(Code, other.Code, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is Tile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Code == null ? 0 : StringComparer.Ordinal.GetHashCode(Code);
        }

        public static bool operator ==(Tile left, Tile right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tile left, Tile right)
        {
            return !left.Equals(right);
        }

        private static bool IsHonorCode(char code)
        {
            return code == 'E' || code == 'S' || code == 'W' || code == 'N' ||
                   code == 'P' || code == 'F' || code == 'C';
        }

        private static string NormalizeCode(string input)
        {
            string code = input.Trim();
            if (code.Length == 1)
                return char.ToUpperInvariant(code[0]).ToString();

            return $"{code[0]}{char.ToLowerInvariant(code[1])}";
        }
    }
}
