using System;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Unity;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class MahjongFlowEventPublisherTests
    {
        private const string PublisherTypeName =
            "MahjongPrototype.Notifications.MahjongFlowEventPublisher, Assembly-CSharp";
        private const string NotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";

        [Test]
        public void Publisher_ForwardsRoundAndAutoSortNotificationsWithOriginalArguments()
        {
            using (UnityObjectTestOwner owner = new UnityObjectTestOwner())
            {
                ReflectionTestAccess reflection = new ReflectionTestAccess();
                object notifier = owner.Own(new GameObject("MahjongFlowEventPublisherTest"))
                    .AddComponent(reflection.RequireType(NotifierTypeName));
                object publisher = reflection.CreateInstance(
                    reflection.RequireType(PublisherTypeName),
                    notifier);

                int roundStartedCount = 0;
                int receivedTurnIndex = -1;
                int receivedWallCount = -1;
                int autoSortChangedCount = 0;
                bool receivedAutoSortValue = false;
                AddEventHandler(
                    notifier,
                    "RoundStarted",
                    new Action<int, int>((turnIndex, wallCount) =>
                    {
                        roundStartedCount++;
                        receivedTurnIndex = turnIndex;
                        receivedWallCount = wallCount;
                    }));
                AddEventHandler(
                    notifier,
                    "AutoSortChanged",
                    new Action<bool>(enabled =>
                    {
                        autoSortChangedCount++;
                        receivedAutoSortValue = enabled;
                    }));

                reflection.Invoke(publisher, "NotifyRoundStarted", 3, 64);
                reflection.Invoke(publisher, "NotifyAutoSortChanged", true);

                Assert.That(roundStartedCount, Is.EqualTo(1));
                Assert.That(receivedTurnIndex, Is.EqualTo(3));
                Assert.That(receivedWallCount, Is.EqualTo(64));
                Assert.That(autoSortChangedCount, Is.EqualTo(1));
                Assert.That(receivedAutoSortValue, Is.True);
            }
        }

        [Test]
        public void Publisher_WithoutNotifier_IgnoresRunStartedNotification()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            object publisher = reflection.CreateInstance(
                reflection.RequireType(PublisherTypeName),
                new object[] { null });

            Assert.DoesNotThrow(() => reflection.Invoke(publisher, "NotifyRunStarted"));
        }

        private static void AddEventHandler(object source, string eventName, Delegate handler)
        {
            EventInfo eventInfo = source.GetType().GetEvent(eventName);
            Assert.That(eventInfo, Is.Not.Null, $"Event not found: {eventName}");
            eventInfo.AddEventHandler(source, handler);
        }
    }
}
