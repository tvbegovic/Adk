using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Adk.Handlers
{
    /// <summary>
    /// Summary description for Getm3u8
    /// </summary>
    public class Getm3u8 : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string date = context.Request.QueryString["date"];
            DateTime myDate = FilesForDate.ParseHttpDate(date);
            string start = context.Request.QueryString["start"];
            string end = context.Request.QueryString["end"];
            DateTime startTime = myDate.Add(FilesForDate.ParseHttpTime(start));
            DateTime endTime = myDate.Add(FilesForDate.ParseHttpTime(end));

            string pathToRead = Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
            var filePath = Path.Combine(pathToRead, startTime.ToString("yyyy-MM-dd"), startTime.ToString("HHmm") + ".m3u8");
            context.Response.ContentType = "application/x-mpegURL";
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