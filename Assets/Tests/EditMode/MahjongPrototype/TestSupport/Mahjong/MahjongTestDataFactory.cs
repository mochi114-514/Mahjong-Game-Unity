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

        public object ParseRoundWind(string roundWindName)
        {
            return Enum.Parse(types.RoundWind, roundWindName);
        }

        public object ParseWinType(string winTypeName)
        {
            return Enum.Parse(types.WinType, winTypeName);
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

        public object CreateHand(params string[] tileCodes)
        {
            object hand = reflection.CreateInstance(types.Hand);
            for (int i = 0; i < tileCodes.Length; i++)
                reflection.Invoke(hand, "Add", CreateTile(tileCodes[i]));

            return hand;
        }

        public Array CreateTileArrayFromText(string tileText)
        {
            return CreateTileArray(MahjongTileTextParser.ParseTileCodes(tileText));
        }

        public object CreateHandFromText(string tileText)
        {
            return CreateHand(MahjongTileTextParser.ParseTileCodes(tileText));
        }

        public void AddHandTilesFromText(object playerSeat, string tileText)
        {
            AddHandTiles(playerSeat, MahjongTileTextParser.ParseTileCodes(tileText));
        }

        public object CreatePlayerSeat(string seatName)
        {
            return reflection.CreateInstance(types.PlayerSeat, ParseSeat(seatName));
        }

        public object GetHand(object playerSeat)
        {
            return reflection.GetProperty(playerSeat, "Hand");
        }

        public object CreateWall(int fixedSeed)
        {
            return reflection.InvokeStatic(types.Wall, "CreateStandardShuffled", fixedSeed);
        }

        public object CreateWindProgress(string roundWindName, int handNumber)
        {
            return reflection.CreateInstance(
                types.WindProgress,
                ParseRoundWind(roundWindName),
                handNumber);
        }

        public object CreateGameState(params string[] seatNames)
        {
            object gameState = reflection.CreateInstance(types.MahjongGameState, CreateWall(12345));
            AssignPlayersToSeats(gameState, seatNames);
            return gameState;
        }

        public object CreateGameStateWithWindProgress(object windProgress)
        {
            return reflection.CreateInstance(
                types.MahjongGameState,
                CreateWall(12345),
                windProgress);
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
            SynchronizeNormalTurnPhase(gameState);
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
            if (reflection.GetProperty(gameState, "CurrentTurn").Equals(ParseSeat(seatName)))
                SynchronizeNormalTurnPhase(gameState);
        }

        public void ClearDrawnTile(object gameState, string seatName)
        {
            reflection.Invoke(GetPlayerSeat(gameState, seatName), "ClearDrawnTile");
            if (reflection.GetProperty(gameState, "CurrentTurn").Equals(ParseSeat(seatName)))
                SynchronizeNormalTurnPhase(gameState);
        }

        private void SynchronizeNormalTurnPhase(object gameState)
        {
            string phaseName = reflection.GetProperty(gameState, "TurnPhase").ToString();
            if (phaseName != "WaitingForDraw" && phaseName != "WaitingForDiscard")
                return;

            object currentTurn = reflection.GetProperty(gameState, "CurrentTurn");
            object currentPlayerSeat = reflection.Invoke(gameState, "GetPlayerSeat", currentTurn);
            bool hasDrawnTile = (bool)reflection.GetProperty(currentPlayerSeat, "HasDrawnTile");
            reflection.Invoke(
                gameState,
                hasDrawnTile ? "EnterWaitingForDiscard" : "EnterWaitingForDraw");
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
            return reflection.Invoke(gameState, "AddDiscard", record);
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
            string openHanName,
            int yakumanMultiplier = 0,
            bool isEnabled = true)
        {
            return CreateYakuDefinitionCore(
                yakuKindName,
                yakuKindName,
                closedHanName,
                openHanName,
                yakumanMultiplier,
                isEnabled);
        }

        public object CreateYakuDefinition(
            string yakuKindName,
            string closedHanName,
            string openHanName,
            bool isYakuman,
            bool isEnabled = true)
        {
            return CreateYakuDefinition(
                yakuKindName,
                closedHanName,
                openHanName,
                isYakuman ? 1 : 0,
                isEnabled);
        }

        public object CreateYakuDefinitionWithDisplayName(
            string yakuKindName,
            string displayName,
            string closedHanName,
            string openHanName,
            int yakumanMultiplier = 0,
            bool isEnabled = true)
        {
            return CreateYakuDefinitionCore(
                yakuKindName,
                displayName,
                closedHanName,
                openHanName,
                yakumanMultiplier,
                isEnabled);
        }

        public object CreateYakuDefinitionWithDisplayName(
            string yakuKindName,
            string displayName,
            string closedHanName,
            string openHanName,
            bool isYakuman,
            bool isEnabled = true)
        {
            return CreateYakuDefinitionWithDisplayName(
                yakuKindName,
                displayName,
                closedHanName,
                openHanName,
                isYakuman ? 1 : 0,
                isEnabled);
        }

        private object CreateYakuDefinitionCore(
            string yakuKindName,
            string displayName,
            string closedHanName,
            string openHanName,
            int yakumanMultiplier,
            bool isEnabled)
        {
            return reflection.CreateInstance(
                types.YakuDefinition,
                Enum.Parse(types.YakuKind, yakuKindName),
                displayName,
                Enum.Parse(types.HanValue, closedHanName),
                Enum.Parse(types.HanValue, openHanName),
                yakumanMultiplier,
                isEnabled);
        }

        public string HandDisplayString(object gameState, string seatName)
        {
            object hand = reflection.GetProperty(GetPlayerSeat(gameState, seatName), "Hand");
            return (string)reflection.Invoke(hand, "ToDisplayString");
        }
    }
}
