using System;

namespace MahjongPrototype.Domain
{
    /// <summary>
    /// One-way bridge from the legacy per-seat type to the shared participant
    /// attribute and runtime answer route. New code should use the resulting
    /// values and must not use ParticipantType as an input.
    /// </summary>
    public static class ParticipantTypeAdapter
    {
        public static MatchParticipant ToMatchParticipant(
            PlayerId playerId,
            ParticipantType participantType)
        {
            return new MatchParticipant(playerId, ToParticipantKind(participantType));
        }

        public static DecisionProviderRegistration ToDecisionProviderRegistration(
            PlayerId playerId,
            ParticipantType participantType)
        {
            DecisionProviderRoute route = ToDecisionProviderRoute(participantType);
            return new DecisionProviderRegistration(
                playerId,
                route,
                route != DecisionProviderRoute.Network);
        }

        public static ParticipantKind ToParticipantKind(ParticipantType participantType)
        {
            switch (participantType)
            {
                case ParticipantType.LocalHuman:
                case ParticipantType.RemoteHuman:
                    return ParticipantKind.Human;
                case ParticipantType.Cpu:
                    return ParticipantKind.Cpu;
                default:
                    throw new ArgumentOutOfRangeException(nameof(participantType));
            }
        }

        public static DecisionProviderRoute ToDecisionProviderRoute(ParticipantType participantType)
        {
            switch (participantType)
            {
                case ParticipantType.LocalHuman:
                    return DecisionProviderRoute.LocalUi;
                case ParticipantType.Cpu:
                    return DecisionProviderRoute.CpuAgent;
                case ParticipantType.RemoteHuman:
                    return DecisionProviderRoute.Network;
                default:
                    throw new ArgumentOutOfRangeException(nameof(participantType));
            }
        }

    }

    /// <summary>
    /// Temporary output projection for legacy SeatSlot.ParticipantType readers.
    /// It does not feed ParticipantType back into the new match configuration.
    /// </summary>
    public static class ParticipantTypeCompatibilityProjection
    {
        public static ParticipantType Create(
            ParticipantKind participantKind,
            DecisionProviderRoute route)
        {
            switch (participantKind)
            {
                case ParticipantKind.Human:
                    if (route == DecisionProviderRoute.LocalUi)
                        return ParticipantType.LocalHuman;
                    if (route == DecisionProviderRoute.Network)
                        return ParticipantType.RemoteHuman;
                    break;
                case ParticipantKind.Cpu:
                    if (route == DecisionProviderRoute.CpuAgent)
                        return ParticipantType.Cpu;
                    break;
            }

            throw new ArgumentException(
                $"Participant kind {participantKind} and decision provider route {route} are incompatible.");
        }
    }
}
