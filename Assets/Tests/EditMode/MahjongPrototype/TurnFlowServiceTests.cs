using System;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Features.Turn;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class TurnFlowServiceTests
    {
        [Test]
        public void AdvanceTurn_SelectsNextActiveSeatAndUpdatesTurnIndex()
        {
            TurnFlowServiceTestDriver driver =
                TurnFlowServiceTestDriver.Create("East", "South", "West");
            driver.InitializeRound("East");

            string nextSeat = driver.AdvanceTurn();

            Assert.That(nextSeat, Is.EqualTo("South"));
            Assert.That(driver.CurrentTurnName, Is.EqualTo("South"));
            Assert.That(driver.TurnIndex, Is.EqualTo(2));
        }

        [Test]
        public void AutomationPolicy_DistinguishesLocalCpuAndRemoteParticipants()
        {
            TurnFlowServiceTestDriver driver =
                TurnFlowServiceTestDriver.Create("East", "South");
            driver.InitializeRound("East");

            object localPolicy = driver.BuildAutomationPolicy("East", false);
            object cpuPolicy = driver.BuildAutomationPolicy("South", false);
            driver.SetParticipantType("South", "RemoteHuman");
            object remotePolicy = driver.BuildAutomationPolicy("South", false);

            Assert.That(driver.PolicyIsCpu(localPolicy), Is.False);
            Assert.That(driver.PolicyUseCpuController(localPolicy), Is.False);
            Assert.That(driver.PolicyIsCpu(cpuPolicy), Is.True);
            Assert.That(driver.PolicyUseCpuController(cpuPolicy), Is.True);
            Assert.That(driver.PolicyIsCpu(remotePolicy), Is.False);
            Assert.That(driver.PolicyUseCpuController(remotePolicy), Is.False);
        }

        [Test]
        public void ReachPolicy_AutoDrawsAndAutoDiscardsOnlyAfterDraw()
        {
            TurnFlowServiceTestDriver driver = TurnFlowServiceTestDriver.Create("East");
            driver.InitializeRound("East");
            driver.DeclareReach("East");

            object policy = driver.BuildAutomationPolicy("East", false);

            Assert.That(driver.PolicyAutoDrawAtTurnStart(policy), Is.True);
            Assert.That(driver.PolicyAutoDiscardDrawnTileAfterDraw(policy), Is.True);
            Assert.That(driver.ShouldAutoDiscardDrawnTileAfterDraw("East", false), Is.False);

            driver.SetDrawnTile("East", "5m");

            Assert.That(driver.ShouldAutoDiscardDrawnTileAfterDraw("East", false), Is.True);
        }

        [Test]
        public void CurrentTurnChecks_RejectStaleTurnIndex()
        {
            TurnFlowServiceTestDriver driver = TurnFlowServiceTestDriver.Create("East");
            driver.InitializeRound("East");

            Assert.That(driver.IsSameCurrentTurn("East", 1), Is.True);
            Assert.That(driver.CanContinueAutomaticProcessing("East", 1), Is.True);
            Assert.That(driver.IsSameCurrentTurn("East", 2), Is.False);
            Assert.That(driver.CanContinueAutomaticProcessing("East", 2), Is.False);
        }

        [Test]
        public void CpuTurnController_UsesGatewayInsteadOfMahjongGameFlowParameter()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Type controllerType = reflection.RequireType(
                "MahjongPrototype.CpuTurnController, Assembly-CSharp");
            MethodInfo method = controllerType.GetMethod("TryStartCpuTurn");

            Assert.That(method, Is.Not.Null);
            Assert.That(method.GetParameters()[0].ParameterType.FullName, Is.EqualTo("MahjongPrototype.ICpuTurnGateway"));
        }
    }
}
