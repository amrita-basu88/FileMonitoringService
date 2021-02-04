namespace FileMonitorService.Core
{
    using System;

    public class SubscriptionAlreadyExistsException : Exception
    {
        public long SubscriptionId { get; private set; }

        public SubscriptionAlreadyExistsException(long subscriptionId)
        {
            SubscriptionId = subscriptionId;
        }

        public override string ToString()
        {
            return string.Format("Subscription with the specified Id already exists: {0}", SubscriptionId);
        }
    }
}
