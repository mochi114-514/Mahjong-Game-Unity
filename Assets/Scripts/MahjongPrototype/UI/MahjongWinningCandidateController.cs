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
        [SerializeField] private GameObject root;
        [SerializeField] private Transform groupsContainer;
        [SerializeField] private MahjongWinningCandidateGroupView groupViewPrefab;
        [SerializeField] private MahjongWinningTileCandidateView candidateViewPrefab;
        [SerializeField] private MahjongTileSpriteCatalog tileSpriteCatalog;

        private readonly List<MahjongWinningCandidateGroupView> spawnedGroups =
            new List<MahjongWinningCandidateGroupView>();
        private readonly HashSet<int> warnedMissingSpriteTypeIndexes =
            new HashSet<int>();
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
            Clear();
            if (!CanPopulate() || groups == null)
                return;

            int visibleGroupCount = CountVisibleGroups(groups);
            if (visibleGroupCount <= 0)
                return;

            bool showDiscardHeadings = visibleGroupCount > 1;
            for (int i = 0; i < groups.Count; i++)
            {
                ReachWinningCandidateGroup group = groups[i];
                if (group == null || group.WinningTiles.Count <= 0)
                    continue;

                MahjongWinningCandidateGroupView groupView = Instantiate(
                    groupViewPrefab,
                    groupsContainer);
                groupView.SetHeading(
                    showDiscardHeadings,
                    BuildDiscardHeading(group.DiscardCandidates));

                int spawnedCandidateCount = PopulateCandidates(groupView, group.WinningTiles);
                if (spawnedCandidateCount <= 0)
                {
                    DestroyView(groupView.gameObject);
                    continue;
                }

                groupView.gameObject.SetActive(true);
                spawnedGroups.Add(groupView);
            }

            if (spawnedGroups.Count <= 0)
                return;

            root.SetActive(true);
            RebuildLayout();
        }

        public void Clear()
        {
            for (int i = spawnedGroups.Count - 1; i >= 0; i--)
            {
                MahjongWinningCandidateGroupView group = spawnedGroups[i];
                if (group != null)
                    DestroyView(group.gameObject);
            }

            spawnedGroups.Clear();
            if (root != null)
                root.SetActive(false);
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
