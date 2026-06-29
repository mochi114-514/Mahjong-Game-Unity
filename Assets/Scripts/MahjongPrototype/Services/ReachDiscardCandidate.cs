using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public readonly struct ReachDiscardCandidate
    {
        public ReachDiscardCandidate(DiscardSource source, int handIndex, Tile tile)
        {
            Source = source;
            HandIndex = handIndex;
            Tile = tile;
        }

        public DiscardSource Source { get; }
        public int HandIndex { get; }
        public Tile Tile { get; }
    }
}
