using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using IEdlService;
using LoggerSingleton;
using WMTClasses;
using WMTInterplay;
using System.Runtime.InteropServices;
using Utilities;

namespace EdlService
{
    public class EdlLogic : IEdlLogic
    {
        private static readonly String BasePathAvidInterplay = ConfigurationManager.AppSettings["BasePathAvidInterplay"];
        private static readonly String EdlProcessingXMlFile = ConfigurationManager.AppSettings["EdlStorePath"];
        private static readonly Int32 IntervalEdlProcessing = Int32.Parse(ConfigurationManager.AppSettings["IntervalEdlProcessing"]);
        private static ConcurrentDictionary<String, EdlData> EdlDataDictionary = new ConcurrentDictionary<String, EdlData>();
        private volatile bool _shouldStop;

        public void StartWatchingForNewEdls(String editFolderIsilon, String subFolderIsilon, String highresfolderInterplay, String deviceName)
        {
            SingletonLogger.Instance.Info(String.Format("StartWatchingForNewEdls editFolderIsilon={0} subFolderIsilon={1} highresfolderInterplay={2} deviceName={3}",
                editFolderIsilon, subFolderIsilon, highresfolderInterplay, deviceName));

            if (!EdlDataDictionary.ContainsKey(highresfolderInterplay))
            {
                EdlData edlData = new EdlData
                {
                    EditFolderIsilon = editFolderIsilon,
                    SubFolderIsilon = subFolderIsilon,
                    HighresfolderInterplay = highresfolderInterplay,
                    DeviceName = deviceName
                };

                EdlDataDictionary.GetOrAdd(highresfolderInterplay, edlData);

                XmlHelper.WriteXmlFile(EdlDataDictionary.Values.ToList(), EdlProcessingXMlFile);
            }
        }

        public void RequestStop()
        {
            _shouldStop = true;
        }

        public void EdlProcessing()
        {
            if (File.Exists(EdlProcessingXMlFile))
            {
                EdlDataList edlDataList = XmlHelper.ReadXmlFile<EdlDataList>(EdlProcessingXMlFile);
                if (edlDataList != null)
                {
                    foreach (var edlData in edlDataList)
                    {
                        EdlDataDictionary.GetOrAdd(edlData.HighresfolderInterplay, edlData);
                    }
                }
            }
            else
            {
                using (File.CreateText(EdlProcessingXMlFile))
                {
                }

                EdlDataList edlDataList = new EdlDataList();
                edlDataList.AddRange(EdlDataDictionary.Values.ToList());
                XmlHelper.WriteXmlFile(edlDataList, EdlProcessingXMlFile);
            }

            while (!_shouldStop)
            {
                try
                {
                    foreach (EdlData edlData in EdlDataDictionary.Values)
                    {
                        EdlWatcher(edlData);

                        if (_shouldStop)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(IntervalEdlProcessing));
                }
                catch (Exception ex)
                {
                    SingletonLogger.Instance.Error(String.Format("Unhandled exception in EdlProcessing.{0}Exception={1}", Environment.NewLine, ex));
                }
            }
        }

        public void EdlWatcher(EdlData edldata)
        {
            try
            {
                SingletonLogger.Instance.Trace(String.Format("Starting EdlWatcher loop with HighresfolderInterplay={0} SubFolderIsilon={1} EditFolderIsilon={2} DeviceName={3}",
                    edldata.HighresfolderInterplay, edldata.SubFolderIsilon, edldata.EditFolderIsilon, edldata.DeviceName));

                if (!Directory.Exists(edldata.SubFolderIsilon))
                {
                    SingletonLogger.Instance.Error(String.Format("SubFolderIsilon does not exist. Name:{0}", edldata.SubFolderIsilon));
                    return;
                }

                if (!Directory.Exists(edldata.EditFolderIsilon))
                {
                    SingletonLogger.Instance.Error(String.Format("EditFolderIsilon does not exist. Name:{0}", edldata.EditFolderIsilon));
                    return;
                }

                String camSerialDirectory = Path.GetDirectoryName(edldata.SubFolderIsilon);

                // set in EdlData
                QRScan qrScan = ReadQrCodeFile(camSerialDirectory);
                if (qrScan == null)
                {
                    SingletonLogger.Instance.Error(String.Format("QRCODE.XML is locked or does not exists in directory:{0}.", camSerialDirectory));
                    return;
                }

                AvidInterplay interplay = new AvidInterplay(AvidInterplay.UserAvidInterplay, AvidInterplay.PasswordAvidInterplay, AvidInterplay.BaseUriAvidInterplay, AvidInterplay.HostUrlAvidInterplay);
                StatusValue status = interplay.GetStatus();
                if (status.Error)
                {
                    SingletonLogger.Instance.Error(String.Format("interplay constructor has errorstatus:{0}", status.Status));
                    return;
                }

                interplay.FillParameters(qrScan, BasePathAvidInterplay);
                status = interplay.GetStatus();
                if (status.Error)
                {
                    SingletonLogger.Instance.Error(String.Format("FillParameters has errorstatus:{0}", status.Status));
                    return;
                }
                List<String> mobIds = interplay.GetFetchHighresSequences();
                status = interplay.GetStatus();
                if (status.Error)
                {
                    SingletonLogger.Instance.Error(String.Format("GetFetchHighresSequences has errorstatus:{0}", status.Status));
                    return;
                }

                if (mobIds == null)
                {
                    SingletonLogger.Instance.Warn(String.Format("null list of mobids. HighresFolder:{0}", edldata.HighresfolderInterplay));
                    return;
                }

                SingletonLogger.Instance.Debug(String.Format("Got {0} mobIds from GetFetchHighresSequences", mobIds.Count));

                Dictionary<String, String> masterModIdClipNameMapping = GetMasterMobIdClipNameMapping(edldata.SubFolderIsilon);
                Int32 edlNumber = 1;
                foreach (var mobId in mobIds)
                {
                    bool isEdlCreated = interplay.CheckEdlCreated(mobId, edldata.DeviceName);

                    status = interplay.GetStatus();
                    if (status.Error)
                    {
                        SingletonLogger.Instance.Error(String.Format("CheckEdlCreated has errorstatus:{0}", status.Status));
                        return;
                    }

                    if (isEdlCreated)
                    {
                        SingletonLogger.Instance.Debug(String.Format("EDL is already created, Status:{0}", status.Status));
                        continue;
                    }

                    EdlMeta edlMeta = new EdlMeta
                    {
                        Edls = interplay.GetEdlFromSequence(mobId)
                    };

                    status = interplay.GetStatus();
                    if (status.Error)
                    {
                        SingletonLogger.Instance.Error(String.Format("GetEdlFromSequence has errorstatus:{0}", status.Status));
                        continue;
                    }


                    SingletonLogger.Instance.Trace(String.Format("Starting Converting mobId to clipname"));

                    // Convert mobid to clipname
                    List<Edl> edlsToRemove = new List<Edl>();
                    foreach (var edlItem in edlMeta.Edls)
                    {
                        String masterMobId = edlItem.ClipName;
                        if (masterModIdClipNameMapping.ContainsKey(masterMobId))
                        {
                            edlItem.ClipName = masterModIdClipNameMapping[masterMobId];

                            SingletonLogger.Instance.Debug(String.Format("Found Clipname={0} for mobId:{1}", masterModIdClipNameMapping[masterMobId], mobId));
                        }
                        else
                        {
                            edlsToRemove.Add(edlItem);
                            SingletonLogger.Instance.Debug(String.Format("No Clipname found for mobId:{0}", mobId));
                        }
                    }

                    foreach (var edl in edlsToRemove)
                    {
                        edlMeta.Edls.Remove(edl);
                    }

                    if (edlMeta.Edls.Count > 0)
                    {
                        String edlMetaFile = Path.Combine(edldata.EditFolderIsilon, String.Format("{0}-{1:000}.EDL", edldata.DeviceName, edlNumber++));
                        SingletonLogger.Instance.Debug(String.Format("WriteXmlFile:{0}", edlMetaFile));
                        XmlHelper.WriteXmlFile(edlMeta, edlMetaFile, edlMeta.CustomNameSpace);
                    }
                }
            }
            catch (Exception ex)
            {
                SingletonLogger.Instance.Error(String.Format("Unhandled exception in EdlWatcher.{0}Exception:{1}", Environment.NewLine, ex));
            }

            SingletonLogger.Instance.Trace(String.Format("Finished EdlWatcher loop"));
        }

        private QRScan ReadQrCodeFile(String camSerialDirectory)
        {
            String qrcodeXml = Path.Combine(camSerialDirectory, "QRCODE.XML");
            if (!FileHelper.FileExistsAndNotLocked(qrcodeXml))
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

        private static Dictionary<String, String> GetMasterMobIdClipNameMapping(string subFolder)
        {
            Dictionary<String, String> masterModIdClipNameMapping = new Dictionary<String, String>();
            DirectoryInfo directoryInfo = new DirectoryInfo(subFolder);
            FileInfo[] files = directoryInfo.GetFiles("W*.XML", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    if (!File.Exists(file.FullName))
                    {
                        SingletonLogger.Instance.Error(String.Format("Exception when reading file:{0}.", file.FullName));
                        continue;
                    }

                    NonRealTimeMeta nonRealTimeMeta = XmlHelper.ReadXmlFile<NonRealTimeMeta>(file.FullName);
                    if (nonRealTimeMeta.MobId != null && nonRealTimeMeta.MobId.Master != null)
                    {
                        if (!masterModIdClipNameMapping.ContainsKey(nonRealTimeMeta.MobId.Master))
                        {
                            masterModIdClipNameMapping.Add(nonRealTimeMeta.MobId.Master,
                                nonRealTimeMeta.TargetMaterial.ClipId.StartsWith("W") ?
                                nonRealTimeMeta.TargetMaterial.ClipId
                                : "W" + nonRealTimeMeta.TargetMaterial.ClipId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SingletonLogger.Instance.Error(String.Format("Exception when reading file:{0}.{1}Exception={2}",
                        file.FullName, Environment.NewLine, ex));
                }
            }

            return masterModIdClipNameMapping;
        }
    }
}
