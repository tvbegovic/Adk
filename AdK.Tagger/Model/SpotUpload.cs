using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Web;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace AdKontrol.Tagger.Model
{
    public class SpotUpload
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public static string GetNewSpotFilename()
        {
            string fileId = Path.GetRandomFileName();
            return fileId + ".mp3";
        }
        public static string GetSpotPath(string spotFilename)
        {
            string folderPath = HttpContext.Current.Server.MapPath("~/spots_upload");
            if (!Directory.Exists(folderPath)) {
                try {
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex) {
                    Log.Error(string.Format("Canot create the folder {0}. Grant creation rights or create manually then allow ASP.NET write access to it.", folderPath));
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

        public static string ProcessSample(string apiKey, string sampleUrl)
        {
            Log.Info(string.Format("Sending sample {0} for processing with API Key {1}", sampleUrl, apiKey));
            string sampleId;
            using (var service = new SpotUploadService.ServiceProviderSoapClient())
                sampleId = service.AddSampleFromURL(apiKey, sampleUrl);
            Log.Info(string.Format("AddSampleFromURL returned sampleId " + sampleId));

            return sampleId;
        }
        public static string GetSampleStatus(string apiKey, string sampleId)
        {
            using (var service = new SpotUploadService.ServiceProviderSoapClient()) {
                string status = service.GetSampleStatus(apiKey, sampleId);

                string statusOk = "OK ";
                if (status.StartsWith(statusOk)) {
                    int iDuration = int.Parse(status.Substring(statusOk.Length));
                    decimal dDuration = (decimal)iDuration / 1000;
                    Log.Info(string.Format("Setting song duration " + sampleId + ", " + dDuration));
                    Song.UpdateDurationAndStatus(sampleId, dDuration);
                }

                return status;
            }
        }

        #region Authentication
        public static string Login(TaggerUser user)
        {
            string login = user.Id.Replace("-", "");

            if (String.IsNullOrWhiteSpace(user.Pkspwd))
            {
                string secretHashSalt = "f5rd94g2ds";
					 string password = Hash( secretHashSalt + login ).Substring( 0, 20 ); //Take first 20 characters form string

                if (_CreateUser(login, password)) {
                    user.UpdatePksd(password);
                    return _Login(login, password);
                }
            }
            else {
                return _Login(login, user.Pkspwd);
            }

            return null;

		}
		private static string _Login( string username, string password )
		{
			Log.Info( string.Format( "Logging in {0}", username ) );
			string responsebody = null;

			try {
				using ( var service = new SpotUploadService.ServiceProviderSoapClient() )
					responsebody = service.Login( username, password );

				if ( responsebody == "invalid" ) // Account doesn't exist
				{
					Log.Error( String.Format("API call to Login answered 'invalid' for username '{0}' and pkswd '{1}'", username, password ));
					return null;
				}
				else if ( responsebody == "confirmation_pending" ) {
					Log.Error( "API call to Login returned unhandled confirmation_pending" );
					return null;
				}
				else {
					Log.Info( "API call to Login returned API Key " + responsebody );
					return responsebody; // return the API key
				}
			}
			catch ( Exception ex ) {
				Log.Error( string.Format( "Login failed: Body:{0}, Exception:{1}", responsebody, ex.ToString() ) );
				return null;
			}
		}
		private static bool _CreateUser( string username, string password )
		{
			Log.Info( string.Format( "Creating user {0}", username ) );
			string responsebody = null;

			try {
				using ( var service = new SpotUploadService.ServiceProviderSoapClient() )
					responsebody = service.CreateUser( username, password );

				if ( responsebody == "NO ACCESS" )
					Log.Error( "API call to CreateUser returned 'NO ACCESS' – IP address the request came from was not whitelisted" );
				else if ( responsebody == "OK" ) {
					Log.Error( "API call to CreateUser succeeded" );
					return true;
				}
			}
			catch ( Exception ex ) {
				Log.Error( string.Format( "CreateUser failed: Body:{0}, Exception:{1}", responsebody, ex.ToString() ) );
			}

			return false;
		}

		public static string Hash( string strPassword )
		{
			if ( strPassword == null )
				return null;
			byte[] inputArray = System.Text.Encoding.UTF8.GetBytes( strPassword );
			using ( var cryptoProvider = new System.Security.Cryptography.SHA256Managed() ) {
				byte[] outputArray = cryptoProvider.ComputeHash( inputArray );
				string outputString = BitConverter.ToString( outputArray );
				return outputString.Replace( "-", "" ); // as BitConverter generates "-" separators
			}
		}
		#endregion


		#region SongStatusUpdaterService

		/// <summary>
		/// This service runs in background and updates song status/duration
		/// During song updates there is cases when song duration is not updated and is set to 0
		/// This service fix those issues
		/// </summary>
		public class SongStatusUpdaterService
		{
			Thread _queueingThread;
			bool _run;

			public void Start()
			{
				try {
					_queueingThread = new Thread( QueueingServiceThreadProc ) { Name = "Song Status Updater Service" };
					_run = true;
					_queueingThread.Start();
				}
				catch ( Exception err ) {
					Log.Error( err );
				}
			}

			public void Stop()
			{
				_run = false;
				_queueingThread.Join();
			}

			void QueueingServiceThreadProc()
			{
				while ( _run ) {
					try {
						string id = ConfigurationManager.AppSettings["SpotUpload.SongStatusUpdaterServiceUserId"];
						var apiKey = Model.SpotUpload.Login( new TaggerUser { Id = id } );
						if ( !String.IsNullOrEmpty( apiKey ) ) {
							var pendingSongs = Song.GetPendingSongs();
							Log.Info( "SongStatusUpdaterService Processing {0} songs", pendingSongs.Count );

							foreach ( var song in pendingSongs ) {
								var status = SpotUpload.GetSampleStatus( apiKey, song.PksId );
								if ( status.IndexOf( "QUEUED " ) != -1 ) {

									if ( song.Created < DateTime.Now.AddDays( -2 ) ) {
										Log.Info( String.Format( "Update status of the song with id {0} to Processed", song.Id ) );
										//don't include this song in calculation anymore
										Song.UpdateStatus( song.Id, SongStatus.Processed );
									}

								}
								else {
									Log.Info( String.Format( "SongStatusUpdaterService songID {0} have status {1}", song.Id, status ) );
								}
							}
						}
						else {
							Log.Error( "Problem with starting SongStatusUpdaterService NO API Key" );
						}


					}
					catch ( Exception ex ) {
						Log.Error( ex );
					}

					Thread.Sleep( TimeSpan.FromMinutes( 5 ) );
				}
			}
		}

		#endregion
	}
}