using Microsoft.Azure.Search.Models;

namespace SenseNet.Search.Azure.Indexing.Models
{
    public class AzureIndexingResult
    {
        public string ErrorMessage { get; set; }
        public string Key { get; set; }
        public int StatusCode { get; set; }
        public bool Succeeded { get; set; }

        public static explicit operator AzureIndexingResult(IndexingResult result)
        {
            var indexingResult = new AzureIndexingResult();
            indexingResult.ErrorMessage = result.ErrorMessage;
            indexingResult.Key = result.Key;
            indexingResult.StatusCode = result.StatusCode;
            indexingResult.Succeeded = result.Succeeded;
            return indexingResult;
        }
    }
}