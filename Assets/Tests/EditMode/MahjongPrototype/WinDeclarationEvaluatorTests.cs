using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class WinDeclarationEvaluatorTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string RoundWindTypeName = "MahjongPrototype.Domain.RoundWind, Assembly-CSharp";
        private const string WinTypeName = "MahjongPrototype.Domain.WinType, Assembly-CSharp";
        private const string HanValueTypeName = "MahjongPrototype.Domain.HanValue, Assembly-CSharp";
        private const string YakuKindTypeName = "MahjongPrototype.Domain.YakuKind, Assembly-CSharp";
        private const string YakuDefinitionTypeName =
            "MahjongPrototype.Definitions.YakuDefinition, Assembly-CSharp";
        private const string YakuDefinitionCatalogTypeName =
            "MahjongPrototype.Definitions.YakuDefinitionCatalog, Assembly-CSharp";
        private const string WinCheckerTypeName =
            "MahjongPrototype.Services.WinChecker, Assembly-CSharp";
        private const string HandEvaluatorTypeName =
            "MahjongPrototype.Services.HandEvaluator, Assembly-CSharp";
        private const string WinDeclarationEvaluatorTypeName =
            "MahjongPrototype.Services.WinDeclarationEvaluator, Assembly-CSharp";
        private const string WinDeclarationEvaluationContextTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationContext, Assembly-CSharp";

        [Test]
        public void WinCheckerCanWinWithTile_RemainsShapeOnlyWithoutYakuCatalog()
        {
            object winChecker = Activator.CreateInstance(Type.GetType(WinCheckerTypeName, true));
            object handTiles = CreateTileArray(
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C");
            object winningTile = CreateTile("C");

            object canWin = Invoke(winChecker, "CanWinWithTile", handTiles, winningTile);

            Assert.That(canWin, Is.True);
        }

        [Test]
        public void EvaluateWithTile_ReturnsFalseWhenShapeIsMissing()
        {
            object result = EvaluateWithTile(
                CreateCatalog(),
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E S W C",
                "5m",
                "Ron");

            Assert.That(GetProperty(result, "IsWinningShape"), Is.False);
            Assert.That(GetProperty(result, "HasYaku"), Is.False);
            Assert.That(GetProperty(result, "CanDeclareWin"), Is.False);
        }

        [Test]
        public void EvaluateWithTile_ReturnsFalseWhenShapeHasNoRegisteredYaku()
        {
            object result = EvaluateWithTile(
                CreateCatalog(),
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                "C",
                "Ron");

            Assert.That(GetProperty(result, "IsWinningShape"), Is.True);
            Assert.That(GetProperty(result, "HasYaku"), Is.False);
            Assert.That(GetProperty(result, "CanDeclareWin"), Is.False);
        }

        [Test]
        public void EvaluateWithTile_ReturnsTrueForRegisteredTanyao()
        {
            object result = EvaluateWithTile(
                CreateCatalog(CreateDefinition("Tanyao", "One", "One")),
                "2m 3m 4m 2p 3p 4p 2s 3s 4s 6s 7s 8s 5m",
                "5m",
                "Ron");

            AssertCanDeclareWithTotalHan(result, 1);
            AssertYakuContains(result, "Tanyao");
        }

        [Test]
        public void EvaluateWithTile_ReturnsTrueForRegisteredSevenPairs()
        {
            object result = EvaluateWithTile(
                CreateCatalog(CreateDefinition("SevenPairs", "Two", "None")),
                "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s E E C",
                "C",
                "Ron");

            AssertCanDeclareWithTotalHan(result, 2);
            AssertYakuContains(result, "SevenPairs");
        }

        [Test]
        public void EvaluateWithTile_ReturnsYakumanForRegisteredKokushiMusou()
        {
            object result = EvaluateWithTile(
                CreateCatalog(CreateDefinition("KokushiMusou", "None", "None", true)),
                "1m 9m 1p 9p 1s 9s E S W N P F C",
                "E",
                "Ron");
            object handEvaluation = GetProperty(result, "HandEvaluationResult");

            Assert.That(GetProperty(result, "CanDeclareWin"), Is.True);
            Assert.That(GetProperty(handEvaluation, "HasYakuman"), Is.True);
            Assert.That(GetProperty(handEvaluation, "HasYaku"), Is.True);
            AssertYakuContains(result, "KokushiMusou");
        }

        [Test]
        public void EvaluateWithTile_ReturnsReachWhenReachIsDeclared()
        {
            object result = EvaluateWithTile(
                CreateCatalog(CreateDefinition("Reach", "One", "None")),
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                "C",
                "Ron",
                isReachDeclared: true);

            AssertCanDeclareWithTotalHan(result, 1);
            AssertYakuContains(result, "Reach");
        }

        [Test]
        public void EvaluateWithTile_ReturnsMenzenTsumoForClosedTsumo()
        {
            object result = EvaluateWithTile(
                CreateCatalog(CreateDefinition("MenzenTsumo", "One", "None")),
                "1m 2m 3m 1p 2p 3p 1s 2s 3s E E E C",
                "C",
                "Tsumo");

            AssertCanDeclareWithTotalHan(result, 1);
            AssertYakuContains(result, "MenzenTsumo");
        }

        private static object EvaluateWithTile(
            object catalog,
            string handText,
            string winningTileCode,
            string winTypeName,
            bool isReachDeclared = false)
        {
            object winChecker = Activator.CreateInstance(Type.GetType(WinCheckerTypeName, true));
            object handEvaluator = Activator.CreateInstance(
                Type.GetType(HandEvaluatorTypeName, true),
                catalog);
            object evaluator = Activator.CreateInstance(
                Type.GetType(WinDeclarationEvaluatorTypeName, true),
                winChecker,
                handEvaluator);
            object context = CreateContext(handText, winningTileCode, winTypeName, isReachDeclared);

            return Invoke(evaluator, "EvaluateWithTile", context);
        }

        private static object CreateContext(
            string handText,
            string winningTileCode,
            string winTypeName,
            bool isReachDeclared)
        {
            Type contextType = Type.GetType(WinDeclarationEvaluationContextTypeName, true);
            return Activator.CreateInstance(
                contextType,
                CreateTileArray(handText),
                CreateTile(winningTileCode),
                ParseEnum(WinTypeName, winTypeName),
                ParseEnum(SeatIdTypeName, "East"),
                null,
                ParseEnum(RoundWindTypeName, "East"),
                ParseEnum(SeatIdTypeName, "East"),
                isReachDeclared,
                true);
        }

        private static void AssertCanDeclareWithTotalHan(object result, int expectedTotalHan)
        {
            object handEvaluation = GetProperty(result, "HandEvaluationResult");

            Assert.That(GetProperty(result, "IsWinningShape"), Is.True);
            Assert.That(GetProperty(result, "HasYaku"), Is.True);
            Assert.That(GetProperty(result, "CanDeclareWin"), Is.True);
            Assert.That(GetProperty(handEvaluation, "TotalHan"), Is.EqualTo(expectedTotalHan));
            Assert.That(GetProperty(handEvaluation, "HasYaku"), Is.True);
        }

        private static void AssertYakuContains(object result, string yakuKindName)
        {
            object handEvaluation = GetProperty(result, "HandEvaluationResult");
            object yakus = GetProperty(handEvaluation, "Yakus");
            int count = (int)GetProperty(yakus, "Count");

            for (int i = 0; i < count; i++)
            {
                object yaku = GetListItem(yakus, i);
                if (GetProperty(yaku, "Kind").ToString() == yakuKindName)
                    return;
            }

            Assert.Fail($"Expected yaku {yakuKindName} was not evaluated.");
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

        private static object CreateDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName,
            bool isYakuman = false)
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
                isYakuman,
                true
            });
        }

        private static Array CreateTileArray(string handText)
        {
            string[] codes = handText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Type tileType = Type.GetType(TileTypeName, true);
            Array tiles = Array.CreateInstance(tileType, codes.Length);

            for (int i = 0; i < codes.Length; i++)
                tiles.SetValue(CreateTile(codes[i]), i);

            return tiles;
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
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

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
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
