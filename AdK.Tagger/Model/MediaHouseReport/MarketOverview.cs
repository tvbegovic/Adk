using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using AdK.Tagger.Model.Reporting;
using DatabaseCommon;
using MySql.Data.MySqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions;

namespace AdK.Tagger.Model.MediaHouseReport
{
	public class MarketOverviewModel
	{
		public MarketOverviewModel() { }
		public MarketOverviewModel( string key, decimal? value )
		{
			Key = key;
			Value = value;
		}
		public string Key { get; set; }
		public decimal? Value { get; set; }
	}

	public class MarketOverviewDbResult
	{
		public string MediaType { get; set; }
		public string Name { get; set; }
		public decimal Earns { get; set; }
		public decimal AirTime { get; set; }
	}

	public class MarketOverview : ReportBase
	{
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public List<MarketOverviewModel> AllMediaBySales { get; set; }
		public List<MarketOverviewModel> RadioBySales { get; set; }
		public List<MarketOverviewModel> TelevisionBySales { get; set; }
		public List<MarketOverviewModel> AllMediaBySpotTime { get; set; }
		public List<MarketOverviewModel> RadioBySpotTime { get; set; }
		public List<MarketOverviewModel> TelevisionBySpotTime { get; set; }

		private bool _sortByPreviousPeriod;
		private string _marketFilter { get; set; }
		private List<MarketOverviewDbResult> _dbResults { get; set; }
		private List<MarketOverviewDbResult> _dbResultsForRadio { get; set; }
		private List<MarketOverviewDbResult> _dbResultsForTv { get; set; }


		public MarketOverview( string userId, PeriodInfo period, bool sortByPreviousPeriod, GroupingValue value, string by, string marketId )
			: base( userId, value, period )
		{
			_sortByPreviousPeriod = sortByPreviousPeriod;
			_dbResults = new List<MarketOverviewDbResult>();
			_dbResultsForRadio = new List<MarketOverviewDbResult>();
			_dbResultsForTv = new List<MarketOverviewDbResult>();

			_marketFilter = "";
			if ( !String.IsNullOrWhiteSpace( marketId ) && marketId.ToLower() != "all" ) {
				var marketChannels = MarketChannels.GetMarketChannels( marketId ).Select( mc => mc.ChannelId );
				if ( marketChannels.Any() ) {
					_marketFilter = String.Format( "AND r.channel_id {0}", Database.InClause( marketChannels ) );
				}
				else {
					return; //no channels no data to display
				}
			}

			if ( by == "MediaHouse" ) {
				_setAllMediaByMediaHousesDbResults();
			}
			else {
				_setAllMediaByOwnerDbResults( userId );
			}

			AllMediaBySales = _filtrateAndGroupOthers( _dbResults.Select( _earnsMapper ) );
			RadioBySales = _filtrateAndGroupOthers( _dbResultsForRadio.Select( _earnsMapper ) );
			TelevisionBySales = _filtrateAndGroupOthers( _dbResultsForTv.Select( _earnsMapper ) );

			AllMediaBySpotTime = _filtrateAndGroupOthers( _dbResults.Select( _airTimeMapper ) );
			RadioBySpotTime = _filtrateAndGroupOthers( _dbResultsForRadio.Select( _airTimeMapper ) );
			TelevisionBySpotTime = _filtrateAndGroupOthers( _dbResultsForTv.Select( _airTimeMapper ) );

		}

		private void _setAllMediaByMediaHousesDbResults()
		{
			using ( var conn = Database.Get() ) {
				var cmd = conn.CreateCommand();
				cmd.CommandText = string.Format( @"SELECT c.station_name, r.media_type, SUM(r.earns),  SUM(r.duration)
                FROM report_base_cache r
                LEFT JOIN channels c on r.channel_id = c.id
                WHERE r.play_date >= @start AND r.play_date < @end
					 {0}
                GROUP BY c.station_name
                ORDER BY SUM(r.earns) DESC", _marketFilter );
                var start = _sortByPreviousPeriod ? _Period.PreviousStart : _Period.CurrentStart;
                var end = _sortByPreviousPeriod ? _Period.PreviousEnd : _Period.CurrentEnd;
                cmd.Parameters.AddWithValue( "@start", start );
				cmd.Parameters.AddWithValue( "@end", end );

                Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    {2}
                ", start, end, cmd.CommandText));

                _dbResults = _getDbResults( cmd );
			}

			_dbResults.ForEach( db => {
				if ( db.MediaType == "Radio" ) _dbResultsForRadio.Add( db );
				else if ( db.MediaType == "TV" ) _dbResultsForTv.Add( db );
			} );

		}


		private void _setAllMediaByOwnerDbResults( string userId )
		{
			using ( var conn = Database.Get() ) {
				_dbResultsForTv = _getDbResults( _getMediaByOwnerCommand( "Tv", userId, conn ) );
				_dbResultsForRadio = _getDbResults( _getMediaByOwnerCommand( "Radio", userId, conn ) );
			}

			_dbResultsForRadio.ForEach( r => _dbResults.Add( new MarketOverviewDbResult {
				Name = r.Name,
				AirTime = r.AirTime,
				Earns = r.Earns,
				MediaType = r.MediaType
			} ) );

			_dbResultsForTv.ForEach( tv => {
				var all = _dbResults.FirstOrDefault( rs => rs.Name == tv.Name );
				if ( all != null ) {
					all.AirTime += tv.AirTime;
					all.Earns += tv.Earns;
				}
				else {
					_dbResults.Add( tv );
				}
			} );

		}

		private MySqlCommand _getMediaByOwnerCommand( string media, string userId, MySqlConnection conn )
		{
			var cmd = conn.CreateCommand();
			cmd.CommandText = string.Format( @"SELECT h.name, r.media_type, SUM(r.earns), SUM(r.duration)
						FROM report_base_cache r
						JOIN channel_group cg ON cg.channel_id = r.channel_id
						JOIN `group` g ON g.id = cg.group_id
						JOIN holding h ON h.id = g.holding_id
						WHERE r.play_date >= @start AND r.play_date < @end AND h.user_id = @user_id AND r.media_type = '{0}'
						{1}
						GROUP BY h.name", media, _marketFilter );

            var start = _sortByPreviousPeriod ? _Period.PreviousStart : _Period.CurrentStart;
            var end = _sortByPreviousPeriod ? _Period.PreviousEnd : _Period.CurrentEnd;

            cmd.Parameters.AddWithValue( "@start", start );
			cmd.Parameters.AddWithValue( "@end", end );
			cmd.Parameters.AddWithValue( "@user_id", userId );

            Log.Info(string.Format(@"
                    SET @start:='{0:yyyy-MM-dd}'; 
                    SET @end:='{1:yyyy-MM-dd}'; 
                    SET @user_id:='{2}'; 
                    {3}
                ", start, end, userId, cmd.CommandText));


            return cmd;
		}

		private List<MarketOverviewDbResult> _getDbResults( MySqlCommand cmd )
		{
			var dbResults = new List<MarketOverviewDbResult>();

			using ( var dr = cmd.ExecuteReader() ) {
				while ( dr.Read() ) {
					if ( !dr.IsDBNull( 0 ) ) {
						dbResults.Add( new MarketOverviewDbResult {
							Name = dr.GetString( 0 ),
							MediaType = dr.GetStringOrDefault( 1 ),
							Earns = dr.GetDecimalOrDefault( 2 ),
							AirTime = dr.GetDecimalOrDefault( 3 )
						} );

					}
				}
			}

			return dbResults;
		}


		private List<MarketOverviewModel> _filtrateAndGroupOthers( IEnumerable<MarketOverviewModel> allResults )
		{
			var groupedResult = new List<MarketOverviewModel>();
			decimal? total = allResults.Sum( ms => ms.Value );
			var min = total / 100 * 3;
			decimal? sumOfOthers = 0;

			foreach ( var row in allResults ) {
				if ( row.Value > min ) {
					groupedResult.Add( row );
				}
				else {
					sumOfOthers += row.Value.GetValueOrDefault();
				}
			}

			if ( sumOfOthers != 0 ) {
				groupedResult.Add( new MarketOverviewModel {
					Key = "Others",
					Value = sumOfOthers
				} );
			}

			return groupedResult;
		}

		private MarketOverviewModel _earnsMapper( MarketOverviewDbResult dr )
		{
			return new MarketOverviewModel( dr.Name, dr.Earns );
		}

		private MarketOverviewModel _airTimeMapper( MarketOverviewDbResult dr )
		{
			return new MarketOverviewModel( dr.Name, dr.AirTime );
		}

	}
}