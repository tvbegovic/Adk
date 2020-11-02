using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Model
{
	/// <summary>
	/// New class that support price designer 2 with dayparts that occupy less than a hour or more than a hour in a day and where one hour can contain
	/// multiple dayparts (v11 had one part per hour)
	/// </summary>
    public class ChannelWithPriceDefinitionsV2
    {
        public Channel Channel { get; set; }
        public List<PriceDefinition2> PriceDefinitions { get; set; }
        public List<DayPart> DayParts { get; set; }
        public List<DayPartSet> DayPartSets { get; set; }
        public List<ProductDayPart> ProductDayParts { get; set; }
        public List<Product> Products { get; set; }
        public string User { get; set; }
        public List<ProductCode> ProductCodes { get; set; }

        
        public ChannelWithPriceDefinitionsV2()
        {
            
            
        }

        public static ChannelWithPriceDefinitionsV2 Get(Guid channelId, string userId)
        {

            var result = new ChannelWithPriceDefinitionsV2();

            result.Channel = Channel.Get(new List<Guid>() { channelId })[0];
            result.DayPartSets = DayPartSet.GetForUser(userId.ToString());
            if (result.DayPartSets != null)
                result.DayParts = result.DayPartSets.SelectMany(s => s.Parts).ToList();

            result.PriceDefinitions = PriceDefinition2.Get(channelId,userId );
			            
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

   
}
