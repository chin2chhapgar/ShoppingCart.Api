﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.Api.Models.Data;
using ShoppingCart.Api.Contexts;
using ShoppingCart.Api.Models.Dto.Carts;
using ShoppingCart.Api.Repositories.Interfaces;

namespace ShoppingCart.Api.Repositories.Implementation
{
    public class CartRepository : BaseRepository<Cart>, ICartRepository
    {
        private readonly ApiDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICatalogRepository _catalogRepository;

        public CartRepository(ApiDbContext dbContext, 
            IMapper mapper, 
            ICatalogRepository catalogRepository) : base(dbContext)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _catalogRepository = catalogRepository;
        }

        public async Task<Cart> CreateShoppingCartAsync(CartContentsRequestDto cartContentsRequest)
        {
            var cart = new Cart();

            if (cartContentsRequest.Products.Any())
            {
                foreach (var item in cartContentsRequest.Products)
                {
                    var catalogItem = await _catalogRepository.FindByIdAsync(item.Id);
                    if (catalogItem == null)
                        return null;

                    if (item.Quantity > catalogItem.Quantity)
                    {
                        cart.Response = "Item: "+ catalogItem.Name + " is out of stock, Max Qty: " + catalogItem.Quantity;
                        return await EnrichCart(cart);
                    }
                }
            }

            if (cartContentsRequest.Products.Any())
            {
                cart.Products = _mapper.Map<List<CartItem>>(cartContentsRequest.Products).ToList();
            }            

            cart.Response = "Cart created";
            _dbContext.Add(cart);

            await _dbContext.SaveChangesAsync();
            return await EnrichCart(cart);
        }

        public async Task<Cart> UpdateShoppingCartAsync(Guid cartId, 
            CartContentsRequestDto cartContentsRequest)
        {
            var cart = await FindByIdAsync(cartId);
            if (cart == null)
                return null;

            if (cartContentsRequest.Products.Any())
            {
                foreach (var item in cartContentsRequest.Products)
                {
                    var catalogItem = await _catalogRepository.FindByIdAsync(item.Id);
                    if (catalogItem == null)
                        return null;

                    if (item.Quantity > catalogItem.Quantity)
                    {
                        cart.Response = "Item: " + catalogItem.Name + " is out of stock, Max Qty: " + catalogItem.Quantity;
                        return await EnrichCart(cart);
                    }
                }
            }

            cart.Products = _mapper.Map<List<CartItem>>(cartContentsRequest.Products).ToList();
            cart.UpdatedAt = DateTimeOffset.UtcNow;
            cart.Response = "Cart items.";
            _dbContext.Update(cart);

            await _dbContext.SaveChangesAsync();
            return await EnrichCart(cart);
            //return cart;
        }


        public async Task RemoveShoppingCartAsync(Guid cartId)
        {
            var cart = await FindByIdAsync(cartId);
            if (cart != null)
            {
                cart.Response = "Cart removed";
                _dbContext.Carts.Remove(cart);
                await _dbContext.SaveChangesAsync();
            }
        }


        public async Task<Cart> RemoveShoppingCartItemAsync(Guid cartId, Guid itemId)
        {
            var cart = await FindByIdAsync(cartId);
            if (cart == null)
                return null;

            var catalogItem = await _catalogRepository.FindByIdAsync(itemId);
            if (catalogItem == null)
                return null;

            var itemToRemove = cart.Products.FirstOrDefault(x => x.CatalogItemId == itemId);
            if (itemToRemove != null)
            {
                cart.Products.Remove(itemToRemove);
                await _dbContext.SaveChangesAsync();
            }

            cart.Response = "Item removed from cart";
            return await EnrichCart(cart);
        }


        public async Task<Cart> IncreaseShoppingCartItemAsync(Guid cartId, Guid itemId, int quantity)
        {
            var cart = await FindByIdAsync(cartId);
            if (cart == null)
                return null;

            var catalogItem = await _catalogRepository.FindByIdAsync(itemId);
            if (catalogItem == null)
                return null;

            

            if (cart.Products.Any(x => x.CatalogItemId == itemId))
            {
                var item = cart.Products.First(x => x.CatalogItemId == itemId);

                if (item.Quantity + quantity > catalogItem.Quantity)
                    cart.Response = "Item: " + catalogItem.Name + " is out of stock, Max Qty: " + catalogItem.Quantity;
                else
                {
                    item.Quantity += quantity;
                    cart.Response = "Item quantity updated";                   
                }

            }
            else
            {
                cart.Products.Add(new CartItem
                {
                    CartId = cartId,
                    CatalogItemId = itemId,
                    Quantity = quantity
                });

                if (quantity > catalogItem.Quantity)
                    cart.Response = "Item: " + catalogItem.Name + " is out of stock, Max Qty: " + catalogItem.Quantity;
                else
                    cart.Response = "Item quantity updated";
            }

            _dbContext.Attach(cart);
            await _dbContext.SaveChangesAsync();
            return await EnrichCart(cart);
        }


        public async Task<Cart> DecreaseShoppingCartItemAsync(Guid cartId, Guid itemId, int quantity)
        {
            var cart = await FindByIdAsync(cartId);
            if (cart == null)
                return null;

            var catalogItem = await _catalogRepository.FindByIdAsync(itemId);
            if (catalogItem == null)
                return null;

            // Do not allow the quantity to go below zero
            // Arguably we should delete it but then makes it more difficult to increase from client
            if (cart.Products.Any(x => x.CatalogItemId == itemId))
            {
                var item = cart.Products.First(x => x.CatalogItemId == itemId);
                item.Quantity -= (quantity > item.Quantity) ? item.Quantity : quantity;               
            }

            cart.Response = "Item quantity updated";

            // Do not throw error if the item not in the cart
            // Could equally throw a bad request but little benefit
            _dbContext.Attach(cart);
            await _dbContext.SaveChangesAsync();
            return await EnrichCart(cart);
        }


        public override async Task<Cart> FindByIdAsync(Guid id)
        {
            var cart = await _dbContext
                .Carts
                .Include(e => e.Products)
                .FirstOrDefaultAsync(x => x.Id == id);

            return await EnrichCart(cart);
            //return cart;
        }


        // Look to find a better way - perhaps wrap the cart model with the extended data?
        private async Task<Cart> EnrichCart(Cart cart)
        {
            if (cart == null)
                return null;

            foreach (var item in cart.Products)
            {
                var catalogItem = await _catalogRepository.FindByIdAsync(item.CatalogItemId);
                if (catalogItem == null)
                    return null;

                item.UnitPrice = catalogItem.UnitPrice;
                item.MaxQuantity = catalogItem.Quantity;
                item.Name = item.Quantity > 1 ? catalogItem.NamePlural : catalogItem.Name;
            }

            return cart;
        }



    }
}
