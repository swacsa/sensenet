using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using SenseNet.Search.Azure.Indexing.Models;
using IndexBatch = Microsoft.Azure.Search.Models.IndexBatch;

namespace SenseNet.Search.Azure.Indexing
{
    public class AzureIndexingEngine: IIndexingEngine
    {
        private static string _apiKey = "";
        private static string _schema = "https://";
        private static string _serviceName = "";
        private static string _indexName = "";
        private static string _dnsSuffix = "search.windows.net/indexes/";
        //private static string _apiVersion = "2016-09-01";
        private static int? _operationTimeout = 60;
        //private static int _top = 1000;
        private static int _maxTryCount = 5;

        private static SearchCredentials _credentials;
        private static ISearchIndexClient _indexClient;
        private static IDocumentsOperations _documents;
        //private Dictionary<string, List<string>> _customHeaders = null;

        public AzureIndexingEngine()
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

        private readonly int[] _transientErrorCodes = {207, 422, 503};

        public Task<AzureDocumentIndexResult> UploadAsync<T>(IEnumerable<T> documents) where T : IndexDocument
        {
            if (documents == null)
            {
                throw new ArgumentNullException("documents");
            }
            var cancellationToken =  new CancellationToken();
            return Task.Factory.StartNew(()=> Upload(documents), cancellationToken);
        }

        public AzureDocumentIndexResult Upload<T>(IEnumerable<T> documents) where T : IndexDocument
        {
            return Index(IndexBatch.Upload(documents), 1);
        }

        private AzureDocumentIndexResult Index<T>(IndexBatch<T> batch, int tryCount) where T: IndexDocument
        {
            try
            {
                return (AzureDocumentIndexResult) _documents.Index(batch);
            }
            catch (Exception ex)
            {
                if (tryCount > _maxTryCount || !(ex is IndexBatchException))
                {
                    throw;
                }
                var batchException = (IndexBatchException)ex;
                var results = batchException.IndexingResults;
                if (results.Any(r => !r.Succeeded && !_transientErrorCodes.Contains(r.StatusCode)))
                {
                    throw;
                }
                var failedBatch = batchException.FindFailedActionsToRetry(batch,  r => r.GetStringValue(IndexFieldName.Name));
                Thread.Sleep(RetryWaitTime(tryCount));
                return Index(failedBatch, ++tryCount);
            }
        }

        private int RetryWaitTime(int tryCount)
        {
            return (int) Math.Pow(2, tryCount);
        }

        public Task<AzureDocumentIndexResult> DeleteAsync(IEnumerable<string> keys)
        {
            var cancellationToken = new CancellationToken();
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            return Task.Factory.StartNew(() => Delete(keys), cancellationToken);
        }

        public AzureDocumentIndexResult Delete(IEnumerable<string> keys)
        {
            return Index(IndexBatch.Delete(), 1);
            return Index(IndexBatch.Delete(IndexFieldName.Name, keys), 1);
        }

        #region IIndexingEngine

        public bool Running { get; }
        public bool Paused { get; }
        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Continue()
        {
            throw new NotImplementedException();
        }

        public void Start(TextWriter consoleOut)
        {
            throw new NotImplementedException();
        }

        public void WaitIfIndexingPaused()
        {
            throw new NotImplementedException();
        }

        public void ShutDown()
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void ActivityFinished()
        {
            throw new NotImplementedException();
        }

        public void Commit(int lastActivityId = 0)
        {
            throw new NotImplementedException();
        }

        public IIndexingActivityStatus ReadActivityStatusFromIndex()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IIndexDocument> GetDocumentsByNodeId(int nodeId)
        {
            throw new NotImplementedException();
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IndexDocument addition, IEnumerable<DocumentUpdate> updates)
        {
            throw new NotImplementedException();
        }

        public void Actualize(IEnumerable<SnTerm> deletions, IEnumerable<IndexDocument> addition)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}