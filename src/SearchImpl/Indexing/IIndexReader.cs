using System.Collections.Generic;
using Lucene.Net.Support;

namespace SenseNet.Search.Indexing
{
    public interface IIndexReader
    {
        IDictionary<string, string> GetCompletionInfo();
    }
}