using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    public sealed class MatchStartValidationResult
    {
        private MatchStartValidationResult(bool isValid, string failureReason)
        {
            IsValid = isValid;
            FailureReason = failureReason;
        }

        public bool IsValid { get; }
        public string FailureReason { get; }

        public static MatchStartValidationResult Valid()
        {
            return new MatchStartValidationResult(true, null);
        }

        public static MatchStartValidationResult Invalid(string failureReason)
        {
            if (string.IsNullOrWhiteSpace(failureReason))
                throw new ArgumentException("A validation failure reason is required.", nameof(failureReason));

            return new MatchStartValidationResult(false, failureReason);
        }
    }

    /// <summary>
    /// Verifies that every shared participant can be driven by one available,
    /// compatible runtime answer route before any round state is created.
    /// </summary>
    public sealed class MatchStartValidator
    {
        public MatchStartValidationResult Validate(
            MatchRoster roster,
            DecisionProviderRegistry providerRegistry)
        {
            if (roster == null)
                return MatchStartValidationResult.Invalid("Match roster is not configured.");
            if (providerRegistry == null)
                return MatchStartValidationResult.Invalid("Decision provider registry is not configured.");
            if (roster.Participants.Count == 0)
                return MatchStartValidationResult.Invalid("Match roster does not contain any players.");
            if (roster.Participants.Count > 4)
                return MatchStartValidationResult.Invalid("Match roster contains more than four players.");

            HashSet<PlayerId> playerIds = new HashSet<PlayerId>();
            for (int i = 0; i < roster.Participants.Count; i++)
            {
                MatchParticipant participant = roster.Participants[i];
                if (!playerIds.Add(participant.PlayerId))
                {
                    return MatchStartValidationResult.Invalid(
                        $"Match roster contains duplicate PlayerId {participant.PlayerId}.");
                }
            }

            for (int i = 0; i < roster.Participants.Count; i++)
            {
                PlayerId expectedPlayerId = (PlayerId)(i + 1);
                if (!playerIds.Contains(expectedPlayerId))
                {
                    return MatchStartValidationResult.Invalid(
                        $"Match roster must contain {expectedPlayerId} for the current round setup.");
                }
            }

            for (int i = 0; i < roster.Participants.Count; i++)
            {
                MatchParticipant participant = roster.Participants[i];
                IReadOnlyList<DecisionProviderRegistration> registrations =
                    providerRegistry.GetRegistrations(participant.PlayerId);
                if (registrations.Count == 0)
                {
                    return MatchStartValidationResult.Invalid(
                        $"Player {participant.PlayerId} does not have a decision provider.");
                }
                if (registrations.Count != 1)
                {
                    return MatchStartValidationResult.Invalid(
                        $"Player {participant.PlayerId} has multiple decision providers.");
                }

                DecisionProviderRegistration registration = registrations[0];
                if (!registration.IsAvailable)
                {
                    return MatchStartValidationResult.Invalid(
                        $"Decision provider {registration.Route} for player {participant.PlayerId} is unavailable.");
                }
                if (registration.Route == DecisionProviderRoute.Network)
                {
                    return MatchStartValidationResult.Invalid(
                        $"Decision provider Network for player {participant.PlayerId} is not implemented.");
                }
                if (!IsCompatible(participant.Kind, registration.Route))
                {
                    return MatchStartValidationResult.Invalid(
                        $"Participant {participant.PlayerId} ({participant.Kind}) is incompatible with decision provider {registration.Route}.");
                }
            }

            return MatchStartValidationResult.Valid();
        }

        private static bool IsCompatible(ParticipantKind participantKind, DecisionProviderRoute route)
        {
            switch (participantKind)
            {
                case ParticipantKind.Human:
                    return route == DecisionProviderRoute.LocalUi ||
                        route == DecisionProviderRoute.Network;
                case ParticipantKind.Cpu:
                    return route == DecisionProviderRoute.CpuAgent;
                default:
                    return false;
            }
        }
    }
}
