using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class RoundSetupServiceTests
    {
        [Test]
        public void SetupRound_OneParticipantKeepsSelfSeatAndUsesItAsFallbackStart()
        {
            RoundSetupServiceTestDriver driver = RoundSetupServiceTestDriver.Create();

            string startingSeat = driver.SetupRound(1, "North");

            Assert.That(startingSeat, Is.EqualTo("North"));
            Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo("North"));
            Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "North" }));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("North"));
            Assert.That(driver.TurnIndex, Is.EqualTo(1));
        }

        [Test]
        public void SetupRound_TwoParticipantsPreservesAcrossAssignmentAndUsesEast()
        {
            RoundSetupServiceTestDriver driver = RoundSetupServiceTestDriver.Create();

            string startingSeat = driver.SetupRound(2, "West");

            Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo("West"));
            Assert.That(driver.SeatByPlayerId("Player2"), Is.EqualTo("East"));
            Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "East", "West" }));
            Assert.That(startingSeat, Is.EqualTo("East"));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
        }

        [Test]
        public void SetupRound_ThreeParticipantsPreservesAssignmentsAndFallsBackWhenEastIsAbsent()
        {
            RoundSetupServiceTestDriver driver = RoundSetupServiceTestDriver.Create();

            string startingSeat = driver.SetupRound(3, "South");

            Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo("South"));
            Assert.That(driver.SeatByPlayerId("Player2"), Is.EqualTo("West"));
            Assert.That(driver.SeatByPlayerId("Player3"), Is.EqualTo("North"));
            Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "South", "West", "North" }));
            Assert.That(startingSeat, Is.EqualTo("South"));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("South"));
        }

        [Test]
        public void SetupRound_FourParticipantsPreservesAssignmentsAndUsesEast()
        {
            RoundSetupServiceTestDriver driver = RoundSetupServiceTestDriver.Create();

            string startingSeat = driver.SetupRound(4, "North");

            Assert.That(driver.SeatByPlayerId("Player1"), Is.EqualTo("North"));
            Assert.That(driver.SeatByPlayerId("Player2"), Is.EqualTo("East"));
            Assert.That(driver.SeatByPlayerId("Player3"), Is.EqualTo("South"));
            Assert.That(driver.SeatByPlayerId("Player4"), Is.EqualTo("West"));
            Assert.That(driver.ActiveTurnSeatNames, Is.EqualTo(new[] { "East", "South", "West", "North" }));
            Assert.That(startingSeat, Is.EqualTo("East"));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("East"));
        }

        [Test]
        public void DealInitialHands_AddsConfiguredTilesAndRemovesThemFromWall()
        {
            RoundSetupServiceTestDriver driver = RoundSetupServiceTestDriver.Create();
            driver.SetupRound(3, "East");

            bool success = driver.DealInitialHands(2);

            Assert.That(success, Is.True);
            Assert.That(driver.HandCount("East"), Is.EqualTo(2));
            Assert.That(driver.HandCount("South"), Is.EqualTo(2));
            Assert.That(driver.HandCount("West"), Is.EqualTo(2));
            Assert.That(driver.WallCount, Is.EqualTo(116));
        }

        [Test]
        public void DealInitialHands_FailsWhenWallIsEmptyWithoutAddingTiles()
        {
            RoundSetupServiceTestDriver driver = RoundSetupServiceTestDriver.Create();
            driver.SetupRound(1, "East");
            driver.ClearWall();

            bool success = driver.DealInitialHands(1);

            Assert.That(success, Is.False);
            Assert.That(driver.HandCount("East"), Is.EqualTo(0));
            Assert.That(driver.WallCount, Is.EqualTo(0));
        }
    }
}
