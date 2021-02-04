namespace FileMonitorService.Models
{
    using System;

    public class NetworkFileModel
    {
        public string Path { get; set; }
        public DateTime ModificationDate { get; set; }

        public static NetworkFileModel CreateFrom(NetworkFile file)
        {
            return new NetworkFileModel
            {
                Path = file.Path,
                ModificationDate = file.ModificationDate
            };
        }
    }
}
