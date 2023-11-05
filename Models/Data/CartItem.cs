﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCart.Api.Models.Data
{
    public class CartItem
    {
        public Guid CartId { get; set; }
        public Cart Cart { get; set; }

        public Guid CatalogItemId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }

        [NotMapped]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public string Name { get; set; }

        [NotMapped]
        public int MaxQuantity { get; set; }
    }
}
