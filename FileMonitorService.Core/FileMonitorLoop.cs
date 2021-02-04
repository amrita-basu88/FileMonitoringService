using System;
using System.Collections.Generic;
using System.Linq;
using FileMonitorService.Models;
using WebDashboard;
using System.Configuration;
using LoggerSingleton;

namespace FileMonitorService.Core
{
    public class FileMonitorLoop : IIsRunning
    {
        private StorageRepository _storageRepository;
        private Notifier _notifier;

        /// <summary>
        /// Dashboard update interval
        /// </summary>
        private static readonly Int32 IntervalUpdateDashboardData = Int32.Parse(ConfigurationManager.AppSettings["IntervalUpdateDashboardData"]);
        private DateTime lastUpdate;

        /// <summary>
        /// Constructor
        /// </summary>
        public FileMonitorLoop()
        {
            FileMonitor.SubscriptionPathNotFound += FileMonitor_SubscriptionPathNotFoundhandler;
        }

        /// <summary>
        /// Bootstraps the class and starts the timer. Should not contain any blocking calls.
        /// </summary>
        public void Start()
        {
            _storageRepository = new StorageRepository();
            _notifier = new Notifier();

            IsRunning = true;
        }

        public void DoMonitorFiles()
        {
            if (!IsRunning)
            {
                return;
            }
            using (ISubscriptionStore store = new DatabaseSubscriptionStore())
            {
                try
                {
                    store.ChangedEvent += DatabaseSubscriptionStore_ChangedEvent;
                    FileMonitor.MonitorFiles(store, _storageRepository, _notifier, this);
                }
                catch (Exception ex)
                {
                    LoggerSingleton.SingletonLogger.Instance.Error(ex);
                }
            }
        }

        void DatabaseSubscriptionStore_ChangedEvent(IEnumerable<Subscription> subscriptions)
        {
            if (!((DateTime.Now - lastUpdate).TotalSeconds > IntervalUpdateDashboardData))
            {
                return;
            }
            WebDashboardLogic.CreateHTML(subscriptions);
            lastUpdate = DateTime.Now;
        }

        public void OnProcessSubscription(object sender, Subscription subscription)
        {
            ProcessSubscription(subscription);
        }

        private void ProcessSubscription(Subscription subscription)
        {
            if (!IsRunning)
            {
                return;
            }
            using (ISubscriptionStore store = new DatabaseSubscriptionStore())
            {
                try
                {
                    CreateOrUpdateSubscription(store, subscription);
                }
                catch (Exception ex)
                {
                    LoggerSingleton.SingletonLogger.Instance.Error(ex);
                }
            }
        }

        private void CreateOrUpdateSubscription(ISubscriptionStore store, Subscription subscription)
        {
            IEnumerable<Subscription> allSubscriptions = store.GetAll();

            var existingSubscription = allSubscriptions.FirstOrDefault(
                s => s.Path.Equals(subscription.Path, StringComparison.InvariantCultureIgnoreCase));
            if (existingSubscription == null)
            {
                store.Create(subscription);
            }
            else
            {
                subscription.Id = existingSubscription.Id;
                store.Update(subscription);
            }
        }

        /// <summary>
        /// Handler for delete subscription event
        /// </summary>
        /// <param name="subscriptionPath">deleted path</param>
        /// <param name="store">subscription store</param>
        private void FileMonitor_SubscriptionPathNotFoundhandler(string subscriptionPath, ISubscriptionStore store)
        {
            SingletonLogger.Instance.Info(String.Format("Removing subscriptions for path {0}", subscriptionPath));
            IEnumerable<Subscription> allSubscriptions = store.GetAll();
            var matchingSubscriptions = allSubscriptions
                .Where(s => s.Path.StartsWith(subscriptionPath, StringComparison.OrdinalIgnoreCase));

            foreach (var matchingSubscription in matchingSubscriptions)
            {
                store.Delete(matchingSubscription.Id);
                SingletonLogger.Instance.Debug(String.Format("Removed subscription for path {0}", matchingSubscription.Path));
            }
        }

        /// <summary>
        /// Stops the timer and blocks until the current work is completed.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        public bool IsRunning { get; private set; }
    }
}
