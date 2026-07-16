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

        /// <summary>
        /// Creates a round from match-lifetime player configuration. Seat slots
        /// receive only the legacy compatibility projection needed by the
        /// existing turn and rule code.
        /// </summary>
        public RoundSetupResult SetupRound(
            WindProgress windProgress,
            int? randomSeed,
            SeatId selfSeat,
            MatchRoster roster,
            DecisionProviderRegistry providerRegistry)
        {
            if (roster == null)
                throw new ArgumentNullException(nameof(roster));
            if (providerRegistry == null)
                throw new ArgumentNullException(nameof(providerRegistry));

            MahjongGameState gameState = new MahjongGameState(
                Wall.CreateStandardShuffled(randomSeed),
                windProgress);
            AssignParticipantsToSeats(gameState, selfSeat, roster, providerRegistry);
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

        private static void AssignParticipantsToSeats(
            MahjongGameState gameState,
            SeatId selfSeat,
            MatchRoster roster,
            DecisionProviderRegistry providerRegistry)
        {
            int participantCount = roster.Participants.Count;
            AssignConfiguredParticipant(
                gameState,
                selfSeat,
                PlayerId.Player1,
                roster,
                providerRegistry);

            if (participantCount <= 1)
                return;

            if (participantCount == 2)
            {
                AssignConfiguredParticipant(
                    gameState,
                    GetRelativeSeat(selfSeat, 2),
                    PlayerId.Player2,
                    roster,
                    providerRegistry);
                return;
            }

            AssignConfiguredParticipant(
                gameState,
                GetRelativeSeat(selfSeat, 1),
                PlayerId.Player2,
                roster,
                providerRegistry);
            AssignConfiguredParticipant(
                gameState,
                GetRelativeSeat(selfSeat, 2),
                PlayerId.Player3,
                roster,
                providerRegistry);

            if (participantCount >= 4)
            {
                AssignConfiguredParticipant(
                    gameState,
                    GetRelativeSeat(selfSeat, 3),
                    PlayerId.Player4,
                    roster,
                    providerRegistry);
            }
        }

        private static void AssignConfiguredParticipant(
            MahjongGameState gameState,
            SeatId seat,
            PlayerId playerId,
            MatchRoster roster,
            DecisionProviderRegistry providerRegistry)
        {
            if (!roster.TryGetParticipant(playerId, out MatchParticipant participant))
            {
                throw new InvalidOperationException(
                    $"Match roster does not contain exactly one player {playerId}.");
            }
            if (!providerRegistry.TryResolve(playerId, out DecisionProviderRegistration registration))
            {
                throw new InvalidOperationException(
                    $"Decision provider registry does not contain exactly one provider for {playerId}.");
            }

            ParticipantType compatibilityParticipantType =
                ParticipantTypeCompatibilityProjection.Create(
                    participant.Kind,
                    registration.Route);
            gameState.AssignPlayerToSeat(playerId, seat, compatibilityParticipantType);
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
