using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FileMonitorService.Models;
using LoggerSingleton;
using WMTClasses;
using Utilities;

namespace StreampointWorkflow.BusinessLogic
{
    public class WorkflowNewFile
    {
        private static readonly string ClipsProcessingConsoleApp = ConfigurationManager.AppSettings["ClipsProcessingConsoleApp"];

        public void FoundNewTsFolder(NotificationModel notificationModel, String projectXmlFile, String camSerialDirectory)
        {
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("New notification in FoundNewTsFolder. Type={0} Path={1}", notificationModel.Type, notificationModel.Path));
            }

            if (notificationModel.Type.ToLower() != "new")
            {
                return;
            }

            String finishedClipXmlFile = notificationModel.Path.Replace(".TSFOLDER", ".XML");
            if (FileHelper.FileExistsAndNotLocked(finishedClipXmlFile))
            {
                NonRealTimeMeta nonRealTimeMeta = XmlHelper.ReadXmlFile<NonRealTimeMeta>(finishedClipXmlFile);

                if (nonRealTimeMeta.MobId != null && !String.IsNullOrEmpty(nonRealTimeMeta.MobId.Master) && !String.IsNullOrEmpty(nonRealTimeMeta.MobId.Source))
                {
                    SingletonLogger.Instance.Warn(String.Format("MobIds master='{0}' source={1} already exists in XML. Skip processing clips in FoundNewTsFolder={2}", 
                        nonRealTimeMeta.MobId.Master, nonRealTimeMeta.MobId.Source, notificationModel.Path));
                    return;
                }
            }

            String genArgs = String.Format("\"lowres\" \"{0}\" \"{1}\" \"{2}\" \"{3} \"", notificationModel.Path, camSerialDirectory, projectXmlFile, AppDomain.CurrentDomain.BaseDirectory);

            String executableFilename = ClipsProcessingConsoleApp;
            if (ClipsProcessingConsoleApp.Contains(@".\") || !ClipsProcessingConsoleApp.Contains(@":\"))
            {
                executableFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, executableFilename);
            }
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("executableFilename={0}", executableFilename));
            }

            using (Process runProg = new Process())
            {
                try
                {
                    runProg.StartInfo.FileName = executableFilename;
                    runProg.StartInfo.Arguments = genArgs;
                    runProg.StartInfo.CreateNoWindow = false;
                    runProg.Start();
                }
                catch (Exception ex)
                {
                    SingletonLogger.Instance.Error(String.Format("Could not start program {0}. Exception={1}", executableFilename, ex));
                }
            }
        }
    }
}
