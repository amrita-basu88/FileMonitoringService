using System;
using System.Configuration;
using FileMonitorService.Models;
using LoggerSingleton;

namespace StreampointWorkflow.BusinessLogic
{
    public sealed class MonitorStorageChanges
    {
        private static readonly Int32 IntervalChangeInStorageRoot = Int32.Parse(ConfigurationManager.AppSettings["IntervalChangeInStorageRoot"]);

        public static event EventHandler<Subscription> MonitorRootFolder;

        private void OnMonitorRootFolder(Subscription subscription)
        {
            var handler = MonitorRootFolder;
            if (handler != null)
            {
                handler(this, subscription);
            }
        }

        public MonitorStorageChanges(String rootPathToMonitor)
        {
            Subscription subscription = new Subscription
            {
                Path = rootPathToMonitor,
                InvokeMethodData = new InvokeMethodData
                {
                    AssemblyName = typeof(MonitorStorageChanges).Assembly.FullName,
                    ClassName = typeof(MonitorStorageChanges).FullName,
                    MethodName = "ChangeInStorageRoot"  // in C# 6 you can do nameof(MonitorStorageChanges.ChangeInStorageRoot)
                },
                IsRecursive = false,
                IsWatchingDirectories = true,
                IsWatchingFiles = false,
                IntervalInSeconds = IntervalChangeInStorageRoot
            };

            OnMonitorRootFolder(subscription);
        }

        public MonitorStorageChanges()
        {
        }

        public void ChangeInStorageRoot(NotificationModel notificationModel)
        {
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("New notification in ChangeInStorageRoot. Type={0} Path={1}", notificationModel.Type, notificationModel.Path));
            }

            if (notificationModel.Type.ToLower() == "new")
            {
                WorkflowNewProject workflowNewProject = new WorkflowNewProject();
                workflowNewProject.FoundNewProject(notificationModel.Path);
            }
        }
    }
}
