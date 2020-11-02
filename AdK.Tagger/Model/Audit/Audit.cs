using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using AdK.Tagger.Model.MediaHouseReport;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model.Audit
{
    public class Audit
    {
        public Audit()
        {
            AuditChannels = new List<AuditChannel>();
            AuditSongs = new List<AuditSong>();
        }

        public int Id { get; set; }

        [ColumnNameAttr("user_id")]
        public string UserId { get; set; }

        [ColumnNameAttr("date_from")]
        public DateTime DateFrom { get; set; }

        [ColumnNameAttr("date_to")]
        public DateTime DateTo { get; set; }

        public bool Deleted { get; set; }

        public List<AuditSong> AuditSongs { get; set; }
        public List<AuditChannel> AuditChannels { get; set; }


        public static Audit GetAudit(int auditId) {
            string query = String.Format("SELECT * FROM audits WHERE id = {0} AND IFNULL(deleted, 0) = 0", auditId);
            return Database.ItemFetcher<Audit>(query);
        }

		public static IEnumerable<Audit> GetUserAudits( string userId )
		{
			using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();
				cmd.CommandText = @"
				SELECT a.id as audit_id, a.date_from, a.date_to, s.id as song_id, s.title, s.filename, c.id as channel_id, c.external_id, c.match_threshold, auc.Id
				FROM audits a
				LEFT JOIN audits_songs aus ON aus.audit_id = a.id
				LEFT JOIN songs s ON s.id = aus.song_id
				LEFT JOIN audits_channels auc ON auc.audit_id = a.id
				LEFT JOIN channels c ON c.id = auc.channel_id
				WHERE a.user_id = @userId AND IFNULL(a.deleted, 0) = 0
				Order by a.Id DESC";

				cmd.Parameters.AddWithValue( "@userId", userId );

				var auditDic = new Dictionary<int, Audit>();

				using ( var dr = cmd.ExecuteReader() ) {

					while ( dr.Read() ) {

						int auditId = dr.GetInt32( 0 );
						DateTime dateFrom = dr.GetDateTime( 1 );
						DateTime dateTo = dr.GetDateTime( 2 );
						Guid? songId = dr.GetGuidOrNull( 3 );
						string songTitle = dr.GetStringOrDefault( 4 ) != "" ? dr.GetStringOrDefault( 4 ) : dr.GetStringOrDefault( 5 );
						Guid? channelId = dr.GetGuidOrNull( 6 );
						string channelName = dr.GetStringOrDefault( 7 );
						decimal matchThrenshold = dr.GetDecimalOrDefault(8);
						int auditChannelId = dr.GetIntOrDefault(9);

						if ( !auditDic.ContainsKey( auditId ) ) {
							auditDic.Add( auditId, new Audit {
								Id = auditId,
								DateFrom = dateFrom,
								DateTo = dateTo,
							} );
						}

						var audit = auditDic[auditId];

						if (channelId != null && !audit.AuditChannels.Any( ac => ac.ChannelId == channelId ) ) {
							audit.AuditChannels.Add( new AuditChannel {
								Id = auditChannelId,
								AuditId = auditId,
								ChannelId = (Guid)channelId,
								Name = channelName,
								MatchThreshold = matchThrenshold
							} );
						}

						if (songId != null && !audit.AuditSongs.Any( s => s.SongId == songId ) ) {
							audit.AuditSongs.Add( new AuditSong {
								AuditId = auditId,
								SongId = (Guid)songId,
								Title = songTitle
							} );
						}


					}

				}

				return auditDic.ToList().Select( a => a.Value );

			}

		}


		public static int SaveAudit( Audit audit )
		{
			using ( var con = Database.Get() ) {
				using ( var tran = con.BeginTransaction() ) {

					string query = "INSERT INTO audits (user_id, date_from, date_to)  VALUES(@userId, @dateFrom, @dateTo)";
					int auditId = (int)Database.Insert( con, tran, query, "@userId", audit.UserId, "@dateFrom", audit.DateFrom, "@dateTo", audit.DateTo );

					if ( audit.AuditChannels != null ) {
						audit.AuditChannels.ToList().ForEach( ac => ac.AuditId = auditId );
						AuditChannel.Save( con, tran, audit.AuditChannels );
					}

					if ( audit.AuditSongs != null ) {
						audit.AuditSongs.ToList().ForEach( s => s.AuditId = auditId );
						AuditSong.Save( con, tran, audit.AuditSongs );
					}

					tran.Commit();

					return auditId;
				}
			}
		}

        public static void DeleteAudit(int auditId) {
            string query = String.Format(@"UPDATE audits SET deleted = 1 WHERE Id = {0}", auditId);

            Database.ExecuteNonQuery(query);
        }

    }
}