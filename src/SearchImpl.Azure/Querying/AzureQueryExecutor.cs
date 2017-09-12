using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search;
using SenseNet.Search.Azure.Indexing.Models;
using SenseNet.Search.Azure.Querying.Models;

namespace SenseNet.Search.Azure.Querying
{
    public class AzureQueryExecutor //:IQueryExecutor
    {
        private PermissionChecker _permissionChecker;
        private AzureSearchParameters _queryParameters;
        public AzureSearchParameters QueryParameters => _queryParameters;
        public PermissionChecker PermissionChecker => _permissionChecker;

        private IIndexingQuery _queryEngine;


        public AzureQueryExecutor(IIndexingQuery queryEngine)
        {
            _queryEngine = queryEngine;
        }

        //public string QueryString

        //public int TotalCount

        public IEnumerable<IndexDocument> Execute()
        {
            var result = _queryEngine.GetDocuments(_queryParameters);
            var documents = result.Results.Select(r => r.Document);
            return null; //documents;
        }

        public void Initialize(AzureSearchParameters queryParameters, PermissionChecker permisionChecker)
        {
            _queryParameters = queryParameters;
            _permissionChecker = permisionChecker;
        }
    }
}