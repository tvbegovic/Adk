using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using DatabaseCommon;
using AdK.Tagger.Model;
using AdK.Tagger.Services;

namespace AdK.Tagger
{
	/// <summary>
	/// Summary description for SpotUpload
	/// </summary>
	public class SpotUploadHandler : IHttpHandler
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public void ProcessRequest(HttpContext context)
        {
            try {

                string originalName = "";
                string filename = "";
                string sampleId = null;

                if (context.Request.Files.Count == 1) // Handles only one spot at a time
                {
                    var user = GetUser(context);
                    if (user == null) {
                        Log.Warn("Spot upload was called without user context");
                        throw new UnauthorizedAccessException("You are not authorize to preform this operation");
                    }

                    var pkConnector = new PkConnector(user);

                    HttpPostedFile file = context.Request.Files[0];
                    originalName = file.FileName;

                    filename = SpotUpload.GetNewSpotFilename();
                    string path = SpotUpload.GetSpotPath(filename);
                    file.SaveAs(path);

                    Log.Info(string.Format("Uploading: '{0}' Path: '{1}'", originalName, path));


                    string spotUrl = SpotUpload.GetSpotUrl(filename);
                    sampleId = pkConnector.AddSampleFromURL(spotUrl);

                    var defaults = UserSettings.Get(user.Id).Where(s => s.Module == UserSettingModule.Defaults.ToString());
                    var defaultBrand = defaults.FirstOrDefault(d => d.Key == "defaultBrand");
                    var defaultCategory = defaults.FirstOrDefault(d => d.Key == "defaultCategory");
                    var defaultAdvertiser = defaults.FirstOrDefault(d => d.Key == "defaultAdvertiser");

                    int extensionIndex = originalName.LastIndexOf('.');

                    var song = new Model.Song
                    {
                        Title = extensionIndex != -1 ? originalName.Substring(0, extensionIndex) : originalName,
                        PksId = sampleId,
                        Filename = originalName,
                        UserId = user.Id,
                        Brand = defaultBrand != null ? defaultBrand.Value : "",
                        Album = defaultCategory != null ? defaultCategory.Value : "",
                        Performer = defaultAdvertiser != null ? defaultAdvertiser.Value : ""
                    };

                    song.Create();
                    Log.Info(string.Format("Created song '{0}' with PKSID '{1}'", song.Id, song.PksId));
                }

                context.Response.ContentType = "text/plain";
                context.Response.Write(new JavaScriptSerializer().Serialize(new
                {
                    originalName = originalName,
                    filename = filename,
                    sampleId = sampleId
                }));
            }
            catch(Exception err) {
                App.Log.Error(err);
                throw;
            }
		}

		Model.TaggerUser GetUser(HttpContext context)
		{
			string deviceId = context.Request.Cookies["deviceId"].Value;
			string token = context.Request.Cookies["token"].Value;
			return Model.TaggerUser.GetByToken(deviceId, token);
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