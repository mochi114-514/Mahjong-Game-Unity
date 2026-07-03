using MahjongPrototype.Tests.TestSupport.Features.Reach;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongReachDecisionControllerTests
    {
        [Test]
        public void SetVisible_WithAssignedRoot_TogglesActiveEvenWhenNameDiffers()
        {
            using (MahjongReachDecisionControllerTestDriver driver =
                MahjongReachDecisionControllerTestDriver.Create())
            {
                driver.CreateDecisionRoot("RenamedReachPrompt", false);

                driver.SetVisible(true);

                Assert.That(driver.DecisionRootActive, Is.True);

                driver.SetVisible(false);

                Assert.That(driver.DecisionRootActive, Is.False);
            }
        }

        [Test]
        public void SetVisibleTrue_OnInactiveAssignedRoot_RemainsVisibleAfterEnable()
        {
            using (MahjongReachDecisionControllerTestDriver driver =
                MahjongReachDecisionControllerTestDriver.Create())
            {
                driver.CreateDecisionRoot("RenamedReachDecisionArea", false);
                driver.UseDecisionRootAsControllerHost();
                driver.AddReachDecisionControls(true);

                driver.SetVisible(true);

                Assert.That(driver.DecisionRootActive, Is.True);
                Assert.That(driver.ReachDecisionControlInteractable, Is.True);
                Assert.That(driver.DeclineReachControlInteractable, Is.True);
            }
        }

        [Test]
        public void SetReachUiVisible_ShowsDecisionAndHidesCancel()
        {
            using (MahjongReachDecisionControllerTestDriver driver =
                MahjongReachDecisionControllerTestDriver.Create())
            {
                driver.CreateDecisionRoot("ReachDecisionRoot", false);
                driver.CreateCancelRoot("ReachCancelRoot", true);

                driver.SetReachUiVisible(true, false);

                Assert.That(driver.DecisionRootActive, Is.True);
                Assert.That(driver.CancelRootActive, Is.False);
            }
        }

        [Test]
        public void SetReachUiVisible_HidesDecisionAndShowsCancel()
        {
            using (MahjongReachDecisionControllerTestDriver driver =
                MahjongReachDecisionControllerTestDriver.Create())
            {
                driver.CreateDecisionRoot("ReachDecisionRoot", true);
                driver.CreateCancelRoot("ReachCancelRoot", false);

                driver.SetReachUiVisible(false, true);

                Assert.That(driver.DecisionRootActive, Is.False);
                Assert.That(driver.CancelRootActive, Is.True);
            }
        }

        [Test]
        public void SetVisible_StillHidesCancelForBackwardCompatibility()
        {
            using (MahjongReachDecisionControllerTestDriver driver =
                MahjongReachDecisionControllerTestDriver.Create())
            {
                driver.CreateDecisionRoot("ReachDecisionRoot", false);
                driver.CreateCancelRoot("ReachCancelRoot", true);

                driver.SetVisible(true);

                Assert.That(driver.DecisionRootActive, Is.True);
                Assert.That(driver.CancelRootActive, Is.False);
            }
        }

        [Test]
        public void SetVisible_WithoutAssignedRoot_WarnsAndDoesNotUseReachDecisionAreaName()
        {
            using (MahjongReachDecisionControllerTestDriver driver =
                MahjongReachDecisionControllerTestDriver.Create())
            {
                driver.CreateControllerOnDecisionAreaNameWithoutAssignedRoot();
                driver.ExpectWarning(
                    "MahjongReachDecisionController: ReachDecisionRoot is not assigned.");

                driver.SetVisible(false);

                Assert.That(driver.DecisionRootActive, Is.True);
            }
        }
    }
}
