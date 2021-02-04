namespace FileMonitorService.Models
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class FileIndexModel
    {
        [XmlElement("NetworkFile")]
        public List<NetworkFileModel> NetworkFiles { get; set; }

        public static FileIndexModel CreateFrom(IEnumerable<NetworkFile> files)
        {
            var model = new FileIndexModel
            {
                NetworkFiles = new List<NetworkFileModel>()
            };

            foreach (var file in files)
            {
                model.NetworkFiles.Add(NetworkFileModel.CreateFrom(file));
            }

            return model;
        }
    }
}
