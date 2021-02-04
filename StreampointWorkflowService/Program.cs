using LoggerSingleton;
using System;
using System.Configuration;
using System.IO;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace WMTStreampointWorkflowService
{
    static class Program
    {
        private static readonly string SubscriptionStorePath = ConfigurationManager.AppSettings["SubscriptionStorePath"];

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main(string[] arguments)
        {
            AppDomain.CurrentDomain.UnhandledException += UnCaughtExceptionHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += ApplicationThreadException;

            bool isStartedInConsoleMode = false;

            foreach (string argument in arguments)
            {
                if (argument.ToLower().IndexOf("/runasconsole", StringComparison.Ordinal) >= 0)
                {
                    isStartedInConsoleMode = true;
                }
            }

            if (isStartedInConsoleMode)
            {
                WMTWorkflowService wmtWorkflowService = null;
                try
                {
                    wmtWorkflowService = new WMTWorkflowService();
                    wmtWorkflowService.StartService();

                    SingletonLogger.Instance.Info("Press 'Enter' to stop the service...");
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    SingletonLogger.Instance.Error(e);
                }
                finally
                {
                    if (wmtWorkflowService != null)
                    {
                        wmtWorkflowService.StopService();
                    }
                }
            }
            else
            {
                ServiceBase[] servicesToRun = new ServiceBase[] { new WMTWorkflowService() };
                ServiceBase.Run(servicesToRun);
            }
        }

        static void UnCaughtExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;
            SingletonLogger.Instance.Fatal("Uncaught Exception" + Environment.NewLine + ex);
        }

        static void ApplicationThreadException(object sender, ThreadExceptionEventArgs args)
        {
            Exception ex = args.Exception;
            SingletonLogger.Instance.Fatal("Unhandled Application Thread Exception" + Environment.NewLine  + ex);
        }
    }
}

