using System;
using System.Collections;
using System.Collections.Generic;
using MahjongPrototype.Tests.TestSupport.Core;
using UnityEngine;

namespace MahjongPrototype.Tests.TestSupport.Mahjong
{
    internal sealed class MahjongTestDataFactory
    {
        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestTypes types;

        public MahjongTestDataFactory(ReflectionTestAccess reflection, MahjongTestTypes types)
        {
            this.reflection = reflection;
            this.types = types;
        }

        public object ParseSeat(string seatName)
        {
            return Enum.Parse(types.SeatId, seatName);
        }

        public object ParseParticipantType(string participantTypeName)
        {
            return Enum.Parse(types.ParticipantType, participantTypeName);
        }

        public object ParsePlayerId(string playerId)
        {
            return Enum.Parse(types.PlayerId, playerId);
        }

        public object CreateTile(string tileCode)
        {
            return reflection.CreateInstance(types.Tile, tileCode);
        }

        public object CreateInvalidTile()
        {
            return reflection.CreateInstance(types.Tile);
        }

        public Array CreateTileArray(params string[] tileCodes)
        {
            Array tiles = Array.CreateInstance(types.Tile, tileCodes.Length);
            for (int i = 0; i < tileCodes.Length; i++)
                tiles.SetValue(CreateTile(tileCodes[i]), i);

            return tiles;
        }

        public object CreateWall(int fixedSeed)
        {
            return reflection.InvokeStatic(types.Wall, "CreateStandardShuffled", fixedSeed);
        }

        public object CreateGameState(params string[] seatNames)
        {
            object gameState = reflection.CreateInstance(types.MahjongGameState, CreateWall(12345));
            AssignPlayersToSeats(gameState, seatNames);
            return gameState;
        }

        public void AssignPlayersToSeats(object gameState, string[] seatNames)
        {
            for (int i = 0; i < seatNames.Length; i++)
            {
                reflection.Invoke(
                    gameState,
                    "AssignPlayerToSeat",
                    ParsePlayerId($"Player{i + 1}"),
                    ParseSeat(seatNames[i]));
            }

            reflection.Invoke(gameState, "RebuildActiveTurnSeatsFromSeatSlots");
        }

        public object GetPlayerSeat(object gameState, string seatName)
        {
            return reflection.Invoke(gameState, "GetPlayerSeat", ParseSeat(seatName));
        }

        public void SetParticipantType(object gameState, string seatName, string participantTypeName)
        {
            reflection.Invoke(
                gameState,
                "SetParticipantType",
                ParseSeat(seatName),
                ParseParticipantType(participantTypeName));
        }

        public void SetCurrentTurn(object gameState, string seatName)
        {
            reflection.SetProperty(gameState, "CurrentTurn", ParseSeat(seatName));
        }

        public void AddHandTiles(object playerSeat, params string[] tileCodes)
        {
            object hand = reflection.GetProperty(playerSeat, "Hand");
            for (int i = 0; i < tileCodes.Length; i++)
                reflection.Invoke(hand, "Add", CreateTile(tileCodes[i]));
        }

        public void AddHandTile(object playerSeat, object tile)
        {
            reflection.Invoke(reflection.GetProperty(playerSeat, "Hand"), "Add", tile);
        }

        public void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            reflection.Invoke(GetPlayerSeat(gameState, seatName), "SetDrawnTile", CreateTile(tileCode));
        }

        public object CreateDiscardRecord(string seatName, string tileCode, int turnIndex)
        {
            return reflection.CreateInstance(
                types.DiscardRecord,
                ParseSeat(seatName),
                CreateTile(tileCode),
                turnIndex);
        }

        public object AddDiscard(object gameState, string seatName, string tileCode, int turnIndex)
        {
            object record = CreateDiscardRecord(seatName, tileCode, turnIndex);
            reflection.Invoke(gameState, "AddDiscard", record);
            return record;
        }

        public object CreateYakuCatalog(params object[] definitions)
        {
            object catalog = ScriptableObject.CreateInstance(types.YakuDefinitionCatalog);
            Type listType = typeof(List<>).MakeGenericType(types.YakuDefinition);
            IList list = (IList)reflection.CreateInstance(listType);

            for (int i = 0; i < definitions.Length; i++)
                list.Add(definitions[i]);

            reflection.SetPrivateField(catalog, "definitions", list);
            return catalog;
        }

        public object CreateYakuDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName)
        {
            return reflection.CreateInstance(
                types.YakuDefinition,
                Enum.Parse(types.YakuKind, yakuKindName),
                yakuKindName,
                Enum.Parse(types.HanValue, closedHanName),
                Enum.Parse(types.HanValue, openHanName),
                false,
                true);
        }

        public string HandDisplayString(object gameState, string seatName)
        {
            object hand = reflection.GetProperty(GetPlayerSeat(gameState, seatName), "Hand");
            return (string)reflection.Invoke(hand, "ToDisplayString");
        }
    }
}
