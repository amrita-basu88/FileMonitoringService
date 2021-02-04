namespace FileMonitorService.Models
{
    using System;
    using System.Collections.Generic;

    public delegate void ChangedEventHandler(IEnumerable<Subscription> subscriptions);
    
    /// <summary>
    /// Manages the subscription details. Ignores the file indexes.
    /// </summary>
    public interface ISubscriptionStore : IFileIndexStore, IDisposable
    {
        IEnumerable<Subscription> GetAll();
        Subscription Get(long id);
        void Create(Subscription subscription);
        void Update(Subscription subscription);
        void Delete(long subscriptionId);
    
        void AddFile(Subscription subscription, NetworkFile networkFile);
        void UpdateFile(Subscription subscription, NetworkFile networkFile);
        void DeleteFile(Subscription subscription, NetworkFile networkFile);

        event ChangedEventHandler ChangedEvent;
    }
}
