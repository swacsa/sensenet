using System;
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
            parameters.Top = query.CountOnly ? 0 : query.Top;
            parameters.EnableAutofilters = query.EnableAutofilters == FilterStatus.Enabled;
            parameters.EnableLifespanFilter = query.EnableLifespanFilter == FilterStatus.Enabled;
            parameters.IncludeTotalResultCount = true;
            if (query.Projection != null)
            {
                var fields = query.Projection.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                parameters.Select = fields;
            }
            if (query.Sort != null)
            {
                // it is only 32, because of the explicit constraint in Azure Search
                parameters.OrderBy = query.Sort.Take(32).Select(e =>e.FieldName + (e.Reverse ? " desc" : default(string))).ToList();
            }
            var visitor = new SnQueryToAzureQueryVisitor(context, parameters);
            visitor.Visit(query.QueryTree);

            return visitor.Result;
        }
    }
}