using System;
using System.Collections.Generic;

namespace ClipService
{
    public class ClipData
    {
        public ClipData()
        {
            HighresClips = new List<String>();
        }

        public String LowresFullClipName { get; set; }
        public String ProjectXmlFile { get; set; }
        public String AmtTempFilesDirectory { get; set; }

        public List<String> HighresClips { get; set; }
    }
}
