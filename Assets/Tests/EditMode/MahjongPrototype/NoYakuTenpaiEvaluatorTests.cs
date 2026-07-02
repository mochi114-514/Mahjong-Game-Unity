using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class NoYakuTenpaiEvaluatorTests
    {
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string RoundWindTypeName = "MahjongPrototype.Domain.RoundWind, Assembly-CSharp";
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
        private const string NoYakuTenpaiEvaluatorTypeName =
            "MahjongPrototype.Services.NoYakuTenpaiEvaluator, Assembly-CSharp";

        [Test]
        public void Evaluate_ReturnsNotTenpaiForMissingWinningShapeWait()
        {
            object result = EvaluateNoYakuTenpai(
                CreateCatalog(),
                "1m 9m 1p 9p 1s 9s E S W N P F 5m");

            Assert.That(GetProperty(result, "IsEvaluated"), Is.True);
            Assert.That(GetProperty(result, "IsTenpai"), Is.False);
            Assert.That(GetProperty(result, "ShouldShowZeroHanTenpai"), Is.False);
        }

        [Test]
        public void Evaluate_ShowsWhenEveryWinningShapeWaitHasNoYaku()
        {
            object result = EvaluateNoYakuTenpai(
                CreateCatalog(
                    CreateDefinition("Tanyao", "One", "One"),
                    CreateDefinition("Reach", "One", "None"),
                    CreateDefinition("KokushiMusou", "None", "None", true)),
                "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P");

            Assert.That(GetProperty(result, "IsEvaluated"), Is.True);
            Assert.That(GetProperty(result, "IsTenpai"), Is.True);
            Assert.That(GetProperty(result, "HasAnyYakuWait"), Is.False);
            Assert.That(GetProperty(result, "ShouldShowZeroHanTenpai"), Is.True);
        }

        [Test]
        public void Evaluate_HidesWhenTanyaoWaitExists()
        {
            object result = EvaluateNoYakuTenpai(
                CreateCatalog(CreateDefinition("Tanyao", "One", "One")),
                "2m 3m 4m 3p 4p 5p 2s 3s 4s 6s 7s 8s 5m");

            Assert.That(GetProperty(result, "IsTenpai"), Is.True);
            Assert.That(GetProperty(result, "HasAnyYakuWait"), Is.True);
            Assert.That(GetProperty(result, "ShouldShowZeroHanTenpai"), Is.False);
        }

        [Test]
        public void Evaluate_HidesWhenReachWaitExists()
        {
            object result = EvaluateNoYakuTenpai(
                CreateCatalog(CreateDefinition("Reach", "One", "None")),
                "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P",
                isReachDeclared: true);

            Assert.That(GetProperty(result, "IsTenpai"), Is.True);
            Assert.That(GetProperty(result, "HasAnyYakuWait"), Is.True);
            Assert.That(GetProperty(result, "ShouldShowZeroHanTenpai"), Is.False);
        }

        [Test]
        public void Evaluate_HidesForYakumanWaitUsingHasYaku()
        {
            object result = EvaluateNoYakuTenpai(
                CreateCatalog(CreateDefinition("KokushiMusou", "None", "None", true)),
                "1m 9m 1p 9p 1s 9s E S W N P F C");

            Assert.That(GetProperty(result, "IsTenpai"), Is.True);
            Assert.That(GetProperty(result, "HasAnyYakuWait"), Is.True);
            Assert.That(GetProperty(result, "ShouldShowZeroHanTenpai"), Is.False);
        }

        [Test]
        public void Evaluate_ReturnsNotEvaluatedWhenEvaluatorIsMissing()
        {
            Type evaluatorType = Type.GetType(NoYakuTenpaiEvaluatorTypeName, true);
            object evaluator = Activator.CreateInstance(evaluatorType, new object[] { null });

            object result = Invoke(
                evaluator,
                "Evaluate",
                CreateTileArray("1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P"),
                ParseEnum(SeatIdTypeName, "East"),
                ParseEnum(RoundWindTypeName, "East"),
                ParseEnum(SeatIdTypeName, "East"),
                false,
                true);

            Assert.That(GetProperty(result, "IsEvaluated"), Is.False);
            Assert.That(GetProperty(result, "ShouldShowZeroHanTenpai"), Is.False);
        }

        [Test]
        public void Evaluate_ReturnsNotTenpaiForWrongHandTileCount()
        {
            object twelveTileResult = EvaluateNoYakuTenpai(
                CreateCatalog(),
                "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s");
            object fourteenTileResult = EvaluateNoYakuTenpai(
                CreateCatalog(),
                "1m 2m 3m 4m 5m 6m 7p 8p 9p 1s 2s 3s P P");

            Assert.That(GetProperty(twelveTileResult, "IsTenpai"), Is.False);
            Assert.That(GetProperty(twelveTileResult, "ShouldShowZeroHanTenpai"), Is.False);
            Assert.That(GetProperty(fourteenTileResult, "IsTenpai"), Is.False);
            Assert.That(GetProperty(fourteenTileResult, "ShouldShowZeroHanTenpai"), Is.False);
        }

        private static object EvaluateNoYakuTenpai(
            object catalog,
            string handText,
            bool isReachDeclared = false)
        {
            object winChecker = Activator.CreateInstance(Type.GetType(WinCheckerTypeName, true));
            object handEvaluator = Activator.CreateInstance(
                Type.GetType(HandEvaluatorTypeName, true),
                catalog);
            object winDeclarationEvaluator = Activator.CreateInstance(
                Type.GetType(WinDeclarationEvaluatorTypeName, true),
                winChecker,
                handEvaluator);
            object evaluator = Activator.CreateInstance(
                Type.GetType(NoYakuTenpaiEvaluatorTypeName, true),
                winDeclarationEvaluator);

            return Invoke(
                evaluator,
                "Evaluate",
                CreateTileArray(handText),
                ParseEnum(SeatIdTypeName, "East"),
                ParseEnum(RoundWindTypeName, "East"),
                ParseEnum(SeatIdTypeName, "East"),
                isReachDeclared,
                true);
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
