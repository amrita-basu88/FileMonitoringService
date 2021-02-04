using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebDashboard
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Project
    {
        public Project()
        {
            Cameras = new Dictionary<String, Camera>();
        }

        [JsonProperty]
        public String ProjectName { get; set; }

        [JsonProperty]
        public DateTime CreateDate { get; set; }
        public Dictionary<String, Camera> Cameras { get; set; }

        [JsonProperty]
        public List<Camera> CamerasList 
        {
            get
            {
                return new List<Camera>(Cameras.Values);
            }
        }
    }
}
