using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace MahjongPrototype.Tests
{
    public sealed class FuritenRonGameFlowTests
    {
        private const string SeatIdTypeName = "MahjongPrototype.Domain.SeatId, Assembly-CSharp";
        private const string TileTypeName = "MahjongPrototype.Domain.Tile, Assembly-CSharp";
        private const string ParticipantTypeTypeName =
            "MahjongPrototype.Domain.ParticipantType, Assembly-CSharp";
        private const string DiscardRecordTypeName =
            "MahjongPrototype.Domain.DiscardRecord, Assembly-CSharp";
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

        private static readonly string[] SimpleFiveManWait =
        {
            "2m", "3m", "4m",
            "2p", "3p", "4p",
            "2s", "3s", "4s",
            "6s", "7s", "8s",
            "5m"
        };

        private static readonly string[] MultiWait =
        {
            "2p", "3p", "4p",
            "3p", "4p", "5p",
            "2s", "3s", "4s",
            "6s", "6s",
            "4m", "5m"
        };

        [Test]
        public void RonDecision_NonFuritenCandidate_CanRon()
        {
            GameObject gameObject = new GameObject("FuritenRonNonFuritenTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = StartRound(gameFlow);
                AddHandTiles(GetPlayerSeat(gameState, "East"), SimpleFiveManWait);
                SetDrawnTile(gameState, "West", "5m");
                SetCurrentTurn(gameState, "West");

                bool discarded = DiscardDrawnTileForSeat(gameFlow, "West");

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Ron"));
                Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "WinSourceSeat").ToString(), Is.EqualTo("West"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RonDecision_OwnDiscardedWait_DoesNotStartRonDecisionAndAdvancesTurn()
        {
            GameObject gameObject = new GameObject("FuritenRonOwnDiscardBlocksTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = StartRound(gameFlow);
                AddHandTiles(GetPlayerSeat(gameState, "East"), SimpleFiveManWait);
                AddDiscard(gameState, "East", "5m", 0);
                SetDrawnTile(gameState, "West", "5m");
                SetCurrentTurn(gameState, "West");

                bool discarded = DiscardDrawnTileForSeat(gameFlow, "West");

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RonDecision_OtherSeatDiscardedWait_DoesNotCauseFuriten()
        {
            GameObject gameObject = new GameObject("FuritenRonOtherDiscardIgnoredTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = StartRound(gameFlow);
                AddHandTiles(GetPlayerSeat(gameState, "East"), SimpleFiveManWait);
                AddDiscard(gameState, "West", "5m", 0);
                SetDrawnTile(gameState, "West", "5m");
                SetCurrentTurn(gameState, "West");

                bool discarded = DiscardDrawnTileForSeat(gameFlow, "West");

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Ron"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RonDecision_MultiWaitWithOneOwnDiscardedWait_BlocksDifferentWaitRon()
        {
            GameObject gameObject = new GameObject("FuritenRonMultiWaitBlocksTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = StartRound(gameFlow);
                AddHandTiles(GetPlayerSeat(gameState, "East"), MultiWait);
                AddDiscard(gameState, "East", "3m", 0);
                AssertFuriten(gameFlow, "East", true);
                SetDrawnTile(gameState, "West", "6m");
                SetCurrentTurn(gameState, "West");

                bool discarded = DiscardDrawnTileForSeat(gameFlow, "West");

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("East"));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RonDecision_FirstCandidateFuriten_ContinuesToLaterCandidate()
        {
            GameObject gameObject = new GameObject("FuritenRonContinuesToLaterCandidateTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 3);
                object gameState = StartRound(gameFlow);
                SetSeatParticipantType(gameState, "South", "LocalHuman");
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                AddHandTiles(GetPlayerSeat(gameState, "South"), SimpleFiveManWait);
                AddHandTiles(GetPlayerSeat(gameState, "West"), SimpleFiveManWait);
                AddDiscard(gameState, "South", "5m", 0);
                SetDrawnTile(gameState, "East", "5m");
                SetCurrentTurn(gameState, "East");

                bool discarded = DiscardDrawnTileForSeat(gameFlow, "East");

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("West"));
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Ron"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RonDecision_AllCandidatesFuriten_AdvancesTurnOnce()
        {
            GameObject gameObject = new GameObject("FuritenRonAllCandidatesBlockedTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 3);
                object gameState = StartRound(gameFlow);
                SetSeatParticipantType(gameState, "South", "LocalHuman");
                SetSeatParticipantType(gameState, "West", "LocalHuman");
                AddHandTiles(GetPlayerSeat(gameState, "South"), SimpleFiveManWait);
                AddHandTiles(GetPlayerSeat(gameState, "West"), SimpleFiveManWait);
                AddDiscard(gameState, "South", "5m", 0);
                AddDiscard(gameState, "West", "5m", 0);
                SetDrawnTile(gameState, "East", "5m");
                SetCurrentTurn(gameState, "East");

                bool discarded = DiscardDrawnTileForSeat(gameFlow, "East");

                Assert.That(discarded, Is.True);
                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.False);
                Assert.That(GetProperty(gameState, "CurrentTurn").ToString(), Is.EqualTo("South"));
                Assert.That(GetProperty(gameState, "TurnIndex"), Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TsumoDecision_OwnDiscardFuriten_DoesNotBlockTsumo()
        {
            GameObject gameObject = new GameObject("FuritenDoesNotBlockTsumoTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 1);
                object gameState = StartRound(gameFlow);
                AddHandTiles(GetPlayerSeat(gameState, "East"), SimpleFiveManWait);
                AddDiscard(gameState, "East", "5m", 0);

                Invoke(gameFlow, "RequestForceDrawSkill", "5m");
                Invoke(gameFlow, "RequestDraw");

                Assert.That(GetProperty(gameState, "IsWinDecisionPending"), Is.True);
                Assert.That(GetProperty(gameState, "WinDecisionType").ToString(), Is.EqualTo("Tsumo"));
                Assert.That(GetProperty(gameState, "WinDecisionSeat").ToString(), Is.EqualTo("East"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EvaluateAllFuriten_ReturnsOccupiedSeatsWithoutChangingGameState()
        {
            GameObject gameObject = new GameObject("EvaluateAllFuritenGameFlowApiTest");
            try
            {
                object gameFlow = CreateConfiguredGameFlow(gameObject, 2);
                object gameState = StartRound(gameFlow);
                string before = SnapshotGameState(gameState);

                object resultSet = Invoke(gameFlow, "EvaluateAllFuriten");

                Assert.That(GetProperty(resultSet, "Count"), Is.EqualTo(2));
                Assert.That(TryGetSeatResult(resultSet, "East", out _), Is.True);
                Assert.That(TryGetSeatResult(resultSet, "West", out _), Is.True);
                Assert.That(TryGetSeatResult(resultSet, "South", out _), Is.False);
                Assert.That(SnapshotGameState(gameState), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object CreateConfiguredGameFlow(GameObject gameObject, int participantCount)
        {
            gameObject.AddComponent(Type.GetType(MahjongEventNotifierTypeName, true));
            object gameFlow = gameObject.AddComponent(Type.GetType(MahjongGameFlowTypeName, true));
            SetPrivateField(gameFlow, "logWarnings", false);
            SetPrivateField(gameFlow, "participantCount", participantCount);
            SetPrivateField(gameFlow, "initialHandTileCount", 0);
            SetPrivateField(gameFlow, "autoStart", false);
            SetPrivateField(gameFlow, "useFixedRandomSeed", true);
            SetPrivateField(gameFlow, "fixedRandomSeed", 12345);
            SetPrivateField(gameFlow, "enableAutoDraw", false);
            SetPrivateField(gameFlow, "autoDiscardDrawnTileDelaySeconds", 0f);
            SetPrivateField(gameFlow, "randomizeSelfSeat", false);
            SetPrivateField(gameFlow, "fixedSelfSeat", ParseSeat("East"));
            SetPrivateField(
                gameFlow,
                "yakuDefinitionCatalog",
                CreateYakuCatalog(
                    CreateYakuDefinition("MenzenTsumo", "One", "None"),
                    CreateYakuDefinition("Reach", "One", "None"),
                    CreateYakuDefinition("Tanyao", "One", "One")));
            return gameFlow;
        }

        private static object StartRound(object gameFlow)
        {
            Invoke(gameFlow, "StartNewRound");
            return GetProperty(gameFlow, "CurrentState");
        }

        private static void AssertFuriten(object gameFlow, string seatName, bool expected)
        {
            object resultSet = Invoke(gameFlow, "EvaluateAllFuriten");
            Assert.That(TryGetSeatResult(resultSet, seatName, out object result), Is.True);
            Assert.That(GetProperty(result, "IsDiscardFuriten"), Is.EqualTo(expected));
            Assert.That(GetProperty(result, "IsFuriten"), Is.EqualTo(expected));
        }

        private static bool TryGetSeatResult(
            object resultSet,
            string seatName,
            out object result)
        {
            MethodInfo method = resultSet.GetType().GetMethod("TryGet");
            Assert.That(method, Is.Not.Null);

            object[] args = { ParseSeat(seatName), null };
            bool found = (bool)method.Invoke(resultSet, args);
            result = args[1];
            return found;
        }

        private static void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                Invoke(hand, "Add", CreateTile(tileCodes[i]));
        }

        private static void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            Invoke(GetPlayerSeat(gameState, seatName), "SetDrawnTile", CreateTile(tileCode));
        }

        private static void AddDiscard(
            object gameState,
            string seatName,
            string tileCode,
            int turnIndex)
        {
            object record = Activator.CreateInstance(
                Type.GetType(DiscardRecordTypeName, true),
                ParseSeat(seatName),
                CreateTile(tileCode),
                turnIndex);
            Invoke(gameState, "AddDiscard", record);
        }

        private static void SetCurrentTurn(object gameState, string seatName)
        {
            SetProperty(gameState, "CurrentTurn", ParseSeat(seatName));
        }

        private static bool DiscardDrawnTileForSeat(object gameFlow, string seatName)
        {
            return (bool)Invoke(
                gameFlow,
                "TryRequestDiscardDrawnTileForSeat",
                ParseSeat(seatName));
        }

        private static object GetPlayerSeat(object gameState, string seatName)
        {
            return Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
        }

        private static void SetSeatParticipantType(
            object gameState,
            string seatName,
            string participantTypeName)
        {
            Invoke(
                gameState,
                "SetParticipantType",
                ParseSeat(seatName),
                ParseParticipantType(participantTypeName));
        }

        private static object CreateTile(string code)
        {
            Type tileType = Type.GetType(TileTypeName, true);
            ConstructorInfo constructor = tileType.GetConstructor(new[] { typeof(string) });
            Assert.That(constructor, Is.Not.Null);
            return constructor.Invoke(new object[] { code });
        }

        private static object CreateYakuCatalog(params object[] definitions)
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

        private static object CreateYakuDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName)
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
                Enum.Parse(yakuKindType, yakuKindName),
                yakuKindName,
                Enum.Parse(hanValueType, closedHanName),
                Enum.Parse(hanValueType, openHanName),
                false,
                true
            });
        }

        private static string SnapshotGameState(object gameState)
        {
            return string.Join(
                "|",
                GetProperty(gameState, "CurrentTurn"),
                GetProperty(gameState, "TurnIndex"),
                GetProperty(gameState, "IsRoundEnded"),
                GetProperty(gameState, "IsWinDecisionPending"),
                GetProperty(gameState, "IsReachDecisionPending"),
                GetProperty(gameState, "IsReachDiscardSelectionPending"),
                GetListCount(GetProperty(gameState, "Discards")),
                SnapshotSeatSlots(gameState));
        }

        private static string SnapshotSeatSlots(object gameState)
        {
            object seatSlots = GetProperty(gameState, "SeatSlots");
            int count = GetListCount(seatSlots);
            string snapshot = count.ToString();

            for (int i = 0; i < count; i++)
            {
                object slot = GetListItem(seatSlots, i);
                object playerId = GetProperty(slot, "PlayerId");
                object participantType = GetProperty(slot, "ParticipantType");
                snapshot += "|" +
                    GetProperty(slot, "Wind") + ":" +
                    (playerId == null ? "Empty" : playerId.ToString()) + ":" +
                    (participantType == null ? "None" : participantType.ToString());
            }

            return snapshot;
        }

        private static int GetListCount(object list)
        {
            return (int)GetProperty(list, "Count");
        }

        private static object GetListItem(object list, int index)
        {
            PropertyInfo itemProperty = list.GetType().GetProperty("Item");
            Assert.That(itemProperty, Is.Not.Null);
            return itemProperty.GetValue(list, new object[] { index });
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

        private static object ParseSeat(string seatName)
        {
            return Enum.Parse(Type.GetType(SeatIdTypeName, true), seatName);
        }

        private static object ParseParticipantType(string participantTypeName)
        {
            return Enum.Parse(Type.GetType(ParticipantTypeTypeName, true), participantTypeName);
        }
    }
}
