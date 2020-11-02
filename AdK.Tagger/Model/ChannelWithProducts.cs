using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
    public class ProductWithPriceDefs : Product
    {
        public ProductWithPriceDefs()
        {
            PriceDefsMatrix = new decimal[24][];
            for (int i = 0; i < 24; i++)
            {
                PriceDefsMatrix[i] = new decimal[7];
            }

            ChannelHasProduct = false;
        }
        public decimal[][] PriceDefsMatrix { get; set; }

        public bool ChannelHasProduct { get; set; }
    }
    
    public class ChannelWithProducts
    {
        public Channel Channel { get; set; }
        public List<ProductWithPriceDefs> Products { get; set; }


        public ChannelWithProducts()
        {
            Products = new List<ProductWithPriceDefs>();
        }

        public static ChannelWithProducts Get(Guid channelId)
        {

            var channelWithProducts = new ChannelWithProducts();

            channelWithProducts.Channel = Channel.Get(new List<Guid>() { channelId })[0];

            var products = Product.GetAll();

            foreach (var product in products)
            {
                var productWithPriceDefs = new ProductWithPriceDefs
                {
                    Id = product.Id,
                    Name = product.Name
                };

                var pricedefs = PriceDefinition.Get(channelId, product.Id);

                foreach (var pricedef in pricedefs)
                    productWithPriceDefs.PriceDefsMatrix[pricedef.Hour][pricedef.Dow] = pricedef.Pps;

                productWithPriceDefs.ChannelHasProduct = pricedefs.Any(x => x.Pps != 0);

                channelWithProducts.Products.Add(productWithPriceDefs);
            }

            return channelWithProducts;

        }


        public static void AddOrUpdate(List<PriceDefinition> priceDefs)
        {

            PriceDefinition.AddOrUpdate(priceDefs);
        }
    }
}