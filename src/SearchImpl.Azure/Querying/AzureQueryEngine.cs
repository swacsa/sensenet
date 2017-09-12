using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Azure.Querying
{
    public class AzureQueryEngine: IIndexingQuery//IQueryEngine
    {
        private static IDocumentsOperations _documents;
        private static IQueryCompiler _compiler;

        public AzureQueryEngine(ISearchIndexClient client, IQueryCompiler compiler)
        {
            _documents = client.Documents;
            _compiler = compiler;
        }

        //public AzureQueryEngine(IDocumentsOperations documentsOperations)
        //{
        //    _documents = documentsOperations;
        //}

        #region Azure calls
        private Task<DocumentSearchResult> SearchAsync(out CancellationToken cancellationToken, AzureSearchParameters searchParameters = null)
        {
            cancellationToken = new CancellationToken();
            if (searchParameters == null)
            {
                searchParameters = new AzureSearchParameters();
            }
            return Task.Factory.StartNew(()=>Search(searchParameters), cancellationToken);
        }

        private DocumentSearchResult Search(AzureSearchParameters searchParameters)
        {
            return _documents.SearchWithHttpMessagesAsync(searchParameters.SearchText, (SearchParameters)searchParameters).Result.Body;
            //return _documents.Search(searchParameters.SearchText, (SearchParameters)searchParameters);
        }

        private Task<DocumentSearchResult> CountAsync(out CancellationToken cancellationToken, AzureSearchParameters searchParameters = null)
        {
            cancellationToken = new CancellationToken();
            if (searchParameters == null)
            {
                searchParameters = new AzureSearchParameters();
            }
            return Task.Factory.StartNew(() => Count(searchParameters), cancellationToken);
        }

        private DocumentSearchResult Count(AzureSearchParameters searchParameters)
        {
            searchParameters.IncludeTotalResultCount = true;
            searchParameters.Top = 0;
            return _documents.Search(searchParameters.SearchText, (SearchParameters)searchParameters);
        }

        #endregion

        #region IIndexingQuery

        public DocumentSearchResult GetDocuments(AzureSearchParameters searchParameters)
        {
            return Search(searchParameters);
        }
        #endregion

        #region IQueryEngine

        public IQueryResult<Document> ExecuteQuery(SnQuery query, IPermissionFilter filter)
        {
            IDictionary<string, IPerFieldIndexingInfo> indexingInfo = new Dictionary<string, IPerFieldIndexingInfo>();
            var queryContext = new QueryContext(QuerySettings.Default, 0, indexingInfo);
            var searchParameters = _compiler.Compile(query, queryContext);
            var result = Search(searchParameters);
            if (result != null)
            {
                return AzureQueryResult.Parse(result);
            }
            return null;
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}