using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using NUnit.Framework;

namespace MahjongPrototype.Tests
{
    public sealed class FlowServiceSeparationTests
    {
        [TestCase("MahjongPrototype.Services.WinDecisionService, Assembly-CSharp")]
        [TestCase("MahjongPrototype.Services.ReachDecisionService, Assembly-CSharp")]
        [TestCase("MahjongPrototype.Services.SkillFlowService, Assembly-CSharp")]
        [TestCase("MahjongPrototype.Services.HandAutoSortService, Assembly-CSharp")]
        public void Services_DoNotDependOnGameFlowOrEventPublisher(string typeName)
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            Type serviceType = reflection.RequireType(typeName);
            FieldInfo[] fields = serviceType.GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            for (int i = 0; i < fields.Length; i++)
            {
                Assert.That(fields[i].FieldType.FullName, Is.Not.EqualTo("MahjongPrototype.MahjongGameFlow"));
                Assert.That(fields[i].FieldType.FullName, Is.Not.EqualTo("MahjongPrototype.Notifications.MahjongFlowEventPublisher"));
            }
        }

        [Test]
        public void SkillFlowService_ReservesThenActivatesAtReservedSeatTurn()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory data = new MahjongTestDataFactory(reflection, types);
            object state = data.CreateGameState("East", "South");
            data.SetCurrentTurn(state, "East");
            object service = reflection.CreateInstance(
                reflection.RequireType("MahjongPrototype.Services.SkillFlowService, Assembly-CSharp"),
                reflection.CreateInstance(reflection.RequireType("MahjongPrototype.Skills.SkillSystem, Assembly-CSharp")),
                reflection.CreateInstance(reflection.RequireType("MahjongPrototype.Skills.SkillReservationService, Assembly-CSharp")));

            object reserved = reflection.Invoke(service, "RequestForceDraw", state, data.ParseSeat("South"), "1m");
            Assert.That(reflection.GetProperty(reserved, "Type").ToString(), Is.EqualTo("Reserved"));

            data.SetCurrentTurn(state, "South");
            object activated = reflection.Invoke(service, "ResolveReservedBeforeDraw", state, data.ParseSeat("South"));

            Assert.That(reflection.GetProperty(activated, "Type").ToString(), Is.EqualTo("Activated"));
            Assert.That(
                (int)reflection.GetProperty(reflection.GetProperty(state, "ActiveSkillEffects"), "Count"),
                Is.EqualTo(1));
        }

        [Test]
        public void HandAutoSortService_SortsSelfHandAndReportsAppliedResult()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory data = new MahjongTestDataFactory(reflection, types);
            object state = data.CreateGameState("East");
            reflection.Invoke(state, "SetSelfSeat", data.ParseSeat("East"));
            object seat = data.GetPlayerSeat(state, "East");
            data.AddHandTiles(seat, "9s", "1m", "5p");
            object service = reflection.CreateInstance(
                reflection.RequireType("MahjongPrototype.Services.HandAutoSortService, Assembly-CSharp"));

            object result = reflection.Invoke(service, "Apply", state, true, data.ParseSeat("East"), "Test");

            Assert.That(reflection.GetProperty(result, "Type").ToString(), Is.EqualTo("Applied"));
            object hand = reflection.GetProperty(seat, "Hand");
            Array tiles = (Array)reflection.Invoke(hand, "GetTiles");
            Assert.That(tiles.GetValue(0).ToString(), Is.EqualTo("1m"));
        }

        [Test]
        public void ReachDecisionService_AcceptsOnlyConfiguredReachDiscardCandidate()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory data = new MahjongTestDataFactory(reflection, types);
            object state = data.CreateGameState("East");
            object seat = data.ParseSeat("East");
            object playerSeat = data.GetPlayerSeat(state, "East");
            data.AddHandTiles(playerSeat, "1m");
            Type candidateType = reflection.RequireType(
                "MahjongPrototype.Services.ReachDiscardCandidate, Assembly-CSharp");
            Type discardSourceType = reflection.RequireType(
                "MahjongPrototype.Domain.DiscardSource, Assembly-CSharp");
            IList candidates = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(candidateType));
            candidates.Add(reflection.CreateInstance(
                candidateType,
                Enum.Parse(discardSourceType, "Hand"),
                0,
                data.CreateTile("1m")));
            reflection.Invoke(state, "BeginReachDecision", seat, candidates, 1);
            object service = reflection.CreateInstance(
                reflection.RequireType("MahjongPrototype.Services.ReachDecisionService, Assembly-CSharp"),
                reflection.CreateInstance(reflection.RequireType("MahjongPrototype.Services.ReachChecker, Assembly-CSharp")));

            object selection = reflection.Invoke(service, "BeginDiscardSelection", state, seat);

            Assert.That(reflection.GetProperty(selection, "Success"), Is.True);
            Assert.That(
                reflection.Invoke(service, "IsValidDiscardCandidate", state, seat, Enum.Parse(discardSourceType, "Hand"), 0),
                Is.True);
            Assert.That(
                reflection.Invoke(service, "IsValidDiscardCandidate", state, seat, Enum.Parse(discardSourceType, "Hand"), 1),
                Is.False);
        }
    }
}
