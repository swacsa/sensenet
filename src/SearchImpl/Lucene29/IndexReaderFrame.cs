using System;
using Lucene.Net.Index;
using SenseNet.Search.Indexing;

namespace SenseNet.Search.Lucene29
{
    public class IndexReaderFrame : IDisposable, IIndexReader
    {
        private readonly IndexReader _reader;
        public IndexReader IndexReader => _reader;

        public IndexReaderFrame(IndexReader reader)
        {
            _reader = reader;
            reader.IncRef();
        }

        public void Dispose()
        {
            _reader.DecRef();
        }

        internal static IndexReaderFrame GetReaderFrame(bool dirty = false)
        {
            return ((Lucene29IndexingEngine)IndexManager.IndexingEngine).GetIndexReaderFrame(dirty); //UNDONE: refactor: do not use member of another class
        }

        public IDictionary<string, string> GetCompletionInfo()
        {
            return _reader.GetCommitUserData();
        }
    }
}
