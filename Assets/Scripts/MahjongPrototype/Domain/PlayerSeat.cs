using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class PlayerSeat
    {
        private Tile? drawnTile;
        private readonly List<PlayerMeld> melds = new List<PlayerMeld>();

        public PlayerSeat(SeatId seatId)
        {
            SeatId = seatId;
            Hand = new Hand();
        }

        public SeatId SeatId { get; }
        public Hand Hand { get; }
        public bool HasDrawnTile => drawnTile.HasValue;
        public Tile? DrawnTile => drawnTile;
        public bool IsReachDeclared { get; private set; }
        public bool IsDoubleReachDeclared { get; private set; }
        public int ReachDeclaredTurnIndex { get; private set; }
        public bool IsIppatsuEligible { get; private set; }
        public bool IsTemporaryFuriten { get; private set; }
        public bool IsReachPassFuriten { get; private set; }
        public IReadOnlyList<PlayerMeld> Melds => melds;
        public bool IsClosed
        {
            get
            {
                for (int i = 0; i < melds.Count; i++)
                {
                    if (melds[i].IsOpen)
                        return false;
                }

                return true;
            }
        }

        public void SetDrawnTile(Tile tile)
        {
            if (!tile.IsValid)
                throw new ArgumentException("Drawn tile must be valid.", nameof(tile));

            if (drawnTile.HasValue)
                throw new InvalidOperationException("Drawn tile already exists.");

            drawnTile = tile;
        }

        public bool TryTakeDrawnTile(out Tile tile)
        {
            if (!drawnTile.HasValue)
            {
                tile = default;
                return false;
            }

            tile = drawnTile.Value;
            drawnTile = null;
            return true;
        }

        public bool CommitDrawnTileToHand()
        {
            if (!TryTakeDrawnTile(out Tile tile))
                return false;

            Hand.Add(tile);
            return true;
        }

        public void ClearDrawnTile()
        {
            drawnTile = null;
        }

        public void DeclareReach(int turnIndex)
        {
            DeclareReach(turnIndex, false);
        }

        public void DeclareReach(int turnIndex, bool isDoubleReachDeclared)
        {
            IsReachDeclared = true;
            IsDoubleReachDeclared = isDoubleReachDeclared;
            ReachDeclaredTurnIndex = turnIndex;
            IsIppatsuEligible = true;
        }

        public void ClearIppatsuEligibility()
        {
            IsIppatsuEligible = false;
        }

        public void MarkTemporaryFuriten()
        {
            if (IsReachPassFuriten)
                return;

            IsTemporaryFuriten = true;
        }

        public void MarkReachPassFuriten()
        {
            IsReachPassFuriten = true;
            IsTemporaryFuriten = false;
        }

        public void ClearTemporaryFuriten()
        {
            IsTemporaryFuriten = false;
        }

        public void AddMeld(PlayerMeld meld)
        {
            if (meld == null)
                throw new ArgumentNullException(nameof(meld));
            if (!CanAddMeld(meld))
                throw new InvalidOperationException("Meld owner seat must match its player seat.");

            melds.Add(meld);
        }

        public bool CanAddMeld(PlayerMeld meld)
        {
            return meld != null && meld.OwnerSeat == SeatId;
        }

        internal bool TryCommitAnkan(Tile tile, PlayerMeld meld)
        {
            if (!tile.IsValid || meld == null || meld.Type != PlayerMeldType.Ankan ||
                !CanAddMeld(meld) || !drawnTile.HasValue)
            {
                return false;
            }

            Tile existingDrawnTile = drawnTile.Value;
            int matchingHandTileCount = Hand.CountTilesByValue(tile);
            int matchingLogicalTileCount = matchingHandTileCount +
                (existingDrawnTile == tile ? 1 : 0);
            if (matchingLogicalTileCount != 4)
                return false;

            int handTilesToRemove = existingDrawnTile == tile ? 3 : 4;
            if (matchingHandTileCount != handTilesToRemove ||
                !Hand.TryRemoveTilesByValue(tile, handTilesToRemove))
            {
                return false;
            }

            drawnTile = null;
            if (existingDrawnTile != tile)
                Hand.Add(existingDrawnTile);

            melds.Add(meld);
            return true;
        }

        internal bool TryCommitKakan(
            int sourcePonMeldIndex,
            Tile tile,
            SelfKanTileLocation addedTileLocation,
            out PlayerMeld meld)
        {
            meld = null;
            if (!tile.IsValid || sourcePonMeldIndex < 0 ||
                sourcePonMeldIndex >= melds.Count || !drawnTile.HasValue)
            {
                return false;
            }

            PlayerMeld sourcePon = melds[sourcePonMeldIndex];
            if (sourcePon == null || sourcePon.Type != PlayerMeldType.Pon ||
                !sourcePon.HasDiscardSource || sourcePon.AcquiredTile.Value != tile)
            {
                return false;
            }

            Tile existingDrawnTile = drawnTile.Value;
            bool drawnMatches = existingDrawnTile == tile;
            if ((addedTileLocation == SelfKanTileLocation.DrawnTile && !drawnMatches) ||
                (addedTileLocation == SelfKanTileLocation.Hand &&
                    Hand.CountTilesByValue(tile) <= 0))
            {
                return false;
            }

            PlayerMeld preparedMeld = PlayerMeld.CreateKakan(
                new[] { tile, tile, tile, tile },
                SeatId,
                sourcePon.SourceSeat.Value,
                sourcePon.AcquiredTile.Value,
                sourcePon.SourceDiscardId.Value);

            // All checks are complete; the deterministic writes below cannot fail.
            if (addedTileLocation == SelfKanTileLocation.Hand)
            {
                if (!Hand.TryRemoveTilesByValue(tile, 1))
                    return false;

                Hand.Add(existingDrawnTile);
            }

            drawnTile = null;
            melds[sourcePonMeldIndex] = preparedMeld;
            meld = preparedMeld;
            return true;
        }
    }
}
