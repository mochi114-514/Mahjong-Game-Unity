using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class WinDeclarationGameFlowTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string HanValueTypeName = "MahjongPrototype.Domain.HanValue, Assembly-CSharp";
        private const string YakuKindTypeName = "MahjongPrototype.Domain.YakuKind, Assembly-CSharp";
        private const string YakuDefinitionTypeName =
            "MahjongPrototype.Definitions.YakuDefinition, Assembly-CSharp";
        private const string YakuDefinitionCatalogTypeName =
            "MahjongPrototype.Definitions.YakuDefinitionCatalog, Assembly-CSharp";
        private const string MahjongGameFlowTypeName =
            "MahjongPrototype.MahjongGameFlow, Assembly-CSharp";
        private const string MahjongEventNotifierTypeName =
            "MahjongPrototype.Notifications.MahjongEventNotifier, Assembly-CSharp";

        [Test]
        public void WinningShapeWithoutYakuCatalog_DoesNotBeginWinDecision()
        {
            GameObject gameObject = new GameObject("NoYakuWinDeclarationFlowTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, null);
                object gameState = DrawStandardClosedTsumoShape(gameFlow);

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "PendingWinDeclarationEvaluation"), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void WinningShapeWithRegisteredYaku_BeginsWinDecisionAndStoresEvaluation()
        {
            GameObject gameObject = new GameObject("YakuWinDeclarationFlowTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(
                    gameObject,
                    CreateCatalog(CreateDefinition("MenzenTsumo", "One", "None")));
                object gameState = DrawStandardClosedTsumoShape(gameFlow);
                object evaluation = GetProperty(gameState, "PendingWinDeclarationEvaluation");
                object handEvaluation = GetProperty(evaluation, "HandEvaluationResult");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(evaluation, Is.Not.Null);
                Assert.That(GetProperty(evaluation, "CanDeclareWin"), Is.True);
                Assert.That(GetProperty(handEvaluation, "TotalHan"), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object DrawStandardClosedTsumoShape(object gameFlow)
        {
            Invoke(gameFlow, "StartNewRound");
            object gameState = GetProperty(gameFlow, "CurrentState");
            object playerSeat = Invoke(gameState, "GetPlayerSeat", ParseSeat("East"));
            AddHandTiles(
                playerSeat,
                "1m", "2m", "3m",
                "1p", "2p", "3p",
                "1s", "2s", "3s",
                "E", "E", "E",
                "C");

            Invoke(gameFlow, "RequestForceDrawSkill", "C");
            Invoke(gameFlow, "RequestDraw");
            return gameState;
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject, object catalog)
        {
            gameObject.AddComponent(Type.GetType(MahjongEventNotifierTypeName, true));
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "participantCount", 1);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            SetPrivateField(gameFlow, "yakuDefinitionCatalog", catalog);
            return gameFlow;
        }

        private static void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                Invoke(hand, "Add", CreateTile(tileCodes[i]));
        }

        private static object CreateCatalog(params object[] definitions)
        {
            Type catalogType = Type.GetType(YakuDefinitionCatalogTypeName, true);
            object catalog = ScriptableObject.CreateInstance(catalogType);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(
                Type.GetType(YakuDefinitionTypeName, true));
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < definitions.Length; i++)
                list.Add(definitions[i]);

            SetPrivateField(catalog, "definitions", list);
            return catalog;
        }

        private static object CreateDefinition(string yakuKindName, string closedHanName, string openHanName)
        {
            Type definitionType = Type.GetType(YakuDefinitionTypeName, true);
            Type yakuKindType = Type.GetType(YakuKindTypeName, true);
            Type hanValueType = Type.GetType(HanValueTypeName, true);
            ConstructorInfo constructor = definitionType.GetConstructor(new[]
            {
                yakuKindType,
                typeof(string),
                hanValueType,
                hanValueType,
                typeof(bool),
                typeof(bool)
            });
            Assert.That(constructor, Is.Not.Null);

            return constructor.Invoke(new[]
            {
                ParseEnum(YakuKindTypeName, yakuKindName),
                yakuKindName,
                ParseEnum(HanValueTypeName, closedHanName),
                ParseEnum(HanValueTypeName, openHanName),
                false,
                true
            });
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object ParseSeat(string seatName)
        {
            return ParseEnum(SeatIdTypeName, seatName);
        }

        private static object ParseEnum(string typeName, string valueName)
        {
            return Enum.Parse(Type.GetType(typeName, true), valueName);
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
