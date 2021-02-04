using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using AMT_FFMPEG.BusinessLogic;
using ClipServiceProxy;
using EdlServiceProxy;
using LoggerSingleton;
using WMTClasses;
using WMTInterplay;

namespace ClipWorkflow.BusinessLogic
{
    public class ClipProcessingLogic
    {
        private static readonly String IsisPath = ConfigurationManager.AppSettings["IsisPath"];
        private static readonly Int32 TimeoutClipProcessingLogic = Int32.Parse(ConfigurationManager.AppSettings["TimeoutClipProcessingLogic"]);
        private static readonly Int32 TimeoutWhenFileLocked = Int32.Parse(ConfigurationManager.AppSettings["TimeoutWhenFileLocked"]);
        private static readonly String LowresResolution = ConfigurationManager.AppSettings["LowresVideoCompressionFamilyType"];
        private static readonly String BasePathAvidInterplay = ConfigurationManager.AppSettings["BasePathAvidInterplay"];

        private AvidInterplay interplay;

        // input: new partial clip rec-x.ts
        public void StartProcessingLowresClips(String tsFolder, QRScan qrScan, String projectXmlFile, String amtTempFilesDirectory)
        {
            String clipNameMxfOutput = null;
            Int32 recClipId = 1;
            String partialClipFile = Path.Combine(tsFolder, String.Format("rec-{0}.ts", recClipId));

            try
            {
                String finishedClipXmlFile = tsFolder.Replace(".TSFOLDER", ".XML");
                if (FileExistsAndNotLocked(finishedClipXmlFile))
                {
                    NonRealTimeMeta nonRealTimeMeta = XmlHelper.ReadXmlFile<NonRealTimeMeta>(finishedClipXmlFile);

                    if (nonRealTimeMeta.MobId != null && !String.IsNullOrEmpty(nonRealTimeMeta.MobId.Master) && !String.IsNullOrEmpty(nonRealTimeMeta.MobId.Source))
                    {
                        return;
                    }
                }

                if (SingletonLogger.Instance.IsDebugEnabled())
                {
                    SingletonLogger.Instance.Debug(String.Format("Start Processing lowres new partial clip {0}",partialClipFile));
                }

                if (!tsFolder.EndsWith(".TSFOLDER"))
                {
                    SingletonLogger.Instance.Error(String.Format("tsFolder='{0}' does not end with .TSFOLDER", tsFolder));
                    return;
                }

                if (qrScan.OperationMode.Value.Equals(OperationModeEnum.NO))
                {
                    SingletonLogger.Instance.Warn(String.Format("OperationModeEnum is NO for new partial clip {0}",partialClipFile));
                    return;
                }

                if (qrScan.OperationMode.Value.Equals(OperationModeEnum.GROWING))
                {
                    SingletonLogger.Instance.Warn(String.Format("OperationModeEnum is GROWING for new partial clip {0}", partialClipFile));
                    return;
                }

                String partialClipDirectory = Path.GetDirectoryName(partialClipFile);
                if (String.IsNullOrEmpty(partialClipDirectory))
                {
                    SingletonLogger.Instance.Error(String.Format("partialClipDirectory is null or empty. partialClipFile='{0}'", partialClipFile));
                    return;
                }

                int indexOfSubFoldername = tsFolder.IndexOf("Sub", StringComparison.Ordinal);
                if ( -1 >= indexOfSubFoldername)
                {
                    SingletonLogger.Instance.Error(String.Format("subFolder not found in tsFolder string. tsFolder='{0}'", tsFolder));
                    return;
                }

                String editFolder = Path.Combine(tsFolder.Substring(0, indexOfSubFoldername), "Edit");
                if (String.IsNullOrEmpty(editFolder) || !Directory.Exists(editFolder))
                {
                    SingletonLogger.Instance.Error(String.Format("editFolder is null or empty or does not exists. editFolder='{0}'", editFolder));
                    return;
                }

                String subFolder = Path.Combine(tsFolder.Substring(0, indexOfSubFoldername), "Sub");
                if (String.IsNullOrEmpty(subFolder) || !Directory.Exists(subFolder))
                {
                    SingletonLogger.Instance.Error(String.Format("subFolder is null or empty or does not exists. subFolder='{0}'", subFolder));
                    return;
                }

                String highresFullClipName = tsFolder.Replace("Sub", "Clip").Replace(".TSFOLDER", ".TS");

                while (!FileExistsAndNotLocked(partialClipFile))
                {
                }

                // Step 1: process first clip, read TSREC.XML for TimeCode  
                String sonyTimeCode = GetTimeCodeLowRes(partialClipFile);
                ClipConfigsAMTAndFFMPEG clipConfigsAMTAndFFMPEG = Create_AMT_FFMPEG_XML_Configs(sonyTimeCode, qrScan.WMTSerial, qrScan.CamSerial, ClipConfigType.LowresConfig);

                interplay = new AvidInterplay(AvidInterplay.UserAvidInterplay, AvidInterplay.PasswordAvidInterplay, AvidInterplay.BaseUriAvidInterplay, AvidInterplay.HostUrlAvidInterplay);
                StatusValue status = interplay.GetStatus();
                if (status.Error)
                {
                    SingletonLogger.Instance.Error(String.Format("interplay constructor has errorstatus={0}", status.Status));
                    return;
                }

                interplay.FillParameters(qrScan, BasePathAvidInterplay);
                status = interplay.GetStatus();
                if (status.Error)
                {
                    SingletonLogger.Instance.Error(String.Format("FillParameters has errorstatus={0}", status.Status));
                    return;
                } 

                clipNameMxfOutput = new DirectoryInfo(partialClipDirectory).Name.Split('_').First();
                clipNameMxfOutput = String.Format("{0}_{1}", clipNameMxfOutput, qrScan.WMTSerial);

                String interplayMaterialFolder = interplay.LoadedMaterialFolder;

                AMTFFMPEGLogic.Initialize(clipConfigsAMTAndFFMPEG, clipNameMxfOutput, String.Empty, String.Empty, IsisPath, interplayMaterialFolder);

                // Step 2: read Frames with FFMPEG ( External code/program )
                // Step 3: Schrijf Frames naar AMT clip ( External code/program )
                AMTFFMPEGLogic.ProcessClip(partialClipFile);

                recClipId++;

                // Loop read next clip file or xml file is found and then end process
                while (true)
                {
                    if (ProcessedNewUnprocessedClip(tsFolder, ref recClipId))
                    {
                        continue;
                    }

                    if (FileExistsAndNotLocked(finishedClipXmlFile))
                    {
                        if (ProcessedNewUnprocessedClip(tsFolder, ref recClipId))
                        {
                            continue;
                        }
                        break;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(TimeoutClipProcessingLogic));
                }

                ProcessFinishedClipXml(finishedClipXmlFile, projectXmlFile, editFolder, subFolder, highresFullClipName, amtTempFilesDirectory);
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error( String.Format("Unhandled exception in Process StartProcessingLowresClips={0}{1}.Exception={2}", partialClipFile, Environment.NewLine, ex));
            }
            finally
            {
                if (!String.IsNullOrEmpty(clipNameMxfOutput))
                {
                    SingletonLogger.Instance.Debug(String.Format("amtTempFilesDirectory='{0}' clipNameMxfOutput={1}", amtTempFilesDirectory, clipNameMxfOutput));
                    String amtTempClipDirectory = Path.Combine(amtTempFilesDirectory, clipNameMxfOutput);

                    if (Directory.Exists(amtTempClipDirectory))
                    {
                        SingletonLogger.Instance.Info(String.Format("Start Remove recusively AMTTemp directory='{0}'", amtTempClipDirectory));
                        Directory.Delete(amtTempClipDirectory, true);
                        SingletonLogger.Instance.Info(String.Format("Finished Remove recusively AMTTemp directory='{0}'", amtTempClipDirectory));
                    }
                }
            }
        }

        private bool ProcessedNewUnprocessedClip(string tsFolder, ref int recClipId)
        {
            string partialClipFile = Path.Combine(tsFolder, String.Format("rec-{0}.ts", recClipId));
            if (FileExistsAndNotLocked(partialClipFile))
            {
                if (SingletonLogger.Instance.IsDebugEnabled())
                {
                    SingletonLogger.Instance.Debug(String.Format("Start Processing new partial clip {0}", partialClipFile));
                }

                AMTFFMPEGLogic.ProcessClip(partialClipFile);

                recClipId++;
                return true;
            }
            return false;
        }


        private ClipConfigsAMTAndFFMPEG Create_AMT_FFMPEG_XML_Configs(String sonyTimeCode, String wmtSerial, String camSerial, ClipConfigType clipConfigType)
        {
            ClipConfigsAMTAndFFMPEG clipConfigsAMTAndFFMPEG = null;

            String tapeName = String.Format("WMT{0}_{1}", wmtSerial, camSerial);

            clipConfigsAMTAndFFMPEG = new ClipConfigsAMTAndFFMPEG(sonyTimeCode, tapeName, clipConfigType );
            return clipConfigsAMTAndFFMPEG;
        }

        private String GetTimeCodeLowRes(String newPartialClipFile )
        {
            String timecode = String.Empty;

            String tsrecFolder = Path.GetDirectoryName(newPartialClipFile);
            if (String.IsNullOrEmpty(tsrecFolder))
            {
                SingletonLogger.Instance.Error(String.Format("No tsrecFolder for partial clip={0}", newPartialClipFile));
                return timecode;
            }

            String tsrecXmlFile = Path.Combine(tsrecFolder, "TSREC.XML");
            TimeCode timeCodeXml = GetTimeCode(newPartialClipFile, tsrecXmlFile);

            if (timeCodeXml.LtcChangeTable.LtcChange.Count == 0 ||
                String.IsNullOrEmpty(timeCodeXml.LtcChangeTable.LtcChange[0].startTcValueString))
            {
                SingletonLogger.Instance.Error(String.Format("No timecode found in TSREC.XML for partial clip={0}", newPartialClipFile));
                return timecode;
            }

            timecode = timeCodeXml.LtcChangeTable.LtcChange[0].startTcValueString; // Format is FFSSMMHH

            return timecode;
        }

        private static TimeCode GetTimeCode(string newPartialClipFile, string tsrecXmlFile)
        {
            TimeCode timeCode;
            if ( File.Exists(tsrecXmlFile) )
            {
                timeCode = XmlHelper.ReadXmlFile<TimeCode>(tsrecXmlFile);
            }
            else
            {
                SingletonLogger.Instance.Warn( String.Format("No TSREC.XML found for partial Clips={0}. Using default TimeCode 00000000", 
                                               newPartialClipFile));
                
                timeCode = new TimeCode
                {
                    ClipName = "unknown",
                    Fps = "unknown",
                    LtcChangeTable = new LtcChangeTable
                    {
                        tcFps = 0,
                        LtcChange = new List<LtcChange>
                        {
                            new LtcChange
                            {
                                startFrameCountString = "0",
                                startTcValueString = "00000000",
                                status = "unknown"
                            }
                        }
                    }
                };
            }

            return timeCode;
        }

        private Boolean FileExistsAndNotLocked(String filename)
        {
            if (!File.Exists(filename))
            {
                return false;
            }

            FileInfo fileinfo = new FileInfo(filename);

            var isLocked = true;
            try
            {
                using (fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                }

                isLocked = false;
            }
            catch (IOException e)
            {
                SingletonLogger.Instance.Info(String.Format("File is locked {0}.", filename));
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                isLocked = errorCode == 32 || errorCode == 33;

                Thread.Sleep(TimeSpan.FromSeconds(TimeoutWhenFileLocked));
            }

            return !isLocked;
        }

        // input: new NonRealTimeMeta Wxxxx_xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.XML
        private void ProcessFinishedClipXml(String finishedClipXmlFile, String projectXmlFile, String editFolder, String subFolder, String highresFullClipName, String amtTempFilesDirectory)
        {
            try
            {
                if (SingletonLogger.Instance.IsDebugEnabled())
                {
                    SingletonLogger.Instance.Debug(String.Format("Start Processing new finished clip XML {0}", finishedClipXmlFile));
                }

                // Step 1: Check this in in Interplay, returns mob-ID ( sort of string Guid) 
                // master en source mob uit amt stuk
                String masterModId = AMTFFMPEGLogic.GetMasterModId();
                String sourceModId = AMTFFMPEGLogic.GetSourceModId();

                // Step 2: close AMT 
                AMTFFMPEGLogic.Close();

                // Step 3: Choice of right WORKSPACE ?? #CUSTOMER#_#PROGRAM#

                // Step 4: Write MOB-ID in file W000X_DATE_TC.XML ( we can define this in xml file )
                NonRealTimeMeta nonRealTimeMeta = XmlHelper.ReadXmlFile<NonRealTimeMeta>(finishedClipXmlFile);
                nonRealTimeMeta.MobId = new MobId { Master = masterModId, Source = sourceModId };
                XmlHelper.WriteXmlFile(nonRealTimeMeta, finishedClipXmlFile);

                // Step 5: Update PROJECT.XML file with info from previous steps above
                Project project = XmlHelper.ReadXmlFile<Project>(projectXmlFile);
                project.WORKSPACE.RESOLUTION.LOWRES = LowresResolution;
                XmlHelper.WriteXmlFile(project, projectXmlFile);

                if (SingletonLogger.Instance.IsDebugEnabled())
                {
                    SingletonLogger.Instance.Debug("StartWatchingForNewEdls");
                }

                // Set 6: start watch for new Edl
                String highresfolder = interplay.FetchHighresFolder;
                String deviceName = nonRealTimeMeta.Device.deviceName;
                StartWatchingForNewEdls(editFolder, subFolder, highresfolder, deviceName);
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in Process FinishedClipXml={0}.{1}Exception={2}",
                    finishedClipXmlFile, Environment.NewLine, ex));
            }

            try
            {
                StartProcessingHighresClip(highresFullClipName, projectXmlFile, amtTempFilesDirectory);
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in Process FinishedClipXml={0}.{1}Exception={2}",
                    highresFullClipName, Environment.NewLine, ex));
            }
        }

        public void StartProcessingHighresClip(String highresFullClipName, String projectXmlFile, String amtTempFilesDirectory)
        {
            // do not use 'using' on WCF, more info at https://msdn.microsoft.com/en-us/library/ms733912.aspx
            ClipLogicProxy proxy = new ClipLogicProxy();
            try
            {
                proxy.StartProcessingHighresClip(highresFullClipName, projectXmlFile, amtTempFilesDirectory);
                proxy.Close();
            }
            catch (TimeoutException timeProblem)
            {
                SingletonLogger.Instance.Error("The service operation timed out. " + timeProblem.Message);
                proxy.Abort();
            }
            catch (FaultException unknownFault)
            {
                SingletonLogger.Instance.Error("An unknown exception was received. " + unknownFault.Message);
                proxy.Abort();
            }
            catch (CommunicationException commProblem)
            {
                SingletonLogger.Instance.Error("There was a communication problem. " + commProblem.Message + commProblem.StackTrace);
                proxy.Abort();
            }
        }

        public void StartWatchingForNewEdls(string editFolderIsilon, string subFolderIsilon, string highresfolderInterplay, string deviceName)
        {
            // do not use 'using' on WCF, more info at https://msdn.microsoft.com/en-us/library/ms733912.aspx
            EdlLogicProxy proxy = new EdlLogicProxy();
            try
            {
                proxy.StartWatchingForNewEdls(editFolderIsilon, subFolderIsilon, highresfolderInterplay, deviceName);
                proxy.Close();
            }
            catch (TimeoutException timeProblem)
            {
                SingletonLogger.Instance.Error("The service operation timed out. " + timeProblem.Message);
                proxy.Abort();
            }
            catch (FaultException unknownFault)
            {
                SingletonLogger.Instance.Error("An unknown exception was received. " + unknownFault.Message);
                proxy.Abort();
            }
            catch (CommunicationException commProblem)
            {
                SingletonLogger.Instance.Error("There was a communication problem. " + commProblem.Message + commProblem.StackTrace);
                proxy.Abort();
            }
        }
    }
}
