using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model.Audit
{
	public class AuditChannelThreshold
	{
		public int Id { get; set; }
		[ColumnNameAttr( "audit_channel_id" )]
		public int AuditChannelId { get; set; }
		public decimal Threshold { get; set; }

		[ColumnNameAttr( "channel_id" )]
		public Guid ChannelId { get; set; }
		[ColumnNameAttr( "user_id" )]
		public Guid UserId { get; set; }

		[ColumnNameAttr( "audit_id" )]
		public int AuditId { get; set; }

		public static void Upsert( int auditChannelId, double threshold )
		{
			string query = String.Format( @"INSERT INTO audits_channels_threshold 
				(audit_channel_id, threshold) Values({0}, {1})
			ON DUPLICATE KEY UPDATE threshold = {1}", auditChannelId, threshold );

			Database.Insert( query );
		}

		public static List<AuditChannelThreshold> GetForAudit( int auditId )
		{
			string query = String.Format( @"SELECT ac.audit_id, ac.channel_id, t.threshold 
				FROM audits_channels_threshold t
				LEFT JOIN audits_channels ac ON ac.id = t.audit_channel_id
				WHERE ac.audit_id = {0}", auditId );

			return Database.ListFetcher( query, ( dr ) => new AuditChannelThreshold {
				AuditId = dr.GetInt32( 0 ),
				ChannelId = dr.GetGuidOrDefault( 1 ),
				Threshold = dr.GetDecimalOrDefault( 2 )
			} );
		}

		public static AuditChannelThreshold GetForAuditChannel( int auditChannelId )
		{
			string query = String.Format( @"SELECT ac.audit_id, ac.channel_id, t.threshold, a.user_id
				FROM audits_channels_threshold t
				LEFT JOIN audits_channels ac ON ac.id = t.audit_channel_id
				LEFT JOIN audits a ON a.id = ac.audit_id
				WHERE t.audit_channel_id = {0}", auditChannelId );

			return Database.ItemFetcher<AuditChannelThreshold>( query );

		}

	}
}