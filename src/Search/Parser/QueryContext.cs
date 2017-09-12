using System;
using System.Collections.Generic;

namespace SenseNet.Search.Parser
{
    public class QueryContext: IQueryContext
    {
        private IDictionary<string, IPerFieldIndexingInfo> _indexingInfo;

        public QuerySettings Settings { get; }
        public int UserId { get; }

        public IQueryEngine QueryEngine
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName)
        {
            return _indexingInfo[fieldName];
        }

        public QueryContext(QuerySettings settings, int userId, IDictionary<string, IPerFieldIndexingInfo> indexingInfo)
        {
            Settings = settings;
            UserId = userId;
            _indexingInfo = indexingInfo;
        }
    }
}