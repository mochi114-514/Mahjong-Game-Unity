using MahjongPrototype.Tests.TestSupport.Features.Reach;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongReachDecisionUiManagerTests
    {
        [Test]
        public void UiManagerRefreshReachDecision_ShowsSelfPendingReachDecisionArea()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateDecisionArea("RenamedReachDecisionArea", false);
                driver.AddDecisionControllerToArea();
                driver.AssignControllerToUiManager();

                driver.RefreshReachDecision();

                Assert.That(driver.IsReachDecisionPending, Is.True);
                Assert.That(driver.DecisionAreaActive, Is.True);
            }
        }

        [Test]
        public void UiManagerEnsureReachDecisionController_DoesNotAutoAddByReachDecisionAreaName()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.CreateDecisionArea("ReachDecisionArea", true);
                driver.ExpectWarning(
                    "MahjongPrototypeUiManager: MahjongReachDecisionController is not assigned. Assign it in the Inspector.");

                driver.EnsureReachDecisionController();

                Assert.That(driver.DecisionAreaHasController, Is.False);
            }
        }

        [Test]
        public void UiManagerEnsureReachDecisionController_DoesNotAutoFindChildController()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.CreateDecisionArea("RenamedReachDecisionArea", true);
                driver.AddDecisionControllerToArea();
                driver.ExpectWarning(
                    "MahjongPrototypeUiManager: MahjongReachDecisionController is not assigned. Assign it in the Inspector.");

                driver.EnsureReachDecisionController();

                Assert.That(driver.UiManagerControllerReferenceIsNull, Is.True);
            }
        }
    }
}
