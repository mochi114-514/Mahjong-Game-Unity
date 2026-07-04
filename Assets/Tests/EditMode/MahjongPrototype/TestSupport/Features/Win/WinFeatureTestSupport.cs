using System;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Features.Win
{
    internal sealed class WinFeatureTestSupport : IDisposable
    {
        private const string WinCheckerTypeName =
            "MahjongPrototype.Services.WinChecker, Assembly-CSharp";
        private const string HandEvaluatorTypeName =
            "MahjongPrototype.Services.HandEvaluator, Assembly-CSharp";
        private const string WinDeclarationEvaluatorTypeName =
            "MahjongPrototype.Services.WinDeclarationEvaluator, Assembly-CSharp";
        private const string NoYakuTenpaiEvaluatorTypeName =
            "MahjongPrototype.Services.NoYakuTenpaiEvaluator, Assembly-CSharp";
        private const string WinDeclarationEvaluationContextTypeName =
            "MahjongPrototype.Domain.WinDeclarationEvaluationContext, Assembly-CSharp";

        private readonly List<UnityEngine.Object> ownedCatalogs = new List<UnityEngine.Object>();
        private bool disposed;

        private WinFeatureTestSupport(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestTypes types,
            MahjongTestDataFactory dataFactory)
        {
            Reflection = reflection;
            Collections = collections;
            Types = types;
            DataFactory = dataFactory;
        }

        public ReflectionTestAccess Reflection { get; }
        public CollectionTestAccess Collections { get; }
        public MahjongTestTypes Types { get; }
        public MahjongTestDataFactory DataFactory { get; }

        public static WinFeatureTestSupport Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new WinFeatureTestSupport(reflection, collections, types, dataFactory);
        }

        public object CreateTiles(string handText)
        {
            return DataFactory.CreateTileArrayFromText(handText);
        }

        public object CreateWinChecker()
        {
            return Reflection.CreateInstance(Reflection.RequireType(WinCheckerTypeName));
        }

        public object CreateHandEvaluator(object catalog)
        {
            return Reflection.CreateInstance(Reflection.RequireType(HandEvaluatorTypeName), catalog);
        }

        public object CreateWinDeclarationEvaluator(object catalog)
        {
            return Reflection.CreateInstance(
                Reflection.RequireType(WinDeclarationEvaluatorTypeName),
                CreateWinChecker(),
                CreateHandEvaluator(catalog));
        }

        public object CreateNoYakuTenpaiEvaluator(object catalog)
        {
            return Reflection.CreateInstance(
                Reflection.RequireType(NoYakuTenpaiEvaluatorTypeName),
                CreateWinDeclarationEvaluator(catalog));
        }

        public object CreateNoYakuTenpaiEvaluatorWithoutWinDeclarationEvaluator()
        {
            return Reflection.CreateInstance(
                Reflection.RequireType(NoYakuTenpaiEvaluatorTypeName),
                new object[] { null });
        }

        public object CreateWinDeclarationEvaluationContext(
            string handText,
            string winningTileCode,
            string winTypeName,
            bool isReachDeclared)
        {
            return Reflection.CreateInstance(
                Reflection.RequireType(WinDeclarationEvaluationContextTypeName),
                CreateTiles(handText),
                DataFactory.CreateTile(winningTileCode),
                DataFactory.ParseWinType(winTypeName),
                DataFactory.ParseSeat("East"),
                null,
                DataFactory.ParseRoundWind("East"),
                DataFactory.ParseSeat("East"),
                isReachDeclared,
                true);
        }

        public object CreateYakuDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName,
            bool isYakuman = false)
        {
            return DataFactory.CreateYakuDefinition(
                yakuKindName,
                closedHanName,
                openHanName,
                isYakuman);
        }

        public object CreateYakuCatalog(params object[] definitions)
        {
            object catalog = DataFactory.CreateYakuCatalog(definitions);
            RegisterOwnedCatalog(catalog);
            return catalog;
        }

        public object EvaluateNoYakuTenpai(
            object evaluator,
            string handText,
            bool isReachDeclared)
        {
            return Reflection.Invoke(
                evaluator,
                "Evaluate",
                CreateTiles(handText),
                DataFactory.ParseSeat("East"),
                DataFactory.ParseRoundWind("East"),
                DataFactory.ParseSeat("East"),
                isReachDeclared,
                true);
        }

        public bool ContainsYaku(object handEvaluation, string yakuKindName)
        {
            object yakus = Reflection.GetProperty(handEvaluation, "Yakus");
            int count = Collections.Count(yakus);

            for (int i = 0; i < count; i++)
            {
                object yaku = Collections.Item(yakus, i);
                if (Reflection.GetProperty(yaku, "Kind").ToString() == yakuKindName)
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            for (int i = 0; i < ownedCatalogs.Count; i++)
            {
                if (ownedCatalogs[i] != null)
                    UnityEngine.Object.DestroyImmediate(ownedCatalogs[i]);
            }

            ownedCatalogs.Clear();
        }

        private void RegisterOwnedCatalog(object catalog)
        {
            UnityEngine.Object unityObject = catalog as UnityEngine.Object;
            if (unityObject != null && !ownedCatalogs.Contains(unityObject))
                ownedCatalogs.Add(unityObject);
        }

    }
}
