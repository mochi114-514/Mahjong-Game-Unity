using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class WallRinshanTests
    {
        private ReflectionTestAccess reflection;
        private CollectionTestAccess collections;
        private MahjongTestDataFactory data;
        private Type wallType;

        [SetUp]
        public void SetUp()
        {
            reflection = new ReflectionTestAccess();
            collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            wallType = types.Wall;
        }

        [Test]
        public void StandardWall_SplitsIntoLiveWallAndFourReservedRinshanTiles()
        {
            object wall = CreateWall();

            Assert.That((int)reflection.GetProperty(wall, "Count"), Is.EqualTo(122));
            Assert.That((int)reflection.GetProperty(wall, "DeadWallCount"), Is.EqualTo(14));
            Assert.That(
                (int)reflection.GetProperty(wall, "RemainingRinshanTileCount"),
                Is.EqualTo(4));
            Assert.That(
                collections.Count(reflection.Invoke(wall, "GetDeadWallSnapshot")),
                Is.EqualTo(14));
            Assert.That(
                collections.Count(reflection.Invoke(wall, "GetRinshanSnapshot")),
                Is.EqualTo(4));
        }

        [Test]
        public void NormalAndSpecificDraws_DoNotMutateDeadWall()
        {
            object wall = CreateWall();
            string[] deadWallBefore = GetTileCodes(
                reflection.Invoke(wall, "GetDeadWallSnapshot"));

            object[] nextArgs = { null };
            Assert.That((bool)reflection.Invoke(wall, "TryTakeNext", nextArgs), Is.True);
            Assert.That(
                GetTileCodes(reflection.Invoke(wall, "GetDeadWallSnapshot")),
                Is.EqualTo(deadWallBefore));

            object target = collections.Item(
                reflection.Invoke(wall, "GetDeadWallSnapshot"),
                0);
            object[] specificArgs = { target, null };
            reflection.Invoke(wall, "TryTakeSpecific", specificArgs);
            Assert.That(
                GetTileCodes(reflection.Invoke(wall, "GetDeadWallSnapshot")),
                Is.EqualTo(deadWallBefore));
            Assert.That((int)reflection.GetProperty(wall, "DeadWallCount"), Is.EqualTo(14));
        }

        [Test]
        public void InitialDealAndNormalDraw_UseOnlyLiveWall()
        {
            object gameState = data.CreateGameState("East");
            object wall = reflection.GetProperty(gameState, "Wall");
            string[] deadWallBefore = GetTileCodes(
                reflection.Invoke(wall, "GetDeadWallSnapshot"));
            Type drawPurposeType = reflection.RequireType(
                "MahjongPrototype.Services.DrawPurpose, Assembly-CSharp");
            object drawService = reflection.CreateInstance(reflection.RequireType(
                "MahjongPrototype.Services.DrawService, Assembly-CSharp"));

            for (int i = 0; i < 13; i++)
            {
                object result = reflection.Invoke(
                    drawService,
                    "DrawTile",
                    data.ParseSeat("East"),
                    gameState,
                    Enum.Parse(drawPurposeType, "InitialDeal"));
                Assert.That((bool)reflection.GetProperty(result, "Success"), Is.True);
            }
            object normalResult = reflection.Invoke(
                drawService,
                "DrawTile",
                data.ParseSeat("East"),
                gameState,
                Enum.Parse(drawPurposeType, "TurnDraw"));

            Assert.That((bool)reflection.GetProperty(normalResult, "Success"), Is.True);
            Assert.That((int)reflection.GetProperty(wall, "Count"), Is.EqualTo(108));
            Assert.That(
                GetTileCodes(reflection.Invoke(wall, "GetDeadWallSnapshot")),
                Is.EqualTo(deadWallBefore));
            Assert.That((int)reflection.GetProperty(wall, "DeadWallCount"), Is.EqualTo(14));
            Assert.That(
                (int)reflection.GetProperty(wall, "RemainingRinshanTileCount"),
                Is.EqualTo(4));
        }

        [Test]
        public void RinshanDraw_UsesReservedOrderReplenishesDeadWallAndStopsAfterFour()
        {
            object wall = CreateWall();
            string[] reservedOrder = GetTileCodes(
                reflection.Invoke(wall, "GetRinshanSnapshot"));

            for (int drawIndex = 0; drawIndex < 4; drawIndex++)
            {
                int liveWallBefore = (int)reflection.GetProperty(wall, "Count");
                object liveWallSnapshot = reflection.Invoke(wall, "GetSnapshot");
                string expectedReplacement = GetTileCode(
                    collections.Item(liveWallSnapshot, liveWallBefore - 1));
                object[] args = { null };

                Assert.That(
                    (bool)reflection.Invoke(wall, "TryTakeRinshan", args),
                    Is.True);
                Assert.That(GetTileCode(args[0]), Is.EqualTo(reservedOrder[drawIndex]));
                Assert.That(
                    (int)reflection.GetProperty(wall, "Count"),
                    Is.EqualTo(liveWallBefore - 1));
                Assert.That((int)reflection.GetProperty(wall, "DeadWallCount"), Is.EqualTo(14));
                object deadWallSnapshot = reflection.Invoke(wall, "GetDeadWallSnapshot");
                Assert.That(
                    GetTileCode(collections.Item(deadWallSnapshot, 13)),
                    Is.EqualTo(expectedReplacement));
                Assert.That(
                    (int)reflection.GetProperty(wall, "RemainingRinshanTileCount"),
                    Is.EqualTo(3 - drawIndex));
            }

            int liveWallAfterFour = (int)reflection.GetProperty(wall, "Count");
            object[] rejectedArgs = { null };
            Assert.That(
                (bool)reflection.Invoke(wall, "TryTakeRinshan", rejectedArgs),
                Is.False);
            Assert.That(
                (int)reflection.GetProperty(wall, "Count"),
                Is.EqualTo(liveWallAfterFour));
            Assert.That((int)reflection.GetProperty(wall, "DeadWallCount"), Is.EqualTo(14));
        }

        private object CreateWall()
        {
            return reflection.InvokeStatic(wallType, "CreateStandardShuffled", 12345);
        }

        private string[] GetTileCodes(object tiles)
        {
            int count = collections.Count(tiles);
            string[] codes = new string[count];
            for (int i = 0; i < count; i++)
                codes[i] = GetTileCode(collections.Item(tiles, i));

            return codes;
        }

        private string GetTileCode(object tile)
        {
            return reflection.GetProperty(tile, "Code").ToString();
        }
    }
}
