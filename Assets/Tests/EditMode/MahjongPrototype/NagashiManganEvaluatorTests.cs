using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class NagashiManganEvaluatorTests
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.NagashiManganEvaluator, Assembly-CSharp";
        private const string PlayerMeldTypeName =
            "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";

        private ReflectionTestAccess reflection;
        private CollectionTestAccess collections;
        private MahjongTestTypes types;
        private MahjongTestDataFactory data;
        private object evaluator;

        [SetUp]
        public void SetUp()
        {
            reflection = new ReflectionTestAccess();
            collections = new CollectionTestAccess(reflection);
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            evaluator = reflection.CreateInstance(reflection.RequireType(EvaluatorTypeName));
        }

        [Test]
        public void Evaluate_OnlyTerminalsAndHonorsWithoutClaims_ReturnsSeat()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "1m", 1);
            data.AddDiscard(gameState, "East", "9p", 2);
            data.AddDiscard(gameState, "East", "E", 3);

            Assert.That(Evaluate(gameState), Is.EqualTo(new[] { "East" }));
        }

        [Test]
        public void Evaluate_WithAnySimpleTile_DoesNotReturnSeat()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "1m", 1);
            data.AddDiscard(gameState, "East", "5s", 2);
            data.AddDiscard(gameState, "East", "C", 3);

            Assert.That(Evaluate(gameState), Is.Empty);
        }

        [Test]
        public void Evaluate_WhenTerminalOrHonorDiscardWasClaimed_DoesNotReturnSeat()
        {
            object gameState = CreateFourPlayerState();
            object discard = data.AddDiscard(gameState, "East", "1m", 1);
            ClaimDiscard(gameState, discard, "South");

            Assert.That(Evaluate(gameState), Is.Empty);
        }

        [Test]
        public void Evaluate_SeatWithoutDiscard_DoesNotReturnSeat()
        {
            object gameState = CreateFourPlayerState();

            Assert.That(Evaluate(gameState), Is.Empty);
        }

        [Test]
        public void Evaluate_OwnOpenMeldDoesNotDisqualifyOtherwiseSatisfiedSeat()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "1m", 1);
            data.AddDiscard(gameState, "East", "N", 2);
            object southDiscard = data.AddDiscard(gameState, "South", "9p", 3);
            ClaimDiscard(gameState, southDiscard, "East");

            Assert.That(Evaluate(gameState), Is.EqualTo(new[] { "East" }));
            Assert.That(
                collections.Count(reflection.GetProperty(
                    data.GetPlayerSeat(gameState, "East"),
                    "Melds")),
                Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_MultipleSatisfiedSeats_ReturnsAllInParticipantOrder()
        {
            object gameState = CreateFourPlayerState();
            data.AddDiscard(gameState, "East", "1m", 1);
            data.AddDiscard(gameState, "West", "9s", 2);
            data.AddDiscard(gameState, "East", "P", 3);
            data.AddDiscard(gameState, "West", "S", 4);

            Assert.That(Evaluate(gameState), Is.EqualTo(new[] { "East", "West" }));
        }

        [Test]
        public void Evaluate_DoesNotMutateDiscardsClaimsOrMelds()
        {
            object gameState = CreateFourPlayerState();
            object discard = data.AddDiscard(gameState, "South", "9p", 1);
            ClaimDiscard(gameState, discard, "East");
            data.AddDiscard(gameState, "East", "1m", 2);
            int discardCount = collections.Count(reflection.GetProperty(gameState, "Discards"));
            int claimCount = collections.Count(reflection.GetProperty(gameState, "DiscardClaims"));
            int meldCount = collections.Count(reflection.GetProperty(
                data.GetPlayerSeat(gameState, "East"),
                "Melds"));

            Evaluate(gameState);

            Assert.That(
                collections.Count(reflection.GetProperty(gameState, "Discards")),
                Is.EqualTo(discardCount));
            Assert.That(
                collections.Count(reflection.GetProperty(gameState, "DiscardClaims")),
                Is.EqualTo(claimCount));
            Assert.That(
                collections.Count(reflection.GetProperty(
                    data.GetPlayerSeat(gameState, "East"),
                    "Melds")),
                Is.EqualTo(meldCount));
        }

        private object CreateFourPlayerState()
        {
            return data.CreateGameState("East", "South", "West", "North");
        }

        private string[] Evaluate(object gameState)
        {
            object result = reflection.Invoke(
                evaluator,
                "Evaluate",
                reflection.GetProperty(gameState, "ActiveSeats"),
                reflection.GetProperty(gameState, "Discards"),
                reflection.GetProperty(gameState, "DiscardClaims"));
            List<string> seats = new List<string>();
            foreach (object seat in (IEnumerable)result)
                seats.Add(seat.ToString());

            return seats.ToArray();
        }

        private void ClaimDiscard(
            object gameState,
            object discard,
            string claimingSeatName)
        {
            object sourceSeat = reflection.GetProperty(discard, "ActorSeat");
            object tile = reflection.GetProperty(discard, "Tile");
            int discardId = (int)reflection.GetProperty(discard, "Id");
            Type tileListType = typeof(List<>).MakeGenericType(types.Tile);
            IList tiles = (IList)reflection.CreateInstance(tileListType);
            tiles.Add(tile);
            tiles.Add(tile);
            tiles.Add(tile);
            object meld = reflection.InvokeStatic(
                reflection.RequireType(PlayerMeldTypeName),
                "CreatePon",
                tiles,
                data.ParseSeat(claimingSeatName),
                sourceSeat,
                tile,
                discardId);

            reflection.Invoke(
                data.GetPlayerSeat(gameState, claimingSeatName),
                "AddMeld",
                meld);
            Assert.That(reflection.Invoke(gameState, "TryClaimDiscard", meld), Is.True);
        }
    }
}
