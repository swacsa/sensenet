using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Azure.Querying
{
    public interface IQueryCompiler
    {
        AzureSearchParameters Compile(SnQuery query, IQueryContext context);
    }
}