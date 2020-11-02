using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace AdK.Tagger.Model
{
	public class OverpassQuery
	{
		static readonly string _Url = "http://overpass-api.de/api/interpreter";

		public List<City> GetCountryCities(string isoCountryCode)
		{
			long countryId = GetCountryId(isoCountryCode);
			var cities = GetAreaPlaces(countryId);
			cities.ForEach(p => p.country_code = isoCountryCode);
			return cities;
		}
		public long GetCountryId(string isoCountryCode)
		{
			var countries = _QueryJson("area[\"ISO3166-1\"=\"" + isoCountryCode + "\"]");
			return countries.Any() ? countries.First().osm_id : 0;
		}
		public List<City> GetAreaPlaces(long areaId)
		{
			return _QueryJson("area(" + areaId + ")->.searchArea;(node[\"place\"~\"town|city\"](area.searchArea);)");
		}
		private List<City> _QueryJson(string query)
		{
			var parameters = new Dictionary<string, string>() {
				{"data", "[out:json];" + query + ";out body;>;out skel qt;" }
			};
			var json = _QueryApiGet(parameters);
			return _ReceiveResultValue(json);
		}
		private static string _QueryApiGet(Dictionary<string, string> parameters)
		{
			var uri = new Uri(_Url + "?" + _buildQueryString(parameters), UriKind.Absolute);

			using (var webClient = new WebClient())
			{
				webClient.Encoding = Encoding.UTF8;
				return webClient.DownloadString(uri);
			}
		}
		private static string _buildQueryString(Dictionary<string, string> parameters)
		{
			var query = HttpUtility.ParseQueryString("");
			foreach (var kv in parameters)
				query[kv.Key] = kv.Value;
			return query.ToString();
		}
		private List<City> _ReceiveResultValue(string json)
		{
			var ser = new JavaScriptSerializer();
			var overpassResponse = ser.Deserialize<OverpassResponse>(json);
			return overpassResponse.elements
				.Select(e => e.ToCity())
				.ToList();
		}

		class OverpassResponse
		{
			// Disable "never assigned" warning as fields are assigned by JSON parser
#pragma warning disable 0649
			public List<Place> elements;
#pragma warning restore 0649
		}
		class Place
		{
			// Disable "never assigned" warning as fields are assigned by JSON parser
#pragma warning disable 0649
			public long id;
			public decimal lat;
			public decimal lon;

			public Tags tags;

			public class Tags
			{
				public string name;
				public string place;
				public string population;
			}
#pragma warning restore 0649

			public City ToCity()
			{
				return new City
				{
					osm_id = this.id,
					name = this.tags.name,
					kind = this.tags.place,
					lat = this.lat,
					lng = this.lon,
					population = this.tags.population != null ? int.Parse(this.tags.population) : (int?)null
				};
			}
		}
	}
}