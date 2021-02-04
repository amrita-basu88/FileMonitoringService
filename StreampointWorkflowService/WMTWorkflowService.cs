using System.ServiceProcess;
using LoggerSingleton;

namespace WMTStreampointWorkflowService
{
    public partial class WMTWorkflowService : ServiceBase
    {
        private readonly IServiceMain servicemain;

        public WMTWorkflowService()
        {
            InitializeComponent();
            
            servicemain = new ServiceMain();
        }

        protected override void OnStart(string[] args)
        {
            StartService();
        }

        protected override void OnStop()
        {
            StopService();
        }

        public void StartService()
        {
            SingletonLogger.Instance.Info("Starting service");
            servicemain.Start();
            SingletonLogger.Instance.Info("Service has been started");
        }

        public void StopService()
        {
            SingletonLogger.Instance.Info("Stopping service...");
            servicemain.Stop();
            SingletonLogger.Instance.Info("Service has been stopped.");
        }
    }
}
