namespace FileMonitorService.Core
{
    using System;

    public class InvalidSubscriptionException : Exception
    {
        public InvalidSubscriptionException(string message)
            : base(message)
        {
        }

        public override string ToString()
        {
            return string.Format("The subscription is invalid because of the following reason: {0}", Message);
        }
    }
}
