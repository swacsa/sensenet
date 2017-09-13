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
using SenseNet.Search.Azure.Indexing;
using SenseNet.Search.Azure.Querying;
using SenseNet.Search.Azure.Querying.Models;
using Xunit;

namespace Sensenet.Search.Azure.Tests
{
    public class AzureIndexingEngineTests
    {

        [Fact]
        public void WriteIndexTest()
        {
            string text= null;
            string searchText = null;
            var  batch = default(IndexBatch<IndexDocument>);
            SearchParameters parameters = new SearchParameters();
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
            var indexResult = new AzureOperationResponse<DocumentIndexResult>();
            var indexingResults = new List<IndexingResult>();
            indexingResults.Add(new IndexingResult("12", null, true));
            indexingResults.Add(new IndexingResult("13", null, true));
            indexingResults.Add(new IndexingResult("0", null, true));
            indexingResults.Add(new IndexingResult("1", null, true));
            indexingResults.Add(new IndexingResult("2", null, true));
            indexResult.Body = new DocumentIndexResult(indexingResults);
            var indexTask = Task.FromResult(indexResult);
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken))).Returns(indexTask)
                .Callback((IndexBatch<IndexDocument> b, SearchRequestOptions o, Dictionary<string, List<string>> c,CancellationToken t) =>
                {
                    batch = b;
                });

            //var queryEngine = new AzureQueryEngine(mockDocuments.Object);
            var mockQueryEngine = new Mock<IIndexingQuery>();
            mockQueryEngine.Setup(o => o.GetDocuments(It.IsAny<AzureSearchParameters>())).Returns(searchResult.Body).Callback((AzureSearchParameters p) =>
            {
                searchText = p.SearchText;
                parameters.Filter = p.Filter;
                parameters.IncludeTotalResultCount = p.IncludeTotalResultCount;
                parameters.Skip = p.Skip;
                parameters.Top = p.Top;
            });
            var indexingEngine = new AzureIndexingEngine(mockQueryEngine.Object, mockDocuments.Object);
            var deletions = new List<SnTerm>();
            deletions.Add(new SnTerm("VersionId", "12"));
            var addable = new IndexDocument();
            addable.Add(new IndexField("VersionId", int.Parse("13"), IndexingMode.NotAnalyzed,IndexStoringMode.Yes, IndexTermVector.Default));
            addable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            var updateables = new List<DocumentUpdate>();
            for (int i = 0; i < 3; i++)
            {
                var updateable = new IndexDocument();
                updateable.Add(new IndexField("VersionId", i, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateables.Add(new DocumentUpdate { Document = updateable, UpdateTerm = new SnTerm("", "") });
            }
            indexingEngine.WriteIndex(deletions, addable, updateables);

            mockQueryEngine.Verify(o => o.GetDocuments(It.IsAny<AzureSearchParameters>()), Times.Once);
            mockDocuments.Verify(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)), Times.Once);
            Assert.Equal(1, batch.Actions.Count(a => a.ActionType == IndexActionType.Delete));
            Assert.Equal(3, batch.Actions.Count(a => a.ActionType == IndexActionType.Merge));
            Assert.Equal(1, batch.Actions.Count(a => a.ActionType == IndexActionType.MergeOrUpload));
            Assert.Equal("VersionId:12", searchText);
            Assert.Equal("", parameters.Filter);
            Assert.Equal(false, parameters.IncludeTotalResultCount);
            Assert.Equal(null, parameters.Skip);
            Assert.Equal(null, parameters.Top);
        }

        [Fact]
        public void WriteIndexWithStringArrayTermTest()
        {
            string filter = null;
            string searchText = "";
            var batch = default(IndexBatch<IndexDocument>);
            SearchParameters parameters = new SearchParameters();
            var mockDocuments = new Mock<IDocumentsOperations>();
            var searchResult = new AzureOperationResponse<DocumentSearchResult>();
            searchResult.Body = new DocumentSearchResult();
            searchResult.Body.Results = new List<SearchResult>();
            var document = new Document();
            document.Add("VersionId", "12");
            document.Add("Name", "ContentName");
            searchResult.Body.Results.Add(new SearchResult { Document = document });

            var indexResult = new AzureOperationResponse<DocumentIndexResult>();
            var indexingResults = new List<IndexingResult>();
            indexingResults.Add(new IndexingResult("12", null, true));
            indexingResults.Add(new IndexingResult("13", null, true));
            indexingResults.Add(new IndexingResult("0", null, true));
            indexingResults.Add(new IndexingResult("1", null, true));
            indexingResults.Add(new IndexingResult("2", null, true));
            indexResult.Body = new DocumentIndexResult(indexingResults);
            var indexTask = Task.FromResult(indexResult);
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken))).Returns(indexTask)
                .Callback((IndexBatch<IndexDocument> b, SearchRequestOptions o, Dictionary<string, List<string>> c, CancellationToken t) =>
                 {
                     batch = b;
                 });

            var mockQueryEngine = new Mock<IIndexingQuery>();
            mockQueryEngine.Setup(o => o.GetDocuments(It.IsAny<AzureSearchParameters>())).Returns(searchResult.Body).Callback((AzureSearchParameters p) =>
            {
                filter = p.Filter;
                searchText = p.SearchText;
                parameters.IncludeTotalResultCount = p.IncludeTotalResultCount; 
                parameters.Skip = p.Skip;
                parameters.Top = p.Top;
            });
            var indexingEngine = new AzureIndexingEngine(mockQueryEngine.Object, mockDocuments.Object);
            var deletions = new List<SnTerm>();
            deletions.Add(new SnTerm("VersionId", new [] {"2", "12", "5" }));
            var addable = new IndexDocument();
            addable.Add(new IndexField("VersionId", int.Parse("13"), IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            addable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            var updateables = new List<DocumentUpdate>();
            for (int i = 0; i < 3; i++)
            {
                var updateable = new IndexDocument();
                updateable.Add(new IndexField("VersionId", i, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateables.Add(new DocumentUpdate { Document = updateable, UpdateTerm = new SnTerm("", "") });
            }
            indexingEngine.WriteIndex(deletions, addable, updateables);

            mockQueryEngine.Verify(o => o.GetDocuments(It.IsAny<AzureSearchParameters>()), Times.Once);
            mockDocuments.Verify(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)), Times.Once);
            Assert.Equal(1, batch.Actions.Count(a => a.ActionType == IndexActionType.Delete));
            Assert.Equal(3, batch.Actions.Count(a => a.ActionType == IndexActionType.Merge));
            Assert.Equal(1, batch.Actions.Count(a => a.ActionType == IndexActionType.MergeOrUpload));
            Assert.Equal("search.in(VersionId,'2,12,5')", filter);
            Assert.Equal("", searchText);
            Assert.Equal(false, parameters.IncludeTotalResultCount);
            Assert.Equal(null, parameters.Skip);
            Assert.Equal(null, parameters.Top);
        }

        [Fact]
        public void WriteIndexWithoutActionsTest()
        {
            string text = null;
            string searchText = null;
            var batch = default(IndexBatch<IndexDocument>);
            SearchParameters parameters = new SearchParameters();
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
            var indexResult = new AzureOperationResponse<DocumentIndexResult>();
            var indexingResults = new List<IndexingResult>();
            indexingResults.Add(new IndexingResult("12", null, true));
            indexingResults.Add(new IndexingResult("13", null, true));
            indexingResults.Add(new IndexingResult("0", null, true));
            indexingResults.Add(new IndexingResult("1", null, true));
            indexingResults.Add(new IndexingResult("2", null, true));
            indexResult.Body = new DocumentIndexResult(indexingResults);
            var indexTask = Task.FromResult(indexResult);
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken))).Returns(indexTask)
                .Callback((IndexBatch<IndexDocument> b, SearchRequestOptions o, Dictionary<string, List<string>> c, CancellationToken t) =>
                {
                    batch = b;
                });

            //var queryEngine = new AzureQueryEngine(mockDocuments.Object);
            var mockQueryEngine = new Mock<IIndexingQuery>();
            mockQueryEngine.Setup(o => o.GetDocuments(It.IsAny<AzureSearchParameters>())).Returns(searchResult.Body).Callback((AzureSearchParameters p) =>
            {
                searchText = p.SearchText;
                parameters.Filter = p.Filter;
                parameters.IncludeTotalResultCount = p.IncludeTotalResultCount;
                parameters.Skip = p.Skip;
                parameters.Top = p.Top;
            });
            var indexingEngine = new AzureIndexingEngine(mockQueryEngine.Object, mockDocuments.Object);

            indexingEngine.WriteIndex(null, null, null);

            mockQueryEngine.Verify(o => o.GetDocuments(It.IsAny<AzureSearchParameters>()), Times.Never);
            mockDocuments.Verify(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)), Times.Once);
            Assert.Equal(0, batch.Actions.Count(a => a.ActionType == IndexActionType.Delete));
            Assert.Equal(0, batch.Actions.Count(a => a.ActionType == IndexActionType.Merge));
            Assert.Equal(0, batch.Actions.Count(a => a.ActionType == IndexActionType.MergeOrUpload));
            Assert.Equal(null, searchText);
            Assert.Equal(null, parameters.Filter);
            Assert.Equal(false, parameters.IncludeTotalResultCount);
            Assert.Equal(null, parameters.Skip);
            Assert.Equal(null, parameters.Top);
        }

        [Fact]
        public void WriteIndexWithMultipleDeleteTermsTest()
        {
            string text = null;
            string searchText = null;
            var batch = default(IndexBatch<IndexDocument>);
            SearchParameters parameters = new SearchParameters();
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
            var indexResult = new AzureOperationResponse<DocumentIndexResult>();
            var indexingResults = new List<IndexingResult>();
            indexingResults.Add(new IndexingResult("12", null, true));
            indexingResults.Add(new IndexingResult("13", null, true));
            indexingResults.Add(new IndexingResult("0", null, true));
            indexingResults.Add(new IndexingResult("1", null, true));
            indexingResults.Add(new IndexingResult("2", null, true));
            indexResult.Body = new DocumentIndexResult(indexingResults);
            var indexTask = Task.FromResult(indexResult);
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken))).Returns(indexTask)
                .Callback((IndexBatch<IndexDocument> b, SearchRequestOptions o, Dictionary<string, List<string>> c, CancellationToken t) =>
                {
                    batch = b;
                });

            var mockQueryEngine = new Mock<IIndexingQuery>();
            mockQueryEngine.Setup(o => o.GetDocuments(It.IsAny<AzureSearchParameters>())).Returns(searchResult.Body).Callback((AzureSearchParameters p) =>
            {
                searchText = p.SearchText;
                parameters.Filter = p.Filter;
                parameters.IncludeTotalResultCount = p.IncludeTotalResultCount;
                parameters.Skip = p.Skip;
                parameters.Top = p.Top;
            });
            var indexingEngine = new AzureIndexingEngine(mockQueryEngine.Object, mockDocuments.Object);
            var deletions = new List<SnTerm>();
            deletions.Add(new SnTerm("VersionId", "12"));
            deletions.Add(new SnTerm("Path", "/Root/Global"));
            deletions.Add(new SnTerm("NodeId", 1345));
            deletions.Add(new SnTerm("IsGood", true));
            deletions.Add(new SnTerm("Created", new DateTime(2017,2,28,2,2,2,777, DateTimeKind.Utc)));
            var addable = new IndexDocument();
            addable.Add(new IndexField("VersionId", int.Parse("13"), IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            addable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            var updateables = new List<DocumentUpdate>();
            for (int i = 0; i < 3; i++)
            {
                var updateable = new IndexDocument();
                updateable.Add(new IndexField("VersionId", i, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateables.Add(new DocumentUpdate { Document = updateable, UpdateTerm = new SnTerm("", "") });
            }
            indexingEngine.WriteIndex(deletions, addable, updateables);

            mockQueryEngine.Verify(o => o.GetDocuments(It.IsAny<AzureSearchParameters>()), Times.Once);
            mockDocuments.Verify(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)), Times.Once);
            Assert.Equal(1, batch.Actions.Count(a => a.ActionType == IndexActionType.Delete));
            Assert.Equal(3, batch.Actions.Count(a => a.ActionType == IndexActionType.Merge));
            Assert.Equal(1, batch.Actions.Count(a => a.ActionType == IndexActionType.MergeOrUpload));
            Assert.Equal("VersionId:12 Path:/Root/Global NodeId:1345 IsGood:True Created:2017-2-28T02:02:02.777Z", searchText);
            Assert.Equal("", parameters.Filter);
            Assert.Equal(false, parameters.IncludeTotalResultCount);
            Assert.Equal(null, parameters.Skip);
            Assert.Equal(null, parameters.Top);
        }

        [Fact]
        public void WriteIndexWithIndexingErrorsTest()
        {
            string text = null;
            string searchText = null;
            var batch = new IndexBatch<IndexDocument>[2];// default(IndexBatch<IndexDocument>);
            SearchParameters parameters = new SearchParameters();
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
            var indexResult1 = new AzureOperationResponse<DocumentIndexResult>();
            var indexingResults1 = new List<IndexingResult>();
            indexingResults1.Add(new IndexingResult("12", null, false,422));
            indexingResults1.Add(new IndexingResult("13", null, true,200));
            indexingResults1.Add(new IndexingResult("0", null, true, 200));
            indexingResults1.Add(new IndexingResult("1", null, false,503));
            indexingResults1.Add(new IndexingResult("2", null, true, 200));
            indexResult1.Body = new DocumentIndexResult(indexingResults1);
            var indexException = new IndexBatchException(new DocumentIndexResult(indexingResults1));
            var indexResult2 = new AzureOperationResponse<DocumentIndexResult>();
            var indexingResults2 = new List<IndexingResult>();
            indexingResults2.Add(new IndexingResult("12", null, true, 200));
            indexingResults2.Add(new IndexingResult("1", null, true, 200));
            indexResult2.Body = new DocumentIndexResult(indexingResults2);
            var indexTask = Task.FromResult(indexResult2);
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.Is<IndexBatch<IndexDocument>>(i => i.Actions.Count()==5), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)))
                .Returns((IndexBatch<IndexDocument> b, SearchRequestOptions o, Dictionary<string, List<string>> c,CancellationToken t) =>
                        {
                            batch[0] = b;
                            throw indexException;
                        });
            mockDocuments.Setup(o => o.IndexWithHttpMessagesAsync(It.Is<IndexBatch<IndexDocument>>(i => i.Actions.Count()==2), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)))
                .Returns(indexTask)
                .Callback((IndexBatch<IndexDocument> b, SearchRequestOptions o, Dictionary<string, List<string>> c, CancellationToken t) =>
                {
                    batch[1] = b;
                });

            //var queryEngine = new AzureQueryEngine(mockDocuments.Object);
            var mockQueryEngine = new Mock<IIndexingQuery>();
            mockQueryEngine.Setup(o => o.GetDocuments(It.IsAny<AzureSearchParameters>())).Returns(searchResult.Body).Callback((AzureSearchParameters p) =>
            {
                searchText = p.SearchText;
                parameters.Filter = p.Filter;
                parameters.IncludeTotalResultCount = p.IncludeTotalResultCount;
                parameters.Skip = p.Skip;
                parameters.Top = p.Top;
            });
            var indexingEngine = new AzureIndexingEngine(mockQueryEngine.Object, mockDocuments.Object);
            var deletions = new List<SnTerm>();
            deletions.Add(new SnTerm("VersionId", "12"));
            var addable = new IndexDocument();
            addable.Add(new IndexField("VersionId", int.Parse("13"), IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            addable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            var updateables = new List<DocumentUpdate>();
            for (int i = 0; i < 3; i++)
            {
                var updateable = new IndexDocument();
                updateable.Add(new IndexField("VersionId", i, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateable.Add(new IndexField("Name", "ContentName", IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                updateables.Add(new DocumentUpdate { Document = updateable, UpdateTerm = new SnTerm("", "") });
            }
            indexingEngine.WriteIndex(deletions, addable, updateables);

            mockQueryEngine.Verify(o => o.GetDocuments(It.IsAny<AzureSearchParameters>()), Times.Once);
            mockDocuments.Verify(o => o.IndexWithHttpMessagesAsync(It.IsAny<IndexBatch<IndexDocument>>(), It.IsAny<SearchRequestOptions>(), null, default(CancellationToken)), Times.Exactly(2));
            Assert.Equal(1, batch[0].Actions.Count(a => a.ActionType == IndexActionType.Delete));
            Assert.Equal(3, batch[0].Actions.Count(a => a.ActionType == IndexActionType.Merge));
            Assert.Equal(1, batch[0].Actions.Count(a => a.ActionType == IndexActionType.MergeOrUpload));
            Assert.Equal(1, batch[1].Actions.Count(a => a.ActionType == IndexActionType.Delete));
            Assert.Equal(1, batch[1].Actions.Count(a => a.ActionType == IndexActionType.Merge));
            Assert.Equal(0, batch[1].Actions.Count(a => a.ActionType == IndexActionType.MergeOrUpload));
            Assert.Equal("VersionId:12", searchText);
            Assert.Equal("", parameters.Filter);
            Assert.Equal(false, parameters.IncludeTotalResultCount);
            Assert.Equal(null, parameters.Skip);
            Assert.Equal(null, parameters.Top);
        }
    }
}