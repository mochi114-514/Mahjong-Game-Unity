using System;

namespace MahjongPrototype.Domain
{
    public sealed class StandardWinningInterpretation
    {
        public StandardWinningInterpretation(
            StandardHandDecomposition decomposition,
            Tile winningTile,
            WinningTilePlacement placement)
        {
            if (decomposition == null)
                throw new ArgumentNullException(nameof(decomposition));

            if (!winningTile.IsValid)
                throw new ArgumentException("Winning tile must be valid.", nameof(winningTile));

            if (placement == null)
                throw new ArgumentNullException(nameof(placement));

            if (!IsPlacementInDecomposition(decomposition, placement))
                throw new ArgumentException("Placement does not match the decomposition.", nameof(placement));

            Decomposition = decomposition;
            WinningTile = winningTile;
            Placement = placement;
        }

        public StandardHandDecomposition Decomposition { get; }
        public Tile WinningTile { get; }
        public WinningTilePlacement Placement { get; }
        public WaitType WaitType => Placement.WaitType;

        private static bool IsPlacementInDecomposition(
            StandardHandDecomposition decomposition,
            WinningTilePlacement placement)
        {
            if (placement.Type == WinningTilePlacementType.Pair)
                return placement.TargetMeldIndex == WinningTilePlacement.NoTargetMeldIndex;

            if (placement.Type != WinningTilePlacementType.Meld ||
                placement.TargetMeldIndex < 0 ||
                placement.TargetMeldIndex >= decomposition.Melds.Count)
            {
                return false;
            }

            return AreMeldsEquivalent(
                decomposition.Melds[placement.TargetMeldIndex],
                placement.TargetMeld);
        }

        private static bool AreMeldsEquivalent(HandMeld left, HandMeld right)
        {
            if (left == null || right == null || left.Type != right.Type)
                return false;

            if (left.Tiles.Count != right.Tiles.Count)
                return false;

            for (int i = 0; i < left.Tiles.Count; i++)
            {
                if (left.Tiles[i] != right.Tiles[i])
                    return false;
            }

            return true;
        }
    }
}
