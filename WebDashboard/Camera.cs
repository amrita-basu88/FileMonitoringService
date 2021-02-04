using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebDashboard
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Camera
    {
        public Camera()
        {
            Recordings = new Dictionary<String, Recording>();
            CreateDate = DateTime.MinValue;
        }

        [JsonProperty]
        public String CameraName { get; set; }

        [JsonProperty]
        public DateTime CreateDate { get; set; }
        public Dictionary<String, Recording> Recordings { get; set; }

        [JsonProperty]
        public List<Recording> RecordingList
        {
            get
            {
                return new List<Recording>(Recordings.Values);
            }
        }
    }
}
