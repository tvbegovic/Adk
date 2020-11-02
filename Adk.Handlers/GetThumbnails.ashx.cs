using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace Adk.Handlers
{
    /// <summary>
    /// Summary description for GetThumbnails
    /// </summary>
    public class GetThumbnails : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string date = context.Request.QueryString["date"];
            DateTime myDate = FilesForDate.ParseHttpDate(date);
            string start = context.Request.QueryString["start"];
            string end = context.Request.QueryString["end"];            
            DateTime startTime = myDate.Add(FilesForDate.ParseHttpTime(start));
            startTime = startTime.AddMinutes(-1 * (startTime.Minute % 5));
            DateTime endTime = myDate.Add(FilesForDate.ParseHttpTime(end));
            
            string pathToRead = Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
            var filePath = Path.Combine(pathToRead, "thumbnails", startTime.ToString("yyyy-MM-dd"), startTime.ToString("HHmm") + ".jpg");
            context.Response.ContentType = "image/png";
            if (File.Exists(filePath))
                context.Response.WriteFile(filePath);

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