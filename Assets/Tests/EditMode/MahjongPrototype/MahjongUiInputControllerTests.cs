using MahjongPrototype.Tests.TestSupport.Features.UiInput;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongUiInputControllerTests
    {
        [Test]
        public void AssignedControls_InvokeEventsEvenWhenObjectNamesDiffer()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerHost"))
            {
                driver.SubscribeAllRequestEvents();

                driver.TargetTileText = "5m";
                driver.EnableController();
                driver.ClickDraw();
                driver.ClickForceDrawSkill();
                driver.ToggleAutoSort(true);
                driver.ClickRetry();
                driver.ClickWin();
                driver.ClickDeclineWin();
                driver.ClickReach();
                driver.ClickDeclineReach();
                driver.ClickCancelReach();

                Assert.That(driver.DrawCount, Is.EqualTo(1));
                Assert.That(driver.SkillTarget, Is.EqualTo("5m"));
                Assert.That(driver.AutoSortValue, Is.True);
                Assert.That(driver.RetryCount, Is.EqualTo(1));
                Assert.That(driver.WinCount, Is.EqualTo(1));
                Assert.That(driver.DeclineWinCount, Is.EqualTo(1));
                Assert.That(driver.ReachCount, Is.EqualTo(1));
                Assert.That(driver.DeclineReachCount, Is.EqualTo(1));
                Assert.That(driver.CancelReachCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void MissingDrawButton_WarnsAndDoesNotAutoFindChildNamedDrawButton()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoDrawTest"))
            {
                driver.CreateUnassignedDrawButtonChild();
                driver.ClearDrawButton();
                driver.SubscribeDrawRequested();

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: DrawButton is not assigned.");

                driver.EnableController();
                driver.ClickUnassignedDrawButton();

                Assert.That(driver.DrawCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void MissingReachButton_Warns()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoReachTest"))
            {
                driver.ClearReachButton();

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: ReachButton is not assigned.");

                driver.EnableController();
            }
        }

        [Test]
        public void MissingAutoSortToggle_Warns()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerNoAutoSortTest"))
            {
                driver.ClearAutoSortToggle();

                LogAssert.Expect(LogType.Warning, "MahjongUiInputController: AutoSortToggle is not assigned.");

                driver.EnableController();
            }
        }

        [Test]
        public void SetGameplayInputInteractable_ControlsOnlyGameplayInputs()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerInteractableTest"))
            {
                driver.RetryInteractable = true;
                driver.CancelReachInteractable = true;

                driver.SetGameplayInputInteractable(false);

                Assert.That(driver.DrawInteractable, Is.False);
                Assert.That(driver.ForceDrawSkillInteractable, Is.False);
                Assert.That(driver.TargetTileInputInteractable, Is.False);
                Assert.That(driver.RetryInteractable, Is.True);
                Assert.That(driver.CancelReachInteractable, Is.True);
            }
        }

        [Test]
        public void SetAutoSortWithoutNotify_UpdatesToggleWithoutEvent()
        {
            using (MahjongUiInputControllerTestDriver driver =
                MahjongUiInputControllerTestDriver.Create("InputControllerAutoSortTest"))
            {
                driver.SubscribeAutoSortChangedCount();

                driver.SetAutoSortWithoutNotify(true);

                Assert.That(driver.AutoSortIsOn, Is.True);
                Assert.That(driver.AutoSortEventCount, Is.EqualTo(0));
            }
        }
    }
}
