using MahjongPrototype.Tests.TestSupport.Features.Skills;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class SkillEffectDrawIntegrationTests
    {
        [Test]
        public void ActiveSkillEffect_DoesNotApplyOrConsumeOnOtherSeatDraw()
        {
            SkillServicesTestDriver driver = SkillServicesTestDriver.Create();
            object gameState = driver.CreateGameState("East", "South");

            driver.ActivateForceDrawTile(gameState, "East", "6m");
            Assert.That(driver.ActiveSkillEffectCount(gameState), Is.EqualTo(1));

            driver.SetCurrentTurn(gameState, "South");
            object drawResult = driver.DrawTurnTile(gameState, "South");

            Assert.That(driver.SkillWasPresent(drawResult), Is.False);
            Assert.That(driver.ActiveSkillEffectCount(gameState), Is.EqualTo(1));
            object effect = driver.ActiveSkillEffectAt(gameState, 0);
            Assert.That(driver.EffectOwnerSeat(effect), Is.EqualTo("East"));
        }

        [Test]
        public void ActiveSkillEffect_DoesNotApplyOrConsumeOnRinshanDraw()
        {
            SkillServicesTestDriver driver = SkillServicesTestDriver.Create();
            object gameState = driver.CreateGameState("East");

            driver.ActivateForceDrawTile(gameState, "East", "6m");
            object drawResult = driver.DrawRinshanTile(gameState, "East");

            Assert.That(driver.DrawSource(drawResult), Is.EqualTo("Rinshan"));
            Assert.That(driver.SkillWasPresent(drawResult), Is.False);
            Assert.That(driver.ActiveSkillEffectCount(gameState), Is.EqualTo(1));
            Assert.That(driver.WallCountAfterDraw(drawResult), Is.EqualTo(121));
        }
    }
}
