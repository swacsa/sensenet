using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Rest.Azure;
using Moq;
using SenseNet.Search;
using SenseNet.Search.Azure.Indexing;
using SenseNet.Search.Azure.Querying;
using Xunit;

namespace Sensenet.Search.Azure.Tests
{
    public class AzureIndexingEngineTests
    {
        [Fact]
        public void ActualizeTest()
        {
            var mockDocuments = new Mock<IDocumentsOperations>();
            var result = new AzureOperationResponse<DocumentSearchResult>();
            result.Body = new DocumentSearchResult();
            result.Body.Results = new List<SearchResult>();
            var document = new Document();
            document.Add("VersionId", "12");
            document.Add("Name", "ContentName");
            result.Body.Results.Add(new SearchResult {Document = document});
            var searchTask = Task.FromResult(result);
            mockDocuments.Setup(o => o.SearchWithHttpMessagesAsync(It.IsAny<string>(), It.IsAny<SearchParameters>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken))).Returns(searchTask);
            var indexResult = Task.FromResult(new AzureOperationResponse<DocumentIndexResult>());
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken))).Returns(indexResult);

           var queryEngine = new AzureQueryEngine(mockDocuments.Object);
            var indexingEngine = new AzureIndexingEngine(queryEngine, mockDocuments.Object);
            IEnumerable<SnTerm> deletables = new List<SnTerm>();
            IndexDocument addable = null;
            IEnumerable<DocumentUpdate> updateables = null;
            indexingEngine.Actualize(deletables, addable, updateables);
        }
    }
}