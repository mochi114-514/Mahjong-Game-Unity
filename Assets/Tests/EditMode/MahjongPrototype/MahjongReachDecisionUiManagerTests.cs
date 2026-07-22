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

        [Test]
        public void ReachDecisionStart_HidesCandidatesUntilAValidCandidateIsSelected()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateDecisionArea("ReachDecisionArea", false);
                driver.AddDecisionControllerToArea();
                driver.AssignControllerToUiManager();
                driver.CreateAndAssignWinningCandidateController();

                driver.RefreshReachDecision();

                Assert.That(driver.DecisionAreaActive, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);

                driver.SelectFirstReachCandidate();

                Assert.That(driver.HasSelectedSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningCandidateCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void ReachDecision_NonCandidateDoesNotSelectOrDisplayCandidates()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.RefreshReachDecision();

                driver.SelectReachNonCandidate();

                Assert.That(driver.HasSelectedSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
            }
        }

        [Test]
        public void UndeclaredReach_HoverDoesNotDisplayUntilTheTileIsSelected()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);

                driver.SelectSavedReachCandidate();

                Assert.That(driver.HasSelectedSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningCandidateCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void UndeclaredReach_ChangingSelectionRefreshesTheSelectedCandidateDisplay()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                Assert.That(driver.ReachCandidateCount, Is.GreaterThan(1));

                driver.SelectReachCandidate(0);
                string firstSelection = driver.SelectedTileIdentity;
                string firstSignature = driver.WinningCandidateSignature;

                driver.SelectReachCandidate(1);

                Assert.That(driver.SelectedTileIdentity, Is.Not.EqualTo(firstSelection));
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.WinningCandidateSignature, Is.Not.Empty);
                Assert.That(firstSignature, Is.Not.Empty);
            }
        }

        [Test]
        public void UndeclaredReach_ClearingSelectionHidesDisplayWithoutHoverFallback()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.SelectFirstReachCandidate();
                Assert.That(driver.WinningCandidateRootActive, Is.True);

                driver.ClearSelectionFromTable();
                driver.HoverSavedReachCandidate();

                Assert.That(driver.HasSelectedSelfTile, Is.False);
                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);
            }
        }

        [Test]
        public void UndeclaredReach_SelectedTileWithoutWaitHidesDisplayAndKeepsSelection()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();
                driver.ExitCurrentHover();
                driver.DeclineReach();
                driver.RefreshReachDecision();

                driver.SelectHandTile(0);

                Assert.That(driver.HasSelectedSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);

                driver.HoverSavedReachCandidate();

                Assert.That(driver.HasSelectedSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
            }
        }

        [Test]
        public void ReachDeclared_SelectedTileWithoutHoverDoesNotKeepCandidatesVisible()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.SelectFirstReachCandidate();
                Assert.That(driver.HasSelectedSelfTile, Is.True);

                driver.DeclareReachAndDiscardSavedCandidate();
                driver.RefreshReachDecision();

                Assert.That(driver.IsSelfReachDeclared, Is.True);
                Assert.That(driver.HasSelectedSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
            }
        }

        [Test]
        public void ReachDeclared_HoverUsesTheFixedWaitAndExitHidesItEvenWhenSelected()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.SelectFirstReachCandidate();
                driver.DeclareReachAndDiscardSavedCandidate();
                driver.RefreshReachDecision();

                driver.HoverFirstHandTile();
                string handHoverSignature = driver.WinningCandidateSignature;
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(handHoverSignature, Is.Not.Empty);

                driver.ExitCurrentHover();

                Assert.That(driver.HasSelectedSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);

                driver.SetDrawnTileDirectly("9p");
                driver.HoverCurrentDrawnTile();

                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.WinningCandidateSignature,
                    Is.EqualTo(handHoverSignature));
            }
        }

        [Test]
        public void HoverCandidates_NullStateAndDisableClearTheDisplay()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();

                driver.RefreshReachDecisionWithNullState();
                driver.InvokeUiManagerOnDisable();

                Assert.That(driver.HasHoveredSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);
            }
        }
    }
}
