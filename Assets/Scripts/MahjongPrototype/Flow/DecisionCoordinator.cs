using System;
using System.Collections.Generic;
using MahjongPrototype.Domain;

namespace MahjongPrototype
{
    /// <summary>
    /// Routes authority-created decision requests to the configured provider.
    /// It only validates identity/lifecycle and serializes responses; legality,
    /// state changes, priorities, commit, and notification remain in the
    /// authority.
    /// </summary>
    public sealed class DecisionCoordinator
    {
        private readonly DecisionProviderRegistry providerRegistry;
        private readonly IMahjongAuthorityDecisionPort authority;
        private readonly Dictionary<long, PendingDecision> pendingById =
            new Dictionary<long, PendingDecision>();
        private readonly Queue<DecisionResponse> queuedResponses =
            new Queue<DecisionResponse>();
        private bool isPumping;

        public DecisionCoordinator(
            DecisionProviderRegistry providerRegistry,
            IMahjongAuthorityDecisionPort authority)
        {
            this.providerRegistry = providerRegistry ??
                throw new ArgumentNullException(nameof(providerRegistry));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
        }

        public int PendingCount => pendingById.Count;
        public int QueuedResponseCount => queuedResponses.Count;

        public DecisionResponseResult Request(DecisionRequest request)
        {
            if (request == null)
                return DecisionResponseResult.Rejected("DecisionRequestMissing");
            if (pendingById.ContainsKey(request.RequestId))
                return DecisionResponseResult.Rejected("DuplicateDecisionRequestId");
            if (!providerRegistry.TryResolve(request.PlayerId, out DecisionProviderRegistration registration))
                return DecisionResponseResult.Rejected("DecisionProviderMissing");
            if (!registration.IsAvailable || registration.Provider == null)
                return DecisionResponseResult.Rejected("DecisionProviderUnavailable");

            IDecisionProvider provider = registration.Provider;
            if (provider.Route != registration.Route || !provider.IsAvailable)
                return DecisionResponseResult.Rejected("DecisionProviderUnavailable");

            pendingById.Add(request.RequestId, new PendingDecision(request, provider));
            provider.RequestDecision(request, ReceiveResponse);
            return DecisionResponseResult.Succeeded();
        }

        public DecisionResponseResult ReceiveResponse(DecisionResponse response)
        {
            if (response == null)
                return DecisionResponseResult.Rejected("DecisionResponseMissing");
            if (!pendingById.TryGetValue(response.RequestId, out PendingDecision pending))
                return DecisionResponseResult.Rejected("DecisionRequestMissingOrCancelled");
            if (!Matches(pending.Request, response))
                return DecisionResponseResult.Rejected("DecisionResponseIdentityMismatch");
            // Validate the immutable provider-facing payload before removing
            // the pending callback. This lets a Local UI correct an invalid
            // reaction choice and submit again without changing authority
            // state.
            if (!pending.Request.TryValidateResponsePayload(response, out string reason))
                return DecisionResponseResult.Rejected(reason);

            pendingById.Remove(response.RequestId);
            queuedResponses.Enqueue(response);
            return DecisionResponseResult.Succeeded();
        }

        public void Pump()
        {
            if (isPumping)
                return;

            isPumping = true;
            try
            {
                while (queuedResponses.Count > 0)
                    authority.TryExecuteDecisionResponse(queuedResponses.Dequeue());
            }
            finally
            {
                isPumping = false;
            }
        }

        public bool Cancel(long requestId)
        {
            if (!pendingById.TryGetValue(requestId, out PendingDecision pending))
                return false;

            pendingById.Remove(requestId);
            pending.Provider.CancelDecision(requestId);
            return true;
        }

        public void CancelAll()
        {
            List<PendingDecision> pending = new List<PendingDecision>(pendingById.Values);
            pendingById.Clear();
            queuedResponses.Clear();
            for (int i = 0; i < pending.Count; i++)
                pending[i].Provider.CancelDecision(pending[i].Request.RequestId);
        }

        private static bool Matches(DecisionRequest request, DecisionResponse response)
        {
            return request.RequestId == response.RequestId &&
                request.Kind == response.Kind &&
                request.PlayerId == response.PlayerId &&
                request.ActorSeat == response.ActorSeat &&
                request.TurnIndex == response.TurnIndex;
        }

        private readonly struct PendingDecision
        {
            public PendingDecision(DecisionRequest request, IDecisionProvider provider)
            {
                Request = request;
                Provider = provider;
            }

            public DecisionRequest Request { get; }
            public IDecisionProvider Provider { get; }
        }
    }
}
