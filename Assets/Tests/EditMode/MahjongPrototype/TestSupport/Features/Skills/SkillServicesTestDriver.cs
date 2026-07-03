using System;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Skills
{
    internal sealed class SkillServicesTestDriver
    {
        private const string DrawServiceTypeName =
            "MahjongPrototype.Services.DrawService, Assembly-CSharp";
        private const string DrawPurposeTypeName =
            "MahjongPrototype.Services.DrawPurpose, Assembly-CSharp";
        private const string SkillSystemTypeName =
            "MahjongPrototype.Skills.SkillSystem, Assembly-CSharp";
        private const string SkillEffectKindTypeName =
            "MahjongPrototype.Skills.SkillEffectKind, Assembly-CSharp";
        private const string PendingSkillReservationTypeName =
            "MahjongPrototype.Skills.PendingSkillReservation, Assembly-CSharp";
        private const string SkillReservationServiceTypeName =
            "MahjongPrototype.Skills.SkillReservationService, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly Type drawPurposeType;
        private readonly Type skillEffectKindType;
        private readonly Type pendingSkillReservationType;
        private readonly object drawService;
        private readonly object skillSystem;
        private readonly object skillReservationService;

        private SkillServicesTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            Type drawPurposeType,
            Type skillEffectKindType,
            Type pendingSkillReservationType,
            object drawService,
            object skillSystem,
            object skillReservationService)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.drawPurposeType = drawPurposeType;
            this.skillEffectKindType = skillEffectKindType;
            this.pendingSkillReservationType = pendingSkillReservationType;
            this.drawService = drawService;
            this.skillSystem = skillSystem;
            this.skillReservationService = skillReservationService;
        }

        public static SkillServicesTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            Type drawPurposeType = reflection.RequireType(DrawPurposeTypeName);
            Type skillEffectKindType = reflection.RequireType(SkillEffectKindTypeName);
            Type pendingSkillReservationType = reflection.RequireType(PendingSkillReservationTypeName);

            return new SkillServicesTestDriver(
                reflection,
                collections,
                dataFactory,
                drawPurposeType,
                skillEffectKindType,
                pendingSkillReservationType,
                reflection.CreateInstance(reflection.RequireType(DrawServiceTypeName)),
                reflection.CreateInstance(reflection.RequireType(SkillSystemTypeName)),
                reflection.CreateInstance(reflection.RequireType(SkillReservationServiceTypeName)));
        }

        public object CreateReservation(
            string ownerSeat,
            string skillEffectKind,
            string targetTile,
            string reservedOnTurnSeat,
            int reservedTurnIndex)
        {
            return reflection.CreateInstance(
                pendingSkillReservationType,
                dataFactory.ParseSeat(ownerSeat),
                ParseSkillEffectKind(skillEffectKind),
                dataFactory.CreateTile(targetTile),
                dataFactory.ParseSeat(reservedOnTurnSeat),
                reservedTurnIndex);
        }

        public bool Reserve(object reservation)
        {
            return (bool)reflection.Invoke(skillReservationService, "Reserve", reservation, null);
        }

        public bool HasReservation(string ownerSeat)
        {
            return (bool)reflection.Invoke(
                skillReservationService,
                "HasReservation",
                dataFactory.ParseSeat(ownerSeat));
        }

        public bool TryConsumeForTurn(string currentTurnSeat, out object reservation)
        {
            object[] args = { dataFactory.ParseSeat(currentTurnSeat), null };
            bool consumed = (bool)reflection.Invoke(skillReservationService, "TryConsumeForTurn", args);
            reservation = args[1];
            return consumed;
        }

        public string ReservationOwnerSeat(object reservation)
        {
            return reflection.GetProperty(reservation, "OwnerSeat").ToString();
        }

        public string ReservationEffectKind(object reservation)
        {
            return reflection.GetProperty(reservation, "SkillEffectKind").ToString();
        }

        public string ReservationTargetTile(object reservation)
        {
            return reflection.GetProperty(reservation, "TargetTile").ToString();
        }

        public string ReservationReservedOnTurnSeat(object reservation)
        {
            return reflection.GetProperty(reservation, "ReservedOnTurnSeat").ToString();
        }

        public int ReservationTurnIndex(object reservation)
        {
            return (int)reflection.GetProperty(reservation, "ReservedTurnIndex");
        }

        public object CreateGameState(params string[] seatNames)
        {
            return dataFactory.CreateGameState(seatNames);
        }

        public void SetCurrentTurn(object gameState, string seatName)
        {
            dataFactory.SetCurrentTurn(gameState, seatName);
        }

        public object ActivateForceDrawTile(object gameState, string ownerSeat, string targetTile)
        {
            return reflection.Invoke(
                skillSystem,
                "ActivateForceDrawTile",
                gameState,
                dataFactory.ParseSeat(ownerSeat),
                dataFactory.CreateTile(targetTile));
        }

        public object DrawTurnTile(object gameState, string drawingSeat)
        {
            return reflection.Invoke(
                drawService,
                "DrawTile",
                dataFactory.ParseSeat(drawingSeat),
                gameState,
                ParseDrawPurpose("TurnDraw"));
        }

        public int ActiveSkillEffectCount(object gameState)
        {
            return collections.Count(reflection.GetProperty(gameState, "ActiveSkillEffects"));
        }

        public object ActiveSkillEffectAt(object gameState, int index)
        {
            return collections.Item(reflection.GetProperty(gameState, "ActiveSkillEffects"), index);
        }

        public bool SkillWasPresent(object drawResult)
        {
            return (bool)reflection.GetProperty(drawResult, "SkillWasPresent");
        }

        public string EffectOwnerSeat(object activeEffect)
        {
            return reflection.GetProperty(activeEffect, "OwnerSeat").ToString();
        }

        private object ParseDrawPurpose(string purpose)
        {
            return Enum.Parse(drawPurposeType, purpose);
        }

        private object ParseSkillEffectKind(string kind)
        {
            return Enum.Parse(skillEffectKindType, kind);
        }
    }
}

