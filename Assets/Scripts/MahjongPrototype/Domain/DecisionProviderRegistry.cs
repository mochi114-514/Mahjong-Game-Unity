using System;
using System.Collections.Generic;

namespace MahjongPrototype.Domain
{
    /// <summary>
    /// Runtime answer route. This is configuration only in the first phase; it
    /// does not yet dispatch decisions.
    /// </summary>
    public enum DecisionProviderRoute
    {
        LocalUi = 0,
        CpuAgent = 1,
        Network = 2
    }

    public readonly struct DecisionProviderRegistration
    {
        public DecisionProviderRegistration(
            PlayerId playerId,
            DecisionProviderRoute route,
            bool isAvailable)
        {
            PlayerId = playerId;
            Route = route;
            IsAvailable = isAvailable;
        }

        public PlayerId PlayerId { get; }
        public DecisionProviderRoute Route { get; }
        public bool IsAvailable { get; }
    }

    /// <summary>
    /// Match-lifetime registry of answer routes, keyed by PlayerId rather than
    /// the round-local SeatId. Duplicate registrations are retained so the
    /// match-start validator can report them explicitly.
    /// </summary>
    public sealed class DecisionProviderRegistry
    {
        private readonly List<DecisionProviderRegistration> registrations;

        public DecisionProviderRegistry(IEnumerable<DecisionProviderRegistration> registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            this.registrations = new List<DecisionProviderRegistration>(registrations);
        }

        public IReadOnlyList<DecisionProviderRegistration> Registrations => registrations;

        public IReadOnlyList<DecisionProviderRegistration> GetRegistrations(PlayerId playerId)
        {
            List<DecisionProviderRegistration> matching =
                new List<DecisionProviderRegistration>();
            for (int i = 0; i < registrations.Count; i++)
            {
                DecisionProviderRegistration registration = registrations[i];
                if (registration.PlayerId == playerId)
                    matching.Add(registration);
            }

            return matching;
        }

        public bool TryResolve(PlayerId playerId, out DecisionProviderRegistration registration)
        {
            registration = default;
            bool found = false;
            for (int i = 0; i < registrations.Count; i++)
            {
                DecisionProviderRegistration current = registrations[i];
                if (current.PlayerId != playerId)
                    continue;

                if (found)
                    return false;

                registration = current;
                found = true;
            }

            return found;
        }
    }
}
