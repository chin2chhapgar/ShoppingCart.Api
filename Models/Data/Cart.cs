using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShoppingCart.Api.Repositories.Interfaces;

namespace ShoppingCart.Api.Models.Data
{
    public sealed class Cart : Entity
    {
        public List<CartItem> Products { get; set; } = new List<CartItem>();
        public string Response { get; set; } = string.Empty;
        public Cart()
        {
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        
    }
}
