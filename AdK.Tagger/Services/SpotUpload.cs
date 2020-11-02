using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Web;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.Collections.Generic;
using AdK.Tagger.Model;

namespace AdK.Tagger.Services
{
    public class SpotUpload
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public static string GetNewSpotFilename()
        {
            return String.Format("{0}.mp3", Guid.NewGuid().ToString().ToString());
        }

        public static string GetSpotPath(string spotFilename)
        {
            string folderPath = HttpContext.Current.Server.MapPath("~/spots_upload");
            if (!Directory.Exists(folderPath)) {
                try {
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex) {
                    Log.Error(string.Format("Can't create the folder {0}. Grant creation rights or create manually then allow ASP.NET write access to it.", folderPath));
                    Log.Error(ex.ToString());
                }
            }

            string path = Path.Combine(folderPath, spotFilename);
            return path;
        }

        public static string GetSpotUrl(string spotFilename)
        {
            return new Uri(HttpContext.Current.Request.Url, "/spots_upload/" + spotFilename).ToString();
        }

        public static List<SampleStatus> UpdateSampleStatuses(TaggerUser user, List<string> sampleIds)
        {
            var pkConnector = new PkConnector(user);
            var statuses = new List<SampleStatus>();

            foreach (var sampleId in sampleIds)
            {
                var status = pkConnector.GetSampleStatus(sampleId);
                if (status.Status == PkSampeStatusType.OK) {
                    Song.UpdateDurationAndStatus(sampleId, status.Duration, SongStatus.Processed);
                }

                statuses.Add(status);
            }

            return statuses;
        }

	}
}