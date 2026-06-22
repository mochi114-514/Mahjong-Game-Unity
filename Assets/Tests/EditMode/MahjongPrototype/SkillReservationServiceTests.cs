using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class SkillReservationServiceTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string WallTypeName = "MahjongPrototype.Domain.Wall, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string MahjongGameStateTypeName = "MahjongPrototype.Domain.MahjongGameState, Assembly-CSharp";
        private const string DrawServiceTypeName = "MahjongPrototype.Services.DrawService, Assembly-CSharp";
        private const string DrawPurposeTypeName = "MahjongPrototype.Services.DrawPurpose, Assembly-CSharp";
        private const string SkillSystemTypeName = "MahjongPrototype.Skills.SkillSystem, Assembly-CSharp";
        private const string SkillEffectKindTypeName = "MahjongPrototype.Skills.SkillEffectKind, Assembly-CSharp";
        private const string PendingSkillReservationTypeName = "MahjongPrototype.Skills.PendingSkillReservation, Assembly-CSharp";
        private const string SkillReservationServiceTypeName = "MahjongPrototype.Skills.SkillReservationService, Assembly-CSharp";
        private const string MahjongGameFlowTypeName = "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";

        [Test]
        public void Reserve_StoresReservationWithOwnerSeat()
        {
            object service = CreateSkillReservationService();
            object reservation = CreateReservation("East", "ForceDrawTile", "1m", "South", 3);

            object result = Invoke(service, "Reserve", reservation, null);

            Assert.That(result, Is.True);
            Assert.That((bool)Invoke(service, "HasReservation", ParseSeat("East")), Is.True);

            object[] consumeArgs = { ParseSeat("East"), null };
            Assert.That((bool)Invoke(service, "TryConsumeForTurn", consumeArgs), Is.True);
            object consumed = consumeArgs[1];
            Assert.That(GetProperty(consumed, "OwnerSeat").ToString(), Is.EqualTo("East"));
            Assert.That(GetProperty(consumed, "SkillEffectKind").ToString(), Is.EqualTo("ForceDrawTile"));
            Assert.That(GetProperty(consumed, "TargetTile").ToString(), Is.EqualTo("1m"));
            Assert.That(GetProperty(consumed, "ReservedOnTurnSeat").ToString(), Is.EqualTo("South"));
            Assert.That(GetProperty(consumed, "ReservedTurnIndex"), Is.EqualTo(3));
        }

        [Test]
        public void TryConsumeForTurn_ConsumesOnlyMatchingSeat()
        {
            object service = CreateSkillReservationService();
            object reservation = CreateReservation("East", "ForceDrawTile", "2m", "South", 4);
            Invoke(service, "Reserve", reservation, null);

            object[] southArgs = { ParseSeat("South"), null };
            Assert.That((bool)Invoke(service, "TryConsumeForTurn", southArgs), Is.False);
            Assert.That((bool)Invoke(service, "HasReservation", ParseSeat("East")), Is.True);

            object[] eastArgs = { ParseSeat("East"), null };
            Assert.That((bool)Invoke(service, "TryConsumeForTurn", eastArgs), Is.True);
            Assert.That((bool)Invoke(service, "HasReservation", ParseSeat("East")), Is.False);
        }

        [Test]
        public void ReservationResolution_UsesSkillSystemToRegisterActiveSkillEffect()
        {
            GameObject gameObject = new GameObject("ReservationResolutionRegistersSkillTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false, "East", "South");
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                SetProperty(gameState, "CurrentSeat", ParseSeat("South"));
                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "3m");

                Assert.That(GetActiveSkillEffectCount(gameState), Is.EqualTo(0));

                SetProperty(gameState, "CurrentSeat", ParseSeat("East"));
                Invoke(gameFlow, "StartTurn", ParseSeat("East"), GetProperty(gameState, "TurnIndex"));

                Assert.That(GetActiveSkillEffectCount(gameState), Is.EqualTo(1));
                object effect = GetActiveSkillEffectAt(gameState, 0);
                Assert.That(GetProperty(effect, "OwnerSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(effect, "TargetTile").ToString(), Is.EqualTo("3m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ReservationResolution_AutoDrawAppliesReservedEffect()
        {
            GameObject gameObject = new GameObject("ReservationAutoDrawAppliesSkillTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, true, "East", "South");
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                SetProperty(gameState, "CurrentSeat", ParseSeat("South"));
                Invoke(gameFlow, "RequestForceDrawSkillForSeat", ParseSeat("East"), "4m");

                object eastSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat("East"));
                Invoke(eastSeat, "ClearDrawnTile");
                SetProperty(gameState, "CurrentSeat", ParseSeat("East"));
                Invoke(gameFlow, "StartTurn", ParseSeat("East"), GetProperty(gameState, "TurnIndex"));

                Assert.That(GetProperty(eastSeat, "DrawnTile").ToString(), Is.EqualTo("4m"));
                Assert.That(GetActiveSkillEffectCount(gameState), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CurrentTurnSkill_AfterDrawDoesNotChangeExistingDrawnTile()
        {
            GameObject gameObject = new GameObject("CurrentTurnSkillKeepsDrawnTileTest");
            try
            {
                object gameFlow = AddConfiguredGameFlow(gameObject, false, "East");
                Invoke(gameFlow, "StartNewRound");
                object gameState = GetProperty(gameFlow, "CurrentState");

                Invoke(gameFlow, "RequestDraw");
                object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat("East"));
                string drawnTileBeforeSkill = GetProperty(playerSeat, "DrawnTile").ToString();

                Invoke(gameFlow, "RequestForceDrawSkill", "5m");

                Assert.That(GetProperty(playerSeat, "DrawnTile").ToString(), Is.EqualTo(drawnTileBeforeSkill));
                Assert.That(GetActiveSkillEffectCount(gameState), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ActiveSkillEffect_DoesNotApplyOrConsumeOnOtherSeatDraw()
        {
            object gameState = CreateGameState("East", "South");
            object skillSystem = Activator.CreateInstance(Type.GetType(SkillSystemTypeName, true));
            object drawService = Activator.CreateInstance(Type.GetType(DrawServiceTypeName, true));

            Invoke(skillSystem, "ActivateForceDrawTile", gameState, ParseSeat("East"), CreateTile("6m"));
            Assert.That(GetActiveSkillEffectCount(gameState), Is.EqualTo(1));

            SetProperty(gameState, "CurrentSeat", ParseSeat("South"));
            object drawResult = Invoke(drawService, "DrawTile", ParseSeat("South"), gameState, ParseDrawPurpose("TurnDraw"));

            Assert.That(GetProperty(drawResult, "SkillWasPresent"), Is.False);
            Assert.That(GetActiveSkillEffectCount(gameState), Is.EqualTo(1));
            object effect = GetActiveSkillEffectAt(gameState, 0);
            Assert.That(GetProperty(effect, "OwnerSeat").ToString(), Is.EqualTo("East"));
        }

        private static object AddConfiguredGameFlow(GameObject gameObject, bool enableAutoDraw, params string[] activeSeats)
        {
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "enableDevLog", false);
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "initialHandTileCount", 1);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", enableAutoDraw);
            SetPrivateField(gameFlow, "initialActiveSeats", CreateSeatList(activeSeats));
            return gameFlow;
        }

        private static object CreateSkillReservationService()
        {
            return Activator.CreateInstance(Type.GetType(SkillReservationServiceTypeName, true));
        }

        private static object CreateReservation(
            string ownerSeat,
            string skillEffectKind,
            string targetTile,
            string reservedOnTurnSeat,
            int reservedTurnIndex)
        {
            Type reservationType = Type.GetType(PendingSkillReservationTypeName, true);
            return Activator.CreateInstance(
                reservationType,
                ParseSeat(ownerSeat),
                ParseSkillEffectKind(skillEffectKind),
                CreateTile(targetTile),
                ParseSeat(reservedOnTurnSeat),
                reservedTurnIndex);
        }

        private static object CreateGameState(params string[] seatNames)
        {
            Type gameStateType = Type.GetType(MahjongGameStateTypeName, true);
            Type wallType = Type.GetType(WallTypeName, true);
            MethodInfo createWall = wallType.GetMethod("CreateStandardShuffled");
            Assert.That(createWall, Is.Not.Null);

            object wall = createWall.Invoke(null, new object[] { 12345 });
            return Activator.CreateInstance(gameStateType, wall, CreateSeatList(seatNames));
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object ParseDrawPurpose(string purpose)
        {
            return Enum.Parse(Type.GetType(DrawPurposeTypeName, true), purpose);
        }

        private static object ParseSkillEffectKind(string kind)
        {
            return Enum.Parse(Type.GetType(SkillEffectKindTypeName, true), kind);
        }

        private static int GetActiveSkillEffectCount(object gameState)
        {
            object activeSkillEffects = GetProperty(gameState, "ActiveSkillEffects");
            return (int)GetProperty(activeSkillEffects, "Count");
        }

        private static object GetActiveSkillEffectAt(object gameState, int index)
        {
            object activeSkillEffects = GetProperty(gameState, "ActiveSkillEffects");
            PropertyInfo itemProperty = activeSkillEffects.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(activeSkillEffects, new object[] { index });
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(target, args);
        }

        private static object GetProperty(object target, string propertyName)
        {
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static IList CreateSeatList(params string[] seatNames)
        {
            Type seatIdType = Type.GetType(SeatIdTypeName, true);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(seatIdType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < seatNames.Length; i++)
                list.Add(ParseSeat(seatNames[i]));

            return list;
        }

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }
    }
}
