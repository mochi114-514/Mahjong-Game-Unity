using System.Collections.Generic;
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
            Candidates = 0
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
                        BuildCandidateDisplayStates(candidates))
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
                IReadOnlyList<WinningCandidateDisplayState> candidates)
            {
                Candidates = candidates;
            }

            private IReadOnlyList<WinningCandidateDisplayState> Candidates { get; }

            public bool Equals(DisplayGroupState other)
            {
                return other != null &&
                    HasSameItems(Candidates, other.Candidates);
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
