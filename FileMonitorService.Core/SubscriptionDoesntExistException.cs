namespace FileMonitorService.Core
{
    using System;

    public class SubscriptionDoesntExistException : Exception
    {
        public long SubscriptionId { get; private set; }

        public SubscriptionDoesntExistException(long subscriptionId)
        {
            SubscriptionId = subscriptionId;
        }

        public override string ToString()
        {
            return string.Format("Subscription with the specified Id doesn't exist: {0}", SubscriptionId);
        }
    }
}
