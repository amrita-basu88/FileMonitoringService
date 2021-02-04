namespace FileMonitorService.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manages the subscriptions' file indexes.
    /// </summary>
    public interface IFileIndexStore
    {
        IEnumerable<Subscription> GetSubscriptionsDueForMonitoring(DateTime now);
        void SaveFileIndex(Subscription subscription);
        void SaveLastRunAndNextCheckDate(Subscription subscription);
    }
}
