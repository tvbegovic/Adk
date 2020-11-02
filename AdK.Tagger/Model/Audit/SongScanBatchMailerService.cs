using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AdK.Tagger.Model.AppSettings;
using DatabaseCommon;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace AdK.Tagger.Model.Audit
{
	public class SongsToMail
	{
		public Guid SongId { get; set; }
		public string Email { get; set; }
	}

	public class SongScanBatchMailerService
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		Thread _queueingThread;
		bool _run;

		public void Start()
		{
			try {
				_queueingThread = new Thread( QueueingServiceThreadProc ) { Name = "Song scan Batch mailer service" };
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
					var auditSettings = AuditSettings.Get();

					if ( auditSettings.SendMailOnSpotScan ) {
						DateTime? lastMailTime = Settings.Get( "Audit", "SpotScanMailSendOn", (DateTime?)null );


						//start mailing process
						if ( lastMailTime.GetValueOrDefault().Date < DateTime.Now.Date ) {

							var songsToMail = GetSongsForMailing();

							if ( !songsToMail.Any() ) {
								Log.Info( String.Format( "Finish checking SpotScanSongSend there is no new scanned spots" ) );
							}

							UpdateStatusToMailed(songsToMail.Select(s => s.SongId));

							var songsToMailGrouped = songsToMail.GroupBy( s => s.Email );


							foreach ( var userToMail in songsToMailGrouped ) {
								string mailTo = userToMail.Key;
								int numberOfSongs = userToMail.Count();
								string subject = auditSettings.SpotScanMailSubject;
								string body = auditSettings.SpotScanMailBody
									.Replace( "[numberOfScannedSpots]", numberOfSongs.ToString() );


								if ( !String.IsNullOrEmpty( auditSettings.SpotScanMailMockReciver ) ) {
									subject = String.Format( "{0} original-recipient: <{1}>", subject, mailTo );
									mailTo = auditSettings.SpotScanMailMockReciver;
								}

								Log.Info( "Sending SpotScan mail to {0} {1} {2}", mailTo, subject, body );
								Mailer.Send( mailTo, subject, body, isHtml: true );

							}

							Settings.Set( "Audit", "SpotScanMailSendOn", DateTime.Now );
						}
						else {
							Log.Info( String.Format( "SpotScanMailSendOn date is same as date we are checking. Mails have already been sent" ) );
						}

					}
					else {
						Log.Info( String.Format( "SendMailOnSpotScan is false skip checking" ) );
					}

					//every hour check for previous date
					var sleep = TimeSpan.FromHours( 1 );
					Thread.Sleep( sleep );

				}
				catch ( Exception ex ) {
					Log.Error( ex );
				}


			}
		}

		private List<SongsToMail> GetSongsForMailing()
		{
			var now = DateTime.Now;

			var query = String.Format( @"SELECT s.id, u.email
				FROM songs s
				LEFT JOIN users u ON u.id = s.user_id
				WHERE status= {1}", now.AddDays( -1 ).Date, (int)SongStatus.Processed );

			return Database.ListFetcher( query,
				dr => new SongsToMail {
					SongId = dr.GetGuid( 0 ),
					Email = dr.GetStringOrDefault( 1 )
				} );
		}


		private void UpdateStatusToMailed( IEnumerable<Guid> songIds )
		{
			if ( songIds != null && songIds.Any() ) {
				string query = String.Format( @"UPDATE songs Set status = {0}
					WHERE id {1}", (int)SongStatus.Mailed, Database.InClause( songIds ) );

				Database.Insert( query );
			}
		}
	}
}