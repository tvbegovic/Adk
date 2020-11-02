using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
    public class ChannelWithPriceDefs11
    {
        public Channel Channel { get; set; }
        public List<PriceDefinition11> PriceDefinitions { get; set; }
        public List<DayPart> DayParts { get; set; }
        public List<DayPartSet> DayPartSets { get; set; }
        public List<ProductDayPart> ProductDayParts { get; set; }
        public List<Product> Products { get; set; }
        public string User { get; set; }
        public List<ProductCode> ProductCodes { get; set; }

        public DayPart[][] PriceDefsMatrix { get; set; }

        public ChannelWithPriceDefs11()
        {
            PriceDefsMatrix = new DayPart[24][];
            for (int i = 0; i < 24; i++)
            {
                PriceDefsMatrix[i] = new DayPart[7];
                for (int j = 0; j < PriceDefsMatrix[i].Length; j++)
                {
                    PriceDefsMatrix[i][j] = new DayPart();
                }
            }
            
        }

        public static ChannelWithPriceDefs11 Get(Guid channelId, string userId, bool isV2 = false)
        {

            var result = new ChannelWithPriceDefs11();

            result.Channel = Channel.Get(new List<Guid>() { channelId })[0];
            result.DayPartSets = DayPartSet.GetForUser(userId.ToString());
            if (result.DayPartSets != null)
                result.DayParts = result.DayPartSets.SelectMany(s => s.Parts).ToList();

            result.PriceDefinitions = PriceDefinition11.Get(channelId,userId );

            foreach (var pricedef in result.PriceDefinitions)
                result.PriceDefsMatrix[pricedef.Hour][pricedef.Day] = pricedef.DayPart;

            var user = TaggerUser.Get(userId.ToString());
            if (user != null)
                result.User = user.Name;

            result.ProductDayParts = ProductDayPart.Get(channelId,userId);
            result.Products = ProductDayPart.GetProducts(channelId,userId);
            
            result.ProductCodes = result.ProductDayParts.GroupBy(pd => new { pd.ProductId, pd.ProductCode })
                                    .Select(g => new ProductCode
                                    {
										Id = Guid.NewGuid(),
                                        Code = g.Key.ProductCode,
                                        ProductId = g.Key.ProductId,
                                        DayParts = g.OrderBy(pd=>pd.DayPartId).ToList(),
                                        DurationMax = g.Min(pd=>pd.DurationMax),
                                        PriceMode = g.Min(pd=>pd.PriceMode)
                                    }).ToList();

            return result;
        }
    }

    /// <summary>
    /// Helper class for matrix on price designer products tab
    /// </summary>
    public class ProductCode
    {
		public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Code { get; set; }
        public int? DurationMax { get; set; }
        public List<ProductDayPart> DayParts { get; set; }
        public int? PriceMode { get; set; }
    }
}
