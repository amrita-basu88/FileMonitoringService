namespace FileMonitorService.Core
{
    using FileMonitorService.Data;
    using FileMonitorService.Models;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Entity;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// Persists the subscriptions to an XML file.
    /// </summary>
    public class DatabaseSubscriptionStore : ISubscriptionStore
    {
        private static readonly Int32 IntervalUpdateDashboardData = Int32.Parse(ConfigurationManager.AppSettings["IntervalUpdateDashboardData"]);
        private static readonly object _subscriptionStoreLock = new object();

        /// <summary>
        /// Event for reporting updates to dashboard
        /// </summary>
        public event ChangedEventHandler ChangedEvent;

        /// <summary>
        /// Database context, entity framework
        /// </summary>
        private FileMonitorDbContext context;

        /// <summary>
        /// Initializes a new instance of the XmlSubscriptionStore.
        /// </summary>
        public DatabaseSubscriptionStore()
        {
            context = new FileMonitorDbContext();
        }

        /// <summary>
        /// Get all subscriptions
        /// </summary>
        /// <returns>All subscriptions</returns>
        public IEnumerable<Subscription> GetAll()
        {
            return context.Subscriptions.Include(s => s.InvokeMethodData).ToList();
        }

        public Subscription Get(long id)
        {
            return context.Subscriptions.Include(s => s.InvokeMethodData).FirstOrDefault(s => s.Id == id);            
        }

        public void Create(Subscription subscription)
        {
            ValidateSubscription(subscription);

            if (context.Subscriptions.Any(s => s.Id == subscription.Id))
            {
                throw new SubscriptionAlreadyExistsException(subscription.Id);
            }

            subscription.NextCheckDate = null;
            subscription.LastRunDate = null;
            subscription.fileIndex.Clear();

            context.Subscriptions.Add(subscription);
            SaveChanges();
        }

        public void Update(Subscription subscription)
        {
            ValidateSubscription(subscription);

            var existingSubscription = Get(subscription.Id);
            if (existingSubscription == null)
            {
                throw new SubscriptionDoesntExistException(subscription.Id);
            }

            existingSubscription.Path = subscription.Path;
            existingSubscription.IsRecursive = subscription.IsRecursive;
            existingSubscription.IsWatchingDirectories = subscription.IsWatchingDirectories;
            existingSubscription.IsWatchingFiles = subscription.IsWatchingFiles;
            existingSubscription.IntervalInSeconds = subscription.IntervalInSeconds;
            var oldInvokeMethodData = existingSubscription.InvokeMethodData;
            existingSubscription.InvokeMethodData = subscription.InvokeMethodData;

            // When replacing the InvokeMethodData entry, make sure the old one is removed
            context.InvokeMethodDatas.Remove(oldInvokeMethodData);

            SaveChanges();
        }

        public void Delete(long subscriptionId)
        {
            var existingSubscription = context.Subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (existingSubscription == null)
            {
                throw new SubscriptionDoesntExistException(subscriptionId);
            }

            // Unfortunately, need to delete property InvokeMethodData  manually
            // Because EF can't handle the foreign key direction properly
            if (existingSubscription.InvokeMethodData != null)
            {
                context.InvokeMethodDatas.Remove(existingSubscription.InvokeMethodData);
            }
            context.Subscriptions.Remove(existingSubscription);
            SaveChanges();
        }

        public IEnumerable<Subscription> GetSubscriptionsDueForMonitoring(DateTime now)
        {
            var dueSubscriptions = context.Subscriptions
                .Include(s => s.InvokeMethodData)
                .Include(s => s.InvokeMethodData.MethodParameters)
                .Where(s => !s.NextCheckDate.HasValue || (s.NextCheckDate.HasValue && s.NextCheckDate <= now));
            return dueSubscriptions;          
        }

        public void SaveChanges()
        {
            context.SaveChanges();
            if (ChangedEvent != null)
            {
                ChangedEvent(GetAll());
            }
        }

        public void SaveFileIndex(Subscription subscription)
        {
            // is part of the interface, but saving already happens automatically
        }

        public void SaveLastRunAndNextCheckDate(Subscription subscription)
        {
            var storedSubscription = Get(subscription.Id);
            if (storedSubscription == null)
            {
                throw new SubscriptionDoesntExistException(subscription.Id);
            }

            storedSubscription.LastRunDate = subscription.LastRunDate;
            storedSubscription.NextCheckDate = subscription.NextCheckDate;
            SaveChanges();
        }

        private void ValidateSubscription(Subscription subscription)
        {
            if (subscription == null)
            {
                throw new InvalidSubscriptionException("Please provide a subscription instance.");
            }

            if (string.IsNullOrWhiteSpace(subscription.Path))
            {
                throw new InvalidSubscriptionException("Please provide a subscription path.");
            }

            if (string.IsNullOrWhiteSpace(subscription.InvokeMethodData.AssemblyName))
            {
                throw new InvalidSubscriptionException("Please provide a subscription AssemblyName.");
            }

            if (string.IsNullOrWhiteSpace(subscription.InvokeMethodData.ClassName))
            {
                throw new InvalidSubscriptionException("Please provide a subscription ClassName.");
            }

            if (string.IsNullOrWhiteSpace(subscription.InvokeMethodData.MethodName))
            {
                throw new InvalidSubscriptionException("Please provide a subscription MethodName.");
            }

            if (subscription.IntervalInSeconds < 1)
            {
                throw new InvalidSubscriptionException("The subscription interval in seconds should be greater than zero.");
            }
        }

        public void Dispose()
        {
            context.Dispose();
        }


        #region FileOperations
        
        public void AddFile(Subscription subscription, NetworkFile networkFile)
        {
            subscription.AddToFileIndex(networkFile);
            SaveChanges();
        }

        public void UpdateFile(Subscription subscription, NetworkFile networkFile)
        {
            subscription.UpdateFileIndex(networkFile);
            SaveChanges();
        }

        public void DeleteFile(Subscription subscription, NetworkFile networkFile)
        {
            subscription.DeleteFromFileIndex(networkFile);
            context.NetworkFiles.Remove(networkFile);
            SaveChanges();
        }

        #endregion

    }
}
