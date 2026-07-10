using MahjongPrototype.Tests.TestSupport.Features.WindProgress;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class RoundLifecycleServiceTests
    {
        [Test]
        public void InitialRound_IsEastOne()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();

            Assert.That(driver.InitialRoundWindName, Is.EqualTo("East"));
            Assert.That(driver.InitialHandNumber, Is.EqualTo(1));
        }

        [Test]
        public void ExhaustiveDrawAdvance_ProgressesFromEastOneThroughSouthFour()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            string[] expectedWinds = { "East", "East", "East", "East", "South", "South", "South", "South" };
            int[] expectedHands = { 1, 2, 3, 4, 1, 2, 3, 4 };
            string selfSeat = "East";

            for (int i = 0; i < expectedWinds.Length; i++)
            {
                driver.StartRound(expectedWinds[i], expectedHands[i], selfSeat);
                object result = driver.EndRound("WallEmpty");
                object transition = driver.AdvanceFromRoundResult();

                Assert.That(driver.RoundResultTypeName(result), Is.EqualTo("ExhaustiveDraw"));
                if (i == expectedWinds.Length - 1)
                {
                    Assert.That(driver.TransitionType(transition), Is.EqualTo("GameEnded"));
                    Assert.That(driver.TransitionRoundResult(transition), Is.SameAs(result));
                    continue;
                }

                Assert.That(driver.TransitionType(transition), Is.EqualTo("StartNextRound"));
                Assert.That(
                    driver.TransitionNextRoundWindName(transition),
                    Is.EqualTo(expectedWinds[i + 1]));
                Assert.That(
                    driver.TransitionNextHandNumber(transition),
                    Is.EqualTo(expectedHands[i + 1]));
                selfSeat = driver.TransitionNextSelfSeatName(transition);
            }
        }

        [Test]
        public void AdvanceFromRoundResult_RotatesSelfSeatForEachNextRound()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            string selfSeat = "East";
            string[] expectedNextSeats = { "North", "West", "South", "East" };

            for (int i = 0; i < expectedNextSeats.Length; i++)
            {
                driver.StartRound("East", i + 1, selfSeat);
                driver.EndRound("WallEmpty");
                object transition = driver.AdvanceFromRoundResult();

                Assert.That(driver.TransitionType(transition), Is.EqualTo("StartNextRound"));
                Assert.That(driver.TransitionNextSelfSeatName(transition), Is.EqualTo(expectedNextSeats[i]));
                selfSeat = expectedNextSeats[i];
            }
        }

        [Test]
        public void EndRound_WinCreatesResultWithSelectedCandidate()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            driver.StartRound("East", 1, "East");

            object result = driver.EndWinWithSelectedCandidate();

            Assert.That(driver.RoundResultTypeName(result), Is.EqualTo("Win"));
            Assert.That(
                driver.RoundResultSelectedCandidate(result),
                Is.SameAs(driver.LastSelectedCandidate));
            Assert.That(driver.IsRoundEnded, Is.True);
            Assert.That(driver.IsRoundResultPending, Is.True);
        }

        [Test]
        public void EndRound_WallEmptyCreatesExhaustiveDraw()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            driver.StartRound("East", 1, "East");

            object result = driver.EndRound("WallEmpty");

            Assert.That(driver.RoundResultTypeName(result), Is.EqualTo("ExhaustiveDraw"));
            Assert.That(driver.IsRoundResultPending, Is.True);
        }

        [Test]
        public void EndRound_NonResultReasonEndsRoundWithoutRoundResult()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            driver.StartRound("East", 1, "East");

            object result = driver.EndRound("WallEmptyDuringInitialDeal");

            Assert.That(result, Is.Null);
            Assert.That(driver.IsRoundEnded, Is.True);
            Assert.That(driver.IsRoundResultPending, Is.False);
            Assert.That(driver.CurrentRoundResultIsNull, Is.True);
        }

        [Test]
        public void AdvanceFromRoundResult_NormalRoundClearsResultAndReturnsNextRoundInfo()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            driver.StartRound("East", 1, "South");
            object result = driver.EndRound("WallEmpty");

            object transition = driver.AdvanceFromRoundResult();

            Assert.That(driver.TransitionType(transition), Is.EqualTo("StartNextRound"));
            Assert.That(driver.TransitionNextRoundWindName(transition), Is.EqualTo("East"));
            Assert.That(driver.TransitionNextHandNumber(transition), Is.EqualTo(2));
            Assert.That(driver.TransitionNextSelfSeatName(transition), Is.EqualTo("East"));
            Assert.That(driver.CurrentRoundResultIsNull, Is.True);
            Assert.That(driver.IsRoundResultPending, Is.False);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AdvanceFromRoundResult_FinalRoundKeepsResultAndMarksGameEnded()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            driver.StartRound("South", 4, "South");
            object result = driver.EndRound("WallEmpty");

            object transition = driver.AdvanceFromRoundResult();

            Assert.That(driver.RoundResultIsFinalRound(result), Is.True);
            Assert.That(driver.TransitionType(transition), Is.EqualTo("GameEnded"));
            Assert.That(driver.TransitionRoundResult(transition), Is.SameAs(result));
            Assert.That(driver.IsGameEnded, Is.True);
            Assert.That(driver.CurrentRoundResult, Is.SameAs(result));
        }

        [Test]
        public void AdvanceFromRoundResult_WithoutPendingResultDoesNotChangeState()
        {
            RoundLifecycleServiceTestDriver driver = RoundLifecycleServiceTestDriver.Create();
            driver.StartRound("East", 1, "East");
            object stateBefore = driver.StateToken;

            object transition = driver.AdvanceFromRoundResult();

            Assert.That(driver.TransitionType(transition), Is.EqualTo("None"));
            Assert.That(driver.StateToken, Is.SameAs(stateBefore));
            Assert.That(driver.IsRoundEnded, Is.False);
            Assert.That(driver.IsRoundResultPending, Is.False);
            Assert.That(driver.IsGameEnded, Is.False);
        }
    }
}
