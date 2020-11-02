using AdK.Tagger.Model;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdK.Tagger
{
	public class TaggerLabExport : IHttpHandler
	{
		public bool IsReusable
		{
			get { return false; }
		}

		public void ProcessRequest(HttpContext context)
		{
			string userId = context.Request.PathInfo.TrimStart('/');
			var adTaggings = AdTagging.Get(userId, OneAd.SchemaVersion);

			using (var package = new ExcelPackage())
			{
				var worksheet = package.Workbook.Worksheets.Add("Tagging");
				int iRow = 1;
				worksheet.SetValues(new string[]
				{
					"Ad type",
					"Exp date",
					"Campaign",
					"Title",
					"Male",
					"Female",
					"Child",
					"Music bed",
					"Song Title",
					"Song Artist",
					"Issue",
					"Status",

					"Brand",
					"Category",
					"Product or Item",
					"Item type",
					"Advertiser",
					"Industry",
					"Media type",

					"Brand 2",
					"Category 2",
					"Product or Item 2",
					"Item type 2",
					"Advertiser 2",
					"Industry 2",
					"Media type 2",

					"Brand 3",
					"Category 3",
					"Product or Item 3",
					"Item type 3",
					"Advertiser 3",
					"Industry 3",
					"Media type 3",
				}, iRow++);

				foreach (var t in adTaggings)
				{
					var values = new List<object>(new object[]
					{
						t.OneAd.AdType.ToString(),
						t.OneAd.ExpirationDate,
						t.OneAd.Campaign,
						t.OneAd.AdTitle,
						t.OneAd.Voice != null && t.OneAd.Voice.Male ? "Male" : null,
						t.OneAd.Voice != null && t.OneAd.Voice.Female ? "Female" : null,
						t.OneAd.Voice != null && t.OneAd.Voice.Child ? "Child" : null,
						t.OneAd.MusicBed,
						t.OneAd.SongTitle,
						t.OneAd.SongArtist,
						t.TaggingIssue,
						t.Status.ToString(),
					});
					foreach (var fact in t.OneAd.AdFacts.Where(af => !af.IsEmpty()))
						values.AddRange(new object[]
						{
							fact.Brand,
							fact.Category,
							fact.ProductOrItem,
							fact.ItemType.ToString(),
							fact.Advertiser,
							fact.Industry,
							fact.MediaType
						});
					worksheet.SetValues(values, iRow++);
				}

				byte[] content = package.GetAsByteArray();
				_ExportSpreadsheet(context, content);
			}
		}
		private static void _ExportSpreadsheet(HttpContext context, byte[] spreadsheet)
		{
			context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
			context.Response.AddHeader("content-disposition", "attachment; filename=export.xlsx");
			context.Response.BinaryWrite(spreadsheet);
			context.Response.End();
		}
	}
	public static class RangeExtensions
	{
		public static ExcelWorksheet SetValues(this ExcelWorksheet worksheet, IEnumerable<object> values, int iRow, int iCol = 1)
		{
			for (int i = 0; i < values.Count(); ++i)
				worksheet.Cells[iRow, iCol + i].Value = values.ElementAt(i);
			return worksheet;
		}
	}
}