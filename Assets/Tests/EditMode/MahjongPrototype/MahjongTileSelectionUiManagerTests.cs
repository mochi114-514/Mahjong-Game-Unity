using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongTileSelectionUiManagerTests
    {
        [Test]
        public void UiManagerSubscribesToRaw3DTileClicksExactlyOnce()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.SubscribeTileClicksTwice();

                Assert.That(fixture.HandClickSubscriptionCount, Is.EqualTo(1));
                Assert.That(fixture.DrawnClickSubscriptionCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void HandClick_FirstSelectsAndSecondDiscardsExactlyOnce()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                object tile = fixture.HandTileAt(0);

                fixture.ClickHand(0, tile);

                Assert.That(fixture.DiscardCount, Is.Zero);
                Assert.That(fixture.HasSelection, Is.True);
                Assert.That(fixture.SelectedSource, Is.EqualTo("Hand"));
                Assert.That(fixture.SelectedHandIndex, Is.Zero);

                fixture.ClickHand(0, tile);

                Assert.That(fixture.DiscardCount, Is.EqualTo(1));
                Assert.That(fixture.HasSelection, Is.False);

                fixture.ClickHand(0, tile);
                Assert.That(fixture.DiscardCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void DifferentHandTile_SwitchesSelectionWithoutDiscard()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.ClickHand(0, fixture.HandTileAt(0));
                fixture.ClickHand(1, fixture.HandTileAt(1));

                Assert.That(fixture.DiscardCount, Is.Zero);
                Assert.That(fixture.SelectedSource, Is.EqualTo("Hand"));
                Assert.That(fixture.SelectedHandIndex, Is.EqualTo(1));
            }
        }

        [Test]
        public void HandAndDrawnTile_SwitchExclusivelyAndSecondDrawnClickDiscards()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                object handTile = fixture.HandTileAt(0);
                object drawnTile = fixture.DrawnTile;

                fixture.ClickHand(0, handTile);
                fixture.ClickDrawn(drawnTile);

                Assert.That(fixture.DiscardCount, Is.Zero);
                Assert.That(fixture.SelectedSource, Is.EqualTo("DrawnTile"));

                fixture.ClickHand(0, handTile);
                Assert.That(fixture.SelectedSource, Is.EqualTo("Hand"));

                fixture.ClickDrawn(drawnTile);
                fixture.ClickDrawn(drawnTile);

                Assert.That(fixture.DiscardCount, Is.EqualTo(1));
                Assert.That(fixture.FirstDiscardSource, Is.EqualTo("DrawnTile"));
            }
        }

        [Test]
        public void TableClickClearsSelection_AndOpponentClickDoesNotChangeIt()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                object selectedTile = fixture.HandTileAt(0);
                fixture.ClickHand(0, selectedTile);

                fixture.ClickOpponentHand(0, selectedTile);

                Assert.That(fixture.HasSelection, Is.True);
                Assert.That(fixture.SelectedHandIndex, Is.Zero);

                fixture.ClickTable();

                Assert.That(fixture.HasSelection, Is.False);
                Assert.That(fixture.DiscardCount, Is.Zero);
            }
        }

        [Test]
        public void InteractableRefresh_KeepsStillValidSelection()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.SelectFirstHandTile();

                fixture.RefreshInteraction();

                Assert.That(fixture.HasSelection, Is.True);
                Assert.That(fixture.DiscardCount, Is.Zero);
            }
        }

        [Test]
        public void StaleTurnOrStaleHandIdentity_DoesNotDiscard()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                object oldTile = fixture.HandTileAt(0);
                fixture.ClickHand(0, oldTile);
                fixture.AdvanceTurnIndexOnly();

                fixture.ClickHand(0, oldTile);

                Assert.That(fixture.DiscardCount, Is.Zero);
                Assert.That(fixture.HasSelection, Is.True);

                fixture.MoveFirstHandTileToEnd();
                fixture.ClickHand(0, oldTile);

                Assert.That(fixture.DiscardCount, Is.Zero);
                Assert.That(fixture.HasSelection, Is.False);
            }
        }

        [Test]
        public void OptionalWinDecision_FirstClickDoesNotDecline_SecondClickUsesDiscardPath()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                object tile = fixture.HandTileAt(0);
                fixture.BeginWinDecision();

                fixture.ClickHand(0, tile);

                Assert.That(fixture.IsWinDecisionPending, Is.True);
                Assert.That(fixture.DiscardCount, Is.Zero);

                fixture.ClickHand(0, tile);

                Assert.That(fixture.IsWinDecisionPending, Is.False);
                Assert.That(fixture.DiscardCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void ReachDiscardSelection_DoesNotSelectNonCandidate()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.BeginDrawnTileOnlyReachDiscardSelection();

                fixture.ClickHand(0, fixture.HandTileAt(0));

                Assert.That(fixture.HasSelection, Is.False);
                Assert.That(fixture.DiscardCount, Is.Zero);
            }
        }

        [Test]
        public void RedrawSortTurnMeldInputDisableAndRoundEnd_ClearSelection()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.SelectFirstHandTile();
                fixture.NotifySelfHandRedraw();
                Assert.That(fixture.HasSelection, Is.False, "hand redraw");

                fixture.ClickDrawn(fixture.DrawnTile);
                fixture.NotifySelfDrawnTileRedraw();
                Assert.That(fixture.HasSelection, Is.False, "drawn tile redraw");

                fixture.SelectFirstHandTile();
                fixture.NotifyAutoSort();
                Assert.That(fixture.HasSelection, Is.False, "auto sort");

                fixture.SelectFirstHandTile();
                fixture.NotifyTurnStarted();
                Assert.That(fixture.HasSelection, Is.False, "turn change");

                fixture.SelectFirstHandTile();
                fixture.NotifySelfMeldDeclared();
                Assert.That(fixture.HasSelection, Is.False, "meld");

                fixture.SelectFirstHandTile();
                fixture.DisableSelfTileInput();
                Assert.That(fixture.HasSelection, Is.False, "input disabled");

                fixture.RestoreSelfTurn();
                fixture.SelectFirstHandTile();
                fixture.EndRound();
                Assert.That(fixture.HasSelection, Is.False, "round end");
            }
        }

        [Test]
        public void OnDisable_ClearsSelection_WhenPresenterIsValid()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.SelectFirstHandTile();

                Assert.DoesNotThrow(() => fixture.InvokeUiManagerOnDisable());

                Assert.That(fixture.HasSelection, Is.False);
            }
        }

        [Test]
        public void OnDisable_ClearsSelection_WhenPresenterWasDestroyedFirst()
        {
            using (SelectionFixture fixture = new SelectionFixture())
            {
                fixture.SelectFirstHandTile();
                fixture.DestroyPresenter();

                Assert.DoesNotThrow(() => fixture.InvokeUiManagerOnDisable());

                Assert.That(fixture.HasSelection, Is.False);
            }
        }

        private sealed class SelectionFixture : IDisposable
        {
            private const string FlowTypeName =
                "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
            private const string NotifierTypeName =
                "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";
            private const string InputTypeName =
                "MahjongPrototype.UI.MahjongUiInputController, Assembly-CSharp";
            private const string RouterTypeName =
                "MahjongPrototype.UI.MahjongUiCommandRouter, Assembly-CSharp";
            private const string ManagerTypeName =
                "MahjongPrototype.UI.MahjongPrototypeUiManager, Assembly-CSharp";
            private const string PresenterTypeName =
                "MahjongPrototype.UI3D.Mahjong3DPlayerAreaPresenter, Assembly-CSharp";
            private const string SeatTypeName =
                "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
            private const string DiscardSourceTypeName =
                "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp";
            private const string ReachCandidateTypeName =
                "MahjongPrototype.Services.ReachDiscardCandidate, Assembly-CSharp";
            private const string PlayerMeldTypeName =
                "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";

            private readonly GameObject root;
            private readonly object flow;
            private readonly object presenter;
            private readonly object manager;
            private readonly object state;
            private readonly object east;
            private readonly bool previousIgnoreFailingMessages;

            public SelectionFixture()
            {
                previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true;

                root = new GameObject("MahjongTileSelectionUiManagerTest");
                root.SetActive(false);
                root.AddComponent(RequireType(NotifierTypeName));
                flow = root.AddComponent(RequireType(FlowTypeName));
                ConfigureFlow(flow);
                object input = root.AddComponent(RequireType(InputTypeName));
                presenter = root.AddComponent(RequireType(PresenterTypeName));
                object router = root.AddComponent(RequireType(RouterTypeName));
                manager = root.AddComponent(RequireType(ManagerTypeName));

                SetPrivateField(router, "gameFlow", flow);
                SetPrivateField(router, "inputController", input);
                SetPrivateField(manager, "gameFlow", flow);
                SetPrivateField(manager, "playerArea3DPresenter", presenter);
                SetPrivateField(manager, "commandRouter", router);

                Invoke(flow, "StartNewRound");
                Invoke(flow, "RequestDraw");
                state = GetProperty(flow, "CurrentState");
                east = Enum.Parse(RequireType(SeatTypeName), "East");
            }

            public int DiscardCount => CollectionCount(GetProperty(state, "Discards"));

            public bool HasSelection => GetPrivateField(manager, "selectedSelfTile") != null;

            public string SelectedSource =>
                GetProperty(GetPrivateField(manager, "selectedSelfTile"), "Source").ToString();

            public int SelectedHandIndex =>
                (int)GetProperty(GetPrivateField(manager, "selectedSelfTile"), "HandIndex");

            public object DrawnTile => GetProperty(SelfPlayer, "DrawnTile");

            public string FirstDiscardSource =>
                GetProperty(CollectionItem(GetProperty(state, "Discards"), 0), "Source").ToString();

            public bool IsWinDecisionPending =>
                (bool)GetProperty(state, "IsWinDecisionPending");

            public int HandClickSubscriptionCount =>
                CountEventSubscriberTarget(presenter, "HandTileClicked", manager);

            public int DrawnClickSubscriptionCount =>
                CountEventSubscriberTarget(presenter, "DrawnTileClicked", manager);

            private object SelfPlayer => Invoke(state, "GetPlayerSeat", east);

            public object HandTileAt(int index)
            {
                object hand = GetProperty(SelfPlayer, "Hand");
                return CollectionItem(Invoke(hand, "GetTiles"), index);
            }

            public void ClickHand(int handIndex, object tile)
            {
                Invoke(manager, "HandleHandTileClicked", east, handIndex, tile);
            }

            public void ClickDrawn(object tile)
            {
                Invoke(manager, "HandleDrawnTileClicked", east, tile);
            }

            public void ClickOpponentHand(int handIndex, object tile)
            {
                object south = Enum.Parse(RequireType(SeatTypeName), "South");
                Invoke(manager, "HandleHandTileClicked", south, handIndex, tile);
            }

            public void ClickTable()
            {
                Invoke(manager, "HandleTableInputSurfaceClicked");
            }

            public void SubscribeTileClicksTwice()
            {
                Invoke(manager, "SubscribeTileHoverPresentation");
                Invoke(manager, "SubscribeTileHoverPresentation");
            }

            public void SelectFirstHandTile()
            {
                ClickHand(0, HandTileAt(0));
                Assert.That(HasSelection, Is.True);
            }

            public void AdvanceTurnIndexOnly()
            {
                SetProperty(state, "TurnIndex", (int)GetProperty(state, "TurnIndex") + 1);
            }

            public void MoveFirstHandTileToEnd()
            {
                object hand = GetProperty(SelfPlayer, "Hand");
                object tile = HandTileAt(0);
                Invoke(hand, "TryRemoveAt", 0, Activator.CreateInstance(tile.GetType()));
                Invoke(hand, "Add", tile);
            }

            public void BeginWinDecision()
            {
                Invoke(state, "BeginWinDecision", east, GetProperty(state, "TurnIndex"));
            }

            public void BeginDrawnTileOnlyReachDiscardSelection()
            {
                Type candidateType = RequireType(ReachCandidateTypeName);
                object source = Enum.Parse(RequireType(DiscardSourceTypeName), "DrawnTile");
                object candidate = Activator.CreateInstance(
                    candidateType,
                    source,
                    -1,
                    DrawnTile);
                IList candidates = (IList)Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(candidateType));
                candidates.Add(candidate);
                Invoke(state, "BeginReachDecision", east, candidates, GetProperty(state, "TurnIndex"));
                Invoke(state, "BeginReachDiscardSelection", east);
            }

            public void NotifySelfHandRedraw()
            {
                Invoke(manager, "RefreshPlayerHand3DForSeat", state, east);
            }

            public void NotifySelfDrawnTileRedraw()
            {
                Invoke(manager, "RefreshPlayerDrawnTile3DForSeat", state, east);
            }

            public void NotifyAutoSort()
            {
                Invoke(manager, "HandleHandAutoSorted", east, GetProperty(state, "TurnIndex"));
            }

            public void NotifyTurnStarted()
            {
                Invoke(manager, "HandleTurnStarted", east, GetProperty(state, "TurnIndex"));
            }

            public void NotifySelfMeldDeclared()
            {
                Type tileType = HandTileAt(0).GetType();
                IList tiles = (IList)Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(tileType));
                object tile = HandTileAt(0);
                for (int i = 0; i < 4; i++)
                    tiles.Add(tile);

                MethodInfo createAnkan = RequireType(PlayerMeldTypeName).GetMethod(
                    "CreateAnkan",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.That(createAnkan, Is.Not.Null);
                object meld = createAnkan.Invoke(null, new[] { tiles, east });
                Invoke(manager, "HandleMeldDeclared", meld);
            }

            public void DisableSelfTileInput()
            {
                object south = Enum.Parse(RequireType(SeatTypeName), "South");
                SetProperty(state, "CurrentTurn", south);
                Invoke(manager, "RefreshInteractionState", state);
            }

            public void RefreshInteraction()
            {
                Invoke(manager, "RefreshInteractionState", state);
            }

            public void RestoreSelfTurn()
            {
                SetProperty(state, "CurrentTurn", east);
            }

            public void EndRound()
            {
                Invoke(state, "EndRoundWithoutResult");
                Invoke(manager, "HandleRoundEnded", "Test");
            }

            public void InvokeUiManagerOnDisable()
            {
                Invoke(manager, "OnDisable");
            }

            public void DestroyPresenter()
            {
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)presenter);
            }

            public void Dispose()
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                UnityEngine.Object.DestroyImmediate(root);
            }

            private static void ConfigureFlow(object gameFlow)
            {
                SetPrivateField(gameFlow, "logWarnings", false);
                SetPrivateField(gameFlow, "initialHandTileCount", 3);
                SetPrivateField(gameFlow, "autoStart", false);
                SetPrivateField(gameFlow, "useFixedRandomSeed", true);
                SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
                SetPrivateField(gameFlow, "enableAutoDraw", false);
                SetPrivateField(gameFlow, "randomizeSelfSeat", false);
                SetPrivateField(
                    gameFlow,
                    "fixedSelfSeat",
                    Enum.Parse(RequireType(SeatTypeName), "East"));
            }
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo[] methods = target.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name == methodName && method.GetParameters().Length == args.Length)
                    return method.Invoke(target, args);
            }

            Assert.Fail($"Method not found: {target.GetType().FullName}.{methodName}");
            return null;
        }

        private static object GetProperty(object target, string propertyName)
        {
            Assert.That(target, Is.Not.Null, $"Cannot get {propertyName} from null.");
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(target);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null);
            property.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static int CollectionCount(object collection)
        {
            return collection is ICollection nonGeneric
                ? nonGeneric.Count
                : (int)collection.GetType().GetProperty("Count").GetValue(collection);
        }

        private static int CountEventSubscriberTarget(
            object target,
            string eventName,
            object subscriberTarget)
        {
            FieldInfo eventField = target.GetType().GetField(
                eventName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate handler = eventField?.GetValue(target) as Delegate;
            if (handler == null)
                return 0;

            int count = 0;
            Delegate[] delegates = handler.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                if (ReferenceEquals(delegates[i].Target, subscriberTarget))
                    count++;
            }

            return count;
        }

        private static object CollectionItem(object collection, int index)
        {
            if (collection is IList list)
                return list[index];

            PropertyInfo item = collection.GetType().GetProperty("Item");
            return item.GetValue(collection, new object[] { index });
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName, true);
            Assert.That(type, Is.Not.Null);
            return type;
        }
    }
}
