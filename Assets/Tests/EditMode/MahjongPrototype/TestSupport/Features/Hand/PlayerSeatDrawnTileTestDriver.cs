using MahjongPrototype.Tests.TestSupport.Core;
using MahjongPrototype.Tests.TestSupport.Mahjong;

namespace MahjongPrototype.Tests.TestSupport.Features.Hand
{
    internal sealed class PlayerSeatDrawnTileTestDriver
    {
        private readonly ReflectionTestAccess reflection;
        private readonly MahjongTestDataFactory dataFactory;
        private readonly object playerSeat;

        private PlayerSeatDrawnTileTestDriver(
            ReflectionTestAccess reflection,
            MahjongTestDataFactory dataFactory,
            object playerSeat)
        {
            this.reflection = reflection;
            this.dataFactory = dataFactory;
            this.playerSeat = playerSeat;
        }

        public static PlayerSeatDrawnTileTestDriver Create(string seatName = "East")
        {
            ReflectionTestAccess reflection = new ReflectionTestAccess();
            MahjongTestTypes types = new MahjongTestTypes(reflection);
            MahjongTestDataFactory dataFactory = new MahjongTestDataFactory(reflection, types);
            return new PlayerSeatDrawnTileTestDriver(
                reflection,
                dataFactory,
                dataFactory.CreatePlayerSeat(seatName));
        }

        public bool HasDrawnTile => (bool)reflection.GetProperty(playerSeat, "HasDrawnTile");

        public string DrawnTileCode => reflection.GetProperty(playerSeat, "DrawnTile").ToString();

        public string HandDisplayString => (string)reflection.Invoke(Hand, "ToDisplayString");

        private object Hand => dataFactory.GetHand(playerSeat);

        public void SetDrawnTile(string tileCode)
        {
            reflection.Invoke(playerSeat, "SetDrawnTile", dataFactory.CreateTile(tileCode));
        }

        public bool TryTakeDrawnTile(out string tileCode)
        {
            object[] args = { null };
            bool result = (bool)reflection.Invoke(playerSeat, "TryTakeDrawnTile", args);
            tileCode = args[0] == null ? null : args[0].ToString();
            return result;
        }

        public bool CommitDrawnTileToHand()
        {
            return (bool)reflection.Invoke(playerSeat, "CommitDrawnTileToHand");
        }

        public void ClearDrawnTile()
        {
            reflection.Invoke(playerSeat, "ClearDrawnTile");
        }

        public void AddHandTile(string tileCode)
        {
            reflection.Invoke(Hand, "Add", dataFactory.CreateTile(tileCode));
        }

        public void SortHand()
        {
            reflection.Invoke(Hand, "SortByTypeIndex");
        }
    }
}

