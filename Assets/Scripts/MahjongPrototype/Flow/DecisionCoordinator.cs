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
        private readonly Queue<long> queuedRequestIds = new Queue<long>();
        private readonly Dictionary<long, DecisionResponse> queuedResponsesByRequestId =
            new Dictionary<long, DecisionResponse>();
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
        public int QueuedResponseCount => queuedResponsesByRequestId.Count;

        public bool IsPending(long requestId)
        {
            return pendingById.ContainsKey(requestId);
        }

        public bool IsResponseQueued(long requestId)
        {
            return queuedResponsesByRequestId.ContainsKey(requestId);
        }

        public DecisionResponseResult Request(DecisionRequest request)
        {
            if (request == null)
                return DecisionResponseResult.Rejected("DecisionRequestMissing");
            if (pendingById.ContainsKey(request.RequestId) ||
                queuedResponsesByRequestId.ContainsKey(request.RequestId))
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
            queuedResponsesByRequestId.Add(response.RequestId, response);
            queuedRequestIds.Enqueue(response.RequestId);
            return DecisionResponseResult.Succeeded();
        }

        public void Pump()
        {
            if (isPumping)
                return;

            isPumping = true;
            try
            {
                while (queuedRequestIds.Count > 0)
                {
                    long requestId = queuedRequestIds.Dequeue();
                    if (!queuedResponsesByRequestId.TryGetValue(
                            requestId,
                            out DecisionResponse response))
                    {
                        continue;
                    }

                    queuedResponsesByRequestId.Remove(requestId);
                    DecisionResponseResult result =
                        authority.TryExecuteDecisionResponse(response);
                    if (!result.Accepted && response.Kind == DecisionKind.Reaction &&
                        authority is IReactionDecisionResponseRejectionHandler handler)
                    {
                        handler.HandleRejectedReactionDecisionResponse(response, result);
                    }
                }
            }
            finally
            {
                isPumping = false;
            }
        }

        public bool Cancel(long requestId)
        {
            bool cancelled = queuedResponsesByRequestId.Remove(requestId);
            if (cancelled)
                RemoveQueuedRequestId(requestId);
            if (!pendingById.TryGetValue(requestId, out PendingDecision pending))
                return cancelled;

            pendingById.Remove(requestId);
            pending.Provider.CancelDecision(requestId);
            return true;
        }

        public void CancelAll()
        {
            List<PendingDecision> pending = new List<PendingDecision>(pendingById.Values);
            pendingById.Clear();
            queuedRequestIds.Clear();
            queuedResponsesByRequestId.Clear();
            for (int i = 0; i < pending.Count; i++)
                pending[i].Provider.CancelDecision(pending[i].Request.RequestId);
        }

        private void RemoveQueuedRequestId(long requestId)
        {
            int queuedCount = queuedRequestIds.Count;
            for (int i = 0; i < queuedCount; i++)
            {
                long queuedRequestId = queuedRequestIds.Dequeue();
                if (queuedRequestId != requestId)
                    queuedRequestIds.Enqueue(queuedRequestId);
            }
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
