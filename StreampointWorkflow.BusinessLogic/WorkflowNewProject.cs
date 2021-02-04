using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using FileMonitorService.Models;
using LoggerSingleton;
using WMTClasses;
using WMTInterplay;

namespace StreampointWorkflow.BusinessLogic
{
    public class WorkflowNewProject
    {
        private static readonly String IgnoreDirectoryIsilon = ConfigurationManager.AppSettings["IgnoreDirectoryIsilon"];
        private static readonly Int32 IntervalChangeInCamSerialDirectory = Int32.Parse(ConfigurationManager.AppSettings["IntervalChangeInCamSerialDirectory"]);
        private static readonly Int32 IntervalFoundNewTsFolder = Int32.Parse(ConfigurationManager.AppSettings["IntervalFoundNewTsFolder"]);
        private static readonly String BasePathAvidInterplay = ConfigurationManager.AppSettings["BasePathAvidInterplay"];

        public static event EventHandler<Subscription> MonitorSubFolder;

        protected virtual void OnMonitorSubFolder(Subscription subscription)
        {
            var handler = MonitorSubFolder;
            if (handler != null)
            {
                handler(this, subscription);
            }
        }

        public static event EventHandler<Subscription> MonitorCamSerialFolder;

        protected virtual void OnMonitorCamSerialFolder(Subscription subscription)
        {
            var handler = MonitorCamSerialFolder;
            if (handler != null)
            {
                handler(this, subscription);
            }
        }

        // Input: TopLevel Directory project where an QRCODE.XML should be in one of the subfolders
        public void FoundNewProject( String path )
        {
            try
            {
                String directory = Path.GetFileName(path);
                if (directory == null)
                {
                    SingletonLogger.Instance.Info(String.Format("Isilon folder is null. Full Path={0}", path));
                    return;
                }

                if (directory.Equals(IgnoreDirectoryIsilon))
                {
                    SingletonLogger.Instance.Info(String.Format("Ignoring Isilon folder with name='{0}'", IgnoreDirectoryIsilon));
                    return;
                }

                // Read new project directory 
                List<String> camSerialDirectorys = CamSerialDirectorys(path);
                foreach (var camSerialDirectory in camSerialDirectorys)
                {
                    FoundNewCamSerialDirectory(camSerialDirectory);
                }

                // start monitoring subfolder FoundNewProject for new WMTxxxx_xxxxx directories
                StartMonitoringCamserialFolder(path);
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in FoundNewProject.{0}Exception={1}",
                    Environment.NewLine, ex));
            }
        }

        // CamserialDirectory has name WMTxxxx_xxxx ( example 'WMT4146_35467' ) and is located under project map ( example 'United_SoftwareTest' )
        public void FoundNewCamSerialDirectory(String camSerialDirectory)
        {
            String projectXml = Path.Combine(camSerialDirectory, "General", "PROJECT.XML");
            String clipsSubFolder = Path.Combine(camSerialDirectory, "Sub");

            // Step 1: does project XML exists? If yes, exit, if no continue
            if (!Directory.Exists(clipsSubFolder))
            {
                SingletonLogger.Instance.Error(String.Format("Folder 'SUB' does not exists in {0}", camSerialDirectory));
                return;
            }

            // Step 2: read QRCODE.XML
            QRScan qrScan = ReadQrCodeFile(camSerialDirectory);
            if (qrScan == null)
            {
                SingletonLogger.Instance.Error(String.Format("Could not find QRCODE.XML in map {0}.", camSerialDirectory));
                return;
            }

            if (!File.Exists(projectXml))
            {
                // Step 3: Create PROJECT.XML file with info from previous steps above Isilon(big storage)
                CreateProjectXmlFile(qrScan, projectXml);
            }

            AvidInterplay interplay = new AvidInterplay(AvidInterplay.UserAvidInterplay, AvidInterplay.PasswordAvidInterplay, AvidInterplay.BaseUriAvidInterplay, AvidInterplay.HostUrlAvidInterplay);
            if (!interplay.CreateProjectFolders(qrScan, BasePathAvidInterplay))
            {
                SingletonLogger.Instance.Warn("interplay.CreateProjectFolders failed, Basepath:{0}", BasePathAvidInterplay);
            }

            // Step 4: start to monitor for new videofiles in folder 'Sub'
            StartMonitoringSubFolder(clipsSubFolder, projectXml, camSerialDirectory);
        }

        private List<String> CamSerialDirectorys(String path)
        {
            List<String> camSerialDirectorys = new List<String>();

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            DirectoryInfo[] directories = directoryInfo.GetDirectories("WMT*", SearchOption.TopDirectoryOnly);

            foreach (var directory in directories)
            {
                camSerialDirectorys.Add(directory.FullName);
            }
            return camSerialDirectorys;
        }

        private QRScan ReadQrCodeFile(String camSerialDirectory)
        {
            String qrcodeXml = Path.Combine(camSerialDirectory, "QRCODE.XML");
            if (!File.Exists(qrcodeXml))
            {
                return null;
            }

            QRScan qrScan = XmlHelper.ReadXmlFile<QRScan>(qrcodeXml);

            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("qrcode camserial={0} ProjectNo={1} Customer={2} WMTSerial={3} Customer={4} Episode={5}",
                        qrScan.CamSerial, qrScan.ProjectNR, qrScan.Customer, qrScan.WMTSerial, qrScan.Customer, qrScan.Episode));
            }

            return qrScan;
        }

        private void CreateProjectXmlFile(QRScan qrScan, String projectXml)
        {
            String workSpace = String.Format("{0}/{1}", qrScan.Customer, qrScan.Program);
            String baseFolder = String.Format("Projects/{0}/{1}", workSpace, qrScan.Episode);
            String ingestFolder = String.Format("1 INGELADEN MATERIAAL/{0}", qrScan.CamSerial);

            Project project = new Project
            {
                INTERPLAY = new Interplay
                {
                    BASEPATH = "interplay://AvidWorkGroup",
                    BASEFOLDER = baseFolder,
                    INGESTFOLDER = ingestFolder,
                    SYSTEM = "--info volgt--"
                },
                WORKSPACE = new Workspace
                {
                    NAME = workSpace,
                    RESOLUTION = new Resolution {LOWRES = "", HIGHRES = ""}
                },
                LOOMS = "--info volgt--"
            };

            XmlHelper.WriteXmlFile(project, projectXml);

            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("New project file written to {0}.", projectXml));
            }
        }

        public void StartMonitoringCamserialFolder(String rootPathToMonitor)
        {
            Subscription subscription = new Subscription
            {
                Path = rootPathToMonitor,
                InvokeMethodData = new InvokeMethodData
                {
                    AssemblyName = typeof(WorkflowNewProject).Assembly.FullName,
                    ClassName = typeof(WorkflowNewProject).FullName,
                    MethodName = "ChangeInCamSerialDirectory"  // in C# 6 you can do nameof(WorkflowNewProject.ChangeInCamSerialDirectory)
                },
                IsRecursive = false,
                IsWatchingDirectories = true,
                IsWatchingFiles = false,
                IntervalInSeconds = IntervalChangeInCamSerialDirectory
            };

            OnMonitorCamSerialFolder(subscription);
        }

        public void ChangeInCamSerialDirectory(NotificationModel notificationModel)
        {
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug( String.Format("New notification in ChangeInCamSerialDirectory. Type={0} Path={1}", notificationModel.Type, notificationModel.Path));
            }

            if (notificationModel.Type.ToLower() == "new")
            {
                FoundNewCamSerialDirectory(notificationModel.Path);
            }
        }

        private void StartMonitoringSubFolder(String subFolder, String projectXmlFile, String camSerialDirectory)
        {
            Subscription subscription = new Subscription
            {
                Path = subFolder,
                InvokeMethodData = new InvokeMethodData
                {
                    AssemblyName = typeof(WorkflowNewFile).Assembly.FullName,
                    ClassName = typeof(WorkflowNewFile).FullName,
                    MethodName = "FoundNewTsFolder", // in C# 6 you can do nameof(WorkflowNewFile.FoundNewFile)
                    MethodParameters = new List<InvokeMethodParameterData>()
                    {
                        new InvokeMethodParameterData
                        {
                            AssemblyName = typeof(String).Assembly.FullName,
                            ClassName = typeof(String).FullName,
                            XmlData = InvokeMethodParameterData.SerializeToXmlData( projectXmlFile )
                        },
                        new InvokeMethodParameterData
                        {
                            AssemblyName = typeof(String).Assembly.FullName,
                            ClassName = typeof(String).FullName,
                            XmlData = InvokeMethodParameterData.SerializeToXmlData( camSerialDirectory )
                        }
                    }
                },
                IsRecursive = false,
                IsWatchingDirectories = true,
                IsWatchingFiles = false,
                IntervalInSeconds = IntervalFoundNewTsFolder
            };

            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("Start subscription on monitoring folder {0}", subFolder));
            }

            OnMonitorSubFolder(subscription);
        }
    }
}
