﻿using System;
using System.Collections.Generic;
using System.Linq;
using ShoppingCart.Api.Models.Dto.Common;

namespace ShoppingCart.Api.Models.Dto.Carts
{
    public class CartResponseDto : Resource
    {
        public Guid Id { get; set; }
        public string Response { get; set; }
        public decimal Total => Products.Sum(x => x.SubTotal);

        public List<CartItemResponseDto> Products { get; set; } = new List<CartItemResponseDto>();
    }
}
