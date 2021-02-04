namespace FileMonitorService.Models
{
    using System.Collections.Generic;

    public interface IStorageRepository
    {
        IEnumerable<NetworkFile> GetFiles(string path, bool isRecursive, bool isWatchingDirectories, bool isWatchingFiles);
    }
}
