using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace Adk.Handlers
{
    /// <summary>
    /// Summary description for GetChannelWave
    /// </summary>
    public class GetChannelWave : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            int duration = GetIntParameter(context, "d");
            int width = duration * 100;
            int height = GetIntParameter(context, "h");
            Guid channelId = GetGuidParameter(context, "cid");
            var date = GetDateTimeParameter(context, "s");
            var filePath = context.Server.MapPath($"waves/{channelId.ToString()}/{date.ToString("yyyy-MM-dd")}/{date.ToString("HHmm")}.png");
            context.Response.ContentType = "image/png";
            if (File.Exists(filePath))
            {                
                context.Response.WriteFile(filePath);
            }                           
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        private DateTime GetDateTimeParameter(HttpContext context, string hstring)
        {
            return DateTime.Parse(context.Request.QueryString[hstring]);
        }

        private Guid GetGuidParameter(HttpContext context, string hstring)
        {
            return new Guid((context.Request.QueryString[hstring]));
        }

        private static int GetIntParameter(HttpContext context, string hstring)
        {
            return int.Parse(context.Request.QueryString[hstring]);
        }
    }
}