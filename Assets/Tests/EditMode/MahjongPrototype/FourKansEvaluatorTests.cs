using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FourKansEvaluatorTests
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.FourKansEvaluator, Assembly-CSharp";
        private const string PlayerMeldTypeName =
            "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection = new ReflectionTestAccess();
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory data;
        private readonly object evaluator;
        private readonly Type playerMeldType;

        public FourKansEvaluatorTests()
        {
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            evaluator = reflection.CreateInstance(reflection.RequireType(EvaluatorTypeName));
            playerMeldType = reflection.RequireType(PlayerMeldTypeName);
        }

        [Test]
        public void IsSatisfied_WithFourKansOwnedByMultiplePlayersAfterFourthRinshan_ReturnsTrue()
        {
            object gameState = CreateFourPlayerState();
            AddKanForSeat(gameState, "East", "1m");
            AddKanForSeat(gameState, "South", "2m");
            AddKanForSeat(gameState, "West", "3m");
            AddKanForSeat(gameState, "West", "4m");
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.True);
        }

        [Test]
        public void IsSatisfied_CountsAnkanDaiminkanAndKakanAsKans()
        {
            object gameState = CreateFourPlayerState();
            AddKanForSeat(gameState, "East", "1m", "CreateAnkan");
            AddKanForSeat(gameState, "South", "2m", "CreateDaiminkan");
            AddKanForSeat(gameState, "West", "3m", "CreateKakan");
            AddKanForSeat(gameState, "West", "4m");
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.True);
        }

        [Test]
        public void IsSatisfied_WithThreeKans_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddKanForSeat(gameState, "East", "1m");
            AddKanForSeat(gameState, "South", "2m");
            AddKanForSeat(gameState, "West", "3m");
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.False);
        }

        [Test]
        public void IsSatisfied_WhenOnePlayerOwnsAllFourKans_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddKanForSeat(gameState, "East", "1m");
            AddKanForSeat(gameState, "East", "2m");
            AddKanForSeat(gameState, "East", "3m");
            AddKanForSeat(gameState, "East", "4m");
            object discard = data.AddDiscard(gameState, "East", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.False);
        }

        [Test]
        public void IsSatisfied_DoesNotCountChiPonOrPendingKakanPon()
        {
            object gameState = CreateFourPlayerState();
            AddMeld(gameState, "East", "CreateChi", "1m");
            AddMeld(gameState, "South", "CreatePon", "2m");
            AddMeld(gameState, "West", "CreatePon", "3m");
            AddKanForSeat(gameState, "West", "4m");
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.False);
        }

        [Test]
        public void IsSatisfied_DoesNotCountKanOwnedBySeatOutsideInput()
        {
            object gameState = CreateFourPlayerState();
            AddKanForSeat(gameState, "East", "1m");
            AddKanForSeat(gameState, "South", "2m");
            AddKanForSeat(gameState, "West", "3m");
            object outsideSeat = data.CreatePlayerSeat("North");
            AddKan(outsideSeat, "4m");
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.False);
        }

        [Test]
        public void IsSatisfied_WithDuplicateActiveSeats_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddRequiredKans(gameState);
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);

            Assert.That(
                IsSatisfied(
                    CreateSeatArray("East", "South", "West", "West"),
                    CreatePlayerSeatArray(gameState, "East", "South", "West", "North"),
                    reflection.GetProperty(gameState, "Discards"),
                    discard,
                    0),
                Is.False);
        }

        [Test]
        public void IsSatisfied_WithThreeActiveSeatsOrRemainingRinshan_ReturnsFalse()
        {
            object gameState = data.CreateGameState("East", "South", "West");
            AddKanForSeat(gameState, "East", "1m");
            AddKanForSeat(gameState, "South", "2m");
            AddKanForSeat(gameState, "West", "3m");
            AddKanForSeat(gameState, "West", "4m");
            object discard = data.AddDiscard(gameState, "West", "5m", 3);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, discard), Is.False);

            object fourPlayerState = CreateFourPlayerState();
            AddRequiredKans(fourPlayerState);
            object fourPlayerDiscard = data.AddDiscard(fourPlayerState, "West", "5m", 4);
            Assert.That(IsSatisfied(fourPlayerState, fourPlayerDiscard), Is.False);
        }

        [Test]
        public void IsSatisfied_WhenDiscardIsNotLatestOrActorOwnsNoKan_ReturnsFalse()
        {
            object gameState = CreateFourPlayerState();
            AddKanForSeat(gameState, "South", "1m");
            AddKanForSeat(gameState, "South", "2m");
            AddKanForSeat(gameState, "West", "3m");
            AddKanForSeat(gameState, "West", "4m");
            object westDiscard = data.AddDiscard(gameState, "West", "5m", 4);
            object eastDiscard = data.AddDiscard(gameState, "East", "6m", 5);
            ExhaustRinshan(gameState);

            Assert.That(IsSatisfied(gameState, westDiscard), Is.False);
            Assert.That(IsSatisfied(gameState, eastDiscard), Is.False);
        }

        [Test]
        public void IsSatisfied_WithNullOrMalformedInput_ReturnsFalseWithoutThrowing()
        {
            Assert.That(
                () => reflection.Invoke(evaluator, "IsSatisfied", null, null, null, null, 0),
                Throws.Nothing);
            Assert.That(
                (bool)reflection.Invoke(evaluator, "IsSatisfied", null, null, null, null, 0),
                Is.False);

            object gameState = CreateFourPlayerState();
            AddRequiredKans(gameState);
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);
            Assert.That(
                IsSatisfied(
                    reflection.GetProperty(gameState, "ActiveTurnSeats"),
                    CreatePlayerSeatArray(gameState, "East", "South", "West"),
                    reflection.GetProperty(gameState, "Discards"),
                    discard,
                    0),
                Is.False);

            object malformedState = CreateFourPlayerState();
            AddRequiredKans(malformedState);
            object malformedDiscard = reflection.Invoke(
                malformedState,
                "AddDiscard",
                reflection.CreateInstance(
                    types.DiscardRecord,
                    data.ParseSeat("West"),
                    data.CreateInvalidTile(),
                    4));
            ExhaustRinshan(malformedState);
            Assert.That(IsSatisfied(malformedState, malformedDiscard), Is.False);
        }

        [Test]
        public void IsSatisfied_DoesNotMutateMeldsDiscardsOrWall()
        {
            object gameState = CreateFourPlayerState();
            AddRequiredKans(gameState);
            object discard = data.AddDiscard(gameState, "West", "5m", 4);
            ExhaustRinshan(gameState);
            object wall = reflection.GetProperty(gameState, "Wall");
            int meldCountBefore = ((ICollection)reflection.GetProperty(
                data.GetPlayerSeat(gameState, "West"), "Melds")).Count;
            int discardCountBefore = ((ICollection)reflection.GetProperty(gameState, "Discards")).Count;
            int rinshanBefore = (int)reflection.GetProperty(wall, "RemainingRinshanTileCount");

            Assert.That(IsSatisfied(gameState, discard), Is.True);

            Assert.That(
                ((ICollection)reflection.GetProperty(data.GetPlayerSeat(gameState, "West"), "Melds")).Count,
                Is.EqualTo(meldCountBefore));
            Assert.That(
                ((ICollection)reflection.GetProperty(gameState, "Discards")).Count,
                Is.EqualTo(discardCountBefore));
            Assert.That(
                (int)reflection.GetProperty(wall, "RemainingRinshanTileCount"),
                Is.EqualTo(rinshanBefore));
        }

        private object CreateFourPlayerState()
        {
            return data.CreateGameState("East", "South", "West", "North");
        }

        private void AddRequiredKans(object gameState)
        {
            AddKanForSeat(gameState, "East", "1m");
            AddKanForSeat(gameState, "South", "2m");
            AddKanForSeat(gameState, "West", "3m");
            AddKanForSeat(gameState, "West", "4m");
        }

        private void AddKanForSeat(
            object gameState,
            string ownerSeatName,
            string tileCode,
            string factoryMethod = "CreateAnkan")
        {
            AddKan(data.GetPlayerSeat(gameState, ownerSeatName), tileCode, factoryMethod);
        }

        private void AddKan(object playerSeat, string tileCode, string factoryMethod = "CreateAnkan")
        {
            object ownerSeat = reflection.GetProperty(playerSeat, "SeatId");
            object meld;
            if (factoryMethod == "CreateAnkan")
            {
                meld = reflection.InvokeStatic(
                    playerMeldType,
                    factoryMethod,
                    data.CreateTileArray(tileCode, tileCode, tileCode, tileCode),
                    ownerSeat);
            }
            else
            {
                object sourceSeat = ownerSeat.Equals(data.ParseSeat("East"))
                    ? data.ParseSeat("South")
                    : data.ParseSeat("East");
                meld = reflection.InvokeStatic(
                    playerMeldType,
                    factoryMethod,
                    data.CreateTileArray(tileCode, tileCode, tileCode, tileCode),
                    ownerSeat,
                    sourceSeat,
                    data.CreateTile(tileCode),
                    1);
            }

            reflection.Invoke(playerSeat, "AddMeld", meld);
        }

        private void AddMeld(object gameState, string ownerSeatName, string factoryMethod, string tileCode)
        {
            object playerSeat = data.GetPlayerSeat(gameState, ownerSeatName);
            object ownerSeat = reflection.GetProperty(playerSeat, "SeatId");
            object sourceSeat = ownerSeat.Equals(data.ParseSeat("East"))
                ? data.ParseSeat("South")
                : data.ParseSeat("East");
            object meld;
            if (factoryMethod == "CreateChi")
            {
                meld = reflection.InvokeStatic(
                    playerMeldType,
                    factoryMethod,
                    data.CreateTileArray("1m", "2m", "3m"),
                    ownerSeat,
                    sourceSeat,
                    data.CreateTile("2m"),
                    1);
            }
            else
            {
                meld = reflection.InvokeStatic(
                    playerMeldType,
                    factoryMethod,
                    data.CreateTileArray(tileCode, tileCode, tileCode),
                    ownerSeat,
                    sourceSeat,
                    data.CreateTile(tileCode),
                    1);
            }

            reflection.Invoke(playerSeat, "AddMeld", meld);
        }

        private void ExhaustRinshan(object gameState)
        {
            object wall = reflection.GetProperty(gameState, "Wall");
            while ((int)reflection.GetProperty(wall, "RemainingRinshanTileCount") > 0)
            {
                object[] arguments = { null };
                Assert.That((bool)reflection.Invoke(wall, "TryTakeRinshan", arguments), Is.True);
            }
        }

        private object LastDiscard(object gameState)
        {
            IList discards = (IList)reflection.GetProperty(gameState, "Discards");
            return discards[discards.Count - 1];
        }

        private Array CreateSeatArray(params string[] seatNames)
        {
            Array seats = Array.CreateInstance(types.SeatId, seatNames.Length);
            for (int i = 0; i < seatNames.Length; i++)
                seats.SetValue(data.ParseSeat(seatNames[i]), i);

            return seats;
        }

        private Array CreatePlayerSeatArray(object gameState, params string[] seatNames)
        {
            Array seats = Array.CreateInstance(types.PlayerSeat, seatNames.Length);
            for (int i = 0; i < seatNames.Length; i++)
                seats.SetValue(data.GetPlayerSeat(gameState, seatNames[i]), i);

            return seats;
        }

        private bool IsSatisfied(object gameState, object discard)
        {
            return IsSatisfied(
                reflection.GetProperty(gameState, "ActiveTurnSeats"),
                CreatePlayerSeatArray(gameState, "East", "South", "West", "North"),
                reflection.GetProperty(gameState, "Discards"),
                discard,
                (int)reflection.GetProperty(
                    reflection.GetProperty(gameState, "Wall"),
                    "RemainingRinshanTileCount"));
        }

        private bool IsSatisfied(
            object activeSeats,
            object playerSeats,
            object discards,
            object discard,
            int remainingRinshanTileCount)
        {
            return (bool)reflection.Invoke(
                evaluator,
                "IsSatisfied",
                activeSeats,
                playerSeats,
                discards,
                discard,
                remainingRinshanTileCount);
        }
    }
}
