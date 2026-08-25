using System;
using System.Collections.Generic;
using ThriftHub.Models;

namespace ThriftHub.ViewModels
{
    public class MarketplaceViewModel
    {
        public List<Product> Products { get; set; } = new();

        public string? Search { get; set; }

        public string? Category { get; set; }

        public string? Subcategory { get; set; }

        public string? Condition { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public string SortBy { get; set; } = "newest";

        public int TotalResults { get; set; }

        public List<string> Categories { get; set; } = new();

        public List<string> Subcategories { get; set; } = new();

        public List<string> Conditions { get; set; } = new();
    }
}