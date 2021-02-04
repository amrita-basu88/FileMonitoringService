using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebDashboard
{
    public enum RecordingStateEnum
    {
       Active,
       Completed,
    }

    public class Recording 
    {
        public String RecordingName { get; set; }
        public DateTime CreateDate { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecordingStateEnum RecordingState { get; set; }
    }
}
