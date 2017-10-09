using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;

namespace SenseNet.Search.Azure.Indexing.Models
{
    public class IndexDocument
    {
        [Key]
        [IsFilterable, IsSortable, IsRetrievable(true)]
        public string Id { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        public int? NodeId { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true), IsFacetable]
        public string Type { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true)]
        public string Name { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true)]
        public string Path { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        public string Version { get; set; }

        public string GetKey(IndexDocument document)
        {
            return document.Id;
        }

    }
}