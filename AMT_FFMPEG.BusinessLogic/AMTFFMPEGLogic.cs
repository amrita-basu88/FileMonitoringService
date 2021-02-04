using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using LoggerSingleton;
using U4A_WMT_AMT_FFMPEG_Wrapper;
using WMTInterplay;

namespace AMT_FFMPEG.BusinessLogic
{
    public class AMTFFMPEGLogic
    {
        private static readonly String AAFAvidInterplay = ConfigurationManager.AppSettings["AAFAvidInterplay"];

        public static void AMT_LoadPlugins()
        {
            SingletonLogger.Instance.Info("Before AMT_LoadPlugins");
            CAMTFF_Wrapper.AMT_LoadPlugins();
            SingletonLogger.Instance.Info("After AMT_LoadPlugins");
        }

        public static void AMT_UnLoadPlugins()
        {
            SingletonLogger.Instance.Info("Before AMT_UnLoadPlugins");
            CAMTFF_Wrapper.AMT_UnLoadPlugins();
            SingletonLogger.Instance.Info("After AMT_UnLoadPlugins");
        }

        private enum ErrorCode_t
        {
            AMT_FFMPEG_NO_ERROR = 0,
            AMT_FFMPEG_INIT_FAIL = 1,
            AMT_FFMPEG_CREATE_CLIP_FAIL = 2,
            AMT_FFMPEG_NO_STREAM_AVAILABLE = 3,
            AMT_FFMPEG_NO_VIDEO_STREAM_PRESENT = 4,
            AMT_FFMPEG_NO_AUDIO_STREAM_PRESENT = 5,
            AMT_FFMPEG_CREATE_VTRACK_FAIL = 6,
            AMT_FFMPEG_CREATE_ATRACK_FAIL = 7,
            AMT_FFMPEG_CLOSE_TRACKS_FAIL = 8,
            AMT_FFMPEG_CLOSE_CLIP_FAIL = 9,
            AMT_FFMPEG_OBJ_CLEANUP_FAIL = 10,
            AMT_FFMPEG_UNKNOWN = 256
        }

        private static CAMTFF_Wrapper camttf_Wrapper = null;

        public static void Initialize(ClipConfigsAMTAndFFMPEG clipConfigsAMTAndFFMPEG, String mxfOutput, String masterMobId, String sourceMobId, String isisPath, String uriAvidInterplay)
        {
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("uriAvidInterplay={0}", uriAvidInterplay));

                SingletonLogger.Instance.Debug("Begin Initialize CAMTFF_Wrapper");
            }

            if (camttf_Wrapper != null)
            {
                camttf_Wrapper.Dispose();
            }

            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("clipConfigsAMTAndFFMPEG.FFMpegConfig={0}", clipConfigsAMTAndFFMPEG.FFMpegConfig));
            }

            camttf_Wrapper = new CAMTFF_Wrapper();
            camttf_Wrapper.interplay_Clip = true;
            camttf_Wrapper.AMT_FFMPEG_SetFFMpegParams(clipConfigsAMTAndFFMPEG.FFMpegConfig);

            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug(String.Format("clipConfigsAMTAndFFMPEG.AMTConfigCompressed={0}", clipConfigsAMTAndFFMPEG.AMTConfigCompressed));
                SingletonLogger.Instance.Debug(String.Format("clipConfigsAMTAndFFMPEG.AMTConfigUncompressed={0}", clipConfigsAMTAndFFMPEG.AMTConfigUncompressed));

                SingletonLogger.Instance.Debug(String.Format("mxfOutput={0} masterMobId={1} sourceMobId={2} uriAvidInterplay={3} isisPath={4}", mxfOutput, masterMobId, sourceMobId, uriAvidInterplay, isisPath));

                SingletonLogger.Instance.Debug("Before AMT_FFMPEG_CreateInstance CAMTFF_Wrapper");
            }

            try
            {
                camttf_Wrapper.AMT_FFMPEG_CreateInstance(clipConfigsAMTAndFFMPEG.AMTConfigCompressed,
                                                          clipConfigsAMTAndFFMPEG.AMTConfigUncompressed,
                                                          mxfOutput,
                                                          masterMobId,
                                                          sourceMobId,
                                                          AvidInterplay.HostUrlAvidInterplay,
                                                          uriAvidInterplay,
                                                          AvidInterplay.UserAvidInterplay,
                                                          AvidInterplay.PasswordAvidInterplay,
                                                          isisPath,
                                                          AAFAvidInterplay,
                                                          false);
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in Process AMT_FFMPEG_CreateInstance.{0}Exception={1}", Environment.NewLine, ex));
            }

            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug("After AMT_FFMPEG_CreateInstance CAMTFF_Wrapper");
            }
        }

        public static void ProcessClip(String clipFilename)
        {
            // AMT and FFMPEG Initialization 
            ErrorCode_t errCode = ErrorCode_t.AMT_FFMPEG_UNKNOWN;
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug("Before AMT_FFMPEG_Init");
            }
            errCode = (ErrorCode_t)camttf_Wrapper.AMT_FFMPEG_Init(clipFilename);
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug("After AMT_FFMPEG_Init");
            }
            if (errCode != ErrorCode_t.AMT_FFMPEG_NO_ERROR)
            {
                SingletonLogger.Instance.Error(String.Format("-- AMT_FFMPEG_Init() Fail. ErrorCode={0}", errCode));
            }

            // Decoding
            SingletonLogger.Instance.Info("Start decoding");
            SingletonLogger.Instance.Info("Path=" + Path.GetDirectoryName(clipFilename));
            SingletonLogger.Instance.Info("File=" + Path.GetFileName(clipFilename));

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            errCode = (ErrorCode_t)camttf_Wrapper.AMT_FFMPEG_DecodeTS();
            stopwatch.Stop();
            TimeSpan t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            SingletonLogger.Instance.Info("Finished decoding");
            SingletonLogger.Instance.Info("Path=" + Path.GetDirectoryName(clipFilename));
            SingletonLogger.Instance.Info("File=" + Path.GetFileName(clipFilename));
            SingletonLogger.Instance.Info(String.Format("{3} Time={0:D2}m:{1:D2}s:{2:D2}ms", t.Minutes, t.Seconds, t.Milliseconds, Environment.NewLine));
            if (errCode != ErrorCode_t.AMT_FFMPEG_NO_ERROR)
            {
                SingletonLogger.Instance.Error(String.Format("-- AMT_FFMPEG_DecodeTS() Fail. ErrorCode={0}", errCode));
            }
        }

        public static String GetMasterModId()
        {
            if (camttf_Wrapper == null)
            {
                return null;
            }
            return camttf_Wrapper.master_MobId;
        }

        public static String GetSourceModId()
        {
            if (camttf_Wrapper == null)
            {
                return null;
            }
            return camttf_Wrapper.source_MobId;
        }

        public static void Close()
        {
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug("Before AMT_FFMPEG_CloseAMTContainer");
            }
            ErrorCode_t errCode = (ErrorCode_t)camttf_Wrapper.AMT_FFMPEG_CloseAMTContainer();
            if (SingletonLogger.Instance.IsDebugEnabled())
            {
                SingletonLogger.Instance.Debug("After AMT_FFMPEG_CloseAMTContainer");
            }
            if (errCode != ErrorCode_t.AMT_FFMPEG_NO_ERROR)
            {
                SingletonLogger.Instance.Error(String.Format("-- AMT_FFMPEG_CloseAMTContainer() Fail. ErrorCode={0}", errCode));
            }

            if (camttf_Wrapper != null)
            {
                camttf_Wrapper.Dispose();
            }
            camttf_Wrapper = null;
        }
    }
}
