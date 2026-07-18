using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class PlayerMeldTests
    {
        private const string PlayerMeldTypeName =
            "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";
        private const string PlayerSeatTypeName =
            "MahjongPrototype.Domain.PlayerSeat, Assembly-CSharp";
        private const string DiscardClaimTypeName =
            "MahjongPrototype.Domain.DiscardClaim, Assembly-CSharp";
        private const string WinningHandAnalyzerTypeName =
            "MahjongPrototype.Services.WinningHandAnalyzer, Assembly-CSharp";

        private ReflectionTestAccess reflection;
        private CollectionTestAccess collections;
        private MahjongTestTypes types;
        private MahjongTestDataFactory data;
        private Type playerMeldType;

        [SetUp]
        public void SetUp()
        {
            reflection = new ReflectionTestAccess();
            collections = new CollectionTestAccess(reflection);
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            playerMeldType = reflection.RequireType(PlayerMeldTypeName);
        }

        [TestCase("Chi", "3m 4m 5m", "4m", "Sequence")]
        [TestCase("Pon", "5m 5m 5m", "5m", "Triplet")]
        public void ChiAndPon_ArePublicThreeTileSingleMeldsWithDiscardSource(
            string kind,
            string tileText,
            string acquiredTileCode,
            string structuralType)
        {
            object meld = CreateDiscardDerivedMeld(
                kind,
                tileText,
                acquiredTileCode);

            Assert.That(PropertyText(meld, "Type"), Is.EqualTo(kind));
            Assert.That((bool)reflection.GetProperty(meld, "IsOpen"), Is.True);
            Assert.That((bool)reflection.GetProperty(meld, "HasDiscardSource"), Is.True);
            Assert.That((int)reflection.GetProperty(meld, "PhysicalTileCount"), Is.EqualTo(3));
            Assert.That((int)reflection.GetProperty(meld, "StructuralTileCount"), Is.EqualTo(3));
            Assert.That((int)reflection.GetProperty(meld, "StructuralMeldCount"), Is.EqualTo(1));
            Assert.That(PropertyText(meld, "StructuralType"), Is.EqualTo(structuralType));
            Assert.That(PropertyText(meld, "OwnerSeat"), Is.EqualTo("East"));
            Assert.That(PropertyText(meld, "SourceSeat"), Is.EqualTo("West"));
            Assert.That((int)reflection.GetProperty(meld, "SourceDiscardId"), Is.EqualTo(1));
        }

        [TestCase("Daiminkan", true, true)]
        [TestCase("Ankan", false, false)]
        [TestCase("Kakan", true, true)]
        public void KanKinds_AreFourPhysicalTilesAndOneStructuralMeld(
            string kind,
            bool expectedOpen,
            bool expectedDiscardSource)
        {
            object meld = CreateKan(kind, "1m");

            Assert.That(PropertyText(meld, "Type"), Is.EqualTo(kind));
            Assert.That((bool)reflection.GetProperty(meld, "IsKan"), Is.True);
            Assert.That((bool)reflection.GetProperty(meld, "IsOpen"), Is.EqualTo(expectedOpen));
            Assert.That(
                (bool)reflection.GetProperty(meld, "PreservesClosedHand"),
                Is.EqualTo(!expectedOpen));
            Assert.That(
                (bool)reflection.GetProperty(meld, "HasDiscardSource"),
                Is.EqualTo(expectedDiscardSource));
            Assert.That((int)reflection.GetProperty(meld, "PhysicalTileCount"), Is.EqualTo(4));
            Assert.That((int)reflection.GetProperty(meld, "StructuralTileCount"), Is.EqualTo(3));
            Assert.That((int)reflection.GetProperty(meld, "StructuralMeldCount"), Is.EqualTo(1));
            Assert.That(PropertyText(meld, "StructuralType"), Is.EqualTo("Triplet"));
        }

        [Test]
        public void PlayerSeat_AnkanPreservesClosedHandWhilePublicMeldOpensIt()
        {
            object playerSeat = reflection.CreateInstance(
                reflection.RequireType(PlayerSeatTypeName),
                data.ParseSeat("East"));

            reflection.Invoke(playerSeat, "AddMeld", CreateKan("Ankan", "1m"));
            Assert.That((bool)reflection.GetProperty(playerSeat, "IsClosed"), Is.True);

            reflection.Invoke(
                playerSeat,
                "AddMeld",
                CreateDiscardDerivedMeld("Pon", "2m 2m 2m", "2m"));
            Assert.That((bool)reflection.GetProperty(playerSeat, "IsClosed"), Is.False);
            Assert.That(collections.Count(reflection.GetProperty(playerSeat, "Melds")), Is.EqualTo(2));
        }

        [TestCase("Chi", "3m 3m 4m", "3m")]
        [TestCase("Pon", "5m 5m 6m", "5m")]
        [TestCase("Daiminkan", "1m 1m 1m", "1m")]
        [TestCase("Kakan", "1m 1m 1m 1m", "2m")]
        public void DiscardDerivedFactory_RejectsContradictoryKindAndTileComposition(
            string kind,
            string tileText,
            string acquiredTileCode)
        {
            Assert.That(
                playerMeldType.GetConstructors(BindingFlags.Public | BindingFlags.Instance),
                Is.Empty);
            Assert.Throws<TargetInvocationException>(() =>
                CreateDiscardDerivedMeld(kind, tileText, acquiredTileCode));
        }

        [Test]
        public void DiscardDerivedFactory_RejectsOwnerAsSourceSeat()
        {
            Assert.Throws<TargetInvocationException>(() =>
                CreateDiscardDerivedMeld(
                    "Pon",
                    "5m 5m 5m",
                    "5m",
                    "East",
                    "East"));
        }

        [Test]
        public void DiscardClaim_AcceptsDiscardDerivedMeldAndRejectsAnkan()
        {
            Type claimType = reflection.RequireType(DiscardClaimTypeName);
            object pon = CreateDiscardDerivedMeld("Pon", "5m 5m 5m", "5m");
            object claim = reflection.CreateInstance(claimType, pon);

            Assert.That((int)reflection.GetProperty(claim, "DiscardId"), Is.EqualTo(1));
            Assert.That(PropertyText(claim, "ClaimingSeat"), Is.EqualTo("East"));
            Assert.That(reflection.GetProperty(claim, "Meld"), Is.SameAs(pon));
            Assert.Throws<TargetInvocationException>(() =>
                reflection.CreateInstance(claimType, CreateKan("Ankan", "1m")));
        }

        [TestCase("Daiminkan")]
        [TestCase("Ankan")]
        [TestCase("Kakan")]
        public void WinningHandAnalyzer_TreatsKanAsOneMeldAndRequiresElevenConcealedTiles(
            string kanKind)
        {
            object melds = CreateMeldList(CreateKan(kanKind, "1m"));
            object analyzer = reflection.CreateInstance(
                reflection.RequireType(WinningHandAnalyzerTypeName));

            object completed = reflection.Invoke(
                analyzer,
                "AnalyzeCompletedHand",
                data.CreateTileArrayFromText(
                    "2m 2m 2m 3p 3p 3p 4s 4s 4s 5s 5s"),
                melds);
            object withTile = reflection.Invoke(
                analyzer,
                "AnalyzeWithTile",
                data.CreateTileArrayFromText(
                    "2m 2m 2m 3p 3p 3p 4s 4s 4s 5s"),
                data.CreateTile("5s"),
                melds);

            Assert.That((bool)reflection.GetProperty(completed, "CanWin"), Is.True);
            Assert.That((bool)reflection.GetProperty(withTile, "CanWin"), Is.True);
            AssertFixedMeldVisibility(completed, kanKind != "Ankan");
        }

        [Test]
        public void WinningHandAnalyzer_CountsAllFourKanTilesForPerTypeLimit()
        {
            object melds = CreateMeldList(CreateKan("Ankan", "1m"));
            object analyzer = reflection.CreateInstance(
                reflection.RequireType(WinningHandAnalyzerTypeName));
            object result = reflection.Invoke(
                analyzer,
                "AnalyzeCompletedHand",
                data.CreateTileArrayFromText(
                    "1m 2m 3m 4m 5m 6m 7m 8m 9m 2p 2p"),
                melds);

            Assert.That((bool)reflection.GetProperty(result, "CanWin"), Is.False);
        }

        private object CreateKan(string kind, string tileCode)
        {
            if (kind == "Ankan")
            {
                return reflection.InvokeStatic(
                    playerMeldType,
                    "CreateAnkan",
                    data.CreateTileArray(tileCode, tileCode, tileCode, tileCode),
                    data.ParseSeat("East"));
            }

            return CreateDiscardDerivedMeld(
                kind,
                string.Join(" ", tileCode, tileCode, tileCode, tileCode),
                tileCode);
        }

        private object CreateDiscardDerivedMeld(
            string kind,
            string tileText,
            string acquiredTileCode,
            string ownerSeatName = "East",
            string sourceSeatName = "West")
        {
            return reflection.InvokeStatic(
                playerMeldType,
                "Create" + kind,
                data.CreateTileArrayFromText(tileText),
                data.ParseSeat(ownerSeatName),
                data.ParseSeat(sourceSeatName),
                data.CreateTile(acquiredTileCode),
                1);
        }

        private object CreateMeldList(params object[] melds)
        {
            IList list = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(playerMeldType));
            for (int i = 0; i < melds.Length; i++)
                list.Add(melds[i]);

            return list;
        }

        private void AssertFixedMeldVisibility(object analysis, bool expectedOpen)
        {
            object decompositions = reflection.GetProperty(analysis, "StandardDecompositions");
            Assert.That(collections.Count(decompositions), Is.GreaterThan(0));
            object decomposition = collections.Item(decompositions, 0);
            object handMelds = reflection.GetProperty(decomposition, "Melds");

            object fixedMeld = null;
            for (int i = 0; i < collections.Count(handMelds); i++)
            {
                object candidate = collections.Item(handMelds, i);
                if ((bool)reflection.GetProperty(candidate, "IsFixed"))
                {
                    fixedMeld = candidate;
                    break;
                }
            }

            Assert.That(fixedMeld, Is.Not.Null);
            Assert.That(
                (bool)reflection.GetProperty(fixedMeld, "IsOpen"),
                Is.EqualTo(expectedOpen));
        }

        private string PropertyText(object target, string propertyName)
        {
            object value = reflection.GetProperty(target, propertyName);
            return value?.ToString();
        }
    }
}
