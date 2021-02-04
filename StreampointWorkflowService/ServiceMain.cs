using LoggerSingleton;
using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;
using System.Threading;
using ClipService;
using EdlService;
using FileMonitorService.Core;
using StreampointWorkflow.BusinessLogic;

namespace WMTStreampointWorkflowService
{
    public class ServiceMain : IServiceMain
    {
        private readonly Int32 fileMonitorLoopInterval;
        private readonly String subscriptionStorePath;
        private readonly String IsilonPath;

        private Timer timer;
        private FileMonitorLoop fileMonitorLoop;
        private MonitorStorageChanges monitorStorageChanges;
        private ServiceHost serviceHostEdlService;
        private ServiceHost serviceHostClipService;
        private Thread edlLogicThread;
        private EdlLogic edlLogic;
        private Thread clipLogicThread;
        private ClipLogic clipLogic;

        public ServiceMain()
        {
            subscriptionStorePath = ConfigurationManager.AppSettings["SubscriptionStorePath"];
            SingletonLogger.Instance.Info(String.Format("FileMonitorLoop SubscriptionStorePath is '{0}'", subscriptionStorePath));
            IsilonPath = ConfigurationManager.AppSettings["IsilonPath"];
            SingletonLogger.Instance.Info(String.Format("IsilonPath To Monitor is '{0}'", IsilonPath));

            fileMonitorLoopInterval = Int32.Parse(ConfigurationManager.AppSettings["FileMonitorLoopInterval"]);
            TimeSpan t = TimeSpan.FromMilliseconds(fileMonitorLoopInterval);
            SingletonLogger.Instance.Info(String.Format("FileMonitorLoop interval is every {0:D2}m:{1:D2}s", t.Minutes, t.Seconds));
        }

        public void Start()
        {
            try
            {
                String clipsProcessingConsoleApp = ConfigurationManager.AppSettings["ClipsProcessingConsoleApp"];
                if (!File.Exists(clipsProcessingConsoleApp))
                {
                    SingletonLogger.Instance.Error(String.Format("File={0} does not exists", clipsProcessingConsoleApp));
                    return;
                }

                fileMonitorLoop = new FileMonitorLoop();
                WorkflowNewProject.MonitorSubFolder += fileMonitorLoop.OnProcessSubscription;
                WorkflowNewProject.MonitorCamSerialFolder += fileMonitorLoop.OnProcessSubscription;
                MonitorStorageChanges.MonitorRootFolder += fileMonitorLoop.OnProcessSubscription;

                // to prevent WCF http error, for WCF with tcp it is not needed
                // netsh http add urlacl url=http://+:9090/EdlService/EdlLogic  user=Everyone 
                serviceHostEdlService = new ServiceHost(typeof(EdlLogic));
                serviceHostEdlService.Open();

                // to prevent WCF http error, for WCF with tcp it is not needed
                // netsh http add urlacl url=http://+:9190/ClipService/ClipLogic  user=Everyone 
                serviceHostClipService = new ServiceHost(typeof(ClipLogic));
                serviceHostClipService.Open();

                clipLogic = new ClipLogic();
                clipLogicThread = new Thread(clipLogic.ClipProcessing) { Name = "ClipProcessingThread" };

                edlLogic = new EdlLogic();
                edlLogicThread = new Thread(edlLogic.EdlProcessing) { Name = "EdlProcessingThread" };

                clipLogicThread.Start();
                edlLogicThread.Start();

                timer = new Timer(ScheduledProcess, null, fileMonitorLoopInterval, 0);

                fileMonitorLoop.Start();

                monitorStorageChanges = new MonitorStorageChanges(IsilonPath);
            }
            catch (Exception ex)
            {
               SingletonLogger.Instance.Error(ex);
            }
        }

        public void Stop()
        {
            try
            {
                fileMonitorLoop.Stop();
                MonitorStorageChanges.MonitorRootFolder -= fileMonitorLoop.OnProcessSubscription;
                WorkflowNewProject.MonitorCamSerialFolder -= fileMonitorLoop.OnProcessSubscription;
                WorkflowNewProject.MonitorSubFolder -= fileMonitorLoop.OnProcessSubscription;
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(ex);
            }
            finally
            {
                //  CleanupResources here
                if (edlLogic != null)
                {
                    edlLogic.RequestStop();
                }

                if (clipLogic != null)
                {
                    clipLogic.RequestStop();
                }

                if (edlLogicThread != null)
                {
                    edlLogicThread.Join();
                }

                if (clipLogicThread != null)
                {
                    clipLogicThread.Join();
                }

                if (timer != null)
                {
                    timer.Dispose();
                    timer = null;
                }

                if (serviceHostEdlService != null)
                {
                    serviceHostEdlService.Close();
                    serviceHostEdlService = null;
                }

                if (serviceHostClipService != null)
                {
                    serviceHostClipService.Close();
                    serviceHostClipService = null;
                }
            }
        }

        private void ScheduledProcess(object data)
        {
            try
            {
                fileMonitorLoop.DoMonitorFiles();
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(ex);
            }
            finally
            {
                // Start Timer again
                timer.Change(fileMonitorLoopInterval, 0);
            }
        }
    }
}
