using System;
using System.Collections.Generic;

namespace SenseNet.Search.Azure.Indexing
{
    public class ActivityStatusPersistor : IActivityStatusPersistor
    {
        public IIndexingActivityStatus GetStatus()
        {
            throw new NotImplementedException();
        }

        public void PutStatus(IIndexingActivityStatus status)
        {
            throw new NotImplementedException();
        }
    }
}