using FileMonitorService.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace FileMonitorService.Core
{
    /// <summary>
    /// DEPRECATED (May still work though. Currently the project uses DatabaseSubscriptionStore)
    /// 
    /// Persists the subscriptions to an XML file.
    /// </summary>
    public class XmlSubscriptionStore : ISubscriptionStore
    {
        private static readonly object _subscriptionStoreLock = new object();
        private readonly string _subscriptionStorePath;
        private readonly XmlSerializer _serializer;

        /// <summary>
        /// Event for reporting updates to dashboard
        /// </summary>
        public event ChangedEventHandler ChangedEvent;

        /// <summary>
        /// Initializes a new instance of the XmlSubscriptionStore.
        /// </summary>
        /// <param name="subscriptionStorePath">The absolute path to the subscription store XML file.</param>
        public XmlSubscriptionStore(string subscriptionStorePath)
        {
            _subscriptionStorePath = subscriptionStorePath;
            _serializer = new XmlSerializer(typeof(SubscriptionsModel));
        }

        public IEnumerable<Subscription> GetAll()
        {
            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                return store.Convert();
            }
        }

        public Subscription Get(long id)
        {
            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var subscription = store.Subscriptions.FirstOrDefault(s => s.Id == id);
                if (subscription == null)
                {
                    return null;
                }

                return subscription.Convert();
            }
        }

        public void Create(Subscription subscription)
        {
            ValidateSubscription(subscription);

            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var existingSubscription = store.Subscriptions.FirstOrDefault(s => s.Id == subscription.Id);
                if (existingSubscription != null)
                {
                    throw new SubscriptionAlreadyExistsException(subscription.Id);
                }

                var model = SubscriptionModel.CreateFrom(subscription);
                model.NextCheckDate = null;
                model.LastRunDate = null;
                model.FileIndex.NetworkFiles.Clear();

                store.Subscriptions.Add(model);
                Serialize(store);
            }
        }

        public void Update(Subscription subscription)
        {
            ValidateSubscription(subscription);

            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var existingSubscription = store.Subscriptions.FirstOrDefault(s => s.Id == subscription.Id);
                if (existingSubscription == null)
                {
                    throw new SubscriptionDoesntExistException(subscription.Id);
                }

                existingSubscription.Path = subscription.Path;
                existingSubscription.IsRecursive = subscription.IsRecursive;
                existingSubscription.IsWatchingDirectories = subscription.IsWatchingDirectories;
                existingSubscription.IsWatchingFiles = subscription.IsWatchingFiles;
                existingSubscription.InvokeMethodData = new InvokeMethodData(subscription.InvokeMethodData);
                existingSubscription.IntervalInSeconds = subscription.IntervalInSeconds;
                Serialize(store);
            }
        }

        public void Delete(long subscriptionId)
        {
            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var existingSubscription = store.Subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
                if (existingSubscription == null)
                {
                    throw new SubscriptionDoesntExistException(subscriptionId);
                }

                store.Subscriptions.Remove(existingSubscription);
                Serialize(store);
            }
        }

        public IEnumerable<Subscription> GetSubscriptionsDueForMonitoring(DateTime now)
        {
            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var dueSubscriptions = store.Subscriptions.Where(s => !s.NextCheckDate.HasValue || (s.NextCheckDate.HasValue && s.NextCheckDate <= now));
                return SubscriptionsModel.Convert(dueSubscriptions);
            }
        }

        public void SaveFileIndex(Subscription subscription)
        {
            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var storedSubscription = store.Subscriptions.FirstOrDefault(s => s.Id == subscription.Id);
                if (storedSubscription == null)
                {
                    throw new SubscriptionDoesntExistException(subscription.Id);
                }

                storedSubscription.FileIndex = FileIndexModel.CreateFrom(subscription.GetFileIndex());
                Serialize(store);
            }
        }

        public void SaveLastRunAndNextCheckDate(Subscription subscription)
        {
            lock (_subscriptionStoreLock)
            {
                var store = Deserialize();
                var storedSubscription = store.Subscriptions.FirstOrDefault(s => s.Id == subscription.Id);
                if (storedSubscription == null)
                {
                    throw new SubscriptionDoesntExistException(subscription.Id);
                }

                storedSubscription.LastRunDate = subscription.LastRunDate;
                storedSubscription.NextCheckDate = subscription.NextCheckDate;
                Serialize(store);
            }
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

        private SubscriptionsModel Deserialize()
        {
            var fileInfo = new FileInfo(_subscriptionStorePath);
            if (!fileInfo.Exists)
            {
                return new SubscriptionsModel
                {
                    Subscriptions = new List<SubscriptionModel>()
                };
            }

            using (var reader = XmlReader.Create(_subscriptionStorePath))
            {
                return (SubscriptionsModel)_serializer.Deserialize(reader);
            }
        }

        private void Serialize(SubscriptionsModel store)
        {
            using ( var writer = XmlWriter.Create(_subscriptionStorePath, new XmlWriterSettings {Indent = true}) )
            {
                _serializer.Serialize(writer, store);
            }

            if (ChangedEvent != null)
            {
                ChangedEvent(GetAll());
            }
        }

        public void AddFile(Subscription subscription, NetworkFile networkFile)
        {
            subscription.AddToFileIndex(networkFile);
            SaveFileIndex(subscription);
        }

        public void UpdateFile(Subscription subscription, NetworkFile networkFile)
        {
            subscription.UpdateFileIndex(networkFile);
            SaveFileIndex(subscription);
        }

        public void DeleteFile(Subscription subscription, NetworkFile networkFile)
        {
            subscription.DeleteFromFileIndex(networkFile);
            SaveFileIndex(subscription);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
