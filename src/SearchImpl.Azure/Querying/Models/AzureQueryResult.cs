using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search.Models;

namespace SenseNet.Search.Azure.Querying.Models
{
    public class AzureQueryResult : IQueryResult<Document>
    {
        private IEnumerable<Document> _hits;
        private int _totalCount;

        public static AzureQueryResult Parse(DocumentSearchResult documents)
        {
            return new AzureQueryResult { Hits = documents.Results.Select(r => r.Document)};
        }
        public IEnumerable<Document> Hits
        {
            get{return _hits;}
            set
            {
                _totalCount = value.ToArray().Length;
                _hits = value;
            }
        }

        public int TotalCount => _totalCount;
    }
}