using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DatabaseCommon;

namespace AdK.Tagger.Model
{
	public class ClipFields
	{
		public const int Client = 1;
		public const int ProgramType = 2;
		public const int ProgramNames = 3;
		public const int Event = 4;
		public const int Language = 5;
	}

	public class ClipValues
	{
		public int id { get; set; }
		public int? list_id { get; set; }
		public string text_value { get; set; }

		public static List<ClipValues> Search(int? list_id, string text)
		{
			text += "%";
			return Database.ListFetcher<ClipValues>("SELECT * FROM clip_values WHERE list_id = @list_id AND text_value LIKE @text",
				dr => new ClipValues
				{
					id = dr.GetInt32(0),
					list_id = dr.GetInt32(1),
					text_value = dr.GetString(2)
				},
				"list_id", list_id,
				"text", text);
		}
	}
}
