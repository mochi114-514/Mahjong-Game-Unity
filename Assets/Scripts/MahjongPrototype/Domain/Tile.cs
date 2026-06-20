using System;

namespace MahjongPrototype.Domain
{
    public enum TileSuit
    {
        None = 0,
        Man = 1,
        Pin = 2,
        Sou = 3
    }

    public enum HonorKind
    {
        None = 0,
        East = 1,
        South = 2,
        West = 3,
        North = 4,
        White = 5,
        Green = 6,
        Red = 7
    }

    public readonly struct Tile : IEquatable<Tile>
    {
        public Tile(string code)
        {
            if (!TryParse(code, out Tile tile))
                throw new ArgumentException($"Invalid tile code: {code}", nameof(code));

            Suit = tile.Suit;
            Rank = tile.Rank;
            Honor = tile.Honor;
        }

        private Tile(TileSuit suit, int rank, HonorKind honor)
        {
            Suit = suit;
            Rank = rank;
            Honor = honor;
        }

        public TileSuit Suit { get; }
        public int Rank { get; }
        public HonorKind Honor { get; }
        public bool IsValid => IsNumberTile || IsHonorTile;
        public bool IsNumberTile => IsNumberSuit(Suit) && Rank >= 1 && Rank <= 9 && Honor == HonorKind.None;
        public bool IsHonorTile => Suit == TileSuit.None && Rank == 0 && Honor != HonorKind.None;
        public string Code => IsValid ? FormatCode() : string.Empty;

        public static Tile CreateNumber(TileSuit suit, int rank)
        {
            if (!IsNumberSuit(suit))
                throw new ArgumentException($"Invalid number tile suit: {suit}", nameof(suit));

            if (rank < 1 || rank > 9)
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Number tile rank must be 1-9.");

            return new Tile(suit, rank, HonorKind.None);
        }

        public static Tile CreateHonor(HonorKind honor)
        {
            if (honor == HonorKind.None)
                throw new ArgumentException("Honor tile kind must not be None.", nameof(honor));

            return new Tile(TileSuit.None, 0, honor);
        }

        public static bool TryParse(string input, out Tile tile)
        {
            tile = default;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            string code = input.Trim();
            if (code.Length == 1)
            {
                if (!TryParseHonorCode(char.ToUpperInvariant(code[0]), out HonorKind honor))
                    return false;

                tile = CreateHonor(honor);
                return true;
            }

            if (code.Length != 2)
                return false;

            char rankCode = code[0];
            if (rankCode < '1' || rankCode > '9')
                return false;

            if (!TryParseSuitCode(char.ToLowerInvariant(code[1]), out TileSuit suit))
                return false;

            // PROTOTYPE: 赤ドラは未対応。0m/0p/0s は受け付けない。
            tile = CreateNumber(suit, rankCode - '0');
            return true;
        }

        public static bool IsValidCode(string input)
        {
            return TryParse(input, out _);
        }

        public override string ToString()
        {
            // PROTOTYPE: UI/log still use code text until a dedicated formatter/resolver is introduced.
            return Code;
        }

        public bool Equals(Tile other)
        {
            return Suit == other.Suit && Rank == other.Rank && Honor == other.Honor;
        }

        public override bool Equals(object obj)
        {
            return obj is Tile other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)Suit;
                hash = hash * 31 + Rank;
                hash = hash * 31 + (int)Honor;
                return hash;
            }
        }

        public static bool operator ==(Tile left, Tile right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Tile left, Tile right)
        {
            return !left.Equals(right);
        }

        private string FormatCode()
        {
            if (IsNumberTile)
                return $"{Rank}{FormatSuitCode(Suit)}";

            if (IsHonorTile)
                return FormatHonorCode(Honor).ToString();

            return string.Empty;
        }

        private static bool IsNumberSuit(TileSuit suit)
        {
            return suit == TileSuit.Man || suit == TileSuit.Pin || suit == TileSuit.Sou;
        }

        private static bool TryParseSuitCode(char code, out TileSuit suit)
        {
            switch (code)
            {
                case 'm':
                    suit = TileSuit.Man;
                    return true;
                case 'p':
                    suit = TileSuit.Pin;
                    return true;
                case 's':
                    suit = TileSuit.Sou;
                    return true;
                default:
                    suit = TileSuit.None;
                    return false;
            }
        }

        private static bool TryParseHonorCode(char code, out HonorKind honor)
        {
            switch (code)
            {
                case 'E':
                    honor = HonorKind.East;
                    return true;
                case 'S':
                    honor = HonorKind.South;
                    return true;
                case 'W':
                    honor = HonorKind.West;
                    return true;
                case 'N':
                    honor = HonorKind.North;
                    return true;
                case 'P':
                    honor = HonorKind.White;
                    return true;
                case 'F':
                    honor = HonorKind.Green;
                    return true;
                case 'C':
                    honor = HonorKind.Red;
                    return true;
                default:
                    honor = HonorKind.None;
                    return false;
            }
        }

        private static char FormatSuitCode(TileSuit suit)
        {
            switch (suit)
            {
                case TileSuit.Man:
                    return 'm';
                case TileSuit.Pin:
                    return 'p';
                case TileSuit.Sou:
                    return 's';
                default:
                    return '?';
            }
        }

        private static char FormatHonorCode(HonorKind honor)
        {
            switch (honor)
            {
                case HonorKind.East:
                    return 'E';
                case HonorKind.South:
                    return 'S';
                case HonorKind.West:
                    return 'W';
                case HonorKind.North:
                    return 'N';
                case HonorKind.White:
                    return 'P';
                case HonorKind.Green:
                    return 'F';
                case HonorKind.Red:
                    return 'C';
                default:
                    return '?';
            }
        }
    }
}
