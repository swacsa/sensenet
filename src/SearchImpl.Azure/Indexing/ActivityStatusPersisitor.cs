using System;
using System.Collections.Generic;

namespace SenseNet.Search.Azure.Indexing
{
    public class ActivityStatusPersisitor : IActivityStatusPersisitor
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