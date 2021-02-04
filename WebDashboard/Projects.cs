using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebDashboard
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Projects
    {
        public Projects()
        {
            ProjectsDictionary = new Dictionary<String, Project>();
        }

        public Dictionary<String, Project> ProjectsDictionary { get; set; }

        [JsonProperty]
        public List<Project> ProjectList
        {
            get
            {
                return new List<Project>(ProjectsDictionary.Values);
            }
        }
    }
}
