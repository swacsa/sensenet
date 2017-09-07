using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using SenseNet.Search.Azure.Querying.Models;

namespace SenseNet.Search.Azure.Querying
{
    public class AzureQueryEngine: IQueryEngine
    {
        private static string _apiKey = "";
        private static string _schema = "https://";
        private static string _serviceName = "";
        private static string _indexName = "";
        private static string _dnsSuffix = "search.windows.net/indexes/";
        //private static string _apiVersion = "2016-09-01";
        private static int? _operationTimeout = 60;
        //private static int _top = 1000;

        private static SearchCredentials _credentials;
        private static ISearchIndexClient _indexClient;
        private static IDocumentsOperations _documents;
        //private Dictionary<string, List<string>> _customHeaders = null;

        public AzureQueryEngine()
        {
            if (_credentials == null)
            {
                _credentials = new SearchCredentials(_apiKey);
                _indexClient = new SearchIndexClient(_serviceName, _indexName, _credentials); 
                _indexClient.BaseUri = new Uri(_schema + _serviceName + "." + _dnsSuffix + _indexName);
                _indexClient.LongRunningOperationRetryTimeout = _operationTimeout;
                _documents = _indexClient.Documents;
            }
        }

        public AzureQueryEngine(IDocumentsOperations documentsOperations)
        {
            _documents = documentsOperations;
        }

        public Task<DocumentSearchResult> SearchAsync(out CancellationToken cancellationToken, AzureSearchParameters searchParameters = null)
        {
            cancellationToken = new CancellationToken();
            if (searchParameters == null)
            {
                searchParameters = new AzureSearchParameters();
            }
            return Task.Factory.StartNew(()=>Search(searchParameters), cancellationToken);
        }

        public DocumentSearchResult Search(AzureSearchParameters searchParameters)
        {
            return _documents.SearchWithHttpMessagesAsync(searchParameters.SearchText, (SearchParameters)searchParameters).Result.Body;
            //return _documents.Search(searchParameters.SearchText, (SearchParameters)searchParameters);
        }

        public Task<DocumentSearchResult> CountAsync(out CancellationToken cancellationToken, AzureSearchParameters searchParameters = null)
        {
            cancellationToken = new CancellationToken();
            if (searchParameters == null)
            {
                searchParameters = new AzureSearchParameters();
            }
            return Task.Factory.StartNew(() => Count(searchParameters), cancellationToken);
        }

        public DocumentSearchResult Count(AzureSearchParameters searchParameters)
        {
            searchParameters.IncludeTotalResultCount = true;
            searchParameters.Top = 0;
            return _documents.Search(searchParameters.SearchText, (SearchParameters)searchParameters);
        }

        public IQueryResult<int> ExecuteQuery(SnQuery query, IPermissionFilter filter)
        {
            throw new NotImplementedException();
        }

        public IQueryResult<string> ExecuteQueryAndProject(SnQuery query, IPermissionFilter filter)
        {
            throw new NotImplementedException();
        }
    }
}