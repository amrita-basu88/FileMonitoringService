using LoggerSingleton;
using System;
using System.IO;
using System.Linq;
using FileMonitorService.Models;
using System.Collections.Generic;


namespace FileMonitorService.Core
{
    public delegate void SubscriptionPathNotFoundEventHandler(string path, ISubscriptionStore store);

    public static class FileMonitor
    {
        public static event SubscriptionPathNotFoundEventHandler SubscriptionPathNotFound;

        public static void MonitorFiles(ISubscriptionStore subscriptionStore, IStorageRepository storageRepository, INotifier notifier, IIsRunning isRunning)
        {
            if (!isRunning.IsRunning)
            {
                SingletonLogger.Instance.Info("FileMonitor is stopping as requested.");
                return;
            }

            var dueSubscriptions = subscriptionStore.GetSubscriptionsDueForMonitoring(DateTime.Now);
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("{0} subscriptions due for monitoring.", dueSubscriptions.Count()));
            }

            foreach (var subscription in dueSubscriptions.ToList()) // Avoid lazy loading here, to close datareader
            {
                if (!isRunning.IsRunning)
                {
                    SingletonLogger.Instance.Info("FileMonitor is stopping as requested.");
                    return;
                }

                if (SingletonLogger.Instance.IsDebugEnabled())
                {
                    SingletonLogger.Instance.Debug(String.Format("Monitoring subscription {0} @ {1}", subscription.Id, subscription.Path));
                }

                if (!Directory.Exists(subscription.Path))
                {
                    SingletonLogger.Instance.Warn(String.Format("Path {0} does not exist for subscription {1}", subscription.Path, subscription.Id));

                    if (SubscriptionPathNotFound != null)
                    {
                        SubscriptionPathNotFound(subscription.Path, subscriptionStore);
                    }
                }
                if (Directory.Exists(subscription.Path))
                {
                    var files = storageRepository.GetFiles(subscription.Path, subscription.IsRecursive, subscription.IsWatchingDirectories, subscription.IsWatchingFiles);
                    var fileIndex = subscription.GetFileIndex();

                    // start
                    var newFiles = IndexComparer.GetNewFiles(files, fileIndex);
                    var updatedFiles = IndexComparer.GetUpdatedFiles(files, fileIndex);
                    var deletedFiles = IndexComparer.GetDeletedFiles(files, fileIndex);

                    foreach (var file in newFiles)
                    {
                        SendNotificationAndUpdateIndexIfSuccessfull(subscriptionStore, notifier, subscription, file, NotificationType.New);
                    }

                    foreach (var file in updatedFiles)
                    {
                        SendNotificationAndUpdateIndexIfSuccessfull(subscriptionStore, notifier, subscription, file, NotificationType.Update);
                    }

                    foreach (var file in deletedFiles)
                    {
                        SendNotificationAndUpdateIndexIfSuccessfull(subscriptionStore, notifier, subscription, file, NotificationType.Delete);
                    }
                    // end

                    if (newFiles.Any() | updatedFiles.Any() | deletedFiles.Any())
                    {
                        if (SingletonLogger.Instance.IsDebugEnabled())
                        {
                            SingletonLogger.Instance.Debug(String.Format("Found files new={0} updated={1} deleted={2} for subscription {3} @ {4}",
                                    newFiles.Count(), updatedFiles.Count(), deletedFiles.Count(), subscription.Id, subscription.Path));
                        }
                    }
                    subscription.UpdateLastRunAndNextCheckDate(DateTime.Now);
                    subscriptionStore.SaveLastRunAndNextCheckDate(subscription);
                }
            }
        }

        private static void SendNotificationAndUpdateIndexIfSuccessfull(ISubscriptionStore subscriptionStore, INotifier notifier, Subscription subscription, NetworkFile file, NotificationType notificationType)
        {
            if (SingletonLogger.Instance.IsTraceEnabled())
            {
                SingletonLogger.Instance.Trace(String.Format("Sending '{0}' notification for file: {1}.", notificationType, file.Path));
            }

            try
            {
                NotificationData notificationData = new NotificationData
                {
                    InvokeMethodData = new InvokeMethodData(subscription.InvokeMethodData),
                    FilePath = file.Path,
                    NotificationType = notificationType
                };

                notifier.Notify(notificationData);
                switch (notificationType)
                {
                    case NotificationType.New:
                        subscriptionStore.AddFile(subscription, file);
                        break;
                    case NotificationType.Update:
                        subscriptionStore.UpdateFile(subscription, file);
                        break;
                    case NotificationType.Delete:
                        subscriptionStore.DeleteFile(subscription, file);
                        break;
                    default:
                        throw new NotSupportedException(string.Format("NotificationType {0} not supported.", notificationType));
                }
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Could not notify subscriber at {0} {1}. Continuing with next notification. Error: {2}",
                    subscription.InvokeMethodData.ClassName, subscription.InvokeMethodData.MethodName, ex));
            }
        }

    }
}
