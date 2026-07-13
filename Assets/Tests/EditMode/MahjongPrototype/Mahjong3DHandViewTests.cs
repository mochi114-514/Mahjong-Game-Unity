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
        private const string OpenMeldTypeName = "MahjongPrototype.Domain.OpenMeld, Assembly-CSharp";
        private const string OpenMeldKindTypeName =
            "MahjongPrototype.Domain.OpenMeldType, Assembly-CSharp";
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
        public void RenderOpenMelds_ShowsThreeTilesPerMeldAndClearsWhenEmpty()
        {
            GameObject root = new GameObject("OpenMeldViewTest");
            GameObject prefab = new GameObject("Tile3DPrefab");
            try
            {
                object view = root.AddComponent(Type.GetType(Mahjong3DOpenMeldViewTypeName, true));
                object tilePrefab = prefab.AddComponent(Type.GetType(Mahjong3DTileViewTypeName, true));
                SetPrivateField(view, "tilePrefab", tilePrefab);

                Type openMeldType = RequireType(OpenMeldTypeName);
                IList openMelds = CreateList(openMeldType);
                openMelds.Add(CreateOpenMeld("Pon", "5m 5m 5m", "5m", 1));
                openMelds.Add(CreateOpenMeld("Chi", "3m 4m 5m", "5m", 2));

                Invoke(view, "RenderOpenMelds", openMelds);

                Component[] tileViews = GetTileViews(root);
                Assert.That(tileViews.Length, Is.EqualTo(6));
                Assert.That(GetProperty(tileViews[0], "Tile").ToString(), Is.EqualTo("5m"));
                Assert.That(GetProperty(tileViews[3], "Tile").ToString(), Is.EqualTo("3m"));
                Assert.That(GetProperty(tileViews[4], "Tile").ToString(), Is.EqualTo("4m"));
                Assert.That(GetProperty(tileViews[5], "Tile").ToString(), Is.EqualTo("5m"));
                Assert.That(tileViews[0].transform.localPosition.x, Is.EqualTo(-3.2f).Within(0.0001f));
                Assert.That(tileViews[1].transform.localPosition.x, Is.EqualTo(-1.6f).Within(0.0001f));
                Assert.That(tileViews[2].transform.localPosition.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(tileViews[3].transform.localPosition.x, Is.EqualTo(-7.4f).Within(0.0001f));
                Assert.That(tileViews[4].transform.localPosition.x, Is.EqualTo(-5.8f).Within(0.0001f));
                Assert.That(tileViews[5].transform.localPosition.x, Is.EqualTo(-4.2f).Within(0.0001f));

                Invoke(view, "RenderOpenMelds", CreateList(openMeldType));
                Assert.That(GetTileViews(root).Length, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static object CreateDiscard(int id, string seatName, string tileCode)
        {
            return Activator.CreateInstance(
                RequireType(DiscardRecordTypeName),
                id,
                Seat(seatName),
                CreateTile(tileCode),
                1,
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

        private static object CreateOpenMeld(
            string kind,
            string tileText,
            string calledTileCode,
            int sourceDiscardId)
        {
            return Activator.CreateInstance(
                RequireType(OpenMeldTypeName),
                Enum.Parse(RequireType(OpenMeldKindTypeName), kind),
                CreateTiles(tileText),
                Seat("East"),
                Seat("West"),
                CreateTile(calledTileCode),
                sourceDiscardId);
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
