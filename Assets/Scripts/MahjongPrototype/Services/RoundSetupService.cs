using System;
using MahjongPrototype.Domain;

namespace MahjongPrototype.Services
{
    public sealed class RoundSetupService
    {
        private readonly RoundStartingSeatResolver roundStartingSeatResolver;
        private readonly PlayerTurnManager playerTurnManager;
        private readonly DrawService drawService;

        public RoundSetupService(
            RoundStartingSeatResolver roundStartingSeatResolver,
            PlayerTurnManager playerTurnManager,
            DrawService drawService)
        {
            this.roundStartingSeatResolver = roundStartingSeatResolver ??
                throw new ArgumentNullException(nameof(roundStartingSeatResolver));
            this.playerTurnManager = playerTurnManager ??
                throw new ArgumentNullException(nameof(playerTurnManager));
            this.drawService = drawService ?? throw new ArgumentNullException(nameof(drawService));
        }

        public RoundSetupResult SetupRound(
            WindProgress windProgress,
            int? randomSeed,
            SeatId selfSeat,
            int participantCount)
        {
            MahjongGameState gameState = new MahjongGameState(
                Wall.CreateStandardShuffled(randomSeed),
                windProgress);
            AssignParticipantsToSeats(gameState, selfSeat, participantCount);
            gameState.RebuildActiveTurnSeatsFromSeatSlots();

            SeatId startingSeat = roundStartingSeatResolver.Resolve(gameState.ActiveTurnSeats);
            playerTurnManager.InitializeRound(gameState, startingSeat);
            return new RoundSetupResult(gameState, startingSeat);
        }

        public InitialDealResult DealInitialHands(
            MahjongGameState gameState,
            int initialHandTileCount,
            Action<DrawResult> tileDrawn = null)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            // PROTOTYPE: Deal a fixed starting hand only to active turn seats.
            for (int seatIndex = 0; seatIndex < gameState.ActiveTurnSeats.Count; seatIndex++)
            {
                SeatId seat = gameState.ActiveTurnSeats[seatIndex];
                for (int i = 0; i < initialHandTileCount; i++)
                {
                    DrawResult result = drawService.DrawTile(seat, gameState, DrawPurpose.InitialDeal);
                    if (!result.Success)
                        return InitialDealResult.Failed(result);

                    gameState.GetPlayerSeat(seat).Hand.Add(result.Tile);
                    tileDrawn?.Invoke(result);
                }
            }

            return InitialDealResult.Completed;
        }

        private static void AssignParticipantsToSeats(
            MahjongGameState gameState,
            SeatId selfSeat,
            int participantCount)
        {
            gameState.SetSelfSeat(selfSeat);

            if (participantCount <= 1)
                return;

            if (participantCount == 2)
            {
                gameState.AssignPlayerToSeat(PlayerId.Player2, GetRelativeSeat(selfSeat, 2));
                return;
            }

            gameState.AssignPlayerToSeat(PlayerId.Player2, GetRelativeSeat(selfSeat, 1));
            gameState.AssignPlayerToSeat(PlayerId.Player3, GetRelativeSeat(selfSeat, 2));

            if (participantCount >= 4)
                gameState.AssignPlayerToSeat(PlayerId.Player4, GetRelativeSeat(selfSeat, 3));
        }

        private static SeatId GetRelativeSeat(SeatId originSeat, int offset)
        {
            return (SeatId)(((int)originSeat + offset) % 4);
        }
    }

    public sealed class RoundSetupResult
    {
        public RoundSetupResult(MahjongGameState gameState, SeatId startingSeat)
        {
            GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            StartingSeat = startingSeat;
        }

        public MahjongGameState GameState { get; }
        public SeatId StartingSeat { get; }
    }

    public readonly struct InitialDealResult
    {
        private InitialDealResult(bool success, DrawResult failedDraw)
        {
            Success = success;
            FailedDraw = failedDraw;
        }

        public static InitialDealResult Completed => new InitialDealResult(true, default);

        public bool Success { get; }
        public DrawResult FailedDraw { get; }

        public static InitialDealResult Failed(DrawResult failedDraw)
        {
            return new InitialDealResult(false, failedDraw);
        }
    }
}
