using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using FileMonitorService.Models;
using LoggerSingleton;

namespace FileMonitorService.Core
{
    public class StorageRepository : IStorageRepository
    {
        public IEnumerable<NetworkFile> GetFiles(string path, bool isRecursive, bool isWatchingDirectories, bool isWatchingFiles)
        {
            var networkFiles = new List<NetworkFile>();


            var directoryInfo = new DirectoryInfo(path);

            FileInfo[] files = null;
            if (isWatchingFiles)
            {
                files = directoryInfo.GetFiles("*",
                    isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            DirectoryInfo[] directories = null;
            if (isWatchingDirectories)
            {
                directories = directoryInfo.GetDirectories("*",
                    isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            if (files != null)
            {
                foreach (var file in files)
                {
                    var isLocked = true;
                    try
                    {
                        using (file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                        }

                        isLocked = false;
                    }
                    catch (IOException e)
                    {
                        SingletonLogger.Instance.Info(String.Format("File is locked {0}", file.FullName));

                        var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                        isLocked = errorCode == 32 || errorCode == 33;

                    }

                    if (!isLocked)
                    {
                        networkFiles.Add(new NetworkFile
                        {
                            Path = file.FullName,
                            ModificationDate = file.LastWriteTime
                        });
                    }
                }
            }

            if (directories != null)
            {
                foreach (var directory in directories)
                {
                    networkFiles.Add(new NetworkFile
                    {
                        Path = directory.FullName,
                        ModificationDate = directory.LastWriteTime
                    });
                }
            }

            networkFiles.Sort((x, y) => DateToMilliseconds(x.ModificationDate).CompareTo(DateToMilliseconds(y.ModificationDate)));
            return networkFiles;
        }

        private long DateToMilliseconds(DateTime datetime)
        {
            return (long)(datetime - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
}
