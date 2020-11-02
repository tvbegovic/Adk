using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger.Modules.WebPlayer
{
	/// <summary>
	/// Summary description for FakeThumbs
	/// </summary>
	public class FakeThumbs : IHttpHandler
	{

		public void ProcessRequest(HttpContext context)
		{
			var sFrom = context.Request["from"];
			DateTime from;
			if(DateTime.TryParse(sFrom, out from))
			{
				var fileName = from.Minute % 10 == 0 ? "merge_from_ofoct1.jpg" : "merge_from_ofoct0.jpg";
				context.Response.ContentType = "image/jpeg";
				context.Response.WriteFile(context.Server.MapPath("/img/vp/" + fileName));
			}
			
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}
}
