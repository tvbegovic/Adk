using DatabaseCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdK.Tagger.Model
{
	public class Sample
	{
		public Guid Id;
		public string Pksid;
		public DateTime? Created;
		public decimal Duration;
		public string Title;
		public string Product;
		public string Brand;
		public string Company;
		public string Category;
		public string Campaign;
		public string MessageType;
		public int? Queued;
		public int Transcribed;
		public byte Priority; // Ranges from 0 (lower priority 'Idle') to 100 (highest priority 'Immediate'). Default: 50 'Normal'
		public int CorrectionPendingCount;
		public int CorrectedCount;
		public int MasterCount;
		public int TrashedCount;

		public static List<Sample> GetFiltered(
			int pageNum, int pageSize,
			string sortColumn, bool ascending,
			DateTime? startDate, DateTime? endDate, string text,
			object status, object product, object category, object company, object brand, string userId,
			out int totalCount)
		{
			#region Order by
			string orderBy;
			switch (sortColumn)
			{
				case "title":
					orderBy = "s.title";
					break;
				case "company":
					orderBy = "a.company_name";
					break;
				case "brand":
					orderBy = "b.brand_name";
					break;
				case "category":
					orderBy = "ac.category_name";
					break;
				case "product":
					orderBy = "p.product_name";
					break;
				case "duration":
					orderBy = "s.duration";
					break;
				default:
					orderBy = "s.created";
					break;
			}
			if (!ascending)
				orderBy += " DESC";
			#endregion

			#region Where date filter
			string whereCreated = "";
			if (startDate.HasValue)
				whereCreated += "s.created >= @startDate AND ";
			if (endDate.HasValue)
				whereCreated += "s.created < @endDate AND ";
			#endregion

			#region Where filters
			#region Text
			string whereText = "1 = 1";
			if (!string.IsNullOrWhiteSpace(text))
				whereText = @"s.title LIKE @text";
			#endregion

			#region Status
			string sStatus = status is string ? (string)status : null;
			string whereStatus = "1 = 1";
			if (status is bool && !(bool)status) // status == false: "(Blanks)"
				whereStatus = "wq.queue_length IS NULL";
			else if (status is bool && (bool)status) // status == true: "(Non blanks)"
				whereStatus = "wq.queue_length IS NOT NULL";
			else if (sStatus == "notOrdered")
				whereStatus = "(wq.queue_length IS NULL OR wq.queue_length = 0)";
			else if (sStatus == "queued")
				whereStatus = "wq.queue_length IS NOT NULL AND wq.queue_done IS NOT NULL AND wq.queue_length > wq.queue_done";
			else if (sStatus == "done" || sStatus == "inReview" || sStatus == "completed")
				whereStatus = "wq.queue_length IS NOT NULL AND wq.queue_done IS NOT NULL AND wq.queue_length <= wq.queue_done";

			string having = "";
			if (sStatus == "done")
				having = "HAVING masterCount = 0 AND correctionPendingCount = 0 AND correctedCount = 0";
			else if (sStatus == "completed")
				having = "HAVING masterCount > 0";
			else if (sStatus == "inReview")
				having = "HAVING (correctionPendingCount > 0 OR correctedCount > 0)";
			#endregion

			#region Product
			string whereProduct = "1 = 1";
			if (product is bool && !(bool)product) // product == false: "(Blanks)"
				whereProduct = "p.product_name IS NULL";
			else if (product is bool && (bool)product) // product == true: "(Non blanks)"
				whereProduct = "p.product_name IS NOT NULL";
			else if (product is string)
				whereProduct = "p.product_name = '" + ((string)product).Replace("'", "''") + "'";
			#endregion

			#region Category
			string whereCategory = "1 = 1";
			if (category is bool && !(bool)category) // category == false: "(Blanks)"
				whereCategory = "ac.category_name IS NULL";
			else if (category is bool && (bool)category) // category == true: "(Non blanks)"
				whereCategory = "ac.category_name IS NOT NULL";
			else if (category is string)
				whereCategory = "ac.category_name = '" + ((string)category).Replace("'", "''") + "'";
			#endregion

			#region Company
			string whereCompany = "1 = 1";
			if (company is bool && !(bool)company) // company == false: "(Blanks)"
				whereCompany = "a.company_name IS NULL";
			else if (company is bool && (bool)company) // company == true: "(Non blanks)"
				whereCompany = "a.company_name IS NOT NULL";
			else if (company is string)
				whereCompany = "a.company_name = '" + ((string)company).Replace("'", "''") + "'";
			#endregion

			#region Brand
			string whereBrand = "1 = 1";
			if (brand is bool && !(bool)brand) // brand == false: "(Blanks)"
				whereBrand = "b.brand_name IS NULL";
			else if (brand is bool && (bool)brand) // brand == true: "(Non blanks)"
				whereBrand = "b.brand_name IS NOT NULL";
			else if (brand is string)
				whereBrand = "b.brand_name = '" + ((string)brand).Replace("'", "''") + "'";
			#endregion
			#endregion

			#region SQL
			#region fromAndWhere
			string fromAndWhere = string.Format(@"
FROM songs s

LEFT JOIN
(SELECT song_id, queue_length, queue_done, priority FROM
	(SELECT
		song_id,
		SUM(request_count) as queue_length,
		(SELECT COUNT(id) FROM song_transcript st WHERE st.song_id = tq.song_id AND st.training = 0 AND st.review_of IS NULL) as queue_done,
		MAX(priority) as priority
	FROM song_transcript_queue tq
	INNER JOIN songs ON songs.id = tq.song_id
	GROUP BY tq.song_id) as done
) wq
ON s.id = wq.song_id

LEFT JOIN products p ON p.id = s.product_id
LEFT JOIN brands b ON b.id = s.brand_id
LEFT JOIN advertisers a ON a.id = b.advertiser_id
LEFT JOIN ad_categories ac ON ac.id = s.category_id
LEFT JOIN industries i ON i.id = b.industry_id

WHERE {1} # optional date clause
{2} AND {3} AND {4} AND {5} AND {6} AND {7} # where clause for each column
 AND (@userId IS NULL OR s.user_id = @userId) AND COALESCE(s.deleted,0) = 0
{8} # having for status

ORDER BY {0}
",
orderBy, // {0}
whereCreated, // {1}
whereText, whereStatus, whereProduct, whereCategory, whereCompany, whereBrand, // {2} .. {7}
having); // {8}
			#endregion

			string selectStatusCount = @"
	(SELECT COUNT(id) FROM song_transcript WHERE song_transcript.song_id = s.id AND status = 1) as correctionPendingCount,
	(SELECT COUNT(id) FROM song_transcript WHERE song_transcript.song_id = s.id AND status = 2 AND is_master = 0) as correctedCount,
	(SELECT COUNT(id) FROM song_transcript WHERE song_transcript.song_id = s.id AND status = 2 AND is_master = 1) as masterCount,
	(SELECT COUNT(id) FROM song_transcript WHERE song_transcript.song_id = s.id AND status = 3) as trashedCount
";
			string selectTotalCount = "SELECT COUNT(*) FROM (SELECT " + selectStatusCount + fromAndWhere + ") as mainselect";

			#region selectSamples
			string selectSamples = @"
SELECT s.id, s.created, s.pksid, s.duration, s.title,
	p.product_name, b.brand_name, a.company_name, ac.category_name, s.campaign, s.message_type,
	wq.queue_length, IFNULL(wq.queue_done, 0), wq.priority, " + selectStatusCount +
	fromAndWhere +
	" LIMIT @offset, @max";
			#endregion
			#endregion

			using (var db = Database.Get())
			{
				#region get total count
				var cmdTotalCount = db.CreateCommand();
				cmdTotalCount.CommandText = selectTotalCount;
				cmdTotalCount.Parameters.AddWithValue("@userId", userId != null ? (object) userId : DBNull.Value);
				if (startDate.HasValue)
					cmdTotalCount.Parameters.AddWithValue("@startDate", startDate.Value.Date);
				if (endDate.HasValue)
					cmdTotalCount.Parameters.AddWithValue("@endDate", endDate.Value.Date.AddDays(1));
				if (!string.IsNullOrWhiteSpace(text))
					cmdTotalCount.Parameters.AddWithValue("@text", string.Format("%{0}%", text));

				totalCount = Convert.ToInt32(cmdTotalCount.ExecuteScalar());
				#endregion

				#region get samples
				var cmdSamples = db.CreateCommand();
				cmdSamples.CommandText = selectSamples;
				cmdSamples.Parameters.AddWithValue("@userId", userId != null ? (object)userId : DBNull.Value);
				cmdSamples.Parameters.AddWithValue("@offset", pageSize * pageNum);
				cmdSamples.Parameters.AddWithValue("@max", pageSize);
				if (startDate.HasValue)
					cmdSamples.Parameters.AddWithValue("@startDate", startDate.Value.Date);
				if (endDate.HasValue)
					cmdSamples.Parameters.AddWithValue("@endDate", endDate.Value.Date.AddDays(1));
				if (!string.IsNullOrWhiteSpace(text))
					cmdSamples.Parameters.AddWithValue("@text", string.Format("%{0}%", text));

				var samples = new List<Sample>();
				using (var dr = cmdSamples.ExecuteReader())
				{
					while (dr.Read())
					{
						var sample = new Sample
						{
							Id = dr.GetGuid(0),
							Created = dr.IsDBNull(1) ? (DateTime?)null : dr.GetDateTime(1),
							Pksid = dr.GetString(2),
							Duration = dr.GetDecimal(3),
							Title = dr.IsDBNull(4) ? null : dr.GetString(4),
							Product = dr.IsDBNull(5) ? null : dr.GetString(5),
							Brand = dr.IsDBNull(6) ? null : dr.GetString(6),
							Company = dr.IsDBNull(7) ? null : dr.GetString(7),
							Category = dr.IsDBNull(8) ? null : dr.GetString(8),
							Campaign = dr.IsDBNull(9) ? null : dr.GetString(9),
							MessageType = dr.IsDBNull(10) ? null : dr.GetString(10),
							Queued = dr.IsDBNull(11) ? (int?)null : dr.GetInt32(11),
							Transcribed = dr.GetInt32(12),
							Priority = dr.GetByte(13),
							CorrectionPendingCount = dr.GetInt32(14),
							CorrectedCount = dr.GetInt32(15),
							MasterCount = dr.GetInt32(16),
							TrashedCount = dr.GetInt32(17)
						};
						samples.Add(sample);
					}
				}
				#endregion

				return samples;
			}
		}

		public static int SetPriority(
			DateTime? startDate, DateTime? endDate, string text,
			object status, object product, object category, object company, object brand,
			byte priority)
		{
			#region Where date filter
			string whereCreated = "";
			if (startDate.HasValue)
				whereCreated += "s.created >= @startDate AND ";
			if (endDate.HasValue)
				whereCreated += "s.created < @endDate AND ";
			#endregion

			#region Where filters
			#region Text
			string whereText = "1 = 1";
			if (!string.IsNullOrWhiteSpace(text))
				whereText = @"s.title LIKE @text";
			#endregion

			#region "Ordered" status
			string whereStatus = "wq.queue_length IS NOT NULL AND wq.queue_done IS NOT NULL AND wq.queue_length > wq.queue_done";
			#endregion

			#region Product
			string whereProduct = "1 = 1";
			if (product is bool && !(bool)product) // product == false: "(Blanks)"
				whereProduct = "p.product_name IS NULL";
			else if (product is bool && (bool)product) // product == true: "(Non blanks)"
				whereProduct = "p.product_name IS NOT NULL";
			else if (product is string)
				whereProduct = "p.product_name = '" + ((string)product).Replace("'", "''") + "'";
			#endregion

			#region Category
			string whereCategory = "1 = 1";
			if (category is bool && !(bool)category) // category == false: "(Blanks)"
				whereCategory = "ac.category_name IS NULL";
			else if (category is bool && (bool)category) // category == true: "(Non blanks)"
				whereCategory = "ac.category_name IS NOT NULL";
			else if (category is string)
				whereCategory = "ac.category_name = '" + ((string)category).Replace("'", "''") + "'";
			#endregion

			#region Company
			string whereCompany = "1 = 1";
			if (company is bool && !(bool)company) // company == false: "(Blanks)"
				whereCompany = "a.company_name IS NULL";
			else if (company is bool && (bool)company) // company == true: "(Non blanks)"
				whereCompany = "a.company_name IS NOT NULL";
			else if (company is string)
				whereCompany = "a.company_name = '" + ((string)company).Replace("'", "''") + "'";
			#endregion

			#region Brand
			string whereBrand = "1 = 1";
			if (brand is bool && !(bool)brand) // brand == false: "(Blanks)"
				whereBrand = "b.brand_name IS NULL";
			else if (brand is bool && (bool)brand) // brand == true: "(Non blanks)"
				whereBrand = "b.brand_name IS NOT NULL";
			else if (brand is string)
				whereBrand = "b.brand_name = '" + ((string)brand).Replace("'", "''") + "'";
			#endregion
			#endregion

			using (var db = Database.Get())
			{
				var command = db.CreateCommand();
				command.CommandText = string.Format(@"
UPDATE song_transcript_queue SET priority = @priority WHERE song_id IN
(SELECT s.id
FROM songs s

LEFT JOIN
(SELECT song_id, queue_length, queue_done, priority FROM
	(SELECT
		song_id,
		SUM(request_count) as queue_length,
		(SELECT COUNT(id) FROM song_transcript st WHERE st.song_id = tq.song_id AND st.training = 0 AND st.review_of IS NULL) as queue_done,
		MAX(priority) as priority
	FROM song_transcript_queue tq
	INNER JOIN songs ON songs.id = tq.song_id
	GROUP BY tq.song_id) as done
) as wq
ON s.id = wq.song_id

LEFT JOIN products p ON p.id = s.product_id
LEFT JOIN brands b ON b.id = s.brand_id
LEFT JOIN advertisers a ON a.id = b.advertiser_id
LEFT JOIN ad_categories ac ON ac.id = s.category_id
LEFT JOIN industries i ON i.id = b.industry_id

WHERE {0} # optional date clause
{1} AND {2} AND {3} AND {4} AND {5} AND {6} # where clause for each column
)",
whereCreated, // {0}
whereText, whereStatus, whereProduct, whereCategory, whereCompany, whereBrand); // {1} .. {6}

				if (startDate.HasValue)
					command.Parameters.AddWithValue("@startDate", startDate.Value.Date); // From start of this day
				if (endDate.HasValue)
					command.Parameters.AddWithValue("@endDate", endDate.Value.Date.AddDays(1)); // To end of this day
				if (!string.IsNullOrWhiteSpace(text))
					command.Parameters.AddWithValue("@text", string.Format("%{0}%", text));
				command.Parameters.AddWithValue("@priority", priority);

				return command.ExecuteNonQuery();
			}
		}
		public static int SetPriority(string[] songIds, byte priority)
		{
			string select = "UPDATE song_transcript_queue SET priority = @priority WHERE song_id " + Database.InClause(songIds);
			return Database.ExecuteNonQuery(select, "@priority", priority);
		}
	}
}
