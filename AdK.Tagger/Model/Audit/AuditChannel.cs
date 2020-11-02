using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;
using MySql.Data.MySqlClient;

namespace AdK.Tagger.Model.Audit
{
	public class AuditChannel
	{

		public AuditChannel()
		{

		}

		public int Id { get; set; }
		[ColumnNameAttr("audit_id")]
		public int AuditId { get; set; }
		[ColumnNameAttr( "channel_id" )]
		public Guid ChannelId { get; set; }
		public string Name { get; set; }
		public decimal MatchThreshold { get; set; }

		public static void Save(MySqlConnection conn, MySqlTransaction tran, IEnumerable<AuditChannel> auditChannes )
		{
			if ( auditChannes != null && auditChannes.Any() ) {
				string query = "INSERT INTO audits_channels (audit_id, channel_id)  VALUES ";
				string separator = "";
				foreach ( var channel in auditChannes ) {
					query = String.Format( "{0} {1} ({2}, '{3}')", query, separator, channel.AuditId, channel.ChannelId );
					separator = ",";
				}

				Database.Insert( conn, tran, query );
			}
		}


		public static AuditChannel GetAuditChannel(int auditId, Guid channelId)
		{
			string query = String.Format("SELECT id, audit_id, channel_id FROM audits_channels WHERE audit_id={0} AND channel_id='{1}'", auditId, channelId);
			return Database.ItemFetcher( query, dr =>
				new AuditChannel {
					Id = dr.GetInt32( 0 ),
					AuditId = dr.GetInt32( 1 ),
					ChannelId = dr.GetGuid( 2 )
				} );
		}
	}
}