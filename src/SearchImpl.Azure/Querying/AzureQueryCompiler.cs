using System.Linq;
using SenseNet.Search.Azure.Querying.Models;
using SenseNet.Search.Parser;

namespace SenseNet.Search.Azure.Querying
{
    public class AzureQueryCompiler
    {
        public AzureSearchParameters Compile(SnQuery query, IQueryContext context)
        {
            var parameters = new AzureSearchParameters();
            parameters.Skip = query.Skip;
            parameters.Top = query.Top;
            //parameters.OrderBy = query.Sort;
            parameters.EnableAutofilters = query.EnableAutofilters == FilterStatus.Enabled;
            parameters.EnableLifespanFilter = query.EnableLifespanFilter == FilterStatus.Enabled;
            //parameters.Facets = 
            parameters.IncludeTotalResultCount = query.CountOnly;
            //parameters.Select = query.Projection.;
            return parameters;
        }
    }
}