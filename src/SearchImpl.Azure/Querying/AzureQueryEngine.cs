using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Azure.Querying
{
    public class AzureQueryEngine: IIndexingQuery, IQueryEngine
    {
        private static IDocumentsOperations _documents;
        private static IQueryCompiler _compiler;

        public AzureQueryEngine(ISearchIndexClient client, IQueryCompiler compiler)
        {
            _documents = client.Documents;
            _compiler = compiler;
        }

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

        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            var searchParameters = _compiler.Compile(query, context);
            var result = Search(searchParameters);
            if (result != null)
            {
                return new QueryResult<int>(result.Results.Select(r =>
                {
                    object value;
                    r.Document.TryGetValue(IndexFieldName.VersionId, out value);
                    return int.Parse(value.ToString());
                }), result.Results.Count);
            }
            return null;
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter, IQueryContext context)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}