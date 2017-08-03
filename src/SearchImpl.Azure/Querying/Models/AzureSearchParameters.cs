using System.Collections.Generic;
using Microsoft.Azure.Search.Models;

namespace SenseNet.Search.Azure.Querying.Models
{
    public class AzureSearchParameters
    {
        public IList<string> Facets { get; set; }
        public string Filter { get; set; }
        public string SearchText { get; set; }
        public bool IncludeTotalResultCount { get; set; }
        public IList<string> OrderBy { get; set; }
        public IList<string> SearchFields { get; set; }
        public IList<string> Select { get; set; }
        public int? Skip { get; set; }
        public int? Top { get; set; }
        public bool EnableAutofilters { get; set; }
        public bool EnableLifeSpanFilter { get; set; }

        public static explicit operator SearchParameters(AzureSearchParameters searchParameters)
        {
            var parameters = new SearchParameters();
            parameters.Filter = searchParameters.Filter;
            parameters.QueryType = QueryType.Full;
            parameters.SearchMode = SearchMode.All;
            parameters.Facets = searchParameters.Facets;
            parameters.IncludeTotalResultCount = searchParameters.IncludeTotalResultCount;
            parameters.OrderBy = searchParameters.OrderBy;
            parameters.SearchFields = searchParameters.SearchFields;
            parameters.Select = searchParameters.Select;
            parameters.Skip = searchParameters.Skip;
            parameters.Top = searchParameters.Top ?? 1000;

            return parameters;
        }
    }
}