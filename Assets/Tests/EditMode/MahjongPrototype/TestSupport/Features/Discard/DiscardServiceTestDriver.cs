using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Discard
{
    internal sealed class DiscardServiceTestDriver
    {
        private const string DiscardServiceTypeName =
            "MahjongPrototype.Services.DiscardService, Assembly-CSharp";

        private readonly ReflectionTestAccess reflection;
        private readonly CollectionTestAccess collections;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object discardService;

        private DiscardServiceTestDriver(
            ReflectionTestAccess reflection,
            CollectionTestAccess collections,
            MahjongTestDataFactory dataFactory,
            object discardService)
        {
            this.reflection = reflection;
            this.collections = collections;
            this.dataFactory = dataFactory;
            this.discardService = discardService;
        }

        public static DiscardServiceTestDriver Create()
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            CollectionTestAccess collections = new CollectionTestAccess(reflection);
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            object discardService = reflection.CreateInstance(reflection.RequireType(DiscardServiceTypeName));
            return new DiscardServiceTestDriver(reflection, collections, dataFactory, discardService);
        }

        public object CreateGameState(params string[] seatNames)
        {
            return dataFactory.CreateGameState(seatNames);
        }

        public void AddHandTile(object gameState, string seatName, string tileCode)
        {
            dataFactory.AddHandTiles(dataFactory.GetPlayerSeat(gameState, seatName), tileCode);
        }

        public void SetDrawnTile(object gameState, string seatName, string tileCode)
        {
            dataFactory.SetDrawnTile(gameState, seatName, tileCode);
        }

        public void RecordTurnDraw(
            object gameState,
            string seatName,
            string tileCode,
            int turnIndex,
            bool isLastLiveWallDraw)
        {
            reflection.Invoke(
                gameState,
                "RecordTurnDraw",
                dataFactory.ParseSeat(seatName),
                dataFactory.CreateTile(tileCode),
                turnIndex,
                isLastLiveWallDraw);
        }

        public object DiscardHandTile(object gameState, string seatName, int handIndex)
        {
            return reflection.Invoke(
                discardService,
                "DiscardTile",
                gameState,
                dataFactory.ParseSeat(seatName),
                handIndex);
        }

        public object DiscardDrawnTile(object gameState, string seatName)
        {
            return reflection.Invoke(
                discardService,
                "DiscardDrawnTile",
                gameState,
                dataFactory.ParseSeat(seatName));
        }

        public object RecordOf(object discardResult)
        {
            return reflection.GetProperty(discardResult, "Record");
        }

        public int DiscardCount(object gameState)
        {
            return collections.Count(reflection.GetProperty(gameState, "Discards"));
        }

        public object DiscardAt(object gameState, int index)
        {
            return collections.Item(reflection.GetProperty(gameState, "Discards"), index);
        }

        public string RecordSource(object record)
        {
            return reflection.GetProperty(record, "Source").ToString();
        }

        public string RecordActorSeat(object record)
        {
            return reflection.GetProperty(record, "ActorSeat").ToString();
        }

        public string RecordTile(object record)
        {
            return reflection.GetProperty(record, "Tile").ToString();
        }

        public int RecordTurnIndex(object record)
        {
            return (int)reflection.GetProperty(record, "TurnIndex");
        }

        public bool RecordIsLastLiveWallDiscard(object record)
        {
            return (bool)reflection.GetProperty(record, "IsLastLiveWallDiscard");
        }
    }
}
