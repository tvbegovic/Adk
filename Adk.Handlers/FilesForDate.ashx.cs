using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NLog;
using System.IO;
using System.Globalization;

namespace Adk.Handlers
{
	/// <summary>
	/// Summary description for FilesForDate
	/// </summary>
	public class FilesForDate : IHttpHandler
	{
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                context.Response.ContentType = "text/plain";
                bool size = false;
                bool created = false;
                bool modified = false;
                bool deep = false;
                if (context.Request.QueryString["opt"] != null)
                {
                    Log.Info<string>("opt={0}", context.Request.QueryString["opt"]);
                    if (context.Request.QueryString["opt"].Contains("s"))
                    {
                        size = true;
                    }
                    if (context.Request.QueryString["opt"].Contains("c"))
                    {
                        created = true;
                    }
                    if (context.Request.QueryString["opt"].Contains("m"))
                    {
                        modified = true;
                    }
                    if (context.Request.QueryString["opt"].Contains("d"))
                    {
                        deep = true;
                    }
                }
                if (context.Request.QueryString["get"] != null)
                {
                    string pathToRead = RequestFolder(context);
                    Log.Info<string>("reading folder '{0}'", pathToRead);
                    if (Directory.Exists(pathToRead))
                    {
                        Log.Info<string>("exists: '{0}'", pathToRead);
                        string mask = "*." + context.Request.QueryString["get"];
                        int charsInPath = pathToRead.Length;
                        foreach (var fi in FastDirectoryEnumerator.EnumerateFiles(pathToRead, mask, deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                        {
                            context.Response.Write(fi.Path.Substring(charsInPath + 1).Replace('\\', '/'));
                            if (size)
                            {
                                context.Response.Write(string.Format("\t{0}", fi.Size));
                            }
                            if (created)
                            {
                                context.Response.Write(string.Format("\t{0}", fi.CreationTime.ToString(CultureInfo.InvariantCulture)));
                            }
                            if (modified)
                            {
                                context.Response.Write(string.Format("\t{0}", fi.LastWriteTime.ToString(CultureInfo.InvariantCulture)));
                            }
                            context.Response.Write("\r\n");
                        }
                        Log.Info("files read");
                    }
                }
                else
                {
                    string date = context.Request.QueryString["date"];
                    DateTime myDate = ParseHttpDate(date);
                    string channel = context.Request.QueryString["channel"];
                    if (channel == null)
                    {
                        channel = "";
                    }
                    string pathToRead = Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
                    pathToRead = Path.Combine(pathToRead, channel);
                    Log.Info<string>("pathToRead={0}", pathToRead);
                    string hourstring = context.Request.QueryString["hour"];
                    int? hour = null;
                    if (hourstring != null)
                    {
                        hour = int.Parse(hourstring);
                    }
                    string mask = string.Format("?????-{0:0000}{1:00}{2:00}*", myDate.Year, myDate.Month, myDate.Day);
                    if (hour.HasValue)
                    {
                        mask = mask.Replace("*", string.Format("{0:00}*", hour));
                    }
                    foreach (string f in Directory.GetFiles(pathToRead, mask, deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        if (!f.EndsWith(".fmap") && !f.EndsWith(".$temp$"))
                        {
                            if (!deep)
                            {
                                context.Response.Write(Path.GetFileName(f));
                            }
                            else
                            {
                                context.Response.Write(f.ToLower().Replace(pathToRead.ToLower(), "").Substring(1).Replace('\\', '/'));
                            }
                            context.Response.Write("\r\n");
                        }
                    }
                    List<string> files = new List<string>();
                    GetAudio.GetFilesFromDaily(myDate, pathToRead, files);
                    foreach (string f in files)
                    {
                        context.Response.Write(UnDaily(f));
                        context.Response.Write("\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                context.Response.Write(string.Format("error!\r\n{0}\r\n{1}", ex.Message, ex.StackTrace));
                Log.Error<string, string>("exception!\r\n{0}\r\n{1}", ex.Message, ex.StackTrace);
                Log.Error<string>("problem was for folder '{0}'", context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
                throw;
            }
        }
        public static string UnDaily(string f)
        {
            // 2019\06\06\00000-000001.mp3 -> 00000-20190606000001.mp3
            return f.Substring(11, 5) + "-" + f.Substring(0, 4) + f.Substring(5, 2) + f.Substring(8, 2) + f.Substring(17);
        }
        public static DateTime ParseHttpDate(string date)
        {
            string[] dateParts = date.Split(new char[] { '-' });
            DateTime myDate = new DateTime(int.Parse(dateParts[0]),
              int.Parse(dateParts[1]),
              int.Parse(dateParts[2])
            );
            return myDate;
        }
        internal static TimeSpan ParseHttpTime(string Time)
        {
            string[] timeParts = Time.Split(":.".ToCharArray());
            TimeSpan myTime;
            if (timeParts.Length == 3)
            {
                myTime = new TimeSpan(int.Parse(timeParts[0]),
                  int.Parse(timeParts[1]),
                  int.Parse(timeParts[2]));
            }
            else
            {
                myTime = new TimeSpan(0,
                  int.Parse(timeParts[0]),
                  int.Parse(timeParts[1]),
                  int.Parse(timeParts[2]),
                  int.Parse(timeParts[3]));
            }
            return myTime;
        }
        private static int GetIntParameter(HttpContext context, string hstring)
        {
            return int.Parse(context.Request.QueryString[hstring]);
        }
        internal static string RequestFolder(HttpContext context)
        {
            return Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
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