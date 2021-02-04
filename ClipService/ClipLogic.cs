using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using IClipService;
using LoggerSingleton;
using WMTClasses;
using System.Collections.Generic;
using Utilities;

namespace ClipService
{
    public class ClipLogic : IClipLogic
    {
        private static readonly string ClipsProcessingConsoleApp = ConfigurationManager.AppSettings["ClipsProcessingConsoleApp"];
        private static readonly String ClipProcessingXMlFile = ConfigurationManager.AppSettings["ClipStorePath"];
        private static readonly Int32 IntervalClipProcessing = Int32.Parse(ConfigurationManager.AppSettings["IntervalClipProcessing"]);

        private static ConcurrentDictionary<String, ClipData> ClipDataDictionary = new ConcurrentDictionary<String, ClipData>();
        private volatile bool _shouldStop;

        public void StartProcessingHighresClip(String highresFullClipName, String projectXmlFile, String amtTempFilesDirectory)
        {
            SingletonLogger.Instance.Info(String.Format("StartProcessingHighresClip highresFullClipName={0} projectXmlFile={1} amtTempFilesDirectory={2}",
                highresFullClipName, projectXmlFile, amtTempFilesDirectory ));

            if (!ClipDataDictionary.ContainsKey(highresFullClipName))
            {
                ClipData clipData = new ClipData
                {
                    LowresFullClipName = highresFullClipName,
                    ProjectXmlFile = projectXmlFile,
                    AmtTempFilesDirectory = amtTempFilesDirectory,
                    HighresClips = new List<String>()
                    
                };

                ClipDataDictionary.GetOrAdd(highresFullClipName, clipData);

                XmlHelper.WriteXmlFile(ClipDataDictionary.Values.ToList(), ClipProcessingXMlFile);
            }
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }

        public void ClipProcessing()
        {
            if (File.Exists(ClipProcessingXMlFile))
            {
                ClipDataList clipDataList = XmlHelper.ReadXmlFile<ClipDataList>(ClipProcessingXMlFile);
                if (clipDataList != null)
                {
                    foreach (var clipData in clipDataList)
                    {
                        ClipDataDictionary.GetOrAdd(clipData.LowresFullClipName, clipData);
                    }
                }
            }
            else
            {
                using (File.CreateText(ClipProcessingXMlFile))
                {
                }

                ClipDataList clipDataList = new ClipDataList();
                clipDataList.AddRange(ClipDataDictionary.Values.ToList());
                XmlHelper.WriteXmlFile(clipDataList, ClipProcessingXMlFile);
            }

            while (!_shouldStop)
            {
                try
                {
                    foreach (ClipData clipData in ClipDataDictionary.Values)
                    {
                        ProcesHighresClips(clipData);

                        if (_shouldStop)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(IntervalClipProcessing));
                }
                catch (Exception ex)
                {
                    SingletonLogger.Instance.Error(String.Format("Unhandled exception in ClipProcessing.{0}Exception={1}", Environment.NewLine, ex));
                }
            }
        }

        public void ProcesHighresClips(ClipData clipData)
        {
            if(clipData==null)
            {
                return;
            }
            SingletonLogger.Instance.Info(String.Format("start processing highres clip with lowres clipname='{0}'", clipData.LowresFullClipName));

            String clipsDirectory = Path.GetDirectoryName(clipData.LowresFullClipName);
            if (String.IsNullOrEmpty(clipsDirectory))
            {
                SingletonLogger.Instance.Error(String.Format("lowresFullClipName='{0}' has no directory", clipData.LowresFullClipName));
                return;
            }

            String camSerialDirectory = Path.GetDirectoryName(clipsDirectory);

            DirectoryInfo directoryInfo = new DirectoryInfo(clipsDirectory);
            String fileFilter = Path.GetFileName(clipData.LowresFullClipName.Replace("_", "*"));

            SingletonLogger.Instance.Info(String.Format("Start looking for highres full clips with filter={0}", fileFilter));
            if (!directoryInfo.Exists)
            {
                string lowresClipData = clipData.LowresFullClipName;
                ClipDataList clipDataList = XmlHelper.ReadXmlFile<ClipDataList>(ClipProcessingXMlFile);
                var allSubscriptions = clipDataList
                    .Where(x => x.LowresFullClipName.StartsWith(lowresClipData, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var subscription in allSubscriptions)
                {
                    clipDataList.Remove(subscription);
                }

                XmlHelper.WriteXmlFile(clipDataList, ClipProcessingXMlFile);
                SingletonLogger.Instance.Warn(String.Format("Could not find the Clips directory {0}", clipsDirectory)); 
                return;
            }
            FileInfo[] files = directoryInfo.GetFiles(fileFilter, SearchOption.TopDirectoryOnly);

            SingletonLogger.Instance.Info(String.Format("Found {0} files with filter={1}", files.Length, fileFilter));

            foreach (var file in files)
            {
                if ( clipData.HighresClips.Contains( file.FullName ) )
                {
                    SingletonLogger.Instance.Info(String.Format("highres clip={0} is already processed", file.FullName));
                    continue;
                }

                if (!FileHelper.FileExistsAndNotLocked(file.FullName))
                {
                    SingletonLogger.Instance.Info(String.Format("locked file={0}", file.FullName));
                    continue;
                }

                SingletonLogger.Instance.Info(String.Format("Start processing highres full clip {0}", file.FullName));

                String genArgs = String.Format("\"highres\" \"{0}\" \"{1}\" \"{2}\" \"{3} \"", file.FullName, camSerialDirectory, clipData.ProjectXmlFile, AppDomain.CurrentDomain.BaseDirectory);

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

                SingletonLogger.Instance.Info(String.Format("Processed processing highres full clip {0}", file.FullName));
                clipData.HighresClips.Add(file.FullName);
                XmlHelper.WriteXmlFile(ClipDataDictionary.Values.ToList(), ClipProcessingXMlFile);
            }
        }
    }
}
