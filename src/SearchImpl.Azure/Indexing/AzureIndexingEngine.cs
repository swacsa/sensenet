using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using SenseNet.Search.Azure.Indexing.Models;
using SenseNet.Search.Azure.Querying;
using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Indexing;
using IndexBatch = Microsoft.Azure.Search.Models.IndexBatch;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Search.Azure.Indexing
{
    // Azure Search API Version: 2016-09-01
    public class AzureIndexingEngine : IIndexingEngine
    {
        private static string _apiKey = "";
        private static string _schema = "https://";
        private static string _serviceName = "";
        private static string _indexName = "";
        private static string _dnsSuffix = "search.windows.net/indexes/";
        private static int? _operationTimeout = 60;
        //private static int _top = 1000;
        private static int _maxTryCount = 5;

        private static SearchCredentials _credentials;
        private static ISearchIndexClient _indexClient;

        private static IDocumentsOperations _documents;
        private static ReaderWriterLockSlim _statusLock = new ReaderWriterLockSlim();
        private static IActivityStatusPersistor _status;
        //private Dictionary<string, List<string>> _customHeaders = null;

        //private AzureQueryExecutor _queryExecutor;
        private IIndexingQuery _queryEngine;

        public AzureIndexingEngine(IIndexingQuery queryEngine, IDocumentsOperations documents, IActivityStatusPersistor status)
        {
            //_queryExecutor = new AzureQueryExecutor(new AzureQueryEngine());
            _queryEngine = queryEngine;
            _status = status;
            //_credentials = new SearchCredentials(_apiKey);
            //_indexClient = new SearchIndexClient(_serviceName, _indexName, _credentials);
            //_indexClient.BaseUri = new Uri(_schema + _serviceName + "." + _dnsSuffix + _indexName);
            //_indexClient.LongRunningOperationRetryTimeout = _operationTimeout;
            //_documents = _indexClient.Documents;
            _documents = documents;
        }

        #region Azure calls

        public Task<AzureDocumentIndexResult> UploadAsync<T>(IEnumerable<T> documents) where T : IndexDocument
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }
            var cancellationToken = new CancellationToken();
            return Task.Factory.StartNew(() => Upload(documents), cancellationToken);
        }

        public AzureDocumentIndexResult Upload<T>(IEnumerable<T> documents) where T : IndexDocument
        {
            return Index(IndexBatch.Upload(documents), 1);
        }

        private AzureDocumentIndexResult Index<T>(IndexBatch<T> batch, int tryCount) where T : IndexDocument
        {
            try
            {
                return (AzureDocumentIndexResult) _documents.IndexWithHttpMessagesAsync(batch).Result.Body;
            }
            catch (Exception ex)
            {
                if (tryCount > _maxTryCount || !(ex is IndexBatchException))
                {
                    throw;
                }
                var batchException = (IndexBatchException) ex;
                var failedBatch = batchException.FindFailedActionsToRetry(batch,
                    r => r.GetIntegerValue(IndexFieldName.VersionId).ToString());
                if (!failedBatch.Actions.Any())
                {
                    throw;
                }
                Thread.Sleep(RetryWaitTime(tryCount));
                return Index(failedBatch, ++tryCount);
            }
        }

        private int RetryWaitTime(int tryCount)
        {
            return (int) Math.Pow(2, tryCount);
        }

        public Task<AzureDocumentIndexResult> DeleteAsync<T>(IEnumerable<T> documents) where T : IndexDocument
        {
            var cancellationToken = new CancellationToken();
            if (documents == null)
            {
                throw new ArgumentNullException("keys");
            }
            return Task.Factory.StartNew(() => Delete(documents), cancellationToken);
        }

        public AzureDocumentIndexResult Delete<T>(IEnumerable<T> documents) where T : IndexDocument
        {
            //return Index( IndexBatch.Delete((IndexFieldName.Name, keys), 1);
            return Index(IndexBatch.Delete(documents), 1);
        }

        #endregion

        #region IIndexingEngine

        public bool Running { get; private set; }
        //public bool Paused { get; private set; }

        //public void Pause()
        //{
        //    Paused = true;
        //}

        //public void Continue()
        //{
        //    Paused = false;
        //}

        public void Start(TextWriter consoleOut)
        {
            Running = true;
        }


        public void ShutDown()
        {
            Running = false;
        }


        public void ActivityFinished()
        {
        }

        public void Commit(int lastActivityId = 0)
        {
        }

        public IIndexingActivityStatus ReadActivityStatusFromIndex()
        {
            _statusLock.EnterReadLock();
            try
            {
                var docs =_queryEngine.GetDocuments(new AzureSearchParameters {SearchText = "VersionId:0"});
                if (docs.Results != null && docs.Results.Count>0)
                {
                    var stateField = docs.Results[0].Document["Version"] as string;
                    var lastId = int.Parse(stateField.Split(';')[0]);
                    var gaps = stateField.Split(';')[1].Split(',').Select( g => int.Parse(g)).ToArray();
                    return new IndexingActivityStatus {LastActivityId = lastId, Gaps = gaps};
                }
                return null;
                //return _status.GetStatus();
            }
            finally
            {
                _statusLock.ExitReadLock();
            }
        }

        public void WriteActivityStatusToIndex(IIndexingActivityStatus state)
        {
            _statusLock.EnterWriteLock();
            try
            {
                var document = new IndexDocument();
                var versionIdField = new IndexField(IndexFieldName.VersionId, 0,IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No);
                var stateField = new IndexField(IndexFieldName.Version, state.LastActivityId+ ";" + string.Join(",", state.Gaps.Select( g => g.ToString())),
                IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No);
                document.Add(versionIdField);
                document.Add(stateField);
                var action = IndexAction.MergeOrUpload(document);
                var indexActions = new List<IndexAction<IndexDocument>>();
                indexActions.Add(action);
                Index(new IndexBatch<IndexDocument>(indexActions), 1);
                //_status.PutStatus(state);
            }
            catch (Exception)
            {
                _statusLock.ExitWriteLock();
                throw;
            }
        }
        public IEnumerable<IndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            var nodes = new [] { new SnTerm(IndexFieldName.NodeId, nodeId)};
            string filter;
            var searchText = GetFilterCondition(nodes, out filter);
            AzureSearchParameters queryParameters = new AzureSearchParameters { SearchText = searchText, Filter = filter };
            var docs = _queryEngine.GetDocuments(queryParameters).Results.Select(r => r.Document).ToArray();
            var documents = new List<IndexDocument>();
            foreach (var doc in docs)
            {
                documents.Add(GetIndexDocument(doc));
            }
            return documents;
        }

        private IndexDocument GetIndexDocument(Document doc)
        {
            var document = new IndexDocument();
            foreach (var key in doc.Keys)
            {
                if (doc[key] is bool)
                {
                    document.Add(new IndexField(key, bool.Parse(doc[key].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No));
                }
                if (doc[key] is int)
                {
                    document.Add(new IndexField(key, int.Parse(doc[key].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No));
                }
                if (doc[key] is long)
                {
                    document.Add(new IndexField(key, long.Parse(doc[key].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No));
                }
                if (doc[key] is float)
                {
                    document.Add(new IndexField(key, float.Parse(doc[key].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No));
                }
                if (doc[key] is double)
                {
                    document.Add(new IndexField(key, double.Parse(doc[key].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No));
                }
                if (doc[key] is DateTime)
                {
                    document.Add(new IndexField(key, DateTime.Parse(doc[key].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.No, IndexTermVector.No));
                }
            }
            return document;
        }
        public void WriteIndex(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates)
        {
            Actualize(deletions, addition == null ? null : new [] {addition}, updates);
        }
        public void WriteIndex(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> additions)
        {
            Actualize(deletions, additions, null);
        }

        public void ClearIndex()
        {
            throw new NotImplementedException();
        }

        private void Actualize(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> additions, IEnumerable<DocumentUpdate> updates) 
        {
            var indexActions = new List<IndexAction<IndexDocument>>();
            if (deletions != null)
            { 
                var dels = deletions.ToArray();
                string filter;
                var searchText = GetFilterCondition(dels, out filter);
                AzureSearchParameters queryParameters = new AzureSearchParameters {SearchText = searchText, Filter = filter};
                var deletables = _queryEngine.GetDocuments(queryParameters).Results.Select(r => r.Document).ToArray(); 
                if (deletables.Any())
                {
                    indexActions.AddRange(deletables.Select(d =>
                    {
                        var document = new IndexDocument {};
                        document.Add(new IndexField(IndexFieldName.VersionId, int.Parse(d[IndexFieldName.VersionId].ToString()), IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default ));
                        return IndexAction.Delete(document);
                    }));
                }
            }
            if (updates != null)
            {
                //it needs to be MergeOrUpload because if it is a repeated call for this batch this way it can eliminate inconsistencies
                indexActions.AddRange(updates.Select(u => IndexAction.MergeOrUpload(u.Document)));
            }
            if (additions != null)
            {
                //it needs to be MergeOrUpload because if it is a repeated call for this batch this way it can eliminate inconsistencies
                indexActions.AddRange(additions.Select(IndexAction.MergeOrUpload));
            }
            Index(new IndexBatch<IndexDocument>(indexActions), 1);
        }

        private string GetFilterCondition(IEnumerable<SnTerm> terms, out string filter)
        {
            var searchText = new StringBuilder();
            var filterText = new StringBuilder();
            filter = "";
            // we take only the first 1024 terms, because of the explicit constraint in Azure Search
            foreach (var term in terms.Take(1024))
            {
                if (searchText.Length > 0)
                {
                    searchText.Append(" ");
                }
                string filterPart;
                searchText.Append(GetFilter(term, out filterPart));
                if (filterPart.Length > 0)
                {
                    if (filterText.Length > 0)
                    {
                        filterText.Append(" ");
                    }
                    filterText.Append(filterPart);
                }
            }
            filter = filterText.ToString();
            return searchText.ToString();
        }
        private string GetFilter(SnTerm term, out string filter)
        {
            var searchText = new StringBuilder();
            var filterText = new StringBuilder();
            filter = "";

            if (term.Type == SnTermType.StringArray)
            {
                filterText.Append("search.in(");
                filterText.Append(term.Name);
                filterText.Append(",'");
                var notFirst = false;
                foreach (var value in term.StringArrayValue)
                {
                    if (notFirst)
                    {
                        filterText.Append(",");
                    }
                    filterText.Append(value);
                    notFirst = true;
                }
                filterText.Append("')");
                filter = filterText.ToString();
                return searchText.ToString();
            }
            // else
            searchText.Append($"{term.Name.Replace("#", "")}:");
            switch (term.Type)
            {
                case SnTermType.String:
                    var phrase = term.StringValue.WordCount() > 1;
                    if (phrase)
                    {
                        searchText.Append("\"");
                    }
                    searchText.Append(term.StringValue);
                    if (phrase)
                    {
                        searchText.Append("\"");
                    }
                    break;
                case SnTermType.Bool:
                    searchText.Append(term.BooleanValue);break;
                case SnTermType.Int:
                    searchText.Append(term.IntegerValue);break;
                case SnTermType.Long:
                    searchText.Append(term.LongValue);break;
                case SnTermType.Float:
                    searchText.Append(term.SingleValue);break;
                case SnTermType.Double:
                    searchText.Append(term.DoubleValue);break;
                case SnTermType.DateTime:
                    searchText.Append(GetODataV4DateTimeUtcString(term.DateTimeValue)); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return searchText.ToString();
        }

        private string GetODataV4DateTimeUtcString(DateTime datetime)
        {
            var utc = datetime.ToUniversalTime();
            var result = $"{utc.Year}-{utc.Month}-{utc.Day}T{utc.Hour.ToString("D2")}:{utc.Minute.ToString("D2")}:{utc.Second.ToString("D2")}.{utc.Millisecond}Z";
            return result;
        }

        #endregion
    }
}