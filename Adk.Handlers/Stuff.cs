#if !MATCHER_PCM_SDK
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using NLog;

#endif
using System;

using System.Net;
using System.Threading;

namespace Adk.Handlers
{
    public static class SystemConstants
    {
        /// <summary>
        /// When datapoint are collated, that is the number of seconds that they have to be apart to be counted as distinct events
        /// </summary>
        public const int MAXDISTANCE = 15;
        /// <summary>
        /// LastScanDb file name.
        /// </summary>
        public const string LAST_SCAN_DB = "LastScanDateDb.dat";
        /// <summary>
        /// Fake sample that represents selfmatching of one day of archive.
        /// </summary>
        public const string SELFMATCH_SAMPLE = "selfmatch 1";
        /// <summary>
        /// When self-matching, grow will go how many hashes to the left and right.
        /// </summary>
        public const int GROW_OFFSET = 4;
        /// <summary>
        /// When self-matching, stop growing clips after that many seconds.
        /// </summary>
        public const int MAX_GROW_SIZE = 3600;
        public static readonly DateTime ORIGIN_DATE = new DateTime(2010, 1, 1);
    }
    #if !MATCHER_PCM_SDK
    public class RetryStuff
    {
        #region Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        public delegate void WhatTodo();
        public delegate T WhatTodo<T>();
        public static T TrySeveralTimes<T>(WhatTodo<T> Task)
        {
            int Retries = 10;
            int RetryDelay = 1000;
            int retries = 0;
            while (true)
            {
                try
                {
                    return Task();
                }
                catch (Exception ex)
                {
                    retries++;
                    Log.Info<string, int>("Problem doing it {0}, try {1}", ex.Message, retries);
                    if (retries > Retries)
                    {
                        Log.Info("Giving up...");
                        throw;
                    }
                    Thread.Sleep(RetryDelay);
                }
            }
        }
        public static void TrySeveralTimes(WhatTodo Task)
        {
            int Retries = 10;
            int RetryDelay = 1000;
            int retries = 0;
            while (true)
            {
                try
                {
                    Task();
                    break;
                }
                catch (Exception ex)
                {
                    retries++;
                    Log.Info<string, int>("Problem doing it {0}, try {1}", ex.Message, retries);
                    if (retries > Retries)
                    {
                        Log.Info("Giving up...");
                        throw;
                    }
                    Thread.Sleep(RetryDelay);
                }
            }
        }
    }
    [Serializable]
    public class ArchiveFileInfo : IComparer<ArchiveFileInfo>
    {
        #region Logger
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        static public long RemoteFileLength(string mangledName)
        {
            WebRequest request = WebRequest.Create(new Uri(mangledName));
            request.Method = "HEAD";

            WebResponse response = request.GetResponse();
            long retval = response.ContentLength;
            response.Close();
            return retval;
        }
        static public DateTime FileNameToDateTime(string FileName)
        {
            if (!FileName.Contains("\\"))
            {
                int year = int.Parse(FileName.Substring(6, 4));
                int month = int.Parse(FileName.Substring(10, 2)); //  mjesec
                int day = int.Parse(FileName.Substring(12, 2)); //  dan
                int hour = int.Parse(FileName.Substring(14, 2)); //  sat
                int minutes = int.Parse(FileName.Substring(16, 2)); //  min
                int seconds = int.Parse(FileName.Substring(18, 2)); //  sec
                int mseconds = 0;
                int.TryParse(FileName.Substring(20, 3), out mseconds);
                return new DateTime(year, month, day, hour, minutes, seconds, mseconds);
            }
            else
            {
                // 2019\06\06\00000-013500.mp3
                int year = int.Parse(FileName.Substring(0, 4));
                int month = int.Parse(FileName.Substring(5, 2)); //  mjesec
                int day = int.Parse(FileName.Substring(8, 2)); //  dan
                int hour = int.Parse(FileName.Substring(17, 2)); //  sat
                int minutes = int.Parse(FileName.Substring(19, 2)); //  min
                int seconds = int.Parse(FileName.Substring(21, 2)); //  sec
                int mseconds = 0;
                int.TryParse(FileName.Substring(23, 3), out mseconds);
                return new DateTime(year, month, day, hour, minutes, seconds, mseconds);
            }
        }
        public static string DateTimeToFileName(DateTime Start)
        {
            return string.Format("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}",
                Start.Year, Start.Month, Start.Day, Start.Hour, Start.Minute, Start.Second);
        }
        //public static List<string> GetArchiveForTime(string ArchiveDirectory, DateTime Start, DateTime End, TimeSpan Overflow)
        //{
        //    List<ArchiveFileInfo> filesOnDisk;
        //    filesOnDisk = GetArchiveFiles(ArchiveDirectory);
        //    List<ArchiveFileInfo> filtered = FilterByTime(filesOnDisk, Start, End, Overflow);
        //    return new List<string>(filtered.ConvertAll(fi => ArchiveDirectory + "\\" + fi.FileName));
        //}
        public static List<ArchiveFileInfo> FilterByTime(List<ArchiveFileInfo> filesOnDisk, DateTime Start, DateTime End, TimeSpan Overflow)
        {
            return filesOnDisk.FindAll(f => f.End + Overflow >= Start && f.Start - Overflow <= End);
        }
        /// <summary>
        /// Returns the sorted collection of all files in the directory.
        /// </summary>
        /// <param name="ArchiveDirectory"></param>
        /// <returns></returns>
        //public static List<ArchiveFileInfo> GetArchiveFiles(string ArchiveDirectory)
        //{
        //    List<ArchiveFileInfo> filesOnDisk;
        //    DirectoryInfo di = new DirectoryInfo(ArchiveDirectory);
        //    Log.Info<string>("Loading files for {0}", ArchiveDirectory);
        //    FileData[] fileInfos = FastDirectoryEnumerator.GetFiles(ArchiveDirectory, "*.hash", SearchOption.TopDirectoryOnly);
        //    Log.Info("directory read completed");
        //    filesOnDisk = new List<ArchiveFileInfo>();
        //    foreach (FileData fi in fileInfos)
        //    {
        //        ArchiveFileInfo afi = new ArchiveFileInfo();
        //        Log.Debug<string>("name in = {0}", fi.Name);
        //        afi.FileName = fi.Name;
        //        Log.Debug<string>("name out = {0}", afi.FileName);
        //        afi.Length = (int)fi.Size;
        //        filesOnDisk.Add(afi);
        //    }
        //    Log.Info<int>("{0} files loaded from disk", filesOnDisk.Count);
        //    List<ArchiveFileInfo> filesInDb = new List<ArchiveFileInfo>();
        //    IHashDatabase db = new HashDatabaseFolderPacked(ArchiveDirectory);
        //    foreach (PackedFileInfo packedFile in db.GetAllPackedFiles())
        //    {
        //        ArchiveFileInfo afi = new ArchiveFileInfo(packedFile.FileDateTime, packedFile.Length);
        //        filesInDb.Add(afi);
        //    }
        //    Log.Info<int>("{0} files loaded from database", filesInDb.Count);
        //    filesInDb.Sort((f1, f2) => f1.Start.CompareTo(f2.Start));
        //    IComparer<ArchiveFileInfo> comp = new ArchiveFileInfo();
        //    List<ArchiveFileInfo> filesToKeep=new List<ArchiveFileInfo>();
        //    foreach (ArchiveFileInfo fileToTest in filesOnDisk)
        //    {
        //        if (filesInDb.BinarySearch(fileToTest, comp) <= -1)
        //        {
        //            filesToKeep.Add(fileToTest);
        //        }
        //    }
        //    filesInDb.AddRange(filesToKeep);
        //    filesInDb.Sort((f1, f2) => f1.Start.CompareTo(f2.Start));
        //    Log.Info("Sorting done");
        //    return filesInDb;
        //}
        public static List<ArchiveFileInfo> CreateFromStrips(List<string> ArchiveStrips)
        {
            List<ArchiveFileInfo> filesToLoad = new List<ArchiveFileInfo>();
            foreach (string stripURL in ArchiveStrips)
            {
                ArchiveFileInfo afi = new ArchiveFileInfo();
                string onlyFile = Path.GetFileName(new Uri(stripURL).LocalPath);
                afi.FileName = onlyFile;
                afi.Length = (int)RemoteFileLength(stripURL);
                filesToLoad.Add(afi);
            }
            return filesToLoad;
        }
        public int Compare(ArchiveFileInfo f1, ArchiveFileInfo f2)
        {
            return f1.Start.CompareTo(f2.Start);
        }
        public string FileName
        {
            get
            {
                return string.Format("{0:00000}-{1}{2}",
                    _index, DateTimeToFileName(_start),
                    Extension);
            }
            set
            {
                _index = int.Parse(value.Substring(0, 5));
                _start = FileNameToDateTime(value);
                string lowercase = value.ToLower();
                if (lowercase.EndsWith(".aac.hash"))
                {
                    _type = 0;
                }
                else if (lowercase.EndsWith(".mp3.hash"))
                {
                    _type = 1;
                }
                else if (lowercase.EndsWith(".wma.hash"))
                {
                    _type = 2;
                }
                else if (lowercase.EndsWith(".ogg.hash"))
                {
                    _type = 3;
                }
                else if (lowercase.EndsWith(".wmv.hash"))
                {
                    _type = 5;
                }
                else if (lowercase.EndsWith(".wmv.hash"))
                {
                    _type = 5;
                }
                else if (lowercase.EndsWith(".mp2.hash"))
                {
                    _type = 6;
                }
                else if (lowercase.EndsWith(".hash"))
                {
                    _type = 4;
                }
                else
                {
                    throw new ArgumentException("unknown file type");
                }
                Log.Debug<byte>("type = {0}", _type);
            }
        }
        internal string Extension
        {
            get
            {
                Log.Debug<byte>("Extension; type = {0}", _type);
                switch (_type)
                {
                    case 0:
                        return ".aac.hash";
                    case 1:
                        return ".mp3.hash";
                    case 2:
                        return ".wma.hash";
                    case 3:
                        return ".ogg.hash";
                    case 4:
                        return ".hash";
                    case 5:
                        return ".wmv.hash";
                    case 6:
                        return ".mp2.hash";
                }
                throw new Exception("unknown file type");
            }
        }
        /// <summary>
        /// Parsed serial number of the file.
        /// </summary>
        private int _index;
        /// <summary>
        /// Parsed starting time of the file.
        /// </summary>
        private DateTime _start;
        public ArchiveFileInfo()
        {
        }
        public ArchiveFileInfo(DateTime StartDateTime, int Length)
        {
            _start = StartDateTime;
            _length = Length;
            _type = 4;
        }
        public DateTime Start
        {
            get { return _start; }
        }
        public DateTime End
        {
            get
            {
                DateTime retval = Start;
                retval = retval.AddSeconds(_length / 4.0 / 80.0);
                return retval;
            }
        }
        public TimeSpan Duration
        {
            get
            {
                return End - Start;
            }
        }

        /// <summary>
        /// Length of the file in BYTES.
        /// </summary>
        private int _length;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }
        /// <summary>
        /// Encoded file type (extension wise):
        /// 0: aac
        /// 1: mp3
        /// 2: wma
        /// 3: ogg
        /// 4: no type, plain .hash
        /// </summary>
        private byte _type;

        public static string NormalNameFromDailyName(string f)
        {
            return f.Substring(11, 5) + "-" + f.Substring(0, 4) + f.Substring(5, 2) + f.Substring(8, 2) + f.Substring(17);
        }
    }
    //public interface IMatcherControl
    //{
    //    bool ReportResult(Guid taskToken, Dictionary<int, List<DataPoint>> collatedPoints, string HostName);
    //    void Start();
    //    void Stop();
    //    void InsertMatcherTask(Guid taskToken, List<string> samples, string ChannelID, int Year, int Month, int Day);
    //    bool TaskInQueue(Guid taskToken);
    //    void GetMatcherTask(out Guid taskToken, out List<string> samples, out List<string> archiveStrips, out object[] Params);
    //    void SetLastScanDateForChannel(string ChannelID, int Year, int Month, int Day);
    //    void InsertSample(string SampleFileName);
    //}
#endif
    public class Performancer
    {
        Logger _loggerToUse;
        int _reportEveryNth;
        int _reportInterval;
        string _tag;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="LoggerToUse"></param>
        /// <param name="ReportEveryNth">report everty nth invocation</param>
        /// <param name="ReportInterval">report interval in seconds</param>
        public Performancer(Logger LoggerToUse, int ReportEveryNth, int ReportInterval, string Tag)
        {
            _loggerToUse = LoggerToUse;
            _reportEveryNth = ReportEveryNth;
            _reportInterval = ReportInterval;
            _tag = Tag;
        }
        double _accumulatedTime;
        int _count;
        DateTime _lastReportTime;
        public void DoReport(double MeasuredTime)
        {
            _count++;
            _accumulatedTime += MeasuredTime;
            DateTime now = DateTime.Now;
            bool show = false;
            if (_reportEveryNth != -1)
            {
                if (_count % _reportEveryNth == 0)
                {
                    show = true;
                }
            }
            if (_reportInterval != -1)
            {
                if (now.AddSeconds(-_reportInterval) > _lastReportTime)
                {
                    show = true;
                }
            }
            if (show)
            {
                string format=string.Format("{3}: count {0} totaltime {1:0.000} percall {2:0.000}", _count, _accumulatedTime, _accumulatedTime / _count, _tag);
                _loggerToUse.Debug(format);
                _lastReportTime = now;
            }
        }
    }
}
