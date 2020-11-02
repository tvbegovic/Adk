using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Services;
using System.IO;
using Newtonsoft.Json;

namespace Adk.Handlers
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class GetInfoForDate : IHttpHandler
    {
        /*
            0 - none
            1 - full
            2 - startonly
            3 - endonly
            4 - middlemissing
            5 - middle
         */
        public void ProcessRequest(HttpContext context)
        {
            Dictionary<int, int> retval = new Dictionary<int, int>();
            for (int n = 0; n < 24; n++)
            {
                retval.Add(n, 0);
            }
            string pathToRead = Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
            int year;
            ReadQueryParameter(context, "year", out year, -1);
            if (year == -1)
            {
                GetInfoForMonth.Error(context, "use 'year' parameter");
                return;
            }
            int month;
            ReadQueryParameter(context, "month", out month, -1);
            if (month == -1)
            {
                GetInfoForMonth.Error(context, "use 'month' parameter");
                return;
            }
            int day;
            ReadQueryParameter(context, "day", out day, -1);
            if (day == -1)
            {
                GetInfoForMonth.Error(context, "use 'day' parameter");
                return;
            }
            string dateMask = string.Format("?????-{0:0000}{1:00}{2:00}*", year, month, day);
            string[] filesWeNeed = Directory.GetFiles(pathToRead, dateMask);
            List<string> moreFiles = new List<string>(filesWeNeed).ConvertAll(f => Path.GetFileName(f));
            GetAudio.GetFilesFromDaily(new DateTime(year, month, day, 0, 0, 0), pathToRead, moreFiles);
            filesWeNeed = moreFiles.ConvertAll(f => f.Contains("\\") ? FilesForDate.UnDaily(f) : f).ToArray();
            List<TimeSpan> videoFragments = GetChunkedVideoHours(pathToRead, year, month, day);
            Dictionary<int, bool[]> scratchHours = new Dictionary<int, bool[]>();
            for (int n = 0; n < 24; n++)
            {
                scratchHours.Add(n, new bool[12]);
            }
            foreach (string f in filesWeNeed)
            {
                int hour = int.Parse(f.Substring(14, 2));
                int minute = int.Parse(f.Substring(16, 2));
                scratchHours[hour][minute / 5] = true;
            }
            foreach (TimeSpan fragmentStart in videoFragments)
            {
                scratchHours[fragmentStart.Hours][fragmentStart.Minutes / 5] = true;
            }
            for (int n = 0; n < 24; n++)
            {
                bool[] b = scratchHours[n];
                if (AllFalse(b))
                {
                    //  nothing here
                    retval[n] = 0;
                }
                else if (AllTrue(b))
                {
                    //  all there
                    retval[n] = 1;
                }
                else if (b[0])
                {
                    if (b[11])
                    {
                        //  start and end
                        retval[n] = 4;
                    }
                    else
                    {
                        //  start only
                        retval[n] = 2;
                    }
                }
                else if (b[11])
                {
                    //  end only
                    retval[n] = 3;
                }
                else
                {
                    retval[n] = 5;
                }
            }
            context.Response.Write(JsonConvert.SerializeObject(retval, Formatting.Indented));
            context.Response.ContentType = "application/json";
        }
        private List<TimeSpan> GetChunkedVideoHours(string pathToRead, int year, int month, int day)
        {
            string dayFolder = string.Format("{0}\\{1:0000}-{2:00}-{3:00}", pathToRead, year, month, day);
            List<TimeSpan> retval = new List<TimeSpan>();
            if (Directory.Exists(dayFolder))
            {
                foreach (string fiveMinuteFolder in Directory.GetDirectories(dayFolder))
                {
                    string folderOnly = Path.GetFileName(fiveMinuteFolder);
                    int hour = int.Parse(folderOnly.Substring(0, 2));
                    int minute = int.Parse(folderOnly.Substring(3, 2));
                    retval.Add(new TimeSpan(hour, minute, 0));
                }
            }
            return retval;
        }
        private bool AllTrue(bool[] b)
        {
            for (int n = 0; n < b.Length; n++)
            {
                if (!b[n])
                {
                    return false;
                }
            }
            return true;
        }
        private bool AllFalse(bool[] b)
        {
            for (int n = 0; n < b.Length; n++)
            {
                if (b[n])
                {
                    return false;
                }
            }
            return true;
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
    }
}
