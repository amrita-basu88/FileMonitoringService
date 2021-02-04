using System;
using System.Configuration;
using System.IO;
using System.Linq;
using AMT_FFMPEG.BusinessLogic;
using LoggerSingleton;
using WMTClasses;
using WMTInterplay;

namespace ClipWorkflow.BusinessLogic
{
    public class ClipHighresProcessingLogic
    {
        private static readonly String IsisPath = ConfigurationManager.AppSettings["IsisPath"];
        private static readonly String BasePathAvidInterplay = ConfigurationManager.AppSettings["BasePathAvidInterplay"];
        private static readonly String HighresResolution = ConfigurationManager.AppSettings["HighresVideoCompressionFamilyType"];

        public void StartProcessingHighresClips(String highresFullClipName, QRScan qrScan, String projectXmlFile, String amtTempFilesDirectory)
        {
            String clipNameMxfOutput = String.Empty;

            try
            {
                clipNameMxfOutput = new DirectoryInfo(highresFullClipName).Name.Split('_').First();
                clipNameMxfOutput = String.Format("{0}_{1}", clipNameMxfOutput, qrScan.WMTSerial);

                // Step 1: process first clip, read TSREC.XML for TimeCode  
                String finishedClipXmlFile = highresFullClipName.Replace(".TS", ".XML");
                NonRealTimeMeta nonRealTimeMeta = XmlHelper.ReadXmlFile<NonRealTimeMeta>(finishedClipXmlFile);
                String sonyTimeCode = nonRealTimeMeta.LtcChangeTable.LtcChange[0].startTcValueString;

                Int32 startframecount = Convert.ToInt32(nonRealTimeMeta.LtcChangeTable.LtcChange[0].startFrameCountString);
                if( startframecount > 0 )
                {
                    String newSonyTimeCode = RecalculateTimeCode( nonRealTimeMeta.LtcChangeTable.tcFps, sonyTimeCode, startframecount);
                    SingletonLogger.Instance.Info(String.Format("highresFullClipXml={0} oldTimeCode={1} newTimeCode={2}", finishedClipXmlFile, sonyTimeCode, newSonyTimeCode));
                    sonyTimeCode = newSonyTimeCode;
                }

                ClipConfigsAMTAndFFMPEG clipConfigsAMTAndFFMPEG = Create_AMT_FFMPEG_XML_Configs(sonyTimeCode,
                    qrScan.WMTSerial, qrScan.CamSerial, ClipConfigType.HighresConfig);

                AvidInterplay interplay = new AvidInterplay(AvidInterplay.UserAvidInterplay, AvidInterplay.PasswordAvidInterplay, AvidInterplay.BaseUriAvidInterplay, AvidInterplay.HostUrlAvidInterplay);
                interplay.FillParameters(qrScan, BasePathAvidInterplay);
                String interplayMaterialFolder = interplay.LoadedMaterialHighresFolder;

                AMTFFMPEGLogic.Initialize(clipConfigsAMTAndFFMPEG, clipNameMxfOutput, String.Empty, String.Empty,
                    IsisPath, interplayMaterialFolder);

                // Step 2: read Frames with FFMPEG ( External code/program )
                // Step 3: Schrijf Frames naar AMT clip ( External code/program )
                AMTFFMPEGLogic.ProcessClip(highresFullClipName);

                // Step 1: Check this in in Interplay, returns mob-ID ( sort of string Guid) 
                // master en source mob uit amt stuk
                String masterModId = AMTFFMPEGLogic.GetMasterModId();
                String sourceModId = AMTFFMPEGLogic.GetSourceModId();

                // Step 2: close AMT 
                AMTFFMPEGLogic.Close();

                // Step 3: Choice of right WORKSPACE ?? #CUSTOMER#_#PROGRAM#

                // Step 4: Write MOB-ID in file W000X_DATE_TC.XML ( we can define this in xml file )
                nonRealTimeMeta.MobId = new MobId {Master = masterModId, Source = sourceModId};
                XmlHelper.WriteXmlFile(nonRealTimeMeta, finishedClipXmlFile);

                // Step 5: Update PROJECT.XML file with info from previous steps above
                Project project = XmlHelper.ReadXmlFile<Project>(projectXmlFile);
                project.WORKSPACE.RESOLUTION.HIGHRES = HighresResolution;
                XmlHelper.WriteXmlFile(project, projectXmlFile);

            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in Process StartProcessingHighresClips={0}{1}.Exception={2}", highresFullClipName, Environment.NewLine, ex));
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
                    else
                    {
                        SingletonLogger.Instance.Warn(String.Format("AMTTemp to remove does not exist directory='{0}'", amtTempClipDirectory));
                    }
                }
            }
        }

        private String RecalculateTimeCode( Int32 fps, String sonyTimeCode, Int32 startframecount)
        {
            Int32 seconds = Convert.ToInt32(sonyTimeCode.Substring(2, 2));
            Int32 minutes = Convert.ToInt32(sonyTimeCode.Substring(4, 2));
            Int32 hours = Convert.ToInt32(sonyTimeCode.Substring(6, 2));
            Int32 frames = Convert.ToInt32(sonyTimeCode.Substring(0, 2));

            Int64 totalnewFrames = (seconds + minutes * 60 + hours * 3600) * fps + frames + startframecount;
            Int64 newframes = totalnewFrames % fps;
            Int64 totalseconds = (totalnewFrames - newframes) / fps;

            Int64 newhours = totalseconds / 3600;
            Int64 newminutes = (totalseconds - newhours * 3600) / 60;
            Int64 newseconds = (totalseconds - newhours * 3600) - (newminutes * 60);
            return String.Format("{0:D2}{1:D2}{2:D2}{3:D2}", newframes, newseconds, newminutes, newhours);
        }

        private ClipConfigsAMTAndFFMPEG Create_AMT_FFMPEG_XML_Configs(String sonyTimeCode, String wmtSerial, String camSerial, ClipConfigType clipConfigType)
        {
            ClipConfigsAMTAndFFMPEG clipConfigsAMTAndFFMPEG = null;

            String tapeName = String.Format("WMT{0}_{1}", wmtSerial, camSerial);

            clipConfigsAMTAndFFMPEG = new ClipConfigsAMTAndFFMPEG(sonyTimeCode, tapeName, clipConfigType);
            return clipConfigsAMTAndFFMPEG;
        }
    }
}
