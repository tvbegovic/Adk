using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using NLog;

namespace Adk.Handlers
{
	/// <summary>
	/// Summary description for GetAudio
	/// </summary>
	public class GetAudio : IHttpHandler
	{

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void ProcessRequest(HttpContext context)
        {
            //  get list of all files
            string date = context.Request.QueryString["date"];
            string start = context.Request.QueryString["start"];
            string end = context.Request.QueryString["end"];
            DateTime myDate = FilesForDate.ParseHttpDate(date);
            DateTime startTime = myDate.Add(FilesForDate.ParseHttpTime(start));
            DateTime endTime = myDate.Add(FilesForDate.ParseHttpTime(end));
            if (endTime < startTime)
            {
                endTime = endTime.AddDays(1);
            }
            string pathToRead = Path.GetDirectoryName(context.Server.MapPath(HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath));
            Log.Info<string, DateTime, DateTime>("export request for '{0}' from {1} to {2}", pathToRead, startTime, endTime);
            List<string> allFiles = new List<string>();
            AddFpFilesForDate(myDate, pathToRead, allFiles);
            AddFpFilesForDate(myDate.AddDays(1), pathToRead, allFiles);
            //  for files that are in interval, prepare cutlist
            List<RecordedFileInfo> cutlist = allFiles.ConvertAll<RecordedFileInfo>(f => new RecordedFileInfo { Name = f });
            foreach (RecordedFileInfo r in cutlist)
            {
                r.Start = ArchiveFileInfo.FileNameToDateTime(r.Name);
            }
            cutlist.Sort((r1, r2) => r1.Start.CompareTo(r2.Start));
            for (int n = 0; n < cutlist.Count - 1; n++)
            {
                cutlist[n].Duration = (int)(cutlist[n + 1].Start - cutlist[n].Start).TotalMilliseconds;
            }
            cutlist.RemoveAll(r => r.End <= startTime || r.Start >= endTime);
            foreach (RecordedFileInfo r in cutlist)
            {
                r.CutStart = (startTime - r.Start).TotalSeconds;
                if (endTime >= r.End)
                {
                    r.CutEnd = (double)r.Duration / 1000.0;
                }
                else
                {
                    r.CutEnd = (double)r.Duration / 1000.0 - (r.End - endTime).TotalSeconds;
                }
                startTime = r.End;
            }
            Log.Info<int>("{0} files prepared in cutlist", cutlist.Count);
            if (context.Request.QueryString["format"] == "mp3")
            {
                //  invoke exporter, stream directly to web response stream
                context.Response.ContentType = "audio/mpeg";
                //  feed cutlist to exporter
                var exporter = new AudioExport();
                exporter.AudioBitrate = 96;
                exporter.AudioSampleRate = 44100;
                exporter.Channels = 2;
                exporter.OutputStream = context.Response.OutputStream;
                foreach (RecordedFileInfo r in cutlist)
                {
                    string filename = Path.Combine(pathToRead, r.Name);
                    exporter.AddFile(filename, r.CutStart, r.CutEnd);
                }
                Log.Info("exporter started");
                exporter.Export();
                exporter.Close();
            }
            else
            {
                context.Response.ContentType = "text/plain";
                foreach (RecordedFileInfo r in cutlist)
                {
                    context.Response.Write(string.Format("{0},{1}:{2}-{3}\r\n", r.Name, r.Duration, r.CutStart, r.CutEnd));
                }
            }
            Log.Info("exporter request done");
            //  the end
        }

        private static void AddFpFilesForDate(DateTime myDate, string pathToRead, List<string> allFiles)
        {
            GetFilesFromArchive(myDate, pathToRead, allFiles);
            GetFilesFromDaily(myDate, pathToRead, allFiles);
        }
        public static void GetFilesFromDaily(DateTime myDate, string pathToRead, List<string> allFiles)
        {
            string mask = "?????-*";
            string folder = string.Format("{0}\\{1:0000}\\{2:00}\\{3:00}", pathToRead, myDate.Year, myDate.Month, myDate.Day);
            if (!Directory.Exists(folder))
            {
                return;
            }
            foreach (string f in Directory.GetFiles(folder, mask))
            {
                if (!f.EndsWith(".fmap") && !f.EndsWith(".$temp$"))
                {
                    allFiles.Add(f.Substring(pathToRead.Length + 1));
                }
            }
        }
        internal static void GetFilesFromDaily(int year, int month, string pathToRead, List<string> allFiles)
        {
            string mask = "?????-*";
            string folder = string.Format("{0}\\{1:0000}\\{2:00}", pathToRead, year, month);
            if (!Directory.Exists(folder))
            {
                return;
            }
            foreach (string f in Directory.GetFiles(folder, mask, SearchOption.AllDirectories))
            {
                if (!f.EndsWith(".fmap") && !f.EndsWith(".$temp$"))
                {
                    allFiles.Add(f.Substring(pathToRead.Length + 1));
                }
            }
        }
        public static void GetFilesFromArchive(DateTime myDate, string pathToRead, List<string> allFiles)
        {
            if (!Directory.Exists(pathToRead))
            {
                return;
            }
            string mask = string.Format("?????-{0:0000}{1:00}{2:00}*", myDate.Year, myDate.Month, myDate.Day);
            foreach (string f in Directory.GetFiles(pathToRead, mask))
            {
                if (!f.EndsWith(".fmap") && !f.EndsWith(".$temp$"))
                {
                    allFiles.Add(Path.GetFileName(f));
                }
            }
        }
        class RecordedFileInfo
        {
            public string Name;
            public DateTime Start;
            public DateTime End
            {
                get
                {
                    return Start.AddMilliseconds(Duration);
                }
            }
            public int Duration;
            public double CutStart;
            public double CutEnd;
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