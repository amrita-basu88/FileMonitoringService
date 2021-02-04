using System;
using System.Configuration;
using WMTConfig.Amt;
using WMTConfig.enums;
using WMTConfig.FFMpeg;

namespace AMT_FFMPEG.BusinessLogic
{
    public enum ClipConfigType
    {
        LowresConfig,
        HighresConfig
    }

    public class ClipConfigsAMTAndFFMPEG
    {
        // Important: SystemFormatType and VideoCompressionFamilyType should have same resolution
        private static readonly SystemFormatType SystemFormatType = (SystemFormatType)Enum.Parse(typeof(SystemFormatType), ConfigurationManager.AppSettings["SystemFormatType"] , true);
        public static readonly VideoCompressionFamilyType LowresVideoCompressionFamilyType = (VideoCompressionFamilyType)Enum.Parse(typeof(VideoCompressionFamilyType), ConfigurationManager.AppSettings["LowresVideoCompressionFamilyType"], true);
        private static readonly AudioCompressionFamilyType LowresAudioCompressionFamilyType = (AudioCompressionFamilyType)Enum.Parse(typeof(AudioCompressionFamilyType), ConfigurationManager.AppSettings["LowresAudioCompressionFamilyType"], true);
        public static readonly VideoCompressionFamilyType HighresVideoCompressionFamilyType = (VideoCompressionFamilyType)Enum.Parse(typeof(VideoCompressionFamilyType), ConfigurationManager.AppSettings["HighresVideoCompressionFamilyType"], true);
        private static readonly AudioCompressionFamilyType HighresAudioCompressionFamilyType = (AudioCompressionFamilyType)Enum.Parse(typeof(AudioCompressionFamilyType), ConfigurationManager.AppSettings["HighresAudioCompressionFamilyType"], true); 

        public String AMTConfigCompressed { get; private set; }
        public String AMTConfigUncompressed { get; private set; }
        public String FFMpegConfig { get; private set; }

        public ClipConfigsAMTAndFFMPEG(String sonyTimeCode, String tapeName, ClipConfigType clipConfigType)
        {
            AmtMediaAttributesClass a = new AmtMediaAttributesClass(VideoCompressionFamilyType.AvidUnknownCompressionType);
            a.InitUncompressed(SystemFormatType);
            AMTConfigUncompressed = a.getXDocument().ToString();

            VideoCompressionFamilyType videoCompressionFamilyType;
            AudioCompressionFamilyType audioCompressionFamilyType;

            switch (clipConfigType)
            {
                case ClipConfigType.LowresConfig:
                {
                    videoCompressionFamilyType = LowresVideoCompressionFamilyType;
                    audioCompressionFamilyType = LowresAudioCompressionFamilyType;
                }
                break;
                case ClipConfigType.HighresConfig:
                {
                    videoCompressionFamilyType = HighresVideoCompressionFamilyType;
                    audioCompressionFamilyType = HighresAudioCompressionFamilyType;
                }
                break;
                default:
                {
                    return;
                }
            }

            AmtMediaAttributesClass ua = new AmtMediaAttributesClass(videoCompressionFamilyType);
            ua.InitCompressed(audioCompressionFamilyType, tapeName);
            ua.timecode.SetTimecodeBySonyTimeCode(sonyTimeCode); 
            AMTConfigCompressed = ua.getXDocument().ToString();

            FFMpegMediaAttributesClass f = new FFMpegMediaAttributesClass();
            f.Init(SystemFormatType);
            FFMpegConfig = f.getXDocument().ToString();  
        }
    }
}
