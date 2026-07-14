using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class Mahjong3DHandViewTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string Mahjong3DHandViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DHandView, Assembly-CSharp";
        private const string Mahjong3DTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";

        [Test]
        public void SetTileInteractableByIndices_OnlyEnablesMatchingTilesWithoutDimming()
        {
            GameObject root = new GameObject("Hand3DViewReachCandidatesTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DHandViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Invoke(
                    view,
                    "RenderHand",
                    CreateTileList(CreateTile("1m"), CreateTile("2m"), CreateTile("3m")),
                    true,
                    true);

                Invoke(view, "SetTileInteractableByIndices", new List<int> { 2 });

                Component[] tileViews = root.GetComponentsInChildren(Type.GetType(Mahjong3DTileViewTypeName, true));
                Assert.That(tileViews.Length, Is.EqualTo(3));
                Assert.That(GetProperty(tileViews[0], "Interactable"), Is.False);
                Assert.That(GetProperty(tileViews[1], "Interactable"), Is.False);
                Assert.That(GetProperty(tileViews[2], "Interactable"), Is.True);
                Assert.That(GetProperty(tileViews[0], "IsDimmed"), Is.False);
                Assert.That(GetProperty(tileViews[1], "IsDimmed"), Is.False);
                Assert.That(GetProperty(tileViews[2], "IsDimmed"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetReachCandidateInteractableByIndices_DimsOnlyNonCandidates()
        {
            GameObject root = new GameObject("Hand3DViewReachCandidateDimmedTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DHandViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Invoke(
                    view,
                    "RenderHand",
                    CreateTileList(CreateTile("1m"), CreateTile("2m"), CreateTile("3m")),
                    true,
                    true);

                Invoke(view, "SetReachCandidateInteractableByIndices", new List<int> { 1 });

                Component[] tileViews = root.GetComponentsInChildren(Type.GetType(Mahjong3DTileViewTypeName, true));
                Assert.That(tileViews.Length, Is.EqualTo(3));
                Assert.That(GetProperty(tileViews[0], "Interactable"), Is.False);
                Assert.That(GetProperty(tileViews[1], "Interactable"), Is.True);
                Assert.That(GetProperty(tileViews[2], "Interactable"), Is.False);
                Assert.That(GetProperty(tileViews[0], "IsDimmed"), Is.True);
                Assert.That(GetProperty(tileViews[1], "IsDimmed"), Is.False);
                Assert.That(GetProperty(tileViews[2], "IsDimmed"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetTilesInteractable_ClearsReachCandidateDimming()
        {
            GameObject root = new GameObject("Hand3DViewClearDimmedTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DHandViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Invoke(
                    view,
                    "RenderHand",
                    CreateTileList(CreateTile("1m"), CreateTile("2m"), CreateTile("3m")),
                    true,
                    true);

                Invoke(view, "SetReachCandidateInteractableByIndices", new List<int> { 1 });
                Invoke(view, "SetTilesInteractable", false);

                Component[] tileViews = root.GetComponentsInChildren(Type.GetType(Mahjong3DTileViewTypeName, true));
                Assert.That(tileViews.Length, Is.EqualTo(3));
                for (int i = 0; i < tileViews.Length; i++)
                {
                    Assert.That(GetProperty(tileViews[i], "Interactable"), Is.False);
                    Assert.That(GetProperty(tileViews[i], "IsDimmed"), Is.False);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderHandAndSpawnTestTiles_KeepFirstTileAtSpawnRootOrigin()
        {
            GameObject root = new GameObject("Hand3DViewLeftAnchoredLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DHandViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);
                SetPrivateField(view, "spacing", 2f);
                SetPrivateField(view, "testTileCount", 3);

                Invoke(
                    view,
                    "RenderHand",
                    CreateTileList(CreateTile("1m"), CreateTile("2m"), CreateTile("3m")),
                    true,
                    true);
                AssertTileLocalXPositions(root, 0f, 2f, 4f);

                Invoke(
                    view,
                    "RenderHand",
                    CreateTileList(CreateTile("1m"), CreateTile("2m")),
                    true,
                    true);
                AssertTileLocalXPositions(root, 0f, 2f);

                Invoke(view, "SpawnTestTiles");
                AssertTileLocalXPositions(root, 0f, 2f, 4f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateTileList(params object[] tiles)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(tileType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < tiles.Length; i++)
                list.Add(tiles[i]);

            return list;
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static void AssertTileLocalXPositions(GameObject root, params float[] expectedPositions)
        {
            Component[] tileViews = root.GetComponentsInChildren(Type.GetType(Mahjong3DTileViewTypeName, true));
            Assert.That(tileViews.Length, Is.EqualTo(expectedPositions.Length));
            for (int i = 0; i < expectedPositions.Length; i++)
            {
                Assert.That(tileViews[i].transform.localPosition.x, Is.EqualTo(expectedPositions[i]).Within(0.0001f));
            }
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = null;
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo candidate = methods[i];
                if (candidate.Name != methodName)
                    continue;

                if (candidate.GetParameters().Length != args.Length)
                    continue;

                method = candidate;
                break;
            }

            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }

    public sealed class Mahjong3DCallDisplayTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string DiscardSourceTypeName =
            "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";
        private const string DiscardRecordTypeName =
            "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
        private const string DiscardClaimTypeName =
            "MahjongPrototype.Domain.DiscardClaim, Assembly-CSharp";
        private const string PlayerMeldTypeName = "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";
        private const string Mahjong3DDiscardRiverViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DDiscardRiverView, Assembly-CSharp";
        private const string Mahjong3DOpenMeldViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DOpenMeldView, Assembly-CSharp";
        private const string Mahjong3DTileViewTypeName =
            "MahjongPrototype.UI3D.Mahjong3DTileView, Assembly-CSharp";

        [Test]
        public void RenderDiscardRiver_ExcludesClaimedRecordsWithoutMutatingDiscardHistory()
        {
            GameObject root = new GameObject("DiscardRiverClaimFilterTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDiscardRiverViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(1, "East", "1m"));
                discards.Add(CreateDiscard(2, "East", "5m"));
                discards.Add(CreateDiscard(3, "West", "9m"));
                IDictionary claims = CreateClaims(2);

                Invoke(view, "RenderDiscardRiver", discards, claims, Seat("East"));

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(1));
                Assert.That(GetProperty(tileViews[0], "Tile").ToString(), Is.EqualTo("1m"));
                Assert.That(discards.Count, Is.EqualTo(3));
                Assert.That(claims.Count, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_OnlyNormalReachDeclarationTileIsHorizontal()
        {
            GameObject root = new GameObject("DiscardRiverNormalReachLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDiscardRiverViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(1, "East", "1m", 4));
                discards.Add(CreateDiscard(2, "East", "2m", 5));
                discards.Add(CreateDiscard(3, "East", "3m", 6));

                Invoke(view, "RenderDiscardRiver", discards, null, Seat("East"), true, 5);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(3));
                AssertVertical(tileViews[0]);
                AssertHorizontal(tileViews[1]);
                AssertVertical(tileViews[2]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_DoubleReachDeclarationTileIsHorizontal()
        {
            GameObject root = new GameObject("DiscardRiverDoubleReachLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDiscardRiverViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(1, "East", "1m", 1));
                discards.Add(CreateDiscard(2, "East", "2m", 2));

                Invoke(view, "RenderDiscardRiver", discards, null, Seat("East"), true, 1);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(2));
                AssertHorizontal(tileViews[0]);
                AssertVertical(tileViews[1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_UsesMixedTileSpacingAndStartsEachRowAtRoot()
        {
            GameObject root = new GameObject("DiscardRiverSpacingLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDiscardRiverViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);
                SetPrivateField(view, "verticalTileSpacing", 2f);
                SetPrivateField(view, "horizontalTileSpacing", 1f);
                SetPrivateField(view, "spacingY", 10f);

                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                for (int id = 1; id <= 7; id++)
                    discards.Add(CreateDiscard(id, "East", $"{id}m", id));

                Invoke(view, "RenderDiscardRiver", discards, null, Seat("East"), true, 2);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(7));
                Assert.That(tileViews[0].transform.localPosition, Is.EqualTo(new Vector3(0f, 0f, 0f)));
                Assert.That(tileViews[1].transform.localPosition.x, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.That(tileViews[2].transform.localPosition.x, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(tileViews[5].transform.localPosition.x, Is.EqualTo(9f).Within(0.0001f));
                Assert.That(tileViews[6].transform.localPosition, Is.EqualTo(new Vector3(0f, 10f, 0f)));
                AssertHorizontal(tileViews[1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_ExcludesClaimedReachDeclarationTile()
        {
            GameObject root = new GameObject("DiscardRiverClaimedReachFilterTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDiscardRiverViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(1, "East", "1m", 4));
                discards.Add(CreateDiscard(2, "East", "2m", 5));
                discards.Add(CreateDiscard(3, "East", "3m", 6));

                Invoke(view, "RenderDiscardRiver", discards, CreateClaims(2), Seat("East"), true, 5);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(2));
                Assert.That(GetProperty(tileViews[0], "Tile").ToString(), Is.EqualTo("1m"));
                Assert.That(GetProperty(tileViews[1], "Tile").ToString(), Is.EqualTo("3m"));
                AssertVertical(tileViews[0]);
                AssertVertical(tileViews[1]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderDiscardRiver_ClearRemovesGeneratedTiles()
        {
            GameObject root = new GameObject("DiscardRiverClearTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DDiscardRiverViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList discards = CreateList(RequireType(DiscardRecordTypeName));
                discards.Add(CreateDiscard(1, "East", "1m"));
                Invoke(view, "RenderDiscardRiver", discards, null, Seat("East"), false, 0);
                Assert.That(GetTileViews(root).Length, Is.EqualTo(1));

                Invoke(view, "Clear");
                Assert.That(GetTileViews(root).Length, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderOpenMelds_ShowsThreeTilesPerMeldAndClearRemovesThem()
        {
            GameObject root = new GameObject("OpenMeldViewTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Type playerMeldType = RequireType(PlayerMeldTypeName);
                IList melds = CreateList(playerMeldType);
                melds.Add(CreateDiscardDerivedMeld("Pon", "5m 5m 5m", "5m", 1));
                melds.Add(CreateDiscardDerivedMeld("Chi", "3m 4m 5m", "5m", 2));

                Invoke(view, "RenderOpenMelds", melds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(6));

                Invoke(view, "Clear");
                Assert.That(GetTileViews(root).Length, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderOpenMelds_ChiPlacesOnlyCalledTileHorizontallyAtLeftEdge()
        {
            GameObject root = new GameObject("OpenMeldChiLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);
                SetPrivateField(view, "verticalTileSpacing", 2f);
                SetPrivateField(view, "horizontalTileSpacing", 1f);

                IList melds = CreateList(RequireType(PlayerMeldTypeName));
                melds.Add(CreateDiscardDerivedMeld("Chi", "3m 4m 5m", "4m", 1, "East", "North"));
                Invoke(view, "RenderOpenMelds", melds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(3));
                Assert.That(GetProperty(tileViews[0], "Tile").ToString(), Is.EqualTo("4m"));
                Assert.That(GetProperty(tileViews[1], "Tile").ToString(), Is.EqualTo("3m"));
                Assert.That(GetProperty(tileViews[2], "Tile").ToString(), Is.EqualTo("5m"));
                AssertHorizontal(tileViews[0]);
                AssertVertical(tileViews[1]);
                AssertVertical(tileViews[2]);
                Assert.That(tileViews[0].transform.localPosition.x, Is.EqualTo(-3.5f).Within(0.0001f));
                Assert.That(tileViews[1].transform.localPosition.x, Is.EqualTo(-2f).Within(0.0001f));
                Assert.That(tileViews[2].transform.localPosition.x, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase("North", 0)]
        [TestCase("West", 1)]
        [TestCase("South", 2)]
        public void RenderOpenMelds_PonPlacesCalledTileBySourceSeat(
            string sourceSeatName,
            int expectedCalledTileIndex)
        {
            GameObject root = new GameObject("OpenMeldPonSourceLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList melds = CreateList(RequireType(PlayerMeldTypeName));
                melds.Add(CreateDiscardDerivedMeld("Pon", "5m 5m 5m", "5m", 1, "East", sourceSeatName));
                Invoke(view, "RenderOpenMelds", melds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(3));
                for (int tileIndex = 0; tileIndex < tileViews.Length; tileIndex++)
                {
                    if (tileIndex == expectedCalledTileIndex)
                        AssertHorizontal(tileViews[tileIndex]);
                    else
                        AssertVertical(tileViews[tileIndex]);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderOpenMelds_UsesSeparateTileSpacingAndMaintainsMeldGapFromRightEdge()
        {
            GameObject root = new GameObject("OpenMeldSpacingLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);
                SetPrivateField(view, "verticalTileSpacing", 2f);
                SetPrivateField(view, "horizontalTileSpacing", 1f);
                SetPrivateField(view, "meldSpacing", 3f);

                IList melds = CreateList(RequireType(PlayerMeldTypeName));
                melds.Add(CreateDiscardDerivedMeld("Chi", "3m 4m 5m", "4m", 1, "East", "North"));
                melds.Add(CreateDiscardDerivedMeld("Pon", "6m 6m 6m", "6m", 2, "East", "South"));
                Invoke(view, "RenderOpenMelds", melds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(6));
                Assert.That(tileViews[2].transform.localPosition.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(tileViews[0].transform.localPosition.x, Is.EqualTo(-3.5f).Within(0.0001f));
                Assert.That(tileViews[3].transform.localPosition.x, Is.EqualTo(-11f).Within(0.0001f));
                Assert.That(tileViews[4].transform.localPosition.x, Is.EqualTo(-9f).Within(0.0001f));
                Assert.That(tileViews[5].transform.localPosition.x, Is.EqualTo(-7.5f).Within(0.0001f));
                Assert.That(tileViews[0].transform.localPosition.x - 0.5f, Is.EqualTo(-4f).Within(0.0001f));
                Assert.That(tileViews[5].transform.localPosition.x + 0.5f, Is.EqualTo(-7f).Within(0.0001f));
                AssertHorizontal(tileViews[0]);
                AssertHorizontal(tileViews[5]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderOpenMelds_DaiminkanAndAnkanRenderFourTilesWithOnlyCalledKanTileRotated()
        {
            GameObject root = new GameObject("OpenMeldKanLayoutTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                IList melds = CreateList(RequireType(PlayerMeldTypeName));
                melds.Add(CreateDiscardDerivedMeld(
                    "Daiminkan",
                    "5m 5m 5m 5m",
                    "5m",
                    1,
                    "East",
                    "South"));
                melds.Add(CreateAnkan("P", "East"));
                Invoke(view, "RenderOpenMelds", melds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(8));
                for (int tileIndex = 0; tileIndex < 4; tileIndex++)
                {
                    if (tileIndex == 2)
                        AssertHorizontal(tileViews[tileIndex]);
                    else
                        AssertVertical(tileViews[tileIndex]);
                }
                for (int tileIndex = 4; tileIndex < 8; tileIndex++)
                    AssertVertical(tileViews[tileIndex]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderOpenMelds_ThreeAndFourTileBlocksKeepFirstAnchorAndMeldSpacing()
        {
            GameObject root = new GameObject("OpenMeldMixedWidthSpacingTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);
                SetPrivateField(view, "verticalTileSpacing", 2f);
                SetPrivateField(view, "horizontalTileSpacing", 1f);
                SetPrivateField(view, "meldSpacing", 3f);

                IList melds = CreateList(RequireType(PlayerMeldTypeName));
                melds.Add(CreateDiscardDerivedMeld(
                    "Pon",
                    "6m 6m 6m",
                    "6m",
                    1,
                    "East",
                    "West"));
                melds.Add(CreateAnkan("P", "East"));
                Invoke(view, "RenderOpenMelds", melds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(7));
                Assert.That(tileViews[2].transform.localPosition.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(tileViews[0].transform.localPosition.x, Is.EqualTo(-3f).Within(0.0001f));
                Assert.That(tileViews[3].transform.localPosition.x, Is.EqualTo(-14f).Within(0.0001f));
                Assert.That(tileViews[6].transform.localPosition.x, Is.EqualTo(-8f).Within(0.0001f));
                Assert.That(
                    tileViews[0].transform.localPosition.x - 1f,
                    Is.EqualTo(-4f).Within(0.0001f));
                Assert.That(
                    tileViews[6].transform.localPosition.x + 1f,
                    Is.EqualTo(-7f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateDiscard(int id, string seatName, string tileCode, int turnIndex = 1)
        {
            return Activator.CreateInstance(
                RequireType(DiscardRecordTypeName),
                id,
                Seat(seatName),
                CreateTile(tileCode),
                turnIndex,
                Enum.Parse(RequireType(DiscardSourceTypeName), "Hand"),
                false);
        }

        private static IDictionary CreateClaims(int discardId)
        {
            Type claimType = RequireType(DiscardClaimTypeName);
            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(int), claimType);
            IDictionary claims = (IDictionary)Activator.CreateInstance(dictionaryType);
            claims.Add(discardId, Activator.CreateInstance(claimType));
            return claims;
        }

        private static object CreateDiscardDerivedMeld(
            string kind,
            string tileText,
            string calledTileCode,
            int sourceDiscardId,
            string callerSeatName = "East",
            string sourceSeatName = "West")
        {
            Type playerMeldType = RequireType(PlayerMeldTypeName);
            MethodInfo factory = playerMeldType.GetMethod(
                "Create" + kind,
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(factory, Is.Not.Null);
            return factory.Invoke(
                null,
                new[]
                {
                    CreateTiles(tileText),
                    Seat(callerSeatName),
                    Seat(sourceSeatName),
                    CreateTile(calledTileCode),
                    (object)sourceDiscardId
                });
        }

        private static object CreateAnkan(string tileCode, string ownerSeatName)
        {
            Type playerMeldType = RequireType(PlayerMeldTypeName);
            MethodInfo factory = playerMeldType.GetMethod(
                "CreateAnkan",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(factory, Is.Not.Null);
            return factory.Invoke(
                null,
                new[]
                {
                    CreateTiles($"{tileCode} {tileCode} {tileCode} {tileCode}"),
                    Seat(ownerSeatName)
                });
        }

        private static IList CreateTiles(string tileText)
        {
            string[] codes = tileText.Split(' ');
            IList tiles = CreateList(RequireType(TileTypeName));
            for (int i = 0; i < codes.Length; i++)
                tiles.Add(CreateTile(codes[i]));

            return tiles;
        }

        private static IList CreateList(Type itemType)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        }

        private static object CreateTile(string code)
        {
            ConstructorInfo constructor = RequireType(TileTypeName).GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object Seat(string name)
        {
            return Enum.Parse(RequireType(SeatIdTypeName), name);
        }

        private static Component[] GetTileViews(GameObject root)
        {
            return root.GetComponentsInChildren(Type.GetType(Mahjong3DTileViewTypeName, true), true);
        }

        private static void AssertHorizontal(Component tileView)
        {
            Assert.That(
                Quaternion.Angle(tileView.transform.localRotation, Quaternion.Euler(0f, 0f, 90f)),
                Is.LessThan(0.001f));
        }

        private static void AssertVertical(Component tileView)
        {
            Assert.That(Quaternion.Angle(tileView.transform.localRotation, Quaternion.identity), Is.LessThan(0.001f));
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name == methodName && method.GetParameters().Length == args.Length)
                    return method.Invoke(target, args);
            }

            Assert.Fail($"Method not found: {target.GetType().FullName}.{methodName}");
            return null;
        }

        private static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName, true);
            Assert.That(type, Is.Not.Null);
            return type;
        }
    }
}
