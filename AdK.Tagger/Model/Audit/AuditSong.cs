using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model.Audit
{
	public class AuditSong
	{
		public int Id { get; set; }
		public int AuditId { get; set; }
		public Guid SongId { get; set; }
		public string Title { get; set; }

		public static void Save(MySqlConnection conn, MySqlTransaction tran, IEnumerable<AuditSong> auditSongs )
		{
			if ( auditSongs != null && auditSongs.Any() ) {
				string query = "INSERT INTO audits_songs (audit_id, song_id)  VALUES ";
				string separator = "";
				foreach ( var channel in auditSongs ) {
					query = String.Format( "{0} {1} ({2}, '{3}')", query, separator, channel.AuditId, channel.SongId );
					separator = ",";
				}

				Database.Insert( conn, tran, query );
			}
		}
	}
}