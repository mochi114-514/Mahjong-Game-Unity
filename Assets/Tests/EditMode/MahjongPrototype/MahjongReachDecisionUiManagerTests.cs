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
        public void UiManagerRefreshReachDecision_ShowsAndClearsCandidatesWithReachButton()
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
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.GreaterThan(0));

                driver.AcceptReach();
                driver.RefreshReachDecision();

                Assert.That(driver.DecisionAreaActive, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);
            }
        }

        [Test]
        public void UiManagerRefreshReachDecision_DeclineNullAndDisableLeaveNoCandidates()
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
                driver.DeclineReach();
                driver.RefreshReachDecision();

                Assert.That(driver.DecisionAreaActive, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.False);

                driver.RefreshReachDecisionWithNullState();
                driver.InvokeUiManagerOnDisable();

                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);
            }
        }

        [Test]
        public void HoverReachCandidate_ShowsOnlyItsWaitAndExitRestoresAutomaticGroups()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.RefreshReachDecision();
                int automaticGroupCount = driver.SpawnedWinningGroupCount;

                driver.HoverFirstReachCandidate();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));
                Assert.That(driver.SpawnedWinningCandidateCount, Is.GreaterThan(0));

                driver.ExitCurrentHover();

                Assert.That(driver.HasHoveredSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(automaticGroupCount));
            }
        }

        [Test]
        public void HoverReachCandidate_RefreshReachDecisionUiKeepsDisplayUntilHoverIsReevaluated()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();

                driver.RefreshReachDecisionUi();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.IsHoverReevaluationPending, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));

                driver.CompleteHoverReevaluation();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void HoverReachCandidate_WinCheckedKeepsDisplayUntilHoverIsReevaluated()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();

                driver.NotifyWinChecked();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.IsHoverReevaluationPending, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);

                driver.CompleteHoverReevaluation();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
            }
        }

        [Test]
        public void HandRedrawHoverExitThenReplacementEnterKeepsCandidatesForTheNewTile()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();

                driver.BeginCurrentHoverExit();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.True);

                driver.HoverSavedReachCandidate();
                driver.CompleteHoverReevaluation();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.IsHoverReevaluationPending, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void DrawnTileRedrawHoverExitThenReplacementEnterKeepsCandidates()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidateFromSource("DrawnTile");

                Assert.That(driver.WinningCandidateRootActive, Is.True);
                driver.BeginCurrentHoverExit();
                driver.HoverSavedReachCandidate();
                driver.CompleteHoverReevaluation();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.IsHoverReevaluationPending, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void HoverExitWithoutReplacementClearsAfterHoverReevaluation()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();
                driver.BeginCurrentHoverExit();

                Assert.That(driver.WinningCandidateRootActive, Is.True);

                driver.CompleteHoverReevaluation();

                Assert.That(driver.HasHoveredSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void HoverReachNonCandidate_HidesAutomaticGroupsUntilExit()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.RefreshReachDecision();
                Assert.That(driver.WinningCandidateRootActive, Is.True);

                driver.HoverReachNonCandidate();

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);

                driver.ExitCurrentHover();

                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void OpponentHover_DoesNotReplaceSelfReachAutomaticDisplay()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.RefreshReachDecision();
                int automaticGroupCount = driver.SpawnedWinningGroupCount;

                driver.HoverFirstReachCandidate("South");

                Assert.That(driver.HasHoveredSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(automaticGroupCount));
            }
        }

        [Test]
        public void NonReachDrawnTileState_HoverUsesAfterDiscardEvaluation()
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

                Assert.That(driver.WinningCandidateRootActive, Is.False);
                driver.HoverSavedReachCandidate();

                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));
                Assert.That(driver.SpawnedWinningCandidateCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void NonReachDrawnTileState_NontenHoverStaysHidden()
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

                driver.HoverHandTile(0);

                Assert.That(driver.HasHoveredSelfTile, Is.True);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningCandidateCount, Is.Zero);
            }
        }

        [Test]
        public void NonReachWithoutDrawnTile_HandHoverUsesCurrentFixedWait()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();
                driver.ExitCurrentHover();
                driver.DeclineReach();
                driver.ClearDrawnTileDirectly();
                driver.RefreshReachDecision();

                driver.HoverFirstHandTile();

                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningGroupCount, Is.EqualTo(1));
                Assert.That(driver.SpawnedWinningCandidateCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void HoverCandidates_OnDisableClearsHoverAndDisplay()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();
                Assert.That(driver.HasHoveredSelfTile, Is.True);

                driver.InvokeUiManagerOnDisable();

                Assert.That(driver.HasHoveredSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);
            }
        }

        [Test]
        public void ReachDeclared_HoverUsesSameFixedWaitForHandAndDrawnTile()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();
                driver.ExitCurrentHover();
                driver.DeclareReachAndDiscardSavedCandidate();
                driver.RefreshReachDecision();
                Assert.That(driver.IsSelfReachDeclared, Is.True);

                driver.HoverFirstHandTile();
                int handHoverCandidateCount = driver.SpawnedWinningCandidateCount;
                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(handHoverCandidateCount, Is.GreaterThan(0));
                driver.ExitCurrentHover();

                driver.SetDrawnTileDirectly("9p");
                driver.HoverCurrentDrawnTile();

                Assert.That(driver.WinningCandidateRootActive, Is.True);
                Assert.That(driver.SpawnedWinningCandidateCount,
                    Is.EqualTo(handHoverCandidateCount));
            }
        }

        [Test]
        public void HoverCandidates_NullStateClearsHoverAndDisplay()
        {
            using (MahjongReachDecisionUiManagerTestDriver driver =
                MahjongReachDecisionUiManagerTestDriver.Create())
            {
                driver.PrepareReachableGameState();
                driver.CreateAndAssignWinningCandidateController();
                driver.HoverFirstReachCandidate();
                Assert.That(driver.HasHoveredSelfTile, Is.True);

                driver.RefreshReachDecisionWithNullState();

                Assert.That(driver.HasHoveredSelfTile, Is.False);
                Assert.That(driver.WinningCandidateRootActive, Is.False);
                Assert.That(driver.SpawnedWinningGroupCount, Is.Zero);
            }
        }
    }
}
