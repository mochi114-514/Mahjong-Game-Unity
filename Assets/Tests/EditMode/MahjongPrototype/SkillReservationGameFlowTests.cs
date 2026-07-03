using MahjongPrototype.Tests.TestSupport.Features.Skills;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class SkillReservationGameFlowTests
    {
        [Test]
        public void ReservationResolution_UsesSkillSystemToRegisterActiveSkillEffect()
        {
            using (SkillReservationGameFlowTestDriver driver =
                SkillReservationGameFlowTestDriver.CreateReservationResolutionScenario())
            {
                driver.StartRound();

                driver.SetCurrentTurn("West");
                driver.RequestForceDrawSkillForSeat("East", "3m");

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(0));

                driver.SetCurrentTurn("East");
                driver.StartTurn("East");

                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(1));
                object effect = driver.ActiveSkillEffectAt(0);
                Assert.That(driver.EffectOwnerSeat(effect), Is.EqualTo("East"));
                Assert.That(driver.EffectTargetTile(effect), Is.EqualTo("3m"));
            }
        }

        [Test]
        public void ReservationResolution_AutoDrawAppliesReservedEffect()
        {
            using (SkillReservationGameFlowTestDriver driver =
                SkillReservationGameFlowTestDriver.CreateAutoDrawReservationScenario())
            {
                driver.StartRound();

                driver.SetCurrentTurn("West");
                driver.RequestForceDrawSkillForSeat("East", "4m");

                driver.ClearDrawnTile("East");
                driver.SetCurrentTurn("East");
                driver.StartTurn("East");

                Assert.That(driver.DrawnTile("East"), Is.EqualTo("4m"));
                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void CurrentTurnSkill_AfterDrawDoesNotChangeExistingDrawnTile()
        {
            using (SkillReservationGameFlowTestDriver driver =
                SkillReservationGameFlowTestDriver.CreateCurrentTurnSkillScenario())
            {
                driver.StartRound();

                driver.RequestDraw();
                string drawnTileBeforeSkill = driver.DrawnTile("East");

                driver.RequestForceDrawSkill("5m");

                Assert.That(driver.DrawnTile("East"), Is.EqualTo(drawnTileBeforeSkill));
                Assert.That(driver.ActiveSkillEffectCount, Is.EqualTo(1));
            }
        }
    }
}

