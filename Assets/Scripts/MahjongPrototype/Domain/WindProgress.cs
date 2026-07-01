using System;

namespace MahjongPrototype.Domain
{
    public readonly struct WindProgress : IEquatable<WindProgress>
    {
        public static WindProgress East1 => new WindProgress(RoundWind.East, 1);

        public WindProgress(RoundWind roundWind, int handNumber)
        {
            if (handNumber < 1 || handNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(handNumber), handNumber, "Hand number must be 1-4.");

            RoundWind = roundWind;
            HandNumber = handNumber;
        }

        public RoundWind RoundWind { get; }
        public int HandNumber { get; }

        public bool TryGetNext(out WindProgress next)
        {
            if (HandNumber < 4)
            {
                next = new WindProgress(RoundWind, HandNumber + 1);
                return true;
            }

            if (RoundWind == RoundWind.East)
            {
                next = new WindProgress(RoundWind.South, 1);
                return true;
            }

            next = this;
            return false;
        }

        public bool Equals(WindProgress other)
        {
            return RoundWind == other.RoundWind && HandNumber == other.HandNumber;
        }

        public override bool Equals(object obj)
        {
            return obj is WindProgress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)RoundWind * 397) ^ HandNumber;
            }
        }

        public override string ToString()
        {
            return $"{RoundWind} {HandNumber}";
        }
    }
}
