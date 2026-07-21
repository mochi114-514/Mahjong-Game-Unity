using System.Collections.Generic;
using MahjongPrototype.Domain;
using MahjongPrototype.Services;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongPrototype.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mahjong Prototype/UI/Mahjong Winning Candidate Controller")]
    public sealed class MahjongWinningCandidateController : MonoBehaviour
    {
        private enum DisplayMode
        {
            Candidates = 0,
            ReachGroups = 1
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Transform groupsContainer;
        [SerializeField] private MahjongWinningCandidateGroupView groupViewPrefab;
        [SerializeField] private MahjongWinningTileCandidateView candidateViewPrefab;
        [SerializeField] private MahjongTileSpriteCatalog tileSpriteCatalog;

        private readonly List<MahjongWinningCandidateGroupView> spawnedGroups =
            new List<MahjongWinningCandidateGroupView>();
        private readonly HashSet<int> warnedMissingSpriteTypeIndexes =
            new HashSet<int>();
        private readonly List<DisplayGroupState> displayedGroups =
            new List<DisplayGroupState>();
        private DisplayMode? displayedMode;
        private bool warnedMissingRoot;
        private bool warnedMissingGroupsContainer;
        private bool warnedMissingGroupPrefab;
        private bool warnedMissingCandidatePrefab;
        private bool warnedMissingCatalog;

        public int SpawnedGroupCount => spawnedGroups.Count;
        public bool IsVisible => root != null && root.activeSelf;

        private void OnDisable()
        {
            Clear();
        }

        public void SetGroups(IReadOnlyList<ReachWinningCandidateGroup> groups)
        {
            if (groups == null)
            {
                Clear();
                return;
            }

            int visibleGroupCount = CountVisibleGroups(groups);
            if (visibleGroupCount <= 0)
            {
                Clear();
                return;
            }

            bool showDiscardHeadings = visibleGroupCount > 1;
            List<DisplayGroupState> nextDisplayGroups =
                BuildGroupDisplayStates(groups, showDiscardHeadings);
            if (HasSameDisplay(DisplayMode.ReachGroups, nextDisplayGroups))
                return;

            if (!CanPopulate())
            {
                Clear();
                return;
            }

            ClearSpawnedGroups();
            for (int i = 0; i < groups.Count; i++)
            {
                ReachWinningCandidateGroup group = groups[i];
                if (group == null || group.WinningTiles.Count <= 0)
                    continue;

                SpawnGroup(
                    group.WinningTiles,
                    showDiscardHeadings,
                    BuildDiscardHeading(group.DiscardCandidates));
            }

            ShowPopulatedRoot(DisplayMode.ReachGroups, nextDisplayGroups);
        }

        public void SetCandidates(IReadOnlyList<WinningTileCandidate> candidates)
        {
            if (candidates == null || candidates.Count <= 0)
            {
                Clear();
                return;
            }

            List<DisplayGroupState> nextDisplayGroups =
                new List<DisplayGroupState>
                {
                    new DisplayGroupState(
                        false,
                        string.Empty,
                        BuildCandidateDisplayStates(candidates),
                        new List<DiscardCandidateDisplayState>())
                };
            if (HasSameDisplay(DisplayMode.Candidates, nextDisplayGroups))
                return;

            if (!CanPopulate())
            {
                Clear();
                return;
            }

            ClearSpawnedGroups();
            SpawnGroup(candidates, false, string.Empty);
            ShowPopulatedRoot(DisplayMode.Candidates, nextDisplayGroups);
        }

        public void Clear()
        {
            ClearSpawnedGroups();
            ClearDisplayState();
            if (root != null)
                root.SetActive(false);
        }

        private void ClearSpawnedGroups()
        {
            for (int i = spawnedGroups.Count - 1; i >= 0; i--)
            {
                MahjongWinningCandidateGroupView group = spawnedGroups[i];
                if (group != null)
                    DestroyView(group.gameObject);
            }

            spawnedGroups.Clear();
        }

        private int PopulateCandidates(
            MahjongWinningCandidateGroupView groupView,
            IReadOnlyList<WinningTileCandidate> candidates)
        {
            if (groupView == null || groupView.CandidateContainer == null)
                return 0;

            int spawnedCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                WinningTileCandidate candidate = candidates[i];
                if (!tileSpriteCatalog.TryGetSprite(candidate.Tile, out Sprite sprite))
                {
                    WarnMissingSpriteOnce(candidate.Tile.TypeIndex);
                    continue;
                }

                MahjongWinningTileCandidateView candidateView = Instantiate(
                    candidateViewPrefab,
                    groupView.CandidateContainer);
                if (!candidateView.TrySet(sprite, candidate.VisibleRemainingCount))
                {
                    DestroyView(candidateView.gameObject);
                    continue;
                }

                candidateView.gameObject.SetActive(true);
                spawnedCount++;
            }

            return spawnedCount;
        }

        private void SpawnGroup(
            IReadOnlyList<WinningTileCandidate> candidates,
            bool showHeading,
            string heading)
        {
            MahjongWinningCandidateGroupView groupView = Instantiate(
                groupViewPrefab,
                groupsContainer);
            groupView.SetHeading(showHeading, heading);

            if (PopulateCandidates(groupView, candidates) <= 0)
            {
                DestroyView(groupView.gameObject);
                return;
            }

            groupView.gameObject.SetActive(true);
            spawnedGroups.Add(groupView);
        }

        private void ShowPopulatedRoot(
            DisplayMode mode,
            IReadOnlyList<DisplayGroupState> nextDisplayGroups)
        {
            if (spawnedGroups.Count <= 0)
            {
                ClearDisplayState();
                if (root != null)
                    root.SetActive(false);
                return;
            }

            displayedMode = mode;
            displayedGroups.Clear();
            for (int i = 0; i < nextDisplayGroups.Count; i++)
                displayedGroups.Add(nextDisplayGroups[i]);
            root.SetActive(true);
            RebuildLayout();
        }

        private bool HasSameDisplay(
            DisplayMode mode,
            IReadOnlyList<DisplayGroupState> nextDisplayGroups)
        {
            if (!displayedMode.HasValue || displayedMode.Value != mode ||
                displayedGroups.Count != nextDisplayGroups.Count)
            {
                return false;
            }

            for (int i = 0; i < displayedGroups.Count; i++)
            {
                if (!displayedGroups[i].Equals(nextDisplayGroups[i]))
                    return false;
            }

            return true;
        }

        private void ClearDisplayState()
        {
            displayedMode = null;
            displayedGroups.Clear();
        }

        private static List<DisplayGroupState> BuildGroupDisplayStates(
            IReadOnlyList<ReachWinningCandidateGroup> groups,
            bool showDiscardHeadings)
        {
            List<DisplayGroupState> states = new List<DisplayGroupState>();
            for (int i = 0; i < groups.Count; i++)
            {
                ReachWinningCandidateGroup group = groups[i];
                if (group == null || group.WinningTiles.Count <= 0)
                    continue;

                states.Add(new DisplayGroupState(
                    showDiscardHeadings,
                    BuildDiscardHeading(group.DiscardCandidates),
                    BuildCandidateDisplayStates(group.WinningTiles),
                    BuildDiscardCandidateDisplayStates(group.DiscardCandidates)));
            }

            return states;
        }

        private static List<WinningCandidateDisplayState> BuildCandidateDisplayStates(
            IReadOnlyList<WinningTileCandidate> candidates)
        {
            List<WinningCandidateDisplayState> states =
                new List<WinningCandidateDisplayState>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                WinningTileCandidate candidate = candidates[i];
                states.Add(new WinningCandidateDisplayState(
                    candidate.Tile.TypeIndex,
                    candidate.VisibleRemainingCount));
            }

            return states;
        }

        private static List<DiscardCandidateDisplayState> BuildDiscardCandidateDisplayStates(
            IReadOnlyList<ReachDiscardCandidate> discardCandidates)
        {
            List<DiscardCandidateDisplayState> states =
                new List<DiscardCandidateDisplayState>();
            if (discardCandidates == null)
                return states;

            for (int i = 0; i < discardCandidates.Count; i++)
            {
                ReachDiscardCandidate candidate = discardCandidates[i];
                states.Add(new DiscardCandidateDisplayState(
                    candidate.Source,
                    candidate.HandIndex,
                    candidate.Tile.TypeIndex));
            }

            return states;
        }

        private bool CanPopulate()
        {
            bool valid = true;
            if (root == null)
            {
                WarnMissingOnce(ref warnedMissingRoot, "Root is not assigned.");
                valid = false;
            }

            if (groupsContainer == null)
            {
                WarnMissingOnce(
                    ref warnedMissingGroupsContainer,
                    "GroupsContainer is not assigned.");
                valid = false;
            }

            if (groupViewPrefab == null)
            {
                WarnMissingOnce(
                    ref warnedMissingGroupPrefab,
                    "GroupViewPrefab is not assigned.");
                valid = false;
            }

            if (candidateViewPrefab == null)
            {
                WarnMissingOnce(
                    ref warnedMissingCandidatePrefab,
                    "CandidateViewPrefab is not assigned.");
                valid = false;
            }

            if (tileSpriteCatalog == null)
            {
                WarnMissingOnce(
                    ref warnedMissingCatalog,
                    "MahjongTileSpriteCatalog is not assigned.");
                valid = false;
            }

            return valid;
        }

        private void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (groupsContainer is RectTransform groupsRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(groupsRect);
            if (root.transform is RectTransform rootRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        private static int CountVisibleGroups(
            IReadOnlyList<ReachWinningCandidateGroup> groups)
        {
            int count = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] != null && groups[i].WinningTiles.Count > 0)
                    count++;
            }

            return count;
        }

        private static string BuildDiscardHeading(
            IReadOnlyList<ReachDiscardCandidate> discardCandidates)
        {
            if (discardCandidates == null || discardCandidates.Count <= 0)
                return string.Empty;

            List<string> names = new List<string>();
            HashSet<int> addedTypeIndexes = new HashSet<int>();
            for (int i = 0; i < discardCandidates.Count; i++)
            {
                Tile tile = discardCandidates[i].Tile;
                if (!tile.IsValid || !addedTypeIndexes.Add(tile.TypeIndex))
                    continue;

                names.Add(FormatTileName(tile));
            }

            return names.Count > 0 ? string.Join("・", names) + "切り" : string.Empty;
        }

        private static string FormatTileName(Tile tile)
        {
            if (tile.IsNumberTile)
            {
                string[] rankNames =
                    { "", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
                string suitName = tile.Suit == TileSuit.Man
                    ? "萬"
                    : tile.Suit == TileSuit.Pin ? "筒" : "索";
                return rankNames[tile.Rank] + suitName;
            }

            switch (tile.Honor)
            {
                case HonorKind.East:
                    return "東";
                case HonorKind.South:
                    return "南";
                case HonorKind.West:
                    return "西";
                case HonorKind.North:
                    return "北";
                case HonorKind.White:
                    return "白";
                case HonorKind.Green:
                    return "發";
                case HonorKind.Red:
                    return "中";
                default:
                    return tile.Code;
            }
        }

        private void WarnMissingSpriteOnce(int typeIndex)
        {
            if (!warnedMissingSpriteTypeIndexes.Add(typeIndex))
                return;

            Debug.LogWarning(
                $"{nameof(MahjongWinningCandidateController)}: " +
                $"Tile sprite is not registered for TypeIndex {typeIndex}.",
                this);
        }

        private void WarnMissingOnce(ref bool warned, string message)
        {
            if (warned)
                return;

            warned = true;
            Debug.LogWarning($"{nameof(MahjongWinningCandidateController)}: {message}", this);
        }

        private sealed class DisplayGroupState
        {
            public DisplayGroupState(
                bool showHeading,
                string heading,
                IReadOnlyList<WinningCandidateDisplayState> candidates,
                IReadOnlyList<DiscardCandidateDisplayState> discardCandidates)
            {
                ShowHeading = showHeading;
                Heading = heading ?? string.Empty;
                Candidates = candidates;
                DiscardCandidates = discardCandidates;
            }

            private bool ShowHeading { get; }
            private string Heading { get; }
            private IReadOnlyList<WinningCandidateDisplayState> Candidates { get; }
            private IReadOnlyList<DiscardCandidateDisplayState> DiscardCandidates { get; }

            public bool Equals(DisplayGroupState other)
            {
                return other != null &&
                    ShowHeading == other.ShowHeading &&
                    Heading == other.Heading &&
                    HasSameItems(Candidates, other.Candidates) &&
                    HasSameItems(DiscardCandidates, other.DiscardCandidates);
            }

            private static bool HasSameItems<T>(
                IReadOnlyList<T> left,
                IReadOnlyList<T> right)
                where T : struct, System.IEquatable<T>
            {
                if (left.Count != right.Count)
                    return false;

                for (int i = 0; i < left.Count; i++)
                {
                    if (!left[i].Equals(right[i]))
                        return false;
                }

                return true;
            }
        }

        private readonly struct WinningCandidateDisplayState :
            System.IEquatable<WinningCandidateDisplayState>
        {
            public WinningCandidateDisplayState(int typeIndex, int visibleRemainingCount)
            {
                TypeIndex = typeIndex;
                VisibleRemainingCount = visibleRemainingCount;
            }

            private int TypeIndex { get; }
            private int VisibleRemainingCount { get; }

            public bool Equals(WinningCandidateDisplayState other)
            {
                return TypeIndex == other.TypeIndex &&
                    VisibleRemainingCount == other.VisibleRemainingCount;
            }
        }

        private readonly struct DiscardCandidateDisplayState :
            System.IEquatable<DiscardCandidateDisplayState>
        {
            public DiscardCandidateDisplayState(
                DiscardSource source,
                int handIndex,
                int tileTypeIndex)
            {
                Source = source;
                HandIndex = handIndex;
                TileTypeIndex = tileTypeIndex;
            }

            private DiscardSource Source { get; }
            private int HandIndex { get; }
            private int TileTypeIndex { get; }

            public bool Equals(DiscardCandidateDisplayState other)
            {
                return Source == other.Source &&
                    HandIndex == other.HandIndex &&
                    TileTypeIndex == other.TileTypeIndex;
            }
        }

        private static void DestroyView(GameObject target)
        {
            if (target == null)
                return;

            target.SetActive(false);
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
