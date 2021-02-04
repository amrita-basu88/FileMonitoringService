using System;
using System.ServiceModel;

namespace IClipService
{
    [ServiceContract]
    public interface IClipLogic
    {
        [OperationContract]
        void StartProcessingHighresClip(String highresFullClipName, String projectXmlFile, String amtTempFilesDirectory);
    }
}


