using System.Collections;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class NineTerminalsAndHonorsEvaluatorTests
    {
        private const string EvaluatorTypeName =
            "MahjongPrototype.Services.NineTerminalsAndHonorsEvaluator, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection = new ReflectionTestAccess();
        private readonly MahjongTestTypes types;
        private readonly MahjongTestDataFactory data;
        private readonly object evaluator;

        public NineTerminalsAndHonorsEvaluatorTests()
        {
            types = new MahjongTestTypes(reflection);
            data = new MahjongTestDataFactory(reflection, types);
            evaluator = reflection.CreateInstance(reflection.RequireType(EvaluatorTypeName));
        }

        [Test]
        public void CanDeclare_EastFirstDrawWithNineDistinctTypes_ReturnsTrue()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.SetDrawnTile(gameState, "East", "W");

            Assert.That(CanDeclare(gameState, "East"), Is.True);
        }

        [Test]
        public void CanDeclare_ChildFirstDrawAllowsOtherSeatsDiscards_ReturnsTrue()
        {
            object gameState = CreateFirstDrawState(
                "South",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            data.AddDiscard(gameState, "East", "6m", 1);
            data.SetDrawnTile(gameState, "South", "2p");

            Assert.That(CanDeclare(gameState, "South"), Is.True);
        }

        [Test]
        public void CanDeclare_DrawCompletesNinthDistinctType_ReturnsTrue()
        {
            object gameState = CreateFirstDrawState(
                "West",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.SetDrawnTile(gameState, "West", "P");

            Assert.That(CanDeclare(gameState, "West"), Is.True);
        }

        [Test]
        public void CanDeclare_DuplicateTerminalOrHonorCountsOnce_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "North",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.SetDrawnTile(gameState, "North", "E");

            Assert.That(CanDeclare(gameState, "North"), Is.False);
        }

        [Test]
        public void CanDeclare_EightDistinctTypes_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E",
                "2m", "3m", "4m", "5m", "6m", "7m");
            data.SetDrawnTile(gameState, "East", "S");

            Assert.That(CanDeclare(gameState, "East"), Is.False);
        }

        [Test]
        public void CanDeclare_AfterOwnDiscard_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "South",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            data.AddDiscard(gameState, "South", "6m", 4);
            data.SetDrawnTile(gameState, "South", "P");

            Assert.That(CanDeclare(gameState, "South"), Is.False);
        }

        [Test]
        public void CanDeclare_AfterCallOrKan_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");
            data.SetDrawnTile(gameState, "East", "P");
            reflection.Invoke(gameState, "MarkCallOccurred");

            Assert.That(CanDeclare(gameState, "East"), Is.False);
        }

        [Test]
        public void CanDeclare_WithoutDrawnTile_ReturnsFalse()
        {
            object gameState = CreateFirstDrawState(
                "East",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S", "W",
                "2m", "3m", "4m", "5m");

            Assert.That(CanDeclare(gameState, "East"), Is.False);
        }

        [Test]
        public void CanDeclare_DoesNotMutateInputState()
        {
            object gameState = CreateFirstDrawState(
                "South",
                "1m", "9m", "1p", "9p", "1s", "9s", "E", "S",
                "2m", "3m", "4m", "5m", "6m");
            data.AddDiscard(gameState, "East", "7m", 1);
            data.SetDrawnTile(gameState, "South", "W");
            object playerSeat = data.GetPlayerSeat(gameState, "South");
            string handBefore = data.HandDisplayString(gameState, "South");
            string drawnTileBefore = reflection.GetProperty(
                reflection.GetProperty(playerSeat, "DrawnTile"),
                "Value").ToString();
            int discardCountBefore = ((ICollection)reflection.GetProperty(
                gameState,
                "Discards")).Count;

            Assert.That(CanDeclare(gameState, "South"), Is.True);

            Assert.That(data.HandDisplayString(gameState, "South"), Is.EqualTo(handBefore));
            object drawnTileAfter = reflection.GetProperty(playerSeat, "DrawnTile");
            Assert.That(
                reflection.GetProperty(drawnTileAfter, "Value").ToString(),
                Is.EqualTo(drawnTileBefore));
            Assert.That(
                ((ICollection)reflection.GetProperty(gameState, "Discards")).Count,
                Is.EqualTo(discardCountBefore));
            Assert.That(
                (bool)reflection.GetProperty(gameState, "HasCallOccurred"),
                Is.False);
        }

        private object CreateFirstDrawState(string currentSeat, params string[] handTiles)
        {
            object gameState = data.CreateGameState("East", "South", "West", "North");
            data.SetCurrentTurn(gameState, currentSeat);
            data.AddHandTiles(data.GetPlayerSeat(gameState, currentSeat), handTiles);
            return gameState;
        }

        private bool CanDeclare(object gameState, string seatName)
        {
            object playerSeat = data.GetPlayerSeat(gameState, seatName);
            return (bool)reflection.Invoke(
                evaluator,
                "CanDeclare",
                playerSeat,
                reflection.GetProperty(gameState, "Discards"),
                reflection.GetProperty(gameState, "HasCallOccurred"));
        }
    }
}
