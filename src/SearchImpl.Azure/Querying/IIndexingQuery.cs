using Microsoft.Azure.Search.Models;
using SenseNet.Search.Azure.Querying.Models;

namespace SenseNet.Search.Azure.Querying
{
    public interface IIndexingQuery
    {
        DocumentSearchResult GetDocuments(AzureSearchParameters searchParameters);
    }
}