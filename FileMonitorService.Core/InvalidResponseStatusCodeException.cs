namespace FileMonitorService.Core
{
    using System;

    public class InvalidResponseStatusCodeException : Exception
    {
        public int ResponseStatusCode { get; private set; }

        public InvalidResponseStatusCodeException(int responseStatusCode)
        {
            ResponseStatusCode = responseStatusCode;
        }

        public override string ToString()
        {
            return string.Format("The subscriber responded with HTTP status code: {0}.", ResponseStatusCode);
        }
    }
}
