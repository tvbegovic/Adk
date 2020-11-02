using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.IO;
using Newtonsoft.Json;

namespace Adk.Handlers
{
	/// <summary>
	/// Summary description for GetInfoForMonth
	/// </summary>
	public class GetInfoForMonth : IHttpHandler
	{

		public void ProcessRequest(HttpContext context)
		{
            Dictionary<int, bool> retval = new Dictionary<int, bool>();
            string pathToRead = Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
            int year;
            ReadQueryParameter(context, "year", out year, -1);
            if (year == -1)
            {
                context.AddError(new ArgumentException("use 'year' parameter"));
                return;
            }
            int month;
            ReadQueryParameter(context, "month", out month, -1);
            if (month == -1)
            {
                context.AddError(new ArgumentException("use 'month' parameter"));
                return;
            }
            string monthMask = string.Format("?????-{0:0000}{1:00}*", year, month);
            string[] filesWeNeed = Directory.GetFiles(pathToRead, monthMask);
            List<string> moreFiles = new List<string>(filesWeNeed).ConvertAll(f => Path.GetFileName(f));
            GetAudio.GetFilesFromDaily(year, month, pathToRead, moreFiles);
            filesWeNeed = moreFiles.ConvertAll(f => f.Contains("\\") ? FilesForDate.UnDaily(f) : f).ToArray();
            foreach (string f in filesWeNeed)
            {
                int day = int.Parse(Path.GetFileName(f).Substring(12, 2));
                if (!retval.ContainsKey(day))
                {
                    retval.Add(day, true);
                }
            }
            List<int> days = GetChunkedVideoDays(year, month, pathToRead);
            foreach (int day in days)
            {
                if (!retval.ContainsKey(day))
                {
                    retval.Add(day, true);
                }
            }
            context.Response.Write(JsonConvert.SerializeObject(retval, Formatting.Indented));
            context.Response.ContentType = "application/json";
        }

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}

        private void ReadQueryParameter(HttpContext context, string ParameterName, out int Target, int DefaultValue)
        {
            if (context.Request.QueryString[ParameterName] != null)
            {
                Target = int.Parse(context.Request.QueryString[ParameterName]); 
            }
            else
            {
                Target = DefaultValue;
            }
        }

        private List<int> GetChunkedVideoDays(int year, int month, string pathToRead)
        {
            List<int> retval = new List<int>();
            foreach (string directory in Directory.GetDirectories(pathToRead, string.Format("{0:0000}-{1:00}-*", year, month)))
            {
                string dirOnly = Path.GetFileName(directory);
                int day = int.Parse(dirOnly.Substring(8));
                if (!retval.Contains(day))
                {
                    retval.Add(day);
                }
            }
            return retval;


        }

        public static void Error(HttpContext context, string p)
        {
            context.Response.Write("Error: " + p);
            context.Response.ContentType = "text/plain";
        }
                
    }
}