using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search.Models;

namespace SenseNet.Search.Azure.Indexing.Models
{
    public class AzureDocumentIndexResult
    {
        public List<AzureIndexingResult> Results { get; set; }

        public AzureDocumentIndexResult()
        {
            Results = new List<AzureIndexingResult>();
        }

        public static explicit operator AzureDocumentIndexResult(DocumentIndexResult result)
        {
            var indexResult = new AzureDocumentIndexResult();
            indexResult.Results = result.Results.Select(r => (AzureIndexingResult)r).ToList();
            return indexResult;
        }
    }
}