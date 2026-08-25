using System.Collections.Generic;
using ThriftHub.Models;

namespace ThriftHub.ViewModels
{
    public class SellerProfileViewModel
    {
        public ApplicationUser Seller { get; set; } = null!;

        public List<Product> Products { get; set; } = new();

        public int TotalProducts { get; set; }
    }
}