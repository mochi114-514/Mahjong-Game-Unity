using MahjongPrototype.Tests.TestSupport.Features.Furiten;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenUiTests
    {
        private static readonly string[] SimpleFiveManWait =
        {
            "2m", "3m", "4m",
            "2p", "3p", "4p",
            "2s", "3s", "4s",
            "6s", "7s", "8s",
            "5m"
        };

        private static readonly string[] NoYakuSingleWait =
        {
            "1m", "2m", "3m",
            "4m", "5m", "6m",
            "7p", "8p", "9p",
            "1s", "2s", "3s",
            "P"
        };

        [Test]
        public void RefreshFuritenUi_SelfNotFuriten_HidesText()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddHandTiles("East", SimpleFiveManWait);

                driver.RefreshFuritenUi();

                Assert.That(driver.FuritenTextVisible, Is.False);
            }
        }

        [Test]
        public void RefreshFuritenUi_SelfDiscardFuriten_ShowsText()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddSelfFuritenHand(SimpleFiveManWait);

                driver.RefreshFuritenUi();

                Assert.That(driver.FuritenTextVisible, Is.True);
                Assert.That(driver.FuritenText, Is.EqualTo("フリテン"));
            }
        }

        [Test]
        public void RefreshFuritenUi_CpuSeatOnlyFuriten_DoesNotShowSelfUi()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(2))
            {
                driver.StartRound();
                driver.AddHandTiles("West", SimpleFiveManWait);
                driver.AddDiscard("West", "5m", 0);

                driver.RefreshFuritenUi();

                Assert.That(driver.FuritenTextVisible, Is.False);
            }
        }

        [Test]
        public void RefreshFuritenUi_OtherSeatDiscardOnly_DoesNotShow()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(2))
            {
                driver.StartRound();
                driver.AddHandTiles("East", SimpleFiveManWait);
                driver.AddDiscard("West", "5m", 0);

                driver.RefreshFuritenUi();

                Assert.That(driver.FuritenTextVisible, Is.False);
            }
        }

        [Test]
        public void HandleTileDrawn_SelfDraw_ClearsFuritenText()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddSelfFuritenHand(SimpleFiveManWait);
                driver.RefreshFuritenUi();
                driver.SetDrawnTile("East", "9m");

                driver.HandleTileDrawn("East", "9m", "TurnDraw");

                Assert.That(driver.FuritenTextVisible, Is.False);
            }
        }

        [Test]
        public void HandleTileDiscarded_SelfDiscard_ReevaluatesAndShowsFuritenText()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddHandTiles("East", SimpleFiveManWait);
                object record = driver.AddDiscard("East", "5m", 1);

                driver.HandleTileDiscarded(record);

                Assert.That(driver.FuritenTextVisible, Is.True);
            }
        }

        [Test]
        public void RoundStartedAndRoundEnded_ClearFuritenText()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddSelfFuritenHand(SimpleFiveManWait);
                driver.RefreshFuritenUi();

                driver.HandleRoundStarted(1, 70);

                Assert.That(driver.FuritenTextVisible, Is.False);

                driver.RefreshFuritenUi();
                driver.HandleRoundEnded("Win");

                Assert.That(driver.FuritenTextVisible, Is.False);
            }
        }

        [Test]
        public void Refresh_NullStateAndNotEvaluatedState_HideFuritenText()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.SetFuritenVisible(true);

                driver.RefreshNullState();

                Assert.That(driver.FuritenTextVisible, Is.False);

                driver.StartRound();
                driver.AddHandTiles(
                    "East",
                    "2m", "3m", "4m",
                    "2p", "3p", "4p",
                    "2s", "3s", "4s",
                    "6s", "7s", "8s");
                driver.AddDiscard("East", "5m", 0);

                driver.RefreshFuritenUi();

                Assert.That(driver.FuritenTextVisible, Is.False);
            }
        }

        [Test]
        public void Refresh_CanShowZeroHanTenpaiAndFuritenTogether()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddHandTiles("East", NoYakuSingleWait);
                driver.AddDiscard("East", "P", 0);

                driver.RefreshCurrentState();

                Assert.That(driver.ZeroHanTextVisible, Is.True);
                Assert.That(driver.FuritenTextVisible, Is.True);
            }
        }

        [Test]
        public void RefreshFuritenUi_DoesNotChangeGameStateHandOrDiscards()
        {
            using (FuritenUiTestDriver driver = FuritenUiTestDriver.Create(1))
            {
                driver.StartRound();
                driver.AddSelfFuritenHand(SimpleFiveManWait);
                string before = driver.SnapshotState();

                driver.RefreshFuritenUi();

                Assert.That(driver.SnapshotState(), Is.EqualTo(before));
            }
        }
    }
}
