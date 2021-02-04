using LoggerSingleton;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class FileHelper
    {
        private static readonly Int32 TimeoutWhenFileLocked = Int32.Parse(ConfigurationManager.AppSettings["TimeoutWhenFileLocked"]);

        public static Boolean FileExistsAndNotLocked(String filename)
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
    }
}
