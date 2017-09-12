using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Rest.Azure;
using Moq;
using SenseNet.Search;
using SenseNet.Search.Azure.Querying;
using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Parser;
using Xunit;

namespace Sensenet.Search.Azure.Tests
{
    public class AzureQueryEngineTests
    {
        //private static string _apiKey = "apikey";
        //private static string _schema = "https://";
        //private static string _serviceName = "servicename";
        //private static string _indexName = "indexname";
        //private static string _dnsSuffix = "search.windows.net/indexes/";
        //private static int? _operationTimeout = 60;
        ////private static int _top = 1000;

        //private static SearchCredentials _credentials;
        //private static ISearchIndexClient _indexClient;
        //private static IDocumentsOperations _documents;
        //private Dictionary<string, List<string>> _customHeaders = null;

        


        [Fact]
        public void ExecuteQueryTest()
        {
            //_credentials = new SearchCredentials(_apiKey);
            //_indexClient = new SearchIndexClient(_serviceName, _indexName, _credentials);
            //_indexClient.BaseUri = new Uri(_schema + _serviceName + "." + _dnsSuffix + _indexName);
            //_indexClient.LongRunningOperationRetryTimeout = _operationTimeout;

            string text;
            var parameters = new SearchParameters();
            var mockContext = new Mock<IQueryContext>();
            var mockDocuments = new Mock<IDocumentsOperations>();
            var searchResult = new AzureOperationResponse<DocumentSearchResult>();
            searchResult.Body = new DocumentSearchResult();
            searchResult.Body.Results = new List<SearchResult>();
            var document = new Document();
            document.Add("VersionId", "12");
            document.Add("Name", "ContentName");
            searchResult.Body.Results.Add(new SearchResult { Document = document });
            var searchTask = Task.FromResult(searchResult);
            mockDocuments.Setup(o => o.SearchWithHttpMessagesAsync(It.IsAny<string>(), It.IsAny<SearchParameters>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)))
                .Returns(searchTask).Callback((string st, SearchParameters p, SearchRequestOptions o, Dictionary<string, List<string>> c, CancellationToken t) =>
                {
                    text = st;
                    parameters = p;
                });
            var mockClient = new Mock<ISearchIndexClient>();
            mockClient.SetupGet(o => o.Documents).Returns(mockDocuments.Object);

            QuerySettings settings = null;
            mockContext.SetupGet(o => o.Settings).Returns(settings);
            mockContext.SetupGet(o => o.UserId).Returns(1);
            mockContext.Setup(o => o.GetPerFieldIndexingInfo(It.IsAny<string>())).Returns((IPerFieldIndexingInfo)null);
            IPermissionFilterFactory permissionFilterFactory = new DefaultPermissionFilterFactory();
            var permissionFilter = permissionFilterFactory.Create(mockContext.Object.UserId);
            var mockCompiler = new Mock<IQueryCompiler>();
            var searchParameters = new AzureSearchParameters();
            mockCompiler.Setup(o => o.Compile(It.IsAny<SnQuery>(), It.IsAny<IQueryContext>())).Returns(searchParameters);
            //IQueryEngine 
            var engine = new AzureQueryEngine(mockClient.Object, mockCompiler.Object);
            SnQuery query = new SnQuery();
            query.Querytext = "";

            var result = engine.ExecuteQuery(query, permissionFilter);

            Assert.Equal(1,result.Hits.Count());
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(null, parameters.Filter);
            Assert.Equal(false, parameters.IncludeTotalResultCount);
            Assert.Equal(QueryType.Full, parameters.QueryType);
            Assert.Equal(SearchMode.All, parameters.SearchMode);
            Assert.Equal(null, parameters.Skip);
            Assert.Equal(1000, parameters.Top);
        }

    }
}