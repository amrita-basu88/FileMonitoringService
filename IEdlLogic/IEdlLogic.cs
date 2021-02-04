using System;
using System.ServiceModel;

namespace IEdlService
{
    [ServiceContract]
    public interface IEdlLogic
    {
        [OperationContract]
        void StartWatchingForNewEdls(String editFolderIsilon, String subFolderIsilon, String highresfolderInterplay, String deviceName);
    }
}
