using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;
using MahjongPrototype.Tests.TestSupport.Unity;

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
        private const string PlayerMeldTypeName =
            "MahjongPrototype.Domain.PlayerMeld, Assembly-CSharp";

        private readonly UnityObjectTestOwner owner = new UnityObjectTestOwner();
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
            bool isReachDeclared,
            string roundWindName = "East",
            string seatWindName = "East",
            bool isClosed = true,
            bool isIppatsuEligible = false,
            bool isDoubleReachDeclared = false,
            bool isFirstTurnTsumoEligible = false,
            bool isLastLiveWallDraw = false,
            bool isLastLiveWallDiscard = false,
            object melds = null,
            bool isRinshanDraw = false)
        {
            Type contextType = Reflection.RequireType(WinDeclarationEvaluationContextTypeName);
            if (melds != null || isRinshanDraw)
            {
                return Reflection.CreateInstance(
                    contextType,
                    CreateTiles(handText),
                    DataFactory.CreateTile(winningTileCode),
                    DataFactory.ParseWinType(winTypeName),
                    DataFactory.ParseSeat("East"),
                    null,
                    DataFactory.ParseRoundWind(roundWindName),
                    DataFactory.ParseSeat(seatWindName),
                    isReachDeclared,
                    isClosed,
                    isIppatsuEligible,
                    isDoubleReachDeclared,
                    isFirstTurnTsumoEligible,
                    isLastLiveWallDraw,
                    isLastLiveWallDiscard,
                    melds,
                    isRinshanDraw);
            }

            return Reflection.CreateInstance(
                contextType,
                CreateTiles(handText),
                DataFactory.CreateTile(winningTileCode),
                DataFactory.ParseWinType(winTypeName),
                DataFactory.ParseSeat("East"),
                null,
                DataFactory.ParseRoundWind(roundWindName),
                DataFactory.ParseSeat(seatWindName),
                isReachDeclared,
                isClosed,
                isIppatsuEligible,
                isDoubleReachDeclared,
                isFirstTurnTsumoEligible,
                isLastLiveWallDraw,
                isLastLiveWallDiscard);
        }

        public object CreateOpenPonMelds(
            string tileCode,
            string callerSeatName = "East",
            string sourceSeatName = "West",
            int sourceDiscardId = 1)
        {
            Type playerMeldType = Reflection.RequireType(PlayerMeldTypeName);
            Type meldListType = typeof(List<>).MakeGenericType(playerMeldType);
            IList melds = (IList)Reflection.CreateInstance(meldListType);
            object calledTile = DataFactory.CreateTile(tileCode);
            object meld = Reflection.InvokeStatic(
                playerMeldType,
                "CreatePon",
                DataFactory.CreateTileArrayFromText(
                    string.Join(" ", tileCode, tileCode, tileCode)),
                DataFactory.ParseSeat(callerSeatName),
                DataFactory.ParseSeat(sourceSeatName),
                calledTile,
                sourceDiscardId);

            melds.Add(meld);
            return melds;
        }

        public object CreateOpenChiMelds(
            string meldTileText,
            string calledTileCode,
            string callerSeatName = "East",
            string sourceSeatName = "West",
            int sourceDiscardId = 1)
        {
            Type playerMeldType = Reflection.RequireType(PlayerMeldTypeName);
            Type meldListType = typeof(List<>).MakeGenericType(playerMeldType);
            IList melds = (IList)Reflection.CreateInstance(meldListType);
            object meld = Reflection.InvokeStatic(
                playerMeldType,
                "CreateChi",
                DataFactory.CreateTileArrayFromText(meldTileText),
                DataFactory.ParseSeat(callerSeatName),
                DataFactory.ParseSeat(sourceSeatName),
                DataFactory.CreateTile(calledTileCode),
                sourceDiscardId);

            melds.Add(meld);
            return melds;
        }

        public object CreateAnkanMelds(
            string tileCode,
            string ownerSeatName = "East")
        {
            Type playerMeldType = Reflection.RequireType(PlayerMeldTypeName);
            Type meldListType = typeof(List<>).MakeGenericType(playerMeldType);
            IList melds = (IList)Reflection.CreateInstance(meldListType);
            object meld = Reflection.InvokeStatic(
                playerMeldType,
                "CreateAnkan",
                DataFactory.CreateTileArrayFromText(
                    string.Join(" ", tileCode, tileCode, tileCode, tileCode)),
                DataFactory.ParseSeat(ownerSeatName));

            melds.Add(meld);
            return melds;
        }

        public object CreateMelds(
            string[] ankanTileCodes = null,
            string[] daiminkanTileCodes = null,
            string[] ponTileCodes = null)
        {
            Type playerMeldType = Reflection.RequireType(PlayerMeldTypeName);
            Type meldListType = typeof(List<>).MakeGenericType(playerMeldType);
            IList melds = (IList)Reflection.CreateInstance(meldListType);
            int sourceDiscardId = 1;

            AddAnkanMelds(playerMeldType, melds, ankanTileCodes);
            AddDiscardDerivedMelds(
                playerMeldType,
                melds,
                "CreateDaiminkan",
                daiminkanTileCodes,
                4,
                ref sourceDiscardId);
            AddDiscardDerivedMelds(
                playerMeldType,
                melds,
                "CreatePon",
                ponTileCodes,
                3,
                ref sourceDiscardId);
            return melds;
        }

        private void AddAnkanMelds(
            Type playerMeldType,
            IList melds,
            IReadOnlyList<string> tileCodes)
        {
            if (tileCodes == null)
                return;

            for (int i = 0; i < tileCodes.Count; i++)
            {
                string tileCode = tileCodes[i];
                melds.Add(Reflection.InvokeStatic(
                    playerMeldType,
                    "CreateAnkan",
                    DataFactory.CreateTileArrayFromText(RepeatTileCode(tileCode, 4)),
                    DataFactory.ParseSeat("East")));
            }
        }

        private void AddDiscardDerivedMelds(
            Type playerMeldType,
            IList melds,
            string factoryMethodName,
            IReadOnlyList<string> tileCodes,
            int tileCount,
            ref int sourceDiscardId)
        {
            if (tileCodes == null)
                return;

            for (int i = 0; i < tileCodes.Count; i++)
            {
                string tileCode = tileCodes[i];
                melds.Add(Reflection.InvokeStatic(
                    playerMeldType,
                    factoryMethodName,
                    DataFactory.CreateTileArrayFromText(RepeatTileCode(tileCode, tileCount)),
                    DataFactory.ParseSeat("East"),
                    DataFactory.ParseSeat("West"),
                    DataFactory.CreateTile(tileCode),
                    sourceDiscardId++));
            }
        }

        private static string RepeatTileCode(string tileCode, int count)
        {
            return string.Join(" ", new string[count].Select(_ => tileCode));
        }

        public object CreateYakuDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName,
            bool isYakuman = false,
            bool isEnabled = true)
        {
            return DataFactory.CreateYakuDefinition(
                yakuKindName,
                closedHanName,
                openHanName,
                isYakuman,
                isEnabled);
        }

        public object CreateYakuCatalog(params object[] definitions)
        {
            object catalog = DataFactory.CreateYakuCatalog(definitions);
            owner.Register(catalog);
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
            owner.Dispose();
        }
    }
}
