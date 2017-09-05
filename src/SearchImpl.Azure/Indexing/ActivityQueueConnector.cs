using System.Collections.Generic;

namespace SenseNet.Search.Azure.Indexing
{
    public class ActivityQueueConnector: IActivityQueueConnector
    {
        public IDictionary<string, string> GetCompletionInfo()
        {
            return new Dictionary<string, string>();
        }
    }
}